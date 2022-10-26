//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers.Infrastructure;
using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETWAnalyzer.Analyzers;
using System.IO;
using ETWAnalyzer.ProcessTools;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.Commands;
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
        public int MinWorkingSetMB { get; internal set; }
        public DumpCommand.SortOrders SortOrder { get; internal set; }
        public bool NoCmdLine { get; internal set; }


        public class Match
        {
            public string Process;
            public string ProcessName;
            public DateTimeOffset SessionEnd;
            public DateTime PerformedAt;
            public string TestCase;
            public string Machine;
            public uint TestDurationInMs;
            public ulong CommitedMiB;
            public ulong WorkingSetMiB;
            public ulong SharedCommitInMiB;
            public string CmdLine;
            public long DiffMb;
            public string SourceFile;
            public string Baseline;

            public DateTimeOffset SessionStart { get; internal set; }
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

        bool MemoryFilter(ProcessWorkingSet set)
        {
            return set.WorkingSetInMiB > (ulong)MinWorkingSetMB;
        }

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
            foreach(var mem1 in memory.WorkingSetsAtStart.Where(MemoryFilter).OrderBy(x=>x.CommitInMiB))
            {
                ETWProcess process = file.FindProcessByKey(mem1.Process);

                if (process == null || !IsMatchingProcessAndCmdLine(file, mem1.Process))
                {
                    continue;
                }

                long DiffMb = 0;
                foreach(var mem2 in memory.WorkingSetsAtEnd.OrderBy(x => x.CommitInMiB))
                {
                    if( mem2.Process.EqualNameAndPid(mem1.Process) )
                    {
                        DiffMb = (long) mem2.CommitInMiB - (long) mem1.CommitInMiB;
                        if ( Math.Abs(DiffMb) >= MinDiffMB)
                        {
                            matches.Add(new Match
                            {
                                CmdLine = process.CommandLineNoExe,
                                CommitedMiB = mem2.CommitInMiB,
                                SharedCommitInMiB = mem2.SharedCommitSizeInMiB,
                                SessionEnd = file.Extract.SessionEnd,
                                Process = process.GetProcessWithId(UsePrettyProcessName),
                                ProcessName = process.GetProcessName(UsePrettyProcessName),
                                WorkingSetMiB = mem2.WorkingSetInMiB,
                                DiffMb = DiffMb,

                                SourceFile = file.FileName,
                                PerformedAt = file.PerformedAt,
                                Baseline = file.Extract?.MainModuleVersion?.Version ?? "",
                                TestCase = file.TestName,
                                TestDurationInMs = (uint)file.DurationInMs,
                                Machine = file.MachineName,
                                SessionStart = file.Extract.SessionStart,
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

                if( !matches.Any( x=> x.Process == process.GetProcessWithId(UsePrettyProcessName) ) &&
                    mem2.CommitInMiB >= (ulong) MinDiffMB)
                {
                    matches.Add(new Match
                    {
                        CmdLine = process.CommandLineNoExe,
                        CommitedMiB = mem2.CommitInMiB,
                        SharedCommitInMiB = mem2.SharedCommitSizeInMiB,
                        SessionEnd = file.Extract.SessionEnd,
                        Process = process.GetProcessWithId(UsePrettyProcessName),
                        ProcessName = process.GetProcessName(UsePrettyProcessName),
                        WorkingSetMiB = mem2.WorkingSetInMiB,
                        DiffMb = 0,
                        SourceFile = file.FileName,
                        PerformedAt = file.PerformedAt,
                        Baseline = file.Extract?.MainModuleVersion?.Version ?? "",
                        TestCase = file.TestName,
                        TestDurationInMs = (uint) file.DurationInMs,
                        Machine = file.MachineName,
                        SessionStart = file.Extract.SessionStart,
                    });
                }
            }
        }

        private void WriteToCSV(List<Match> matches)
        {
            OpenCSVWithHeader("CSVOptions", "Time", "Process", "ProcessName", "Commit MiB", "Shared CommitMiB", "Working Set MiB", "Cmd Line", "Baseline", "TestCase", "TestDurationInMs", "SourceJsonFile", "Machine");
            foreach (var match in matches)
            {
                WriteCSVLine(CSVOptions, base.GetDateTimeString(match.SessionEnd, match.SessionStart, TimeFormatOption), match.Process, match.ProcessName, match.CommitedMiB, match.SharedCommitInMiB, match.WorkingSetMiB, match.CmdLine, match.Baseline, match.TestCase, match.TestDurationInMs, match.SourceFile, match.Machine);
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
            foreach (var fileGroup in matches.GroupBy(x=>x.SourceFile).OrderBy(x=>x.First().SessionEnd))
            {
                PrintFileName(fileGroup.Key, null, fileGroup.First().PerformedAt, fileGroup.First().Baseline);

                foreach (var m in fileGroup.SortAscendingGetTopNLast(SortByValue, null, TopN))
                {
                    ColorConsole.WriteEmbeddedColorLine($"[darkcyan]{GetDateTimeString(m.SessionEnd, m.SessionStart, TimeFormatOption)}[/darkcyan] [{GetColor(m.DiffMb)}]Diff: {m.DiffMb,4}[/{GetColor(m.DiffMb)}] [{GetColorTotal(m.CommitedMiB)}]Commit {m.CommitedMiB,4} MiB[/{GetColorTotal(m.CommitedMiB)}] [{GetColorTotal(m.WorkingSetMiB)}]WorkingSet {m.WorkingSetMiB,4} MiB[/{GetColorTotal(m.WorkingSetMiB)}] [{GetColorTotal(m.SharedCommitInMiB)}]Shared Commit: {m.SharedCommitInMiB,4} MiB [/{GetColorTotal(m.SharedCommitInMiB)}] ", null, true);
                    ColorConsole.WriteLine($"{m.Process} {(NoCmdLine ? "" : m.CmdLine)}", ConsoleColor.Magenta);
                }
            }
        }
    }
}
