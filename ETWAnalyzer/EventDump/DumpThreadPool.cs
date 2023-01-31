//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers.Infrastructure;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.ThreadPool;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.EventDump
{
    /// <summary>
    /// Dump .NET Threadpool events. Currently only threadpool starvation events
    /// </summary>
    class DumpThreadPool : DumpFileDirBase<DumpThreadPool.MatchData>
    {
        public bool Merge { get; internal set; }
        public bool NoCmdLine { get; internal set; }

        internal List<MatchData> myUTestData;

        public override List<MatchData> ExecuteInternal()
        {
            List<MatchData> data = ReadFileData();

            if (IsCSVEnabled)
            {
                OpenCSVWithHeader(Col_CSVOptions, "Directory", Col_FileName, Col_Date, Col_TestCase, Col_TestTimeinms, Col_Baseline, 
                    Col_ProcessName, Col_Process, Col_CommandLine, "Starvation Time", "ThreadCount");

                foreach (var threadEvent in data)
                {
                    foreach (var starvation in threadEvent.Starvations)
                    {
                        WriteCSVLine(CSVOptions, Path.GetDirectoryName(threadEvent.File.FileName),
                            Path.GetFileNameWithoutExtension(threadEvent.File.FileName), threadEvent.File.PerformedAt, threadEvent.File.TestName, threadEvent.File.DurationInMs, threadEvent.BaseLine,
                            threadEvent.Process.GetProcessName(UsePrettyProcessName), threadEvent.Process.GetProcessWithId(UsePrettyProcessName), threadEvent.Process.CommandLineNoExe,
                            GetDateTimeString(starvation.DateTime, threadEvent.SessionStart, TimeFormatOption), starvation.NewWorkerThreadCount);
                    }
                }
                return data;
            }
            else
            {
                PrintSummary(data);

            }

            return data;
        }

        private void PrintSummary(List<MatchData> data)
        {
            foreach(var match in data.GroupBy(x => x.File).OrderBy(x=>x.Key.PerformedAt))
            {
                PrintFileName(match.Key.FileName, null, match.Key.PerformedAt, match.First().BaseLine);
                foreach(var starvation in match.OrderBy(x=>x.Starvations.Count))
                {
                    ColorConsole.Write($"{starvation.Process.GetProcessWithId(UsePrettyProcessName)}{starvation.Process.StartStopTags}", ConsoleColor.Yellow);
                    if (!NoCmdLine)
                    {
                        ColorConsole.Write(starvation.Process.CommandLineNoExe, ConsoleColor.DarkCyan);
                    }
                    Console.WriteLine();
                    decimal last = starvation?.Starvations?.Count > 0 ? starvation.Starvations[0].TotalSeconds : 0;

                    foreach(var incident in starvation.Starvations)
                    {
                        string diff = $"{incident.TotalSeconds - last:F3}";
                        string timepoint = GetDateTimeString(incident.DateTime, starvation.SessionStart, TimeFormatOption);
                        
                        ColorConsole.WriteEmbeddedColorLine($"\t[green]Starvation at {timepoint,10} [/green] [red]ThreadCount: {incident.NewWorkerThreadCount,3}[/red] [magenta]DiffSinceLast {diff,7} s[/magenta]");
                        last = incident.TotalSeconds;
                    }
                }
            }
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
                    if (file.Extract == null || file.Extract.ThreadPool == null)
                    {
                        ColorConsole.WriteError($"Warning: File {Path.GetFileNameWithoutExtension(file.FileName)} does not contain ThreadPool data");
                        continue;
                    }

                    if( file.Extract.ThreadPool.PerProcessThreadPoolStarvations.Count == 0)
                    {
                        ColorConsole.WriteWarning($"File {Path.GetFileNameWithoutExtension(file.FileName)} contains no starvation events. Either you had none, or the input ETL was not recorded with .NET ThreadPool events.");
                        continue;
                    }

                    foreach (KeyValuePair<ProcessKey, IList<ThreadPoolStarvationInfo>> starved in file.Extract.ThreadPool.PerProcessThreadPoolStarvations)
                    {
                        ETWProcess process = file.Extract.GetProcessByPID(starved.Key.Pid, starved.Key.StartTime);

                        if (!IsMatchingProcessAndCmdLine(file, starved.Key))
                        {
                            continue;
                        }

                        MatchData data = new()
                        {
                            SessionStart = file.Extract.SessionStart,
                            Process = process,
                            Starvations = starved.Value,
                            File = file,
                            BaseLine = file.Extract.MainModuleVersion != null ? file.Extract.MainModuleVersion.ToString() : "",
                        };
                        lret.Add(data);
                    }
                }
            }

            return lret;
        }

        internal class MatchData
        {
            public ETWProcess Process { get; internal set; }
            public IList<ThreadPoolStarvationInfo> Starvations { get; internal set; }
            public TestDataFile File { get; internal set; }
            public string BaseLine { get; internal set; }
            public DateTimeOffset SessionStart { get; internal set; }
        }
    }
}
