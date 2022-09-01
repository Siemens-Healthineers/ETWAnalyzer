//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Helper;
using ETWAnalyzer.ProcessTools;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ETWAnalyzer
{
    /// <summary>
    /// Decompress zipped ETL file besides the compressed file. If the file was already decompressed to not decompress it again.
    /// Name convention is xxxx.7z/zip results in a xxxx.etl file.
    /// </summary>
    class EtlZipCommand
    {
        const string ZipExt = ".zip";
        const string SevenZipExt = ".7z";
        const string EtlExt = ".etl";

        /// <summary>
        /// Most output contains only strings of no interest we filter them away
        /// </summary>
        const string ExtractingStartStr = "Extracting  ";

        /// <summary>
        /// Error message when deleting input file is not possible due to concurrent unpacking of same file from different archives or when the file is really in use by someone else
        /// </summary>
        const string DeleteErrorStr = "ERROR: Can not delete output file ";

        /// <summary>
        /// File does not exist because someone did remove it since 7z did check last time
        /// </summary>
        const string CannotOpenOutputFile = "ERROR: Can not open output file ";

        /// <summary>
        /// In our ETL archives we share the same log file name which will lead to concurrency violations when we do a parallel unzip. Ignore these errors
        /// </summary>
        public const string SharedLogFile = "7ZipLog.txt";

        /// <summary>
        /// At the end we get an summary how many errors we had
        /// </summary>
        const string ErrorLine = "Sub items Errors: ";


        // see https://sevenzip.osdn.jp/chm/cmdline/exit_codes.htm for return codes of 7zip
        enum ZipReturnCode
        {
            NoError = 0, 
            Warning = 1, // Non fatal e.g. locked files
            Error = 2, 
            InputArgError = 7, 
            OutOfMemory = 8,
            KilledByUser = 255, 
        }

        /// <summary>
        /// Get external command line program 7zip
        /// </summary>
        public static string SevenZipExe
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z.exe");
            }
        }

        /// <summary>
        /// If the ETL file was already unpacked the unzip operation is skipped. By default this is enabled. 
        /// </summary>
        public bool ForceOverwrite
        {
           get;
        }

        /// <summary>
        /// Create an zip command which by default does not overwrite an already unzipped archive again if the resulting
        /// ETL file is already in place. 
        /// </summary>
        public EtlZipCommand():this(false)
        {
        }

        /// <summary>
        /// Create an extractor with given policy
        /// </summary>
        /// <param name="forceOverwrite"></param>
        public EtlZipCommand(bool forceOverwrite)
        {
            ForceOverwrite = forceOverwrite;
        }

        /// <summary>
        /// Check if input file has .ETL extension hence needs no decompression
        /// </summary>
        /// <param name="inputFile">Input file name which can have any extension.</param>
        /// <returns>true if file extension is .etl, false otherwise</returns>
        public static bool IsETLFile(string inputFile)
        {
            string extension = Path.GetExtension(inputFile);
            return extension.ToLowerInvariant() == EtlExt;
        }

        /// <summary>
        /// Unzip a ZIP file which must contain an ETL file.
        /// </summary>
        /// <param name="pathInputFile">Parameter of input file as path to unzip zip file, if input file not a zip file it will return the input file name</param>
        /// <param name="outDir">Uncompress file to optional output directory. If null it is decompressed at the input directory.</param>
        /// <param name="symbols">To prevent MAX_PATH issues with loading too long file names place a link of the xxx.etl.NGenPDB folder to the symbol directory</param>
        /// <returns>Full path of decompressed ETL file or an exception if ETL file is missing.</returns>
        public string Unzip(string pathInputFile, string outDir, SymbolPaths symbols)
        {
            if( String.IsNullOrEmpty(pathInputFile) )
            {
                throw new ArgumentException($"{nameof(pathInputFile)} was null or empty");
            }

            if( symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            string extension = Path.GetExtension(pathInputFile);

            switch (extension.ToLowerInvariant())
            {
                case EtlExt:
                    return pathInputFile;
                case ZipExt:
                case SevenZipExt:
                    return Decompress(pathInputFile, outDir, symbols);
                default:
                    throw new NotSupportedException($"Zip file {pathInputFile} extension '{extension}' was not recognized.");
            }
        }

        /// <summary>
        /// Command line process for unzipping zip file
        /// </summary>
        /// <param name="zipFile">Path of zip file to be unzipped</param>
        /// <param name="outputFolder">Path to where the contents will be unziped to. Can be null</param>
        /// <param name="symbols">To prevent MAX_PATH issues with loading too long file names place a link of the xxx.etl.NGenPDB folder to the symbol directory</param>
        private string Decompress(string zipFile, string outputFolder, SymbolPaths symbols)
        {
            if( String.IsNullOrEmpty(outputFolder) )
            {
                outputFolder = Path.GetDirectoryName(Path.GetFullPath(zipFile));  // we need to expand the path to a full path name or else we do get an empty output folder
            }

            string finalEtLFile = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(zipFile) + ".etl");

            // file already exists. Do not decompress again.
            if(!ForceOverwrite && File.Exists(finalEtLFile))
            {
                Logger.Info($"Output etl file already exists. No extraction or Symbol folder creation necessary for file {finalEtLFile}");
                return finalEtLFile;
            }
            
            var command = new ProcessCommand(SevenZipExe, $"x \"{zipFile}\" -o\"{outputFolder}\" -y -x!{SharedLogFile}");
                        
            ExecResult res = command.Execute(ProcessPriorityClass.BelowNormal);

            ZipReturnCode code = (ZipReturnCode) res.ReturnCode;

            if ( code > ZipReturnCode.Warning) // only fail on fatal errors
            {
                string[] lines = res.AllOutput.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Where(x => !x.StartsWith(ExtractingStartStr, StringComparison.InvariantCultureIgnoreCase)).ToArray();
                string filteredLines = String.Join(Environment.NewLine, lines);

                if(HasSingleFileError(lines) )
                {
                    Logger.Info($"File sharing violation in {SharedLogFile} during unzip. Ignoring"); 
                }
                else
                {
                    try
                    {
                        // in case of unzip error remove all unzipped data to not clutter up the directory with partial data which is never deleted 
                        Deleter.DeleteTempFilesAfterExtracting(outputFolder, finalEtLFile, true);
                    }
                    catch (Exception)
                    { }
                    throw new InvalidDataException($"Unzipping of file {zipFile} failed with error {(int)code}. Args: {res.ExitedProcess.StartInfo.FileName} {res.ExitedProcess.StartInfo.Arguments}. Output: {filteredLines}");
                }
            }

            if ( !File.Exists(finalEtLFile))
            {
                try
                {
                    // in case of unzip error remove all unzipped data to not clutter up the directory with partial data which is never deleted 
                    Deleter.DeleteTempFilesAfterExtracting(outputFolder, finalEtLFile, true);
                }
                catch (Exception)
                {
                }
                throw new InvalidZipContentsException($"Unzipping of file {zipFile} failed. Expected a decompressed ETL file at {finalEtLFile}.", zipFile);
            }

            return finalEtLFile;
        }

        internal static bool HasSingleFileError(string[] lines)
        {
            bool lret = false;
            if (GetSubItemErrors(lines) == 1)
            {
                var sharedErrors = lines.Where(x =>
                ((x.StartsWith(DeleteErrorStr, StringComparison.InvariantCultureIgnoreCase) ||
                  x.StartsWith(CannotOpenOutputFile, StringComparison.InvariantCultureIgnoreCase)) && x.IndexOf(SharedLogFile, StringComparison.InvariantCultureIgnoreCase) != -1));

                if (sharedErrors.Count() == 1)
                {
                    lret = true;
                }
            }

            return lret;
        }

        static int GetSubItemErrors(string[] lines)
        {
            int errorCount = int.MaxValue;
            // Sub items Errors: 1
            string errorLine = lines.Where(x => x.StartsWith(ErrorLine, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if( errorLine != null)
            {
                string intPart = errorLine.Substring(ErrorLine.Length);
                if( int.TryParse(intPart, out int tmpInt) )
                {
                    errorCount = tmpInt;
                }
            }

            return errorCount;
        }
    }
}
