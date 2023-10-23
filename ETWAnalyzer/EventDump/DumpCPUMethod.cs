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
using System.Text.RegularExpressions;
using System.Diagnostics;

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
        /// When not in process total mode select with -topnmethods the number of methods to print
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
        internal bool IsProcessTotalMode
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
        /// Only show info from Configuration\WellKnownDrivers.json
        /// </summary>
        public bool ShowDriversOnly { get; internal set; }

        /// <summary>
        /// Method sort order
        /// </summary>
        public DumpCommand.SortOrders SortOrder { get; internal set; }

        /// <summary>
        /// Merge CPU consumption of all processes with the same pid across multiple ETL files which can e.g. 
        /// originate from a long term tracing run.
        /// </summary>
        public bool Merge { get; internal set; }

        /// <summary>
        /// When present print method totals
        /// </summary>
        public TotalModes? ShowTotal { get; internal set; }

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
        /// Only show methods where the ready time in ms matches the time range. Default is everything.
        /// </summary>
        public MinMaxRange<int> MinMaxReadyMs { get; internal set; } = new MinMaxRange<int>();

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
        /// Needed for StackDepth sorting of methods where we determine the highest CPU consuming method with stack depth 0.
        /// Then we use for all methods > myMaxCPUSortData the metric CPU/exp(StackDepth), for the rest CPU/exp(StackDepth+10) to ensure
        /// that the deepest methods consuming most CPU are coming first while other methods with less CPU but have a
        /// shallow stack are coming later. 
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

            /// <summary>
            /// Module data of the executable when -smi is used
            /// </summary>
            public ModuleDefinition ExeModule { get; internal set; }

            public Driver Driver { get; internal set; }
            public string ModuleName { get; internal set; }
            public DateTimeOffset SessionStart { get; internal set; }

            public double ZeroTimeS { get; internal set; }
            public uint ReadyMs { get; internal set; }
            public bool? HasCPUSamplingData { get; internal set; }
            public bool? HasCSwitchData { get; internal set; }
            public ProcessKey ProcessKey { get; internal set; }

            public override string ToString()
            {
                return $"{TestName} {ProcessAndPid} {CPUMs}ms {WaitMs}ms {ReadyMs}ms {Method} {FirstCallTime} {LastCallTime} {SourceFile}";
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
                        ModuleDefinition exeModule = null;
                        if (ShowModuleInfo && file.Extract.Modules != null)
                        {
                            exeModule = file.Extract.Modules.FindModule(process.ProcessName, process);
                        }

                        matches.Add(new MatchData
                        {
                            TestName = file.TestName,
                            PerformedAt = file.PerformedAt,
                            DurationInMs = file.DurationInMs,
                            Method = stacktag.Stacktag.CutMinMax(MethodFormatter.MethodCutStart, MethodFormatter.MethodCutLength),
                            CPUMs = (uint)stacktag.CPUInMs,
                            WaitMs = (uint)stacktag.WaitDurationInMs,
                            ReadyMs = 0, // Stacktags have no recorded ready time. If there is demand we still can add it later
                            HasCPUSamplingData = file.Extract.CPU.PerProcessMethodCostsInclusive.HasCPUSamplingData,
                            HasCSwitchData = file.Extract.CPU.PerProcessMethodCostsInclusive.HasCSwitchData,
                            BaseLine = file.Extract.MainModuleVersion != null ? file.Extract.MainModuleVersion.ToString() : "",
                            ProcessAndPid = process.GetProcessWithId(UsePrettyProcessName),
                            ProcessKey = process.ToProcessKey(),
                            SourceFile = file.JsonExtractFileWhenPresent,
                            FirstCallTime = stacktag.FirstOccurence.AddSeconds(-1.0d* zeroTimeS),
                            LastCallTime =  stacktag.FirstOccurence.AddSeconds(-1.0d* zeroTimeS) + stacktag.FirstLastOccurenceDuration,
                            FirstLastCallDurationS = (float)stacktag.FirstLastOccurenceDuration.TotalSeconds,
                            SessionStart = file.Extract.SessionStart,
                            Process = process,
                            ExeModule = exeModule,
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
                        ModuleDefinition exeModule = null;
                        Driver driver = null;

                        if (ShowModuleInfo && file.Extract.Modules != null )
                        {
                            driver = Drivers.Default.TryGetDriverForModule(methodCost.Module);
                            module = file.Extract.Modules.FindModule(methodCost.Module, process);
                            exeModule = file.Extract.Modules.FindModule(process.ProcessName, process);
                        }

                        if( !IsMatchingModule(module) ) // filter by module string
                        {
                            continue;
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
                            ReadyMs = methodCost.ReadyMs,
                            Threads = methodCost.Threads,
                            HasCPUSamplingData = file.Extract.CPU.PerProcessMethodCostsInclusive.HasCPUSamplingData,
                            HasCSwitchData = file.Extract.CPU.PerProcessMethodCostsInclusive.HasCSwitchData,
                            BaseLine = file.Extract.MainModuleVersion != null ? file.Extract.MainModuleVersion.ToString() : "",
                            ProcessAndPid = process.GetProcessWithId(UsePrettyProcessName),
                            ProcessKey = process.ToProcessKey(),
                            SourceFile = file.JsonExtractFileWhenPresent,
                            FirstCallTime = firstCallTime,
                            LastCallTime = lastCallTime,
                            FirstLastCallDurationS = methodCost.LastOccurenceInSecond - methodCost.FirstOccurenceInSecond,
                            SessionStart = file.Extract.SessionStart,
                            StackDepth = methodCost.DepthFromBottom,
                            Process = process,
                            Module = module,
                            ExeModule = exeModule,
                            Driver = driver,
                            ZeroTimeS = zeroTimeS,
                        }) ;
                    }
                }
            }
        }

        /// <summary>
        /// Extract data from TestData, but also print CPU totals while file is traversed to be able
        /// to print data while data is still read.
        /// </summary>
        /// <param name="matches">Current list of matches</param>
        /// <param name="file">Current file to process</param>
        internal void AddAndPrintTotalStats(List<MatchData> matches, TestDataFile file)
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

            // When printing data with -topN dd we sort by CPU ascending because in a shell console window we do not want
            // to scroll back for the highest values
            // Then we skip the first N entries to show only the last TopNSafe results which are the highest values

            Dictionary<ProcessKey, ETWProcess> lookupCache = new();

            ETWProcess Lookup(ProcessKey key)
            {
                if (!lookupCache.TryGetValue(key, out ETWProcess process))
                {
                    process = ProcessExtensions.FindProcessByKey(file, key);
                    lookupCache[key] = process;
                }

                return process;
            }

            List<MatchData> filtered = file.Extract.CPU.PerProcessCPUConsumptionInMs.Where(ProcessFilter).Select(x =>
            {
                ETWProcess process = Lookup(x.Key);

                if (process == null)
                {
                    return null;
                }

                ModuleDefinition module = ShowModuleInfo ? file.Extract.Modules.Modules.Where(x => x.Processes.Contains(process)).Where(x => x.ModuleName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)).FirstOrDefault() : null;

                if (!IsMatchingModule(module)) // filter by module string
                {
                    return null;
                }

                return new MatchData
                {
                    TestName = file.TestName,
                    PerformedAt = file.PerformedAt,
                    DurationInMs = file.DurationInMs,
                    CPUMs = x.Value,
                    BaseLine = file.Extract.MainModuleVersion != null ? file.Extract.MainModuleVersion.ToString() : "",
                    Method = "",
                    ProcessAndPid = process.GetProcessWithId(UsePrettyProcessName),
                    ProcessKey = process,
                    SourceFile = file.JsonExtractFileWhenPresent,
                    Process = process,
                    Module = module,

                };
            }).Where(x=> x!=null).SortAscendingGetTopNLast(SortBySortOrder, null, TopN).ToList();


            PrintTotalCPUHeaderOnce();

            if (!Merge)
            {
                string CpuString = "";
                if( (ShowTotal != null && ShowTotal != TotalModes.None))
                {
                    long cpuTotal = filtered.Select(x => (long) x.CPUMs).Sum();
                    CpuString = $" [green]CPU {cpuTotal:N0} ms[/green] ";
                }
                PrintFileName(file.JsonExtractFileWhenPresent, CpuString, file.PerformedAt, file.Extract.MainModuleVersion?.ToString());
            }

          
            foreach (var cpu in filtered)
            {
                matches.Add(cpu);
                if (!IsCSVEnabled && !Merge && !(ShowTotal == TotalModes.Total) )
                {
                    PrintCPUTotal(cpu.CPUMs, cpu.Process, Path.GetFileNameWithoutExtension(file.FileName), file.Extract.SessionStart, matches[matches.Count-1].Module);
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

        void PrintCPUTotal(long cpu, ETWProcess process, string  sourceFile, DateTimeOffset sessionStart, ModuleDefinition exeModule)
        {
            string fileName = this.Merge ? $" {sourceFile}" : "";

            string cpuStr = cpu.ToString("N0");

            string moduleInfo = "";
            if(exeModule != null)
            {
                moduleInfo = GetModuleString(exeModule, true);
            }

            if (NoCmdLine)
            {
                ColorConsole.WriteEmbeddedColorLine($"\t[Green]{cpuStr,CPUTotal_CPUColumnWidth} ms[/Green] [yellow]{process.GetProcessWithId(UsePrettyProcessName),CPUTotal_ProcessNameWidth}{GetProcessTags(process, sessionStart)}[/yellow]{fileName} [red]{moduleInfo}[/red]");
            }
            else
            {
                ColorConsole.WriteEmbeddedColorLine($"\t[Green]{cpuStr,CPUTotal_CPUColumnWidth} ms[/Green] [yellow]{process.GetProcessWithId(UsePrettyProcessName),CPUTotal_ProcessNameWidth}{GetProcessTags(process, sessionStart)}[/yellow] {process.CommandLineNoExe}", ConsoleColor.DarkCyan, true);
                ColorConsole.WriteEmbeddedColorLine($" {fileName} [red]{moduleInfo}[/red]");
            }
        }

        internal List<MatchData> PrintMatches(List<MatchData> matches)
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

        internal void ProcessPerMethodMatches(List<MatchData> matches, List<MatchData> printed)
        {
            string threadCountHeader = null;
            if (ThreadCount)
            {
                threadCountHeader = "ThreadCount ";
            }

            string firstlastDurationHeader = null;
            string cpuHeader = null;
            string waitHeader = null;
            string readyHeader = null;
            Func<MatchData, string> waitFormatter = _ => "";
            Func<MatchData, string> readyFormatter = _ => "";
            Func<MatchData, string> firstLastFormatter = (data) => "";
            Func<MatchData, string> cpuFormatter = _ => "";

            GetHeaderFormatter(matches, ref cpuHeader, ref cpuFormatter, ref firstlastDurationHeader, ref firstLastFormatter, ref waitHeader, ref waitFormatter, ref readyHeader, ref readyFormatter);

            // The header is omitted when total or process mode is active
            if (!IsCSVEnabled && !(ShowTotal == TotalModes.Total || ShowTotal == TotalModes.Process))
            {
                ColorConsole.WriteEmbeddedColorLine($"[green]{cpuHeader}[/green][yellow]{waitHeader}[/yellow][red]{readyHeader}[/red]{threadCountHeader}{firstlastDurationHeader}Method");
            }

            

            decimal overallCPUTotal = 0;
            decimal overallWaitTotal = 0;

            (Dictionary<string, FileTotals> fileTotals, 
             Dictionary<string, Dictionary<ProcessKey,ProcessTotals>> fileProcessTotals ) = GetFileAndProcessTotals(matches);

            // order files by test time (default) or by totals if enabled
            List<IGrouping<string,MatchData>> byFileOrdered = matches.GroupBy(GetFileGroupName)
                                                .OrderBy(CreateFileSorter(fileTotals)).ToList();

            foreach (var fileGroup in byFileOrdered)
            {
                MatchData firstFileGroup = fileGroup.First();

                // order processes by CPU (default) or sort oder criteria ascending 
                // then take the TopN processes and reverse the list so that we display on console
                // the process with highest CPU or sort order criteria as last so we do not need to scroll upwards in the output in the optimal scenario
                IGrouping<ProcessKey, MatchData>[] topNProcessesBySortOrder = fileGroup.GroupBy(x => x.ProcessKey)
                    .OrderByDescending(CreateProcessSorter(GetFileGroupName(firstFileGroup), fileProcessTotals))
                    .Take(TopN.TakeN)
                    .Reverse()
                    .ToArray();

                string fileTotalString = null;
                int totalWidth = 12;

                if (!IsCSVEnabled && (ShowTotal != null && ShowTotal != TotalModes.None))
                {
                    FileTotals total = fileTotals[GetFileGroupName(firstFileGroup)];
                    overallCPUTotal += total.CPUMs;
                    overallWaitTotal += total.WaitMs;
                   
                    fileTotalString = $" [green]CPU {"N0".WidthFormat(total.CPUMs, totalWidth)} ms[/green] " +
                                      (waitHeader == null ? ""  : $"[yellow]Wait {"N0".WidthFormat(total.WaitMs, totalWidth)} ms[/yellow] ")+
                                      (readyHeader == null ? "" : $"[red]Ready: {"N0".WidthFormat(total.ReadyMs, totalWidth)} ms[/red] ") +
                                      ( (waitHeader == null && readyHeader == null ) ? "" : $"[magenta]Total {"N0".WidthFormat(total.GetTotal(SortOrder), totalWidth)} ms[/magenta] ");
                }

                PrintFileName(firstFileGroup.SourceFile, fileTotalString, firstFileGroup.PerformedAt, firstFileGroup.BaseLine);

                if (ShowTotal == TotalModes.Total)
                {
                    // Skip process details
                    continue;
                }

                foreach (var processGroup in topNProcessesBySortOrder)
                {
                    MatchData firstProcessGroup = processGroup.First();

                    // when we display all we show the highest CPU last in console (ascending) 
                    MatchData[] sorted = processGroup.SortAscendingGetTopNLast(SortBySortOrder, SortByValueStatePreparer, TopNMethods);

                    if (String.IsNullOrEmpty(CSVFile) && !ShowDetailsOnMethodLine)
                    {
                        string processTotalString = "";
                        if(ShowTotal == TotalModes.Process || ShowTotal == TotalModes.Method)
                        {
                            ProcessTotals processTotals = fileProcessTotals[GetFileGroupName(firstFileGroup)][firstProcessGroup.ProcessKey];
                            processTotalString = $"[green]CPU {"N0".WidthFormat(processTotals.CPUMs, totalWidth)} ms[/green] "+
                                                 (waitHeader == null ? "" :  $"[yellow]Wait: {"N0".WidthFormat(processTotals.WaitMs, totalWidth)} ms[/yellow] ")+
                                                 (readyHeader == null ? "" : $"[red]Ready: {"N0".WidthFormat(processTotals.ReadyMs, totalWidth)} ms[/red] ")+
                                                 ((waitHeader == null && readyHeader == null) ? "" : $"[magenta]Total: {"N0".WidthFormat(processTotals.GetTotal(SortOrder), totalWidth)} ms[/magenta] ");
                        }

                        string cmdLine = NoCmdLine ? "" : firstProcessGroup.Process.CommandLineNoExe;
                        MatchData current = processGroup.First();
                        ETWProcess process = current.Process;
                        string moduleString = current.ExeModule != null ? " " + GetModuleString(current.ExeModule, true) : "";

                        ColorConsole.WriteEmbeddedColorLine($"   {processTotalString}[grey]{process.GetProcessWithId(UsePrettyProcessName)}{GetProcessTags(process, current.SessionStart.AddSeconds(current.ZeroTimeS))}[/grey] {cmdLine}", ConsoleColor.DarkCyan, true);
                        ColorConsole.WriteEmbeddedColorLine($"[red]{moduleString}[/red]");
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

                            ColorConsole.WriteEmbeddedColorLine($"  [Green]{cpuFormatter(match)}[/Green] [yellow]{waitFormatter(match)}[/yellow][red]{readyFormatter(match)}[/red]{threadCount}{firstLastFormatter(match)}{match.Method}[darkyellow]{process}[/darkyellow] ", null, true);

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

            if ((ShowTotal != null && ShowTotal != TotalModes.None) && !IsCSVEnabled)
            {
                string crossFileTotal =  (overallWaitTotal == 0 ? "Total " : $"[magenta]Total {overallCPUTotal + overallWaitTotal:N0} ms[/magenta] ") +
                                                                       $"[green]CPU {overallCPUTotal:N0} ms[/green] " +
                                         (overallWaitTotal == 0 ? "" : $"[yellow]Wait {overallWaitTotal:N0} ms[/yellow]");
                ColorConsole.WriteEmbeddedColorLine(crossFileTotal);
            }
        }

        private void GetHeaderFormatter(List<MatchData> matches, ref string cpuHeader, ref Func<MatchData, string> cpuFormatter, ref string firstlastDurationHeader, ref Func<MatchData, string> firstLastFormatter, ref string waitHeader, ref Func<MatchData, string> waitFormatter, ref string readyHeader, ref Func<MatchData, string> readyFormatter)
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

            cpuHeader = "         CPU ms ";
            cpuFormatter = (data) => "N0".WidthFormat(data.CPUMs, 10) +" ms";

            if ( matches.Any(x => x.HasCSwitchData.GetValueOrDefault() || x.WaitMs != 0 ) )
            {
                waitHeader  = "      Wait ms";
                waitFormatter = (data) =>  " " + "N0".WidthFormat(data.WaitMs, 9) + " ms ";
            }

            // only data in enhanced format can contain ready data
            if( matches.Any( x => x.HasCSwitchData.GetValueOrDefault() ) )
            {
                readyHeader = "  Ready ms ";
                readyFormatter = (data) => "N0".WidthFormat(data.ReadyMs, 6) + " ms ";
            }
        }

        /// <summary>
        /// Used by total calculation
        /// </summary>
        internal class FileTotals
        {
            public decimal CPUMs = 0;
            public decimal WaitMs = 0;
            public decimal ReadyMs = 0;

            /// <summary>
            /// Sum of CPU+Wait+Ready
            /// </summary>
            private decimal CPUWaitReadyMs => CPUMs + WaitMs + ReadyMs;

            /// <summary>
            /// Sum of CPU+Wait
            /// </summary>
            private decimal CPUWaitMs => CPUMs + WaitMs;

            public decimal GetTotal(SortOrders order)
            {
                return order switch
                {
                    SortOrders.CPUWaitReady => CPUWaitReadyMs,
                    _ => CPUWaitMs
                };
            }
        }

        internal class ProcessTotals : FileTotals
        {
            
        }

        /// <summary>
        /// Calculate file and process totals which are later needed to sort by various totals
        /// </summary>
        /// <param name="matches"></param>
        /// <returns>Dictionary of file name as key and value is the total per file sum</returns>
        internal (Dictionary<string, FileTotals>, Dictionary<string,Dictionary<ProcessKey, ProcessTotals>>) GetFileAndProcessTotals(List<MatchData> matches)
        {
            Dictionary<string, FileTotals> fileTotals = new();
            Dictionary<string, Dictionary<ProcessKey, ProcessTotals>> fileProcessTotals = new();

            foreach (IGrouping<string, MatchData> fileGroup in matches.GroupBy(GetFileGroupName))
            {
                // order processes by CPU (default) or sort oder criteria ascending 
                // then take the TopN processes and reverse the list so that we display on console
                // the process with highest CPU or sort order criteria as last so we do not need to scroll upwards in the output in the optimal scenario
                IGrouping<ProcessKey, MatchData>[] topNProcessesBySortCriteria = fileGroup.GroupBy(x => x.ProcessKey).OrderByDescending(x => x.Sum(SortBySortOrder)).Take(TopN.TakeN).Reverse().ToArray();

                // calculate totals at file level, but respect -topn -topnmethod filters for total calculation

                MatchData[] filteredSubItems = topNProcessesBySortCriteria.SelectMany(x => x.SortAscendingGetTopNLast(SortBySortOrder, SortByValueStatePreparer, TopNMethods)).ToArray();
                FileTotals total = new FileTotals
                {
                    CPUMs = filteredSubItems.Sum(x => x.CPUMs),
                    WaitMs = filteredSubItems.Sum(x => x.WaitMs),
                    ReadyMs = filteredSubItems.Sum(x => x.ReadyMs),
                };

                fileTotals.Add(fileGroup.Key, total);

                foreach (MatchData data in filteredSubItems)
                {
                    if( !fileProcessTotals.TryGetValue(GetFileGroupName(data), out Dictionary<ProcessKey, ProcessTotals> processDict) )
                    {
                        processDict = new();
                        fileProcessTotals.Add(GetFileGroupName(data), processDict);
                    }

                    if ( !processDict.TryGetValue(data.ProcessKey, out ProcessTotals processTotals))
                    {
                        processTotals = new();
                        processDict[data.ProcessKey] = processTotals;
                    }

                    processTotals.CPUMs += data.CPUMs;
                    processTotals.WaitMs += data.WaitMs;
                    processTotals.ReadyMs += data.ReadyMs;
                }
                    

                
            }

            return (fileTotals, fileProcessTotals);
        }

        /// <summary>
        /// Get input file name for grouping
        /// </summary>
        /// <param name="data"></param>
        /// <returns>key for file grouping</returns>
        string GetFileGroupName(MatchData data) => data.SourceFile;

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
                OpenCSVWithHeader("CSVOptions", "Test Case", "Date", "Test Time in ms", "Module", "Method", "CPU ms", "Wait ms", "Ready ms", "# Threads", "Baseline", "Process", "Process Name", "Start Time", "StackDepth",
                                  "FirstLastCall Duration in s", $"First Call time in {GetAbbreviatedName(firstFormat)}", $"Last Call time in {GetAbbreviatedName(lastFormat)}", "Command Line", "SourceFile", "IsNewProcess", "Module and Driver Info");
                myCSVHeaderPrinted = true;
            }

            WriteCSVLine(CSVOptions, match.TestName, match.PerformedAt, match.DurationInMs, match.ModuleName, match.Method, match.CPUMs, match.WaitMs, match.ReadyMs, match.Threads, match.BaseLine, match.ProcessAndPid, match.Process.GetProcessName(UsePrettyProcessName), match.Process.StartTime, match.CPUMs / Math.Exp(match.StackDepth),
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
            foreach (var group in matches.GroupBy(x => x.Process.GetProcessName(this.UsePrettyProcessName)).OrderBy(x => x.Sum(SortBySortOrder)))
            {
                foreach (var subgroup in group.GroupBy(x => x.ProcessKey).OrderBy(x => x.Sum(SortBySortOrder)))
                {
                    long cpu = subgroup.Sum(x => x.CPUMs);

                    long diff = group.ETWMaxBy(x => x.PerformedAt).CPUMs - cpu;

                    PrintCPUTotal(cpu, subgroup.First().Process, String.Join(" ", subgroup.Select(x => Path.GetFileNameWithoutExtension(x.SourceFile)).ToHashSet()) + $" Diff: {diff:N0} ms ", subgroup.First().SessionStart, subgroup.FirstOrDefault().Module);
                }
            }
        }

        private void WriteCSVProcessTotal(List<MatchData> matches)
        {
            OpenCSVWithHeader(Col_CSVOptions, Col_TestCase, Col_Date, Col_TestTimeinms, "CPU ms", Col_Baseline, Col_Process, Col_ProcessName, 
                Col_StartTime, Col_CommandLine, Col_SourceJsonFile, "SourceDirectory", "IsNewProcess", 
                Col_FileVersion, Col_VersionString, Col_ProductVersion, Col_ProductName, Col_Description, Col_Directory);

            foreach (var match in matches.OrderBy(x => x.PerformedAt).ThenByDescending(x => x.CPUMs))
            {
                string fileVersion = match.Module?.Fileversion?.ToString()?.Trim() ?? "";
                string versionString = match.Module?.FileVersionStr?.Trim() ?? "";
                string productVersion = match.Module?.ProductVersionStr?.Trim() ?? "";
                string productName = match.Module?.ProductName?.Trim() ?? "";
                string description = match.Module?.Description?.Trim() ?? "";
                string directory = match.Module?.ModulePath ?? "";
                WriteCSVLine(CSVOptions, match.TestName, match.PerformedAt, match.DurationInMs, match.CPUMs, match.BaseLine, match.ProcessAndPid, match.Process.GetProcessName(UsePrettyProcessName), match.Process.StartTime, match.Process.CmdLine, 
                    Path.GetFileNameWithoutExtension(match.SourceFile), Path.GetDirectoryName(match.SourceFile), (match.Process.IsNew ? 1 : 0),
                    fileVersion, versionString, productVersion, productName, description, directory);
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

        /// <summary>
        /// Used by context sensitive help to print valid sort order based on context.
        /// </summary>
        static readonly internal SortOrders[] ValidCPUSortOrders =
            new SortOrders[]
            {
                SortOrders.CPU,
                SortOrders.Wait,                
                SortOrders.Ready,
                SortOrders.CPUWait,
                SortOrders.CPUWaitReady,
                SortOrders.StackDepth,
                SortOrders.First,
                SortOrders.Last,
                SortOrders.TestTime,
                SortOrders.StartTime,
            };

        /// <summary>
        /// Used for totals calculation to cut off the -topn processes which after 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal double SortBySortOrder(MatchData data)
        {
            return SortOrder switch
            {
                SortOrders.Wait => data.WaitMs,
                SortOrders.CPU => data.CPUMs,
                SortOrders.Ready => data.ReadyMs,
                SortOrders.CPUWait => data.CPUMs+data.WaitMs,
                SortOrders.CPUWaitReady => data.CPUMs+data.WaitMs+data.ReadyMs,
                SortOrders.StartTime => IsProcessTotalMode ? data.Process.StartTime.Ticks : data.CPUMs,
                // We normalize CPU consumption with the stack depth. The depth starts from 0 which is the method which consumes CPU.
                // Upwards in the stack we must reduce the weight of CPU to get an approximate ordering of the methods which consume most CPU.
                // This can be viewed as a special kind of distance metric in the 2D-space of Time vs StackDepth
                SortOrders.StackDepth => data.CPUMs > myMaxCPUSortData ? data.CPUMs / Math.Exp(data.StackDepth) : data.CPUMs / Math.Exp(data.StackDepth+10), 
                SortOrders.First => IsProcessTotalMode  ? data.Process.StartTime.Ticks : data.FirstCallTime.Ticks,  // in CPU total mode we can sort by process start time with -Sortby First which is useful with -ProcessFmt s
                SortOrders.Last => IsProcessTotalMode  ?  (data.Process.EndTime == DateTimeOffset.MaxValue ? 0 : data.Process.EndTime.Ticks) : data.LastCallTime.Ticks,     // in CPU total mode we can sort by process end time with -SortBy Last
                _ => data.CPUMs,  // by default sort by CPU
            };
        }

        /// <summary>
        /// Sort files by time (default), or when Total mode is enabled by per file totals which 
        /// can be configured with -SortBy clause by which total the files are sorted.
        /// </summary>
        /// <param name="fileTotals">Precalculated file totals</param>
        /// <returns>Delegate which is used for sorting</returns>
        internal Func<IGrouping<string, MatchData>, decimal> CreateFileSorter(Dictionary<string, FileTotals> fileTotals)
        {
            return x =>
            {
                MatchData data = x.First();
                decimal lret = data.PerformedAt.Ticks; // default is sort by file time
                if( (ShowTotal != null && ShowTotal != TotalModes.None) ) // when totals are printed we sort cross file by file totals to show the highest total
                {
                    FileTotals ftotal = fileTotals[GetFileGroupName(data)];
                    lret = SortOrder switch
                    {
                        SortOrders.CPU => ftotal.CPUMs,
                        SortOrders.Wait => ftotal.WaitMs,
                        SortOrders.Ready => ftotal.ReadyMs,
                        SortOrders.CPUWait => ftotal.GetTotal(SortOrders.CPUWait),
                        SortOrders.CPUWaitReady => ftotal.GetTotal(SortOrders.CPUWaitReady),
                        SortOrders.TestTime => data.PerformedAt.Ticks,
                        _ => ftotal.GetTotal(SortOrders.CPUWait),
                    };
                }
                return lret;
            };
        }

        /// <summary>
        /// Sort processes inside one file by total CPU (default) or sort order given by -SortBy
        /// </summary>
        /// <param name="fileGroupName">Input file name</param>
        /// <param name="fileProcessTotals">Process totals per input file where fileGroupName is used to look up totals.</param>
        /// <returns>Delegate which is used for sorting.</returns>
        internal Func<IGrouping<ProcessKey, MatchData>, decimal> CreateProcessSorter(string fileGroupName, Dictionary<string, Dictionary<ProcessKey, ProcessTotals>> fileProcessTotals)
        {
            return x =>
            {
                decimal lret = 0.0M;

                // since we cut during totals already TopN ... we can get dictionary misses which are for the sort order not relevant anyway because
                // they do not contribute to totals calculation.
                if (fileProcessTotals.TryGetValue(fileGroupName, out Dictionary<ProcessKey, ProcessTotals> processDict))
                {
                    if (processDict.TryGetValue(x.Key, out ProcessTotals totals))
                    {
                        lret = SortOrder switch
                        {
                            SortOrders.CPU => totals.CPUMs,
                            SortOrders.Wait => totals.WaitMs,
                            SortOrders.Ready => totals.ReadyMs,
                            SortOrders.CPUWait => totals.GetTotal(SortOrders.CPUWait),
                            SortOrders.CPUWaitReady => totals.GetTotal(SortOrders.CPUWaitReady),
                            SortOrders.StartTime => x.Key.StartTime.Ticks,
                            _ => totals.CPUMs,
                        };
                    }
                }

                return lret;
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
                lret = MinMaxReadyMs.IsWithin((int)cost.ReadyMs);
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
