﻿//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Configuration;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extractors.Dns;
using ETWAnalyzer.Extractors;
using ETWAnalyzer.Extractors.CPU;
using ETWAnalyzer.Extractors.FileIO;
using ETWAnalyzer.Extractors.Modules;
using ETWAnalyzer.Extractors.PMC;
using ETWAnalyzer.Helper;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ETWAnalyzer.EventDump;
using ETWAnalyzer.Extractors.TCP;
using ETWAnalyzer.Extractors.Memory;
using ETWAnalyzer.Extractors.Power;
using ETWAnalyzer.Extractors.Handle;

namespace ETWAnalyzer.Commands
{
    /// <summary>
    /// Command object for -extract including the command line handling. Constructed by <see cref="CommandFactory"/> if the arguments contain -extract.
    /// </summary>
    class ExtractCommand : ArgParser
    {
        internal const string AllExtractCommands = "All, Default or CPU Disk Dns Exception File Frequency Memory Module ObjectRef PMC Power Stacktag TCP ThreadPool";

        internal static readonly string HelpString =
        $"ETWAnalyzer [-extract [{AllExtractCommands}] -filedir/-fd inEtlOrZip [-DryRun] [-symServer NtSymbolPath/MS/Google/syngo] [-keepTemp] [-NoOverwrite] [-pThreads dd] [-nThreads dd]" + Environment.NewLine +
         "            [-NoReady] [-allCPU] [-Concurrency dd] [-NoIndent] [-NoCompress] [-LastNDays dd] [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-NoTestRunGrouping]  " + Environment.NewLine + 
         "Retrieve data from ETL files and store extracted data in a serialized format in Json in the output directory \\Extract folder." + Environment.NewLine +
         "The data can the be analyzed by other tools or ETWAnalyzer itself which can also analyze the data for specific patterns or issues." + Environment.NewLine +
         "Multiple extractor options are separated by space." + Environment.NewLine +
         " -extract Op1 Op2 ..." + Environment.NewLine +
         "  All       : Include all extractors" + Environment.NewLine +
         "  CPU       : CPU consumption of all proceses part of the recording. CPU Sampling (PROFILE) and/or Context Switch tracing (CSWITCH) data with stacks must be present." + Environment.NewLine +
         "  Default   : Include all extractors except File" + Environment.NewLine +
         "  Disk      : Disk IO summary and a per file summary of read/write/flush disk service times. DISK_IO data must be present in trace to get this data." + Environment.NewLine +
         "  DNS       : Extract DNS Queries. You need to enable ETW provider Microsoft-Windows-DNS-Client." + Environment.NewLine +
         "  Exception : Get all .NET Exception Messages, Type and their call stacks when present with Process,ThreadId and TimeStamp" + Environment.NewLine +
         "              To get call stacks you need symbols. See below -symServer section. The Microsoft-Windows-DotNETRuntime ETW provider with ExceptionKeyword 0x8000 and stacks must be present." + Environment.NewLine +
         "  File      : Open/Close/Read/Write summary of all accessed files per process" + Environment.NewLine +
         "              The ETL file must contain FILEIO data." + Environment.NewLine +
         "  Frequency : Extract CPU Frequency data when present from enabled Microsoft-Windows-Kernel-Processor-Power and Microsoft-Windows-Kernel-Power (capture state data from both providers is also needed) ETW providers." + Environment.NewLine +
         "  Memory    : Get workingset/committed memory machine wide and of all processes at trace start and a second time at trace end. MEMINFO_WS must be present." + Environment.NewLine +
         "  Module    : Dump all loaded modules with file path and version. LOADER data must be present in trace." + Environment.NewLine +
         "  ObjectRef : Extract all Handle (Create/Duplicate/Close) with kernel provider OB_HANDLE, Object (AddRef/ReleaseRef) with kernel provider OB_OBJECT and File map/unmap events with provider VAMAP." + Environment.NewLine +
         "  PMC       : Extract Performance Monitoring Counters and Last Branch Record CPU traces. You need to enable PMC/LBR ETW Tracing during the recording to get data." + Environment.NewLine +
         "  Power     : Extract Power profile data when present from Microsoft-Windows-Kernel-Power provider (capture state is needed to get power profile data)." + Environment.NewLine +
         "  Stacktag  : Get from all processes the CPU call stack summary by the WPA stacktag names" + Environment.NewLine +
         "              To work properly you need symbols. See below -symServer section" + Environment.NewLine +
         "              Json Nodes: SummaryStackTags-UsedStackTagFiles,Stats..." + Environment.NewLine +
         "                           This uses default.stacktags and GCAndJit.stacktags. For each process the GC and JIT overhead is printed extra while the default stacktags contain implicitly GC and JIT also." + Environment.NewLine +
         "              Json Nodes: SpecialStackTags-UsedStackTagFiles,Stats..." + Environment.NewLine +
         "                           There you can configure with the ETWAnalyzer\\Configuration\\Special.stacktags to trend e.g. specific methods over one or more testruns to find regression issues or when an issue did start occurring." + Environment.NewLine +
         "  TCP       : Extract TCP statistic per connection. You need to enable the provider Microsoft-Windows-TCPIP." + Environment.NewLine +
         "  ThreadPool: Extract relevant data from .NET Runtime ThreadPool if available. ThreadingKeyword 0x10000 needs to be set for the Microsoft-Windows-DotNETRuntime ETW Provider during recording." + Environment.NewLine +
         "              Json Nodes: ThreadPool-PerProcessThreadPoolStarvations" + Environment.NewLine +
         "The following filters work only if the input files adhere to a specific file naming convention." + Environment.NewLine + 
         "Select files from a testrun (all tests which have a time gap < 1h) to e.g. select only the first, or skip the warmump run or to extract just a sample of test cases." + Environment.NewLine +
         "         TestCaseName_ddddmsMachineName.yyyymmdd-hhmmss.7z/.zip/.etl  e.g. Build_166375msfv-az192-659.20230127-093520"  + Environment.NewLine + 
         "         TestCaseName_ddddms_Machine_CLT/SRV/SINGLE_TestStatus-Passed/Failed_yyyymmdd-hhmmss.7z/.etl e.g. LoadPrepUseCase_4897ms_RN6498AA8B-B18F_SRV_TestStatus-Passed_20230112-170100.7z" + Environment.NewLine + 
         "   -TestRunIndex dd           Select only data from a specific test run by index. To get the index value use -dump TestRun -filedir xxxx " + Environment.NewLine +
         "   -TestRunCount dd           Select from a given TestRunIndex the next dd TestRuns. " + Environment.NewLine +
         "   -NoTestRunGrouping         Do not group tests into TestRuns which are tests which have tests with a gap > 1h." + Environment.NewLine +
         "   -TestsPerRun dd            Number of test cases to load of each test run. Useful if you want get an overview how a test behaves over time without loading thousands of files." + Environment.NewLine +
         "   -SkipNTests dd             Skip the first n tests of a testcase in a TestRun. Use this to e.g. skip the first test run which shows normally first time init effects which may be not representative" + Environment.NewLine +
         " -DryRun              Do not extract. Only print which files would be extracted." + Environment.NewLine + 
         " -NoOverwrite         By default existing Json files are overwritten during a new extraction run. If you want to extract from a large directory only the missing extraction files you can use this option." + Environment.NewLine +
         "                      This way you can have the same extract command line in a script after a profiling run to extract only the newly added profiling data." + Environment.NewLine +
         " -Indent              By default a not readable non indented json file is written to save space. For readability you can save the json files with extra spaces and line feeds. This increases the file size ca. by 30%." + Environment.NewLine +
        $" -NoCompress          By default the extracted json files are stored in a 7z archive with the extension {TestRun.CompressedExtractExtension}. Use this flag if you need uncompressed json files." + Environment.NewLine +
         " -recursive           Test data is searched recursively below -filedir" + Environment.NewLine +
         " -filedir/-fd  xxx    Can occur multiple times. If a directory is entered all compressed and contained ETL files are extracted. You can also specify a single etl/zip file." + Environment.NewLine +
        @"                      File queries and exclusions are also supported. E.g. -fd C:\Temp\*error*.etl;!*disk* will extract all etl files in c:\temp containing the name error but exclude the ones which contain disk in the file name" + Environment.NewLine +
         " -LastNDays dd        Only extract files which are not older then dd days. Fractional days are supported." + Environment.NewLine + 
         " -pthreads dd         Percentage of threads to use during extract. Default is 75% of all cores  with a cap at 5 parallel extractions. " + Environment.NewLine + 
         "                      This can already utilize 100% of all cores because some operations are multithreaded like symbol transcoding." + Environment.NewLine +
         " -nthreads dd         Number of extraction processes (one per file) used during extraction." + Environment.NewLine +
         " -Concurrency dd      Number of threads used in one extraction process to extract data multithreaded." + Environment.NewLine + 
         " -symServer [NtSymbolPath, MS, Google, syngo or your own symbol server]  Load pdbs from remote symbol server which is stored in the ETWAnalyzer.dll/exe.config file." + Environment.NewLine +
         "                      With NtSymbolPath the contents of the environment variable _NT_SYMBOL_PATH are used." + Environment.NewLine +
         "                      You can use multiple remote symbols servers e.g. https://msdl.microsoft.com/download/symbols;https://chromium-browser-symsrv.commondatastorage.googleapis.com." + Environment.NewLine +
         "                      The symbols are downloaded to C:\\Symbols unless you use -SymCache." + Environment.NewLine + 
        $"                      The config file {ConfigFiles.RequiredPDBs} declares which pdbs must have been successfully loaded during extraction. Otherwise a warning is printed due to symbol loading errors." + Environment.NewLine + 
         " -SymCache folder     Stored downloaded pdbs in a different folder. By default c:\\SymCache is used, regardless of the NtSymbolPath settings." + Environment.NewLine + 
         " -keepTemp            If you want to analyze the ETL files more than once from compressed files you can use this option to keep the uncompressed ETL files in the output folder. " + Environment.NewLine +
         " -allCPU              By default only methods with CPU or Wait > 10 ms are extracted. Used together with -extract CPU." + Environment.NewLine +
         " -NoSampling          Do not process CPU sampling data. Sometimes VMWare VMs produce invalid CPU sampling data. In that case you can ignore it and process only the Context Switch data. " + Environment.NewLine + 
         " -NoCSwitch           Do not process Context Switch data. Useful to reduce memory consumption if you do not need CPU Wait/Ready timings." + Environment.NewLine +
         " -NoReady             By default when Context switch data is present an extra Json file with extended CPU data is created." + Environment.NewLine +
         "                      You will miss with -Dump CPU -Details Ready time percentiles." + Environment.NewLine +
         " -allExceptions       By default exceptions are filtered away by the rules configured in Configuration\\ExceptionFilters.xml. To get all specify this flag." + Environment.NewLine +
         " -timeLine dd         When CPU data is extracted additionally extract CPU timeline data with given sampling interval in seconds. This data is only accessible at API level at IETWExtract.CPU.TimeLine." + Environment.NewLine +
         " -symFolder xxx       Default is C:\\Symbols. Path to a short directory name in which links are created from the unzipped ETL files to prevent symbol loading issues due to MAX_PATH limitations." + Environment.NewLine +
         " -child               Force single threaded in-process extraction" + Environment.NewLine +
         " -recursive           Search below -filedir directory recursively for data to extract." + Environment.NewLine +
        $" -outdir xxxx         By default the extracted data will be put into the folder \"{Program.ExtractFolder}\" besides the input file. You can override the output folder with that switch." + Environment.NewLine +
         " -tempdir xxxx        If input data needs to be unzipped you can enter here an alternate path to e.g. prevent extraction on slow network shares. " + Environment.NewLine + 
        $" -unzipoperation \"cmd\" Execute command after a compressed zip archive was uncompressed. The variables {ETLFileNameVariable} and {ETLFileDirVariable} are expanded. " + Environment.NewLine +
        $"                      You can use ' to escape an exe with spaces. E.g. -unzipoperation \"'C:\\Program Files\\Relogger.exe' {ETLFileNameVariable}\"" + Environment.NewLine + 
         " -nocolor             Do not colorize output on shells with different color schemes. Writing console output is also much faster if it is not colorized." + Environment.NewLine +

         "[yellow]Examples[/yellow]" + Environment.NewLine +
         "[green]Extract data from a single ETL file and use the symbol server from the environment variable _NT_SYMBOL_PATH.[/green]" + Environment.NewLine +
         " ETWAnalyzer -extract Disk CPU Memory Exception Stacktag -filedir xxx.etl -symServer NtSymbolPath " + Environment.NewLine +
         "[green]Extract from zip/7z file and keep extracted ETL files besides the zip/7z file. Use the Microsoft symbol server to resolve stack traces.[/green]" + Environment.NewLine +
         " ETWAnalyzer -extract Disk CPU Memory Exception Stacktag -filedir xxx.7z -symServer MS -keepTemp " + Environment.NewLine +
         "[green]Extract all data except FileIO data from a network share with compressed files. Uncompress files to a local folder to prevent long path issues and to get faster." + Environment.NewLine + 
          "The extracted Json files are written to -outdir xxx. The extracted ETL files are kept in C:\\RawETL.[/green]" + Environment.NewLine +
         " ETWAnalyzer -extract Default -filedir \\\\PerformanceHost\\ProfilingShare\\Baseline_12122022 -outdir c:\\temp\\Profiling\\Baseline_Extracted -keepTemp -tempdir c:\\RawETL" + Environment.NewLine +
         "[green]Update after a profiling run the missing Json extract files but do not overwrite already extracted ETL files again[/green]" + Environment.NewLine +
         " ETWAnalyzer -extract All -filedir C:\\Profiling\\Baseline_12122022 -symServer MS -outdir c:\\temp\\Profiling\\Baseline_Extracted -noOverwrite" + Environment.NewLine +
         "[green]Show which files would be extracted. Select only files which are not older than one day.[/green]" + Environment.NewLine +
         " ETWAnalyzer -extract All -fd C:\\Profiling\\Baseline_12122022 -DryRun -LastNDays 1" + Environment.NewLine +
         "" + Environment.NewLine
         ;

        /// <summary>
        /// Expanded by custom command after a zip file was extracted via -unzipcommand
        /// </summary>
        internal const string ETLFileDirVariable = "#EtlFileDir#";

        /// <summary>
        /// Expanded by custom command after a zip file was extracted via -unzipcommand
        /// </summary>
        internal const string ETLFileNameVariable = "#ETLFileName#";


        // Extract specific command line Arguments

        internal const string TempDirArg = "-tempdir";
        internal const string KeepTempArg = "-keeptemp";
        internal const string NoOverWriteArg = "-nooverwrite";
        internal const string PThreadsArgs = "-pthreads";
        internal const string NThreadsArg = "-nthreads";
        internal const string TimeLineArg = "-timeline";
        internal const string NoReadyArg = "-noready";
        internal const string ChildArg = "-child";  // Marker argument to prevent by accident to spawn child of child processes. Child processes process a trace single threaded
        internal const string AllCPUArg = "-allcpu";
        internal const string ConcurrencyArg = "-concurrency";
        internal const string DryRunArg = "-dryrun";
        internal const string NoSampling = "-nosampling";
        internal const string NoCSwitch = "-nocswitch";
        internal const string NoCompress = "-nocompress";



        public override string Help => HelpString;

        /// <summary>
        /// The -extract xxxx argument can be one of the following lower cased values
        /// </summary>
        public enum ExtractionOptions
        {
            All,
            Default,

            Disk = 10,
            File,
            CPU,
            Memory,
            Exception,
            StackTag,
            ThreadPool,
            Module,
            PMC,
            Dns,
            TCP,
            Frequency,
          //  VirtualAlloc,
            ObjectRef,
            Power,
        }

        /// <summary>
        /// Input/Output arguments from -extract / -analyse command
        /// Can be Disk,CPU,Memory,VirtualAlloc,Exception / NewException,TestCount  where on the command line each one is
        /// separated with a comma
        /// </summary>
        List<string> myProcessingActionList = new();

        /// <summary>
        /// Set via <see cref="NoCompress"/> flag. Default is compressed
        /// </summary>
        bool Compress
        {
            get; set;

        } = true;


        /// <summary>
        /// Contains extraction options that are linked to create new instance of the extractors
        /// </summary>
        readonly Dictionary<ExtractionOptions, Func<ExtractorBase>> myExtractorFactory = new()
        {
            { ExtractionOptions.Disk,        () => new DiskExtractor()         },
            { ExtractionOptions.File,        () => new FileExtractor()         },
            { ExtractionOptions.CPU,         () => new CPUExtractor()          },
            { ExtractionOptions.Memory,      () => new MemoryExtractor()       },
            { ExtractionOptions.Exception,   () => new ExceptionExtractor()    },
            { ExtractionOptions.StackTag,    () => new StackTagExtractor()     },
            { ExtractionOptions.ThreadPool,  () => new ThreadPoolExtractor()   },
            { ExtractionOptions.Module,      () => new ModuleExtractor()       },
            { ExtractionOptions.PMC,         () => new PMCExtractor()          },
            { ExtractionOptions.Dns,         () => new DnsClientExtractor()    },
            { ExtractionOptions.TCP,         () => new TCPExtractor()          },
            { ExtractionOptions.Frequency,   () => new CpuFrequencyExtractor() },
            //    { ExtractionOptions.VirtualAlloc,() => new VirtualAllocExtractor() },
            { ExtractionOptions.ObjectRef,   () => new ObjectRefExtractor() },
            { ExtractionOptions.Power       ,() => new PowerExtractor() },
        };

        /// <summary>
        /// Key is the lower cased enum value of the enum ExtractionOptions.
        /// Value is the corresponding enum name. This map is used to make command line parsing which correspond
        /// to the enum name easy
        /// </summary>
        readonly Dictionary<string, ExtractionOptions> myExtractionEnumStringMap = Enum.GetValues(typeof(ExtractionOptions))
                                                                              .Cast<ExtractionOptions>()
                                                                              .ToDictionary(x => x.ToString().ToLowerInvariant());

        /// <summary>
        /// Deletes temp Files after extracting when -keepTemp is not given (default is true)
        /// </summary>
        bool HaveToDeleteTemp { get; set; } = true;

        /// <summary>
        /// Overridden by -recursive when searching in all subdirectories for test run data
        /// </summary>
        SearchOption mySearchOption = SearchOption.TopDirectoryOnly;

        /// <summary>
        /// Number of successfully extracted files
        /// </summary>
        long mySuccessExtracted;

        /// <summary>
        /// Number of failed extracted files. This is set to 1 in child processes where only one file is processed
        /// The parent process adds the return code which counts the number of failed files so we return from the parent process
        /// the total number of failed files. If all did succeed the main process returns 0;
        /// </summary>
        public static int myFailedExtracted;

        /// <summary>
        /// Number of ETL files which are extracted.
        /// </summary>
        int myTotalFilesToExtract;

        /// <summary>
        /// Number of threads to use in %. Default is 75;
        /// </summary>
        int? myPThreads;

        /// <summary>
        /// Explicitly define the number of threads/processes ETWAnalyzer should use to extract files concurrently
        /// </summary>
        int? myNThreads;

        /// <summary>
        /// Set via -timeLine dd
        /// </summary>
        float? TimelineDataExtractionIntervalS { get; set; }

        /// <summary>
        /// List to store extracting actions 
        /// </summary>
        List<ExtractorBase> Extractors { get; } = new List<ExtractorBase>() { new MachineDetailsExtractor() };

        /// <summary>
        /// -fd/-filedir
        /// </summary>
        public List<string> InputFileOrDirectories { get; private set; } = new List<string>();

        /// <summary>
        /// By default exceptions are filtered during extraction already if in the file Configuration/ExceptionFilters.xml rules are defined.
        /// </summary>
        public bool DisableExceptionFilter { get; private set; }

        /// <summary>
        /// Command which is executed after a ETL zip file was uncompressed to e.g. rewrite it to enrich it with additional marker events or to filter out
        /// specific events
        /// </summary>
        public string AfterUnzipCommand { get; set; }

        /// <summary>
        /// Extract all CPU data which disables the 10ms default threshold during extraction.
        /// </summary>
        public bool ExtractAllCPUData { get; private set; }

        /// <summary>
        /// When present do not extract Ready time percentiles when CPU is extracted and Context Switch data is found in the ETL file.
        /// </summary>
        public bool NoReady { get; private set; }


        /// <summary>
        /// Extract and temp extraction directories
        /// </summary>
        public OutDir OutDir { get; private set; } = new OutDir();

        /// <summary>
        /// When true execute extraction single threaded in every case
        /// </summary>
        public bool IsChildProcess { get; private set; }

        /// <summary>
        /// If the option -nooverwrite is used during extraction the output Json files are not overwritten as this is currently the default
        /// </summary>
        public bool OverwriteJsonExtractFiles { get; set; } = true;


        /// <summary>
        /// Start Extraction at TestRun x
        /// </summary>
        public int TestRunIndex { get; internal set; } = -1;

        /// <summary>
        /// Extract N TestRuns
        /// </summary>
        public int TestRunCount { get; internal set; }

        /// <summary>
        /// Skip N tests of a testrun for extraction
        /// </summary>
        public int SkipNTests { get; internal set; }

        /// <summary>
        /// -TestsPerRun dd Extract N tests per run per test case
        /// </summary>
        public int TestsPerRun { get; internal set; }

        /// <summary>
        /// -LastNDays dd Extract data which was generated during the last N Days
        /// </summary>
        public double LastNDays { get; private set; } = double.MaxValue;

        /// <summary>
        /// -DryRun Do not extract but just print what would be extracted
        /// </summary>
        public bool IsDryRun { get; private set; }

        /// <summary>
        /// -concurrency Defines how many threads are used during CPU and Stacktag Extraction.
        /// </summary>
        public int? Concurrency { get; private set; }

        /// <summary>
        /// -NoSampling Ignore CPU sampling data 
        /// </summary>
        public bool IgnoreCPUSampling { get; private set; }

        /// <summary>
        /// -NoCSwitch Ignore Context Switch data
        /// </summary>
        public bool IgnoreCSwitchData { get; private set; } 


        /// <summary>
        /// Create an extract command with given command line switches
        /// </summary>
        /// <param name="args"></param>
        public ExtractCommand(string[] args):base(args)
        {
        }

        /// <summary>
        /// Parse command line arguments
        /// </summary>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public override void Parse()
        {
            while (myInputArguments.Count > 0)
            {
                string arg = myInputArguments.Dequeue();
                switch (arg?.ToLowerInvariant())
                {
                    case CommandFactory.ExtractCommand:    // -extract xxxx
                        myProcessingActionList = GetArgList(ExtractArg, false);
                        break;
                    case FileOrDirectoryArg:   // -filedir
                    case FileOrDirectoryAlias:  // -fd
                        string fileOrDir = GetNextNonArg(FileOrDirectoryArg);
                        InputFileOrDirectories.Add(fileOrDir); // we support multiple occurrences 
                        break;
                    // All optional Arguments
                    case OutDirArg:   // -outdir
                        string outDir = GetNextNonArg(OutDirArg);
                        OutDir.OutputDirectory = ArgParser.CheckIfFileOrDirectoryExistsAndExtension(outDir);
                        OutDir.IsDefault = false;
                        break;
                    case NoCompress:
                        Compress = false;
                        break;
                    case NoTestRunGrouping:
                        TestRun.MaxTimeBetweenTests = TimeSpan.MaxValue;
                        break;
                    case TempDirArg:  // -tempdir
                        string tmpDir = GetNextNonArg(TempDirArg);
                        if (!Directory.Exists(tmpDir))
                        {
                            throw new DirectoryNotFoundException($"Temp Directory {tmpDir} does not exist.");
                        }
                        OutDir.TempDirectory = tmpDir;
                        break;
                    case PThreadsArgs: // -pthreads
                        string pThreads = GetNextNonArg(PThreadsArgs);
                        pThreads = pThreads.TrimEnd('%');
                        if (!int.TryParse(pThreads, out int tmpPThreads))
                        {
                            throw new InvalidDataException($"-pthreads {pThreads} is not a valid percentage. Valid values are 0-100.");
                        }
                        myPThreads = tmpPThreads;
                        break;
                    case NThreadsArg:   // -nthreads
                        string nThreadsStr = GetNextNonArg(NThreadsArg);
                        if( !int.TryParse(nThreadsStr, out int tmpNThreads))
                        {
                            throw new InvalidDataException($"{NThreadsArg} {nThreadsStr} is not a valid number for the thread count.");
                        }
                        myNThreads = tmpNThreads;
                        break;
                    case ConcurrencyArg: // -concurrency
                        string nConcurrencyStr = GetNextNonArg(ConcurrencyArg);
                        if (!int.TryParse(nConcurrencyStr, out int nConcurrency))
                        {
                            throw new InvalidDataException($"{NThreadsArg} {nConcurrencyStr} is not a valid number for the concurrency count.");
                        }
                        Concurrency = nConcurrency;
                        break;
                    case TimeLineArg:   // -timeline
                        string timelineInterval = GetNextNonArg(TimeLineArg);
                        if( !float.TryParse(timelineInterval, out float tmpTimeLine))
                        {
                            throw new InvalidDataException($"{TimeLineArg} {timelineInterval} is not a valid number for the timeline sampling interval.");
                        }
                        TimelineDataExtractionIntervalS = tmpTimeLine;
                        break;
                    case UnzipOperationArg:   // -unzipoperation
                        AfterUnzipCommand =  GetNextNonArg(UnzipOperationArg);
                        break;
                    case ChildArg: // -child
                        IsChildProcess = true;
                        break;
                    case NoSampling:
                        IgnoreCPUSampling = true;
                        break;
                    case NoCSwitch:
                        IgnoreCSwitchData = true;
                        break;
                    case AllCPUArg:   // -allcpu
                        ExtractAllCPUData = true;
                        break;
                    case NoReadyArg: // -noready
                        NoReady = true;
                        break;
                    case AllExceptionsArgs:  // -allexceptions
                        DisableExceptionFilter = true;
                        break;
                    case RecursiveArg:    // -recursive
                        mySearchOption = SearchOption.AllDirectories;
                        break;
                    case NoOverWriteArg: // -noOverwrite
                        OverwriteJsonExtractFiles = false;
                        break;
                    case SymbolServerArg: // -symserver
                        Symbols.RemoteSymbolServer = ParseSymbolServer(GetNextNonArg(SymbolServerArg));
                        break;
                    case KeepTempArg: // -keepTemp
                        HaveToDeleteTemp = false;
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
                    case NoColorArg:
                        ColorConsole.EnableColor = false;
                        break;
                    case DebugArg:    // -debug
                        Program.DebugOutput = true;
                        break;
                    case SkipNTestsArg:  // -skipntests
                        string skipNTests = GetNextNonArg(SkipNTestsArg);
                        SkipNTests = int.Parse(skipNTests, CultureInfo.InvariantCulture);
                        break;
                    case TestRunIndexArg:  // -testrunindex
                    case TRIArg:
                        string testRun = GetNextNonArg(TestRunIndexArg);
                        TestRunIndex = int.Parse(testRun, CultureInfo.InvariantCulture);
                        break;
                    case TestRunCountArg:  // -testruncount
                    case TRCArg:
                        string testrunCount = GetNextNonArg(TestRunCountArg);
                        TestRunCount = int.Parse(testrunCount, CultureInfo.InvariantCulture);
                        break;
                    case TestsPerRunArg:  // -testsperrun
                        TestsPerRun = int.Parse(GetNextNonArg(TestsPerRunArg), CultureInfo.InvariantCulture);
                        break;
                    case LastNDaysArg:   // -lastndays
                        string lastNDays = GetNextNonArg(LastNDaysArg);
                        LastNDays = ParseDouble(lastNDays);
                        break;
                    case DryRunArg:      // -dryrun
                        IsDryRun = true;
                        break;
                    case HelpArg:
                        throw new InvalidOperationException(HelpArg);
                    default: throw new NotSupportedException($"Invalid Argument: {arg}");
                }
            }

            
            // If Output directory is not set, set it to Input folder. The extract file will then go into InputFolder/Extract
            // If explicitly set we will not append Extract to the folder
            if (OutDir.OutputDirectory == null && InputFileOrDirectories.Count == 1)
            {
                OutDir.SetDefault( File.Exists(InputFileOrDirectories[0]) ? Path.GetDirectoryName(Path.GetFullPath(InputFileOrDirectories[0])) : InputFileOrDirectories[0]);
            }

            ConfigureExtractors(Extractors, myProcessingActionList);
            SetExtractorFilters(Extractors, ExtractAllCPUData, DisableExceptionFilter, TimelineDataExtractionIntervalS, Concurrency, NoReady, IgnoreCPUSampling, IgnoreCSwitchData);

        }

        /// <summary>
        /// Configure extractors
        /// </summary>
        public void ConfigureExtractors(List<ExtractorBase> extractors, List<string> stringExtractors)
        {
            string processingAction = null;
            bool wrongCommand = true;
            foreach (var iterateActions in stringExtractors)
            {
                wrongCommand = true;
                processingAction = iterateActions.ToLowerInvariant();

                if (myExtractionEnumStringMap.TryGetValue(processingAction, out ExtractionOptions enumActionExtract))
                {
                    wrongCommand = false;
                    if (enumActionExtract == ExtractionOptions.All)
                    {
                        extractors.AddRange(myExtractorFactory.Values.Select(x => x()));
                    }
                    else if (enumActionExtract == ExtractionOptions.Default) // all except File IO so far
                    {
                        extractors.Add(myExtractorFactory[ExtractionOptions.Frequency]());
                        extractors.Add(myExtractorFactory[ExtractionOptions.Disk]());
                        extractors.Add(myExtractorFactory[ExtractionOptions.CPU]());
                        extractors.Add(myExtractorFactory[ExtractionOptions.Memory]());
                        extractors.Add(myExtractorFactory[ExtractionOptions.Exception]());
                        extractors.Add(myExtractorFactory[ExtractionOptions.StackTag]());
                        extractors.Add(myExtractorFactory[ExtractionOptions.ThreadPool]());
                        extractors.Add(myExtractorFactory[ExtractionOptions.Module]());
                        extractors.Add(myExtractorFactory[ExtractionOptions.PMC]());
                        extractors.Add(myExtractorFactory[ExtractionOptions.Dns]());
                        extractors.Add(myExtractorFactory[ExtractionOptions.TCP]());
                        extractors.Add(myExtractorFactory[ExtractionOptions.Power]());
                        extractors.Add(myExtractorFactory[ExtractionOptions.ObjectRef]());
                 //       extractors.Add(myExtractorFactory[ExtractionOptions.VirtualAlloc]());
                    }
                    else
                    {
                        extractors.Add(myExtractorFactory[enumActionExtract]());

                        if (enumActionExtract == ExtractionOptions.CPU) // with failed pdb lookup support we also need module information to be able to later resolve missing symbols
                        {
                            extractors.Add(myExtractorFactory[ExtractionOptions.Module]());
                        }
                    }
                }

                if (wrongCommand)
                {
                    throw new ArgumentException($"Wrong action command: {processingAction}");
                }
            }

            SortDependantExtractors(extractors);

        }

        private static void SortDependantExtractors(List<ExtractorBase> extractors)
        {
            // CPUFrequency must first extract its data before CPU extractor can calcualte average CPU frequencies for methods
            for (int i = 0; i < extractors.Count; i++)
            {
                ExtractorBase extractor = extractors[i];
                if (extractor is CpuFrequencyExtractor)
                {
                    var tmp = extractors[1]; // 0 is MachineDetailsExtractor which needs to be first which sets also CPU topology information which is needed for CPUFrequency extractor
                    extractors[1] = extractor;
                    extractors[i] = tmp;
                }
            }
        }

        static void SetExtractorFilters(List<ExtractorBase> extractors, bool extractAllCpuData, bool disableExceptionFilter, float? timelineExtractionInterval, int ?concurrency, bool noReady, bool noSampling, bool noCSwitch)
        {
            var cpu = extractors.OfType<CPUExtractor>().SingleOrDefault();
            if (cpu != null)
            {
                cpu.ExtractAllCPUData = extractAllCpuData;
                cpu.Concurrency = concurrency;
                cpu.NoReady = noReady;
                cpu.NoSampling = noSampling;
                cpu.NoCSwitch = noCSwitch;
                cpu.TimelineDataExtractionIntervalS = timelineExtractionInterval;
            }

            var stacktag = extractors.OfType<StackTagExtractor>().SingleOrDefault();
            if( stacktag != null)
            {
                stacktag.Concurrency = concurrency;
                stacktag.NoSampling = noSampling;
                stacktag.NoCSwitch = noCSwitch;
            }

            var exception = extractors.OfType<ExceptionExtractor>().SingleOrDefault();
            if (exception != null)
            {
                exception.DisableExceptionFilter = disableExceptionFilter;
            }
        }

        /// <summary>
        /// Check if passed command line argument is one of the recognized enums
        /// </summary>
        /// <param name="symbolServer">command line argument passed to -symserver</param>
        /// <returns>Saved value from App.Config which are part of the executable App.Config via the settings mechanism.</returns>
        /// <remarks>The App.config mechanism is (a bit) explained at https://stackoverflow.com/questions/13043530/what-is-app-config-in-c-net-how-to-use-it/13043569</remarks>
        static internal string ParseSymbolServer(string symbolServer)
        {
            string symbolServerName = "";
            ParseEnum<SymbolServers>("SymServer", symbolServer, () =>
            {
                Enum.TryParse<SymbolServers>(symbolServer, true, out SymbolServers symServerEnum);
                symbolServerName = symServerEnum switch
                {
                    SymbolServers.MS => Settings.Default.SymbolServerMS,
                    SymbolServers.Syngo => Settings.Default.SymbolServerSyngo,
                    SymbolServers.Google => Settings.Default.SymbolServerGoogle,
                    SymbolServers.NtSymbolPath => SymbolPaths.GetRemoteSymbolServerFromNTSymbolPath(),
                    _ => symbolServer,
                };
            }, SymbolServers.None);

            Logger.Instance.Write($"Using configured remote symbol server {symbolServer}: {symbolServerName}");

            return symbolServerName;
        }

        public override void Run()
        {
            TestRunData runData = new(InputFileOrDirectories, mySearchOption, OutDir.OutputDirectory);
            SingleTest[] tests = DumpFileDirBase<ExtractCommand>.GetTestRunsFromTestRunData(runData, false, true, TestRunIndex, TestRunCount, SkipNTests, TestsPerRun, LastNDays).ToArray();

            IReadOnlyList<TestDataFile> filesToAnalyze = tests.SelectMany(x=>x.Files).ToList();
            TestDataFile[] nonEmptyFiles = filesToAnalyze.Where(file => file.SizeInMB != 0).ToArray();
            myTotalFilesToExtract = nonEmptyFiles.Length;

            if (!IsChildProcess)
            {
                Console.WriteLine($"{myTotalFilesToExtract} - files found to extract.");
                if( IsDryRun )
                {
                    Console.WriteLine("The following files would be extracted:");
                    foreach(var file in nonEmptyFiles)
                    {
                        Console.WriteLine($"{file.FileName}");
                    }
                    return;
                }
            }

            var sw = Stopwatch.StartNew();

            int maxThreads = CalcMaxThreadCount();

            Assembly entry = Assembly.GetEntryAssembly();
            bool useInProcess = true;
            if (entry?.GetName()?.Name == "ETWAnalyzer" && !IsChildProcess)  // started via exe and not a testhost process
            {
                useInProcess = false;
            }
            else
            {
                // We cannot run parallel extraction inside one process because  TraceProcessor does not support multithreaded loading
                //System.Runtime.InteropServices.InvalidComObjectException: COM object that has been separated from its underlying RCW cannot be used. The COM object was released while it was still in use on another thread.
                //  InterfaceMarshaler.ConvertToManaged(IntPtr pUnk, IntPtr itfMT, IntPtr classMT, Int32 flags)
                //  AddInManagerAdapter.Create(String toolkitPath)
                //  ToolkitTraceProcessingEngine.Create(String path, ITraceProcessorSettings settings, ITraceProcessorSettings & effectiveSettings)
                //  TraceProcessor.Create(String path, ITraceProcessorSettings settings)
                maxThreads = 1;
            }

            Action<TestDataFile> parallelAction = useInProcess ? new Action<TestDataFile>(ExtractSingleFileInProcess) : new Action<TestDataFile>(ExtractSingleFileOutOfProcess);

            int skipCount = 0;
            // Until symbol transcoding bug of MS is fixed we take for multiple etl files the first file single threaded to 
            // speed up symbol transcoding which already runs multithreaded. If we would use multithreading for the first etl 
            // with multiple instances it would hang for hours due to interprocess locking which is not efficient at all.
            if (nonEmptyFiles.Length > 5 && OverwriteJsonExtractFiles == false && useInProcess == false)
            {
                Logger.Info("Executing first file single threaded to speed up symbol transcoding");
                parallelAction(nonEmptyFiles.First());
                skipCount = 1;
            }

            var queue = new Queue<TestDataFile>(nonEmptyFiles.Skip(skipCount));

            // Paralle.Foreach does not evenly distribute long running task leading to a long tail 
            // of single threaded extractions
            Parallel.For(0, maxThreads, i =>
            {
                do
                {
                    TestDataFile file = null;
                    lock (queue)
                    {
                        if (queue.Count > 0)
                        {
                            file = queue.Dequeue();
                        }
                    }

                    if (file != null)
                    {
                        parallelAction(file);
                    }
                } while (queue.Count > 0);
            });

            sw.Stop();

            if (!IsChildProcess)
            {
                string durationStr = sw.Elapsed.ToString("dd\\ hh\\:mm\\:ss", CultureInfo.InvariantCulture);
                string str = $"Extracted: {mySuccessExtracted} files in {durationStr}, Failed Files {myFailedExtracted}";
                myReturnCode = myFailedExtracted;  // set return code as defined by contract on main method. 0 is successs. > 0 is the number of failed files.
                Logger.Info(str);
                Console.WriteLine(str);
            }
        }

        int CalcMaxThreadCount()
        {
            bool defaultThreading = myNThreads == null && myPThreads == null;

            int maxThreads = myNThreads ?? (int)(Environment.ProcessorCount * ( (myPThreads ?? 75) / 100.0f)); // use at most myPThreads % of all cores

            if (defaultThreading) // Only cap at 5 parallel extractors when default threading policy (-pthreads was not set) is active
            {
                maxThreads = Math.Max(1, Math.Min(5, maxThreads)); // Cap threads at 5 but use at least 1
            }
            else
            {
                maxThreads = Math.Max(1, maxThreads); // use at least 1 extractor
            }

            Logger.Info($"Used thread count is {maxThreads}.");

            return maxThreads;
        }
        void ExtractSingleFileOutOfProcess(TestDataFile fileToAnalyze)
        {
            long current = 0;
            if (fileToAnalyze.JsonExtractFileWhenPresent != null && !OverwriteJsonExtractFiles)
            {
                current = Interlocked.Increment(ref mySuccessExtracted);
                Console.WriteLine($"Skipping file {fileToAnalyze.FileName} {current}/{myTotalFilesToExtract} because extract already exists");
                Logger.Info($"Extract found at {fileToAnalyze.JsonExtractFileWhenPresent}");
                return;
            }

            string subCommand = GetCommandLineForSingleExtractFile(fileToAnalyze.FileName);

            // when only one file is extracted we by default use up to 75% of all cores to speed up CPU and Stacktag extraction
            if(myTotalFilesToExtract == 1 && Concurrency == null)
            {
                subCommand += $" {ConcurrencyArg} {CalcMaxThreadCount()}";
            }

            var command = new ProcessCommand(ConfigFiles.ETWAnalyzerExe, subCommand);
            try
            {
                ExecResult res = command.Execute(ProcessPriorityClass.BelowNormal);
                Console.WriteLine(res.AllOutput.TrimEnd(Environment.NewLine.ToCharArray()));
                Logger.Info($"Pid: {res.ExitedProcess.Id} exited with {res.ReturnCode}, Hex: 0x{(uint)res.ReturnCode:X}");
                // Exception is already caught in command.Execute Method, so it is necessary to check if file really exists - Program reaches this point
                fileToAnalyze.JsonExtractFileWhenPresent = null; // force to reevaluate search 
                if (fileToAnalyze.JsonExtractFileWhenPresent != null && res.ReturnCode == 0)
                {
                    current = Interlocked.Increment(ref mySuccessExtracted);
                }
                else
                {
                    current = Interlocked.Read(ref mySuccessExtracted); // update counter otherwise we see 0 below
                    Interlocked.Add(ref myFailedExtracted, res.ReturnCode <= 0 ? 1 : 0);  // when it crashes a negative return code is used. Treat it as failed as well. 
                    Console.WriteLine($"Cannot extract {fileToAnalyze.FileName}, Return Code: {res.ReturnCode}");
                }

                Console.WriteLine($"Extracted {current}/{myTotalFilesToExtract} - Failed {myFailedExtracted} files.");
            }
            catch (InvalidZipContentsException ex)
            {
                Console.WriteLine($"Warning: Zip file {ex.ZipFile} did not contain an ETL file with the same file name as the zip file. Skipping this file.");
            }
        }


        /// <summary>
        /// This method is called in a parallel loop 
        /// </summary>
        /// <param name="fileToAnalyze"></param>
        void ExtractSingleFileInProcess(TestDataFile fileToAnalyze)
        {
            long current = 0;
            if (fileToAnalyze.JsonExtractFileWhenPresent != null && !OverwriteJsonExtractFiles)
            {
                current = Interlocked.Increment(ref mySuccessExtracted);
                Console.WriteLine($"Skipping file {fileToAnalyze.FileName} {current}/{myTotalFilesToExtract} because extract already exists");
                Logger.Info($"Extract found at {fileToAnalyze.JsonExtractFileWhenPresent}");
                return;
            }

            IReadOnlyList<string> outFiles = ExtractFile(Extractors, fileToAnalyze.EtlFileNameIfPresent ?? fileToAnalyze.FileName, OutDir, Symbols, HaveToDeleteTemp, AfterUnzipCommand, Compress);

            if( outFiles == null || outFiles.Count == 0 )
            {
                string msg = "outFiles was null or count was 0!";
                Logger.Error(msg);
                ColorConsole.WriteError("Error: " + msg);
                return;
            }

            if (File.Exists(outFiles[0]))
            {
                current = Interlocked.Increment(ref mySuccessExtracted);
            }
            else
            {
                Interlocked.Increment(ref myFailedExtracted);
                Console.WriteLine($"Cannot extract {outFiles[0]}");
            }

            if (IsChildProcess)
            {
                string status = myFailedExtracted > 0 ? $"Failed Extraction of {fileToAnalyze.EtlFileNameIfPresent}" : $"Success Extraction of {outFiles[0]}";
                Console.WriteLine(status);
            }
            else
            {
                Console.WriteLine($"Extracted {current}/{myTotalFilesToExtract} files. Current: {outFiles[0]}.");
            }
        }
        /// <summary>
        /// Get a command line to extract a single etl file. From the original command line we replace the -filedir argument and re-escape the output directory with ""
        /// </summary>
        /// <param name="fileToExtract">Single ETL to extract</param>
        /// <returns>Command line for ETWAnalyzer to extract a single file</returns>
        internal string GetCommandLineForSingleExtractFile(string fileToExtract)
        {
            StringBuilder sb = new();
            bool fileDirSeen = false;
            for (int i = 0; i < myOriginalInputArguments.Length; i++)
            {
                if (myOriginalInputArguments[i].ToLower(CultureInfo.InvariantCulture) == FileOrDirectoryArg || (myOriginalInputArguments[i].ToLower(CultureInfo.InvariantCulture) == FileOrDirectoryAlias) )
                {
                    if (!fileDirSeen)
                    {
                        sb.Append(myOriginalInputArguments[i]);
                        sb.Append(' ');

                        i++;
                        sb.Append($"\"{fileToExtract}\" ");
                        fileDirSeen = true;
                    }
                    else
                    {
                        i++; // ignore secondary -fd/fildir file argument
                    }
                }
                else
                {
                    sb.Append(myOriginalInputArguments[i]);
                    sb.Append(' ');
                }

                if( myOriginalInputArguments[i].ToLowerInvariant() == UnzipOperationArg)
                {
                    i++;
                    sb.Append($"\"{AfterUnzipCommand}\" ");
                }
                else if (myOriginalInputArguments[i].ToLower(CultureInfo.InvariantCulture) == OutDirArg)
                {
                    i++;
                    // we need to escape directories again for child processes because in Main(string[] args) we get escaped strings back without quotes!
                    sb.Append($"\"{myOriginalInputArguments[i]}\" ");
                }
            }

            sb.Append(" " + ChildArg);

            return sb.ToString();
        }
        /// <summary>
        /// Extract data from a single ETL or compressed etl file.
        /// </summary>
        /// <param name="extractors">List of extraction operations</param>
        /// <param name="inputETLFileOrZip">etl file or compressed etl file</param>
        /// <param name="outputDirectory">Directory where the extracted data is and temporary files are created</param>
        /// <param name="symbols">Gives access to local and remote symbol folder and servers.</param>
        /// <param name="haveToDeleteTemp">True: Deletes all temp files</param>
        /// <param name="afterUnzipCommand">Command line of exe which is executed after an ETL was extracted.</param>
        /// <param name="bCompress">if true output files are written to a compressed file with extension <see cref="TestRun.CompressedExtractExtension"/></param>
        /// <returns>Serialized Json file name</returns>
        static IReadOnlyList<string> ExtractFile(List<ExtractorBase> extractors, string inputETLFileOrZip, OutDir outputDirectory, SymbolPaths symbols, bool haveToDeleteTemp, string afterUnzipCommand, bool bCompress)
        {
            var singleFile = new ExtractSingleFile(inputETLFileOrZip, extractors, outputDirectory.TempDirectory ?? Path.GetDirectoryName(inputETLFileOrZip), symbols, afterUnzipCommand, bCompress); // unzip
            return singleFile.Execute(outputDirectory, haveToDeleteTemp);
        }

    }
}
