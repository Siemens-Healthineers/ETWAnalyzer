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
using ETWAnalyzer.Extract.CPU;
using ETWAnalyzer.Extract.CPU.Extended;
using ETWAnalyzer.Extractors.CPU;
using static ETWAnalyzer.EventDump.DumpMemory;
using System.Security.Claims;

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
        public KeyValuePair<string, Func<string, bool>> StackTagFilter { get; internal set; }

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

        public bool ShowDetails { get; internal set; }

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
        public MethodFormatter MethodFormatter { get; internal set; } = new();

        /// <summary>
        /// Only show methods/stacktags where the CPU time in ms matches the range. Default is everything.
        /// </summary>
        public MinMaxRange<int> MinMaxCPUMs { get; internal set; } = new();

        /// <summary>
        /// Only show methods/stacktags where the Wait time in ms time matches the range. Default is everything.
        /// </summary>
        public MinMaxRange<int> MinMaxWaitMs { get; internal set; } = new();

        /// <summary>
        /// Only show methods where the ready time in ms matches the time range. Default is everything.
        /// </summary>
        public MinMaxRange<int> MinMaxReadyMs { get; internal set; } = new();

        /// <summary>
        /// Only show methods for which the average ready time is within bounds. Default is everything.
        /// </summary>
        public MinMaxRange<int> MinMaxReadyAverageUs { get; internal set; } = new();

        /// <summary>
        /// Only show methods which have at least [x-y] context switches. Default is everything.
        /// </summary>
        public MinMaxRange<int> MinMaxCSwitch { get; internal set; } = new();

        /// <summary>
        /// Only show methods/stacktags where the first occurrence in seconds  matches the range. Default is everything.
        /// </summary>
        public MinMaxRange<double> MinMaxFirstS { get; internal set; } = new();
        /// <summary>
        /// Only show methods/stacktags where the last occurrence in seconds matches the range. Default is everything.
        /// </summary>
        public MinMaxRange<double> MinMaxLastS { get; internal set; } = new();

        /// <summary>
        /// Only show methods/stacktags where the Last-First occurrence matches the range. Default is everything.
        /// This is not to be confused with a method runtime! The input of this data is CPU sampling and Context Switch events.
        /// The method might be called 1 100 or 10000 times. The duration only shows the range where the method CPU sampling/Context Switch data 
        /// did show up in the trace timeline. This value can still be useful to e.g. estimate the runtime of asynchronous methods, or to check
        /// if it was called more than once in a trace if e.g. the ctor which is supposed to run a short time is several seconds apart. 
        /// </summary>
        public MinMaxRange<double> MinMaxDurationS { get; internal set; } = new();

        /// <summary>
        /// When -Details is enabled and Ready Extended metrics are present Ready details are omitted.
        /// </summary>
        public bool NoReadyDetails { get; internal set; }

        /// <summary>
        /// When -Details are enabled and Frequency data was extracted data is no printed
        /// </summary>
        public bool NoFrequencyDetails { get; internal set; }

        /// <summary>
        /// When -Details are enabled CPU consumption is normalized to 100% of the nominal CPU Frequency to make number of different runs with potentially 
        /// different 
        /// </summary>
        public bool Normalize { get; internal set; }

        /// <summary>
        /// Omit priority in output 
        /// </summary>
        public bool NoPriorityDetails { get; internal set; }



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
        /// Total CPU column width
        /// </summary>
        const int CPUtotal_PriorityColumnWidth = 8;


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
            public uint ReadyMs { get; internal set; }
            public ulong ReadyAverageUs { get; internal set; }
            public uint ContextSwitchCount { get; internal set; }
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
            public bool? HasCPUSamplingData { get; internal set; }
            public bool? HasCSwitchData { get; internal set; }
            public ProcessKey ProcessKey { get; internal set; }
            public int SessionId { get; set; }
            public ICPUUsage[] CPUUsage { get; internal set; }
            public IReadOnlyDictionary<CPUNumber, ICPUTopology> Topology { get; internal set; }
            public IReadyTimes ReadyDetails { get; internal set; }
            public float ProcessPriority { get; internal set; }

            public override string ToString()
            {
                return $"{TestName} {ProcessAndPid} {CPUMs}ms {WaitMs}ms {ReadyMs}ms {ReadyAverageUs}us {Method} {FirstCallTime} {LastCallTime} {SourceFile}";
            }
        }

        public override List<MatchData> ExecuteInternal()
        {
            var testsOrderedByTime = GetTestRuns(true, SingleTestCaseFilter, TestFileFilter);
            WarnIfNoTestRunsFound(testsOrderedByTime);

            List<MatchData> matches = new();

            foreach (var test in testsOrderedByTime)
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
                            if ((MethodFilter.Key == null && StackTagFilter.Key == null) || MethodFilter.Key != null)
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
            if (tags == null)
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
                            FirstCallTime = stacktag.FirstOccurence.AddSeconds(-1.0d * zeroTimeS),
                            LastCallTime = stacktag.FirstOccurence.AddSeconds(-1.0d * zeroTimeS) + stacktag.FirstLastOccurenceDuration,
                            FirstLastCallDurationS = (float)stacktag.FirstLastOccurenceDuration.TotalSeconds,
                            SessionStart = file.Extract.SessionStart,
                            Process = process,
                            SessionId = process.SessionId,
                            ExeModule = exeModule,
                            ZeroTimeS = zeroTimeS,
                        });
                    }
                }
            }
        }

        private void AddPerMethodStats(List<MatchData> matches, TestDataFile file, double zeroTimeS)
        {
            if (file.Extract?.CPU?.PerProcessMethodCostsInclusive == null)
            {
                ColorConsole.WriteWarning($"Warning: File {file.JsonExtractFileWhenPresent} contains no CPU data.");
                return;
            }

            Func<MethodCost, bool> methodFilter = (cost) => IsMethodMatching(cost, zeroTimeS);

            foreach (var perProcess in file.Extract.CPU.PerProcessMethodCostsInclusive.MethodStatsPerProcess)
            {
                if (!IsMatchingProcessAndCmdLine(file, perProcess.Process))
                {
                    continue;
                }

                foreach (var methodCost in perProcess.Costs.OrderBy(x => x.FirstOccurenceInSecond).Where(methodFilter))
                {
                    ETWProcess process = file.FindProcessByKey(perProcess.Process);

                    if (process != null)
                    {

                        DateTimeOffset lastCallTime = file.Extract.ConvertTraceRelativeToAbsoluteTime(methodCost.LastOccurenceInSecond - (float)zeroTimeS);
                        DateTimeOffset firstCallTime = file.Extract.ConvertTraceRelativeToAbsoluteTime(methodCost.FirstOccurenceInSecond - (float)zeroTimeS);
                        ModuleDefinition module = null;
                        ModuleDefinition exeModule = null;
                        Driver driver = null;

                        if (ShowModuleInfo && file.Extract.Modules != null)
                        {
                            driver = Drivers.Default.TryGetDriverForModule(methodCost.Module);
                            module = file.Extract.Modules.FindModule(methodCost.Module, process);
                            exeModule = file.Extract.Modules.FindModule(process.ProcessName, process);
                        }

                        if (!IsMatchingModule(module)) // filter by module string
                        {
                            continue;
                        }

                        ICPUUsage[] cpuUsage = null;
                        IReadyTimes readyTimes = null;
                        float prio = 0.0f;

                        if (perProcess.Process.Pid > WindowsConstants.IdleProcessId)
                        {
                            ETWProcessIndex idx = file.Extract.GetProcessIndexByPID(perProcess.Process.Pid, perProcess.Process.StartTime);
                            file.Extract?.CPU?.PerProcessAvgCPUPriority?.TryGetValue(idx, out prio); // get process priority

                            ProcessMethodIdx procMethod = idx.Create(methodCost.MethodIdx);
                            if (file.Extract.CPU?.ExtendedCPUMetrics?.MethodIndexToCPUMethodData?.ContainsKey(procMethod) == true)
                            {
                                cpuUsage = file.Extract.CPU.ExtendedCPUMetrics.MethodIndexToCPUMethodData[procMethod].CPUConsumption;
                            }

                            IReadOnlyDictionary<ProcessMethodIdx, ICPUMethodData> extendedCPUMetrics = file?.Extract?.CPU?.ExtendedCPUMetrics?.MethodIndexToCPUMethodData;

                            if (extendedCPUMetrics != null && extendedCPUMetrics.TryGetValue(procMethod, out ICPUMethodData extendedCPUData))
                            {
                                readyTimes = extendedCPUData.ReadyMetrics;
                            }
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
                            ReadyAverageUs = methodCost.ReadyAverageUs,
                            ProcessPriority = prio,
                            ContextSwitchCount = methodCost.ContextSwitchCount,
                            Threads = methodCost.Threads,
                            CPUUsage = cpuUsage,
                            Topology = file?.Extract?.CPU?.Topology,
                            ReadyDetails = readyTimes,
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
                            SessionId = process.SessionId,
                            Module = module,
                            ExeModule = exeModule,
                            Driver = driver,
                            ZeroTimeS = zeroTimeS,
                        });
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
                if (!IsMatchingProcessAndCmdLine(file, procCPU.Key))
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
                        if (process.ProcessID == 0) // exclude DeepSleep process
                        {
                            lret = false;
                        }
                    }

                }

                if (lret && !MinMaxCPUMs.IsWithin((int)procCPU.Value))
                {
                    lret = false;
                }
                return lret;
            }

            // When printing data with -topN dd we sort by CPU ascending because in a shell console window we do not want
            // to scroll back for the highest values
            // Then we skip the first N entries to show only the last TopNSafe results which are the highest values

            Dictionary<ProcessKey, ETWProcess> lookupCache = new();
            Dictionary<ProcessKey, ETWProcessIndex> indexCache = new();

            ETWProcess Lookup(ProcessKey key)
            {
                if (!lookupCache.TryGetValue(key, out ETWProcess process))
                {
                    process = ProcessExtensions.FindProcessByKey(file, key);
                    lookupCache[key] = process;
                }

                return process;
            }

            ETWProcessIndex LookupIndex(ProcessKey key)
            {
                if (!indexCache.TryGetValue(key, out ETWProcessIndex processIndex))
                {
                    processIndex = file.Extract.GetProcessIndexByPidAtTime(key.Pid, key.StartTime);
                    indexCache[key] = processIndex;
                }
                return processIndex;
            }

            List<MatchData> filtered = file.Extract.CPU.PerProcessCPUConsumptionInMs.Where(ProcessFilter).Select(x =>
            {
                ETWProcess process = Lookup(x.Key);
                if (process == null)
                {
                    return null;
                }

                ETWProcessIndex index = LookupIndex(x.Key);
                if (index == ETWProcessIndex.Invalid)
                {
                    return null;
                }

                ModuleDefinition module = ShowModuleInfo ? file.Extract.Modules.Modules.Where(x => x.Processes.Contains(process)).Where(x => x.ModuleName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)).FirstOrDefault() : null;

                if (!IsMatchingModule(module)) // filter by module string
                {
                    return null;
                }

                float prio = 0.0f;
                file.Extract?.CPU.PerProcessAvgCPUPriority?.TryGetValue(index, out prio);

                return new MatchData
                {
                    TestName = file.TestName,
                    PerformedAt = file.PerformedAt,
                    DurationInMs = file.DurationInMs,
                    CPUMs = x.Value,
                    ProcessPriority = prio,
                    BaseLine = file.Extract.MainModuleVersion != null ? file.Extract.MainModuleVersion.ToString() : "",
                    Method = "",
                    ProcessAndPid = process.GetProcessWithId(UsePrettyProcessName),
                    ProcessKey = process,
                    SourceFile = file.JsonExtractFileWhenPresent,
                    Process = process,
                    SessionId = process.SessionId,
                    Module = module,

                };
            }).Where(x => x != null).SortAscendingGetTopNLast(SortBySortOrder, null, TopN).ToList();


            PrintTotalCPUHeaderOnce();

            if (!Merge)
            {
                string CpuString = "";
                if ((ShowTotal != null && ShowTotal != TotalModes.None))
                {
                    long cpuTotal = filtered.Select(x => (long)x.CPUMs).Sum();
                    CpuString = $" [green]CPU {cpuTotal:N0} ms[/green] ";
                }
                PrintFileName(file.JsonExtractFileWhenPresent, CpuString, file.PerformedAt, file.Extract.MainModuleVersion?.ToString());
            }


            foreach (var cpu in filtered)
            {
                matches.Add(cpu);
                if (!IsCSVEnabled && !Merge && !(ShowTotal == TotalModes.Total))
                {
                    PrintCPUTotal(cpu.CPUMs, cpu.ProcessPriority, cpu.Process, Path.GetFileNameWithoutExtension(file.FileName), file.Extract.SessionStart, matches[matches.Count - 1].Module);
                }
            }
        }

        void PrintTotalCPUHeaderOnce()
        {
            if (!IsCSVEnabled)
            {
                string cpuHeaderName = "CPU";
                string priorityHeaderName = ShowTotal == TotalModes.Total || NoPriorityDetails ? "" : "Priority".WithWidth(CPUtotal_PriorityColumnWidth)+" ";
                string processHeaderName = "Process Name";
                string sessionHeaderName = ShowDetails ? "Session " : "";

                if (myCPUTotalHeaderShown == false)
                {
                    ColorConsole.WriteEmbeddedColorLine($"\t[Green]{cpuHeaderName.WithWidth(CPUTotal_CPUColumnWidth)} ms[/Green] [red]{priorityHeaderName}[/red][yellow]{sessionHeaderName}{processHeaderName.WithWidth(CPUTotal_ProcessNameWidth)}[/yellow]");
                    myCPUTotalHeaderShown = true;
                }
            }
        }

        /// <summary>
        /// Format priority with color when it is greater 8 which is normal, or in a different color when it is below normal
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        string FormatPriorityColor(float priority, int width)
        {
            if (NoPriorityDetails || priority == 0)
            {
                return "";
            }
            return priority >= 8.0f ? $"[red]{"F1".WidthFormat(priority, width)}[/red]" : $"[green]{"F1".WidthFormat(priority, width)}[/green]";
        }

        void PrintCPUTotal(long cpu, float priority, ETWProcess process, string sourceFile, DateTimeOffset sessionStart, ModuleDefinition exeModule)
        {
            string fileName = this.Merge ? $" {sourceFile}" : "";

            string cpuStr = cpu.ToString("N0");

            string sessionIdStr = ShowDetails ? $"{process.SessionId,7} " : "";

            string moduleInfo = "";
            if (exeModule != null)
            {
                moduleInfo = GetModuleString(exeModule, true);
            }

            string prio = "";
            if (NoPriorityDetails == false && priority > 0)
            {
                prio = FormatPriorityColor(priority, CPUtotal_PriorityColumnWidth) + " ";
            }

            

            if (NoCmdLine)
            {
                ColorConsole.WriteEmbeddedColorLine($"\t[Green]{cpuStr,CPUTotal_CPUColumnWidth} ms[/Green] {prio}[yellow]{process.GetProcessWithId(UsePrettyProcessName),CPUTotal_ProcessNameWidth}{GetProcessTags(process, sessionStart)}[/yellow]{fileName} [red]{moduleInfo}[/red]");
            }
            else
            {
                ColorConsole.WriteEmbeddedColorLine($"\t[Green]{cpuStr,CPUTotal_CPUColumnWidth} ms[/Green] {prio}[yellow]{sessionIdStr}{process.GetProcessWithId(UsePrettyProcessName),CPUTotal_ProcessNameWidth}{GetProcessTags(process, sessionStart)}[/yellow] {process.CommandLineNoExe}", ConsoleColor.DarkCyan, true);
                ColorConsole.WriteEmbeddedColorLine($" {fileName} [red]{moduleInfo}[/red]");
            }
        }

        internal List<MatchData> PrintMatches(List<MatchData> matches)
        {
            List<MatchData> printed = new();

            if (IsProcessTotalMode)
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
            Formatter<MatchData> cpuFormatter = GetHeaderFormatter(matches, FormatterType.CPU);
            Formatter<MatchData> waitFormatter = GetHeaderFormatter(matches, FormatterType.Wait);
            Formatter<MatchData> readyFormatter = GetHeaderFormatter(matches, FormatterType.Ready);
            Formatter<MatchData> readyAverageFormatter = GetHeaderFormatter(matches, FormatterType.ReadyAverage);
            Formatter<MatchData> cswitchCountFormatter = GetHeaderFormatter(matches, FormatterType.CSwitchCount);
            Formatter<MatchData> threadCountFormatter = GetHeaderFormatter(matches, FormatterType.ThreadCount);
            Formatter<MatchData> coreFrequencyFormatter = GetHeaderFormatter(matches, FormatterType.Frequency);
            Formatter<MatchData> firstLastFormatter = GetHeaderFormatter(matches, FormatterType.FirstLast);
            Formatter<MatchData> readyDetailsFormatter = GetHeaderFormatter(matches, FormatterType.ReadyDetails);

            // The header is omitted when total or process mode is active
            if (!IsCSVEnabled && !(ShowTotal == TotalModes.Total || ShowTotal == TotalModes.Process))
            {
                ColorConsole.WriteEmbeddedColorLine($"[green]{cpuFormatter.Header}[/green][yellow]{waitFormatter.Header}[/yellow][red]{readyFormatter.Header}{readyAverageFormatter.Header}[/red][yellow]{cswitchCountFormatter.Header}[/yellow]{threadCountFormatter.Header}{firstLastFormatter.Header}Method");
            }

            decimal overallCPUTotal = 0;
            decimal overallWaitTotal = 0;

            (Dictionary<string, FileTotals> fileTotals,
             Dictionary<string, Dictionary<ProcessKey, ProcessTotals>> fileProcessTotals) = GetFileAndProcessTotals(matches);

            // order files by test time (default) or by totals if enabled
            List<IGrouping<string, MatchData>> byFileOrdered = matches.GroupBy(GetFileGroupName)
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
                                      (waitFormatter.Header == "" ? "" : $"[yellow]Wait {"N0".WidthFormat(total.WaitMs, totalWidth)} ms[/yellow] ") +
                                      (readyFormatter.Header == "" ? "" : $"[red]Ready: {"N0".WidthFormat(total.ReadyMs, totalWidth)} ms[/red] ") +
                                      ((waitFormatter.Header == "" && readyFormatter.Header == "") ? "" : $"[magenta]Total {"N0".WidthFormat(total.GetTotal(SortOrder), totalWidth)} ms[/magenta] ");
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
                        if (ShowTotal == TotalModes.Process || ShowTotal == TotalModes.Method)
                        {
                            ProcessTotals processTotals = fileProcessTotals[GetFileGroupName(firstFileGroup)][firstProcessGroup.ProcessKey];
                            processTotalString = $"[green]CPU {"N0".WidthFormat(processTotals.CPUMs, totalWidth)} ms[/green] " +
                                                 (waitFormatter.Header == "" ? "" : $"[yellow]Wait: {"N0".WidthFormat(processTotals.WaitMs, totalWidth)} ms[/yellow] ") +
                                                 (readyFormatter.Header == "" ? "" : $"[red]Ready: {"N0".WidthFormat(processTotals.ReadyMs, totalWidth)} ms[/red] ") +
                                                 ((waitFormatter.Header == "" && readyFormatter.Header == "") ? "" : $"[magenta]Total: {"N0".WidthFormat(processTotals.GetTotal(SortOrder), totalWidth)} ms[/magenta] ");
                        }

                        string cmdLine = NoCmdLine ? "" : firstProcessGroup.Process.CommandLineNoExe;
                        MatchData current = processGroup.First();
                        ETWProcess process = current.Process;
                        string moduleString = current.ExeModule != null ? " " + GetModuleString(current.ExeModule, true) : "";

                        string processPriority = "";
                        if (!NoPriorityDetails && current.ProcessPriority > 0)
                        {
                            processPriority = ShowDetails ? $"  Priority: {FormatPriorityColor(current.ProcessPriority, 0)}" : "";
                        }
                        ColorConsole.WriteEmbeddedColorLine($"   {processTotalString}[grey]{process.GetProcessWithId(UsePrettyProcessName)}{GetProcessTags(process, current.SessionStart.AddSeconds(current.ZeroTimeS))}[/grey]{processPriority} {cmdLine}", ConsoleColor.DarkCyan, true);
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
                            string process = "";

                            if (ShowDetailsOnMethodLine)
                            {
                                process = " " + match.ProcessAndPid;
                            }

                            ColorConsole.WriteEmbeddedColorLine($"  [Green]{cpuFormatter.Print(match)}[/Green] [yellow]{waitFormatter.Print(match)}[/yellow][red]{readyFormatter.Print(match)}{readyAverageFormatter.Print(match)}[/red][yellow]{cswitchCountFormatter.Print(match)}[/yellow]{threadCountFormatter.Print(match)}{firstLastFormatter.Print(match)}{match.Method}[darkyellow]{process}[/darkyellow] ", null, true);

                            if (coreFrequencyFormatter.Header != "" && match.CPUUsage != null)
                            {
                                Console.WriteLine();
                                ColorConsole.WriteEmbeddedColorLine($"{coreFrequencyFormatter.Print(match)}", ConsoleColor.Cyan, true);
                            }

                            if (readyDetailsFormatter.Header != "" && match.ReadyDetails != null)
                            {
                                ColorConsole.WriteLine("");
                                ColorConsole.WriteEmbeddedColorLine($"[red]{readyDetailsFormatter.Print(match)}[/red]", null, true);
                            }


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
                string crossFileTotal = (overallWaitTotal == 0 ? "Total " : $"[magenta]Total {overallCPUTotal + overallWaitTotal:N0} ms[/magenta] ") +
                                                                       $"[green]CPU {overallCPUTotal:N0} ms[/green] " +
                                         (overallWaitTotal == 0 ? "" : $"[yellow]Wait {overallWaitTotal:N0} ms[/yellow]");
                ColorConsole.WriteEmbeddedColorLine(crossFileTotal);
            }
        }

        enum FormatterType
        {
            Invalid = 0,
            CPU,
            FirstLast,
            Wait,
            Ready,
            ReadyAverage,
            CSwitchCount,
            Frequency,
            ThreadCount,
            ReadyDetails,
        }

        private Formatter<MatchData> GetHeaderFormatter(List<MatchData> matches, FormatterType type)
        {
            return type switch
            {
                FormatterType.FirstLast => FirstTimeFormat switch
                {
                    null => new Formatter<MatchData>
                    {
                        Header = FirstLastDuration ? "Last-First " : "",
                        Print = FirstLastDuration ? (data) => $"{"F3".WidthFormat(data.FirstLastCallDurationS, SecondsColWidth)} s " : (data) => "",
                    },
                    TimeFormats.s or
                    TimeFormats.second or
                    TimeFormats.Here or
                    TimeFormats.HereTime or
                    TimeFormats.Local or
                    TimeFormats.LocalTime or
                    TimeFormats.UTC or
                    TimeFormats.UTCTime => LastTimeFormat switch
                    {
                        null => new Formatter<MatchData>
                        {
                            Header = FirstLastDuration ? "Last-First " + $"First({GetAbbreviatedName(FirstTimeFormat.Value)})".WithWidth(-1 * GetWidth(FirstTimeFormat.Value)) + " " : "",
                            Print = FirstLastDuration ? (data) => $"{"F3".WidthFormat(data.FirstLastCallDurationS, SecondsColWidth)} s {GetDateTimeString(data.FirstCallTime, data.SessionStart, FirstTimeFormat.Value, true)} " : (data) => "",
                        },
                        TimeFormats.s or
                        TimeFormats.second or
                        TimeFormats.Here or
                        TimeFormats.HereTime or
                        TimeFormats.Local or
                        TimeFormats.LocalTime or
                        TimeFormats.UTC or
                        TimeFormats.UTCTime =>
                          new Formatter<MatchData>
                          {
                              Header = FirstLastDuration ? "Last-First " + $"First({GetAbbreviatedName(FirstTimeFormat.Value)})".WithWidth(-1 * GetWidth(FirstTimeFormat.Value)) + " " + $"Last({GetAbbreviatedName(LastTimeFormat.Value)})".WithWidth(-1 * GetWidth(LastTimeFormat.Value)) + " " : "",
                              Print = FirstLastDuration ? (data) => $"{"F3".WidthFormat(data.FirstLastCallDurationS, SecondsColWidth)} s {GetDateTimeString(data.FirstCallTime, data.SessionStart, FirstTimeFormat.Value, true)}" +
                                                                 $" {GetDateTimeString(data.LastCallTime, data.SessionStart, LastTimeFormat.Value, true)} " : (data) => "",
                          },
                        _ => throw new InvalidOperationException($"LastTimeFormat {LastTimeFormat} is not yet supported."),
                    },
                    _ => throw new InvalidOperationException($"FirstTimeFormat {FirstTimeFormat} is not yet supported."),
                },
                FormatterType.CPU => new Formatter<MatchData>
                {
                    Header = "         CPU ms ",
                    Print = (data) => "N0".WidthFormat(data.CPUMs, 10) + " ms"
                },
                FormatterType.Wait => new Formatter<MatchData>
                {
                    Header = matches.Any(x => x.HasCSwitchData.GetValueOrDefault() || x.WaitMs != 0) ? "      Wait ms " : "",
                    Print = matches.Any(x => x.HasCSwitchData.GetValueOrDefault() || x.WaitMs != 0) ? (data) => " " + "N0".WidthFormat(data.WaitMs, 9) + " ms " : (data) => "",
                },
                FormatterType.Ready => new Formatter<MatchData>
                {
                    // only data in enhanced format can contain ready data
                    Header = matches.Any(x => x.HasCSwitchData.GetValueOrDefault()) && !NoReadyDetails ? " Ready ms " : "",
                    Print = matches.Any(x => x.HasCSwitchData.GetValueOrDefault()) && !NoReadyDetails ? (data) => "N0".WidthFormat(data.ReadyMs, 6) + " ms " : (data) => "",
                },
                FormatterType.ReadyAverage => new Formatter<MatchData>
                {
                    Header = (matches.Any(x => x.HasCSwitchData.GetValueOrDefault()) && ShowDetails && !NoReadyDetails) ? "ReadyAvg " : "",
                    Print = (matches.Any(x => x.HasCSwitchData.GetValueOrDefault()) && ShowDetails && !NoReadyDetails) ?
                                            (data) => (data.ReadyAverageUs > 0 ? $"{data.ReadyAverageUs,5} us " : "".WithWidth(5 + 4)) :
                                            (data) => "",
                },
                FormatterType.CSwitchCount => new Formatter<MatchData>
                {
                    Header = matches.Any(x => x.HasCSwitchData.GetValueOrDefault()) && ShowDetails ? " CSwitches " : "",
                    Print = matches.Any(x => x.HasCSwitchData.GetValueOrDefault()) && ShowDetails ? (data) => "N0".WidthFormat(data.ContextSwitchCount, 10) + " " : (data) => "",
                },
                FormatterType.Frequency => new Formatter<MatchData>
                {
                    Header = ShowDetails && !NoFrequencyDetails && matches.Any(x => x.CPUUsage != null) ? "CoreData" : "",
                    Print = ShowDetails && !NoFrequencyDetails && matches.Any(x => x.CPUUsage != null) ? FormatCoreData : (data) => "",
                },
                FormatterType.ThreadCount => new Formatter<MatchData>
                {
                    Header = ThreadCount ? "#Threads " : "",
                    Print = (data) => ThreadCount ? "#" + "N0".WidthFormat(data.Threads, -9) : "",
                },
                FormatterType.ReadyDetails => new Formatter<MatchData>
                {
                    Header = ShowDetails && !NoReadyDetails && matches.Any(x => x.ReadyDetails != null) ? "ReadyDetails" : "",
                    Print = ShowDetails && !NoReadyDetails && matches.Any(x => x.ReadyDetails != null) ? FormatReadyData : (data) => "",
                },
                _ => throw new NotSupportedException($"Formatter {type} is not supported"),
            };
        }

        /// <summary>
        /// Show Ready percentiles which is aligned with ReadyAverage column for the 50% Median value to make it easy to compare values.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string FormatReadyData(MatchData data)
        {
            string lret = "";
            if (data.ReadyDetails != null)
            {
                if (data.ReadyDetails.HasDeepSleepTimes)
                {
                    double outlierSumS = 0.01 * data.ReadyDetails.CSwitchCountDeepSleep * data.ReadyDetails.Percentile99DeepSleepUs/1_000_000.0;
                    lret = $"  CPU Wakeup Ready Min/5%/25%/50%/90%/95%/99%/Max Percentiles in us: {"F1".WidthFormat(data.ReadyDetails.MinDeepSleepUs, 3)} {"F1".WidthFormat(data.ReadyDetails.Percentile5DeepSleepUs, 4)} {"F1".WidthFormat(data.ReadyDetails.Percentile25DeepSleepUs, 4)} {"F1".WidthFormat(data.ReadyDetails.Percentile50DeepSleepUs, 4)} " +
                           $"{"F0".WidthFormat(data.ReadyDetails.Percentile90DeepSleepUs, 5)} {"F0".WidthFormat(data.ReadyDetails.Percentile95DeepSleepUs, 5)} {"F0".WidthFormat(data.ReadyDetails.Percentile99DeepSleepUs, 5)} {"F0".WidthFormat(data.ReadyDetails.MaxDeepSleepUs, 7)} us >99% Sum: {"F4".WidthFormat(outlierSumS,8)} s " +
                           $"Sum: {"F4".WidthFormat(data.ReadyDetails.SumDeepSleepUs / 1_000_000.0d, 8)} s Count: {"N0".WidthFormat(data.ReadyDetails.CSwitchCountDeepSleep, 10)}";
                }
                if (lret != "" && data.ReadyDetails.HasNonDeepSleepTimes)
                {
                    lret += Environment.NewLine;
                }

                if (data.ReadyDetails.HasNonDeepSleepTimes)
                {
                    int spaces = data.ReadyDetails.HasNonDeepSleepTimes ? 5 : 0;
                    double outlierOtherSumS = 0.01 * data.ReadyDetails.CSwitchCountNonDeepSleep * data.ReadyDetails.Percentile99NonDeepSleepUs / 1_000_000.0;
                    lret += "".WithWidth(spaces) + $"  Other Ready Min/5%/25%/50%/90%/95%/99%/Max Percentiles in us: {"F1".WidthFormat(data.ReadyDetails.MinNonDeepSleepUs, 3)} {"F1".WidthFormat(data.ReadyDetails.Percentile5NonDeepSleepUs, 4)} {"F1".WidthFormat(data.ReadyDetails.Percentile25NonDeepSleepUs, 4)} {"F1".WidthFormat(data.ReadyDetails.Percentile50NonDeepSleepUs, 4)} " +
                        $"{"F0".WidthFormat(data.ReadyDetails.Percentile90NonDeepSleepUs, 5)} {"F0".WidthFormat(data.ReadyDetails.Percentile95NonDeepSleepUs, 5)} {"F0".WidthFormat(data.ReadyDetails.Percentile99NonDeepSleepUs, 5)} {"F0".WidthFormat(data.ReadyDetails.MaxNonDeepSleepUs, 7)} us " +
                        $">99% Sum: {"F4".WidthFormat(outlierOtherSumS, 8)} s Sum: {"F4".WidthFormat(data.ReadyDetails.SumNonDeepSleepUs / 1_000_000.0d, 8)} s Count: {"N0".WidthFormat(data.ReadyDetails.CSwitchCountNonDeepSleep, 10)}";
                }
            }
            return lret;
        }

        /// <summary>
        /// Show CPU consumption per CPU efficiency class.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        string FormatCoreData(MatchData data)
        {
            string lret = "";
            if (data.CPUUsage != null)
            {
                int totalCPUMs = data.CPUUsage.Sum(x => x.CPUMs);
                int totalNormalizedCPUMs = 0;
                foreach (var usage in data.CPUUsage.OrderByDescending(x => x.EfficiencyClass))
                {
                    float percentAboveNominal = (100.0f * usage.AverageMHz / data.Topology.First(x => x.Value.EfficiencyClass == usage.EfficiencyClass).Value.NominalFrequencyMHz);

                    string cpuPercent = $"{(100.0f * usage.CPUMs / totalCPUMs):F0}".WithWidth(3);
                    string normalizedCPU = "";
                    if (Normalize)
                    {
                        float factor = (percentAboveNominal / 100.0f);
                        int normalizedCPUMs = (int)(usage.CPUMs * factor);  // Scale CPU to 100% CPU Frequency
                        totalNormalizedCPUMs += normalizedCPUMs;
                        normalizedCPU = $"N0".WidthFormat(normalizedCPUMs, 10) + " ms ";
                    }
                    lret += "  [green]" + $"N0".WidthFormat(usage.CPUMs, 10) + $" ms {normalizedCPU}[/green]" + $"Class: {usage.EfficiencyClass} " + $"({cpuPercent} % CPU) on {usage.UsedCores,2} Cores " + "N0".WidthFormat(usage.AverageMHz, 5) + $" MHz ({(int)percentAboveNominal,3}% Frequency) " + $"Enabled Cores: {usage.EnabledCPUsAvg,2} Duration: {usage.LastS - usage.FirstS:F3} s" + Environment.NewLine;
                }

                if (Normalize && data.CPUUsage.Length > 1)
                {
                    lret += "".WithWidth(4) + $"[green]Normalized: {"N0".WidthFormat(totalNormalizedCPUMs, 10)} ms [/green]";
                }
            }
            return lret.TrimEnd(StringFormatExtensions.NewLineChars);
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
        internal (Dictionary<string, FileTotals>, Dictionary<string, Dictionary<ProcessKey, ProcessTotals>>) GetFileAndProcessTotals(List<MatchData> matches)
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
                    if (!fileProcessTotals.TryGetValue(GetFileGroupName(data), out Dictionary<ProcessKey, ProcessTotals> processDict))
                    {
                        processDict = new();
                        fileProcessTotals.Add(GetFileGroupName(data), processDict);
                    }

                    if (!processDict.TryGetValue(data.ProcessKey, out ProcessTotals processTotals))
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

            int? cpuClass0Ms = null;
            int? cpuClass1Ms = null;
            int? frequencyClass0MHz = null;
            int? frequencyClass1MHz = null;
            int? frequencyRelativeToNominalPercentClass0 = null;
            int? frequencyRelativeToNominalPercentClass1 = null;
            long? usedCoresClass0 = null;
            long? usedCoresClass1 = null;
            long? enabledCores = null;

            if (match.CPUUsage != null)
            {
                foreach (var usage in match.CPUUsage)
                {
                    if (usage.EfficiencyClass == (EfficiencyClass)0)
                    {
                        GetCPUUsage(usage, match, ref cpuClass0Ms, ref frequencyClass0MHz, ref frequencyRelativeToNominalPercentClass0, ref enabledCores, ref usedCoresClass0);
                    }
                    else if (usage.EfficiencyClass == (EfficiencyClass)1)
                    {
                        GetCPUUsage(usage, match, ref cpuClass1Ms, ref frequencyClass1MHz, ref frequencyRelativeToNominalPercentClass1, ref enabledCores, ref usedCoresClass1);
                    }
                }
            }

            if (!myCSVHeaderPrinted)
            {
                OpenCSVWithHeader("CSVOptions", "Test Case", "Date", "Test Time in ms", "Module", "Method", "CPU ms", "Wait ms", "Ready ms", "Ready Average us", 
                    "CPU ms Efficiency Class 0",
                    "Average Frequency Efficiency Class 0",
                    "FrequencyRelativeToNominal % Efficiency Class 0",
                    "UsedCores Class 0",
                    "CPU ms Efficiency Class 1",
                    "Average Frequency Efficiency Class 1",
                    "FrequencyRelativeToNominal % Efficiency Class 1",
                    "UsedCores Class 1",
                    "Enabled Cores",
                    "# Threads",
                    Col_AveragePriority,
                    Col_Baseline, Col_Process, Col_ProcessName, Col_Session, "Start Time", "StackDepth",
                                  "FirstLastCall Duration in s", $"First Call time in {GetAbbreviatedName(firstFormat)}", $"Last Call time in {GetAbbreviatedName(lastFormat)}", Col_CommandLine, "SourceFile", "IsNewProcess", "Module and Driver Info",
                       "DeepSleep Ready Min us",
                    "NonDeepSleep Ready Min us",
                       "DeepSleep Ready Max us",
                    "NonDeepSleep Ready Max us",
                       "DeepSleep Ready 5% Percentile us",
                    "NonDeepSleep Ready 5% Percentile us",
                       "DeepSleep Ready 25% Percentile us",
                    "NonDeepSleep Ready 25% Percentile us",
                       "DeepSleep Ready 50% Percentile us (Median)",
                    "NonDeepSleep Ready 50% Percentile us (Median)",
                       "DeepSleep Ready 90% Percentile us",
                    "NonDeepSleep Ready 90% Percentile us",
                       "DeepSleep Ready 95% Percentile us",
                    "NonDeepSleep Ready 95% Percentile us",
                       "DeepSleep Ready 99% Percentile us",
                    "NonDeepSleep Ready 99% Percentile us",
                       "DeepSleep Ready Count",
                    "NonDeepSleep Ready Count",
                    "Context Switch Count"
                );
                myCSVHeaderPrinted = true;
            }


            WriteCSVLine(CSVOptions, match.TestName, match.PerformedAt, match.DurationInMs, match.ModuleName, match.Method, match.CPUMs, match.WaitMs, match.ReadyMs, match.ReadyAverageUs, 
                cpuClass0Ms,
                frequencyClass0MHz,
                frequencyRelativeToNominalPercentClass0,
                usedCoresClass0,
                cpuClass1Ms,
                frequencyClass1MHz,
                frequencyRelativeToNominalPercentClass1,
                usedCoresClass1,
                enabledCores,
                match.Threads,
                GetNullIfZero(match.ProcessPriority),
                match.BaseLine, match.ProcessAndPid, match.Process.GetProcessName(UsePrettyProcessName), match.Process.SessionId, match.Process.StartTime, match.CPUMs / Math.Exp(match.StackDepth),
                firstLastDurationS, GetDateTimeString(match.FirstCallTime, match.SessionStart, firstFormat), GetDateTimeString(match.LastCallTime, match.SessionStart, lastFormat), NoCmdLine ? "" : match.Process.CmdLine, match.SourceFile, (match.Process.IsNew ? 1 : 0), 
                moduleDriverInfo,

                match?.ReadyDetails?.HasDeepSleepTimes == true ?    match?.ReadyDetails?.MinDeepSleepUs : (double?) null,
                match?.ReadyDetails?.HasNonDeepSleepTimes == true ? match?.ReadyDetails?.MinNonDeepSleepUs : (double?)null,

                match?.ReadyDetails?.HasDeepSleepTimes == true ?    match?.ReadyDetails?.MaxDeepSleepUs : (double?)null,
                match?.ReadyDetails?.HasNonDeepSleepTimes == true ? match?.ReadyDetails?.MaxNonDeepSleepUs : (double?)null,

                match?.ReadyDetails?.HasDeepSleepTimes == true ?    match?.ReadyDetails?.Percentile5DeepSleepUs : (double?)null,
                match?.ReadyDetails?.HasNonDeepSleepTimes == true ? match?.ReadyDetails?.Percentile5NonDeepSleepUs : (double?)null,

                match?.ReadyDetails?.HasDeepSleepTimes == true ?    match?.ReadyDetails?.Percentile25DeepSleepUs : (double?)null,
                match?.ReadyDetails?.HasNonDeepSleepTimes == true ? match?.ReadyDetails?.Percentile25NonDeepSleepUs : (double?)null,

                match?.ReadyDetails?.HasDeepSleepTimes == true ? match?.ReadyDetails?.Percentile50DeepSleepUs : (double?)null,
                match?.ReadyDetails?.HasNonDeepSleepTimes == true ? match?.ReadyDetails?.Percentile50NonDeepSleepUs : (double?)null,

                match?.ReadyDetails?.HasDeepSleepTimes == true ?    match?.ReadyDetails?.Percentile90DeepSleepUs : (double?)null,
                match?.ReadyDetails?.HasNonDeepSleepTimes == true ? match?.ReadyDetails?.Percentile90NonDeepSleepUs : (double?)null,

                match?.ReadyDetails?.HasDeepSleepTimes == true ?    match?.ReadyDetails?.Percentile95DeepSleepUs : (double?)null,
                match?.ReadyDetails?.HasNonDeepSleepTimes == true ? match?.ReadyDetails?.Percentile95NonDeepSleepUs : (double?)null,

                match?.ReadyDetails?.HasDeepSleepTimes == true ?    match?.ReadyDetails?.Percentile99DeepSleepUs : (double?)null,
                match?.ReadyDetails?.HasNonDeepSleepTimes == true ? match?.ReadyDetails?.Percentile99NonDeepSleepUs : (double?)null,

                match?.ReadyDetails?.HasDeepSleepTimes == true ? match?.ReadyDetails?.CSwitchCountDeepSleep : (double?)null,
                match?.ReadyDetails?.HasNonDeepSleepTimes == true ? match?.ReadyDetails?.CSwitchCountNonDeepSleep : (double?)null,
                match.ContextSwitchCount
            );
        }

        /// <summary>
        /// Fill values for given CPU efficiency class
        /// </summary>
        /// <param name="usage"></param>
        /// <param name="data"></param>
        /// <param name="cpuMs"></param>
        /// <param name="frequencyMHz"></param>
        /// <param name="boostPercent"></param>
        /// <param name="enabledCPUCount"></param>
        /// <param name="usedCores"></param>
        void GetCPUUsage(ICPUUsage usage, MatchData data, ref int? cpuMs, ref int? frequencyMHz, ref int? boostPercent, ref long ?enabledCPUCount, ref long? usedCores)
        {
            cpuMs = usage.CPUMs;
            frequencyMHz = usage.AverageMHz;
            enabledCPUCount = usage.EnabledCPUsAvg;
            usedCores = usage.UsedCores;
            if (data.Topology != null)
            {
                int nominalMHz = data.Topology.First(x => x.Value.EfficiencyClass == usage.EfficiencyClass).Value.NominalFrequencyMHz;
                boostPercent = (int)(100.0f * frequencyMHz / nominalMHz);
            }
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

                    PrintCPUTotal(cpu, subgroup.First().ProcessPriority, subgroup.First().Process, String.Join(" ", subgroup.Select(x => Path.GetFileNameWithoutExtension(x.SourceFile)).ToHashSet()) + $" Diff: {diff:N0} ms ", subgroup.First().SessionStart, subgroup.FirstOrDefault().Module);
                }
            }
        }

        private void WriteCSVProcessTotal(List<MatchData> matches)
        {
            OpenCSVWithHeader(Col_CSVOptions, Col_TestCase, Col_Date, Col_TestTimeinms, "CPU ms", Col_AveragePriority, Col_Baseline, Col_Process, "ParentPid", Col_ProcessName, 
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
                
                WriteCSVLine(CSVOptions, match.TestName, match.PerformedAt, match.DurationInMs, match.CPUMs, GetNullIfZero(match.ProcessPriority), match.BaseLine, match.ProcessAndPid, match.Process.ParentPid, match.Process.GetProcessName(UsePrettyProcessName), match.Process.StartTime, match.Process.CmdLine, 
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
                SortOrders.CSwitchCount,
                SortOrders.ReadyAvg,
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
                SortOrders.ReadyAvg => data.ReadyAverageUs,
                SortOrders.CSwitchCount => data.ContextSwitchCount,
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
                lret = MinMaxReadyAverageUs.IsWithin((int)cost.ReadyAverageUs);
            }

            if( lret )
            {
                lret = MinMaxCSwitch.IsWithin((int) cost.ContextSwitchCount);
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
