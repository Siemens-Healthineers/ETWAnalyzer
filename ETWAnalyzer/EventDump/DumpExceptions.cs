//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers;
using ETWAnalyzer.Analyzers.Infrastructure;
using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Exceptions;
using ETWAnalyzer.Extractors;
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
    class DumpExceptions : DumpFileDirBase<DumpExceptions.MatchData>
    {
        public KeyValuePair<string, Func<string, bool>> TypeFilter { get; internal set; }
        public KeyValuePair<string, Func<string, bool>> MessageFilter { get; internal set; }
        public KeyValuePair<string,Func<string, bool>> StackFilter { get; internal set; }

        public bool FilterExceptions { get; internal set; }
        public bool ShowStack { get; internal set; }

        public int CutStackMin { get; internal set; }
        public int CutStackMax { get; internal set; }
        public bool NoCmdLine { get; internal set; }
        public MinMaxRange<double> MinMaxExTimeS { get; internal set; }
        public int MaxMessage { get; internal set; } = DumpCommand.MaxMessageLength;

        public class MatchData
        {
            public string Message;
            public string Type;
            public DateTimeOffset TimeStamp;
            public string Process;
            public string Stack;
            public ETWProcess ETWProcess;
            public string SourceFile;
            internal DateTime PerformedAt;
            public DateTimeOffset SessionStart;

            public string TestCase { get; internal set; }
            public double ZeroTimeS { get; internal set; }
        }

        readonly ExceptionFilters myFilters = ExceptionExtractor.ExceptionFilters();

        /// <summary>
        /// Messages with longer strings are truncated at console output.
        /// </summary>
        const int ExceptionMaxMessageLength = 500;

        public override List<MatchData> ExecuteInternal()
        {
            var testsOrderedByTime = GetTestRuns(true, SingleTestCaseFilter, TestFileFilter);
            WarnIfNoTestRunsFound(testsOrderedByTime);

            if ( CutStackMax == 0 )
            {
                CutStackMax = 50; // by default show the first 50 stack frames
            }

            List<MatchData> matches = new();

            foreach (var test in testsOrderedByTime)
            {
                using (test.Value) // Release deserialized ETWExtract to keep memory footprint in check
                {
                    foreach (TestDataFile file in test.Value.Files.Where(TestFileFilter))
                    {
                        AddRelevantExceptions(file, matches);
                    }

                    // print output while we are reading
                    if (!IsCSVEnabled)
                    {
                        PrintMatches(matches);
                        matches.Clear();
                    }
                }
            }

            if (IsCSVEnabled)
            {
                WriteToCSVFile(matches);
            }

            return matches;
        }

        private void WriteToCSVFile(List<MatchData> matches)
        {
            OpenCSVWithHeader("CSVOptions", "Time", "Exception Type", "Message", "Process", "Command Line", "StackTrace", "TestCase", "PerformedAt", "SourceFile");
            foreach(var match in matches)
            {
                WriteCSVLine(CSVOptions, GetDateTimeString(match.TimeStamp, match.SessionStart, TimeFormatOption), match.Type, match.Message, match.Process, match.ETWProcess.CmdLine, match.Stack, match.TestCase, GetDateTimeString(match.PerformedAt), match.SourceFile);
            }
        }

        private void AddRelevantExceptions(TestDataFile file, List<MatchData> matches)
        {
            if( file?.Extract?.Exceptions == null)
            {
                ColorConsole.WriteError($"File {file.JsonExtractFileWhenPresent} does not contain exception data.");
                return;
            }

            double zeroTimeS = GetZeroTimeInS(file.Extract);

            bool IsMatchingException(ExceptionEventForQuery arg)
            {
                return
                    IsMatchingProcessAndCmdLine(file, arg.Process) &&
                    MessageFilter.Value(arg.Message) &&
                    TypeFilter.Value(arg.Type) && 
                    StackFilter.Value(arg.Stack) &&
                    MinMaxExTimeS.IsWithin( (arg.Time - file.Extract.SessionStart).TotalSeconds - zeroTimeS) && 
                    (FilterExceptions ? myFilters.IsRelevantException(arg.Process.ProcessWithID, arg.Type, arg.Message, arg.Stack) : true);
            };

            foreach (var ex in file.Extract.Exceptions.Exceptions.Where(IsMatchingException))
            {
                var data = new MatchData
                {
                    Message = ex.Message,
                    Type = ex.Type,
                    Process = ex.Process.GetProcessWithId(UsePrettyProcessName),
                    ETWProcess = ex.Process,
                    TimeStamp = ex.Time.AddSeconds(-1.0d*zeroTimeS),
                    Stack = ex.Stack,
                    SourceFile = file.JsonExtractFileWhenPresent,
                    TestCase = file.TestName,
                    PerformedAt = file.PerformedAt,
                    SessionStart = file.Extract.SessionStart,
                    ZeroTimeS = zeroTimeS,
                };

                matches.Add(data);
            }
        }

        private void PrintMatches(List<MatchData> matches)
        {
            foreach(var byFile in matches.GroupBy(x=>x.SourceFile).OrderBy(x=>x.First().PerformedAt) )
            {
                ColorConsole.WriteLine(Path.GetFileNameWithoutExtension(byFile.Key), ConsoleColor.Cyan);
                foreach (var processExceptions in matches.GroupBy(x => x.Process))
                {
                    MatchData firstGroup = processExceptions.First();
                    ColorConsole.WriteEmbeddedColorLine($"[magenta]{processExceptions.Key} {firstGroup.ETWProcess.StartStopTags}[/magenta] {(NoCmdLine ? String.Empty : firstGroup.ETWProcess.CommandLineNoExe)}", ConsoleColor.DarkCyan);
                    foreach (var byType in processExceptions.GroupBy(x => x.Type))
                    {
                        ColorConsole.WriteLine($"\t{byType.Key}", ConsoleColor.Green);
                        foreach (var byMessage in byType.GroupBy(x => x.Message))
                        {
                            ColorConsole.WriteLine($"\t\t{byMessage.Count(),-4} {TruncateMessage(byMessage.Key)}");

                            foreach (var byStack in byMessage.GroupBy(x => ShowStack ? x.Stack : null)) // if we do not display stacks do not group by stack
                            {
                                List<MatchData> chunk = new();
                                if (ShowStack)
                                {
                                    foreach (var line in TruncateStack(byStack.Key))
                                    {
                                        ColorConsole.WriteLine(line);
                                    }
                                }

                                foreach (MatchData data in byStack.OrderBy(x => x.TimeStamp))
                                {
                                    if (TypeFilter.Key != null || MessageFilter.Key != null || StackFilter.Key != null)
                                    {
                                        if (TimeFormatOption == TimeFormats.s || TimeFormatOption == TimeFormats.second)
                                        {
                                            ColorConsole.WriteLine($"\t\t\t{GetDateTimeString(data.TimeStamp, data.SessionStart, TimeFormatOption)}");
                                        }
                                        else
                                        {
                                            ColorConsole.WriteLine($"\t\t\t{GetDateTimeString(data.TimeStamp, data.SessionStart, TimeFormatOption)} {GetDateTimeString(data.TimeStamp, data.SessionStart, TimeFormats.s)}");
                                        }

                                    }
                                }
                            }

                        }
                    }
                }

            }
        }

        string[] TruncateStack(string stack)
        {
            string[] lret = (stack ?? "").Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if((CutStackMin > 0 || CutStackMax > 0) && lret != null)
            {
                int missingFrames = lret.Length - CutStackMax - CutStackMin;
                var tmp = lret.Skip(CutStackMin).Take(CutStackMax);
                if( missingFrames > 0 )
                {
                    tmp = tmp.Append($"Skipped {missingFrames} Frames ... {CutStackMin} from start and {CutStackMax} from end");
                }

                lret = tmp.ToArray();
            }

            return lret;
        }

        /// <summary>
        /// Truncate an exception message at a given length
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        string TruncateMessage(string message)
        {
            if (MaxMessage > 0 && message.Length > MaxMessage)
            {
                return $"{message.Substring(0, MaxMessage)} ... -MaxMessage 0 for full text";
            }
            return message;
        }
    }
}
