//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.Commands;
using ETWAnalyzer.Configuration;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Modules;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ETWAnalyzer.Commands.DumpCommand;
using ETWAnalyzer.TraceProcessorHelpers;

namespace ETWAnalyzer.EventDump
{
    class DumpCPUMethod : DumpFileDirBase<DumpCPUMethod.MatchData>
    {
        /// <summary>
        /// -topn Processes are sorted by CPU totals, Take the top n processes for output which is a good default. 
        /// If you need different filters you can omit that filter simply
        /// </summary>
        public SkipTakeRange TopN { get; internal set; } = new SkipTakeRange();

        /// <summary>
        /// When not in process total mode select with -topnmethods the number of methods to priint
        /// </summary>
        public SkipTakeRange TopNMethods { get; internal set; } = new SkipTakeRange();

        /// <summary>
        /// Key is filter string, value is filter method 
        /// </summary>

        public KeyValuePair<string, Func<string, bool>> MethodFilter { get; internal set; }


        /// <summary>
        /// Key is filter string, value is filter method 
        /// </summary>
        public KeyValuePair<string, Func<string,bool>> StackTagFilter { get; internal set; }

        /// <summary>
        /// When true we print only process total CPU information
        /// </summary>
        bool IsProcessTotalMode
        {
            get => MethodFilter.Key == null && TopNMethods.TakeN == int.MaxValue && StackTagFilter.Key == null;
        }

        /// <summary>
        /// Show ThreadCount of all printed methods. In CSV file this is always present
        /// </summary>
        public bool ThreadCount { get; internal set; }

        /// <summary>
        /// -FirstLastDuration switch is present. use 
        /// </summary>
        public bool FirstLastDuration { get; internal set; }

        /// <summary>
        /// Do not indent by process but show process name on same line as method
        /// </summary>
        public bool ShowDetailsOnMethodLine { get; internal set; }  

        /// <summary>
        /// Omit command line in console and if configured in CSV output to reduce CSV size which is one of the largest string per line
        /// </summary>
        public bool NoCmdLine { get; internal set; }

        /// <summary>
        /// Show module file name and version
        /// </summary>
        public bool ShowModuleInfo { get; internal set; }

        /// <summary>
        /// Only show info from Configuration\WellKnownDrivers.json
        /// </summary>
        public bool ShowDriversOnly { get; internal set; }

        /// <summary>
        /// Method sort order
        /// </summary>
        public DumpCommand.SortOrders SortOrder { get; internal set; }

        /// <summary>
        /// Merge CPU consumption of all processes with the same pid accross multiple ETL files which can e.g. 
        /// originate from a long term tracing run.
        /// </summary>
        public bool Merge { get; internal set; }

        /// <summary>
        /// When present print method totals
        /// </summary>
        public TotalModes ShowTotal { get; internal set; }

        /// <summary>
        /// Used by -FirstLastDuration x
        /// </summary>
        public TimeFormats? FirstTimeFormat { get; internal set; }

        /// <summary>
        /// Used by -FirstLastDuration x y
        /// </summary>
        public TimeFormats? LastTimeFormat { get; internal set; }

        /// <summary>
        /// Configured by command line switches -includedll, -includeargs 
        /// </summary>
        public MethodFormatter MethodFormatter { get; internal set; } = new MethodFormatter();

        /// <summary>
        /// Only show methods/stacktags where the CPU time in ms matches the range. Default is everything.
        /// </summary>
        public MinMaxRange<int> MinMaxCPUMs { get; internal set; } = new MinMaxRange<int>();

        /// <summary>
        /// Only show methods/stacktags where the Wait time in ms time matches the range. Default is everything.
        /// </summary>
        public MinMaxRange<int> MinMaxWaitMs { get; internal set; } = new MinMaxRange<int>();

        /// <summary>
        /// Only show methods/stacktags where the first occurrence in seconds  matches the range. Default is everything.
        /// </summary>
        public MinMaxRange<double> MinMaxFirstS { get; internal set; } = new MinMaxRange<double>();
        /// <summary>
        /// Only show methods/stacktags where the last occurrence in seconds matches the range. Default is everything.
        /// </summary>
        public MinMaxRange<double> MinMaxLastS { get; internal set; } = new MinMaxRange<double>();

        /// <summary>
        /// Only show methods/stacktags where the Last-First occurrence matches the range. Default is everything.
        /// This is not to be confused with a method runtime! The input of this data is CPU sampling and Context Switch events.
        /// The method might be called 1 100 or 10000 times. The duration only shows the range where the method CPU sampling/Context Switch data 
        /// did show up in the trace timeline. This value can still be useful to e.g. estimate the runtime of asynchronous methods, or to check
        /// if it was called more than once in a trace if e.g. the ctor which is supposed to run a short time is several seconds apart. 
        /// </summary>
        public MinMaxRange<double> MinMaxDurationS { get; internal set; } = new MinMaxRange<double>();

        /// <summary>
        /// State flag for CSV output
        /// </summary>
        bool myCSVHeaderPrinted = false;

        /// <summary>
        /// Cache results of filters
        /// </summary>
        readonly Dictionary<string, bool> myMethodFilterResultsCache = new();

        /// <summary>
        /// Cache results of filters
        /// </summary>
        readonly Dictionary<string, bool> myFirstLastMethodFilterResultsCache = new();

        /// <summary>
        /// Needed for StackDepth sorting of methods where we determine the highest CPU consuming method with stack depth 0.
        /// Then we use for all methods > myMaxCPUSortData the metric CPU/exp(StackDepth), for the rest CPU/exp(StackDepth+10) to ensure
        /// that the sort to some extent the methods consuming most CPU in by value order while other methods with less CPU but have a
        /// shallow stack are not coming first. 
        /// </summary>
        uint myMaxCPUSortData;

        /// <summary>
        /// Flag to display total CPU header once
        /// </summary>
        bool myCPUTotalHeaderShown = false;

        /// <summary>
        /// Total CPU Header CPU column width
        /// </summary>
        const int CPUTotal_CPUColumnWidth = 9; 

        /// <summary>
        /// Total CPU Header Process Name column width
        /// </summary>
        const int CPUTotal_ProcessNameWidth = -20;


        public class MatchData
        {
            internal int StackDepth;

            public string TestName { get; set; }
            public DateTime PerformedAt { get; set; }
            public int DurationInMs { get; set; }
            public uint CPUMs { get; set; }
            public uint WaitMs { get; set; }
            public int Threads { get; set; }
            public string BaseLine { get; set; }
            public string ProcessAndPid { get; set; }
            public string Method { get; internal set; }
            public string SourceFile { get; internal set; }
            public ETWProcess Process { get; internal set; }
            public float FirstLastCallDurationS { get; internal set; }
            public DateTimeOffset FirstCallTime { get; internal set; }
            public DateTimeOffset LastCallTime { get; internal set; }

            public ModuleDefinition Module { get; internal set; }
            public Driver Driver { get; internal set; }
            public string ModuleName { get; internal set; }
            public DateTimeOffset SessionStart { get; internal set; }

            public double ZeroTimeS { get; internal set; }
            

            public override string ToString()
            {
                return $"{TestName} {ProcessAndPid} {CPUMs}ms {WaitMs}ms {Method} {FirstCallTime} {LastCallTime} {SourceFile}";
            }
        }

        public override List<MatchData> ExecuteInternal()
        {
            var testsOrderedByTime = GetTestRuns(true, SingleTestCaseFilter, TestFileFilter);
            WarnIfNoTestRunsFound(testsOrderedByTime);

            List<MatchData> matches = new();

            foreach(var test in testsOrderedByTime)
            {
                using (test.Value) // Release deserialized ETWExtract to keep memory footprint in check
                {
                    foreach (TestDataFile file in test.Value.Files.Where(TestFileFilter))
                    {
                        if (IsProcessTotalMode) // Print total CPU stats
                        {
                            AddAndPrintTotalStats(matches, file);
                        }
                        else  // Display / store later in CSV
                        {
                            double zeroS = GetZeroTimeInS(file.Extract);
                            // by default print only methods
                            if( (MethodFilter.Key == null && StackTagFilter.Key == null) || MethodFilter.Key != null)
                            {
                                AddPerMethodStats(matches, file, zeroS);
                            }

                            // If we have Stacktag filters then print them as well
                            if (StackTagFilter.Key != null)
                            {
                                AddStackTagStats(matches, file, zeroS);
                            }
                        }
                    }
                }
            }

            List<MatchData> printed = matches;

            // Total matches are printed while files are loaded 
            // the rest will be shown/printed to CSV file here
            if (Merge || !IsProcessTotalMode || IsCSVEnabled)
            {
                printed = PrintMatches(matches);
            }

            return IsProcessTotalMode ? matches : printed;
        }

        private void AddStackTagStats(List<MatchData> matches, TestDataFile file, double zeroTimeS)
        {
            if (StackTagFilter.Key == null)
            {
                return;
            }

            if (file.Extract?.SummaryStackTags?.Stats == null)
            {
                ColorConsole.WriteWarning($"Warning: File {file.JsonExtractFileWhenPresent} contains no StackTag data.");
                return;
            }

            AddMatchingStackTags(matches, file, file.Extract.SummaryStackTags, zeroTimeS);
            AddMatchingStackTags(matches, file, file.Extract.SpecialStackTags, zeroTimeS);
        }

        private void AddMatchingStackTags(List<MatchData> matches, TestDataFile file, IProcessStackTags tags, double zeroTimeS)
        {
            if( tags == null)
            {
                return;
            }

            Func<IStackTagDuration, bool> stackTagFilter = (stacktagDuration) => IsStackTagMatching(stacktagDuration, file.Extract.SessionStart, zeroTimeS);

            foreach (var perProcess in tags.Stats)
            {
                if (!IsMatchingProcessAndCmdLine(file, perProcess.Key))
                {
                    continue;
                }

                foreach (var stacktag in perProcess.Value.Where(stackTagFilter))
                {
                    ETWProcess process = file.FindProcessByKey(perProcess.Key);

                    if (process != null)
                    {

                        matches.Add(new MatchData
                        {
                            TestName = file.TestName,
                            PerformedAt = file.PerformedAt,
                            DurationInMs = file.DurationInMs,
                            Method = stacktag.Stacktag.CutMinMax(MethodFormatter.MethodCutStart, MethodFormatter.MethodCutLength),
                            CPUMs = (uint)stacktag.CPUInMs,
                            WaitMs = (uint)stacktag.WaitDurationInMs,
                            BaseLine = file.Extract.MainModuleVersion != null ? file.Extract.MainModuleVersion.ToString() : "",
                            ProcessAndPid = process.GetProcessWithId(UsePrettyProcessName),
                            SourceFile = file.JsonExtractFileWhenPresent,
                            FirstCallTime = stacktag.FirstOccurence.AddSeconds(-1.0d* zeroTimeS),
                            LastCallTime =  stacktag.FirstOccurence.AddSeconds(-1.0d* zeroTimeS) + stacktag.FirstLastOccurenceDuration,
                            FirstLastCallDurationS = (float)stacktag.FirstLastOccurenceDuration.TotalSeconds,
                            SessionStart = file.Extract.SessionStart,
                            Process = process,
                            ZeroTimeS = zeroTimeS,
                        });
                    }
                }
            }
        }

        private void AddPerMethodStats(List<MatchData> matches, TestDataFile file, double zeroTimeS)
        {
            if( file.Extract?.CPU?.PerProcessMethodCostsInclusive == null)
            {
                ColorConsole.WriteWarning($"Warning: File {file.JsonExtractFileWhenPresent} contains no CPU data.");
                return;
            }

            Func<MethodCost, bool> methodFilter = (cost) => IsMethodMatching(cost, zeroTimeS);

            foreach (var perProcess in file.Extract.CPU.PerProcessMethodCostsInclusive.MethodStatsPerProcess)
            {
                if ( !IsMatchingProcessAndCmdLine(file, perProcess.Process))
                {
                    continue;
                }

                foreach (var methodCost in perProcess.Costs.OrderBy(x => x.FirstOccurenceInSecond).Where(methodFilter))
                {
                    ETWProcess process = file.FindProcessByKey(perProcess.Process);

                    if (process != null)
                    {
                        
                        DateTimeOffset lastCallTime = file.Extract.ConvertTraceRelativeToAbsoluteTime (methodCost.LastOccurenceInSecond  - (float) zeroTimeS );
                        DateTimeOffset firstCallTime = file.Extract.ConvertTraceRelativeToAbsoluteTime(methodCost.FirstOccurenceInSecond - (float) zeroTimeS );
                        ModuleDefinition module = null;
                        Driver driver = null;

                        if (ShowModuleInfo && file.Extract.Modules != null )
                        {
                            driver = Drivers.Default.TryGetDriverForModule(methodCost.Module);
                            module = file.Extract.Modules.Modules.Where(m => m.ModuleName == methodCost.Module && m.Processes.Any(x => 
                            {
                                // device drivers live in the System process we use therefore any process which has it loaded because there can only be one
                                if (m.ModuleName.EndsWith(".sys", StringComparison.OrdinalIgnoreCase) || m.ModuleName == "ntoskrnl.exe")
                                {
                                    return true;
                                }
                                else
                                {
                                    return x.Equals(process);
                                }
                            } 
                            )).FirstOrDefault();
                        }

                        matches.Add(new MatchData
                        {
                            TestName = file.TestName,
                            PerformedAt = file.PerformedAt,
                            DurationInMs = file.DurationInMs,
                            Method = MethodFormatter.Format(methodCost.Method),
                            ModuleName = methodCost.Module,
                            CPUMs = methodCost.CPUMs,
                            WaitMs = methodCost.WaitMs,
                            Threads = methodCost.Threads,
                            BaseLine = file.Extract.MainModuleVersion != null ? file.Extract.MainModuleVersion.ToString() : "",
                            ProcessAndPid = process.GetProcessWithId(UsePrettyProcessName),
                            SourceFile = file.JsonExtractFileWhenPresent,
                            FirstCallTime = firstCallTime,
                            LastCallTime = lastCallTime,
                            FirstLastCallDurationS = methodCost.LastOccurenceInSecond - methodCost.FirstOccurenceInSecond,
                            SessionStart = file.Extract.SessionStart,
                            StackDepth = methodCost.DepthFromBottom,
                            Process = process,
                            Module = module,
                            Driver = driver,
                            ZeroTimeS = zeroTimeS,
                        });
                    }
                }
            }
        }

        private void AddAndPrintTotalStats(List<MatchData> matches, TestDataFile file)
        {
            if (file.Extract?.CPU?.PerProcessCPUConsumptionInMs == null)
            {
                ColorConsole.WriteWarning($"Warning: File {file.JsonExtractFileWhenPresent} contains no CPU data.");
                return;
            }

            bool ProcessFilter(KeyValuePair<ProcessKey, uint> procCPU)
            {
                bool lret = true;
                if( !IsMatchingProcessAndCmdLine(file, procCPU.Key))
                {
                    lret = false;
                }

                if (lret)
                {
                    ETWProcess process = ProcessExtensions.FindProcessByKey(file, procCPU.Key);
                    if (process == null)
                    {
                        lret = false;
                    }
                    else
                    {
                        if( process.ProcessID == 0) // exclude idle process
                        {
                            lret = false;
                        }
                    }

                }

                if( lret && !MinMaxCPUMs.IsWithin( (int) procCPU.Value) )
                {
                    lret = false;
                }
                return lret;
            }


            List< KeyValuePair<ProcessKey, uint>> filtered = file.Extract.CPU.PerProcessCPUConsumptionInMs.Where(ProcessFilter).SortAscendingGetTopNLast(x => x.Value, null, TopN).ToList();

            PrintTotalCPUHeaderOnce();

            if (!Merge)
            {
                string CpuString = "";
                if( ShowTotal != TotalModes.None)
                {
                    long cpuTotal = filtered.Select(x => (long) x.Value).Sum();
                    CpuString = $" [green]CPU {cpuTotal:N0} ms[/green] ";
                    CpuString = $"{CpuString,10}";
                }
                ColorConsole.WriteEmbeddedColorLine($"{file.PerformedAt,-22} {CpuString}{Path.GetFileNameWithoutExtension(file.JsonExtractFileWhenPresent)} {file.Extract.MainModuleVersion}");
            }

            // When printing data with -topN dd we sort by CPU ascending because in a shell console window we do not want
            // to scroll back for the highest values
            // Then we skip the first N entries to show only the last TopNSafe results which are the highest values

            foreach (var cpu in filtered)
            {
                ETWProcess process = ProcessExtensions.FindProcessByKey(file, cpu.Key);

                matches.Add(new MatchData
                {
                    TestName = file.TestName,
                    PerformedAt = file.PerformedAt,
                    DurationInMs = file.DurationInMs,
                    CPUMs = cpu.Value,
                    BaseLine = file.Extract.MainModuleVersion != null ? file.Extract.MainModuleVersion.ToString() : "",
                    Method = "",
                    ProcessAndPid = process.GetProcessWithId(UsePrettyProcessName),
                    SourceFile = file.JsonExtractFileWhenPresent,
                    Process = process,
                });

                if(!IsCSVEnabled && !Merge && !(ShowTotal == TotalModes.Total) )
                {
                    PrintCPUTotal(cpu.Value, process, Path.GetFileNameWithoutExtension(file.FileName), file.Extract.SessionStart);
                }
            }
        }

        void PrintTotalCPUHeaderOnce()
        {
            if (!IsCSVEnabled)
            {
                string cpuHeaderName = "CPU";
                string processHeaderName = "Process Name";

                if (myCPUTotalHeaderShown == false)
                {
                    ColorConsole.WriteEmbeddedColorLine($"\t[Green]{cpuHeaderName.WithWidth(CPUTotal_CPUColumnWidth)} ms[/Green] [yellow]{processHeaderName.WithWidth(CPUTotal_ProcessNameWidth)}[/yellow]");
                    myCPUTotalHeaderShown = true;
                }
            }
        }

        void PrintCPUTotal(long cpu, ETWProcess process, string  sourceFile, DateTimeOffset sessionStart)
        {
            string fileName = this.Merge ? $" {sourceFile}" : "";

            string cpuStr = cpu.ToString("N0");

            if (NoCmdLine)
            {
                ColorConsole.WriteEmbeddedColorLine($"\t[Green]{cpuStr,CPUTotal_CPUColumnWidth} ms[/Green] [yellow]{process.GetProcessWithId(UsePrettyProcessName),CPUTotal_ProcessNameWidth}{GetProcessTags(process, sessionStart)}[/yellow]{fileName}");
            }
            else
            {
                ColorConsole.WriteEmbeddedColorLine($"\t[Green]{cpuStr,CPUTotal_CPUColumnWidth} ms[/Green] [yellow]{process.GetProcessWithId(UsePrettyProcessName),CPUTotal_ProcessNameWidth}{GetProcessTags(process, sessionStart)}[/yellow] {process.CommandLineNoExe}", ConsoleColor.DarkCyan, true);
                Console.WriteLine($" {fileName}");
            }
        }

        private List<MatchData> PrintMatches(List<MatchData> matches)
        {
            List<MatchData> printed = new();

            if(IsProcessTotalMode)
            {
                ProcessTotalMatches(matches);
            }
            else  // per process per method summary
            {
                ProcessPerMethodMatches(matches, printed);
            }

            return printed;
        }


        private void ProcessPerMethodMatches(List<MatchData> matches, List<MatchData> printed)
        {
            string threadCountHeader = null;
            if (ThreadCount)
            {
                threadCountHeader = "ThreadCount ";
            }

            string firstlastDurationHeader = null;
            Func<MatchData, string> firstLastFormatter = (data) => "";

            GetHeaderFormatter(ref firstlastDurationHeader, ref firstLastFormatter);

            // Show header only when we do not print totals or no per method totals
            if (!IsCSVEnabled && (ShowTotal == TotalModes.None || ShowTotal == TotalModes.Method))
            {
                ColorConsole.WriteEmbeddedColorLine($"      [green]CPU ms[/green]     [yellow]Wait ms[/yellow] {threadCountHeader}{firstlastDurationHeader}Method");
            }

            long overallCPUTotal = 0;
            long overallWaitTotal = 0;

            foreach (var timeGroup in matches.GroupBy(x => $"{x.PerformedAt} {Path.GetFileNameWithoutExtension(x.SourceFile)}"))
            {
                // order processes by CPU ascending by their total CPU
                // then take the TopN processes and reverse the list so that we display on console
                // the process with highest CPU as last so we do not need to scroll upwards in the output in the optimal scenario
                IGrouping<string, MatchData>[] subGroup = timeGroup.GroupBy(x => x.ProcessAndPid).OrderByDescending(x => x.Sum(x => x.CPUMs)).Take(TopN.TakeN).Reverse().ToArray();

                if (!IsCSVEnabled)
                {
                    ColorConsole.Write($"{timeGroup.Key}", ConsoleColor.DarkCyan);

                    // calculate totals at file level, but respect -topn -topnmethod filters for total calculation
                    if (ShowTotal != TotalModes.None)
                    {
                        MatchData[] filteredSubItems = subGroup.SelectMany(x => x.SortAscendingGetTopNLast(SortByValue, SortByValueStatePreparer, TopNMethods)).ToArray();
                        long fileCPUTotal = filteredSubItems.Sum(x => x.CPUMs);
                        long fileWaitTotal = filteredSubItems.Sum(x => x.WaitMs);
                        overallCPUTotal += fileCPUTotal;
                        overallWaitTotal += fileWaitTotal;
                        string total = $" [green]CPU {fileCPUTotal} ms[/green] Wait [yellow]{fileWaitTotal} ms[/yellow] [magenta]Total {fileCPUTotal + fileWaitTotal} ms[/magenta]";
                        ColorConsole.WriteEmbeddedColorLine(total, null, true);
                    }
                    ColorConsole.WriteLine($" {timeGroup.First().BaseLine}");
                }

                if (ShowTotal == TotalModes.Total)
                {
                    // Skip process details
                    continue;
                }


                foreach (var processGroup in subGroup)
                {
                    
                    // when we display all we show the highest CPU last in console (ascending) 
                    MatchData[] sorted = processGroup.SortAscendingGetTopNLast(SortByValue, SortByValueStatePreparer, TopNMethods);

                    if (String.IsNullOrEmpty(CSVFile) && !ShowDetailsOnMethodLine)
                    {
                        long cpuTotal = sorted.Sum(x => x.CPUMs);
                        long waitTotal = sorted.Sum(x => x.WaitMs);

                        string totals = (ShowTotal == TotalModes.Process || ShowTotal == TotalModes.Method) ?
                                            $" [green]CPU {cpuTotal} ms[/green] [yellow]Wait: {waitTotal} ms[/yellow][magenta] Total: {cpuTotal + waitTotal} ms[/magenta]"
                                            : "";

                        string cmdLine = NoCmdLine ? "" : processGroup.First().Process.CommandLineNoExe;
                        MatchData current = processGroup.First();

                        ColorConsole.WriteEmbeddedColorLine($"   [grey]{processGroup.Key}{GetProcessTags(processGroup.First().Process, current.SessionStart.AddSeconds(current.ZeroTimeS))}[/grey]{totals} {cmdLine}", ConsoleColor.DarkCyan, false);

                    }

                    if (ShowTotal == TotalModes.Process)
                    {
                        // Skip method details
                        continue;
                    }

                    printed.AddRange(sorted);
                    string lastModuleInfoLine = null;
                    foreach (MatchData match in sorted)
                    {

                        if (IsCSVEnabled)
                        {
                            ProcessPerMethodCSVLine(match, match.FirstLastCallDurationS);
                        }
                        else
                        {
                            string threadCount = "";
                            string process = "";

                            if (ThreadCount)
                            {
                                threadCount = match.Threads > 0 ? $"#{match.Threads,-3} " : "     "; // for stacktags we do not record threadcount infos!
                            }
                            if (ShowDetailsOnMethodLine)
                            {
                                process = " " + match.ProcessAndPid;
                            }

                            ColorConsole.WriteEmbeddedColorLine($"  [Green]{match.CPUMs,7} ms[/Green] [yellow]{match.WaitMs,8} ms[/yellow] {threadCount}{firstLastFormatter(match)}{match.Method}[darkyellow]{process}[/darkyellow] ", null, true);

                            if (ShowModuleInfo)
                            {
                                string currentModuleLine = null;
                                if (match.Driver != null)
                                {
                                    currentModuleLine += $"[yellow]Vendor: {match?.Driver?.Company} {match?.Driver?.Category}[/yellow] ";

                                }
                                if (match.Module != null && !ShowDriversOnly)
                                {
                                    currentModuleLine += $"[blue]{GetModuleString(match.Module)}[/blue]";
                                }

                                // do not print the same string on subsequent lines
                                if (currentModuleLine != lastModuleInfoLine)
                                {
                                    ColorConsole.WriteEmbeddedColorLine(currentModuleLine, null, true);
                                    lastModuleInfoLine = currentModuleLine;
                                }
                            }

                            Console.WriteLine();
                        }
                    }
                }
            }

            if (ShowTotal != TotalModes.None && !IsCSVEnabled)
            {
                ColorConsole.WriteEmbeddedColorLine($"[magenta]Total {overallCPUTotal + overallWaitTotal:N0} ms[/magenta] [green]CPU {overallCPUTotal:N0} ms[/green] [yellow]Wait {overallWaitTotal:N0} ms[/yellow]");
            }
        }

        private void GetHeaderFormatter(ref string firstlastDurationHeader, ref Func<MatchData, string> firstLastFormatter)
        {
            if (FirstLastDuration)
            {
                switch (FirstTimeFormat)
                {
                    case null:
                        firstlastDurationHeader = "Last-First ";
                        firstLastFormatter = (data) => $"{"F3".WidthFormat(data.FirstLastCallDurationS, SecondsColWidth)} s ";
                        break;
                    case TimeFormats.s:
                    case TimeFormats.second:
                    case TimeFormats.Here:
                    case TimeFormats.HereTime:
                    case TimeFormats.Local:
                    case TimeFormats.LocalTime:
                    case TimeFormats.UTC:
                    case TimeFormats.UTCTime:
                        switch (LastTimeFormat)
                        {
                            case null:
                                firstlastDurationHeader = "Last-First " + $"First({GetAbbreviatedName(FirstTimeFormat.Value)})".WithWidth(-1 * GetWidth(FirstTimeFormat.Value)) + " ";
                                firstLastFormatter = (data) => $"{"F3".WidthFormat(data.FirstLastCallDurationS, SecondsColWidth)} s {GetDateTimeString(data.FirstCallTime, data.SessionStart, FirstTimeFormat.Value, true)} ";
                                break;
                            case TimeFormats.s:
                            case TimeFormats.second:
                            case TimeFormats.Here:
                            case TimeFormats.HereTime:
                            case TimeFormats.Local:
                            case TimeFormats.LocalTime:
                            case TimeFormats.UTC:
                            case TimeFormats.UTCTime:
                                firstlastDurationHeader = "Last-First " + $"First({GetAbbreviatedName(FirstTimeFormat.Value)})".WithWidth(-1 * GetWidth(FirstTimeFormat.Value)) + " " + $"Last({GetAbbreviatedName(LastTimeFormat.Value)})".WithWidth(-1 * GetWidth(LastTimeFormat.Value)) + " ";
                                firstLastFormatter = (data) => $"{"F3".WidthFormat(data.FirstLastCallDurationS, SecondsColWidth)} s {GetDateTimeString(data.FirstCallTime, data.SessionStart, FirstTimeFormat.Value, true)}" +
                                                               $" {GetDateTimeString(data.LastCallTime, data.SessionStart, LastTimeFormat.Value, true)} ";
                                break;
                            default:
                                throw new InvalidOperationException($"LastTimeFormat {LastTimeFormat} is not yet supported.");
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"FirstTimeFormat {FirstTimeFormat} is not yet supported.");
                }
            }
        }

        private void ProcessPerMethodCSVLine(MatchData match, float firstLastDurationS)
        {
            string moduleDriverInfo = "";
            if (ShowModuleInfo)
            {
                if (match.Driver != null)
                {
                    moduleDriverInfo = $"Vendor: {match?.Driver?.Company} Category: {match?.Driver?.Category}";
                }
                if (match.Module != null && !ShowDriversOnly)
                {
                    moduleDriverInfo += GetModuleString(match.Module);
                }
            }

            WritePerMethodCsvLine(match, firstLastDurationS, moduleDriverInfo);
        }

        private void WritePerMethodCsvLine(MatchData match, float firstLastDurationS, string moduleDriverInfo)
        {
            TimeFormats firstFormat = FirstTimeFormat ?? TimeFormats.s;
            TimeFormats lastFormat = LastTimeFormat ?? TimeFormats.s;


            if (!myCSVHeaderPrinted)
            {
                OpenCSVWithHeader("CSVOptions", "Test Case", "Date", "Test Time in ms", "Module", "Method", "CPU ms", "Wait ms", "# Threads", "Baseline", "Process", "Process Name", "StackDepth",
                                  "FirstLastCall Duration in s", $"First Call time in {GetAbbreviatedName(firstFormat)}", $"Last Call time in {GetAbbreviatedName(lastFormat)}", "Command Line", "SourceFile", "IsNewProcess", "Module and Driver Info");
                myCSVHeaderPrinted = true;
            }

            WriteCSVLine(CSVOptions, match.TestName, match.PerformedAt, match.DurationInMs, match.ModuleName, match.Method, match.CPUMs, match.WaitMs, match.Threads, match.BaseLine, match.ProcessAndPid, match.Process.GetProcessName(UsePrettyProcessName), match.CPUMs / Math.Exp(match.StackDepth),
                         firstLastDurationS, GetDateTimeString(match.FirstCallTime, match.SessionStart, firstFormat), GetDateTimeString(match.LastCallTime, match.SessionStart, lastFormat), NoCmdLine ? "" : match.Process.CmdLine, match.SourceFile, (match.Process.IsNew ? 1 : 0), moduleDriverInfo);
        }


        private void ProcessTotalMatches(List<MatchData> matches)
        {
            if (IsCSVEnabled)
            {
                WriteCSVProcessTotal(matches);
            }
            else
            {
                PrintTotalCPUHeaderOnce();
                PrintProcessTotalMatches(matches);
            }
        }

        private void PrintProcessTotalMatches(List<MatchData> matches)
        {
            foreach (var group in matches.GroupBy(x => x.Process.GetProcessName(this.UsePrettyProcessName)).OrderBy(x => x.Sum(x => x.CPUMs)))
            {
                foreach (var subgroup in group.GroupBy(x => x.ProcessAndPid).OrderBy(x => x.Sum(x => x.CPUMs)))
                {
                    long cpu = subgroup.Sum(x => x.CPUMs);

                    long diff = group.ETWMaxBy(x => x.PerformedAt).CPUMs - cpu;

                    PrintCPUTotal(cpu, subgroup.First().Process, String.Join(" ", subgroup.Select(x => Path.GetFileNameWithoutExtension(x.SourceFile)).ToHashSet()) + $" Diff: {diff:N0} ms ", subgroup.First().SessionStart);
                }
            }
        }

        private void WriteCSVProcessTotal(List<MatchData> matches)
        {
            OpenCSVWithHeader("CSVOptions", "Test Case", "Date", "Test Time in ms", "CPU ms", "Baseline", "Process", "Process Name", "Command Line", "SourceFile", "SourceDirectory", "IsNewProcess");
            foreach (var match in matches.OrderBy(x => x.PerformedAt).ThenByDescending(x => x.CPUMs))
            {
                WriteCSVLine(CSVOptions, match.TestName, match.PerformedAt, match.DurationInMs, match.CPUMs, match.BaseLine, match.ProcessAndPid, match.Process.GetProcessName(UsePrettyProcessName), match.Process.CmdLine, 
                    Path.GetFileNameWithoutExtension(match.SourceFile), Path.GetDirectoryName(match.SourceFile), (match.Process.IsNew ? 1 : 0));
            }
        }

        /// <summary>
        /// Before we can sort by stack depth we need to the top CPU consuming method with stack depth 0
        /// </summary>
        /// <param name="data"></param>
        void SortByValueStatePreparer(IEnumerable<MatchData> data)
        {
            var max = data.ETWMaxBy(x => x.StackDepth == 0 ? x.CPUMs : 0);
            myMaxCPUSortData = max.CPUMs;
        }

        double SortByValue(MatchData data)
        {
            return SortOrder switch
            {
                SortOrders.Wait => data.WaitMs,
                SortOrders.CPU => data.CPUMs,
                // We normalize CPU consumption with the stack depth. The depth starts from 0 which is the method which consumes CPU.
                // Upwards in the stack we must reduce the weight of CPU to get an approximate ordering of the methods which consume most CPU.
                // This can be viewed as a special kind of distance metric in the 2D-space of Time vs StackDepth
                SortOrders.StackDepth => data.CPUMs > myMaxCPUSortData ? data.CPUMs / Math.Exp(data.StackDepth) : data.CPUMs / Math.Exp(data.StackDepth+10), 
                SortOrders.First => data.FirstCallTime.Ticks,
                SortOrders.Last => data.LastCallTime.Ticks,
                _ => data.CPUMs,  // by default sort by CPU
            };
        }

        bool IsMethodMatching(MethodCost cost, double zeroDiff)
        {
            bool lret = MinMaxCPUMs.IsWithin( (int) cost.CPUMs );

            if( lret )
            {
                lret = MinMaxWaitMs.IsWithin( (int) cost.WaitMs );
            }

            if( lret )
            {
                lret = MinMaxFirstS.IsWithin( cost.FirstOccurenceInSecond - zeroDiff );
            }

            if( lret )
            {
                lret = MinMaxLastS.IsWithin( cost.LastOccurenceInSecond - zeroDiff );
            }

            if( lret )
            {
                lret = MinMaxDurationS.IsWithin(cost.LastOccurenceInSecond - cost.FirstOccurenceInSecond);
            }

            if( lret )
            {
                // filter what you see, but do not cut method name while filtering
                lret = IsMethodMatching( myMethodFilterResultsCache, MethodFilter.Value, MethodFormatter.Format( cost.Method, noCut:true ) );
            }

            return lret;
        }

        bool IsStackTagMatching(IStackTagDuration tag, DateTimeOffset sessionStart, double zeroDiff)
        {
            bool lret = MinMaxCPUMs.IsWithin( (int) tag.CPUInMs );

            if( lret)
            {
                lret = MinMaxWaitMs.IsWithin( (int) tag.WaitDurationInMs );
            }

            if (lret)
            {
                lret = MinMaxFirstS.IsWithin(tag.GetFirstOccurrenceS(sessionStart, zeroDiff));
            }

            if (lret)
            {
                lret = MinMaxLastS.IsWithin(tag.GetLastOccurrenceS(sessionStart, zeroDiff));
            }

            if( lret )
            {
                lret = MinMaxDurationS.IsWithin(tag.FirstLastOccurenceDuration.TotalSeconds);
            }    

            if (lret)
            {
                lret = IsMethodMatching( myMethodFilterResultsCache, StackTagFilter.Value, tag.Stacktag );
            }

            return lret;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="filter"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        bool IsMethodMatching(Dictionary<string, bool> cache, Func<string,bool> filter,  string method)
        {
            if( filter == null ) // no filter means that everything matches
            {
                return true;
            }

            if( !cache.TryGetValue(method, out bool lret) )
            {
                lret = filter(method);
                cache[method] = lret;
            }

            return lret;
        }

    }
}
