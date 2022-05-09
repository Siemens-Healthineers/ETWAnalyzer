//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.EventDump;
using ETWAnalyzer.Extract;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static ETWAnalyzer.Extract.ETWProcess;
using ETWAnalyzer.TraceProcessorHelpers;

namespace ETWAnalyzer.Commands
{
    /// <summary>
    /// Processes all -dump xxxx commands. Constructed by <see cref="CommandFactory"/> if the arguments contain -dump.
    /// </summary>
    class DumpCommand : ArgParser
    {
        private static readonly string DumpHelpStringPrefix =
        "ETWAnalyzer -Dump [Stats,Process,CPU,Memory,Disk,File,ThreadPool,Exception,Mark,TestRun,Version] [-nocolor]" + Environment.NewLine;

        static readonly string StatsHelpString =
        "   Stats    -filedir/fd x.etl/.json   [-Properties xxxx] [-recursive] [-csv xxx.csv] [-NoCSVSeparator] [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-TestCase xx] [-Machine xxxx] [-Clip]" + Environment.NewLine + "" +
        "                         ETL Only:                  Dump from an ETL file or compressed 7z file which will be uncompressed in-place ETW statistics." + Environment.NewLine +
        "                                                    This includes OS version, bitness, trace start/end and a list of all contained events and their counts and sizes of the ETL file." + Environment.NewLine +
        "                         Json Only:                 When Json files are dumped some or all extracted data is printed or exported to a CSV file. You can also filter by testcase, machine, ... to extract data of specific files" + Environment.NewLine +
        "                         -Properties xxxx           Dump only specific properties of extracted Json to console. Valid property names are " + Environment.NewLine + 
       $"                                                    {DumpStats.AllProperties}" + Environment.NewLine +
        "                         -OneLine                   Print properties on console on a single line per file" + Environment.NewLine +
            Environment.NewLine;

        static readonly string VersionHelpString =
        "   Version  -filedir/fd x.etl/.json [-dll xxxx.dll] [-VersionFilter xxx] [-ModuleFilter xxx] [-ProcessName/pn xxx.exe(pid)] [-NoCmdLine] [-csv xx.csv]" + Environment.NewLine +
        "                           [-Clip] [-PlainProcessNames] [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-TestCase xx] [-Machine xxxx]" + Environment.NewLine +
        "                         Dump module versions of given ETL or Json. For Json files the option '-extract Module' must be used during extraction to get with -dll version information." + Environment.NewLine +
        "                         -dll xxx.dll              All file versions of that dll are printed. If -dll * is used all file versions are printed." + Environment.NewLine +
        "                         -VersionFilter filter     Filter against module path and version strings. Multiple filters are separated by ;. Wildcards are * and ?. Exclusion filters start with !" + Environment.NewLine +
        "                         -ModuleFilter  filter     Print only version information for module. Multiple filters are separated by ;. Wildcards are * and ?. Exclusion filters start with !" + Environment.NewLine;
        static readonly string ProcessHelpString =
        "   Process  -filedir/fd x.etl/.json [-recursive] [-csv xxx.csv] [-NoCSVSeparator] [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-ProcessName/pn xxx.exe(pid)] [-CmdLine *xxx*] [-Crash] " + Environment.NewLine +
        "            [-ZeroTime/zt Marker/First/Last/ProcessStart filter] [-ZeroProcessName/zpn filter]" + Environment.NewLine + 
        "            [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-MinMax xx-yy] [-ShowFileOnLine] [-ShowAllProcesses] [-NoCmdLine] [-Clip] [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-TestCase xx] [-Machine xxxx]" + Environment.NewLine +
        "                         Print process name, pid, command line, start/stop time return code and parent process id" + Environment.NewLine +
        "                         Default: The processes are grouped by exe sorted by name and then sorted by time to allow easy checking of recurring process starts." + Environment.NewLine +
        "                         -csv xx.csv                Write output to a CSV file with ; as separator for later processing." + Environment.NewLine +
        "                                                    Dates are formatted as yyyy-MM-dd HH:mm:ss.fff For Excel use yyyy-mm-dd hh:mm:ss.000 as custom date time format string to parse it back." + Environment.NewLine +
        "                                                    On machines where the . is not the decimal point change the locale setting (Control Panel - Region - Additional Settings - Numbers - Decimal Symbol) to ." + Environment.NewLine +
        "                         -NoCSVSeparator            Skip the first line with sep=; which is there to aid Excel to detect the CSV separator character." + Environment.NewLine +
        "                         -ZeroTime/zt               Shift first/last method time. This also affects -csv output. Useful to see method timings relative to the first occurrence of e.g. method OnClick." + Environment.NewLine +
        "                             Marker filter          Zero is a ETW marker event defined by filter." + Environment.NewLine +
        "                             First  filter          Select the first occurrence of a method/stacktag as zero time point. If the filter is ambiguous consider to refine the filter or add -ZeroProcessName to limit it to a specific process." + Environment.NewLine +
        "                             Last   filter          Select the last occurrence of a method/stacktag as zero time point." + Environment.NewLine +
        "                             ProcessStart/ProcessEnd [CmdLine] Select process start/stop event as zero point which matches the optional CmdLine filter string and the -ZeroProcessName filter." + Environment.NewLine +
        "                         -ZeroProcessName/zpn x.exe Select the process from which the zero time point will be used for ProcessStart/First/Last Method zero point definition." + Environment.NewLine +
        "                         -TimeFmt                   Default is Local." + Environment.NewLine +
        "                            s or second             Print as time in seconds since trace start. This is the time WPA is showing in the UI." + Environment.NewLine +
        "                            Local                   Print time as local time on which the data was recorded. This is usually the time customers report when something did fail." + Environment.NewLine +
        "                            LocalTime               Same as Local but without date string." + Environment.NewLine +
        "                            UTC                     Print time in UTC (Universal Coordinated Time)." + Environment.NewLine +
        "                            UTCTime                 Same as UTC but without date string." + Environment.NewLine +
        "                            Here                    Print time as local time in the current system time zone." + Environment.NewLine +
        "                            HereTime                Same as Here but without date string." + Environment.NewLine + 
        "                         -ProcessName/pn x;y.exe    Filter by process name or process id. Exclusion filters start with !, Multiple filters are separated by ;" + Environment.NewLine +
        "                                                    E.g. cmd;!1234 will filter for all cmd.exe instances excluding cmd.exe(1234). The wildcards * and ? are supported for all filter strings." + Environment.NewLine + 
        "                         -CmdLine substring         Restrict output to processes with a matching command line substring." + Environment.NewLine +
        "                         -NewProcess 0/1/-1/-2/2    If not present all processes are dumped. " + Environment.NewLine + 
        "                                                    0 All processes which have been running from trace start-end. " + Environment.NewLine +
        "                                                    1 Processes which have been started and potentially exited during the trace." + Environment.NewLine +
        "                                                   -1 Processes which have exited during the trace but have been potentially also started." + Environment.NewLine +
        "                                                    2 Processes which have been started but not stopped during the trace. " + Environment.NewLine + 
        "                                                   -2 Processes which are stopped but not started during the trace." + Environment.NewLine +
        "                         -SortBy[Time / Default]    Sort processes by start time or group by process and then sort by start time (default)." + Environment.NewLine +
        "                         -PlainProcessNames         Default is to use pretty process names based on rename rules in Configuration\\ProcessRenameRules.xml. If you do not want this use this flag." + Environment.NewLine +
        "                         -NoCmdLine                 Omit process command line string in output. Default is to print the full exe with command line." + Environment.NewLine +
        "                         -Clip                      Clip printed output to console buffer width to prevent wraparound to keep output readable" + Environment.NewLine + 
        "                         The following commands are specific only to dump Process" + Environment.NewLine +
        "                         -Merge                     Merge all selected Json files to calculate process lifetime across all passed Json files. This also limits the display to only started/ended processes per file." + Environment.NewLine +
        "                         -ShowAllProcesses          When -Merge is used already running processes are only printed once. If you want to know if they were still running use this flag." + Environment.NewLine +
        "                         -MinMaxDuration minS [maxS] Filter for process duration in seconds." + Environment.NewLine +
        "                         -ShowFileOnLine            Show etl file name on each printed line." + Environment.NewLine +
        "                         -Crash                     Show potentially crashed processes with unusual return codes, or did trigger Windows Error Reporting." + Environment.NewLine +
        "                         For other options [-TestsPerRun] [-SkipNTests] [-TestRunIndex] [-TestRunCount] [-TestCase] [-Machine]" + Environment.NewLine +
        "                         refer to help of TestRun. Run \'EtwAnalyzer -help dump\' to get more infos." + Environment.NewLine;
        static readonly string TestRunHelpString =
        "   TestRun  -filedir/fd xxx [-recursive] [-verbose] [-ValidTestsOnly] [[-CopyFilesTo xxx] [-WithEtl] [-OverWrite]] [-TestRunIndex dd -TestRunCount dd] [TestCase xxx] [-PrintFiles] [-Clip]" + Environment.NewLine +
        "                         Print for a directory which contains automated profiling data test execution counts. You can also download data to a local directory once you know which" + Environment.NewLine +
        "                         data you need by selecting a testrun by index (-TestRunIndex) and count (-TestRunCount default is all until end), .. -TestCase ..." + Environment.NewLine +
        "                         -recursive                 Search below all subdirectories for test runs" + Environment.NewLine +
       @"                         -filedir/fd xxx            Can occur multiple times. xxx is an extracted json file name, directory, or a file query like C:\temp\*test*.json;!*success* which matches all files with test in C:\temp excluding success files" + Environment.NewLine + 
       @"                                                    You can query multiple directories. E.g. -fd c:\temp\1 -fd c:\temp\2"+Environment.NewLine + 
        "                         The following filters are only applicable to profiling data which has a fixed file naming convention" + Environment.NewLine +
        "                            -TestRunIndex dd           Select only data from a specific test run by index. To get the index value use -dump TestRun -filedir xxxx " + Environment.NewLine +
        "                            -TestRunCount dd           Select from a given TestRunIndex the next dd TestRuns. " + Environment.NewLine +
        "                            -TestsPerRun dd            Number of test cases to load of each test run. Useful if you want get an overview how a test behaves over time without loading thousands of files." + Environment.NewLine +
        "                            -SkipNTests dd             Skip the first n tests of a testcase in a TestRun. Use this to e.g. skip the first test run which shows normally first time init effects which may be not representative" + Environment.NewLine +
        "                            -TestCase xxx              When a directory is selected restrict output to a single test case" + Environment.NewLine +
        "                            -Machine xxxx              Filter for runs which include this machine." + Environment.NewLine +
        "                            -CopyFilesTo xxx           Copy matching files from e.g. a test run selected by " + Environment.NewLine +
        "                            -WithEtl                   Copy also the ETL/Zip file if present" + Environment.NewLine +
        "                            -Overwrite                 Force overwrite of downloaded data." + Environment.NewLine +
        "                         -ValidTestsOnly            Only consider files which match the automated test file naming convention which include test name, duration, machine in the file name" + Environment.NewLine +
        "                         -verbose                   Print Test Duration as x" + Environment.NewLine +
        "                         -PrintFiles                Print input Json files paths into output" + Environment.NewLine;
        static readonly string CPUHelpString =
        "   CPU      -filedir/fd Extract\\ or xxx.json [-recursive] [-csv xxx.csv] [-NoCSVSeparator] [-ProcessFmt xx] [-Methods method1;method2...] [-FirstLastDuration/fld [firsttimefmt] [lasttimefmt]]" + Environment.NewLine + 
        "            [-ThreadCount] [-SortBy [CPU/Wait/StackDepth/First/Last] [-StackTags tag1;tag2] [-CutMethod xx-yy] [-ShowOnMethod] [-ShowModuleInfo [Driver]] [-NoCmdLine] [-Clip]" + Environment.NewLine +
        "            [-ShowTotal Total, Process, Method] [-topn dd nn] [-topNMethods dd nn] [-ZeroTime/zt Marker/First/Last/ProcessStart filter] [-ZeroProcessName/zpn filter] " + Environment.NewLine + 
        "            [-includeDll] [-includeArgs] [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-TestCase xx] [-Machine xxxx] [-ProcessName/pn xxx.exe(pid)] [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                         Print CPU and Wait duration of selected methods of one extracted Json or a directory of Json files. To get output -extract CPU, All or Default must have been used during extraction." + Environment.NewLine +
        "                         The numbers for a method are method inclusive times (based on CPU Sampling (CPU) and Context Switch (Wait) data) summed across all threads per process." + Environment.NewLine +
        "                         Summed Wait times of multiple threads lead to unreasonably large values." + Environment.NewLine +
        "                         -ShowTotal xxx             Print totals of all selected methods/stacktags. xxx can be Process, Method or Total. " + Environment.NewLine +
        "                                                    Total:   Print only file name and totals." + Environment.NewLine +  
        "                                                    Process: Print file and process totals." + Environment.NewLine + 
        "                                                    Method:  Print additionally the selected methods which were used for total calculation." + Environment.NewLine +
        "                                                    Warning: The input values are method are method inclusive times summed across all threads in a process."+ Environment.NewLine +
        "                                                             You should filter for specific independent methods/stacktags which are not already included to get meaningful results." + Environment.NewLine +
        "                         -ShowOnMethod              Display process name besides method name without the command line. This allows to see trends in CPU changes over time for a specific method in console output better." + Environment.NewLine +
        "                         -ShowModuleInfo [Driver]   Show dll version of each matching method until another dll is show in the printed list. When Driver is specified only module infos of well" + Environment.NewLine +
        "                                                    known AV and Filter drivers are printed (or written to CSV output). This helps to identify which AV solution is running on that machine." + Environment.NewLine +
        "                         -MinMaxFirst minS [maxS]   Include methods/stacktags which match the first occurrence in [min, max] in seconds. You can shift time with -ZeroTime. " + Environment.NewLine +
        "                                                    E.g. \"-MinMaxFirst 0 -ZeroTime First Click\" will show all methods after Click." + Environment.NewLine + 
        "                         -MinMaxLast  minS [maxS]   Include methods/stacktags which match the last occurrence in [min max] in seconds." + Environment.NewLine +
        "                         -MinMaxDuration min [maxS] Include methods/stacktags which have a range of first/last occurrence if [min max] in seconds. This value is ZeroTime independent." + Environment.NewLine + 
        "                         -FirstLastDuration/fld [[first] [lastfmt]]   Show time in s where a stack sample was found the first and last time in this trace. Useful to estimate async method runtime or to correlate times in WPA." + Environment.NewLine +
        "                                                    The options first and lastfmt print, when present, the first and/or last time the method did show up in profiling data. Affects also time format in -CSV output (default is s)." + Environment.NewLine +
        "                         -ZeroTime/zt               Shift first/last method time. This also affects -csv output. Useful to see method timings relative to the first occurrence of e.g. method OnClick." + Environment.NewLine +
        "                             Marker filter          Zero is a ETW marker event defined by filter." + Environment.NewLine +
        "                             First  filter          Select the first occurrence of a method/stacktag as zero time point. If the filter is ambiguous consider to refine the filter or add -ZeroProcessName to limit it to a specific process." + Environment.NewLine +
        "                             Last   filter          Select the last occurrence of a method/stacktag as zero time point." + Environment.NewLine +
        "                             ProcessStart/ProcessEnd [CmdLine] Select process start/stop event as zero point which matches the optional CmdLine filter string and the -ZeroProcessName filter." + Environment.NewLine + 
        "                         -ZeroProcessName/zpn x.exe Select the process from which the zero time point will be used for ProcessStart/First/Last Method zero point definition." + Environment.NewLine +
        "                         -CutMethod xx-yy           Shorten method/stacktag name to make output more readable. Skip xx chars and take yy chars. If -yy is present the last yy characters are taken." + Environment.NewLine +
        "                         -includeDll                Include the declaring dll name in the full method name like xxx.dll!MethodName" + Environment.NewLine +
        "                         -includeArgs               Include the full method prototype when present like MethodName(string arg1, string arg2, ...)" + Environment.NewLine +
        "                         -Methods *Func1*;xxx.dll!FullMethodName   Dump one or more methods from all or selected processes. When omitted only CPU totals of the process and command line are printed to give an overview." + Environment.NewLine +
        "                         -StackTags *tag1;Tag2*     Use * to dump all. Dump one or more stacktags from all or selected processes." + Environment.NewLine +
        "                         -topN dd nn                Include only first dd processes with most CPU in trace. Optional nn skips the first nn lines. To see e.g. Lines 20-30 use -topn 10 20" + Environment.NewLine +
        "                         -topNMethods dd nn         Include dd most expensive methods/stacktags which consume most CPU in trace. Optional nn skips the first nn lines." + Environment.NewLine +
        "                         -ThreadCount               Show # of unique threads that did execute that method." + Environment.NewLine +
        "                         -ProcessFmt timefmt        Format besides process name the start stop time. See -TimeFmt for available options." + Environment.NewLine +
        "                         -SortBy [CPU/Wait/StackDepth/First/Last] Default method sort order is CPU consumption. Wait sorts by wait time, First/Last sorts by first/last occurrence of method/stacktags." + Environment.NewLine +
        "                                                    StackDepth shows hottest methods which consume most CPU but are deepest in the call stack." + Environment.NewLine + 
        "                         -MinMaxCpuMs xx-yy or xx   Only include methods/stacktags with a minimum CPU consumption of [xx,yy] ms." + Environment.NewLine +
        "                         -MinMaxWaitMs xx-yy or xx  Only include methods/stacktags with a minimum wait time of [xx,yy] ms." + Environment.NewLine +
        "                         For other options [-recursive] [-csv] [-NoCSVSeparator] [-TimeFmt] [-TestsPerRun] [-SkipNTests] [-TestRunIndex] [-TestRunCount] [-TestCase] [-Machine] [-ProcessName/pn] [-NewProcess] [-CmdLine]" + Environment.NewLine +
        "                         refer to help of TestRun and Process. Run \'EtwAnalyzer -help dump\' to get more infos." + Environment.NewLine;

        static readonly string MemoryHelpString =
        "  Memory    -filedir/fd Extract\\ or xxx.json [-recursive] [-csv xxx.csv] [-NoCSVSeparator] [-TopN dd nn] [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-TotalMemory] [-MinDiffMB dd] " + Environment.NewLine +
        "                           [-SortBy Commit/WorkingSet/SharedCommit/Diff] [-GlobalDiffMB dd] [-MinWorkingSetMB dd] [-Clip]" + Environment.NewLine + 
        "                           [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-TestCase xx] [-Machine xxxx] [-ProcessName/pn xxx.exe(pid)] [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                         Print memory (Working Set, Committed Memory) of all or some processes from extracted Json files. To get output -extract Memory, All or Default must have been used during extraction." + Environment.NewLine +
        "                         -SortBy Commit/SharedCommit Sort by Committed/Shared Committed (this is are memory mapped files, or page file allocated file mappings). " + Environment.NewLine + "" +
        "                                 WorkingSet/Diff    Sort by working set or committed memory difference" + Environment.NewLine +
        "                         -TopN dd nn                Select top dd processes. Optional nn skips the first nn lines of top list" + Environment.NewLine +
        "                         -TotalMemory               Show System wide commit and active memory metrics. Useful to check if machine was in a bad memory situation." + Environment.NewLine +
        "                         -MinWorkingSetMB dd        Only include processes which had at least a working set of dd MB at trace start" + Environment.NewLine +
        "                         -MinDiffMB    dd           Include processes which have gained inside one Json file more than xx MB of committed memory." + Environment.NewLine +
        "                         -GlobalDiffMB dd           Same as before but the diff is calculated across all incuded Json files." + Environment.NewLine +
        "                         For other options [-recursive] [-csv] [-NoCSVSeparator] [-TimeFmt] [-TestsPerRun] [-SkipNTests] [-TestRunIndex] [-TestRunCount] [-TestCase] [-Machine] [-ProcessName/pn] [-NewProcess] [-CmdLine]" + Environment.NewLine +
        "                         refer to help of TestRun and Process. Run \'EtwAnalyzer -help dump\' to get more infos." + Environment.NewLine;
        static readonly string ExceptionHelpString =
        "  Exception -filedir/fd Extract\\ or xxx.json [-FilterExceptions] [-Type xxx] [-Message xxx] [-FullMessage] [-Showstack] [-CutStack dd-yy] [-Stackfilter xxx] [-recursive] [-csv xxx.csv] [-NoCSVSeparator] " + Environment.NewLine +
        "                           [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-NoCmdLine] [-Clip] [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-TestCase xx] [-Machine xxxx]" + Environment.NewLine +
        "                           [-MinMaxExTime minS [maxS]] [-ZeroTime/zt Marker/First/Last/ProcessStart filter] [-ZeroProcessName/zpn filter]" + Environment.NewLine +
        "                           [-ProcessName/pn xxx.exe(pid)] [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                         Print Managed Exceptions from extracted Json file. To get output -extract Exception, All or Default must have been used during extraction." + Environment.NewLine +
        "                         Before each message the number how often that exception was thrown is printed. That number also includes rethrows in finally blocks which leads to higher numbers as one might expect!" + Environment.NewLine + 
        "                         When a filter (type,message or stack) is used then the exception throw times are also printed." + Environment.NewLine +
        "                         -NoCmdLine                 Do not print command line arguments in process name at console output" + Environment.NewLine +
        "                         -Type *type1*;*type2*      Filter Exception by type e.g. *timeoutexception*. Multiple filters can be combined with ;" + Environment.NewLine +
        "                         -Message *msg1*;*msg2*     Filter Exception by message e.g. *denied*" + Environment.NewLine +
        "                         -StackFilter *f1*;*f2*     Filter Exception by a stack substring of the stacktrace string" + Environment.NewLine +
        "                         -MinMaxExTime minS [maxS]  Filter by exception time in s since trace start. Use -Timefmt s to print time in this format." + Environment.NewLine + 
        "                         -ShowStack                 Show Stacktrace for every exception. By default the first 50 frames are displayed. " + Environment.NewLine + 
        "                                                    To change use -CutStack. You should filter first as much as possible before using this on the console." + Environment.NewLine + 
        "                                                    Only when -type, -message or -stackfilter are active the stack is printed to console." + Environment.NewLine +
        "                         -CutStack dd-yy            Remove the first dd lines of the stack. To display all stack frames use \"-CutStack 0-\". Print yy lines or all if -yy is omitted." + Environment.NewLine + 
        "                                                    E.g. -CutStack -50 will display the first 50 lines of a stack trace." + Environment.NewLine +
        "                         -FilterExceptions          Filter exceptions away which are normally harmless. The filter file is located in Configuration\\ExceptionFilters.xml." + Environment.NewLine +
        "                                                    You need this only when you have used during -extract Exception -allExceptions where the same filter will be applied during extraction already." + Environment.NewLine +
        "                         -FullMessage               By default only the first 500 characters of the exception are printed on the console. Use this to see the full message text." + Environment.NewLine +
        "                         For other options [-ZeroTime ..] [-recursive] [-csv] [-NoCSVSeparator] [-TimeFmt] [-TestsPerRun] [-SkipNTests] [-TestRunIndex] [-TestRunCount] [-TestCase] [-Machine] [-ProcessName/pn] [-NewProcess] [-CmdLine]" + Environment.NewLine +
        "                         refer to help of TestRun and Process. Run \'EtwAnalyzer -help dump\' to get more infos." + Environment.NewLine;

        static readonly string DiskHelpString =
        "  Disk -filedir/fd Extract\\ or xxx.json [-DirLevel dd] [-PerProcess] [-filename *C:*] [-MinMax xx-yy] [-TopN dd nn] [-SortBy order] [-FileOperation op] [-ReverseFileName/rfn] [-Merge] [-recursive] [-csv xxx.csv] [-NoCSVSeparator]" + Environment.NewLine +
        "                         [-TopNProcesses dd nn] [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-Clip] [-TestsPerRun dd - SkipNTests dd] [-TestRunIndex dd - TestRunCount dd] [-TestCase xx] [-Machine xxxx] [-ProcessName/pn xxx.exe(pid)]" + Environment.NewLine +
        "                         [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                         Print disk IO metrics to console or to a CSV file if -csv is used. To get output -extract Disk, All or Default must have been used during extraction." + Environment.NewLine +
        "                         The extracted data is an exact summary per file and all involved processes. If multiple processes access one file you get only the list of processes but not which process did attribute how much." + Environment.NewLine +
        "                         -DirLevel dd               Print Disk IO per directory up to n levels. Default is 0 which shows aggregates per drive. -Dirlevel 100 shows a per file summary." + Environment.NewLine +
        "                         -PerProcess                Print Disk IO for processes where IOTime > 1s. If you use -processname as filter you can restrict io to all files where the process was involved. " + Environment.NewLine +
        "                                                    Note: This might overestimate IO because commonly used files by many processes like pagefiles are then attributed multiple times to all involved processes." + Environment.NewLine +
        "                         -TopNProcesses dd nn       Select top dd (skip nn) processes when -PerProcess is enabled." + Environment.NewLine +
        "                         -FileName *C:*             Filter IO for specific files only. Multiple filters are separated by ;" + Environment.NewLine +
        "                         -FileOperation op          Filter for rows where only specific file operations are present. Possible values are Read and Write" + Environment.NewLine +
        "                                                    Warning: Other columns than the filtered one can be misleading. E.g. if you filter for read then in the write column only files will show up from which data was also read!" + Environment.NewLine +
        "                         -SortBy order              Console Output Only. Valid values are: ReadSize,WriteSize,ReadTime,WriteTime,FlushTime,TotalSize and TotalTime (= Read+Write+Flush). Default is TotalTime." + Environment.NewLine +
        "                         -TopN dd nn                Select top dd (skip nn) files based on current sort order." + Environment.NewLine +
        "                         -MinMax xx-yy              Console Output Only. Filter for rows which have > xx and < yy us of total disk IO time." + Environment.NewLine +
        "                                                    Filter for read duration  > 1ms : -MinMax 1000 -FileOperation Read" + Environment.NewLine +
        "                                                    Filter for write duration > 1ms : -MinMax 1000 -FileOperation Write" + Environment.NewLine +
        "                         -ReverseFileName/rfn       Reverse file name. Useful with -Clip to keep output clean (no console wraparound regardless how long the file name is)." + Environment.NewLine +
        "                         -Merge                     Merge all selected Json files into one summary output. Useful to get a merged view of a session consisting of multiple ETL files." + Environment.NewLine +
        "                         For other options [-recursive] [-csv] [-NoCSVSeparator] [-TimeFmt] [-TestsPerRun] [-SkipNTests] [-TestRunIndex] [-TestRunCount] [-TestCase] [-Machine] [-ProcessName/pn] [-NewProcess] [-CmdLine]" + Environment.NewLine +
        "                         refer to help of TestRun and Process. Run \'EtwAnalyzer -help dump\' to get more infos." + Environment.NewLine;

        static readonly string FileHelpString =
        "  File -filedir/fd Extract\\ or xxx.json [-DirLevel dd] [-PerProcess] [-filename *C:*] [-ShowTotal [Total/Process/File]] [-MinMax xx-yy] [-TopN dd nn] [-SortBy order] [-FileOperation op] [-ReverseFileName/rfn] [-Merge] [-Details] [-recursive] " + Environment.NewLine +
        "                         [-TopNProcesses dd nn] [-csv xxx.csv] [-NoCSVSeparator] [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-Clip] [-TestsPerRun dd -SkipNTests dd] " + Environment.NewLine +
        "                         [-TestRunIndex dd -TestRunCount dd] [-TestCase xx] [-Machine xxxx] [-ProcessName/pn xxx.exe(pid)] [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                         Print File IO metrics to console or to a CSV file if -csv is used. To get output -extract File, All or Default must have been used during extraction." + Environment.NewLine +
        "                         The extracted data is an exact summary per file and process. Unlike Disk IO, File IO tracing captures all file accesses regardless if the data was e.g. read from disk or file system cache." + Environment.NewLine +
        "                         -DirLevel dd               Print File IO per directory up to n levels. Default is 0 which shows summary per drive. -Dirlevel 100 will give a per file summary." + Environment.NewLine +
        "                         -PerProcess                Print File IO per process. If you use -processname as filter you can restrict IO to all files where the process was involved. " + Environment.NewLine +
        "                         -TopNProcesses dd nn       Select top dd (skip nn) processes when -PerProcess is enabled." + Environment.NewLine + 
        "                         -FileName *C:*             Filter IO for specific files only. Multiple filters are separated by ;" + Environment.NewLine +
        "                         -FileOperation op          Filter for rows where only specific file operations are present. " + Environment.NewLine + 
        "                                                    Possible values are " + String.Join(",", Enum.GetNames(typeof(Extract.FileIO.FileIOStatistics.FileOperation)).Where(x => x != "Invalid")) + Environment.NewLine +
        "                                                    Warning: Other columns than the filtered one can be misleading. " +Environment.NewLine +  
        "                                                    E.g. if you filter for open, only the files which were opened are showing up in read/write metrics. IO for already opened files is suppressed!" + Environment.NewLine +
        "                         -SortBy order              Console Output Only. Valid values are: ReadSize,WriteSize,ReadTime,WriteTime,TotalSize and TotalTime (= Open+Close+Read+Write). Default is TotalTime." + Environment.NewLine +
        "                         -TopN dd nn                Select top dd files based on current sort order." + Environment.NewLine +
        "                         -MinMax xx-yy              Console Output Only. Filter for rows which have > xx and < yy. The -FileOperation, -SortBy values define on which values it filters." + Environment.NewLine + 
        "                                                    You can define filters for time,size,length,count of open/close/read/write/setsecurity operations." + Environment.NewLine +
        "                                                    Filter for read operation Count > 500: -MinMax 500 -FileOperation Read -SortBy Count" + Environment.NewLine +
        "                                                    Filter for read operation byte size > 1000000 bytes: -MinMax 1000000 -FileOperation Read -SortBy Size" + Environment.NewLine +
        "                                                    Filter for Open Duration > 10 us: -MinMax 10 -FileOperation Open -SortBy Time " + Environment.NewLine +
        "                                                    Filter by file (read+write) size > 1000000 bytes: -MinMax 1000000 -SortBy Length" + Environment.NewLine + 
        "                         -Details                   Show more columns" + Environment.NewLine + 
        "                         -ReverseFileName/rfn       Reverse file name. Useful with -Clip to keep output clean (no console wraparound regardless how long the file name is)." + Environment.NewLine + 
        "                         -Merge                     Merge all selected Json files into one summary output. Useful to get a merged view of a session consisting of multiple ETL files." + Environment.NewLine + 
        "                         -ShowTotal [Total/Process/File] Show totals for the complete File/per process but skip aggregated directory metrics/per process but show also original aggregated directory metrics." + Environment.NewLine + 
        "                         For other options [-recursive] [-csv] [-NoCSVSeparator] [-TimeFmt] [-TestsPerRun] [-SkipNTests] [-TestRunIndex] [-TestRunCount] [-TestCase] [-Machine] [-ProcessName/pn] [-NewProcess] [-CmdLine]" + Environment.NewLine +
        "                         refer to help of TestRun and Process. Run \'EtwAnalyzer -help dump\' to get more infos." + Environment.NewLine;

        static readonly string ThreadPoolHelpString =
        "  ThreadPool -filedir/fd Extract\\ or xxx.json [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-recursive] [-csv xxx.csv] [-NoCSVSeparator] [-NoCmdLine] [-Clip] " + Environment.NewLine +
        "              [-TestsPerRun dd - SkipNTests dd][-TestRunIndex dd - TestRunCount dd][-TestCase xx][-Machine xxxx][-ProcessName / pn xxxx; yyy] [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                         Print Threadpool Starvation incidents. To get output -extract ThreadPoool or All must have been used during extraction. " + Environment.NewLine + 
        "                         During recording the Microsoft-Windows-DotNETRuntime ETW provider with Keyword ThreadingKeyword (0x10000) must have been enabled. " + Environment.NewLine +
        "                         -NoCmdLine                 Do not print command line arguments in process name at console output" + Environment.NewLine;

        static readonly string MarkHelpString =
        "  Mark -filedir/fd Extract\\ or xxx.json [-MarkerFilter xxx] [-ZeroTime marker filter] [-recursive] [-csv xxx.csv] [-NoCSVSeparator] [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-NoCmdLine] [-Clip] " + Environment.NewLine +
        "       [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-TestCase xx] [-Machine xxxx] [-ProcessName/pn xxx.exe(pid)] [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                         Print ETW Marker events" + Environment.NewLine +
        "                         -MarkerFilter xxx         Filter for specific marker events. Multiple filters are separated by ; Exclusion filters start with ! Supported wildcards are * and ?" + Environment.NewLine + 
        "                         -ZeroTime marker filter   Print diff time relative to a specific marker. The first matching marker (defined by filter) defines the zero time." + Environment.NewLine;

        static readonly string ExamplesHelpString =
        "[yellow]Examples[/yellow]" + Environment.NewLine;

        static readonly string StatsExamples = ExamplesHelpString +
        "[green]Dump from ETL file event statistics, session times, ...[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Stats -filedir/fd xxx.etl" + Environment.NewLine +
        "[green]Dump from Extracted Json files Core Count, Memory, OS Version on a single line (CSV export is also supported)[/green]" + Environment.NewLine+
        " ETWAnalyzer -dump Stats -filedir c:\\MainVersion\\Extract -properties NumberOfProcessors,MemorySizeMB,OSVersion -OneLine" + Environment.NewLine;

        static readonly string VersionExamples = ExamplesHelpString +
        "[green]Module version of all modules. Module marker files are configured in the Configuration\\DllToBuildMap.json file[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Version -fd xxx.etl/xxx.Json" + Environment.NewLine +
        "[green]Get .NET Runtime Versions of all processes[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Version -fd xxx.json -dll clr.dll" + Environment.NewLine +
        "[green]Get Device Drivers versions of System process[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Version -fd xxx.etl -dll *.sys -pn System" + Environment.NewLine +
        "[green]Get version of all .NET Runtimes of all processes in CSV file[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Version -fd xxx.etl -dll coreclr.dll;clr.dll" + Environment.NewLine;


        static readonly string ProcessExamples = ExamplesHelpString +
        "[green]Print potentially crashed processes. These are detected by their return code interpreted as NTStatus, or they did trigger WERFault.exe.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Process -filedir xx.etl/.json -crash" + Environment.NewLine +
        "[green]Processes with their command lines, start/stop times for a cmd.exe with pid 1024. Substrings (e.g. *x*) and exclusion (e.g. !*x*) are also supported. Use -CSV to store data[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Process -filedir xx.etl/.json -ProcessName cmd.exe(1024)" + Environment.NewLine +
        "[green]Processes which have a lifetime between 5-60s[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Process -fd xx.etl/.json -MinMaxDuration 5 60" + Environment.NewLine +
        "[green]Show process start times relative to the start of process with id 43188[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Process -fd xx.etl/.json -NewProcess 1 -timefmt s -ZeroTime ProcessStart -ZeroProcessName 43188" + Environment.NewLine +
        "[green]Dump process of all extracted files in current directory ordered by start time instead of grouped by process name. Print time as UTC.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Process -sortby time -timefmt utc" + Environment.NewLine;

        static readonly string TestRunExamples = ExamplesHelpString +
        "[green]Dump TestRuns from a given directory. Works with ETL and Extracted Json files[green]" + Environment.NewLine + 
        " ETWAnalyzer -dump TestRun -filedir C:\\MainVersion\\Extract" + Environment.NewLine +
        "[green]Download data ETL and Json data from a network share to speed up analysis[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump TestRun -filedir \\\\Server\\MainVersion\\Extract -copyfilesto C:\\Analysis\\MainVersion -TestsPerRun 1 -TestCase Test1 -SkipNTests 1 -WithEtl" + Environment.NewLine;

        static readonly string CPUExamples = ExamplesHelpString +
        "[green]Trend CPU consumption of one method (Type.Method) over the extracted profiling data over time for one Testcase[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -filedir c:\\MainVersion\\Extract -Methods *ImagingViewModel.InitAsync* -TestCase CallupAdhocColdReadingCR" + Environment.NewLine +
        "[green]Print Total CPU consumption of processes and their command line[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -filedir c:\\MainVersion\\Extract" + Environment.NewLine +
        "[green]Save CPU consumption trend of one method into a CSV for all test cases[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -filedir c:\\MainVersion\\Extract -Methods *ImagingViewModel.InitAsync* -csv c:\\temp\\InitAsyncPerf.csv" + Environment.NewLine +
        "[green]Trend CPU consumption of a method for a test case in one process with a specific command line[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -filedir c:\\MainVersion\\Extract -Methods *ImagingViewModel.InitAsync* -Machine TestServer -Test TestCase1 -ProcessName ServerBackend -CmdLine *ServerCmdArg*" + Environment.NewLine +
        "[green]Get an overview of the first 50 methods of the two processes consuming most CPU in the trace[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -fd xxx.json -topN 2 -topNMethods 50" + Environment.NewLine +
        "[green]Show common Antivirus drivers vendors besides module information for all modules for which no symbols could be resolved. The dll/driver name is then the \"method\" name.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -fd xxx.json -methods *.dll;*.sys -ShowModuleInfo Driver" + Environment.NewLine +
        "[green]Show all Import methods but skip file methods. Take only last 35 characters of method and show first last occurrence of method in trace time to relate with WPA timeline.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -methods *import*;!*file* -CutMethod -35 -fld s" + Environment.NewLine +
        "[green]Show method timings (first and last occurrence in trace) relative to OnClick[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -methods *import*;!*file* -ZeroTime FirstMethod *OnClick* -fld s s" + Environment.NewLine +
        "[green]Show unique methods which were executed in the last 5 s before process with pid 136816 did terminate. You see e.g. invoked error handlers just before a crash.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -fd xxx.json -methods * -SortBy First -ZeroTime ProcessEnd -ZeroProcessName 136816 -pn 136816 -MinMaxFirst -5" + Environment.NewLine;


        static readonly string MemoryExamples = ExamplesHelpString +
        "[green]Get an overview about system memory consumption across all ETL files belonging to a test run. The TestRun Index you can get from the output of -dump TestRun -filedir ...[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Memory -filedir C:\\Extract\\TestRuns -TotalMemory -TestRunIndex 100 -TestRunCount 1" + Environment.NewLine +
        "[green]Trace possible leaks across files with a total memory growth of at least 100 MB. Use -CSV to store data.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Memory -filedir C:\\Extract\\TestRuns -GlobalDiffMB 100 -TestRunIndex 100 -TestRunCount 1" + Environment.NewLine +
        "[green]Print top 5 processes having highest diff (diff can be memory growth or loss).[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Memory -SortBy Diff -TopN 5" + Environment.NewLine;

        static readonly string ExceptionExamples = ExamplesHelpString +
        "[green]Show all exceptions which did pass the exception filter during extraction, grouped by process, exception type and message.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Exception -filedir xx.json" + Environment.NewLine +
        "[green]Show all exceptions and their throw times by using a filter which matches all exceptions[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Exception -filedir xx.json -type *exception*" + Environment.NewLine +
        "[green]Show call stack of all SQLiteExceptions of one or all extracted files. Use -ProcessName and/or -CmdLine to focus on specific process/es. Use -CSV to store data.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Exception -fd xx.json -type *SQLiteException* -ShowStack" + Environment.NewLine +
        "[green]Show all exception times of all extracted files in current folder in UTC time. Default is Local time of the customer.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Exception -type * -timefmt utc" + Environment.NewLine +
        "[green]Dump all TimeoutExceptions after the first occurrence of method ShowShutdownWindow and write them to a CSV file.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Exception -Type* timeout* -TimeFmt s -ZeroTime First *ShowShutdownWindow* -MinMaxExTime 0 -CSV Exceptions.csv" + Environment.NewLine +
        "[green]Show stacks of all exceptions of all extracted files in current folder.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Exception -type * -ShowStack"+ Environment.NewLine;


        static readonly string DiskExamples = ExamplesHelpString +
        "[green]Show Disk IO per directory down to 3 levels of the E Drive[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Disk -filedir xx.json -DirLevel 3 -fileName E:*" + Environment.NewLine +
        "[green]Show Disk IO per process with name *Viewing* in the E:\\Store* folder.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Disk -filedir xx.json -PerProcess -fileName E:\\Store* -processname *Viewing*" + Environment.NewLine +
        "[green]Show Disk IO per file with Read Time > 1ms[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Disk -filedir xx.json -FileOperation Read -MinMax 1000 -DirLevel 100" + Environment.NewLine;


        static readonly string FileExamples = ExamplesHelpString +
        "[green]Show File IO summary of all processes at drive level[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -filedir xx.json" + Environment.NewLine +
        "[green]Show File IO per drive of processes Workflow below the folder E:\\lc\\c\\*[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -filedir xx.json -fileName E:\\lc\\c\\* -processname *Workflow*" + Environment.NewLine +
        "[green]Show File IO per first 3 sub folders of process Workflow below the folder E:\\lc\\c\\* of all extracted files in one metric[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -filedir xx.json -Merge -DirLevel 3 -fileName E:\\lc\\c\\* -processname *Workflow*" + Environment.NewLine +
        "[green]Show File IO at file level where Read+Write > 100 KB. Reverse file name and clip to console buffer width to prevent wraparound if file name is too long[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -filedir xx.json -DirLevel 100 -MinMax 100000 -Clip -ReverseFileName" + Environment.NewLine +
        "[green]Dump File IO data of process Workflow to CSV File. If a directory of files is given all data is dumped into the same CSV[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -filedir xx.json -processName *Workflow* -csv Workflow.csv" + Environment.NewLine +
        "[green]Dump File IO per process which is setting File Security. To get the times use the -csv option to export additional data to a file[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -fd xx.json -FileOperation SetSecurity -PerProcess" + Environment.NewLine +
        "[green]Dump File IO per process for all files in current directory, filter for write operations, and sort by Write Count[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -FileOperation Write -SortBy Count -PerProcess" + Environment.NewLine +
        "[green]Show per process totals for all processes.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -PerProcess -ShowTotal File" + Environment.NewLine;


        static readonly string ThreadPoolExamples = ExamplesHelpString +
        "[green]Show .NET ThreadPool starvation events[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump ThreadPool -filedir xx.json" + Environment.NewLine;

        static readonly string MarkerExamples = ExamplesHelpString +
        "[green]Show marker events. Print marker diff time relative to the *_Start event. Exclude all marker messages which contain screenshot in the string.[/green]" + Environment.NewLine +
        " ETWAnalyzer -filedir xx.json -dump Marker -ZeroTime marker *_Start  -MarkerFilter !*Screenshot*" + Environment.NewLine;


        /// <summary>
        /// Default Helpstring which prints all dump commands
        /// </summary>
        public static readonly string HelpString =
            DumpHelpStringPrefix +
            StatsHelpString +
            VersionHelpString +
            ProcessHelpString +
            TestRunHelpString +
            CPUHelpString +
            MemoryHelpString +
            ExceptionHelpString +
            DiskHelpString+
            FileHelpString+
            ThreadPoolHelpString + 
            MarkHelpString;


        DumpCommands myCommand = DumpCommands.None;

        string myEtlFileOrZip;
        bool myIsVerbose;
        bool myPrintFiles;


        /// <summary>
        /// Sort order which can later be added to more and more commands where it makes sense
        /// </summary>
        internal enum SortOrders
        {
            Size = 0,
            Default = 0,
            Count,
            Length,
            Time,
            CPU,
            Wait,
            StackDepth,
            Commit,
            WorkingSet,
            SharedCommit,
            Diff,
            First,
            Last,
            ReadSize,
            ReadTime,
            WriteSize,
            WriteTime,
            FlushTime,
            TotalSize,
            TotalTime,
        }

        /// <summary>
        /// Modes how totals are shown
        /// </summary>
        internal enum TotalModes
        {
            None,
            Total,
            Process,
            File,
            Method,
        }

        /// <summary>
        /// Describes how the -zerotime filter string is used to define the zero timepoint from where all trace absolute times are subtracted.
        /// </summary>
        internal enum ZeroTimeModes
        {
            None,

            /// <summary>
            /// Use marker events as zero timepoint marker
            /// </summary>
            Marker,

            /// <summary>
            /// Use FirstOccurrence of method/stacktag since trace start
            /// </summary>
            First,

            /// <summary>
            /// Use LastOccurrence of method/stacktag since trace start
            /// </summary>
            Last,

            /// <summary>
            /// Process Start
            /// </summary>
            ProcessStart,

            /// <summary>
            /// Process End
            /// </summary>
            ProcessEnd
        }


        SearchOption mySearchOption = SearchOption.TopDirectoryOnly;

        public Func<string,bool> ProcessNameFilter { get; private set; } = _ => true;
        public Func<string, bool> CmdLineFilter { get; private set; } = _ => true;
        public List<string> FileOrDirectoryQueries { get; private set; } = new List<string>();
        public string CSVFile { get; private set; }
        public bool NoCSVSeparator { get; internal set; }
        public Func<string,bool> TestCaseFilter { get; private set; } = _ => true;
        public Func<string, bool> MachineFilter { get; private set; } = _ => true;
        public int TestsPerRun { get; private set; }
        public SkipTakeRange TopN { get; private set; } = new SkipTakeRange();
        public int LastNDays { get; private set; }
        public int TestRunIndex { get; private set; } = -1;
        public int SkipNTests { get; private set; }
        public int TestRunCount { get; private set; }
        public bool ValidTestsOnly { get; private set; }
        public string CopyFilesTo { get; private set; }
        public bool WithETL { get; private set; }
        public bool Overwrite { get; private set; }
        public ProcessStates? NewProcess { get; private set; }
        public bool UsePrettyProcessName { get; private set; } = true;

        /// <summary>
        /// Controls how time is formatted in dump command output
        /// </summary>
        public DumpBase.TimeFormats TimeFormat { get; private set; }

        /// <summary>
        /// Format process start/end time in the desired way
        /// </summary>
        public DumpBase.TimeFormats? ProcessFormat { get; private set; }

        // Zero time definitions
        public ZeroTimeModes ZeroTimeMode { get; private set; }
        public KeyValuePair<string, Func<string, bool>> ZeroTimeFilter { get; private set; } = new KeyValuePair<string,Func<string,bool>>(null, _ => false);
        public Func<string, bool> ZeroTimeProcessNameFilter { get; private set; } = (x) => true;


        // Dump Stats specific flags
        public string Properties { get; private set; }
        public bool OneLine { get; private set; }

        // Dump Version specific flags
        public Func<string, bool> ModuleFilter { get; private set; } = _ => true;
        public KeyValuePair<string, Func<string, bool>> DllFilter { get; set; } = new KeyValuePair<string, Func<string, bool>>(null, _ => true);
        public KeyValuePair<string, Func<string, bool>> VersionFilter { get; set; } = new KeyValuePair<string, Func<string, bool>>(null, _ => true);

        // Dump Exception specific Flags
        public bool FilterExceptions { get; private set; }
        public KeyValuePair<string, Func<string, bool>> TypeFilter { get; private set; } = new KeyValuePair<string, Func<string, bool>>(null, _ => true);
        public KeyValuePair<string, Func<string, bool>> MessageFilter { get; private set; } = new KeyValuePair<string, Func<string, bool>>(null, _ => true);
        public KeyValuePair<string, Func<string, bool>> StackFilter { get; private set; } = new KeyValuePair<string, Func<string, bool>>(null, _ => true);
        public bool ShowFullMessage { get; private set; }
        public int CutStackMin { get; private set; }
        public int CutStackMax { get; private set; }
        public MinMaxRange<double> MinMaxExTimeS { get; private set; } = new MinMaxRange<double>();

        // Dump Process specific Flags
        public bool ShowAllProcesses { get; private set; }
        public bool ShowFileOnLine { get; private set; }
        public bool Crash { get; private set; }

        // Dump CPU specific Flags
        public KeyValuePair<string, Func<string, bool>> StackTagFilter { get; private set; }
        public KeyValuePair<string, Func<string, bool>> MethodFilter { get; private set; }

        public SkipTakeRange TopNMethods { get; private set; } = new SkipTakeRange();
        public bool NoDll { get; private set; } = true;
        public bool NoArgs { get; private set; } = true;

        public MinMaxRange<int> MinMaxCPUMs { get; private set; } = new MinMaxRange<int>();
        public MinMaxRange<int> MinMaxWaitMs { get; private set; } = new MinMaxRange<int>();
        public MinMaxRange<double> MinMaxFirstS { get; private set; } = new MinMaxRange<double>();
        public MinMaxRange<double> MinMaxLastS { get; private set; } = new MinMaxRange<double>();
        public MinMaxRange<double> MinMaxDurationS { get; private set; } = new MinMaxRange<double>();

        public int MethodCutStart { get; private set; }
        public int MethodCutLength { get; private set; } = int.MaxValue;

        public bool ShowStack { get; private set; }
        public bool ShowDetailsOnMethodLine { get; private set; }
        public bool ShowModuleInfo { get; private set; }
        public bool ShowDriversOnly { get; private set; }

        public bool ThreadCount { get; private set; }
        public bool FirstLastDuration { get; private set; }
        public DumpBase.TimeFormats? FirstTimeFormat { get; private set; }
        public DumpBase.TimeFormats? LastTimeFormat { get; private set; }
        public TotalModes ShowTotal { get; private set; }

        // Dump Memory specific Flags
        public bool TotalMemory { get; private set; }

        public int MinWorkingSetMB { get; private set; }
        public int MinDiffMB { get; private set; }
        public int GlobalDiffMB { get; private set; }

        // Dump Disk specific Flags
        public Func<string, bool> FileNameFilter { get; private set; } = _ => true;
        public int DirectoryLevel { get; private set; }
        public bool IsPerProcess { get; private set; }

        // Dump File/Disk common flags
        public bool Merge { get; private set; }
        public bool ReverseFileName { get; private set; }
        public SortOrders SortOrder { get; private set; }
		public SkipTakeRange TopNProcesses { get; private set; } = new SkipTakeRange();
		
        // Dump File specific flags
        public bool ShowAllFiles { get; private set; }
        public int Min { get; private set; }
        public int Max { get; private set; }
        public bool ShowDetails { get; private set; }
        public Extract.FileIO.FileIOStatistics.FileOperation FileOperation { get; private set; }
        

        // Dump ThreadPool specific Flags
        public bool NoCmdLine { get; private set; }

        // Dump Marker specific Flags
        public Func<string, bool> MarkerFilter { get; private set; } = _ => true;


        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="args"></param>
        public DumpCommand(string[] args) : base(args)
        {
        }

        /// <summary>
        /// Parse command and set parsed parameters in member variables
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        public override void Parse()
        {
            Action delayedThrower = () => { };

            while (myInputArguments.Count > 0)
            {
                string curArg = myInputArguments.Dequeue();

                switch (curArg?.ToLowerInvariant())
                {
                    case CommandFactory.DumpCommand:
                        // ignore -dump which is already known by factory
                        break;
                    case DebugArg:
                        Program.DebugOutput = true;
                        break;
                    case NoColorArg:
                        ColorConsole.EnableColor = false;
                        break;
                    case "-clip":
                        ColorConsole.ClipToConsoleWidth = true;
                        break;
                    case "-verbose":
                        myIsVerbose = true;
                        break;
                    case "-printfiles":
                        myPrintFiles = true;
                        break;
                    case "-validtestsonly":
                        ValidTestsOnly = true;
                        break;
                    case "-copyfilesto":
                        CopyFilesTo = GetNextNonArg("-copyfilesto");
                        break;
                    case "-withetl":
                        WithETL = true;
                        break;
                    case "-overwrite":
                        Overwrite = true;
                        break;
                    case RecursiveArg:
                        mySearchOption = SearchOption.AllDirectories;
                        break;
                    case "-filedir":
                    case "-fd":
                        FileOrDirectoryQueries.Add(GetNextNonArg("-filedir")); // we support multiple occurrences 
                        break;
                    case "-etl":
                        myEtlFileOrZip = GetNextNonArg("-etl");
                        break;
                    case "-newprocess":
                        string newProcessArg = GetNextNonArg("-newprocess");
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
                        NewProcess = int.Parse(newProcessArg) switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
                        {
                            0  => ProcessStates.None,
                            1  => ProcessStates.Started,
                            2  => ProcessStates.OnlyStarted,
                            -1 => ProcessStates.Stopped,
                            -2 => ProcessStates.OnlyStopped,
                        };
                        break;
                    case "-plainprocessnames":
                    case "-ppn":
                        UsePrettyProcessName = false;
                        break;
                    case "-showallprocesses":
                        ShowAllProcesses = true;
                        break;
                    case "-crash":
                        Crash = true;
                        break;
                    case "-details":
                        ShowDetails = true;
                        break;
                    case "-showfileonline":
                    case "-sfo":
                        ShowFileOnLine = true;
                        break;
                    case "-reversefilename":
                    case "-rfn":
                        ReverseFileName = true;
                        break;
                    case "-showmoduleinfo":
                    case "-smi":
                        ShowModuleInfo = true;
                        string additionalArg = GetNextNonArg("-showmoduleinfo", false);
                        ShowDriversOnly =  additionalArg?.ToLower() == "driver" ? true : false;
                        break;
                    case "-testsperrun":
                        this.TestsPerRun = int.Parse(GetNextNonArg("-testsperrun"), CultureInfo.InvariantCulture);
                        break;
                    case "-csv":
                        CSVFile = GetNextNonArg("-csv");
                        break;
                    case "-nocsvseparator":
                        NoCSVSeparator = true;
                        break;
                    case "-dirlevel":
                        string dirlevel = GetNextNonArg("-dirlevel");
                        DirectoryLevel = int.Parse(dirlevel, CultureInfo.InvariantCulture);
                        break;
                    case "-perprocess":
                        IsPerProcess = true;
                        break;
                    case "-merge":
                        Merge = true;
                        break;
                    case "-showallfiles":
                        ShowAllFiles = true;
                        break;
                    case "-topn":
                        string topN = GetNextNonArg("-topn");
                        string skip = GetNextNonArg("-topn", false); // skip string is optional
                        Tuple<int,int> topNAndSkip = topN.GetRange(skip);
                        TopN = new SkipTakeRange(topNAndSkip.Item1, topNAndSkip.Item2);
                        break;
                    case "-topnprocesses":
                        string topnProcessses = GetNextNonArg("-topnprocesses");
                        string skiptopnProcessses = GetNextNonArg("-topnprocesses", false);
                        Tuple<int, int> topNProcessesAndSkip = topnProcessses.GetRange(skiptopnProcessses);
                        TopNProcesses = new SkipTakeRange(topNProcessesAndSkip.Item1, topNProcessesAndSkip.Item2);
                        break;
                    case "-topnmethods":
                        string topnMethods = GetNextNonArg("-topnmethods");
                        string skipMethods = GetNextNonArg("-topnmethods", false); // skip string is optional
                        Tuple<int, int> topNAndSkipMethods = topnMethods.GetRange(skipMethods);
                        TopNMethods = new SkipTakeRange(topNAndSkipMethods.Item1, topNAndSkipMethods.Item2);
                        break;
                    case "-filterexceptions":
                        FilterExceptions = true;
                        break;
                    case "-modulefilter":
                        ModuleFilter =      Matcher.CreateMatcher(GetNextNonArg("-modulefilter"));
                        break;
                    case "-filename":
                        FileNameFilter =    Matcher.CreateMatcher(GetNextNonArg("-filename"));
                        break;
                    case "-processname":
                    case "-pn":
                        ProcessNameFilter = Matcher.CreateMatcher(GetNextNonArg("-processname"), MatchingMode.CaseInsensitive, pidFilterFormat:true);
                        break;
                    case "-zeroprocessname":
                    case "-zpn":
                        ZeroTimeProcessNameFilter = Matcher.CreateMatcher(GetNextNonArg("-zeroprocessname"), MatchingMode.CaseInsensitive, pidFilterFormat: true);
                        break;
                    case "-cmdline":
                        CmdLineFilter =     Matcher.CreateMatcher(GetNextNonArg("-cmdline"));
                        break;
                    case "-machine":
                        MachineFilter =     Matcher.CreateMatcher(GetNextNonArg("-machine"));
                        break;
                    case "-testcase":
                    case "-tc":
                        TestCaseFilter =    Matcher.CreateMatcher(GetNextNonArg("-testcase"));
                        break;
                    case "-markerfilter":
                        MarkerFilter =      Matcher.CreateMatcher(GetNextNonArg("-markerfilter"));
                        break;
                    case "-dll":
                        string dllFilter = GetNextNonArg("-dll");
                        DllFilter = new KeyValuePair<string, Func<string, bool>>(dllFilter, Matcher.CreateMatcher(dllFilter));
                        break;
                    case "-versionfilter":
                        string versionFilter = GetNextNonArg("-versionfilter");
                        VersionFilter = new KeyValuePair<string, Func<string, bool>>(versionFilter, Matcher.CreateMatcher(versionFilter));
                        break;
                    case "-type":
                        string typeFilter = GetNextNonArg("-type");
                        TypeFilter =            new KeyValuePair<string, Func<string, bool>>(typeFilter, Matcher.CreateMatcher(typeFilter));
                        break;
                    case "-message":
                        string messageFilter = GetNextNonArg("-message");
                        MessageFilter =         new KeyValuePair<string, Func<string, bool>>(messageFilter, Matcher.CreateMatcher(messageFilter));
                        break;
                    case "-stackfilter":
                    case "-sf":
                        string stackfilter = GetNextNonArg("-stackfilter");
                        StackFilter =           new KeyValuePair<string, Func<string, bool>>(stackfilter, Matcher.CreateMatcher(stackfilter));
                        break;
                    case "-methods":
                        string methodFilter = GetNextNonArg("-methods");
                         MethodFilter =         new KeyValuePair<string, Func<string, bool>>(methodFilter,   Matcher.CreateMatcher(methodFilter));
                        break;
                    case "-stacktags":
                        string stacktagFilter = GetNextNonArg("-stacktags");
                        StackTagFilter =        new KeyValuePair<string, Func<string, bool>>(stacktagFilter, Matcher.CreateMatcher(stacktagFilter));
                        break;
                    case "-zerotime":
                    case "-zt":
                        string zerotimeType = GetNextNonArg("-zerotime");
                        ParseEnum<ZeroTimeModes>("ZeroTimeModes", zerotimeType,
                            () => { ZeroTimeMode = (ZeroTimeModes)Enum.Parse(typeof(ZeroTimeModes), zerotimeType, true); },
                            ZeroTimeModes.None);
                        string zerotimeFilter = GetNextNonArg("-zerotime", ZeroTimeMode != ZeroTimeModes.ProcessStart && ZeroTimeMode != ZeroTimeModes.ProcessEnd);  // cmd line filter is optional for ProcessStart/End
                        ZeroTimeFilter = new KeyValuePair<string, Func<string, bool>>(zerotimeFilter, Matcher.CreateMatcher(zerotimeFilter));
                        break;
                    case "-showonmethod":
                        ShowDetailsOnMethodLine = true;
                        break;
                    case "-showtotal":
                        string showTotal = GetNextNonArg("-showtotal");
                        ParseEnum<TotalModes>("ShowTotal values", showTotal,
                                             () => { ShowTotal = (TotalModes)Enum.Parse(typeof(TotalModes), showTotal, true); });
                        break;
                    case "-cutmethod":
                        string cutmethod = GetNextNonArg("-cutmethod");
                        KeyValuePair<int, int> cutminmax = cutmethod.GetMinMax(true);
                        MethodCutStart = cutminmax.Key;
                        MethodCutLength = cutminmax.Value;
                        break;
                    case "-minmaxcpums":
                        string minMaxCPUms = GetNextNonArg("-minmaxcpums");
                        KeyValuePair<int, int> minMax = minMaxCPUms.GetMinMax();
                        MinMaxCPUMs = new MinMaxRange<int>(minMax.Key, minMax.Value);
                        break;
                    case "-minmaxwaitms":
                        string minMaxWaitms = GetNextNonArg("-minmaxwaitms");
                        KeyValuePair<int, int> minMaxWait = minMaxWaitms.GetMinMax();
                        MinMaxWaitMs = new MinMaxRange<int>(minMaxWait.Key, minMaxWait.Value);
                        break;
                    case "-minmax":
                        string minmaxStr = GetNextNonArg("-minmax");
                        KeyValuePair<int, int> minMaxV = minmaxStr.GetMinMax();
                        Min = minMaxV.Key;
                        Max = minMaxV.Value;
                        break;
                    case "-minmaxfirst":
                        string minFirst = GetNextNonArg("-minmaxfirst");
                        string maxFirst = GetNextNonArg("-minmaxfirst", false); // optional
                        Tuple<double,double> minMaxFirst = minFirst.GetMinMaxDouble(maxFirst);
                        MinMaxFirstS = new MinMaxRange<double>(minMaxFirst.Item1, minMaxFirst.Item2);
                        break;
                    case "-minmaxlast":
                        string minLast = GetNextNonArg("-minmaxlast");
                        string maxLast = GetNextNonArg("-minmaxlast", false); // optional
                        Tuple<double, double> minMaxLast = minLast.GetMinMaxDouble(maxLast);
                        MinMaxLastS = new MinMaxRange<double>(minMaxLast.Item1, minMaxLast.Item2);
                        break;
                    case "-minmaxduration":
                        string minDuration = GetNextNonArg("-minmaxduration");
                        string maxDuration = GetNextNonArg("-minmaxduration", false); // optional
                        Tuple<double, double> minMaxDuration = minDuration.GetMinMaxDouble(maxDuration);
                        MinMaxDurationS = new MinMaxRange<double>(minMaxDuration.Item1, minMaxDuration.Item2);
                        break;
                    case "-minmaxextime":
                        string minExTime = GetNextNonArg("-minmaxextime");
                        string maxExTime = GetNextNonArg("-minmaxextime", false); // optional
                        Tuple<double, double> exMinMax = minExTime.GetMinMaxDouble(maxExTime);
                        MinMaxExTimeS = new MinMaxRange<double>(exMinMax.Item1, exMinMax.Item2);
                        break;
                    case "-cutstack":
                        string cutStackStr = GetNextNonArg("-cutstack");
                        KeyValuePair<int, int> cutStackMinMax = cutStackStr.GetMinMax();
                        CutStackMin = cutStackMinMax.Key;
                        CutStackMax = cutStackMinMax.Value;
                        break;
                    case "-fileoperation":
                        string fileOp = GetNextNonArg("-fileoperation");
                        ParseEnum<Extract.FileIO.FileIOStatistics.FileOperation>("FileOperation values", fileOp,
                            () => { FileOperation = (Extract.FileIO.FileIOStatistics.FileOperation)Enum.Parse(typeof(Extract.FileIO.FileIOStatistics.FileOperation), fileOp, true); },
                            Extract.FileIO.FileIOStatistics.FileOperation.Invalid);
                        break;
                    case "-sortby":
                        string sortOrder = GetNextNonArg("-sortby");
                        ParseEnum<SortOrders>("SortOrder values", sortOrder,
                              () => { SortOrder = (SortOrders)Enum.Parse(typeof(SortOrders), sortOrder, true); });
                        break;
                    case "-timefmt":
                        string timeformatStr = GetNextNonArg("-timefmt");
                        ParseEnum<DumpBase.TimeFormats>("Time Format", timeformatStr, 
                            () => { TimeFormat = (DumpBase.TimeFormats)Enum.Parse(typeof(DumpBase.TimeFormats), timeformatStr, true); });
                        break;
                    case "-processfmt":
                        string processformatStr = GetNextNonArg("-processfmt");
                        ParseEnum<DumpBase.TimeFormats>("Time Format", processformatStr, 
                           () => { ProcessFormat = (DumpBase.TimeFormats)Enum.Parse(typeof(DumpBase.TimeFormats), processformatStr, true); });
                        break;
                    case "-nocmdline":
                        NoCmdLine = true;
                        break;
                    case "-threadcount":
                        ThreadCount = true;
                        break;
                    case "-firstlastduration":
                    case "-fld":
                        FirstLastDuration = true;
                        string firstfmt = GetNextNonArg("-firstlastduration", false);
                        ParseEnum<DumpBase.TimeFormats>("Time Format", firstfmt, 
                           () => { FirstTimeFormat = firstfmt == null ? null : (DumpBase.TimeFormats)Enum.Parse(typeof(DumpBase.TimeFormats), firstfmt, true); }); 
                        string lastfmt = GetNextNonArg("-firstlastduration", false);
                        ParseEnum<DumpBase.TimeFormats>("Time Format", lastfmt, 
                            () => { LastTimeFormat = lastfmt == null ? null : (DumpBase.TimeFormats)Enum.Parse(typeof(DumpBase.TimeFormats), lastfmt, true); });
                        break;
                    case "-includedll":
                    case "-id":
                        NoDll = false;
                        break;
                    case "-includeargs":
                    case "-ia":
                        NoArgs = false;
                        break;
                    case "-testrunindex":
                    case "-tri":
                        string testRun = GetNextNonArg("-testrunindex");
                        TestRunIndex = int.Parse(testRun, CultureInfo.InvariantCulture);
                        break;
                    case "-testruncount":
                    case "-trc":
                        string testrunCount = GetNextNonArg("-testruncount");
                        TestRunCount = int.Parse(testrunCount, CultureInfo.InvariantCulture);
                        break;
                    case "-showstack":
                    case "-ss":
                        ShowStack = true;
                        break;
                    case "-fullmessage":
                    case "-fm":
                        ShowFullMessage = true;
                        break;
                    case "-totalmemory":
                    case "-tm":
                        TotalMemory = true;
                        break;
                    case "-mindiffmb":
                        string minDiffMB = GetNextNonArg("-mindiffmb");
                        MinDiffMB = int.Parse(minDiffMB, CultureInfo.InvariantCulture);
                        break;
                    case "-minworkingsetmb":
                        string minworkingsetmb = GetNextNonArg("-minworkingsetmb");
                        MinWorkingSetMB = int.Parse(minworkingsetmb, CultureInfo.InvariantCulture);
                        break;
                    case "-globaldiffmb":
                        string globalDiffMB = GetNextNonArg("-globaldiffmb");
                        GlobalDiffMB = int.Parse(globalDiffMB, CultureInfo.InvariantCulture);
                        break;
                    case "-lastndays":
                        string lastNWeeks = GetNextNonArg("-lastndays");
                        LastNDays = int.Parse(lastNWeeks, CultureInfo.InvariantCulture);
                        break;
                    case "-skipntests":
                        string skipNTests = GetNextNonArg("-skipntests");
                        SkipNTests = int.Parse(skipNTests, CultureInfo.InvariantCulture);
                        break;
                    case "-properties":
                        Properties = GetNextNonArg("-properties");
                        break;
                    case "-oneline":
                        OneLine = true;
                        break;
                    case "cpu":
                        myCommand = DumpCommands.CPU;
                        break;
                    case "exception":
                        myCommand = DumpCommands.Exceptions;
                        break;
                    case "stats":
                        myCommand = DumpCommands.Stats;
                        break;
                    case "process":
                        myCommand = DumpCommands.Process;
                        break;
                    case "version":
                        myCommand = DumpCommands.Versions;
                        break;
                    case "memory":
                        myCommand = DumpCommands.Memory;
                        break;
                    case "disk":
                        myCommand = DumpCommands.Disk;
                        break;
                    case "file":
                        myCommand = DumpCommands.File;
                        break;
                    case "allocation":
                        myEtlFileOrZip = GetNextNonArg("allocations");
                        break;
                    case "testrun":
                        myCommand = DumpCommands.TestRuns;
                        break;
                    case "threadpool":
                        myCommand = DumpCommands.ThreadPool;
                        break;
                    case "mark":
                        myCommand = DumpCommands.Mark;
                        break;
                    default:
                        // parse all command line arguments and throw exception for last found wrong argument to enable context sensitive help
                        delayedThrower = () =>
                        {
                            string errorMsg = myCommand == DumpCommands.None ?
                                          $"Dump command {curArg} is not valid.{Environment.NewLine}{DumpHelpStringPrefix}" :
                                          $"The argument {curArg} was not recognized as valid argument";

                            throw new NotSupportedException(errorMsg);
                        };
                        break;
                }
            }

            // throw if any exception is pending.
            delayedThrower();
        }

        public override string Help
        {
            get
            {
                string lret = HelpString;

                switch (myCommand)
                {
                    case DumpCommands.CPU:
                        lret = CPUExamples + Environment.NewLine +  CPUHelpString ;
                        break;
                    case DumpCommands.Disk:
                        lret = DiskExamples + Environment.NewLine + DiskHelpString;
                        break;
                    case DumpCommands.File:
                        lret = FileExamples + Environment.NewLine + FileHelpString;
                        break;
                    case DumpCommands.Exceptions:
                        lret = ExceptionExamples + Environment.NewLine + ExceptionHelpString;
                        break;
                    case DumpCommands.Memory:
                        lret = MemoryExamples + Environment.NewLine + MemoryHelpString;
                        break;
                    case DumpCommands.Process:
                        lret = ProcessExamples + Environment.NewLine + ProcessHelpString;
                        break;
                    case DumpCommands.Stats:
                        lret = StatsExamples + Environment.NewLine + StatsHelpString;
                        break;
                    case DumpCommands.TestRuns:
                        lret = TestRunExamples + Environment.NewLine + TestRunHelpString;
                        break;
                    case DumpCommands.Versions:
                        lret = VersionExamples + Environment.NewLine + VersionHelpString;
                        break;
                    case DumpCommands.ThreadPool:
                        lret = ThreadPoolExamples + Environment.NewLine + ThreadPoolHelpString;
                        break;
                    case DumpCommands.Mark:
                        lret = MarkerExamples + Environment.NewLine +  MarkHelpString;
                        break;
                }
                return lret.TrimEnd(Environment.NewLine.ToCharArray());
            }
        }

        public override void Run()
        {
            string decompressedETL = null;
            if (myCommand < DumpCommands.TestRuns && myCommand != DumpCommands.None)
            {
                string ext = Path.GetExtension(myEtlFileOrZip);
                if (ext == TestRun.ExtractExtension)
                {
                    decompressedETL = myEtlFileOrZip;
                }
                else
                {
                    myEtlFileOrZip = ArgParser.CheckIfFileOrDirectoryExistsAndExtension(myEtlFileOrZip, EtlExtension, ZipExtension, SevenZipExtension);
                    decompressedETL = ETWAnalyzer.Extractors.ExtractSingleFile.ExtractETLIfZipped(myEtlFileOrZip, Path.GetDirectoryName(myEtlFileOrZip), Symbols, out bool bWasExtracted);
                }
            }

            if( FileOrDirectoryQueries.Count == 0 )
            {
                FileOrDirectoryQueries.Add("."); // If nothing is specified use current directory
            }

            DumpBase dumper = null;

            try
            {
                switch (myCommand)
                {
                    case DumpCommands.Stats:
                        dumper = new DumpStats()
                        {
                            ETLFile = decompressedETL,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            TestCaseFilter = TestCaseFilter,
                            MachineFilter = MachineFilter,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,

                            Properties = Properties,
                            OneLine = OneLine,
                        };
                        break;
                    case DumpCommands.Versions:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        dumper = new DumpModuleVersions()
                        {
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            TestCaseFilter = TestCaseFilter,
                            MachineFilter = MachineFilter,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,
                            ProcessNameFilter = ProcessNameFilter,
                            NoCmdLine = NoCmdLine,

                            ModuleFilter = ModuleFilter,
                            DllFilter = DllFilter,
                            VersionFilter = VersionFilter,
                        };
                        break;
                    case DumpCommands.Process:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);

                        dumper = new DumpProcesses
                        {
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            TestCaseFilter = TestCaseFilter,
                            MachineFilter = MachineFilter,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,
                            ProcessNameFilter = ProcessNameFilter,
                            CommandLineFilter = CmdLineFilter,
                            UsePrettyProcessName = UsePrettyProcessName,
                            NoCmdLine = NoCmdLine,
                            SortOrder = SortOrder,
                            Merge = Merge,
                            MinMaxDurationS = MinMaxDurationS,

                            NewProcessFilter = NewProcess,
                            ShowFileOnLine = ShowFileOnLine,
                            ShowAllProcesses = ShowAllProcesses,
                            Crash = Crash,
                            ZeroTimeMode = ZeroTimeMode,
                            ZeroTimeFilter = ZeroTimeFilter,
                            ZeroTimeProcessNameFilter = ZeroTimeProcessNameFilter,
                        };
                        break;
                    case DumpCommands.CPU:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        dumper = new DumpCPUMethod
                        {
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            TestCaseFilter = TestCaseFilter,
                            MachineFilter = MachineFilter,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,
                            ProcessNameFilter = ProcessNameFilter,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,
                            NoCmdLine = NoCmdLine,
                            ProcessFormatOption = ProcessFormat,

                            Merge = Merge,
                            TopN = TopN,
                            StackTagFilter = StackTagFilter,
                            MethodFilter = MethodFilter,
                            TopNMethods = TopNMethods,
                            MinMaxCPUMs = MinMaxCPUMs,
                            MinMaxWaitMs = MinMaxWaitMs,
                            MinMaxFirstS = MinMaxFirstS,
                            MinMaxLastS = MinMaxLastS,
                            MinMaxDurationS = MinMaxDurationS,
                            MethodFormatter = new MethodFormatter(NoDll, NoArgs, MethodCutStart, MethodCutLength),
                            ThreadCount = ThreadCount,
                            FirstLastDuration = FirstLastDuration,
                            FirstTimeFormat = FirstTimeFormat,
                            LastTimeFormat = LastTimeFormat,
                            SortOrder = SortOrder,
                            ShowTotal = ShowTotal,
                            ShowDetailsOnMethodLine = ShowDetailsOnMethodLine,
                            ShowModuleInfo = ShowModuleInfo,
                            ShowDriversOnly = ShowDriversOnly,
                            ZeroTimeMode = ZeroTimeMode,
                            ZeroTimeFilter = ZeroTimeFilter,
                            ZeroTimeProcessNameFilter = ZeroTimeProcessNameFilter,
                        };
                        break;
                    case DumpCommands.Disk:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        dumper = new DumpDisk
                        {
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            TestCaseFilter = TestCaseFilter,
                            MachineFilter = MachineFilter,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,
                            ProcessNameFilter = ProcessNameFilter,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,

                            Merge = Merge,
                            DirectoryLevel = DirectoryLevel,
                            IsPerProcess = IsPerProcess,
                            FileNameFilter = FileNameFilter,
                            Min = Min,
                            Max = Max,
                            TopN = TopN,
                            TopNProcesses = TopNProcesses,
                            SortOrder = SortOrder,
                            FileOperationValue = FileOperation,
                            ReverseFileName = ReverseFileName,
                        };
                        break;
                    case DumpCommands.File:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        dumper = new DumpFile
                        {
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            TestCaseFilter = TestCaseFilter,
                            MachineFilter = MachineFilter,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,
                            ProcessNameFilter = ProcessNameFilter,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,
                            NoCmdLine = NoCmdLine,

                            ShowTotal = ShowTotal,
                            Merge = Merge,
                            DirectoryLevel = DirectoryLevel,
                            IsPerProcess = IsPerProcess,
                            FileNameFilter = FileNameFilter,
                            Min = Min,
                            Max = Max,
                            TopN = TopN,
                            TopNProcesses = TopNProcesses,
                            FileOperationValue = FileOperation,
                            SortOrder = SortOrder,
                            ShowAllFiles = ShowAllFiles,
                            ShowDetails = ShowDetails,
                            ReverseFileName = ReverseFileName,
                        };
                        break;
                    case DumpCommands.Exceptions:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        dumper = new DumpExceptions
                        {
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            TestCaseFilter = TestCaseFilter,
                            MachineFilter = MachineFilter,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,
                            ProcessNameFilter = ProcessNameFilter,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,

                            FilterExceptions = FilterExceptions,
                            TypeFilter = TypeFilter,
                            MessageFilter = MessageFilter,
                            StackFilter = StackFilter,
                            ShowFullMessage = ShowFullMessage,
                            ShowStack = ShowStack,
                            CutStackMin = CutStackMin,
                            CutStackMax = CutStackMax,
                            NoCmdLine = NoCmdLine,
                            MinMaxExTimeS = MinMaxExTimeS,
                            ZeroTimeMode = ZeroTimeMode,
                            ZeroTimeFilter = ZeroTimeFilter,
                            ZeroTimeProcessNameFilter = ZeroTimeProcessNameFilter,
                        };
                        break;
                    case DumpCommands.Memory:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        dumper = new DumpMemory
                        {
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            TestCaseFilter = TestCaseFilter,
                            MachineFilter = MachineFilter,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,
                            ProcessNameFilter = ProcessNameFilter,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,

                            TopN = TopN,
                            SortOrder = SortOrder,
                            MinDiffMB = MinDiffMB,
                            GlobalDiffMB = GlobalDiffMB,
                            TotalMemory = TotalMemory,
                            MinWorkingSetMB = MinWorkingSetMB,
                        };
                        break;
                    case DumpCommands.ThreadPool:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        dumper = new DumpThreadPool
                        {
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            TestCaseFilter = TestCaseFilter,
                            MachineFilter = MachineFilter,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,
                            ProcessNameFilter = ProcessNameFilter,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,

                            NoCmdLine = NoCmdLine,
                        };
                        break;
                    case DumpCommands.Mark:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        dumper = new DumpMarks
                        {
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            TestCaseFilter = TestCaseFilter,
                            MachineFilter = MachineFilter,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,
                            ProcessNameFilter = ProcessNameFilter,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,
                            MarkerFilter = MarkerFilter,
                            ZeroTimeMode = ZeroTimeMode,
                            ZeroTimeFilter = ZeroTimeFilter,
                        };
                        break;

                    case DumpCommands.TestRuns:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        dumper = new TestRunDumper
                        {
                            Recursive = mySearchOption,
                            Directories = FileOrDirectoryQueries,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestCaseFilter = TestCaseFilter,
                            MachineFilter = MachineFilter,
                            CopyFilesTo = CopyFilesTo,
                            WithETL = WithETL,
                            Overwrite = Overwrite,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            TimeFormatOption = TimeFormat,

                            IsVerbose = myIsVerbose,
                            PrintFiles = myPrintFiles,
                            ValidTestsOnly = ValidTestsOnly,

                        };
                        break;
                    case DumpCommands.None:
                        throw new NotSupportedException("-dump needs an argument what you want to dump.");
                    case DumpCommands.Allocations:
                        break;
                    default:
                        throw new NotSupportedException($"The dump command {myCommand} is not implemented.");
                }

                dumper.Execute();
            }
            finally
            {
                dumper?.Dispose();
            }

        }

        void ThrowIfFileOrDirectoryIsInvalid(List<string> fileOrDirectoryQueries)
        {
            if(fileOrDirectoryQueries.Count == 0)
            {
                throw new MissingInputException($"You need to specify an existing directory or file with the -filedir option.");
            }
            foreach(var query in fileOrDirectoryQueries)
            {
                if (!query.Contains("?") && !query.Contains("*"))
                {
                    if (!Directory.Exists(query) && !File.Exists(query) && !File.Exists(query + TestRun.ExtractExtension))
                    {
                        throw new ArgumentException($"\"{query}\" was not found. You need to specify an existing directory.");
                    }
                }
            }
        }
    }
}
