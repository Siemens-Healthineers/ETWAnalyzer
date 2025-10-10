//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Modules;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using ETWAnalyzer.Reader.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ETWAnalyzer.Commands.DumpCommand;

namespace ETWAnalyzer.EventDump
{
    class DumpMemory : DumpFileDirBase<DumpMemory.Match>
    {
        /// <summary>
        /// Dump all processes which have N top processes
        /// </summary>
        public SkipTakeRange TopN { get; internal set; }

        /// <summary>
        /// Diff threshold for one process where the Committed memory at start and trace end inside one trace file is compared
        /// </summary>
        public int MinDiffMB { get; internal set; }

        /// <summary>
        /// Diff threshold for one process which is compared across all read files. 
        /// The first last Committed memory diff is checked against this value. If the threshold is reached
        /// we include all memory readings. That allows CSV files with Excel charts which trend leaking processes over time
        /// Since we diff the first/last reading of a process we catch also transient processes which leak memory until they go away
        /// </summary>
        public long GlobalDiffMB { get; internal set; }
        public bool TotalMemory { get; internal set; }

        public MinMaxRange<decimal> MinMaxWorkingSetMiB { get; internal set; } = new();
        public MinMaxRange<decimal> MinMaxCommitMiB { get; internal set; } = new();
        public MinMaxRange<decimal> MinMaxWorkingSetPrivateMiB { get; internal set; } = new();
        public MinMaxRange<decimal> MinMaxSharedCommitMiB { get; internal set; } = new();

        public DumpCommand.SortOrders SortOrder { get; internal set; }
        public bool NoCmdLine { get; internal set; }
        public bool ShowDetails { get; internal set; }


        public TotalModes? ShowTotal { get; internal set; }

        /// <summary>
        /// Show everything, but show totals at the end
        /// </summary>
        bool IsFileTotalMode
        {
            get => ShowTotal == TotalModes.File;
        }

        /// <summary>
        /// Show per file summary
        /// </summary>
        bool IsSummary => ShowTotal switch
        {
            TotalModes.None => false,
            TotalModes.File => true,
            TotalModes.Total => true,
            TotalModes.Process => true,
            _ => true,
        };

        public class Match
        {
            public ETWProcess Process;
            public string ProcessName;
            public DateTimeOffset SessionEnd;
            public DateTime PerformedAt;
            public string TestCase;
            public string Machine;
            public uint TestDurationInMs;
            public ulong CommitedMiB;
            public ulong WorkingSetMiB;
            public ulong WorkingsetPrivateMiB;
            public ulong SharedCommitInMiB;
            public string CmdLine;
            public long DiffMb;
            public string SourceFile;
            public string Baseline;
            public ModuleDefinition Module { get; internal set; }
            public DateTimeOffset SessionStart { get; internal set; }
            public uint SessionId { get; set; }

        }


        public override List<Match> ExecuteInternal()
        {
            Lazy<SingleTest>[] tests = GetTestRuns(true, SingleTestCaseFilter, TestFileFilter);
            WarnIfNoTestRunsFound(tests);
            List<Match> matches = new();

            foreach(var test in tests)
            {
                using (test.Value) // Release deserialized ETWExtract to keep memory footprint in check
                {
                    foreach (TestDataFile file in test.Value.Files.Where(TestFileFilter))
                    {
                        if (file.Extract.MemoryUsage == null || file.Extract.MemoryUsage.WorkingSetsAtStart == null || file.Extract.MemoryUsage.WorkingSetsAtEnd == null)
                        {
                            ColorConsole.WriteError($"File {file.JsonExtractFileWhenPresent} contains no memory extract information!");
                            continue;
                        }

                        if (TotalMemory)
                        {
                            var memory = file.Extract.MemoryUsage;
                            string overcommitColor = GetColorOverCommitted(memory.MachineCommitEndMiB, (ulong) file.Extract.MemorySizeMB);
                            string commitTrendColor = GetTrendColor(memory.MachineCommitDiffMiB);
                            string activeTrendColor = GetTrendColor(memory.MachineActiveDiffMiB);
                            ColorConsole.WriteEmbeddedColorLine($"[darkcyan]{GetDateTimeString(file.Extract.SessionEnd,file.Extract.SessionStart, TimeFormatOption)}[/darkcyan] Committed: [{overcommitColor}]{memory.MachineCommitEndMiB,7}[/{overcommitColor}] CommitDiff: [{commitTrendColor}]{memory.MachineCommitDiffMiB,7} MB[/{commitTrendColor}] Active: {memory.MachineActiveEndMiB,7} MB ActiveDiff: [{activeTrendColor}]{memory.MachineActiveDiffMiB,7} MB[/{activeTrendColor}] Physical Memory: {file.Extract.MemorySizeMB} MB {Path.GetFileNameWithoutExtension(file.JsonExtractFileWhenPresent)}");
                        }
                        else
                        {
                            ExtractPerProcessMemoryInfo(matches, file);
                        }
                    }

                    if (!IsCSVEnabled && GlobalDiffMB == 0)
                    {
                        Print(matches);
                        matches.Clear();
                    }
                }
            }

            if(GlobalDiffMB > 0)
            {
                matches = FilterGlobalLeaks(matches);
                if( !IsCSVEnabled )
                {
                    Print(matches);
                }
            }

            if( CSVFile!=null)
            {
                WriteToCSV(matches);
            }

            return matches;
        }

        private List<Match> FilterGlobalLeaks(List<Match> matches)
        {
            List<Match> globals = new();

            foreach(var m in matches.GroupBy(x=>x.Process))
            {
                var sortedByTime = m.OrderBy(x => x.SessionEnd);
                Match first = m.FirstOrDefault();
                Match last = m.LastOrDefault();
                if( first != null && last != null)
                {
                    if( (long) (last.CommitedMiB - first.CommitedMiB) > GlobalDiffMB)
                    {
                        globals.AddRange(m);
                    }
                }
            }

            return globals;
        }

        bool MemoryFilter(IProcessWorkingSet set)
        {
            return MinMaxWorkingSetMiB.IsWithin( set.WorkingSetInMiB ) &&
                   MinMaxCommitMiB.IsWithin( set.CommitInMiB ) &&
                   MinMaxWorkingSetPrivateMiB.IsWithin( set.WorkingsetPrivateInMiB ) &&
                   MinMaxSharedCommitMiB.IsWithin( set.SharedCommitSizeInMiB);
        }

        /// <summary>
        /// Used by context sensitive help
        /// </summary>
        static internal readonly SortOrders[] ValidSortOrders = new[]
        {
            SortOrders.Commit,
            SortOrders.WorkingSet,
            SortOrders.Diff,
            SortOrders.SharedCommit,
            SortOrders.Default,
        };

        double SortByValue(Match data)
        {
            return SortOrder switch
            {
                 SortOrders.Commit => data.CommitedMiB,
                 SortOrders.WorkingSet => data.WorkingSetMiB,
                 SortOrders.Diff => Math.Abs(data.DiffMb),
                 SortOrders.SharedCommit => data.SharedCommitInMiB,
                 _ => data.CommitedMiB
            };
        }

        private void ExtractPerProcessMemoryInfo(List<Match> matches, TestDataFile file)
        {
            var memory = file.Extract.MemoryUsage;
            foreach(var mem1 in memory.WorkingSetsAtStart.OrderBy(x=>x.CommitInMiB))
            {
                ETWProcess process = file.FindProcessByKey(mem1.Process);

                if (process == null || !IsMatchingProcessAndCmdLine(file, mem1.Process))
                {
                    continue;
                }

                long DiffMb = 0;
                foreach(var mem2 in memory.WorkingSetsAtEnd.Where(MemoryFilter).OrderBy(x => x.CommitInMiB))
                {
                    if( mem2.Process.EqualNameAndPid(mem1.Process) )
                    {
                        DiffMb = (long) mem2.CommitInMiB - (long) mem1.CommitInMiB;
                        if ( Math.Abs(DiffMb) >= MinDiffMB)
                        {
                            ModuleDefinition processModule = ShowModuleInfo ? file.Extract?.Modules?.FindModule(process.ProcessName, process) : null;

                            if( !IsMatchingModule(processModule))
                            {
                                continue;
                            }

                            matches.Add(new Match
                            {
                                CmdLine = process.CommandLineNoExe,
                                CommitedMiB = mem2.CommitInMiB,
                                SharedCommitInMiB = mem2.SharedCommitSizeInMiB,
                                SessionEnd = file.Extract.SessionEnd,
                                Process = process,
                                SessionId = (uint) process.SessionId,
                                ProcessName = process.GetProcessName(UsePrettyProcessName),
                                WorkingSetMiB = mem2.WorkingSetInMiB,
                                WorkingsetPrivateMiB = mem2.WorkingsetPrivateInMiB,
                                DiffMb = DiffMb,

                                SourceFile = file.FileName,
                                PerformedAt = file.PerformedAt,
                                Baseline = file.Extract?.MainModuleVersion?.Version ?? "",
                                TestCase = file.TestName,
                                TestDurationInMs = (uint)file.DurationInMs,
                                Machine = file.MachineName,
                                SessionStart = file.Extract.SessionStart,
                                Module = processModule
                            }); 
                        }

                        break;
                    }
                }
            }

            foreach(var mem2 in memory.WorkingSetsAtEnd.Where(MemoryFilter).OrderBy(x=>x.CommitInMiB))
            {
                ETWProcess process = file.FindProcessByKey(mem2.Process);
                if (process == null || !IsMatchingProcessAndCmdLine(file, mem2.Process))
                {
                    continue;
                }

                if( !matches.Any( x=> x.Process == process ) &&
                    mem2.CommitInMiB >= (ulong) MinDiffMB)
                {
                    ModuleDefinition processModule = ShowModuleInfo ? file.Extract.Modules.FindModule(process.ProcessName, process) : null;

                    if (!IsMatchingModule(processModule))
                    {
                        continue;
                    }

                    matches.Add(new Match
                    {
                        CmdLine = process.CommandLineNoExe,
                        CommitedMiB = mem2.CommitInMiB,
                        SharedCommitInMiB = mem2.SharedCommitSizeInMiB,
                        SessionEnd = file.Extract.SessionEnd,
                        Process = process,
                        SessionId = (uint) process.SessionId,
                        ProcessName = process.GetProcessName(UsePrettyProcessName),
                        WorkingSetMiB = mem2.WorkingSetInMiB,
                        WorkingsetPrivateMiB = mem2.WorkingsetPrivateInMiB,
                        DiffMb = 0,
                        SourceFile = file.FileName,
                        PerformedAt = file.PerformedAt,
                        Baseline = file.Extract?.MainModuleVersion?.Version ?? "",
                        TestCase = file.TestName,
                        TestDurationInMs = (uint) file.DurationInMs,
                        Machine = file.MachineName,
                        SessionStart = file.Extract.SessionStart,
                        Module = processModule
                    });
                }
            }
        }

        internal void WriteToCSV(List<Match> matches)
        {
            OpenCSVWithHeader(Col_CSVOptions, Col_Time, Col_Process, Col_ProcessName, Col_Session, "Commit MiB", "Shared CommitMiB", "Working Set MiB", "Working Set Private MiB",
                Col_CommandLine, Col_Baseline, Col_TestCase, Col_TestTimeinms, Col_SourceJsonFile, "Machine", 
                Col_FileVersion, Col_VersionString, Col_ProductVersion, Col_ProductName, Col_Description, Col_Directory);

            foreach (var match in matches)
            {
                string fileVersion = match.Module?.Fileversion?.ToString()?.Trim() ?? "";
                string versionString = match.Module?.FileVersionStr?.Trim() ?? "";
                string productVersion = match.Module?.ProductVersionStr?.Trim() ?? "";
                string productName = match.Module?.ProductName?.Trim() ?? "";
                string description = match.Module?.Description?.Trim() ?? "";
                string directory = match.Module?.ModulePath ?? "";
                WriteCSVLine(CSVOptions, base.GetDateTimeString(match.SessionEnd, match.SessionStart, TimeFormatOption), match.Process.GetProcessWithId(UsePrettyProcessName), match.ProcessName, match.Process.SessionId,
                    match.CommitedMiB, match.SharedCommitInMiB, match.WorkingSetMiB, match.WorkingsetPrivateMiB, match.CmdLine, match.Baseline, match.TestCase, match.TestDurationInMs, match.SourceFile, match.Machine,
                    fileVersion, versionString, productVersion, productName, description, directory);
            }
        }

        string GetColorOverCommitted(ulong commit, ulong physical)
        {
            return commit > physical ? "red" : "darkcyan";
        }

        string GetColor(long diffMB)
        {
            if( diffMB < 10 )
            {
                return "darkcyan";
            }
            else if( diffMB < 50 )
            {
                return "yellow";
            }
            else
            {
                return "green";
            }
        }

        string GetTrendColor(long diff)
        {
            return diff > 0 ? "yellow" : "green";
        }

        string GetColorTotal(ulong totalMB)
        {
            if( totalMB < 100)
            {
                return "darkcyan";
            }
            if( totalMB < 300 )
            {
                return "darkyellow";
            }
            if( totalMB < 500 )
            {
                return "red";
            }
            if( totalMB < 1000)
            {
                return "yellow";
            }
            return "green";
        }

        private void Print(List<Match> matches)
        {
            int printedFiles = 0;
            foreach (var fileGroup in matches.GroupBy(x=>x.SourceFile).OrderBy(x=>x.First().SessionEnd))
            {
                PrintFileName(fileGroup.Key, null, fileGroup.First().PerformedAt, fileGroup.First().Baseline);

                long totalDiff = 0;
                ulong totalCommitedMemMiB = 0;
                ulong totalWorkingsetPrivateMemMiB = 0;
                int processCount = 0;

                foreach (var m in fileGroup.SortAscendingGetTopNLast(SortByValue, null, TopN))
                {
                    totalDiff += m.DiffMb;
                    totalCommitedMemMiB += m.CommitedMiB;
                    totalWorkingsetPrivateMemMiB += m.WorkingsetPrivateMiB;
                    processCount ++;
                    string moduleInfo = m.Module != null ? GetModuleString(m.Module, true) : "";
                    if (!IsFileTotalMode)
                    {
                        ColorConsole.WriteEmbeddedColorLine($"[darkcyan]{GetDateTimeString(m.SessionEnd, m.SessionStart, TimeFormatOption)}[/darkcyan] " +
                            $"[{GetColor(m.DiffMb)}]Diff: {m.DiffMb,4}[/{GetColor(m.DiffMb)}] " +
                            $"[{GetColorTotal(m.CommitedMiB)}]Commit {m.CommitedMiB,4} MiB[/{GetColorTotal(m.CommitedMiB)}] " +
                            $"[{GetColorTotal(m.WorkingSetMiB)}]WorkingSet {m.WorkingSetMiB,4} MiB[/{GetColorTotal(m.WorkingSetMiB)}] " +
                            (ShowDetails ? $"[{GetColorTotal(m.WorkingsetPrivateMiB)}]WorkingsetPrivate {m.WorkingsetPrivateMiB,4} MiB[/{GetColorTotal(m.WorkingsetPrivateMiB)}] " : "") +
                            $"[{GetColorTotal(m.SharedCommitInMiB)}]Shared Commit: {m.SharedCommitInMiB,4} MiB [/{GetColorTotal(m.SharedCommitInMiB)}] " +
                            (ShowDetails ? $"[yellow] Session: {m.SessionId, 2} [/yellow]" : ""), null, true);
                        ColorConsole.WriteEmbeddedColorLine($"[yellow]{m.Process.GetProcessWithId(UsePrettyProcessName)}[/yellow][grey]{GetProcessTags(m.Process, m.SessionStart)}[/grey] {(NoCmdLine ? "" : m.CmdLine)} ", ConsoleColor.DarkCyan, true);
                        ColorConsole.WriteEmbeddedColorLine($"[red]{moduleInfo}[/red]");
                    }
                    
                    printedFiles++;
                }

                if (IsSummary && printedFiles > 1)
                {
                    ColorConsole.WriteEmbeddedColorLine($"[cyan]Memory Total per File:[/cyan] [{GetTrendColor(totalDiff)}]" +
                        $"TotalDiff: {totalDiff, 6} [/{GetTrendColor(totalDiff)}] " +
                        $"[{GetColorTotal(totalCommitedMemMiB)}] TotalCommited: {totalCommitedMemMiB, 6} MiB [/{GetColorTotal(totalCommitedMemMiB)}] " +
                        ( (IsFileTotalMode || ShowDetails) ? 
                            $"[{GetColorTotal(totalWorkingsetPrivateMemMiB)}] TotalWorkingsetPrivate: {totalWorkingsetPrivateMemMiB, 6} MiB [/{GetColorTotal(totalWorkingsetPrivateMemMiB)}]" : 
                            "")+
                        $"[Darkyellow] Processes: {processCount} [/Darkyellow]");
                }

            }
        }
    }
}
