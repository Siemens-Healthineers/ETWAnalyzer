//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Configuration;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.LoadSymbol;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ETWAnalyzer.Commands
{
    internal class LoadSymbolCommand(string[] args) : ArgParser(args)
    {
        internal static readonly string HelpString =
           "ETWAnalyzer -LoadSymbol -filedir/-fd  xxx.json [-SymServer NtSymbolPath, MS, Google or syngo] [-SymFolder xxxx] [-NoOverwrite] [-Indent] [-OutDir xxxx] [-debug]" + Environment.NewLine +
           "     This supports the use case to extract the data on a machine with no symbols and transfer the json files to another machine." + Environment.NewLine + 
           "     The extracted Json files are much smaller than the original ETL files which allows you to mass record ETW data, extract on the recording machines and send the small json files for analysis to HQ." + Environment.NewLine +
         " -SymFolder xxxx      Default is C:\\Symbols. Path to a short directory name in which links are created from the unzipped ETL files to prevent symbol loading issues due to MAX_PATH limitations." + Environment.NewLine +
         " -SymServer [NtSymbolPath, MS, Google, syngo or your own symbol server]  Load pdbs from remote symbol server which is stored in the ETWAnalyzer.dll/exe.config file." + Environment.NewLine +
         "                      With NtSymbolPath the contents of the environment variable _NT_SYMBOL_PATH are used." + Environment.NewLine +
         "                      When using a custom remote symbol server use this form with a local folder: E.g. SRV*C:\\Symbols*https://msdl.microsoft.com/download/symbols" + Environment.NewLine +
        $"                      The config file {ConfigFiles.RequiredPDBs} declares which pdbs" + Environment.NewLine +
         "                      must have been successfully loaded during extraction. Otherwise a warning is printed due to symbol loading errors." + Environment.NewLine +
         " -Indent              Write Json file indented to save space. Default is non indented." + Environment.NewLine +   
         " -NoOverwrite         By default the input json files will be overwritten." + Environment.NewLine +
        $" -OutDir xxxx         When -NoverWrite is used the extracted data will be put into the folder \"{ExtractSerializer.ExtractFolder}\" besides the input file. You can override the output folder with that switch." + Environment.NewLine +
         " -debug               Print a lot diagnostics messages during symbol lookup to console" + Environment.NewLine + 
         "[yellow]Examples[/yellow]" + Environment.NewLine +
        $"[green]Resolve missing symbols from a json file. The input file/s will be overwritten.[/green]" + Environment.NewLine +
         " ETWAnalyzer -extract All -fd xxxx.etl" + Environment.NewLine +
         " ETWAnalyzer -LoadSymbol -fd xxx.json -symServer MS " + Environment.NewLine +
           "" ;


        /// <summary>
        /// ETL file name query
        /// </summary>
        string myFileDirQuery;
        private bool myDebugOutputToConsole;

        /// <summary>
        /// -NoOverwrite flat status
        /// </summary>
        public bool NoOverwrite { get; set; }

        /// <summary>
        /// Input File List
        /// </summary>
        TestDataFile[] myInputJsonFiles;

        public OutDir OutDir { get; private set; } = new OutDir();

        public override string Help => HelpString;

        /// <summary>
        /// Command line switch
        /// </summary>
        public const string NoOverwriteFlag = "-nooverwrite";

        public override void Parse()
        {
            while (myInputArguments.Count > 0)
            {
                string curArg = myInputArguments.Dequeue();
                switch (curArg?.ToLowerInvariant())
                {
                    case CommandFactory.LoadSymbolArg:
                        break;
                    case OutDirArg:
                        string outDir = GetNextNonArg(OutDirArg);
                        OutDir.OutputDirectory = ArgParser.CheckIfFileOrDirectoryExistsAndExtension(outDir);
                        OutDir.IsDefault = false;
                        break;
                    case FileOrDirectoryArg:
                    case FileOrDirectoryAlias:
                        myFileDirQuery = GetNextNonArg(FileOrDirectoryArg);
                        break;
                    case SymbolServerArg: // -symserver
                        Symbols.RemoteSymbolServer = ExtractCommand.ParseSymbolServer(GetNextNonArg(SymbolServerArg));
                        break;
                    case NoOverwriteFlag:
                        NoOverwrite = true;
                        break;
                    case IndentArg:
                        ExtractSerializer.JsonFormatting = Newtonsoft.Json.Formatting.Indented;
                        break;
                    case SymFolderArg: // -symFolder
                        Symbols.SymbolFolder = GetNextNonArg(SymFolderArg);
                        break;
                    case SymCacheFolderArg: // -symcache
                        Symbols.SymCacheFolder = GetNextNonArg(SymCacheFolderArg);
                        break;
                    case DebugArg:    // -debug 
                        myDebugOutputToConsole = true;
                        Program.DebugOutput = true;
                        break;
                    case NoColorArg:
                        ColorConsole.EnableColor = false;
                        break;
                    case HelpArg:
                        throw new InvalidOperationException(HelpArg);
                    default:
                        throw new NotSupportedException($"The argument {curArg} was not recognized as valid argument");
                }
            }

            if (myFileDirQuery == null)
            {
                throw new NotSupportedException($"You need to enter {FileOrDirectoryArg} with an existing input file.");
            }

            TestRunData runData = new(myFileDirQuery, SearchOption.TopDirectoryOnly);
            IReadOnlyList<TestDataFile> filesToAnalyze = runData.AllFiles;
            myInputJsonFiles = filesToAnalyze.Where(file => file.JsonExtractFileWhenPresent != null)
                                            .ToArray();

            if (myInputJsonFiles.Length == 0)
            {
                throw new NotSupportedException($"No input json files found.");
            }
        }

        public override void Run()
        {
            TextWriter dbgOutputWriter = myDebugOutputToConsole ? Console.Out : new StringWriter();
            using Microsoft.Diagnostics.Symbols.SymbolReader reader = new(dbgOutputWriter, Symbols.GetCombinedSymbolPath(""))
            {
                SecurityCheck = (x) => true,
            };

            using SymbolLoader loader = new SymbolLoader(reader);
            

            for(int i=0;i<myInputJsonFiles.Length;i++)
            {
                TestDataFile jsonFile = myInputJsonFiles[i];
                ColorConsole.WriteEmbeddedColorLine($"Processing file {i+1}/{myInputJsonFiles.Length} {jsonFile.JsonExtractFileWhenPresent}");
                loader.LoadSymbols(jsonFile.Extract);
                string outdir = OutDir.OutputDirectory ?? Path.Combine(Path.GetDirectoryName(jsonFile.JsonExtractFileWhenPresent), ExtractSerializer.ExtractFolder);
                Directory.CreateDirectory(outdir);

                string outputFile = jsonFile.JsonExtractFileWhenPresent;

                if (NoOverwrite)
                {
                    outputFile = Path.Combine(outdir, Path.GetFileName(jsonFile.JsonExtractFileWhenPresent));
                }
                ExtractSerializer ser = new ExtractSerializer(outputFile);
                ser.Serialize((ETWExtract) jsonFile.Extract);
            }
        }
    }
}
