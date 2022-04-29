//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Helper;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using ETWAnalyzer.Extract;

namespace ETWAnalyzer
{
    /// <summary>
    /// Call wpaexporter with a .wpaprofile file to export data from an ETL file to CSV files.
    /// The contents are defined by the wpaProfile which can be used in WPA as viewing profile as well. 
    /// That is the way how you can create a customized export profile by looking in the UI for the things
    /// you want to export and then export that view as .wpaProfile file to use that as input for the exporter.
    /// </summary>
    public class WpaExportCommand
    {
        const string WpaExporterExe = "wpaexporter.exe";

        /// <summary>
        /// Error message when a WPA profile table is exported for which no events can be found in the ETL file. 
        /// </summary>
        const string NoDataWarning = "No data in table for preset ";

        /// <summary>
        /// File name of .wpaProfile file which defines which CSV files are created.
        /// The file names are based on the table names.
        /// </summary>
        public string WpaProfile { get; }

        /// <summary>
        /// File name of ETl file to export data from.
        /// </summary>
        public string InputETLFile { get; }

        /// <summary>
        /// Folder where the exported CSV files are written to
        /// </summary>
        public string OutputFolder { get; }

        /// <summary>
        /// Each exported file is prefixed with this string
        /// </summary>
        public string OutputFilePrefix { get;  }

        /// <summary>
        /// Symbol server path. Used to resolve stack trace method names. When this not null
        /// and remote servers are in the SymbolServer path it can take a long to resolve the relevant stack traces!
        /// </summary>
        public SymbolPaths Symbols { get;  }

        /// <summary>
        /// Declare export options in ctor. The most important thing is that wpaexporter will overwrite the exported
        /// CSV files if multiple etl files are exported to the same directory. The reason is that wpaexporter will use the table names
        /// of the .wpaProfile as output files names. See http://geekswithblogs.net/akraus1/archive/2013/08/03/153594.aspx
        /// </summary>
        /// <param name="etlFile">Path to ETL file to export</param>
        /// <param name="wpaProfile">file name of .wapProfile file used to export data to CSV</param>
        /// <param name="outputFolder">Folder where to export the data. There will be multiple files be created</param>
        /// <param name="outputFilePrefix">Prepend exported CSV files with a prefix.</param>
        /// <param name="symbols">Can be null. If non null the _NT_SYMBOLPATH variable is set with this value to allow stack resolution</param>
        public WpaExportCommand(string etlFile, string wpaProfile, string outputFolder, string outputFilePrefix, SymbolPaths symbols)
        {
            if( String.IsNullOrEmpty(etlFile))
            {
                throw new ArgumentException("Argument is null or empty", nameof(etlFile));
            }
            if( String.IsNullOrEmpty(wpaProfile))
            {
                throw new ArgumentException("Argument is null or empty", nameof(wpaProfile));
            }
            if( String.IsNullOrEmpty(outputFolder))
            {
                throw new ArgumentException("Argument is null or empty", nameof(outputFolder));
            }

            if( !Directory.Exists(outputFolder))
            {
                throw new DirectoryNotFoundException($"Output directory {outputFolder} does not exist.");
            }
            if( !File.Exists(etlFile) )
            {
                throw new FileNotFoundException($"The input etlFile {etlFile} could not be found.");
            }

            string ext = Path.GetExtension(etlFile);
            if( ext.ToLowerInvariant() != ".etl")
            {
                throw new InvalidDataException($"The input etlFile is not an ETL file!. File extension was {ext}. Full file name: {etlFile}");
            }

            WpaProfile = wpaProfile;
            InputETLFile = etlFile;
            OutputFolder = outputFolder;
            OutputFilePrefix = outputFilePrefix;
            Symbols = symbols;
        }

        /// <summary>
        /// Execute wpaexporter to convert ETL file data into CSV files which are defined by the table
        /// names of the passed .wpaProfile file.
        /// </summary>
        /// <param name="requiredTableNames">Array of needed table names which must contain data.</param>
        /// <returns>ExecResult which contains the output of the command and other things.</returns>
        /// <exception cref="InvalidOperationException">If the process could not be started at all.</exception>
        public ExecResult Execute(string[] requiredTableNames)
        {
            if( requiredTableNames == null )
            {
                throw new ArgumentNullException(nameof(requiredTableNames));
            }

            string prefixArg = String.IsNullOrEmpty(OutputFilePrefix) ? "" : $"-prefix {OutputFilePrefix}";
            string symbolsArg = Symbols == null ? "" : "-symbols";


            try
            {
                // wpaexporter has no option to pass it via cmd line probably due to pathes containing spaces and such things
                // This is passed by the environment instead.
                if (Symbols != null)
                {
                    Environment.SetEnvironmentVariable(SymbolPaths.NT_SYMBOLPATH, Symbols.GetCombinedSymbolPath(InputETLFile));
                    Logger.Info($"WPAExport with SymbolServer: {Environment.GetEnvironmentVariable(SymbolPaths.NT_SYMBOLPATH)}");
                }

                var command = new ProcessCommand(WpaExporterExe, $" -profile {WpaProfile} -outputfolder {OutputFolder} -i {InputETLFile} {prefixArg} {symbolsArg}");
                ExecResult res = command.Execute(ProcessPriorityClass.BelowNormal);
                if (res.ReturnCode != 0 ||                                         // return code == 0 means no error from the exporter point of view
                    res.AllOutput.Contains("No data to export was specified") ||   // something was missing 
                    res.AllOutput.Contains("-INPUT FILE OPTIONS-")                 // when help is printed something did not work
                  )
                {

                    bool severeError = true;

                    // some tables might be missing if ETL file was recorded with different options
                    // In that case we need to check if all required tables for the export are present. 
                    // If any of them is missing we fail.
                    if (res.AllOutput.Contains("Error exporting profile"))
                    {
                        severeError = false;
                        var lines = res.AllOutput.Split(Environment.NewLine.ToCharArray()).Where(line => line.Contains(NoDataWarning)).ToArray();
                        foreach (var missing in lines)
                        {
                            if (requiredTableNames.Any(table => missing.Contains(table)))
                            {
                                severeError = true;
                            }
                        }
                    }

                    if (severeError)
                    {
                        res.SetFailed();
                    }
                }
                return res;
            }
            finally
            {
                Environment.SetEnvironmentVariable(SymbolPaths.NT_SYMBOLPATH, null); // clear things up to not disturb other libraries
            }
            
        }
    }
}
