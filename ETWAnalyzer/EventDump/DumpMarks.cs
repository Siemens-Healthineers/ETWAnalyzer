//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.EventDump
{
    class DumpMarks : DumpFileDirBase<DumpMarks.MatchData>
    {
        internal List<MatchData> myUTestData;
        public Func<string, bool> MarkerFilter { get; internal set; } = _ => true;
        public MinMaxRange<double> MinMaxMarkDiffTime { get; internal set; }

        internal class MatchData
        {
            public TestDataFile File { get; internal set; }
            public string BaseLine { get; internal set; }
            public ETWMark Mark { get; internal set; }
            public DateTimeOffset SessionStart { get; internal set; }
            public double ZeroTimeS { get; internal set; }

            public double DiffToZeroS
            {
                get => (Mark.Time - SessionStart).TotalSeconds - ZeroTimeS;
            }
        }

        public override List<MatchData> ExecuteInternal()
        {
            List<MatchData> data = ReadFileData();

            if( IsCSVEnabled)
            {
                OpenCSVWithHeader("CSVOptions", "Directory", "FileName", "Date", "Test Case", "Test Time in ms", "BaseLine", "Mark Time", "Time Diff To Zero in s", "Mark Message");

                foreach (var markEvent in data)
                {
                    WriteCSVLine(CSVOptions, Path.GetDirectoryName(markEvent.File.FileName),
                        Path.GetFileNameWithoutExtension(markEvent.File.FileName), markEvent.File.PerformedAt, markEvent.File.TestName, markEvent.File.DurationInMs, markEvent.BaseLine,
                        GetDateTimeString(markEvent.Mark.Time, markEvent.SessionStart, TimeFormatOption), (markEvent.Mark.Time - markEvent.SessionStart).TotalSeconds - markEvent.ZeroTimeS, markEvent.Mark.MarkMessage);
                }
                return data;
            }
            else
            {
                PrintSummary(data);
            }

            return data;
        }

        void PrintSummary(List<MatchData> data)
        {
            foreach (var match in data.GroupBy(x => x.File).OrderBy(x => x.Key.PerformedAt))
            {
                PrintFileName(match.Key.FileName, null, match.Key.PerformedAt, match.First().BaseLine);
                foreach (var mark in match.Where(x => MinMaxMarkDiffTime.IsWithin(x.DiffToZeroS)).OrderBy(x => x.Mark.Time) ) 
                {
                    string diff = $"{mark.DiffToZeroS:F3} s";
                    string timepoint = GetDateTimeString(mark.Mark.Time, mark.SessionStart, TimeFormatOption);

                    ColorConsole.WriteEmbeddedColorLine($"    [green]{timepoint,10} [/green] [red]DiffToZero: {diff,10}[/red] [magenta]{mark.Mark.MarkMessage}[/magenta]");
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
                    if (file?.Extract?.ETWMarks?.Count == null)
                    {
                        ColorConsole.WriteError($"Warning: File {Path.GetFileNameWithoutExtension(file.FileName)} does not contain Mark data.");
                        continue;
                    }

                    double zeroInS = GetZeroTimeInS(file.Extract);

                    foreach (ETWMark mark in file.Extract.ETWMarks.Where( x=> MarkerFilter(x.MarkMessage)) )
                    {
                        MatchData data = new()
                        {
                            SessionStart = file.Extract.SessionStart,
                            Mark = mark,
                            File = file,
                            BaseLine = file.Extract.MainModuleVersion != null ? file.Extract.MainModuleVersion.ToString() : "",
                            ZeroTimeS = zeroInS,
                        };
                        lret.Add(data);
                    }
                }
            }

            return lret;
        }
    }
}
