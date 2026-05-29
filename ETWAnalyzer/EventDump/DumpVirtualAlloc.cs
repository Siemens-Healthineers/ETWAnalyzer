//// SPDX-FileCopyrightText:  © 2026 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Common;
using ETWAnalyzer.Extract.VirtualAlloc;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ETWAnalyzer.EventDump
{
    /// <summary>
    /// Dump VirtualAlloc data showing per-process statistics or aggregated unreleased allocations per stack.
    /// </summary>
    internal class DumpVirtualAlloc : DumpFileDirBase<DumpVirtualAlloc.MatchData>
    {
        internal class MatchData
        {
            public TestDataFile File { get; set; }
            public IETWExtract Extract { get; set; }
            public IVirtualAllocData VirtualAllocData { get; set; }
        }

        /// <summary>
        /// Unit testing only. ReadFileData will return this list instead of real data.
        /// </summary>
        internal List<MatchData> myUTestData = null;

        /// <summary>
        /// When true show individual allocations (detailed view). Overrides ShowStats.
        /// </summary>
        public bool ShowDetails { get; internal set; }

        /// <summary>
        /// Control total display. None suppresses it.
        /// </summary>
        public DumpCommand.TotalModes? ShowTotal { get; internal set; }

        /// <summary>
        /// Limit number of processes shown. Default takes all.
        /// </summary>
        public SkipTakeRange TopN { get; internal set; } = new();

        /// <summary>
        /// Limit number of stack groups shown per process. Default is 10.
        /// </summary>
        public SkipTakeRange TopNStacks { get; internal set; } = new(10, 0);

        /// <summary>
        /// Show stack traces for allocations.
        /// </summary>
        public bool ShowStack { get; internal set; }

        /// <summary>
        /// Do not show command line
        /// </summary>
        public bool NoCmdLine { get; internal set; }

        /// <summary>
        /// Filter stack traces
        /// </summary>
        public KeyValuePair<string, Func<string, bool>> StackFilter { get; internal set; } = new(null, _ => true);

        /// <summary>
        /// Filter by specific stack indices (semicolon separated). When set, prints individual allocation events for those stacks.
        /// </summary>
        public HashSet<StackIdx> StackIdxFilter { get; internal set; }

        /// <summary>
        /// Filter stacks by not released size (in bytes).
        /// </summary>
        public MinMaxRange<long> MinMaxNotReleasedSize { get; internal set; } = new();

        /// <summary>
        /// Filter stacks by not released commit count.
        /// </summary>
        public MinMaxRange<long> MinMaxNotReleasedCount { get; internal set; } = new();

        /// <summary>
        /// Filter allocations by time in seconds since trace start.
        /// </summary>
        public MinMaxRange<double> MinMaxAllocTime { get; internal set; } = new();

        /// <summary>
        /// Sort order for stats view
        /// </summary>
        public DumpCommand.SortOrders SortOrder { get; internal set; } = DumpCommand.SortOrders.NotReleasedSize;

        // Column name constants
        const string Col_CommitCount = "CommitCount";
        const string Col_CommitSize = "CommitSize";
        const string Col_FreedCount = "FreedCount";
        const string Col_FreedSize = "FreedSize";
        const string Col_NotReleasedCount = "NotReleasedCount";
        const string Col_NotReleasedSize = "NotReleasedSize";
        const string Col_MaxCommitSize = "MaxCommitSize";
        const string Col_StackCount = "StackCount";
        const string Col_StackSize = "StackSize";

        /// <summary>
        /// Valid column names which can be enabled for more flexible output
        /// </summary>
        public static string[] ColumnNames =
        {
            Col_NotReleasedCount, Col_NotReleasedSize, Col_CommitCount, Col_CommitSize, 
            Col_FreedCount, Col_FreedSize, Col_MaxCommitSize,
        };

        bool GetColumnEnable(string columnName)
        {
            return columnName switch
            {
                Col_CommitCount => GetOverrideFlag(Col_CommitCount, true),
                Col_CommitSize => GetOverrideFlag(Col_CommitSize, true),
                Col_FreedCount => GetOverrideFlag(Col_FreedCount, true),
                Col_FreedSize => GetOverrideFlag(Col_FreedSize, true),
                Col_NotReleasedCount => GetOverrideFlag(Col_NotReleasedCount, true),
                Col_NotReleasedSize => GetOverrideFlag(Col_NotReleasedSize, true),
                Col_MaxCommitSize => GetOverrideFlag(Col_MaxCommitSize, true),
                Col_Process => GetOverrideFlag(Col_Process, true),
                Col_StackCount => GetOverrideFlag(Col_StackCount, true),
                Col_StackSize => GetOverrideFlag(Col_StackSize, true),
                _ => throw new NotSupportedException($"Column {columnName} is not configurable."),
            };
        }

        /// <summary>
        /// Valid sort orders for VirtualAlloc command
        /// </summary>
        internal static DumpCommand.SortOrders[] ValidSortOrders = new DumpCommand.SortOrders[]
        {
            DumpCommand.SortOrders.NotReleasedSize,
            DumpCommand.SortOrders.NotReleasedCount,
            DumpCommand.SortOrders.CommitSize,
            DumpCommand.SortOrders.CommitCount,
            DumpCommand.SortOrders.FreedSize,
            DumpCommand.SortOrders.FreedCount,
            DumpCommand.SortOrders.MaxCommitSize,
        };

        public override List<MatchData> ExecuteInternal()
        {
            List<MatchData> lret = ReadFileData();
            if (lret.Count > 0)
            {
                if (IsCSVEnabled)
                {
                    WriteCSVData(lret);
                }
                else
                {
                    PrintMatches(lret);
                }
            }
            return lret;
        }

        private List<MatchData> ReadFileData()
        {
            if (myUTestData != null)
            {
                return myUTestData;
            }

            var lret = new List<MatchData>();

            Lazy<SingleTest>[] runData = GetTestRuns(true, SingleTestCaseFilter, TestFileFilter);
            WarnIfNoTestRunsFound(runData);

            foreach (var test in runData)
            {
                foreach (TestDataFile file in test.Value.Files)
                {
                    if (file?.Extract?.VirtualAlloc == null)
                    {
                        ColorConsole.WriteError($"Warning: File {GetPrintFileName(file.FileName)} does not contain VirtualAlloc data.");
                        continue;
                    }

                    IVirtualAllocData virtualAllocData = file.Extract.VirtualAlloc;
                    if (virtualAllocData.PerProcessStats.Count == 0 && virtualAllocData.VirtualAllocEvents.Count == 0)
                    {
                        continue;
                    }

                    lret.Add(new MatchData
                    {
                        File = file,
                        Extract = file.Extract,
                        VirtualAllocData = virtualAllocData,
                    });
                }
            }

            return lret;
        }

        private void WriteCSVData(List<MatchData> matches)
        {
            if (ShowDetails)
            {
                WriteCSVAllocations(matches);
            }
            else
            {
                WriteCSVStats(matches);
            }
        }

        const string UnitPostFix = "InBytes";

        private void WriteCSVStats(List<MatchData> matches)
        {
            OpenCSVWithHeader(Col_CSVOptions, Col_FileName, Col_Date, Col_TestCase, Col_TestTimeinms, Col_Baseline,
                              Col_Process, Col_ProcessName, Col_StartTime, Col_CommandLine,
                              Col_NotReleasedCount, Col_NotReleasedSize + UnitPostFix,
                              Col_CommitCount, Col_CommitSize+ UnitPostFix, Col_FreedCount, Col_FreedSize+ UnitPostFix,
                              Col_MaxCommitSize+ UnitPostFix);

            foreach (var match in matches)
            {
                foreach (var stats in GetFilteredStats(match))
                {
                    ETWProcess process = match.Extract.GetProcess(stats.ProcessIdx);
                    WriteCSVLine(CSVOptions, Path.GetFileNameWithoutExtension(match.File.FileName), match.File.PerformedAt, match.File.TestName, match.File.DurationInMs, match.File.Extract.MainModuleVersion?.ToString(),
                        process.GetProcessWithId(UsePrettyProcessName), process.GetProcessName(UsePrettyProcessName),
                        process.StartTime, NoCmdLine ? "" : process.CommandLineNoExe,
                        stats.NotReleasedCommitCount, stats.NotReleasedSizeInBytes,
                        stats.CommitCount, stats.CommittedSizeInBytes, stats.FreedCount, stats.FreedSizeInBytes,
                        stats.MaxCommitSizeInBytes);
                }
            }
        }

        private void WriteCSVAllocations(List<MatchData> matches)
        {
            OpenCSVWithHeader(Col_CSVOptions, Col_FileName, Col_Date, Col_TestCase, Col_TestTimeinms, Col_Baseline,
                              Col_Process, Col_ProcessName, Col_StartTime, Col_CommandLine,
                              "StackIdx", "Stack", "Flags", "BaseAddress", "Size"+ UnitPostFix, "TimeInS");

            foreach (var match in matches)
            {
                IStackCollection stacks = match.VirtualAllocData.Stacks;
                Dictionary<ETWProcessIndex, List<StackGroup>> grouped = GetGroupedAllocations(match);

                foreach (var processGroup in grouped)
                {
                    ETWProcess process = match.Extract.GetProcess(processGroup.Key);

                    foreach (var stackGroup in processGroup.Value)
                    {
                        string stackStr = stacks?.GetStack(stackGroup.StackIdx) ?? "";

                        foreach (IVirtualAllocEvent ev in stackGroup.Events)
                        {
                            WriteCSVLine(CSVOptions, Path.GetFileNameWithoutExtension(match.File.FileName), match.File.PerformedAt, match.File.TestName, match.File.DurationInMs, match.File.Extract.MainModuleVersion?.ToString(),
                                process.GetProcessWithId(UsePrettyProcessName), process.GetProcessName(UsePrettyProcessName),
                                process.StartTime, NoCmdLine ? "" : process.CommandLineNoExe,
                                stackGroup.StackIdx, stackStr, ev.Flags, $"0x{ev.BaseAddress:X}", ev.Size, ev.TimeInSecondsSinceTraceStart);
                        }
                    }
                }
            }
        }

        private void PrintMatches(List<MatchData> matches)
        {
            if (ShowDetails)
            {
                PrintAllocations(matches);
            }
            else
            {
                PrintStats(matches);
            }
        }

        private void PrintStats(List<MatchData> matches)
        {
            long totalNotReleasedCount = 0;
            long totalNotReleasedSize = 0;
            int totalProcessCount = 0;

            const int SizeWidth = 15;
            const int CountWidth = 16;

            // Print header
            string header = "  ";
            if (GetColumnEnable(Col_NotReleasedSize))
            {
                header += $"[red]{Col_NotReleasedSize.WithWidth(SizeWidth)}[/red] ";
            }
            if (GetColumnEnable(Col_NotReleasedCount))
            {
                header += $"[red]{Col_NotReleasedCount.WithWidth(CountWidth)}[/red] ";
            }
            if (GetColumnEnable(Col_CommitSize))
            {
                header += $"[yellow]{Col_CommitSize.WithWidth(SizeWidth)}[/yellow] ";
            }
            if (GetColumnEnable(Col_CommitCount))
            {
                header += $"[yellow]{Col_CommitCount.WithWidth(CountWidth)}[/yellow] ";
            }
            if (GetColumnEnable(Col_FreedSize))
            {
                header += $"[cyan]{Col_FreedSize.WithWidth(SizeWidth)}[/cyan] ";
            }
            if (GetColumnEnable(Col_FreedCount))
            {
                header += $"[cyan]{Col_FreedCount.WithWidth(CountWidth)}[/cyan] ";
            }
            if (GetColumnEnable(Col_MaxCommitSize))
            {
                header += $"[darkcyan]{Col_MaxCommitSize.WithWidth(SizeWidth)}[/darkcyan] ";
            }
            if (GetColumnEnable(Col_Process))
            {
                header += $"[grey]{Col_Process}[/grey]";
            }
            ColorConsole.WriteEmbeddedColorLine(header);

            foreach (var match in matches)
            {
                PrintFileName(match.File.FileName, null, match.File.PerformedAt, match.File.Extract.MainModuleVersion?.ToString());

                var filteredStats = GetFilteredStats(match);
                int shown = 0;

                foreach (var stats in filteredStats)
                {
                    ETWProcess process = match.Extract.GetProcess(stats.ProcessIdx);
                    string processName = process.GetProcessWithId(UsePrettyProcessName);

                    string line = "  ";
                    if (GetColumnEnable(Col_NotReleasedSize))
                    {
                        line += $"[red]{FormatBytes(stats.NotReleasedSizeInBytes).WithWidth(SizeWidth)}[/red] ";
                    }
                    if (GetColumnEnable(Col_NotReleasedCount))
                    {
                        line += $"[red]{stats.NotReleasedCommitCount.ToString().WithWidth(CountWidth)}[/red] ";
                    }
                    if (GetColumnEnable(Col_CommitSize))
                    {
                        line += $"[yellow]{FormatBytes(stats.CommittedSizeInBytes).WithWidth(SizeWidth)}[/yellow] ";
                    }
                    if (GetColumnEnable(Col_CommitCount))
                    {
                        line += $"[yellow]{stats.CommitCount.ToString().WithWidth(CountWidth)}[/yellow] ";
                    }
                    if (GetColumnEnable(Col_FreedSize))
                    {
                        line += $"[cyan]{FormatBytes(stats.FreedSizeInBytes).WithWidth(SizeWidth)}[/cyan] ";
                    }
                    if (GetColumnEnable(Col_FreedCount))
                    {
                        line += $"[cyan]{stats.FreedCount.ToString().WithWidth(CountWidth)}[/cyan] ";
                    }
                    if (GetColumnEnable(Col_MaxCommitSize))
                    {
                        line += $"[darkcyan]{FormatBytes(stats.MaxCommitSizeInBytes).WithWidth(SizeWidth)}[/darkcyan] ";
                    }
                    if (GetColumnEnable(Col_Process))
                    {
                        line += $"[grey]{processName}{GetProcessTags(process, match.File.Extract.SessionStart)}[/grey]";
                    }

                    ColorConsole.WriteEmbeddedColorLine(line, ConsoleColor.DarkCyan, true); 
                    if (!NoCmdLine)
                    {
                        ColorConsole.WriteLine($" {process.CommandLineNoExe}", ConsoleColor.DarkCyan); // write cmdline not with embedded coloring because cmd line arguments might distort output coloring!
                    }
                    else
                    {
                        ColorConsole.WriteLine("");
                    }
                    

                    totalNotReleasedCount += stats.NotReleasedCommitCount;
                    totalNotReleasedSize += stats.NotReleasedSizeInBytes;
                    shown++;
                }

                totalProcessCount += shown;
            }

            PrintTotal(totalProcessCount, totalNotReleasedCount, totalNotReleasedSize);
        }

        void PrintTotal(int processCount, long totalNotReleasedCount, long totalNotReleasedSize)
        {
            if (ShowTotal != DumpCommand.TotalModes.None && totalNotReleasedCount > 0)
            {
                ColorConsole.WriteEmbeddedColorLine(
                $"  Total: Processes: {processCount} [red]NotReleased: {totalNotReleasedCount} commits, {FormatBytes(totalNotReleasedSize)}[/red] memory");
            }
        }

        private void PrintAllocations(List<MatchData> matches)
        {
            long totalNotReleasedCount = 0;
            long totalNotReleasedSize = 0;
            int totalProcessCount = 0;

            const int SizeWidth = 15;
            const int CountWidth = 16;

            foreach (var match in matches)
            {
                PrintFileName(match.File.FileName, null, match.File.PerformedAt, match.File.Extract.MainModuleVersion?.ToString());

                IStackCollection stacks = (ShowStack || StackFilter.Key != null || StackIdxFilter != null) ? match.VirtualAllocData.Stacks : null;
                var grouped = GetGroupedAllocations(match);
                int processesShown = 0;

                foreach (var processGroup in grouped)
                {
                    ETWProcess process = match.Extract.GetProcess(processGroup.Key);
                    string processName = process.GetProcessWithId(UsePrettyProcessName);
                    long processNotReleasedSize = processGroup.Value.Sum(x => x.TotalSize);
                    long processNotReleasedCount = processGroup.Value.Sum(x => x.Count);

                    // When StackIdxFilter is set, skip processes that have no matching stacks
                    if (StackIdxFilter != null && !processGroup.Value.Any(sg => StackIdxFilter.Contains(sg.StackIdx)))
                    {
                        continue;
                    }

                    if (GetColumnEnable(Col_Process))
                    {
                        ColorConsole.WriteEmbeddedColorLine($"  [grey]{processName}{GetProcessTags(process, match.File.Extract.SessionStart)}[/grey]", null, true);

                        string processHeader = "";
                        if (GetColumnEnable(Col_NotReleasedSize))
                        {
                            processHeader += $"[red] {FormatBytes(processNotReleasedSize)}[/red]";
                        }
                        if (GetColumnEnable(Col_NotReleasedCount))
                        {
                            processHeader += $" [red]{processNotReleasedCount} commits[/red]";
                        }
                        ColorConsole.WriteEmbeddedColorLine(processHeader, ConsoleColor.DarkCyan, true);

                        if (!NoCmdLine)
                        {
                            ColorConsole.WriteLine($" {process.CommandLineNoExe}", ConsoleColor.DarkCyan); // do not write command line via WriteEmbeddedColorLine where command line arguments might distort coloring!
                        }
                        else
                        {
                            ColorConsole.WriteLine("");
                        }
                    }

                    // Print stack group header
                    string stackHeader = "    ";
                    if (GetColumnEnable(Col_StackSize))
                    {
                        stackHeader += $"[red]{Col_NotReleasedSize.WithWidth(SizeWidth)}[/red] ";
                    }
                    if (GetColumnEnable(Col_StackCount))
                    {
                        stackHeader += $"[red]{Col_NotReleasedCount.WithWidth(CountWidth)}[/red] ";
                    }
                    stackHeader += "[yellow]StackIdx[/yellow]";
                    ColorConsole.WriteEmbeddedColorLine(stackHeader);

                    foreach (var stackGroup in processGroup.Value)
                    {
                        if (StackIdxFilter != null && !StackIdxFilter.Contains(stackGroup.StackIdx))
                        {
                            continue;
                        }

                        string stackLine = "    ";
                        if (GetColumnEnable(Col_StackSize))
                        {
                            stackLine += $"[red]{FormatBytes(stackGroup.TotalSize).WithWidth(SizeWidth)}[/red] ";
                        }
                        if (GetColumnEnable(Col_StackCount))
                        {
                            stackLine += $"[red]{stackGroup.Count.ToString().WithWidth(CountWidth)}[/red] ";
                        }
                        stackLine += $"[yellow]{stackGroup.StackIdx}[/yellow]";
                        ColorConsole.WriteEmbeddedColorLine(stackLine);

                        if (StackIdxFilter != null)
                        {
                            DateTimeOffset sessionStart = match.File.Extract.SessionStart;
                            foreach (var ev in stackGroup.Events)
                            {
                                DateTimeOffset eventTime = sessionStart.AddSeconds(ev.TimeInSecondsSinceTraceStart);
                                string timeStr = GetDateTimeString(eventTime, sessionStart, TimeFormatOption, true);
                                ColorConsole.WriteEmbeddedColorLine($"      [green]{timeStr}[/green] [red]{FormatBytes(ev.Size).WithWidth(SizeWidth)}[/red] 0x{ev.BaseAddress:X}");
                            }
                        }

                        if (ShowStack && stacks != null)
                        {
                            string stackStr = stacks.GetStack(stackGroup.StackIdx);
                            if (!String.IsNullOrEmpty(stackStr))
                            {
                                string[] lines = stackStr.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var line in lines)
                                {
                                    Console.WriteLine($"      {line.TrimEnd()}");
                                }
                            }
                        }
                    }

                    totalNotReleasedCount += processNotReleasedCount;
                    totalNotReleasedSize += processNotReleasedSize;
                    processesShown++;
                }

                totalProcessCount += processesShown;
            }

            PrintTotal(totalProcessCount, totalNotReleasedCount, totalNotReleasedSize);
        }

        /// <summary>
        /// Get per-process stats filtered by process name, sorted by SortOrder.
        /// </summary>
        private IVirtualAllocProcessStats[] GetFilteredStats(MatchData match)
        {
            var stats = new List<IVirtualAllocProcessStats>();
            foreach (var s in match.VirtualAllocData.PerProcessStats)
            {
                ETWProcess process = match.Extract.GetProcess(s.ProcessIdx);
                if (!IsMatchingProcessAndCmdLine(process))
                {
                    continue;
                }
                stats.Add(s);
            }

            return stats.SortAscendingGetTopNLast(SortByStats, null, TopN);
        }

        /// <summary>
        /// Sort key selector for per-process stats.
        /// </summary>
        private decimal SortByStats(IVirtualAllocProcessStats stats)
        {
            return SortOrder switch
            {
                DumpCommand.SortOrders.CommitCount => stats.CommitCount,
                DumpCommand.SortOrders.CommitSize => stats.CommittedSizeInBytes,
                DumpCommand.SortOrders.FreedCount => stats.FreedCount,
                DumpCommand.SortOrders.FreedSize => stats.FreedSizeInBytes,
                DumpCommand.SortOrders.NotReleasedCount => stats.NotReleasedCommitCount,
                DumpCommand.SortOrders.NotReleasedSize => stats.NotReleasedSizeInBytes,
                DumpCommand.SortOrders.MaxCommitSize => stats.MaxCommitSizeInBytes,
                _ => stats.NotReleasedSizeInBytes,
            };
        }

        /// <summary>
        /// Group unreleased allocations by process then by stack, returning top N processes with top N methods each.
        /// </summary>
        private Dictionary<ETWProcessIndex, List<StackGroup>> GetGroupedAllocations(MatchData match)
        {
            IStackCollection stacks = StackFilter.Key != null ? match.VirtualAllocData.Stacks : null;

            var result = new Dictionary<ETWProcessIndex, List<StackGroup>>();

            // Group events by process
            var byProcess = new Dictionary<ETWProcessIndex, Dictionary<StackIdx, List<IVirtualAllocEvent>>>();
            foreach (var ev in match.VirtualAllocData.VirtualAllocEvents)
            {
                ETWProcess process = match.Extract.GetProcess(ev.ProcessIdx);
                if (!IsMatchingProcessAndCmdLine(process))
                {
                    continue;
                }

                if (!MinMaxAllocTime.IsWithin(ev.TimeInSecondsSinceTraceStart))
                {
                    continue;
                }

                if (!byProcess.TryGetValue(ev.ProcessIdx, out var byStack))
                {
                    byStack = new Dictionary<StackIdx, List<IVirtualAllocEvent>>();
                    byProcess[ev.ProcessIdx] = byStack;
                }

                if (!byStack.TryGetValue(ev.StackIdx, out var events))
                {
                    events = new List<IVirtualAllocEvent>();
                    byStack[ev.StackIdx] = events;
                }

                events.Add(ev);
            }

            // Sort processes by sort order, apply TopN using SortAscendingGetTopNLast
            var processEntries = byProcess
                .Select(kvp => new ProcessAllocEntry { ProcessIdx = kvp.Key, Stacks = kvp.Value, TotalSize = kvp.Value.Sum(s => s.Value.Sum(e => e.Size)), Count = kvp.Value.Sum(s => s.Value.Count) })
                .ToList();

            ProcessAllocEntry[] sortedProcesses = processEntries.SortAscendingGetTopNLast(SortByProcessAlloc, null, TopN);

            foreach (ProcessAllocEntry processEntry in sortedProcesses)
            {
                var filteredStackGroups = processEntry.Stacks
                    .Select(kvp => new StackGroup
                    {
                        StackIdx = kvp.Key,
                        Count = kvp.Value.Count,
                        TotalSize = kvp.Value.Sum(e => e.Size),
                        Events = kvp.Value,
                    })
                    .Where(sg =>
                    {
                        if (StackFilter.Key == null) return true;
                        string stackStr = stacks?.GetStack(sg.StackIdx) ?? "";
                        return StackFilter.Value(stackStr);
                    })
                    .Where(sg => MinMaxNotReleasedSize.IsWithin(sg.TotalSize))
                    .Where(sg => MinMaxNotReleasedCount.IsWithin((long)sg.Count))
                    .ToList();

                var stackGroups = filteredStackGroups.SortAscendingGetTopNLast<StackGroup, long>(x => x.TotalSize, null, TopNStacks).ToList();

                // Ensure stack groups matching StackIdxFilter are always included
                if (StackIdxFilter != null)
                {
                    var missing = filteredStackGroups.Where(sg => StackIdxFilter.Contains(sg.StackIdx) && !stackGroups.Contains(sg)).ToList();
                    stackGroups.AddRange(missing);
                }

                if (stackGroups.Count > 0)
                {
                    result[processEntry.ProcessIdx] = stackGroups;
                }
            }

            return result;
        }

        /// <summary>
        /// Format byte count to human readable string.
        /// </summary>
        static string FormatBytes(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (Math.Abs(bytes) >= GB) return $"{bytes / (double)GB:F2} GB";
            if (Math.Abs(bytes) >= MB) return $"{bytes / (double)MB:F2} MB";
            if (Math.Abs(bytes) >= KB) return $"{bytes / (double)KB:F2} KB";
            return $"{bytes} B";
        }

        /// <summary>
        /// Sort key selector for process allocation entries.
        /// </summary>
        private decimal SortByProcessAlloc(ProcessAllocEntry entry)
        {
            return SortOrder switch
            {
                DumpCommand.SortOrders.CommitCount => entry.Count,
                DumpCommand.SortOrders.CommitSize => entry.TotalSize,
                DumpCommand.SortOrders.FreedCount => entry.Count,
                DumpCommand.SortOrders.FreedSize => entry.TotalSize,
                DumpCommand.SortOrders.NotReleasedCount => entry.Count,
                DumpCommand.SortOrders.NotReleasedSize => entry.TotalSize,
                DumpCommand.SortOrders.MaxCommitSize => entry.TotalSize,
                _ => entry.TotalSize,
            };
        }

        /// <summary>
        /// Per-process allocation entry used for sorting.
        /// </summary>
        internal class ProcessAllocEntry
        {
            public ETWProcessIndex ProcessIdx { get; set; }
            public Dictionary<StackIdx, List<IVirtualAllocEvent>> Stacks { get; set; }
            public long TotalSize { get; set; }
            public int Count { get; set; }
        }

        /// <summary>
        /// Group of allocations sharing the same stack trace.
        /// </summary>
        internal class StackGroup
        {
            public StackIdx StackIdx { get; set; }
            public int Count { get; set; }
            public long TotalSize { get; set; }
            public List<IVirtualAllocEvent> Events { get; set; }
        }
    }
}
