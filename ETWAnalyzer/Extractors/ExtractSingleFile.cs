//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Commands;
using ETWAnalyzer.Configuration;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Modules;
using ETWAnalyzer.Helper;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.Reader.ProcessTools;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extractors
{
    /// <summary>
    /// Control extraction of various aspects of an ETL file. As output a Json file is generated which contains the extracted data.
    /// </summary>
    class ExtractSingleFile
    {
        /// <summary>
        /// ETL files to read data from
        /// </summary>
        private readonly string myEtlFile;

        /// <summary>
        /// List of extractors which are executed one by one to retrieve the corresponding data
        /// </summary>
        private readonly List<ExtractorBase> myExtractors;

        /// <summary>
        /// This class contains the extraction results which is serialized as Json file.
        /// </summary>
        readonly ETWExtract myResults = new();
        /// <summary>
        /// temp directory where CSV files and such are generated.
        /// </summary>
        readonly string myOutTempEtlDirectory;

        /// <summary>
        /// Symbol server and local symbol server cached paths.
        /// </summary>
        readonly SymbolPaths mySymbols;

        /// <summary>
        /// Deletes temp etl only if input zip-file was extracted
        /// </summary>
        readonly bool myWasExtracted;

        /// <summary>
        /// When symbols are needed by an extractor this is filled
        /// </summary>
        IPendingResult<ISymbolDataSource> myPendingSymbols;

        /// <summary>
        /// When true output will be compressed.
        /// </summary>
        public bool CompressOutput
        {
            get; private set;
        }

        string Extension => CompressOutput ? TestRun.CompressedExtractExtension : TestRun.ExtractExtension;


        /// <summary>
        /// Extract data from one ETL file
        /// </summary>
        /// <param name="etlOrZipFile">Input etl file</param>
        /// <param name="extractors">List of extractors</param>
        /// <param name="outTempETLDirectory">Temp folder to extract etl files</param>
        /// <param name="symbols">Symbol server and folders.</param>
        /// <param name="afterUnzipCommand">Execute external application after zip file was uncompressed</param>
        /// <param name="bCompress">if true output files are written to a compressed file with extensione <see cref="TestRun.CompressedExtractExtension"/></param>
        public ExtractSingleFile(string etlOrZipFile, List<ExtractorBase> extractors, string outTempETLDirectory, SymbolPaths symbols, string afterUnzipCommand, bool bCompress)
        {
            if (String.IsNullOrWhiteSpace(etlOrZipFile))
            {
                throw new ArgumentException("Pass an existing etl or zip file", nameof(etlOrZipFile));
            }
            if (String.IsNullOrWhiteSpace(outTempETLDirectory))
            {
                throw new ArgumentException("Pass a non null or empty temp output directory", nameof(outTempETLDirectory));
            }
            myOutTempEtlDirectory = outTempETLDirectory;
            CompressOutput = bCompress;
            myExtractors = extractors ?? throw new ArgumentNullException(nameof(extractors));
            mySymbols = symbols;

            myEtlFile = ExtractETLIfZipped(etlOrZipFile, outTempETLDirectory, symbols, out myWasExtracted);
            if (!String.IsNullOrEmpty(afterUnzipCommand) && myWasExtracted)
            {
                ExecutePostUnzipCommand(myEtlFile, afterUnzipCommand);
            }

        }

        private void ExecutePostUnzipCommand(string etlFile, string afterunzipCommand)
        {
            string dir = Path.GetDirectoryName(etlFile);

            afterunzipCommand = afterunzipCommand.Replace(ExtractCommand.ETLFileDirVariable, $"\"{dir}\"");
            afterunzipCommand = afterunzipCommand.Replace(ExtractCommand.ETLFileNameVariable, $"\"{etlFile}\"");

            bool inExeEscaped = false;
            bool inExeNoSpace = false;
            string exe = "";
            string args = "";
            for(int i=0;i< afterunzipCommand.Length;i++)
            {
                if( afterunzipCommand[i] == '"' || afterunzipCommand[i] == '\'')
                {
                    if( inExeEscaped )
                    {
                        exe = afterunzipCommand.Substring(1, i-1);
                        args = i+1 < afterunzipCommand.Length ? afterunzipCommand.Substring(i+1) : "";
                        break;
                    }
                    inExeEscaped = true;
                }
                else if( afterunzipCommand[i] != ' ' && !inExeEscaped)
                {
                    inExeNoSpace = true;
                }
                else if( afterunzipCommand[i] == ' ' && inExeNoSpace)
                {
                    exe = afterunzipCommand.Substring(0, i);
                    args = afterunzipCommand.Substring(i);
                    break;
                }
            }

            ProcessCommand cmd = new ProcessCommand(exe, args);
            string msg = $"Execute post unzip command. Exe: {exe}, Args: {args}";
            if(Program.DebugOutput)
            {
                Console.WriteLine(msg);
            }
            Logger.Info(msg);

            ExecResult res = cmd.Execute(ProcessPriorityClass.BelowNormal);

            msg = $"Command output: {res.AllOutput}";
            if( Program.DebugOutput )
            {
                Console.WriteLine(msg);
            }
            Logger.Info(msg);

        }


        /// <summary>
        /// Extract data from etl file and place generated json file to outputDirectory
        /// </summary>
        /// <param name="outputDirectory">Directory where generated JSON files are put to.</param>
        /// <param name="haveToDeleteTemp">if true temp files are deleted. This can beoverriden by the command line argument -keepTemp option</param>
        /// <returns>File name of serialized output Json file</returns>
        internal IReadOnlyList<string> Execute(OutDir outputDirectory, bool haveToDeleteTemp)
        {
            string outputJsonFile = null;
            List<string> outputFiles = new();
            try
            {
                TraceProcessorBuilder builder = new TraceProcessorBuilder().WithSettings(new TraceProcessorSettings
                {
                    AllowLostEvents = true,
                    AllowTimeInversion = true,
                });

                using ITraceProcessor processor = builder.Build(myEtlFile);

                bool needSymbols = false;
                foreach (ExtractorBase preParse in myExtractors)
                {
                    preParse.RegisterParsers(processor);
                    if (preParse.NeedsSymbols && !needSymbols)
                    {
                        needSymbols = true;
                        myPendingSymbols = processor.UseSymbols();
                    }
                }

                var sw = Stopwatch.StartNew();
                // Parse ETL file with registered parsers to be able to access results
                processor.Process();
                sw.Stop();
                string perfMsg = $"Perf: Loading ETL {sw.Elapsed.TotalSeconds:F0}s";

                if (needSymbols)
                {
                    sw = Stopwatch.StartNew();
                    LoadSymbols();
                    sw.Stop();
                    perfMsg += $" Perf: Loading symbols {sw.Elapsed.TotalSeconds:F0}s";
                }

                Logger.Info(perfMsg);

                perfMsg = "Perf: Extraction:";
                foreach (ExtractorBase options in myExtractors)
                {
                    sw = Stopwatch.StartNew();
                    options.Extract(processor, myResults);
                    sw.Stop();
                    perfMsg += $" {options.GetType().Name} {sw.Elapsed.TotalSeconds:F0}s";
                }
                Logger.Info(perfMsg);
                if (outputDirectory.IsDefault)
                {
                    outputJsonFile = Path.Combine(outputDirectory.OutputDirectory, ExtractSerializer.ExtractFolder, Path.GetFileNameWithoutExtension(myEtlFile) + Extension);
                }
                else
                {
                    // when output directory is explicitly set extract to given folder
                    outputJsonFile = Path.Combine(outputDirectory.OutputDirectory, Path.GetFileNameWithoutExtension(myEtlFile) + Extension);
                }

                outputFiles = SerializeResults(outputJsonFile, myResults);
            }
            finally
            {
                // Delete temp files if -keepTemp is not active
                if (haveToDeleteTemp)
                {
                    DeleteAllTempFiles();
                }
            }

            return outputFiles;
        }

        void LoadSymbols()
        {
            HashSet<string> requiredPdbs = new();
            HashSet<string> loadedPdbs = new();
            foreach (var line in File.ReadLines(ConfigFiles.RequiredPDBs).Where(line => !line.Contains("//")))
            {
                string pdbName = line.Trim().ToLowerInvariant();
                if (!String.IsNullOrEmpty(pdbName))
                {
                    requiredPdbs.Add(pdbName);
                }
            }

            // load symbols. If essential symbols like ntdll.pdb, clr.pdb are present in trace but could not be loaded
            string combinedSymbolPath = mySymbols.GetCombinedSymbolPath(myEtlFile);
            try
            {
                Logger.Info($"Loading symbols. Symbol Cache Folder: {mySymbols.SymCacheFolder}, Symbol Folder: {combinedSymbolPath}");
                LoadSymbolsWithExplicitSymbolPath(combinedSymbolPath);
            }
            finally
            {
                mySymbols.RemoveShortCuts();
            }

            foreach (var pdb in myPendingSymbols.Result.Pdbs)
            {
                if (!pdb.IsLoaded)
                {
                    if( myResults.Modules == null )
                    {
                        myResults.Modules = new Extract.Modules.ModuleContainer();
                    }

                    // Store unresolved pdbs in module data to be later able to resolve symbols
                    // on a different machine from the extracted data.
                    var pdbIdentifier = new PdbIdentifier(Path.GetFileName(pdb.Path), pdb.Id, pdb.Age);
                    myResults.Modules.UnresolvedPdbs.Add(pdbIdentifier);

                    Logger.Info($"Pdb load failure: {pdb.Id} {pdb.Path}");
                    string notloadedPDBFileName = Path.GetFileName(pdb.Path).ToLowerInvariant();
                    if (requiredPdbs.Contains(notloadedPDBFileName))
                    {
                        string symLoadWarning = $"Warning: Essential pdb {notloadedPDBFileName} is present in trace but no symbols could be loaded. Essential pdbs are configured in file {ConfigFiles.RequiredPDBs}";
                        Console.WriteLine(symLoadWarning);
                        Logger.Warn(symLoadWarning);
                    }
                }
            }

            if( myResults?.Modules != null ) // make json readable and binary search enabled
            {
                // The sort uses the default comparer which uses the IComparable interface implementation of PdbIdentifier
                myResults.Modules.UnresolvedPdbs.Sort();
            }

            
        }

        private void LoadSymbolsWithExplicitSymbolPath(string combinedSymbolPath)
        {
            SymCachePath symCachePath = new(mySymbols.SymCacheFolder);
            SymbolPath symPath = new(combinedSymbolPath);
            Task t = myPendingSymbols.Result.LoadSymbolsAsync(symCachePath, symPath, Program.DebugOutput ? ConsoleSymbolLoadingProgress.Instance : null, null);
            t.GetAwaiter().GetResult(); // wait and throw single exception directly instead of AggregateException when task fails https://stackoverflow.com/questions/17284517/is-task-result-the-same-as-getawaiter-getresult
        }

        /// <summary>
        /// Deletes all temp files in folder without .json files
        /// </summary>
        private void DeleteAllTempFiles()
        {
            // Deletes all temp files (myWasExtracted(true): with source etl / myWasExtracted(false): no source etl exists)
            Deleter.DeleteTempFilesAfterExtracting(myOutTempEtlDirectory, myEtlFile, myWasExtracted);
        }

        /// <summary>
        /// If input file is a zip/.7z file extract data file to output folder with ETL file name as subdirectory where 
        /// it will be unzipped to.
        /// </summary>
        /// <param name="etlFileOrZippedFile"></param>
        /// <param name="outputFolder"></param>
        /// <param name="symbols">Gives access to local and remote symbol folder and servers.</param>
        /// <param name="bWasExtracted">determines if temp .etl file exists and if it is necessary to delete it</param>
        /// <returns>Path to unzipped ETL file or input etl file name if it was not zipped.</returns>
        public static string ExtractETLIfZipped(string etlFileOrZippedFile, string outputFolder, SymbolPaths symbols, out bool bWasExtracted)
        {
            bWasExtracted = false;

            string decompressedFile = etlFileOrZippedFile;
            if (!EtlZipCommand.IsETLFile(etlFileOrZippedFile))
            {
                var sw = Stopwatch.StartNew();
                var zipExtract = new EtlZipCommand();
                decompressedFile = zipExtract.Unzip(etlFileOrZippedFile, outputFolder, symbols);
                bWasExtracted = true;
                sw.Stop();
                Logger.Info($"Perf: Zip Extraction in {sw.Elapsed.TotalSeconds:F0}s");
            }

            return decompressedFile;
        }

        /// <summary>
        /// Serialize the extracted data into output json file
        /// </summary>
        /// <param name="outFile"></param>
        /// <param name="result"></param>
        internal static List<string> SerializeResults(string outFile, ETWExtract result)
        {
            List<string> outputFiles = new();
            try
            {
                // create output directory if it does not yet exist
                string outDir = Path.GetDirectoryName(outFile);
                if (!Directory.Exists(outDir))
                {
                    Directory.CreateDirectory(outDir);
                }

                ExtractSerializer serializer = new(outFile);
                outputFiles = serializer.Serialize(result);

            }
            catch (Exception ex)
            {
                throw new SerializationException($"Serialize to file {outFile} failed.", ex);
            };

            return outputFiles;
        }
    }
}