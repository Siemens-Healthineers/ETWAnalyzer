//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
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
using Microsoft.Windows.EventTracing.Metadata;
using System.Drawing;
using System.Numerics;
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Diagnostics.Tracing;

namespace ETWAnalyzer.Commands
{
    /// <summary>
    /// Processes all -dump xxxx commands. Constructed by <see cref="CommandFactory"/> if the arguments contain -dump.
    /// </summary>
    class DumpCommand : ArgParser
    {
        internal const string AllDumpCommands = "[CPU,Disk,Dns,Exception,File,LBR,Mark,Memory,ObjectRef,PMC,Power,Process,Stats,TestRun,ThreadPool,Version]";

        static readonly string DumpHelpStringPrefix =
        "ETWAnalyzer -Dump "+ AllDumpCommands + " [-nocolor]" + Environment.NewLine;

        static readonly string StatsHelpString =
        "   Stats    -filedir/fd x.etl/.json   [-Properties xxxx] [-recursive] [-csv xxx.csv] [-NoCSVSeparator] [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-MinMaxMsTestTimes xx-yy ...] [-Clip]" + Environment.NewLine + "" +
        "                                      [-ShowFullFileName/-sffn]" + Environment.NewLine +
        "                         ETL Only:                  Dump from an ETL file or compressed 7z file which will be uncompressed in-place ETW statistics." + Environment.NewLine +
        "                                                    This includes OS version, bitness, trace start/end and a list of all contained events and their counts and sizes of the ETL file." + Environment.NewLine +
        "                         Json Only:                 When Json files are dumped some or all extracted data is printed or exported to a CSV file. You can also filter by testcase, machine, ... to extract data of specific files" + Environment.NewLine +
        "                         -Properties xxxx           Dump only specific properties of extracted Json to console. Valid property names are " + Environment.NewLine +
       $"                                                    {DumpStats.AllProperties}" + Environment.NewLine +
        "                         -OneLine                   Print properties on console on a single line per file" + Environment.NewLine
        ;

        static readonly string VersionHelpString =
        "   Version  -filedir/fd x.etl/.json [-dll xxxx.dll] [-VersionFilter xxx] [-MissingPdb [xxx.pdb]] [-ModuleFilter xxx] [-ProcessName/pn xxx.exe(pid)] [-CmdLine *xxx*] [-NoCmdLine] [-csv xx.csv]" + Environment.NewLine +
        "                           [-Clip] [-PlainProcessNames] [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-MinMaxMsTestTimes xx-yy ...] [-ShowTotal [Total,None]] [-NewProcess 0/1/-1/-2/2]" + Environment.NewLine +
        "                           [-ShowFullFileName/-sffn] [-topn dd nn]" + Environment.NewLine +
        "                         Dump module versions of given ETL or Json. For Json files the option -extract Module All or Default must be used during extraction to get with -dll version information." + Environment.NewLine +
        "                         -dll xxx.dll              All file versions of that dll are printed. If -dll * is used all file versions are printed." + Environment.NewLine +
        "                         -topn dd [nn]             Valid when -dll ... is used. Limit output to last dd processes where optionally nn are skipped from alphabetically sorted list." + Environment.NewLine + 
        "                         -ShowTotal None           When None omit per process summary of loaded vs visible modules. When Total do not print all matching modules to console. " + Environment.NewLine +  
        "                         -MissingPdb filter        Print a filtered summary of all unresolved pdbs which could not be resolved during extraction and would lead to unresolved methods in CPU Sampling/CSwitch data." + Environment.NewLine +
        "                         -VersionFilter filter     Filter against module path and version strings. Multiple filters are separated by ;. Wildcards are * and ?. Exclusion filters start with !" + Environment.NewLine +
        "                         -ModuleFilter  filter     Extracted data from Config\\DllToBuildMapping.json. Print only version information for module. Multiple filters are separated by ;. Wildcards are * and ?. Exclusion filters start with !" + Environment.NewLine;
        static readonly string ProcessHelpString =
        "   Process  -filedir/fd x.etl/.json [-recursive] [-csv xxx.csv] [-NoCSVSeparator] [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-ProcessName/pn xxx.exe(pid)] [-Parent xxx.exe(pid)]" + Environment.NewLine +
        "            [-CmdLine *xxx*] [-Crash] [-ShowUser] [-Session dd] [-User abc] [-SortBy Tree/Time/StopTime/Default] [-ZeroTime/zt Marker/First/Last/ProcessStart filter] [-ZeroProcessName/zpn filter]" + Environment.NewLine +
        "            [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-MinMaxStart xx-yy] [-ShowFileOnLine] [-ShowAllProcesses] [-NoCmdLine] [-Details] [-Clip] [-TestsPerRun dd -SkipNTests dd] " + Environment.NewLine +
        "            [-TestRunIndex dd -TestRunCount dd] [-MinMaxMsTestTimes xx-yy ...] [-ShowFullFileName/-sffn]" + Environment.NewLine +
        "                         Print process name, pid, command line, start/stop time return code and parent process id" + Environment.NewLine +
        "                         Default: The processes are grouped by exe sorted by name and then sorted by time to allow easy checking of recurring process starts." + Environment.NewLine +
        "                         -csv xx.csv                Write output to a CSV file with ; as separator for later processing." + Environment.NewLine +
        "                                                    Dates are formatted as yyyy-MM-dd HH:mm:ss.fff For Excel use yyyy-mm-dd hh:mm:ss.000 as custom date time format string to parse it back." + Environment.NewLine +
        "                                                    On machines where the . is not the decimal point change the locale setting (Control Panel - Region - Additional Settings - Numbers - Decimal Symbol) to ." + Environment.NewLine +
        "                         -NoCSVSeparator            Skip the first line with sep=; which is there to aid Excel to detect the CSV separator character." + Environment.NewLine +
        "                         -ShowFullFileName/-sffn    Show full file name of input file" + Environment.NewLine +
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
        "                         -Parent         x;y.exe    Same as -ProcessName but it will filter for parent process names/ids. Useful with -SortBy Tree to show child processes of specific parent processes as process tree." + Environment.NewLine +
        "                         -CmdLine substring         Restrict output to processes with a matching command line substring." + Environment.NewLine +
        "                         -NewProcess 0/1/-1/-2/2    If not present all processes are dumped. " + Environment.NewLine +
        "                                                    0 All processes which have been running from trace start-end. " + Environment.NewLine +
        "                                                    1 Processes which have been started and potentially exited during the trace." + Environment.NewLine +
        "                                                   -1 Processes which have exited during the trace but have been potentially also started." + Environment.NewLine +
        "                                                    2 Processes which have been started but not stopped during the trace. " + Environment.NewLine +
        "                                                   -2 Processes which are stopped but not started during the trace." + Environment.NewLine +
        "                         -ShowUser                  Show user name und which the process was started. If extraction is done on a different machine the user sids are displayed." + Environment.NewLine +
        "                         -SortBy[Tree/Time/StopTime/Default] Sort processes by start/stop time or group by process name and then sort by start time (default)." + Environment.NewLine +
        "                                                    Tree will print the process as process tree. You can filter by parent processes with -parent and the actual processes with -pn" + Environment.NewLine +
        "                                                        In tree mode process start/stop indicators are shown as +-, but you can use -TimeFmt/-ProcessFmt to show times and duration in different formats" + Environment.NewLine + 
        "                         -PlainProcessNames         Default is to use pretty process names based on rename rules in Configuration\\ProcessRenameRules.xml. If you do not want this use this flag." + Environment.NewLine +
        "                         -NoCmdLine                 Omit process command line string in output. Default is to print the full exe with command line." + Environment.NewLine +
        "                         -Clip                      Clip printed output to console buffer width to prevent wraparound to keep output readable" + Environment.NewLine +
        "                         The following commands are specific only to dump Process" + Environment.NewLine +
        "                         -Merge                     Merge all selected Json files to calculate process lifetime across all passed Json files. This also limits the display to only started/ended processes per file." + Environment.NewLine +
        "                         -ShowAllProcesses          When -Merge is used already running processes are only printed once. If you want to know if they were still running use this flag." + Environment.NewLine +
        "                         -MinMaxDuration minS [maxS] Filter for process duration in seconds." + Environment.NewLine +
        "                         -MinMaxStart minS [maxS]   Select processes which did start after minS seconds." + Environment.NewLine +
        "                         -ShowFileOnLine            Show etl file name on each printed line." + Environment.NewLine +
        "                         -Crash                     Show potentially crashed processes with unusual return codes, or did trigger Windows Error Reporting." + Environment.NewLine +
        "                         -Details                   Show more columns" + Environment.NewLine +
        "                         -Session dd;yy             Filter processes by Windows session id. Multiple filters are separated by ;" + Environment.NewLine +
        "                                                    E.g. dd;dd2 will filter for all dd instances and dd2. The wildcards * and ? are supported for all filter strings." + Environment.NewLine +
        "                         -User abc;*xyz*            Filter user name by which the process was started. Multiple filters are separated by ;" + Environment.NewLine +
        "                                                    E.g. abc;*xyz* will filter for the user names abc and *xyz*. The wildcards * and ? are supported for all filter strings." + Environment.NewLine +
        "                         For other options [-TestsPerRun] [-SkipNTests] [-TestRunIndex] [-TestRunCount] [-MinMaxMsTestTimes]" + Environment.NewLine +
        "                         [-ShowFullFileName] refer to help of TestRun. Run \'EtwAnalyzer -help dump\' to get more infos." + Environment.NewLine;
        static readonly string TestRunHelpString =
        "   TestRun  -filedir/fd xxx [-recursive] [-verbose] [-ValidTestsOnly] [[-CopyFilesTo xxx] [-WithEtl] [-OverWrite]] [-TestRunIndex dd -TestRunCount dd] [-MinMaxTestTime xx [yy]] [-PrintFiles] [-Clip]" + Environment.NewLine +
        "                         Print for a directory which contains automated profiling data test execution counts. You can also download data to a local directory once you know which" + Environment.NewLine +
        "                         data you need by selecting a testrun by index (-TestRunIndex) and count (-TestRunCount default is all until end)." + Environment.NewLine +
        "                         -recursive                 Search below all subdirectories for test runs" + Environment.NewLine +
       @"                         -filedir/fd xxx            Can occur multiple times. xxx is an extracted json file name, directory, or a file query like C:\temp\*test*.json;!*success* which matches all files with test in C:\temp excluding success files" + Environment.NewLine +
       @"                                                    You can query multiple directories. E.g. -fd c:\temp\1 -fd c:\temp\2" + Environment.NewLine +
        "                         The following filters are only applicable to profiling data which has a fixed file naming convention" + Environment.NewLine +
        "                            -TestRunIndex dd           Select only data from a specific test run by index. To get the index value use -dump TestRun -filedir xxxx " + Environment.NewLine +
        "                            -TestRunCount dd           Select from a given TestRunIndex the next dd TestRuns. " + Environment.NewLine +
        "                            -NoTestRunGrouping         Do not group tests into TestRuns which are tests which have tests with a gap > 1h." + Environment.NewLine +
        "                            -MinMaxMsTestTimes xx-yy ... Select files based on test run time range. Multiple ranges are supported. Useful to e.g. to check fast vs slow testrun for typical test durations excluding outliers." + Environment.NewLine +
        "                            -TestsPerRun dd            Number of test cases to load of each test run. Useful if you want get an overview how a test behaves over time without loading thousands of files." + Environment.NewLine +
        "                            -SkipNTests dd             Skip the first n tests of a testcase in a TestRun. Use this to e.g. skip the first test run which shows normally first time init effects which may be not representative" + Environment.NewLine +
        "                            -CopyFilesTo xxx           Copy matching files from e.g. a test run selected by " + Environment.NewLine +
        "                            -WithEtl                   Copy also the ETL/Zip file if present" + Environment.NewLine +
        "                            -Overwrite                 Force overwrite of downloaded data." + Environment.NewLine +
        "                         -ValidTestsOnly            Only consider files which match the automated test file naming convention which include test name, duration, machine in the file name" + Environment.NewLine +
        "                         -verbose                   Print Test Duration as x" + Environment.NewLine +
        "                         -PrintFiles                Print input Json files paths into output" + Environment.NewLine;
        static readonly string CPUHelpString =
        "   CPU      -filedir/fd Extract\\ or xxx.json [-recursive] [-csv xxx.csv] [-NoCSVSeparator] [-ProcessFmt timefmt] [-Methods method1;method2...] [-FirstLastDuration/fld [firsttimefmt] [lasttimefmt]] [-MinMaxCSwitchCount xx-yy] [-MinMaxReadyAvgus xx-yy]" + Environment.NewLine +
        "            [-ThreadCount] [-SortBy [CPU/Wait/CPUWait/CPUWaitReady/ReadyAvg/CSwitchCount/StackDepth/First/Last/TestTime/StartTime] [-StackTags tag1;tag2] [-CutMethod xx-yy] [-ShowOnMethod] [-ShowModuleInfo [Driver] or [filter]] [-NoCmdLine] [-Clip]" + Environment.NewLine +
        "            [-Details [-NoFrequency] [-Normalize]] [-NoReady] [-ShowTotal Total, Process, Method] [-topn dd nn] [-topNMethods dd nn] [-ZeroTime/zt Marker/First/Last/ProcessStart filter] [-ZeroProcessName/zpn filter] " + Environment.NewLine +
        "            [-includeDll] [-includeArgs] [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-MinMaxMsTestTimes xx-yy ...] [-ProcessName/pn xxx.exe(pid)] [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "            [-ShowFullFileName/-sffn]" + Environment.NewLine +
        "                         Print CPU, Wait and Ready duration of selected methods of one extracted Json or a directory of Json files. To get output -extract CPU, All or Default must have been used during extraction." + Environment.NewLine +
        "                         The numbers for a method are method inclusive times (based on CPU Sampling (CPU) and Context Switch (Wait) data)." + Environment.NewLine +
        "                         CPU   is the method inclusive time summed across all threads. E.g. Main is always the most expensive method but CPU is consumed by the called methods." + Environment.NewLine +
        "                         Wait  is the method inclusive time a method was waiting for a blocking OS call e.g. ReadFile, OpenFile, ... to return. It is the sum of all threads, but overlapping times of multiple threads are counted only once." + Environment.NewLine +
        "                         Ready is the method inclusive time the thread was waiting for a CPU to become free due to CPU oversubscription. It is the sum of all threads, but overlapping times of multiple threads are counted only once." + Environment.NewLine +
        "                         -ShowTotal xxx             Print totals of all selected methods/stacktags. xxx can be Process, Method or Total. " + Environment.NewLine +
        "                                                    Total:   Print only file name and totals. Files are sorted by highest totals." + Environment.NewLine +
        "                                                    Process: Print file and process totals. Processes are sorted by highest totals inside a file." + Environment.NewLine +
        "                                                    Method:  Print additionally the selected methods which were used for total calculation." + Environment.NewLine +
        "                                                    Warning: The input values are method inclusive times summed across all threads in a process." + Environment.NewLine +
        "                                                             You should filter for specific independent methods/stacktags which are not already included to get meaningful results." + Environment.NewLine +
        "                         -ShowOnMethod              Display process name besides method name without the command line. This allows to see trends in CPU changes over time for a specific method in console output better." + Environment.NewLine +
        "                         -ShowModuleInfo/smi [Driver] or [filter] Show exe version or show dll version of each matching method until another dll is show in the printed list. When Driver is specified only module infos of well" + Environment.NewLine +
        "                                                    known AV and Filter drivers are printed (or written to CSV output). [filter] e.g. *Defender* will match on parts of module (version, name, directory, description)." + Environment.NewLine + "" +
        "                         -MinMaxFirst minS [maxS]   Include methods/stacktags which match the first occurrence in [min, max] in seconds. You can shift time with -ZeroTime. " + Environment.NewLine +
        "                                                    E.g. \"-MinMaxFirst 0 -ZeroTime First Click\" will show all methods after Click." + Environment.NewLine +
        "                         -MinMaxLast  minS [maxS]   Include methods/stacktags which match the last occurrence in [min max] in seconds." + Environment.NewLine +
        "                         -MinMaxDuration min [maxS] Include methods/stacktags which have a range of first/last occurrence if [min max] in seconds. This value is ZeroTime independent." + Environment.NewLine +
        "                         -FirstLastDuration/fld [[first] [lastfmt]]   Show time in s where a stack sample was found the first and last time in this trace. Useful to estimate async method runtime or to correlate times in WPA." + Environment.NewLine +
        "                                                    The options first and lastfmt print, when present, the first and/or last time the method did show up in profiling data. Affects also time format in -CSV output (default is s)." + Environment.NewLine +
        "                         -ZeroTime/zt               Shift first/last method time. This also affects -csv output. Useful to see method timings relative to the first occurrence of e.g. method OnClick." + Environment.NewLine +
        "                             Marker filter          Zero is an ETW marker event defined by filter." + Environment.NewLine +
        "                             First  filter          Select the first occurrence of a method/stacktag as zero time point. If the filter is ambiguous consider to refine the filter or add -ZeroProcessName to limit it to a specific process." + Environment.NewLine +
        "                             Last   filter          Select the last occurrence of a method/stacktag as zero time point." + Environment.NewLine +
        "                             ProcessStart/ProcessEnd [CmdLine] Select process start/stop event as zero point which matches the optional CmdLine filter string and the -ZeroProcessName filter." + Environment.NewLine +
        "                         -ZeroProcessName/zpn x.exe Select the process from which the zero time point will be used for ProcessStart/First/Last Method zero point definition." + Environment.NewLine +
        "                         -CutMethod xx-yy           Shorten method/stacktag name to make output more readable. Skip xx chars and take yy chars. If -yy is present the last yy characters are taken." + Environment.NewLine +
        "                         -includeDll/id             Include the declaring dll name in the full method name like xxx.dll!MethodName" + Environment.NewLine +
        "                         -includeArgs/ia            Include the full method prototype when present like MethodName(string arg1, string arg2, ...)" + Environment.NewLine +
        "                         -Methods *Func1*;xxx.dll!FullMethodName   Dump one or more methods from all or selected processes. When omitted only CPU totals of the process and command line are printed to give an overview." + Environment.NewLine +
        "                         -StackTags *tag1;Tag2*     Use * to dump all. Dump one or more stacktags from all or selected processes." + Environment.NewLine +
        "                         -topN dd nn                Include only first dd processes with most CPU in trace. Optional nn skips the first nn lines. To see e.g. Lines 20-30 use -topn 10 20" + Environment.NewLine +
        "                         -topNMethods dd nn         Include dd most expensive methods/stacktags which consume most CPU in trace. Optional nn skips the first nn lines." + Environment.NewLine +
        "                         -ThreadCount               Show # of unique threads that did execute that method." + Environment.NewLine +
        "                         -ProcessFmt timefmt        Add besides process name start/stop time and duration. See -TimeFmt for available options." + Environment.NewLine +
        "                         -SortBy [CPU/Wait/CPUWait/CPUWaitReady/ReadyAvg/CSwitchCount/StackDepth/First/Last/Priority/TestTime/StartTime] Default method sort order is CPU consumption. Wait sorts by wait time, First/Last sorts by first/last occurrence of method/stacktags." + Environment.NewLine +
        "                                                    StackDepth shows hottest methods which consume most CPU but are deepest in the call stack." + Environment.NewLine +
        "                                                    StartTime sorts by process start time to correlate things in the order the processes were started." + Environment.NewLine +   
        "                                                    TestTime can be used to force sort order of files by test time when -ShowTotal is used. When totals are enabled the files are sorted by highest totals." + Environment.NewLine +
        "                                                    Sorting by process Priority is only applicable when you sort CPU totals without methods. " + Environment.NewLine + 
        "                         -MinMaxCSwitchCount xx-yy or xx  Filter by context switch count." + Environment.NewLine +
        "                         -MinMaxReadyAvgus xx-yy    Filter by Ready Average time in us." + Environment.NewLine +
        "                         -MinMaxReadyMs xx-yy or xx Only include methods (stacktags have no recorded ready times) with a minimum ready time of [xx, yy] ms." + Environment.NewLine +
        "                         -MinMaxCpuMs xx-yy or xx   Only include methods/stacktags with a minimum CPU consumption of [xx,yy] ms." + Environment.NewLine +
        "                         -MinMaxWaitMs xx-yy or xx  Only include methods/stacktags with a minimum wait time of [xx,yy] ms." + Environment.NewLine +
        "                         -Details                   Show additionally Session Id, Ready Average time, Context Switch Count, average CPU frequency per CPU efficiency class and ready percentiles." + Environment.NewLine +
        "                           -Normalize               Normalize CPU time to 100% of CPU frequency. Enables comparison of CPU time independant of the used power profile." + Environment.NewLine +
        "                           -NoFrequency             When -Details is present do not print P/E core CPU usage and average frequency." + Environment.NewLine +
        "                         -NoPriority                Omit process Priority in total cpu mode and when methods are printed in -Details mode." + Environment.NewLine +   
        "                         -NoReady                   Do not print Ready time, average or percentiles (when -Details is used) per method." + Environment.NewLine +
        "                         -Session dd;yy             Filter processes by Windows session id. Multiple filters are separated by ;" + Environment.NewLine +
        "                                                    E.g. dd;dd2 will filter for all dd instances and dd2. The wildcards * and ? are supported for all filter strings." + Environment.NewLine +
        "                         For other options [-recursive] [-csv] [-NoCSVSeparator] [-TimeFmt] [-TestsPerRun] [-SkipNTests] [-TestRunIndex] [-TestRunCount] [-MinMaxMsTestTimes] [-ProcessName/pn] [-NewProcess] [-CmdLine]" + Environment.NewLine +
        "                         [-ShowFullFileName] refer to help of TestRun and Process. Run \'EtwAnalyzer -help dump\' to get more infos." + Environment.NewLine;

        static readonly string MemoryHelpString =
        "  Memory    -filedir/fd Extract\\ or xxx.json [-recursive] [-csv xxx.csv] [-NoCSVSeparator] [-TopN dd nn] [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-ProcessFmt timefmt] [-TotalMemory] [-MinDiffMB dd] " + Environment.NewLine +
        "                           [-SortBy Commit/WorkingSet/SharedCommit/Diff] [-GlobalDiffMB dd] [-MinMaxWorkingSetMiB xx-yy] [-MinMaxWorkingSetPrivateMiB xx-yy] [-MinMaxCommitMiB xx-yy] [-MinMaxSharedCommitMiB xx-yy] [-Clip] [-NoCmdLine] [-Details] " + Environment.NewLine +
        "                           [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-MinMaxMsTestTimes xx-yy ...] [-ProcessName/pn xxx.exe(pid)] [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                           [-ShowFullFileName/-sffn] [-ShowModuleInfo [Driver] or [filter]] [-ShowTotal [File,None]] [-ProcessFmt timefmt] " + Environment.NewLine +
        "                         Print memory (Working Set, Committed Memory) of all or some processes from extracted Json files. To get output -extract Memory, All or Default must have been used during extraction." + Environment.NewLine +
        "                         -ShowTotal [File, None, Process, Total]      Show totals per file. Default is Process. None will turn off totals. File mode will turn off the processes." + Environment.NewLine +
        "                         -SortBy Commit/SharedCommit Sort by Committed/Shared Committed (this is are memory mapped files, or page file allocated file mappings). " + Environment.NewLine + "" +
        "                                 WorkingSet/Diff    Sort by working set or committed memory difference" + Environment.NewLine +
        "                         -TopN dd nn                Select top dd processes. Optional nn skips the first nn lines of top list" + Environment.NewLine +
        "                         -TotalMemory               Show System wide commit and active memory metrics. Useful to check if machine was in a bad memory situation." + Environment.NewLine +
        "                         -MinMaxWorkingSetMiB xx-yy  Only include processes which had at least a working set xx-yy MiB (=1024*1024) at trace end. Numbers can have units like Bytes,KiB,MiB,GiB e.g. 500MiB." + Environment.NewLine +
        "                         -MinMaxWorkingSetPrivateMiB xx-yy  Only include processes which had at least a working set private xx-yy MiB (=1024*1024) at trace end. Numbers can have units like Bytes,KiB,MiB,GiB e.g. 500MiB." + Environment.NewLine +
        "                         -MinMaxCommitMiB xx-yy      Only include processes which had at last committed xx-yy MiB at trace end." + Environment.NewLine + 
        "                         -MinMaxSharedCommitMiB xx-yy Only include processes which had at least a shared commit of xx-yy MiB at trace end." + Environment.NewLine + 
        "                         -MinDiffMB    dd           Include processes which have gained inside one Json file more than xx MB of committed memory." + Environment.NewLine +
        "                         -GlobalDiffMB dd           Same as before but the diff is calculated across all incuded Json files." + Environment.NewLine +
        "                         -Details                   Show more columns" + Environment.NewLine +
        "                         -Session dd;yy             Filter processes by Windows session id. Multiple filters are separated by ;" + Environment.NewLine +
        "                                                    E.g. dd;dd2 will filter for all dd instances and dd2. The wildcards * and ? are supported for all filter strings." + Environment.NewLine +
        "                         For other options [-recursive] [-csv] [-NoCSVSeparator] [-TimeFmt] [-NoCmdLine] [-TestsPerRun] [-SkipNTests] [-TestRunIndex] [-TestRunCount] [-MinMaxMsTestTimes] [-ProcessName/pn] [-NewProcess] [-CmdLine]" + Environment.NewLine +
        "                         [-ShowFullFileName] refer to help of TestRun, Process and CPU (-ProcessFmt, -ShowModuleInfo). Run \'EtwAnalyzer -help dump\' to get more infos." + Environment.NewLine;
        static readonly string ExceptionHelpString =
        "  Exception -filedir/fd Extract\\ or xxx.json [-Type xxx] [-Message xxx] [-Showstack] [-MaxMessage dd] [-CutStack dd-yy] [-Stackfilter xxx] [-recursive] [-csv xxx.csv] [-NoCSVSeparator] " + Environment.NewLine +
        "                           [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-ProcessFmt timefmt] [-NoCmdLine] [-Clip] [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-MinMaxMsTestTimes xx-yy ...]" + Environment.NewLine +
        "                           [-MinMaxExTime minS [maxS]] [-ZeroTime/zt Marker/First/Last/ProcessStart filter] [-ZeroProcessName/zpn filter]" + Environment.NewLine +
        "                           [-ProcessName/pn xxx.exe(pid)] [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                           [-ShowFullFileName/-sffn] [-ShowModuleInfo [filter]] [-Details]" + Environment.NewLine +
        "                         Print Managed Exceptions from extracted Json file. To get output -extract Exception, All or Default must have been used during extraction." + Environment.NewLine +
        "                         Before each message the number how often that exception was thrown is printed. That number also includes rethrows in finally blocks which leads to higher numbers as one might expect!" + Environment.NewLine +
        "                         When a filter (type,message or stack) is used then the exception throw times are also printed." + Environment.NewLine +
        "                         -Type *type1*;*type2*      Filter Exception by type e.g. *timeoutexception*. Multiple filters can be combined with ;" + Environment.NewLine +
        "                         -Message *msg1*;*msg2*     Filter Exception by message e.g. *denied*" + Environment.NewLine +
        "                         -StackFilter *f1*;*f2*     Filter Exception by a stack substring of the stacktrace string" + Environment.NewLine +
        "                         -MinMaxExTime minS [maxS]  Filter by exception time in s since trace start. Use -Timefmt s to print time in this format." + Environment.NewLine +
        "                         -ShowStack                 Show Stacktrace for every exception. By default the first 50 frames are displayed. " + Environment.NewLine +
        "                                                    To change use -CutStack. You should filter first as much as possible before using this on the console." + Environment.NewLine +
        "                                                    Only when -type, -message or -stackfilter are active the stack is printed to console." + Environment.NewLine +
       $"                         -MaxMessage dd             Limit exception message to first dd characters. By default the first {MaxMessageLength} characters are printed. Use -MaxMessage 0 to show full text." + Environment.NewLine +
        "                         -CutStack dd-yy            Remove the first dd lines of the stack. To display all stack frames use \"-CutStack 0-\". Print yy lines or all if -yy is omitted." + Environment.NewLine +
        "                                                    E.g. -CutStack -50 will display the first 50 lines of a stack trace." + Environment.NewLine +
        "                         -SortBy [Time / Default]   Sorts exceptions by time or use default grouping." + Environment.NewLine +
        "                         -ShowTime                  Show the time of exception when a -Type filter is active. Time format is controlled by -TimeFmt flag. By default no time is printed." + Environment.NewLine +
        "                         -Details                   Show more columns" + Environment.NewLine +
        "                         -Session dd;yy             Filter processes by Windows session id. Multiple filters are separated by ;" + Environment.NewLine +
        "                                                    E.g. dd;dd2 will filter for all dd instances and dd2. The wildcards * and ? are supported for all filter strings." + Environment.NewLine +
        "                         For other options [-ZeroTime ..] [-recursive] [-csv] [-NoCSVSeparator] [-TimeFmt] [-NoCmdLine] [-TestsPerRun] [-SkipNTests] [-TestRunIndex] [-TestRunCount] [-MinMaxMsTestTimes] [-ProcessName/pn] " + Environment.NewLine +
        "                         [-NewProcess] [-CmdLine] [-ShowFullFileName] refer to help of TestRun, Process and CPU (-ProcessFmt, -ShowModuleInfo).  Run \'EtwAnalyzer -help dump\' to get more infos." + Environment.NewLine;

        static readonly string DiskHelpString =
        "  Disk -filedir/fd Extract\\ or xxx.json [-DirLevel dd] [-PerProcess] [-filename *C:*] [-MinMax[Read/Write/Total][Size/Time] xx-yy] [-TopN dd nn] [-SortBy order] [-FileOperation op] [-ReverseFileName/rfn] [-Merge] [-recursive] [-csv xxx.csv] [-NoCSVSeparator]" + Environment.NewLine +
        "                         [-TopNProcesses dd nn] [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-Clip] [-TestsPerRun dd - SkipNTests dd] [-TestRunIndex dd - TestRunCount dd] [-MinMaxMsTestTimes xx-yy ...] [-ProcessName/pn xxx.exe(pid)]" + Environment.NewLine +
        "                         [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                         [-ShowFullFileName/-sffn]" + Environment.NewLine +
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
        "                         -MinMax[Read/Write/Total][Size/Time] xx-yy Filter column wise for corresponding data. You can add units for size B,MB,MiB,GB,GiB,TB and time s,seconds,ms,us,ns. E.g. -MinMaxReadSize 100MB-500MB. Fractions use . as decimal separator." + Environment.NewLine +
        "                         -ReverseFileName/rfn       Reverse file name. Useful with -Clip to keep output clean (no console wraparound regardless how long the file name is)." + Environment.NewLine +
        "                         -Merge                     Merge all selected Json files into one summary output. Useful to get a merged view of a session consisting of multiple ETL files." + Environment.NewLine +
        "                         For other options [-recursive] [-csv] [-NoCSVSeparator] [-TimeFmt] [-TestsPerRun] [-SkipNTests] [-TestRunIndex] [-TestRunCount] [-MinMaxMsTestTimes] [-ProcessName/pn] [-NewProcess] [-CmdLine]" + Environment.NewLine +
        "                         [-ShowFullFileName] refer to help of TestRun and Process. Run \'EtwAnalyzer -help dump\' to get more infos." + Environment.NewLine;

        static readonly string FileHelpString =
        "  File -filedir/fd Extract\\ or xxx.json [-DirLevel dd] [-PerProcess] [-filename *C:*] [-ShowTotal [Total/Process/File/None]] [-TopN dd nn] [-SortBy order] [-FileOperation op] [-ReverseFileName/rfn] [-Merge] [-Details] [-recursive] " + Environment.NewLine +
        "                         [-TopNProcesses dd nn] [-csv xxx.csv] [-NoCSVSeparator] [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-ProcessFmt timefmt] [-Clip] [-TestsPerRun dd -SkipNTests dd] " + Environment.NewLine +
        "                         [-TestRunIndex dd -TestRunCount dd] [-MinMaxMsTestTimes xx-yy ...] [-ProcessName/pn xxx.exe(pid)] [-NoCmdLine] [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                         [-ShowFullFileName/-sffn] [-MinMax[Read/Write/Total][Size/Time] xx-yy] [-MinMaxTotalCount xx-yy] [-ShowModuleInfo [filter]]" + Environment.NewLine +
        "                         Print File IO metrics to console or to a CSV file if -csv is used. To get output -extract File, All or Default must have been used during extraction." + Environment.NewLine +
        "                         The extracted data is an exact summary per file and process. Unlike Disk IO, File IO tracing captures all file accesses regardless if the data was e.g. read from disk or file system cache." + Environment.NewLine +
        "                         -DirLevel dd               Print File IO per directory up to n levels. Default is 0 which shows summary per drive. -Dirlevel 100 will give a per file summary." + Environment.NewLine +
        "                         -PerProcess                Print File IO per process. If you use -processname as filter you can restrict IO to all files where the process was involved. " + Environment.NewLine +
        "                         -TopNProcesses dd nn       Select top dd (skip nn) processes when -PerProcess is enabled." + Environment.NewLine +
        "                         -FileName *C:*             Filter IO for specific files only. Multiple filters are separated by ;" + Environment.NewLine +
        "                         -FileOperation op          Filter files for specific operations." + Environment.NewLine +
        "                                                    Possible values are " + String.Join(",", Enum.GetNames(typeof(Extract.FileIO.FileIOStatistics.FileOperation))) + Environment.NewLine +
        "                         -SortBy order              Console Output Only. Valid values are: Count (Open+Close+Read+Write+SetSecurity),ReadSize,WriteSize,ReadTime,WriteTime,Length,TotalSize; TotalTime = Time (= Open+Close+Read+Write); OpenCloseTime (= Open+Close).  Default is TotalTime." + Environment.NewLine +
        "                                                    Depending on sort order a dynamic column is added to show the values by which the sort is performed." + Environment.NewLine + 
        "                         -TopN dd nn                Select top dd files based on current sort order." + Environment.NewLine +
        "                         -MinMax[Read/Write/Total][Size/Time] and MinMaxTotalCount xx-yy Filter column wise for corresponding data. You can add units for size: B,MB,MiB,GB,GiB,TB, time: s,seconds,ms,us,ns, count does not require any units." + Environment.NewLine + 
        "                                                    E.g. -MinMaxReadSize 100MB-500MB. Fractions use . as decimal separator." + Environment.NewLine +
        "                         -Details                   Show more columns" + Environment.NewLine +
        "                         -Session dd;yy             Filter processes by Windows session id. Multiple filters are separated by ;" + Environment.NewLine +
        "                         -ReverseFileName/rfn       Reverse file name. Useful with -Clip to keep output clean (no console wraparound regardless how long the file name is)." + Environment.NewLine +
        "                         -Merge                     Merge all selected Json files into one summary output. Useful to get a merged view of a session consisting of multiple ETL files." + Environment.NewLine +
        "                         -ShowTotal [Total/Process/File/None] Show totals for the complete File/per process but skip aggregated directory metrics/per process but show also original aggregated directory metrics. None will turn off totals." + Environment.NewLine +
        "                         For other options [-recursive] [-csv] [-NoCSVSeparator] [-NoCmdLine] [-TimeFmt] [-TestsPerRun] [-SkipNTests] [-TestRunIndex] [-TestRunCount] [-MinMaxMsTestTimes] [-ProcessName/pn] [-NewProcess] [-CmdLine]" + Environment.NewLine +
        "                         [-ShowFullFileName] refer to help of TestRun, Process and CPU (-ProcessFmt). Run \'EtwAnalyzer -help dump\' to get more infos." + Environment.NewLine;

        static readonly string PowerHelpString =
        "  Power -filedir/fd Extract\\ or xxx.json [-Details] [-Diff] [-recursive] [-csv xxx.csv] [-NoCSVSeparator] [-Clip] " + Environment.NewLine +
        "        [-TestsPerRun dd - SkipNTests dd][-TestRunIndex dd - TestRunCount dd] [-MinMaxMsTestTimes xx-yy ...] " + Environment.NewLine +
        "                         Print Power profile CPU settings of one or several extracted files to Console." + Environment.NewLine +
        "                         TraceProcessing can currently parse only 37/75 CPU power settings (38 are missing). See Documentation for full details." + Environment.NewLine +
        "                         -Details      Print help text for all shown CPU power settings along with the values." + Environment.NewLine +
        "                         -Diff         Group files by power settings and print only one file of each group of files which have the same power settings. Only properties which are not identical in the remaining set of files are printed." + Environment.NewLine +
        "                                       This way you can e.g. visualize the differences between Power Saver, Balanced and High Performance power plans if you record an ETL file while each of these profiles were active." + Environment.NewLine + 
        "";

        static readonly string ThreadPoolHelpString =
        "  ThreadPool -filedir/fd Extract\\ or xxx.json [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-recursive] [-csv xxx.csv] [-NoCSVSeparator] [-NoCmdLine] [-Clip] " + Environment.NewLine +
        "              [-TestsPerRun dd - SkipNTests dd][-TestRunIndex dd - TestRunCount dd] [-MinMaxMsTestTimes xx-yy ...] [-ProcessName / pn xxxx; yyy] [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                         Print Threadpool Starvation incidents. To get output -extract ThreadPoool or All must have been used during extraction. " + Environment.NewLine +
        "                         During recording the Microsoft-Windows-DotNETRuntime ETW provider with Keyword ThreadingKeyword (0x10000) must have been enabled. " + Environment.NewLine +
        "                         -NoCmdLine                 Do not print command line arguments in process name at console output" + Environment.NewLine;

        static readonly string MarkHelpString =
        "  Mark -filedir/fd Extract\\ or xxx.json [-MarkerFilter xxx] [-ZeroTime marker filter] [-MinMaxMarkDiffTime min [max]] [-recursive] [-csv xxx.csv] [-NoCSVSeparator] [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-NoCmdLine] [-Clip] " + Environment.NewLine +
        "       [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-MinMaxMsTestTimes xx-yy ...] [-ProcessName/pn xxx.exe(pid)] [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                         Print ETW Marker events" + Environment.NewLine +
        "                         -MarkerFilter xxx          Filter for specific marker events. Multiple filters are separated by ; Exclusion filters start with ! Supported wildcards are * and ?" + Environment.NewLine +
        "                         -MinMaxMarkDiffTime min [max]  Filter all marker events where the Mark Diff time is within the defined time range in seconds." + Environment.NewLine +
        "                         -ZeroTime marker filter    Print diff time relative to a specific marker. The first matching marker (defined by filter) defines the zero time." + Environment.NewLine;

        static readonly string PMCHelpString =
        "  PMC -filedir/fd Extract\\ or xxx.json [-NoCounters] [-recursive] [-csv xxx.csv] [-NoCSVSeparator] [-NoCmdLine] [-Clip] " + Environment.NewLine +
        "       [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-MinMaxMsTestTimes xx-yy ...] [-ProcessName/pn xxx.exe(pid)] [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                         Print CPU PMC (Performance Monitoring Counters. To see data you need to record PMC data with ETW in counting mode together with Context Switch events. Sampling counters are not supported yet." + Environment.NewLine +
        "                         -NoCounters                Do not display raw counter values. Just CPI and CacheMiss % are shown." + Environment.NewLine;

        static readonly string LBRHelpString =
        "  LBR -filedir/fd Extract\\ or xxx.json [-recursive] [-csv xxx.csv] [-NoCSVSeparator] [-NoCmdLine] [-Clip] " + Environment.NewLine +
        "       [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-MinMaxMsTestTimes xx-yy ...] [-ProcessName/pn xxx.exe(pid)] [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                         Print CPU LBR (Last Branch Record CPU data). This gives you a sampled method call estimate. To see data you need to record LBR data with ETW." + Environment.NewLine +
        "                         -ShowCaller                Show callee/caller of LBR Traces." + Environment.NewLine +
        "                         -ScalingFactor dd          Multiply recorded samples call counts with dd to get a better estimate of the true call counts. Based on experiments 1kHz CPU sampling the factor is in the region 1000-10000." + Environment.NewLine +
        "                         -MinMaxCount xx-yy         Only include lines which are in the count range. This filter also applies to caller methods." + Environment.NewLine +
        "                         -CutMethod xx-yy           Shorten method/stacktag name to make output more readable. Skip xx chars and take yy chars. If -yy is present the last yy characters are taken." + Environment.NewLine +
        "                         -includeDll/id             Include the declaring dll name in the full method name like xxx.dll!MethodName" + Environment.NewLine +
        "                         -includeArgs/ia            Include the full method prototype when present like MethodName(string arg1, string arg2, ...)" + Environment.NewLine +
        "                         -topN dd nn                Include only first dd processes with highest call count in trace. Optional nn skips the first nn lines. To see e.g. Lines 20-30 use -topn 10 20" + Environment.NewLine +
        "                         -topNMethods dd nn         Include methods which were called most often in trace. Optional nn skips the first nn lines." + Environment.NewLine +
        "                         -Methods *Func1*;xxx.dll!FullMethodName   Dump one or more methods from all or selected processes. When omitted only process total method call is printed to give an overview." + Environment.NewLine;

        static readonly string DnsHelpString =
        "  Dns -filedir/fd Extract\\ or xxx.json [-DnsQueryFilter xxx] [-Details] [-ShowProcess] [-ShowAdapter] [-ShowReturnCode] [-TopN dd nn] [-TopNDetails dd nn] [-SortBy Time/Count] [-MinMaxTotalTimeMs min [max]] [-MinMaxTimeMs min [max]] [-recursive] " + Environment.NewLine +
        "       [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-csv xxx.csv] [-NoCSVSeparator] [-NoCmdLine] [-Clip] [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-MinMaxMsTestTimes xx-yy ...] [-ProcessName/pn xxx.exe(pid)] " + Environment.NewLine +
        "       [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                         Print Dns summary and delay metrics. To see data you need to enable the Microsoft-Windows-DNS-Client ETW provider" + Environment.NewLine +
        "                         -Details                   Display time, duration, process, resolved IP of every Dns request." + Environment.NewLine +
        "                         -TopNDetails dd nn         Limit detail list to dd elements per DNS Query." + Environment.NewLine + 
        "                         -ShowAdapter               Show which network adapters were used to query Dns." + Environment.NewLine +
        "                         -ShowReturnCode            Show Dns API Win32 return code/s. Success and InvalidParameter are not shown." + Environment.NewLine +
        "                         -ShowProcess               Show for each Dns query the calling process/es in the lines above." + Environment.NewLine +
        "                         -TopN dd nn                Show only the queries with dd highest Dns time/count. Optional nn skips the first nn lines." + Environment.NewLine +
        "                         -SortBy [Time/Count]       Default sort order is total Dns query time. The other option is to sort Dns queries by count." + Environment.NewLine +
        "                         -MinMaxTotalTimeMs min [max] Filter displayed list of all summed query times by total Dns query time in ms." + Environment.NewLine +
        "                         -MinMaxTimeMs min [max]    Filter each Dns query duration before it is summed up. To e.g. count all queries which were slower than e.g. 20 ms add -MinMaxTimeMs 20." + Environment.NewLine +
        "                         -DnsQueryFilter xxx        Filter by host name. Multiple filters are separated by ;" + Environment.NewLine;

        static readonly string TcpHelpString =
        "  Tcp -filedir/fd Extract\\ or xxx.json [-IpPort xxx] [-ShowRetransmits]  [-TopN dd nn] [-SortBy ReceivedCount/SentCount/ReceivedSize/SentSize/TotalCount/TotalSize/ConnectTime/DisconnectTime/RetransmissionCount/RetransmissionTime/MaxRetransmissionTime]   " + Environment.NewLine +
        "       [-SortRetransmitBy Delay/Time] [-MinMaxRetransDelayMs xx-yy] [-MinMaxRetransBytes xx-yy] [-MinMaxRetransCount xx-yy] [-MinMaxSentBytes xx-yy] [-MinMaxReceivedBytes xx-yy] [-TopNRetrans dd nn] [-OnlyClientRetransmit] [-Details] [-Tcb 0xdddddd] " + Environment.NewLine + 
        "       [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-csv xxx.csv] [-NoCSVSeparator] [-NoCmdLine] [-Clip] [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-MinMaxMsTestTimes xx-yy ...] [-ProcessName/pn xxx.exe(pid)] " + Environment.NewLine +
        "       [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring] [-recursive] [-ZeroTime/zt Marker/First/Last/ProcessStart filter] [-ZeroProcessName/zpn filter] [-ShowTotal [File/None]] [-ProcessFmt timefmt] " + Environment.NewLine +
        "                         Print TCP summary and retransmit metrics. To see data you need to enable the Microsoft-Windows-TCPIP ETW provider. Data is sorted by retransmission count by default." + Environment.NewLine +
        "                         It can detect send retransmissions and duplicate received packets which show up as client retransmission events." + Environment.NewLine + 
        "                         -IpPort xxx                Filter for substrings in source/destination IP and port." + Environment.NewLine +
        "                         -ShowTotal [File,None]     Show totals per file. Default is File. None will turn off totals." + Environment.NewLine +
        "                         -TopN dd nn                Show top n connection by current sort order" + Environment.NewLine +
        "                         -TopNRetrans dd nn         Show top n retransmission events when -ShowRetransmit is used" + Environment.NewLine +
        "                         -SortBy [...]              Default sort order is total bytes. Valid sort orders are ReceivedCount/SentCount/ReceivedSize/SentSize/TotalCount/TotalSize/ConnectTime/DisconnectTime/RetransmissionCount/RetransmissionTime/MaxRetransmissionTime" + Environment.NewLine +
        "                                                    Sort applies to totals per connection. RetransmissionTime is the sum of all Delays. MaxRetransmissionTime sorts connections by highest max retransmission delay." + Environment.NewLine + 
        "                         -SortRetransmitBy [...]    When -ShowRetransmit is used the events are sorted by Time. Valid values are Time/Delay" + Environment.NewLine + 
        "                         -ShowRetransmit            Show single retransmission events with timing data. Use -timefmt s to convert time to WPA time. Use this or -Details to get all events into a csv file." + Environment.NewLine + 
        "                         -OnlyClientRetransmit      Only show client retransmissions which are visible by duplicate received packets with a payload > 1 bytes." + Environment.NewLine + 
        "                         -MinMaxRetransDelayMs xx-yy Filter by retransmission delay in ms. By default all retransmissions are shown." + Environment.NewLine +
        "                         -MinMaxRetransBytes xx-yy  Filter every retransmission event by retransmission size (sent/received) in bytes. Default is > 1 bytes because 0 and 1 byte packets are often just ACKs or timer based ping packets." + Environment.NewLine +
        "                         -MinMaxRetransCount xx-yy  Show only connections which have at least xx retransmission events" + Environment.NewLine + 
        "                         -MinMaxSentBytes xx-yy     Filter connections which have sent at least xx bytes." + Environment.NewLine + 
        "                         -MinMaxReceivedBytes xx-yy Filter connections which have received at least xx bytes." + Environment.NewLine +
        "                         -MinMaxConnectionDurationS xx-yy Filter connections which have duration of at least xx-yy seconds." + Environment.NewLine +
        "                         -Details                   Show retransmit Max/Median/Min, connect/disconnect time, used TCP template setting, TCB pointer." + Environment.NewLine +
        "                         -Tcb 0xdddddd              Filter by \"connection\" which is actually the Transfer Control Block pointer. Its value can be reused for new connections."+ Environment.NewLine 
        ;

        static readonly string ObjectRefHelpString =
        " ObjectRef  -filedir/fd Extract\\ or xxx.json" + Environment.NewLine +
        "       [-TimeFmt s,Local,LocalTime,UTC,UTCTime,Here,HereTime] [-csv xxx.csv] [-NoCSVSeparator] [-NoCmdLine] [-Clip] [-TestsPerRun dd -SkipNTests dd] [-TestRunIndex dd -TestRunCount dd] [-MinMaxMsTestTimes xx-yy ...] [-ProcessName/pn xxx.exe(pid)] " + Environment.NewLine +
        "       [-RelatedProcess xxx.exe(pid)] [-MinMaxDuration minS [maxS]] [-MinMaxId min [max]] [-CreateStack filter] [-DestroyStack filter] [-StackFilter filter] [-Object filter] [-ObjectName filter] [-Handle filter] [-ShowRef]" + Environment.NewLine +
        "       [-ShowStack] [-Type filter] [-Leak] [-MultiProcess] [-Map [0,1]] [-PtrInMap 0x...] [-MinMaxMapSize min [max]] [-Overlapped] [-Showtotal Total,File,None]" + Environment.NewLine +   
        "       [-NewProcess 0/1/-1/-2/2] [-PlainProcessNames] [-CmdLine substring]" + Environment.NewLine +
        "                        -ProcessName/pn xxx.exe(pid) Filter for processes which did create the object." + Environment.NewLine +
        "                        -RelatedProcess xxx.exe(pid) Filter in all events for this process. You can also use a negative filter to exclude specific processes like -pn *creator.exe -realatedprocess !other.exe" + Environment.NewLine +    
        "                        -MinMaxDuration minS [maxS]  Filter for handle lifetime. Never closed handles get a lifetime of 9999 s which serves as magic marker value." + Environment.NewLine +
        "                        -MinMaxId min [max]          Filter for one or a range of objects. E.g. -MinMaxId 500 600 to filter for all object events with id 500-600. The ids are sorted by object creation time." + Environment.NewLine +
        "                        -CreateStack filter          Keep all object events (create/objRef/duplicate...) where the create stack matches." + Environment.NewLine +
        "                        -DestroyStack filter         Keep all object events (create/objRef/duplicate...) where the destroy stack matches." + Environment.NewLine +
        "                        -StackFilter filter          Keep only the events where the stack matches and throw away all other events. To keep all events which have e.g. CreateWebRequest in their stack use -StackFilter *CreateWebRequest*" + Environment.NewLine +
        "                        -Object filter               Filter for kernel object pointer value. E.g. -Object 0x8300004." + Environment.NewLine +
        "                        -Type filter                 Filter by object type e.g. -Type Event;Section. This influences also the display of totals." + Environment.NewLine +
        "                        -ShowTotal [Total,Process,None] Do not print individual events, just the counts. Total counts all handle types, Process shows handle counts per process. None omits all totals." + Environment.NewLine +
        "                        -TopN dd nn                  Show top n processes/object types in summary when -ShowTotal is Process or Total." + Environment.NewLine +
        "                        -ObjectName filter           Filter for object name. E.g. -ObjectName *IO to filter for all object which end with :IO." + Environment.NewLine +
        "                        -Handle filter               Text filter for handle value/s. E.g. -Handle 0xABC." + Environment.NewLine +
        "                        -ShowRef                     Show Object Reference increment/decrement operations." + Environment.NewLine +
        "                        -ShowStack                   Show stacks for events if recorded. If -csv is used only the the stack traces are added to CSV file." + Environment.NewLine +    
        "                        -Leak                        Show all events for objects which are not closed during the trace." + Environment.NewLine +  
        "                        -MultiProcess                Show handles which are created/duplicated from more than one process. A process can still inherit a handle which does not show up here." + Environment.NewLine +
        "                        -Inherit                     Show handles which are inherited by a child process." + Environment.NewLine + 
        "                        -Map [0,1]                   When 1 only memory map events are shown. When 0 memory map events are excluded." + Environment.NewLine +  
        "                        -PtrInMap 0x...              Filter file mapping objects which have this pointer inside their map range." + Environment.NewLine +
        "                        -MinMaxMapSize min [max]     Filter file mapping requests by their mapping size in bytes." + Environment.NewLine +
        "                        -Overlapped                  Show objects which are referenced by different handles. E.g. open an already existing named event (CreateEvent last error code returns ALREADY_EXISTS)." + Environment.NewLine 
        ;

        static readonly string ExamplesHelpString =
        "[yellow]Examples[/yellow]" + Environment.NewLine;

        static readonly string StatsExamples = ExamplesHelpString +
        "[green]Dump from ETL file event statistics, session times, ...[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Stats -filedir/fd xxx.etl" + Environment.NewLine +
        "[green]Dump from Extracted Json files Core Count, Memory, OS Version on a single line (CSV export is also supported)[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Stats -filedir c:\\MainVersion\\Extract -properties NumberOfProcessors,MemorySizeMB,OSVersion -OneLine" + Environment.NewLine;

        static readonly string VersionExamples = ExamplesHelpString +
        "[green]Module version of all modules. Module marker files are configured in the Configuration\\DllToBuildMap.json file[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Version -fd xxx.etl/xxx.Json" + Environment.NewLine +
        "[green]Get .NET Runtime Versions of all processes[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Version -fd xxx.json -dll clr.dll" + Environment.NewLine +
        "[green]Get all non Microsoft Device Driver versions of System process[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Version -fd xxx.etl -dll *.sys -pn System -VersionFilter !*Microsoft*" + Environment.NewLine +
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
        " ETWAnalyzer -dump Process -sortby time -timefmt utc" + Environment.NewLine +
        "[green]Dump processes and filter with Parent Process IDs (e.g. -parent 123;*456*).[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Process -fd xx.etl/.json -parent dd;dd2;dd5;... " + Environment.NewLine +
        "[green]Dump processes and filter by Windows session ids. Session -1 must be *-1*, otherwise it would be interpreted as an argument switch.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Process -fd xx.etl/.json -session 1;*-1*;13" + Environment.NewLine +
        "[green]Dump processes and display Windows session ids.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Process -fd xx.etl/.json -user abc;*xyz*" + Environment.NewLine +
        "[green]Dump processes and user names abc and xyz.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Process -fd xx.etl/.json -details" + Environment.NewLine +
        "[green]Dump processes as process tree where the parent process was cmd.exe and show process start/stop times as ETW trace session times in seconds.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Process -fd xx.etl/.json -sortby tree -parent cmd -timefmt s";


        static readonly string TestRunExamples = ExamplesHelpString +
        "[green]Dump TestRuns from a given directory. Works with ETL and Extracted Json files[green]" + Environment.NewLine +
        " ETWAnalyzer -dump TestRun -filedir C:\\MainVersion\\Extract" + Environment.NewLine +
        "[green]Download data ETL and Json data from a network share to speed up analysis[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump TestRun -filedir \\\\Server\\MainVersion\\Extract\\*Test1* -copyfilesto C:\\Analysis\\MainVersion -TestsPerRun 1 -SkipNTests 1 -WithEtl" + Environment.NewLine;

        static readonly string CPUExamples = ExamplesHelpString +
        "[green]Trend CPU consumption of one method (Type.Method) over the extracted profiling data over time for one Testcase[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -filedir c:\\MainVersion\\Extract\\*CallupAdhocColdReadingCR* -Methods *ImagingViewModel.InitAsync*" + Environment.NewLine +
        "[green]Print Total CPU consumption of processes and their command line. Print process start/stop/duration besides process name.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -filedir c:\\MainVersion\\Extract -ProcessFmt s" + Environment.NewLine +
        "[green]Save CPU consumption trend of one method into a CSV for all test cases[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -filedir c:\\MainVersion\\Extract -Methods *ImagingViewModel.InitAsync* -csv c:\\temp\\InitAsyncPerf.csv" + Environment.NewLine +
        "[green]Trend CPU consumption of a method for a test case in one process with a specific command line[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -filedir c:\\MainVersion\\Extract\\*TestCase* -Methods *ImagingViewModel.InitAsync* -ProcessName ServerBackend -CmdLine *ServerCmdArg*" + Environment.NewLine +
        "[green]Get an overview of the first 50 methods of the two processes consuming most CPU in the trace[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -fd xxx.json -topN 2 -topNMethods 50" + Environment.NewLine +
        "[green]Show common Antivirus drivers vendors besides module information for all modules for which no symbols could be resolved. The dll/driver name is then the \"method\" name.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -fd xxx.json -methods *.dll*;*.sys* -ShowModuleInfo Driver" + Environment.NewLine +
        "[green]Show CPU consumption of all executables which match *Trend Micro* in module name, path, product name or description.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -fd xxx.json -smi \"*Trend Micro*\" " + Environment.NewLine +
        "[green]Show CPU consumption of *Trend Micro* in module name, path, product name or description at method level[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -fd xxx.json -smi \"*Trend Micro*\" -methods *" + Environment.NewLine +
        "[green]Show all Import methods but skip file methods. Take only last 35 characters of method and show first last occurrence of method in trace time to relate with WPA timeline.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -methods *import*;!*file* -CutMethod -35 -fld s" + Environment.NewLine +
        "[green]Show method timings (first and last occurrence in trace) relative to OnClick[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -methods *import*;!*file* -ZeroTime FirstMethod *OnClick* -fld s s" + Environment.NewLine +
        "[green]Show unique methods which were executed in the last 5 s before process with pid 136816 did terminate. You see e.g. invoked error handlers just before a crash.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -fd xxx.json -methods * -SortBy Last -ZeroTime ProcessEnd -ZeroProcessName 136816 -pn 136816 -MinMaxFirst -5" + Environment.NewLine +
        "[green]Show CPU and process lifetime (along with duration if it did start/stop) with full Json path name[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -fd xxx.json -ProcessFmt s -ShowFullFileName" + Environment.NewLine +
        "[green]Show session IDs with the session filter IDs of 0 and 8[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump CPU -fd xxx.json -details -session 0;8" + Environment.NewLine ;

        static readonly string MemoryExamples = ExamplesHelpString +
        "[green]Get an overview about system memory consumption across all ETL files belonging to a test run. The TestRun Index you can get from the output of -dump TestRun -filedir ...[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Memory -filedir C:\\Extract\\TestRuns -TotalMemory -TestRunIndex 100 -TestRunCount 1" + Environment.NewLine +
        "[green]Trace possible leaks across files with a total memory growth of at least 100 MB. Use -CSV to store data.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Memory -filedir C:\\Extract\\TestRuns -GlobalDiffMB 100 -TestRunIndex 100 -TestRunCount 1" + Environment.NewLine +
        "[green]Print memory consumption of all non Microsoft and Windows processes. You can also search for a given path where the executable is located. [/green]" + Environment.NewLine +
        " ETWAnalyzer -dump memory -fd C:\\Extract\\TestRuns -smi !*Microsoft*;!*Windows*" + Environment.NewLine +
        "[green]Print top 5 processes having highest diff (diff can be memory growth or loss).[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Memory -SortBy Diff -TopN 5" + Environment.NewLine +
        "[green]WorkingsetPrivate MiB is printed for each process in Details mode for all the processes in a File.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Memory -fd xxx.json -Details" + Environment.NewLine +
        "[green]Only file Summary is printed omitting all the processes details.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Memory -fd xxx.json -ShowTotal File" + Environment.NewLine +
        "[green]Filter all the details with WorkingsetPrivate memory with 10MiB-100MiB default.Numbers can have units like Bytes, KiB, MiB, GiB e.g. 500MiB.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Memory -fd xxx.json -MinMaxWorkingSetPrivateMiB 10-100" + Environment.NewLine +
        "[green]Summary is not printed.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Memory -fd xxx.json -ShowTotal None" + Environment.NewLine +
        "[green]Display and filter by Windows Session Ids by 0.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Memory -fd xxx.json -Details -Session 0" + Environment.NewLine;

        static readonly string ExceptionExamples = ExamplesHelpString +
        "[green]Show all exceptions which did pass the exception filter during extraction, grouped by process, exception type and message.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Exception -filedir xx.json" + Environment.NewLine +
        "[green]Show all exceptions and their throw times by using a filter which matches all exceptions[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Exception -filedir xx.json -type *exception*" + Environment.NewLine +
        "[green]Print exception for each process and executable information. [/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Exception -fd C:\\Extract\\TestRuns -smi" + Environment.NewLine +
        "[green]Show call stack of all SQLiteExceptions of one or all extracted files. Use -ProcessName and/or -CmdLine to focus on specific process/es. Use -CSV to store data.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Exception -fd xx.json -type *SQLiteException* -ShowStack" + Environment.NewLine +
        "[green]Show call stack of all SQLiteExceptions in time appearance. Use -ProcessName and/or -CmdLine to focus on specific process/es. Use -CSV to store data.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Exception -fd xx.json -type *SQLiteException* -sortBy Time" + Environment.NewLine +
        "[green]Show all exception times of all extracted files in current folder in UTC time. Default is Local time of the customer.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Exception -type * -ShowTime -timefmt utc" + Environment.NewLine +
        "[green]Dump all TimeoutExceptions after the first occurrence of method ShowShutdownWindow and write them to a CSV file.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Exception -Type* timeout* -TimeFmt s -ZeroTime First *ShowShutdownWindow* -MinMaxExTime 0 -CSV Exceptions.csv" + Environment.NewLine +
        "[green]Show stacks of all exceptions of all extracted files in current folder. Print process start/stop/duration besides process name.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Exception -type * -ShowStack -ProcessFmt s" + Environment.NewLine +
        "[green]Display and filter by Windows Session Ids by 0.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Memory -fd xxx.json -Details -Session 0" + Environment.NewLine;


        static readonly string DiskExamples = ExamplesHelpString +
        "[green]Show Disk IO per directory down to 3 levels of the E Drive[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Disk -filedir xx.json -DirLevel 3 -fileName E:*" + Environment.NewLine +
        "[green]Show Disk IO per process with name *Viewing* in the E:\\Store* folder.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Disk -filedir xx.json -PerProcess -fileName E:\\Store* -processname *Viewing*" + Environment.NewLine +
        "[green]Show Disk IO per file with Read Time in range 1-10 ms[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Disk -filedir xx.json -MinMaxReadTime 1ms-10ms -DirLevel 100" + Environment.NewLine;


        static readonly string FileExamples = ExamplesHelpString +
        "[green]Show File IO summary of all processes at drive level[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -filedir xx.json" + Environment.NewLine +
        "[green]Show File IO per drive of processes Workflow below the folder E:\\lc\\c\\*[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -filedir xx.json -fileName E:\\lc\\c\\* -processname *Workflow*" + Environment.NewLine +
        "[green]Show File IO per first 3 sub folders of process Workflow below the folder E:\\lc\\c\\* of all extracted files in one metric[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -filedir xx.json -Merge -DirLevel 3 -fileName E:\\lc\\c\\* -processname *Workflow*" + Environment.NewLine +
        "[green]Show File IO at file level where Read+Write > 100 KB (100*1000 bytes or use 100 KiB for 100*1024 bytes). Reverse file name and clip to console buffer width to prevent wraparound if file name is too long[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -filedir xx.json -DirLevel 100 -MinMaxTotalSize 100KB -Clip -ReverseFileName" + Environment.NewLine +
        "[green]Dump File IO data of process Workflow to CSV File. If a directory of files is given all data is dumped into the same CSV[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -filedir xx.json -processName *Workflow* -csv Workflow.csv" + Environment.NewLine +
        "[green]Dump File IO per process which is setting File Security. To get the times use the -csv option to export additional data to a file[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -fd xx.json -FileOperation SetSecurity -PerProcess" + Environment.NewLine +
        "[green]Dump File IO per process for all files in current directory, filter for write operations, and sort by Write Count[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -FileOperation Write -SortBy Count -PerProcess" + Environment.NewLine +
        "[green]Show per process totals for all processes. Print process start/stop/duration besides process name.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -PerProcess -ShowTotal File -ProcessFmt s" + Environment.NewLine +
        "[green]Show File IO per process for all files in current directory with Read Time in range 1-10 ms[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -filedir xx.json -MinMaxReadTime 1ms-10ms -DirLevel 100" + Environment.NewLine +
        "[green]Show files with at least 50 read operations (sorted by read count) by filtering for file read data which will null out all other columns.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -filedir xx.json -FileOperation Read -MinMaxTotalCount 50 -SortBy Count -DirLevel 100" + Environment.NewLine +
        "[green]Show per process totals for all processes with show module information (exclusively to be used perprocess only). Print process start/stop/duration besides process name with information details exclusively for Microsoft processes.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -FileOperation Write -SortBy Count -PerProcess -smi *microsoft*" + Environment.NewLine +
        "[green]Dump files and the summary metrics is not displayed.[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump File -ShowTotal None" + Environment.NewLine +
        "[green]Display and filter by Windows Session Ids by 0.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump File -fd xxx.json -Details -Session 0" + Environment.NewLine;

        static readonly string PowerExamples = ExamplesHelpString +
        "[green]Show Windows Power Profile settings for two files side by side.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Power -filedir xx.json -filedir yy.json" + Environment.NewLine +
        "[green]Show Windows Power Profile settings with detailed profile descriptions.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Power -filedir xx.json -details" + Environment.NewLine +
        "[green]Compare two files and print only different properties. Useful to e.g. compare different used power profiles.[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump Power -filedir xx.json -filedir yy.json -Diff" + Environment.NewLine +
        "";

        static readonly string ThreadPoolExamples = ExamplesHelpString +
        "[green]Show .NET ThreadPool starvation events[/green]" + Environment.NewLine +
        " ETWAnalyzer -dump ThreadPool -filedir xx.json" + Environment.NewLine;

        static readonly string MarkerExamples = ExamplesHelpString +
        "[green]Show marker events. Print marker diff time relative to the *_Start event. Exclude all marker messages which contain screenshot in the string.[/green]" + Environment.NewLine +
        " ETWAnalyzer -filedir xx.json -dump Marker -ZeroTime marker *_Start  -MarkerFilter !*Screenshot*" + Environment.NewLine;

        static readonly string PMCExamples = ExamplesHelpString +
        "[green]Dump PMC values from sorter.exe and the new fastsorter.exe to check if CPU efficiency has improved.[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump PMC -pn fastsorter;sorter" + Environment.NewLine;

        static readonly string LBRExamples = ExamplesHelpString +
        "[green]Dump top 6 methods with highest call estimates from functioncaller.exe. Show also calling method.[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump LBR -pn functioncaller -topnmethods 6 -showcaller" + Environment.NewLine;

        static readonly string DnsExamples = ExamplesHelpString +
        "[green]Dump Dns latency metrics[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump Dns" + Environment.NewLine +
        "[green]Export dns data to CSV file and use as time column just the time part to make it easier to parse in Excel.[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump Dns -timefmt localtime -csv dns.csv" + Environment.NewLine +
        "[green]Show Dns latency for Firefox browser process omitting command line but with queried network adapters. If more than one network adapter was queried it could be that the first adapter query timed out.[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump Dns -ShowAdapter -NoCmdLine -pn firefox" + Environment.NewLine +
        "[green]Count all Dns queries to *google* domains which were slower than 20ms.[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump Dns -DnsQueryFilter *google* -SortBy Count -MinMaxTimeMs 20" + Environment.NewLine +
        "[green]Show every DNS query by time, process and returned IPs which were slower than 20ms. Query time is printed in WPA trace time. Overlapping (async) Dns query durations are only counted once for the sum in Total s column.[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump Dns -Details -MinMaxTimeMs 20 -TimeFmt s" + Environment.NewLine;

        static readonly string TcpExamples = ExamplesHelpString +
        "[green]Dump all TCP connections and summary metrics sorted by retransmission count.[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump Tcp" + Environment.NewLine +
        "[green]Dump all TCP connections and the summary metrics is not displayed.[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump Tcp -ShowTotal None" + Environment.NewLine +
        "[green]Dump all TCP connections which have sent 4MB - 500MB data sorted by sent bytes.[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump Tcp  -SortBy SentSize -MinMaxSentBytes 4MB-500MB" + Environment.NewLine +
        "[green]Dump all TCP connections for a given port range 32* (substring match).[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump Tcp  -IpPort *:32*" + Environment.NewLine +
        "[green]Dump all retransmission events into a csv file.[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump Tcp  -ShowRetransmit -csv Retransmissions.csv" + Environment.NewLine +
        "[green]Dump all TCP connections with duration ranging from 0-10s.[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump Tcp  -MinMaxConnectionDurationS 0 10s" + Environment.NewLine +
        "[green]Dump all all client retransmission events sorted by delay and omit connections which have no retransmissions in output.[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump Tcp -OnlyClientRetransmit -MinMaxRetransCount 1 -ShowRetransmit -SortRetransmitBy Delay" + Environment.NewLine ;

        static readonly string ObjectRefExamples = ExamplesHelpString +
        "[green]Dump all Handle Create/Duplicate/Close/AddRef/ReleaseRef/FileMap/FileUnmap events.[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump ObjectRef" + Environment.NewLine +
        "[green]Dump all leaked objects with stacks and write to a CSV file.[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump ObjectRef -Leak -ShowStack -csv Leaks.csv" + Environment.NewLine +
        "[green]Dump only handles with object names ending with :IO.[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump ObjectRef -ObjectName *:IO" + Environment.NewLine +
        "[green]Dump only file mapping events in the id range 500-600[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump ObjectRef -Map 1 -MinMaxId 500 600" + Environment.NewLine+
        "[green]Get a list of all handles in system[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump ObjectRef -ShowTotal Total" + Environment.NewLine +
        "[green]Get a list of top 10 processes with highest handle counts[/green]" + Environment.NewLine +
        " ETWAnalyzer -fd xx.json -dump ObjectRef -ShowTotal Process -TopN 10" + Environment.NewLine;




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
            DiskHelpString +
            FileHelpString +
            PowerHelpString +
            ThreadPoolHelpString +
            MarkHelpString +
            PMCHelpString +
            LBRHelpString +
            DnsHelpString +
            TcpHelpString + 
            ObjectRefHelpString;


        internal DumpCommands myCommand = DumpCommands.None;

        string myEtlFileOrZip;
        bool myIsVerbose;
        bool myPrintFiles;

        const decimal ByteUnit = 1.0m;
        const decimal SecondUnit = 1.0m;
        const decimal MiBUnit = 1024m * 1024m;
        const decimal MSUnit = 1/1000m;
        const decimal UsUnit = 1 / 1_000_000m;

        /// <summary>
        /// Sort order which can later be added to more and more commands and columns where it makes sense.
        /// Because not all sort orders make sense in for every command we limit the sort order to a list of allowed values per command which 
        /// is also used for command specific help strings. 
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
            Ready,
            CPUWaitReady,
            CPUWait,
            TestTime,
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
            OpenCloseTime,
            TotalSize,
            TotalTime,

            // CPU
            StartTime,
            ReadyAvg,
            CSwitchCount,
            Priority,

            // Process sort order
            StopTime,
            Tree,
            Session,

            // TCP sort orders
            ReceivedCount,
            SentCount,
            ReceivedSize,
            SentSize,
            TotalCount,
            ConnectTime,
            DisconnectTime,
            RetransmissionCount,
            RetransmissionTime,
            MaxRetransmissionTime,

            // Retransmit Orders
            Delay,

        }

        const string SortRetransmitContext = "-SortRetransmitBy";
        const string SortByContext = "-SortBy";

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

        public Func<string, bool> ProcessNameFilter { get; private set; } = _ => true;
        public Func<string, bool> CmdLineFilter { get; private set; } = _ => true;
        public List<string> FileOrDirectoryQueries { get; private set; } = new();
        public string CSVFile { get; private set; }
        public bool NoCSVSeparator { get; internal set; }
        public int TestsPerRun { get; private set; }
        public SkipTakeRange TopN { get; private set; } = new();
        public double LastNDays { get; private set; } = double.MaxValue;
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
        /// Select multiple ranges of test times. Valid for all commands via -MinMaxMsTestTimes xx-yy zz-aa ...
        /// Allows to select e.g. from a good run the good tests while ignoring outliers and selecting from the bad run the degraded ones excluding other outliers. 
        /// </summary>
        public List<MinMaxRange<int>> MinMaxMsTestTimes { get; private set; } = new();

        /// <summary>
        /// Controls how time is formatted in dump command output
        /// </summary>
        public DumpBase.TimeFormats TimeFormat { get; private set; }

        /// <summary>
        /// Show full input file name. By default file name is printed without path and extension
        /// </summary>
        public bool ShowFullFileName { get; internal set; }


        /// <summary>
        /// Format process start/end time in the desired way
        /// </summary>
        public DumpBase.TimeFormats? ProcessFormat { get; private set; }

        // Zero time definitions
        public ZeroTimeModes ZeroTimeMode { get; private set; }
        public KeyValuePair<string, Func<string, bool>> ZeroTimeFilter { get; private set; } = new(null, _ => false);
        public Func<string, bool> ZeroTimeProcessNameFilter { get; private set; } = (x) => true;


        // Dump Stats specific flags
        public string Properties { get; private set; }
        public bool OneLine { get; private set; }

        // Dump Version specific flags
        public DumpModuleVersions.PrintMode ModulePrintMode = DumpModuleVersions.PrintMode.Module;

        public Func<string, bool> ModuleFilter { get; private set; } = _ => true;

        public KeyValuePair<string, Func<string, bool>> DllFilter { get; set; } = new(null, _ => true);
        public KeyValuePair<string, Func<string, bool>> MissingPdbFilter { get; set; } = new(null, _ => true);

        public KeyValuePair<string, Func<string, bool>> VersionFilter { get; set; } = new(null, _ => true);

        // Dump ObjectRef specific flags
        public KeyValuePair<string, Func<string, bool>> CreateStackFilter { get; private set; } = new(null, _ => true);
        public KeyValuePair<string, Func<string, bool>> DestroyStackFilter { get; private set; } = new(null, _ => true);
        
        public KeyValuePair<string, Func<string, bool>> ObjectNameFilter { get; private set; } = new(null, _ => true);
        public KeyValuePair<string, Func<string, bool>> ObjectFilter { get; private set; } = new(null, _ => true);
        public KeyValuePair<string, Func<string, bool>> ViewBaseFilter { get; private set; } = new(null, _ => true);
        
        public KeyValuePair<string, Func<string, bool>> HandleFilter { get; private set; } = new(null, _ => true);

        public KeyValuePair<string, Func<string, bool>> RelatedProcessFilter { get; private set; } = new(null, _ => true);

        public MinMaxRange<long> MinMaxMapSize { get; private set; } = new();
        public MinMaxRange<long> MinMaxId { get; private set; } = new();

        public long? PtrInMap { get; private set; }
        public int? Map { get; private set; }
        public bool ShowRef { get; private set; }
        public bool Leak { get; private set; }
        public bool Overlapped { get; private set; }
        public bool MultiProcess { get; private set; }  
        public bool Inherit {  get ; private set; } 


        // Dump ObjRef/Exception specific Flags
        public KeyValuePair<string, Func<string, bool>> StackFilter { get; private set; } = new(null, _ => true);
        public KeyValuePair<string, Func<string, bool>> TypeFilter { get; private set; } = new(null, _ => true);

        // Dump Exception specific Flags
        public bool FilterExceptions { get; private set; }
        
        public KeyValuePair<string, Func<string, bool>> MessageFilter { get; private set; } = new(null, _ => true);
        
        public int CutStackMin { get; private set; }
        public int CutStackMax { get; private set; }

        public const int MaxMessageLength = 500;
        public int MaxMessage { get; private set; } = MaxMessageLength;
        public MinMaxRange<double> MinMaxExTimeS { get; private set; } = new();
        public bool ShowTime { get; private set; }

        // Dump Process specific Flags
        public bool ShowAllProcesses { get; private set; }
        public bool ShowFileOnLine { get; private set; }
        public bool Crash { get; private set; }
        public bool ShowUser { get; private set; }
        public MinMaxRange<double> MinMaxStart { get; private set; } = new();

        /// <summary>
        /// Parent filter must be null by default to not alter behavior during dumping parent processes.
        /// </summary>
        public Func<string, bool> Parent { get; private set; } = null;

        public Func<string, bool> User { get; private set; } = _ => true;

        public Func<string, bool> Session { get; private set; } = _ => true;

        // Dump CPU specific Flags
        public KeyValuePair<string, Func<string, bool>> StackTagFilter { get; private set; }
        public KeyValuePair<string, Func<string, bool>> MethodFilter { get; private set; }

        public SkipTakeRange TopNMethods { get; private set; } = new();
        public bool NoDll { get; private set; } = true;
        public bool NoArgs { get; private set; } = true;

        public MinMaxRange<int> MinMaxCPUMs { get; private set; } = new();
        public MinMaxRange<int> MinMaxWaitMs { get; private set; } = new();
        public MinMaxRange<int> MinMaxReadyMs { get; private set; } = new();
        public MinMaxRange<int> MinMaxReadyAverageUs { get; private set; } = new();
        public MinMaxRange<int> MinMaxCSwitch { get; private set; } = new();
        public bool NoReadyDetails { get; private set; }
        public bool NoFrequencyDetails { get; private set; }
        public bool NoPriorityDetails { get; private set; }
        
        public bool Normalize { get; private set; }


        public MinMaxRange<double> MinMaxFirstS { get; private set; } = new();
        public MinMaxRange<double> MinMaxLastS { get; private set; } = new();
        public MinMaxRange<double> MinMaxDurationS { get; private set; } = new();

        public MinMaxRange<double> MinMaxConnectionDurationS { get; private set; } = new();

        public int MethodCutStart { get; private set; }
        public int MethodCutLength { get; private set; } = int.MaxValue;

        public bool ShowStack { get; private set; }
        public bool ShowDetailsOnMethodLine { get; private set; }
        public bool ShowModuleInfo { get; private set; }
        public bool ShowDriversOnly { get; private set; }
        public KeyValuePair<string, Func<string, bool>> ShowModuleFilter { get; private set; } = new(null, _ => true);

        public bool ThreadCount { get; private set; }
        public bool FirstLastDuration { get; private set; }
        public DumpBase.TimeFormats? FirstTimeFormat { get; private set; }
        public DumpBase.TimeFormats? LastTimeFormat { get; private set; }
        public TotalModes? ShowTotal { get; private set; }
        
        // Dump Memory specific Flags
        public bool TotalMemory { get; private set; }

        public MinMaxRange<decimal> MinMaxWorkingSetMiB { get; private set; } = new();
        public MinMaxRange<decimal> MinMaxCommitMiB { get; private set; } = new();
        public MinMaxRange<decimal> MinMaxSharedCommitMiB { get; private set; } = new();
        public MinMaxRange<decimal> MinMaxWorkingsetPrivateMiB { get; private set; } = new();
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
        public SkipTakeRange TopNProcesses { get; private set; } = new();
        public MinMaxRange<decimal> MinMaxReadSizeBytes     { get; private set; } = new();
        public MinMaxRange<decimal> MinMaxWriteSizeBytes    { get; private set; } = new();
        public MinMaxRange<decimal> MinMaxTotalSizeBytes    { get; private set; } = new();

        public MinMaxRange<decimal> MinMaxWriteTimeS      { get; private set; } = new();
        public MinMaxRange<decimal> MinMaxReadTimeS       { get; private set; } = new();
        public MinMaxRange<decimal> MinMaxTotalTimeS      { get; private set; } = new();

        public MinMaxRange<decimal> MinMaxTotalCount { get; private set; } = new();


        // Dump File specific flags
        public bool ShowAllFiles { get; private set; }
        public Extract.FileIO.FileIOStatistics.FileOperation FileOperation { get; private set; }


        // Shared by -dump File, Process, CPU, Dns, Power, ...
        public bool ShowDetails { get; private set; }

        // Dump ThreadPool specific Flags
        public bool NoCmdLine { get; private set; }

        // Dump Power specific flags
        public bool ShowDiff { get; private set; }

        // Dump Marker specific Flags
        public Func<string, bool> MarkerFilter { get; private set; } = _ => true;
        public MinMaxRange<double> MinMaxMarkDiffTime = new();

        // Dump PMC specific flags
        public bool NoCounters { get; private set; }

        // Dump LBR specific flags
        public bool ShowCaller { get; private set; }

        public int ScalingFactor { get; private set; } = 1;
        public MinMaxRange<int> MinMaxCount { get; private set; } = new();

        // Dump Dns specific flags
        public bool ShowAdapter { get; set; }
        public bool ShowReturnCode { get; set; }
        public KeyValuePair<string, Func<string, bool>> DnsQueryFilter { get; private set; } = new(null, _ => true);
        public MinMaxRange<double> MinMaxTimeMs { get; private set; } = new();
        public MinMaxRange<double> MinMaxTotalTimeMs { get; private set; } = new();
        public bool ShowProcess { get; set; }
        public SkipTakeRange TopNDetails { get; set; } = new();


        // Dump Tcp specific flags
        public KeyValuePair<string, Func<string, bool>> IpPortFilter { get; private set; } = new(null, _ => true);
        public MinMaxRange<int> MinMaxRetransDelayMs { get; private set; } = new();
        public MinMaxRange<int> MinMaxRetransBytes { get; private set; } = new(2, null);  // by default filter retransmitted packets which are not 0 or 1 bytes which are often just ACKs or keepalive packets.
        public KeyValuePair<string, Func<string, bool>> TcbFilter { get; private set; } = new(null, _ => true);

        public MinMaxRange<int> MinMaxRetransCount { get; private set; } = new();

        public bool ShowRetransmit {get; private set; }
        public SortOrders RetransSortOrder { get; private set; }
        public SkipTakeRange TopNRetrans { get; private set; } = new();
        public MinMaxRange<ulong> MinMaxSentBytes { get; private set; } = new();
        public MinMaxRange<ulong> MinMaxReceivedBytes { get; private set; } = new();

        public bool OnlyClientRetransmit { get; private set; }

        /// <summary>
        /// Do not load data again if in console mode
        /// </summary>
        public Lazy<SingleTest>[] PreloadedData { get; private set; }

        /// <summary>
        /// Current command arguments passed by console or command start 
        /// </summary>
        string myConsoleCommandArgs = Environment.CommandLine;


        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="args"></param>
        public DumpCommand(string[] args) : base(args)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <param name="preloadedData"></param>
        public DumpCommand(string[] args, Lazy<SingleTest>[] preloadedData) : this(args)
        {
            PreloadedData = preloadedData;
            myConsoleCommandArgs = String.Join(" ", args);
            myConsoleCommandArgs = ".dump " + myConsoleCommandArgs;
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
                    case CommandFactory.DumpCommand:  // -dump
                        // ignore -dump which is already known by factory
                        break;
                    case DebugArg:    // -debug
                        Program.DebugOutput = true;
                        break;
                    case NoTestRunGrouping:
                        TestRun.MaxTimeBetweenTests = TimeSpan.MaxValue;
                        break;
                    case NoColorArg:   // -nocolor
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
                    case "-map":
                        string mapArg = GetNextNonArg("-map");
                        Map = int.Parse(mapArg);
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
                    case "-showuser":
                        ShowUser = true;
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
                    case "-nocounters":
                        NoCounters = true;
                        break;
                    case "-showadapter":
                        ShowAdapter = true;
                        break;
                    case "-showreturncode":
                        ShowReturnCode = true;
                        break;
                    case "-showmoduleinfo":
                    case "-smi":
                        ShowModuleInfo = true;
                        string showModuleInfoArg = GetNextNonArg("-showmoduleinfo", false);
                        ShowDriversOnly = showModuleInfoArg?.ToLower() == "driver" ? true : false;
                        if( !ShowDriversOnly )
                        {
                            ShowModuleFilter = new KeyValuePair<string, Func<string, bool>>(showModuleInfoArg, Matcher.CreateMatcher(showModuleInfoArg));
                        }
                        break;
                    case TestsPerRunArg:  // -testsperrun
                        this.TestsPerRun = int.Parse(GetNextNonArg(TestsPerRunArg), CultureInfo.InvariantCulture);
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
                    case "-showcaller":
                        ShowCaller = true;
                        break;
                    case "-showretransmit":
                        ShowRetransmit = true;
                        break;
                    case "-onlyclientretransmit":
                        OnlyClientRetransmit = true;
                        break;
                    case "-topn":
                        string topN = GetNextNonArg("-topn");
                        string skip = GetNextNonArg("-topn", false); // skip string is optional
                        Tuple<int,int> topNAndSkip = topN.GetRange(skip);
                        TopN = new SkipTakeRange(topNAndSkip.Item1, topNAndSkip.Item2);
                        break;
                    case "-topndetails":
                        string topnDetailsStr = GetNextNonArg("-topndetails");
                        string topnDetailSkipStr = GetNextNonArg("-topndetails", false); // skip string is optional
                        Tuple<int, int> topNDetailsSkip = topnDetailsStr.GetRange(topnDetailSkipStr);
                        TopNDetails = new SkipTakeRange(topNDetailsSkip.Item1, topNDetailsSkip.Item2);
                        break;
                    case "-topnretrans":
                        string topnretrans = GetNextNonArg("-topnretrans");
                        string skipretrans = GetNextNonArg("-topnretrans", false);
                        Tuple<int, int> topnskipertrans = topnretrans.GetRange(skipretrans);
                        TopNRetrans = new SkipTakeRange(topnskipertrans.Item1, topnskipertrans.Item2);
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
                    case "-relatedprocess":
                        string realatedProcssFilterStr = GetNextNonArg("-relatedprocess");
                        RelatedProcessFilter = new KeyValuePair<string, Func<string, bool>>(realatedProcssFilterStr, Matcher.CreateMatcher(realatedProcssFilterStr, MatchingMode.CaseInsensitive, pidFilterFormat:true));
                        break;
                    case "-parent":
                        Parent =            Matcher.CreateMatcher(GetNextNonArg("-parent"), MatchingMode.CaseInsensitive, pidFilterFormat:true);
                        break;
                    case "-user":
                        User =              Matcher.CreateMatcher(GetNextNonArg("-user"));
                        break;
                    case "-session":
                        Session =           Matcher.CreateMatcher(GetNextNonArg("-session"));
                        break;
                    case "-zeroprocessname":
                    case "-zpn":
                        ZeroTimeProcessNameFilter = Matcher.CreateMatcher(GetNextNonArg("-zeroprocessname"), MatchingMode.CaseInsensitive, pidFilterFormat: true);
                        break;
                    case "-cmdline":
                        CmdLineFilter =     Matcher.CreateMatcher(GetNextNonArg("-cmdline"));
                        break;
                    case "-markerfilter":
                        MarkerFilter =      Matcher.CreateMatcher(GetNextNonArg("-markerfilter"));
                        break;
                    case "-dll":
                        string dllFilter = GetNextNonArg("-dll");
                        DllFilter = new KeyValuePair<string, Func<string, bool>>(dllFilter, Matcher.CreateMatcher(dllFilter));
                        ModulePrintMode = DumpModuleVersions.PrintMode.Dll;
                        break;
                    case "-missingpdb":
                        string pdbFilter = GetNextNonArg("-missingpdb");
                        MissingPdbFilter = new KeyValuePair<string, Func<string, bool>>(pdbFilter, Matcher.CreateMatcher(pdbFilter));
                        ModulePrintMode = DumpModuleVersions.PrintMode.Pdb;
                        break;
                    case "-versionfilter":
                        string versionFilter = GetNextNonArg("-versionfilter");
                        VersionFilter = new KeyValuePair<string, Func<string, bool>>(versionFilter, Matcher.CreateMatcher(versionFilter));
                        ModulePrintMode = DumpModuleVersions.PrintMode.Dll;
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
                    case "-destroystack":
                        string destroyStackfilter = GetNextNonArg("-destroystack");
                        DestroyStackFilter =    new KeyValuePair<string, Func<string, bool>>(destroyStackfilter, Matcher.CreateMatcher(destroyStackfilter));
                        break;
                    case "-createstack":
                        string createStack = GetNextNonArg("-createstack");
                        CreateStackFilter =     new KeyValuePair<string, Func<string, bool>>(createStack, Matcher.CreateMatcher(createStack));
                        break;
                    case "-objectname":
                        string handleNameFilter = GetNextNonArg("-objectname");
                        ObjectNameFilter =      new KeyValuePair<string, Func<string, bool>>(handleNameFilter, Matcher.CreateMatcher(handleNameFilter));
                        break;
                    case "-methods":
                        string methodFilter = GetNextNonArg("-methods");
                        methodFilter = ReplaceMethodFilterAliases(methodFilter);
                         MethodFilter =         new KeyValuePair<string, Func<string, bool>>(methodFilter,   Matcher.CreateMatcher(methodFilter));
                        break;
                    case "-stacktags":
                        string stacktagFilter = GetNextNonArg("-stacktags");
                        StackTagFilter =        new KeyValuePair<string, Func<string, bool>>(stacktagFilter, Matcher.CreateMatcher(stacktagFilter));
                        break;
                    case "-dnsqueryfilter":
                        string dnsQueryFilter = GetNextNonArg("-dnsqueryfilter");
                        DnsQueryFilter =        new KeyValuePair<string, Func<string, bool>>(dnsQueryFilter, Matcher.CreateMatcher(dnsQueryFilter));
                        break;
                    case "-ipport":
                        string ipPortFilter = GetNextNonArg("-ipport");
                        IpPortFilter =          new KeyValuePair<string, Func<string, bool>>(ipPortFilter, Matcher.CreateMatcher(ipPortFilter));
                        break;
                    case "-tcb":
                        string tcpFilter = GetNextNonArg("-tcb");
                        TcbFilter =             new KeyValuePair<string, Func<string, bool>>(tcpFilter, Matcher.CreateMatcher(tcpFilter));
                        break;
                    case "-object":
                        string objectFilter = GetNextNonArg("-object");
                        ObjectFilter =          new KeyValuePair<string, Func<string, bool>>(objectFilter, Matcher.CreateMatcher(objectFilter));
                        break;
                    case "-viewbase":
                        string viewBaseFilter = GetNextNonArg("-viewbase");
                        ViewBaseFilter =        new KeyValuePair<string, Func<string, bool>>(viewBaseFilter, Matcher.CreateMatcher(viewBaseFilter));
                        break;
                    case "-ptrinmap":
                        string ptrInMap = GetNextNonArg("-ptrinmap");
                        PtrInMap = ptrInMap.ParseLongFromHex();
                        break;
                    case "-handle":
                        string handleFilter = GetNextNonArg("-handle");
                        HandleFilter =         new KeyValuePair<string, Func<string, bool>>(handleFilter, Matcher.CreateMatcher(handleFilter));
                        break;
                    case "-zerotime":
                    case "-zt":
                        string zerotimeType = GetNextNonArg("-zerotime", false);
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
                    case "-showprocess":
                        ShowProcess = true;
                        break;
                    case "-showref":
                        ShowRef = true;
                        break;
                    case "-multiprocess":
                        MultiProcess = true;
                         break;
                    case "-leak":
                        Leak = true;
                        break;
                    case "-overlapped":
                        Overlapped = true;
                        break;
                    case "-cutmethod":
                        string cutmethod = GetNextNonArg("-cutmethod");
                        KeyValuePair<int, int> cutminmax = cutmethod.GetMinMax(1, true);
                        MethodCutStart = cutminmax.Key;
                        MethodCutLength = cutminmax.Value;
                        break;
                    case "-minmaxcpums":
                        string minMaxCPUms = GetNextNonArg("-minmaxcpums");
                        KeyValuePair<decimal, decimal> minMax = minMaxCPUms.GetMinMaxDecimal(MSUnit);
                        MinMaxCPUMs = new MinMaxRange<int>(minMax.Key.ConvertToInt(1 / MSUnit), minMax.Value.ConvertToInt(1 / MSUnit));
                        break;
                    case "-minmaxconnectiondurations":
                        string minConnectionDurationS = GetNextNonArg("-minmaxconnectiondurations");
                        string maxConnectionDurationS = GetNextNonArg("-minmaxconnectiondurations", false);
                        Tuple<double, double> minMaxConnectionDinS = minConnectionDurationS.GetMinMaxDouble(maxConnectionDurationS, SecondUnit);
                        MinMaxConnectionDurationS = new MinMaxRange<double>(minMaxConnectionDinS.Item1, minMaxConnectionDinS.Item2);
                        break;
                    case "-minmaxwaitms":
                        string minMaxWaitms = GetNextNonArg("-minmaxwaitms");
                        KeyValuePair<decimal, decimal> minMaxWait = minMaxWaitms.GetMinMaxDecimal(MSUnit);
                        MinMaxWaitMs = new MinMaxRange<int>(minMaxWait.Key.ConvertToInt(1/MSUnit), minMaxWait.Value.ConvertToInt(1/MSUnit));
                        break;
                    case "-minmaxreadyms":
                        string minmaxreadyms = GetNextNonArg("-minmaxreadyms");
                        KeyValuePair<decimal, decimal> minMaxReady = minmaxreadyms.GetMinMaxDecimal(MSUnit);
                        MinMaxReadyMs = new MinMaxRange<int>(minMaxReady.Key.ConvertToInt(1/MSUnit), minMaxReady.Value.ConvertToInt(1/MSUnit));
                        break;
                    case "-minmaxcswitchcount":
                        string minmaxcswitchcount = GetNextNonArg("-minmaxcswitchcount");
                        KeyValuePair<decimal, decimal> minmaxcswitch = minmaxcswitchcount.GetMinMaxDecimal(1);
                        MinMaxCSwitch = new MinMaxRange<int>(minmaxcswitch.Key.ConvertToInt(1), minmaxcswitch.Value.ConvertToInt(1));
                        break;
                    case "-minmaxreadyavgus":
                        string minmaxReadyAvgus = GetNextNonArg("-minmaxreadyavgus");
                        KeyValuePair<decimal, decimal> minmaxReadyAvg = minmaxReadyAvgus.GetMinMaxDecimal(UsUnit);
                        MinMaxReadyAverageUs = new MinMaxRange<int>(minmaxReadyAvg.Key.ConvertToInt(1 / UsUnit), minmaxReadyAvg.Value.ConvertToInt(1 / UsUnit));
                        break;
                    case "-minmaxcount":
                        string minmaxcount = GetNextNonArg("-minmaxcount");
                        KeyValuePair<int, int> minMaxCount = minmaxcount.GetMinMax();
                        MinMaxCount = new MinMaxRange<int>(minMaxCount.Key, minMaxCount.Value);
                        break;
                    case "-minmaxretransdelayms":
                        string minmaxretransdelaymsStr = GetNextNonArg("-minmaxretransdelayms");
                        KeyValuePair<int, int> minmaxretransdelayms = minmaxretransdelaymsStr.GetMinMax();
                        MinMaxRetransDelayMs = new MinMaxRange<int>(minmaxretransdelayms.Key, minmaxretransdelayms.Value);
                        break;
                    case "-minmaxretranscount":
                        string minmaxretranscountStr = GetNextNonArg("-minmaxretranscount");
                        KeyValuePair<int, int> minmaxretransCount = minmaxretranscountStr.GetMinMax();
                        MinMaxRetransCount = new MinMaxRange<int>(minmaxretransCount.Key, minmaxretransCount.Value);
                        break;
                    case "-minmaxretransbytes":
                        string minmaxretransbytesStr = GetNextNonArg("-minmaxretransbytes");
                        KeyValuePair<int, int> minmaxretransbytes = minmaxretransbytesStr.GetMinMax();
                        MinMaxRetransBytes = new MinMaxRange<int>(minmaxretransbytes.Key, minmaxretransbytes.Value);
                        break;
                    case "-minmaxsentbytes":
                        string minmaxsentbytesStr = GetNextNonArg("-minmaxsentbytes");
                        KeyValuePair<ulong, ulong> minmaxsentBytes = minmaxsentbytesStr.GetMinMaxULong(ByteUnit);
                        MinMaxSentBytes = new MinMaxRange<ulong>(minmaxsentBytes.Key, minmaxsentBytes.Value);
                        break;
                    case "-minmaxreceivedbytes":
                        string minmaxreceivedbytesStr = GetNextNonArg("-minmaxreceivedbytes");
                        KeyValuePair<ulong, ulong> minmaxreceivedBytes = minmaxreceivedbytesStr.GetMinMaxULong(ByteUnit);
                        MinMaxReceivedBytes = new MinMaxRange<ulong>(minmaxreceivedBytes.Key, minmaxreceivedBytes.Value);
                        break;
                    case "-minmaxreadsize":
                        string minmaxreadsizeStr = GetNextNonArg("-minmaxreadsize");
                        KeyValuePair<decimal, decimal> minmaxReadSizeBytes = minmaxreadsizeStr.GetMinMaxDecimal(ByteUnit);
                        MinMaxReadSizeBytes = new MinMaxRange<decimal>(minmaxReadSizeBytes.Key, minmaxReadSizeBytes.Value);
                        break;
                    case "-minmaxwritesize":
                        string minmaxwritesizeStr = GetNextNonArg("-minmaxwritesize");
                        KeyValuePair<decimal, decimal> minmaxWriteSizeBytes = minmaxwritesizeStr.GetMinMaxDecimal(ByteUnit);
                        MinMaxWriteSizeBytes = new MinMaxRange<decimal>(minmaxWriteSizeBytes.Key, minmaxWriteSizeBytes.Value);
                        break;
                    case "-minmaxtotalsize":
                        string minmaxtotalsizeStr = GetNextNonArg("-minmaxtotalsize");
                        KeyValuePair<decimal, decimal> minmaxtotalSizeBytes = minmaxtotalsizeStr.GetMinMaxDecimal(ByteUnit);
                        MinMaxTotalSizeBytes = new MinMaxRange<decimal>(minmaxtotalSizeBytes.Key, minmaxtotalSizeBytes.Value);
                        break;
                    case "-minmaxreadtime":
                        string minmaxreadtimeStr = GetNextNonArg("-minmaxreadtime");
                        KeyValuePair<decimal,decimal> minmaxreadtimeS = minmaxreadtimeStr.GetMinMaxDecimal(SecondUnit);
                        MinMaxReadTimeS = new MinMaxRange<decimal>(minmaxreadtimeS.Key, minmaxreadtimeS.Value);
                        break;
                    case "-minmaxwritetime":
                        string minmaxwritetimeStr = GetNextNonArg("-minmaxwritetime");
                        KeyValuePair<decimal, decimal> minmaxWriteTimeS = minmaxwritetimeStr.GetMinMaxDecimal(SecondUnit);
                        MinMaxWriteTimeS = new MinMaxRange<decimal>(minmaxWriteTimeS.Key, minmaxWriteTimeS.Value);
                        break;
                    case "-minmaxtotaltime":
                        string minmaxtotaltimeStr = GetNextNonArg("-minmaxtotaltime");
                        KeyValuePair<decimal, decimal> minmaxtotalTimeS = minmaxtotaltimeStr.GetMinMaxDecimal(SecondUnit);
                        MinMaxTotalTimeS = new MinMaxRange<decimal>(minmaxtotalTimeS.Key, minmaxtotalTimeS.Value);
                        break;
                    case "-minmaxtotalcount":
                        string minmaxtotalcountStr = GetNextNonArg("-minmaxtotalcount");
                        KeyValuePair<int, int> minmaxtotalCount = minmaxtotalcountStr.GetMinMax();
                        MinMaxTotalCount = new MinMaxRange<decimal>(minmaxtotalCount.Key, minmaxtotalCount.Value);
                        break;
                    case "-minmaxworkingsetmib":
                        string minworkingsetmbStr = GetNextNonArg("-minmaxworkingsetmib");
                        KeyValuePair<decimal, decimal> minworkingsetmb = minworkingsetmbStr.GetMinMaxDecimal(MiBUnit);
                        MinMaxWorkingSetMiB = new MinMaxRange<decimal>(minworkingsetmb.Key/ MiBUnit, minworkingsetmb.Value/ MiBUnit);
                        break;
                    case "-minmaxcommitmib":
                        string minmaxcommitMBStr = GetNextNonArg("-minmaxcommitmib");
                        KeyValuePair<decimal,decimal> minmaxcommitMB = minmaxcommitMBStr.GetMinMaxDecimal(MiBUnit);
                        MinMaxCommitMiB = new MinMaxRange<decimal>(minmaxcommitMB.Key / MiBUnit, minmaxcommitMB.Value / MiBUnit);
                        break;
                    case "-minmaxsharedcommitmib":
                        string minmaxsharedcommitMBStr = GetNextNonArg("-minmaxsharedcommitmib");
                        KeyValuePair<decimal, decimal> minmaxsharedcommit = minmaxsharedcommitMBStr.GetMinMaxDecimal(MiBUnit);
                        MinMaxSharedCommitMiB = new MinMaxRange<decimal>(minmaxsharedcommit.Key / MiBUnit, minmaxsharedcommit.Value / MiBUnit);
                        break;
                    case "-minmaxworkingsetprivatemib":
                        string minmaxworkingsetprivateMiBStr = GetNextNonArg("-minmaxworkingsetprivatemib");
                        KeyValuePair<decimal, decimal> minmaxworkingsetprivate = minmaxworkingsetprivateMiBStr.GetMinMaxDecimal(MiBUnit);
                        MinMaxWorkingsetPrivateMiB = new MinMaxRange<decimal>(minmaxworkingsetprivate.Key / MiBUnit, minmaxworkingsetprivate.Value / MiBUnit);
                        break;
                    case "-minmaxid":
                        string minId = GetNextNonArg("-minmaxid");
                        string maxId = GetNextNonArg("-minmaxid", false); // optional
                        Tuple<long, long> minMaxId = minId.GetMinMaxLong(maxId, 1.0m);
                        MinMaxId = new MinMaxRange<long>(minMaxId.Item1, minMaxId.Item2);
                        break;
                    case "-minmaxfirst":
                        string minFirst = GetNextNonArg("-minmaxfirst");
                        string maxFirst = GetNextNonArg("-minmaxfirst", false); // optional
                        Tuple<double,double> minMaxFirst = minFirst.GetMinMaxDouble(maxFirst, SecondUnit);
                        MinMaxFirstS = new MinMaxRange<double>(minMaxFirst.Item1, minMaxFirst.Item2);
                        break;
                    case "-minmaxlast":
                        string minLast = GetNextNonArg("-minmaxlast");
                        string maxLast = GetNextNonArg("-minmaxlast", false); // optional
                        Tuple<double, double> minMaxLast = minLast.GetMinMaxDouble(maxLast, SecondUnit);
                        MinMaxLastS = new MinMaxRange<double>(minMaxLast.Item1, minMaxLast.Item2);
                        break;
                    case "-minmaxduration":
                        string minDuration = GetNextNonArg("-minmaxduration");
                        string maxDuration = GetNextNonArg("-minmaxduration", false); // optional
                        Tuple<double, double> minMaxDuration = minDuration.GetMinMaxDouble(maxDuration, SecondUnit);
                        MinMaxDurationS = new MinMaxRange<double>(minMaxDuration.Item1, minMaxDuration.Item2);
                        break;
                    case "-minmaxmapsize":
                        string minMaxMapSizeMin = GetNextNonArg("-minmaxmapsize");
                        string minMaxMapSizeMax = GetNextNonArg("-minmaxmapsize", false); // optional
                        Tuple<long, long> minmaxMapSize = minMaxMapSizeMin.GetMinMaxLong(minMaxMapSizeMax, ByteUnit);
                        MinMaxMapSize = new MinMaxRange<long>(minmaxMapSize.Item1, minmaxMapSize.Item2);
                        break;
                    case "-minmaxextime":
                        string minExTime = GetNextNonArg("-minmaxextime");
                        string maxExTime = GetNextNonArg("-minmaxextime", false); // optional
                        Tuple<double, double> exMinMax = minExTime.GetMinMaxDouble(maxExTime, SecondUnit);
                        MinMaxExTimeS = new MinMaxRange<double>(exMinMax.Item1, exMinMax.Item2);
                        break;
                    case "-minmaxmarkdifftime":
                        string minMarkDiffTime = GetNextNonArg("-minmaxmarkdifftime");
                        string maxMarkDiffTime = GetNextNonArg("-minmaxmarkdifftime", false); // optional
                        Tuple<double, double> minmaxmarkdifftimedouble = minMarkDiffTime.GetMinMaxDouble(maxMarkDiffTime, SecondUnit);
                        MinMaxMarkDiffTime = new MinMaxRange<double>(minmaxmarkdifftimedouble.Item1, minmaxmarkdifftimedouble.Item2);
                        break;
                    case "-minmaxtimems":
                        string minTimeMs = GetNextNonArg("-minmaxtimems");
                        string maxTimeMs = GetNextNonArg("-minmaxtimems", false); // optional
                        Tuple<double, double> minMaxTimeMsDouble = minTimeMs.GetMinMaxDouble(maxTimeMs, MSUnit);
                        MinMaxTimeMs = new MinMaxRange<double>(minMaxTimeMsDouble.Item1, minMaxTimeMsDouble.Item2);
                        break;
                    case "-minmaxtotaltimems":
                        string minTotalTimeMs = GetNextNonArg("-minmaxtimems");
                        string maxTotalTimeMs = GetNextNonArg("-minmaxtimems", false); // optional
                        Tuple<double, double> minMaxTotalTimeMsDouble = minTotalTimeMs.GetMinMaxDouble(maxTotalTimeMs, MSUnit);
                        MinMaxTotalTimeMs = new MinMaxRange<double>(minMaxTotalTimeMsDouble.Item1, minMaxTotalTimeMsDouble.Item2);
                        break;
                    case "-minmaxstart":
                        string minStart = GetNextNonArg("-minmaxstart");
                        string maxStart = GetNextNonArg("-minmaxstart", false); //optional
                        Tuple<double, double> minmaxStartDouble = minStart.GetMinMaxDouble(maxStart, SecondUnit);
                        MinMaxStart = new MinMaxRange<double>(minmaxStartDouble.Item1, minmaxStartDouble.Item2);
                        break;
                    case "-minmaxmstesttimes":
                        string minMaxTestTime = GetNextNonArg("-minmaxmstesttimes");
                        do
                        {
                            KeyValuePair<int, int> minmax = minMaxTestTime.GetMinMax(SecondUnit);
                            MinMaxMsTestTimes.Add( new MinMaxRange<int>(minmax.Key, minmax.Value) );
                        } while( (minMaxTestTime = GetNextNonArg("-minmaxmstesttimes", false)) != null);

                        break;
                    case "-showfullfilename":
                    case "-sffn":
                        ShowFullFileName = true;
                        break;
                    case "-cutstack":
                        string cutStackStr = GetNextNonArg("-cutstack");
                        KeyValuePair<int, int> cutStackMinMax = cutStackStr.GetMinMax();
                        CutStackMin = cutStackMinMax.Key;
                        CutStackMax = cutStackMinMax.Value;
                        break;
                    case "-maxmessage":
                        string maxMessageStr = GetNextNonArg("-maxmessage");
                        MaxMessage = int.Parse(maxMessageStr, CultureInfo.InvariantCulture);
                        break;
                    case "-fileoperation":
                        string fileOp = GetNextNonArg("-fileoperation", false);
                        ParseEnum<Extract.FileIO.FileIOStatistics.FileOperation>("FileOperation values", fileOp,
                            () => { FileOperation = (Extract.FileIO.FileIOStatistics.FileOperation)Enum.Parse(typeof(Extract.FileIO.FileIOStatistics.FileOperation), fileOp, true); },
                            Extract.FileIO.FileIOStatistics.FileOperation.All);
                        break;
                    case "-sortby":
                        string sortOrder = GetNextNonArg("-sortby", false);
                        SortOrder = ParseEnum<SortOrders>(SortByContext, sortOrder, GetValidSortOrders(SortByContext));
                        break;
                    case "-sortretransmitby":
                        string retransSortOrder = GetNextNonArg("-sortretransmitby", false);                        
                        RetransSortOrder = ParseEnum<SortOrders>(SortRetransmitContext, retransSortOrder, GetValidSortOrders(SortRetransmitContext));
                        break;
                    case "-timefmt":
                        string timeformatStr = GetNextNonArg("-timefmt", false);
                        ParseEnum<DumpBase.TimeFormats>("Time Format", timeformatStr, 
                            () => { TimeFormat = (DumpBase.TimeFormats)Enum.Parse(typeof(DumpBase.TimeFormats), timeformatStr, true); });
                        break;
                    case "-processfmt":
                        string processformatStr = GetNextNonArg("-processfmt", false);
                        ParseEnum<DumpBase.TimeFormats>("Time Format", processformatStr, 
                           () => { ProcessFormat = (DumpBase.TimeFormats)Enum.Parse(typeof(DumpBase.TimeFormats), processformatStr, true); });
                        break;
                    case "-nocmdline":
                        NoCmdLine = true;
                        break;
                    case "-threadcount":
                        ThreadCount = true;
                        break;
                    case "-noready":
                        NoReadyDetails = true;
                        break;
                    case "-nofrequency":
                        NoFrequencyDetails = true;
                        break;
                    case "-nopriority":
                        NoPriorityDetails = true;
                        break;
                    case "-normalize":
                        Normalize = true;
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
                    case TestRunIndexArg:   // -testrunindex
                    case TRIArg:           // -tri
                        string testRun = GetNextNonArg(TestRunIndexArg);
                        TestRunIndex = int.Parse(testRun, CultureInfo.InvariantCulture);
                        break;
                    case TestRunCountArg:  // -testruncount
                    case TRCArg:           // -trc
                        string testrunCount = GetNextNonArg(TestRunCountArg);
                        TestRunCount = int.Parse(testrunCount, CultureInfo.InvariantCulture);
                        break;
                    case "-showstack":
                    case "-ss":
                        ShowStack = true;
                        break;
                    case "-inherit":
                        Inherit = true;
                        break;
                    case "-showtime":
                        ShowTime = true;
                        break;
                    case "-totalmemory":
                    case "-tm":
                        TotalMemory = true;
                        break;
                    case "-scalingfactor":
                        string scalingFactor = GetNextNonArg("-scalingfactor");
                        ScalingFactor = int.Parse(scalingFactor, CultureInfo.InvariantCulture);
                        break;
                    case "-mindiffmb":
                        string minDiffMB = GetNextNonArg("-mindiffmb");
                        MinDiffMB = int.Parse(minDiffMB, CultureInfo.InvariantCulture);
                        break;
                    case "-globaldiffmb":
                        string globalDiffMB = GetNextNonArg("-globaldiffmb");
                        GlobalDiffMB = int.Parse(globalDiffMB, CultureInfo.InvariantCulture);
                        break;
                    case LastNDaysArg:   // -lastndays
                        string lastNDays = GetNextNonArg(LastNDaysArg);
                        LastNDays = ParseDouble(lastNDays);
                        break;
                    case SkipNTestsArg:  // -skipntests
                        string skipNTests = GetNextNonArg(SkipNTestsArg);
                        SkipNTests = int.Parse(skipNTests, CultureInfo.InvariantCulture);
                        break;
                    case "-properties":
                        Properties = GetNextNonArg("-properties");
                        break;
                    case "-diff":
                        ShowDiff = true;
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
                    case "power":
                        myCommand = DumpCommands.Power;
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
                    case "pmc":
                        myCommand = DumpCommands.PMC;
                        break;
                    case "lbr":
                        myCommand = DumpCommands.LBR;
                        break;
                    case "dns":
                        myCommand = DumpCommands.Dns;
                        break;
                    case "tcp":
                        myCommand = DumpCommands.TCP;
                        break;
                    case "objectref":
                        myCommand = DumpCommands.ObjectRef;
                        break;
                    case "-help":
                        delayedThrower = () =>
                        {
                            throw new NotSupportedException(HelpArg);
                        };
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
                    case DumpCommands.Power:
                        lret = PowerExamples + Environment.NewLine + PowerHelpString;
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
                    case DumpCommands.PMC:
                        lret = PMCExamples + Environment.NewLine + PMCHelpString;
                        break;
                    case DumpCommands.LBR:
                        lret = LBRExamples + Environment.NewLine + LBRHelpString;
                        break;
                    case DumpCommands.Dns:
                        lret = DnsExamples + Environment.NewLine + DnsHelpString;
                        break;
                    case DumpCommands.TCP:
                        lret = TcpExamples + Environment.NewLine + TcpHelpString;
                        break;
                    case DumpCommands.ObjectRef:
                        lret = ObjectRefExamples + Environment.NewLine + ObjectRefHelpString;
                        break;
                }
                return lret.TrimEnd(Environment.NewLine.ToCharArray());
            }
        }


        internal DumpBase myCurrentDumper = null;


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

            myCurrentDumper = null;

            try
            {
                switch (myCommand)
                {
                    case DumpCommands.Stats:
                        myCurrentDumper = new DumpStats()
                        {
                            CommandArguments = myConsoleCommandArgs,
                            ETLFile = decompressedETL,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            ShowFullFileName = ShowFullFileName,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,

                            Properties = Properties,
                            OneLine = OneLine,
                        };
                        break;
                    case DumpCommands.Versions:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        myCurrentDumper = new DumpModuleVersions()
                        {
                            CommandArguments = myConsoleCommandArgs,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            ShowFullFileName = ShowFullFileName,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,
                            ProcessNameFilter = ProcessNameFilter,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            NoCmdLine = NoCmdLine,
                            ShowTotal = ShowTotal,

                            Mode = ModulePrintMode,
                            ModuleFilter = ModuleFilter,
                            DllFilter = DllFilter,
                            MissingPdbFilter = MissingPdbFilter,
                            VersionFilter = VersionFilter,
                            TopN = TopN,
                            SortOrder = SortOrder,
                        };
                        break;
                    case DumpCommands.Process:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);

                        myCurrentDumper = new DumpProcesses
                        {
                            CommandArguments = myConsoleCommandArgs,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            ShowFullFileName = ShowFullFileName,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,
                            ProcessNameFilter = ProcessNameFilter,
                            // Stay consistent and allow -processfmt or -timefmt as time format string for process tree visualization
                            ProcessFormatOption = ProcessFormat ?? (TimeFormat == DumpBase.TimeFormats.Local ? null : TimeFormat),
                            CommandLineFilter = CmdLineFilter,
                            UsePrettyProcessName = UsePrettyProcessName,
                            NoCmdLine = NoCmdLine,
                            ShowDetails = ShowDetails,
                            SortOrder = SortOrder,
                            ShowTotal = ShowTotal,
                            Merge = Merge,
                            MinMaxDurationS = MinMaxDurationS,
                            NewProcessFilter = NewProcess,
                            Parent = Parent,
                            Session = Session,
                            User = User,
                            ShowFileOnLine = ShowFileOnLine,
                            ShowAllProcesses = ShowAllProcesses,
                            Crash = Crash,
                            ShowUser = ShowUser,
                            ZeroTimeMode = ZeroTimeMode,
                            ZeroTimeFilter = ZeroTimeFilter,
                            ZeroTimeProcessNameFilter = ZeroTimeProcessNameFilter,
                            MinMaxStart = MinMaxStart,
                        };
                        break;
                    case DumpCommands.CPU:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        myCurrentDumper = new DumpCPUMethod
                        {
                            CommandArguments = myConsoleCommandArgs,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            ShowFullFileName = ShowFullFileName,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,
                            ProcessNameFilter = ProcessNameFilter,
                            ProcessFormatOption = ProcessFormat,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,
                            NoCmdLine = NoCmdLine,

                            Merge = Merge,
                            TopN = TopN,
                            StackTagFilter = StackTagFilter,
                            MethodFilter = MethodFilter,
                            Session = Session,
                            TopNMethods = TopNMethods,
                            MinMaxCPUMs = MinMaxCPUMs,
                            MinMaxWaitMs = MinMaxWaitMs,
                            MinMaxReadyMs = MinMaxReadyMs,
                            NoReadyDetails = NoReadyDetails,
                            NoFrequencyDetails = NoFrequencyDetails,
                            NoPriorityDetails = NoPriorityDetails,
                            Normalize = Normalize,
                            MinMaxReadyAverageUs = MinMaxReadyAverageUs,
                            MinMaxCSwitch = MinMaxCSwitch,
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
                            ShowDetails = ShowDetails,
                            ShowDetailsOnMethodLine = ShowDetailsOnMethodLine,
                            ShowModuleInfo = ShowModuleInfo,
                            ShowModuleFilter = ShowModuleFilter,
                            ShowDriversOnly = ShowDriversOnly,
                            ZeroTimeMode = ZeroTimeMode,
                            ZeroTimeFilter = ZeroTimeFilter,
                            ZeroTimeProcessNameFilter = ZeroTimeProcessNameFilter,
                        };
                        break;
                    case DumpCommands.Disk:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        myCurrentDumper = new DumpDisk
                        {
                            CommandArguments = myConsoleCommandArgs,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            ShowFullFileName = ShowFullFileName,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
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
                            MinMaxReadSizeBytes = MinMaxReadSizeBytes,
                            MinMaxReadTimeS = MinMaxReadTimeS,
                            MinMaxWriteSizeBytes = MinMaxWriteSizeBytes,
                            MinMaxWriteTimeS = MinMaxWriteTimeS,
                            MinMaxTotalTimeS = MinMaxTotalTimeS,
                            MinMaxTotalSizeBytes = MinMaxTotalSizeBytes,
                            TopN = TopN,
                            TopNProcesses = TopNProcesses,
                            SortOrder = SortOrder,
                            FileOperationValue = FileOperation,
                            ReverseFileName = ReverseFileName,
                        };
                        break;
                    case DumpCommands.File:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        myCurrentDumper = new DumpFile
                        {
                            CommandArguments = myConsoleCommandArgs,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            ShowFullFileName = ShowFullFileName,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,
                            ProcessNameFilter = ProcessNameFilter,
                            ProcessFormatOption = ProcessFormat,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,
                            NoCmdLine = NoCmdLine,

                            ShowTotal = ShowTotal,
                            Merge = Merge,
                            DirectoryLevel = DirectoryLevel,
                            IsPerProcess = IsPerProcess,
                            FileNameFilter = FileNameFilter,
                            MinMaxReadSizeBytes = MinMaxReadSizeBytes,
                            MinMaxReadTimeS = MinMaxReadTimeS,
                            MinMaxWriteSizeBytes = MinMaxWriteSizeBytes,
                            MinMaxWriteTimeS = MinMaxWriteTimeS,
                            MinMaxTotalTimeS = MinMaxTotalTimeS,
                            MinMaxTotalSizeBytes = MinMaxTotalSizeBytes,
                            MinMaxTotalCount = MinMaxTotalCount,
                            TopN = TopN,
                            TopNProcesses = TopNProcesses,
                            FileOperationValue = FileOperation,
                            SortOrder = SortOrder,
                            ShowAllFiles = ShowAllFiles,
                            ShowDetails = ShowDetails,
                            Session = Session,
                            ShowModuleInfo = ShowModuleInfo,
                            ShowModuleFilter = ShowModuleFilter,
                            ReverseFileName = ReverseFileName,
                        };
                        break;
                    case DumpCommands.Power:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        myCurrentDumper = new DumpPower
                        {
                            CommandArguments = myConsoleCommandArgs,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            ShowFullFileName = ShowFullFileName,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,

                            ShowDetails = ShowDetails,
                            ShowDiff = ShowDiff,
                        };
                        break;
                    case DumpCommands.Exceptions:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        myCurrentDumper = new DumpExceptions
                        {
                            CommandArguments = myConsoleCommandArgs,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            ShowFullFileName = ShowFullFileName,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,
                            ProcessNameFilter = ProcessNameFilter,
                            ProcessFormatOption = ProcessFormat,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,
                            SortOrder = SortOrder,
                            ShowModuleInfo = ShowModuleInfo,
                            ShowModuleFilter = ShowModuleFilter,

                            Session = Session,
                            ShowTime  = ShowTime,
                            TypeFilter = TypeFilter,
                            MessageFilter = MessageFilter,
                            StackFilter = StackFilter,
                            ShowStack = ShowStack,
                            CutStackMin = CutStackMin,
                            CutStackMax = CutStackMax,
                            MaxMessage = MaxMessage,
                            NoCmdLine = NoCmdLine,
                            MinMaxExTimeS = MinMaxExTimeS,
                            ZeroTimeMode = ZeroTimeMode,
                            ZeroTimeFilter = ZeroTimeFilter,
                            ZeroTimeProcessNameFilter = ZeroTimeProcessNameFilter,
                            ShowDetails = ShowDetails,
                        };
                        break;
                    case DumpCommands.Memory:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        myCurrentDumper = new DumpMemory
                        {
                            CommandArguments = myConsoleCommandArgs,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            ShowFullFileName = ShowFullFileName,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,
                            ProcessNameFilter = ProcessNameFilter,
                            ProcessFormatOption = ProcessFormat,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,
                            ShowModuleInfo = ShowModuleInfo,
                            ShowModuleFilter = ShowModuleFilter,

                            ShowTotal = ShowTotal,
                            Session = Session,
                            TopN = TopN,
                            SortOrder = SortOrder,
                            MinDiffMB = MinDiffMB,
                            GlobalDiffMB = GlobalDiffMB,
                            TotalMemory = TotalMemory,
                            MinMaxWorkingSetMiB = MinMaxWorkingSetMiB,
                            MinMaxCommitMiB = MinMaxCommitMiB,
                            MinMaxSharedCommitMiB = MinMaxSharedCommitMiB,
                            MinMaxWorkingsetPrivateMiB = MinMaxWorkingsetPrivateMiB,
                            NoCmdLine = NoCmdLine,
                            ShowDetails = ShowDetails,
                           
                        };
                        break;
                    case DumpCommands.ThreadPool:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        myCurrentDumper = new DumpThreadPool
                        {
                            CommandArguments = myConsoleCommandArgs,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            ShowFullFileName = ShowFullFileName,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            TimeFormatOption = TimeFormat,
                            ProcessNameFilter = ProcessNameFilter,
                            ProcessFormatOption = ProcessFormat,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,

                            NoCmdLine = NoCmdLine,
                        };
                        break;
                    case DumpCommands.Mark:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        myCurrentDumper = new DumpMarks
                        {
                            CommandArguments = myConsoleCommandArgs,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            ShowFullFileName = ShowFullFileName,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
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

                            MinMaxMarkDiffTime = MinMaxMarkDiffTime,
                        };
                        break;

                    case DumpCommands.TestRuns:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        myCurrentDumper = new TestRunDumper
                        {
                            Recursive = mySearchOption,
                            Directories = FileOrDirectoryQueries,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
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
                    case DumpCommands.PMC:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        myCurrentDumper = new DumpPMC
                        {
                            CommandArguments = myConsoleCommandArgs,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            ShowFullFileName = ShowFullFileName,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            ProcessNameFilter = ProcessNameFilter,
                            NoCmdLine = NoCmdLine,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,

                            NoCounters = NoCounters,
                        };
                        break;
                    case DumpCommands.LBR:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        myCurrentDumper = new DumpLBR
                        {
                            CommandArguments = myConsoleCommandArgs,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            ShowFullFileName = ShowFullFileName,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            ProcessNameFilter = ProcessNameFilter,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,

                            NoCmdLine = NoCmdLine,
                            TopN = TopN,
                            MethodFilter = MethodFilter,
                            TopNMethods = TopNMethods,
                            MethodFormatter = new MethodFormatter(NoDll, NoArgs, MethodCutStart, MethodCutLength),
                            ShowCaller = ShowCaller,
                            ScalingFactor = ScalingFactor,
                            MinMaxCount = MinMaxCount,
                        };
                        break;
                    case DumpCommands.Dns:
                        ThrowIfFileOrDirectoryIsInvalid(FileOrDirectoryQueries);
                        myCurrentDumper = new DumpDns
                        {
                            CommandArguments = myConsoleCommandArgs,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            ShowFullFileName = ShowFullFileName,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            ProcessNameFilter = ProcessNameFilter,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,
                            TimeFormatOption = TimeFormat,
                            
                            NoCmdLine = NoCmdLine,
                            TopN = TopN,
                            TopNDetails = TopNDetails,
                            SortOrder = SortOrder,
                            ShowDetails = ShowDetails,
                            ShowAdapter = ShowAdapter,
                            ShowReturnCode = ShowReturnCode,
                            DnsQueryFilter = DnsQueryFilter,
                            MinMaxTimeMs = MinMaxTimeMs,
                            MinMaxTotalTimeMs = MinMaxTotalTimeMs,
                            ShowProcess = ShowProcess,
                        };
                        break;
                    case DumpCommands.TCP:
                        myCurrentDumper = new DumpTcp
                        {
                            CommandArguments = myConsoleCommandArgs,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            ShowFullFileName = ShowFullFileName,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            ProcessNameFilter = ProcessNameFilter,
                            ProcessFormatOption = ProcessFormat,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,
                            TimeFormatOption = TimeFormat,

                            ShowTotal = ShowTotal,
                            NoCmdLine = NoCmdLine,
                            TopN = TopN,
                            ShowDetails = ShowDetails,
                            TopNRetrans = TopNRetrans,
                            IpPortFilter = IpPortFilter,
                            SortOrder = SortOrder,
                            RetransSortOrder = RetransSortOrder,
                            MinMaxRetransDelayMs = MinMaxRetransDelayMs,
                            MinMaxRetransBytes = MinMaxRetransBytes,
                            MinMaxSentBytes = MinMaxSentBytes,
                            MinMaxReceivedBytes = MinMaxReceivedBytes,
                            MinMaxConnectionDurationS = MinMaxConnectionDurationS,
                            ShowRetransmit = ShowRetransmit,
                            OnlyClientRetransmit = OnlyClientRetransmit,
                            MinMaxRetransCount = MinMaxRetransCount,
                            TcbFilter = TcbFilter,
                            ZeroTimeMode = ZeroTimeMode,
                            ZeroTimeFilter = ZeroTimeFilter,
                            ZeroTimeProcessNameFilter = ZeroTimeProcessNameFilter,
                        };
                        break;
                    case DumpCommands.ObjectRef:
                        myCurrentDumper = new DumpObjectRef
                        {
                            CommandArguments = myConsoleCommandArgs,
                            FileOrDirectoryQueries = FileOrDirectoryQueries,
                            ShowFullFileName = ShowFullFileName,
                            Recursive = mySearchOption,
                            TestsPerRun = TestsPerRun,
                            SkipNTests = SkipNTests,
                            TestRunIndex = TestRunIndex,
                            TestRunCount = TestRunCount,
                            LastNDays = LastNDays,
                            MinMaxMsTestTimes = MinMaxMsTestTimes,
                            CSVFile = CSVFile,
                            NoCSVSeparator = NoCSVSeparator,
                            ProcessNameFilter = ProcessNameFilter,
                            ProcessFormatOption = ProcessFormat,
                            CommandLineFilter = CmdLineFilter,
                            NewProcessFilter = NewProcess,
                            UsePrettyProcessName = UsePrettyProcessName,
                            TimeFormatOption = TimeFormat,

                            TopN = TopN,
                            ObjectNameFilter = ObjectNameFilter,
                            TypeFilter = TypeFilter,
                            StackFilter = StackFilter,
                            DestroyStackFilter = DestroyStackFilter,
                            CreateStackFilter = CreateStackFilter,
                            ObjectFilter = ObjectFilter,
                            ViewBaseFilter = ViewBaseFilter,
                            HandleFilter = HandleFilter,
                            MinMaxDurationS = MinMaxDurationS,
                            ShowStack = ShowStack,
                            ShowRef = ShowRef,
                            Leak = Leak,
                            MultiProcess = MultiProcess,
                            Inherit = Inherit,
                            Overlapped = Overlapped,
                            RelatedProcessFilter = RelatedProcessFilter,
                            Map = Map,
                            PtrInMap = PtrInMap,
                            MinMaxMapSize = MinMaxMapSize,
                            MinMaxId = MinMaxId,
                            NoCmdLine = NoCmdLine,
                            ShowTotal = ShowTotal,
                        };
                        break;
                    case DumpCommands.None:
                        throw new NotSupportedException("-dump needs an argument what you want to dump.");
                    case DumpCommands.Allocations:
                        break;
                    default:
                        throw new NotSupportedException($"The dump command {myCommand} is not implemented.");
                }

                if( PreloadedData != null)
                {
                    myCurrentDumper.myPreloadedTests = PreloadedData; 
                }

                myCurrentDumper.Execute();
            }
            finally
            {
                myCurrentDumper?.Dispose();
            }

        }


        /// <summary>
        /// Return valid sort orders depending on used command and context. 
        /// Context can be SortByContext, SortRetransmitContext
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Array of valid sort order for current command</returns>
        SortOrders[] GetValidSortOrders(string context)
        {

            return myCommand switch
            {
                DumpCommands.Process => DumpProcesses.ValidSortOrders,
                DumpCommands.CPU => DumpCPUMethod.ValidCPUSortOrders,
                DumpCommands.Disk => DumpDisk.ValidSortOrders,
                DumpCommands.File => DumpFile.ValidSortOrders,
                DumpCommands.Memory => DumpMemory.ValidSortOrders,
                DumpCommands.Exceptions => DumpExceptions.ValidSortOrders,
                DumpCommands.Dns => DumpDns.ValidSortOrders,
                DumpCommands.TCP => context switch
                {
                    SortRetransmitContext => DumpTcp.SupportRetransmitSortOrders,
                    _ => DumpTcp.SupportedSortOrders,
                },

                _ => (SortOrders[]) Enum.GetValues(typeof(SortOrders)),
            };
        }

        /// <summary>
        /// Make method filtering easier by omitting for module RVA names the *.dll+* by allowing you to write *.dll and *.sys 
        /// </summary>
        /// <param name="methodFilter"></param>
        /// <returns></returns>
        private static string ReplaceMethodFilterAliases(string methodFilter)
        {

            Dictionary<string, string> aliases = new(StringComparer.OrdinalIgnoreCase)
            {
                // Since we support RVA addresses make filtering easier
                { "*.dll", "*.dll+*" },         
                { "*.sys", "*.sys+*" },         
                { "*.dll;*.sys", "*.dll+*;*.sys+*" }, 
                { "*.sys;*.dll", "*.dll+*;*.sys+*" }, 
            };

            if (aliases.TryGetValue(methodFilter ?? "", out string replaced))
            {
                return replaced;
            }
            else
            {
                return methodFilter;
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
