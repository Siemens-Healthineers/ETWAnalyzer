﻿//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers;
using ETWAnalyzer.Analyzers.Infrastructure;
using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Exceptions;
using ETWAnalyzer.Extractors;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using Microsoft.Diagnostics.Tracing.Parsers.FrameworkEventSource;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public KeyValuePair<string, Func<string, bool>> StackFilter { get; internal set; }

        public bool ShowStack { get; internal set; }

        public int CutStackMin { get; internal set; }
        public int CutStackMax { get; internal set; }
        public bool NoCmdLine { get; internal set; }
        public MinMaxRange<double> MinMaxExTimeS { get; internal set; }
        public int MaxMessage { get; internal set; } = DumpCommand.MaxMessageLength;
        public DumpCommand.SortOrders SortOrder { get; internal set; }

        public class MatchData
        {
            /// <summary>
            /// Exception Message
            /// </summary>
            public string Message;

            /// <summary>
            /// Exception Type
            /// </summary>
            public string Type;

            /// <summary>
            ///  Exception timestamp
            /// </summary>
            public DateTimeOffset TimeStamp;

            /// <summary>
            /// Throwing proces
            /// </summary>
            public ETWProcess Process;

            /// <summary>
            /// Exception stacktrace
            /// </summary>
            public string Stack;

            /// <summary>
            /// Input json file name 
            /// </summary>
            public string SourceFile;

            /// <summary>
            /// Time when test was run
            /// </summary>
            internal DateTime PerformedAt;


            /// <summary>
            /// ETW Session start time
            /// </summary>
            public DateTimeOffset SessionStart;


            /// <summary>
            /// Test case name
            /// </summary>
            public string TestCase { get; internal set; }

            /// <summary>
            /// Time shift offset if -zt (zerotime) feature is used
            /// </summary>
            public double ZeroTimeS { get; internal set; }

            /// <summary>
            /// Baseline version
            /// </summary>
            public string BaseLine { get; internal set; }
        }

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
            OpenCSVWithHeader("CSVOptions", "Time", "Exception Type", "Message", "Process", "Process Name", "Start Time", "Command Line", "StackTrace", "TestCase", "BaseLine", "PerformedAt", "SourceFile");
            foreach(var match in matches)
            {
                WriteCSVLine(CSVOptions, GetDateTimeString(match.TimeStamp, match.SessionStart, TimeFormatOption), match.Type, match.Message, match.Process.GetProcessWithId(UsePrettyProcessName), 
                    match.Process.GetProcessName(UsePrettyProcessName), match.Process.StartTime,
                    match.Process.CmdLine, match.Stack, match.TestCase, match.BaseLine, GetDateTimeString(match.PerformedAt), match.SourceFile);
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
                    MinMaxExTimeS.IsWithin( (arg.Time - file.Extract.SessionStart).TotalSeconds - zeroTimeS); 
            };

            foreach (var ex in file.Extract.Exceptions.Exceptions.Where(IsMatchingException))
            {
                var data = new MatchData
                {
                    Message = ex.Message,
                    Type = ex.Type,
                    Process = ex.Process,
                    TimeStamp = ex.Time.AddSeconds(-1.0d*zeroTimeS),
                    Stack = ex.Stack,
                    SourceFile = file.JsonExtractFileWhenPresent,
                    TestCase = file.TestName,
                    PerformedAt = file.PerformedAt,
                    BaseLine = file.Extract?.MainModuleVersion?.ToString(),
                    SessionStart = file.Extract.SessionStart,
                    ZeroTimeS = zeroTimeS,
                };

                matches.Add(data);
            }
        }


        private void PrintMatches(List<MatchData> matches)
        {
            foreach (var byFile in matches.GroupBy(x => x.SourceFile).OrderBy(x => x.First().PerformedAt))
            {
                PrintFileName(byFile.Key, null, byFile.First().PerformedAt, byFile.First().BaseLine);
                
                if (SortOrder == DumpCommand.SortOrders.Time)
                {
                    PrintByTime(byFile);
                }
                else
                {
                    PrintByFileAndProcess(matches);
                }
            }
        }
        
        private void PrintByTime(IGrouping<string, MatchData> byFile)
        {
            List<MatchData> matches = new List<MatchData>();
            string timeHeader = "Time";
            int timeWidth = TimeFormatOption switch
            {
                TimeFormats.s => 6,
                TimeFormats.Local => 24,
                TimeFormats.LocalTime => 12,
                TimeFormats.UTC => 24,
                TimeFormats.UTCTime => 12,
                TimeFormats.Here => 24,
                TimeFormats.HereTime => 12,
                _ => 100,
            };
            const int expTypeWidth = 60;
            string excheader = "Exception Type";
            const int processWidth = 60;
            string processheader = "Process";
            const int expmsgWidth = 90;
            string expmsgheader = "Exception Message";
            ColorConsole.WriteEmbeddedColorLine($"{timeHeader.WithWidth(timeWidth)}[magenta]{processheader.WithWidth(processWidth)}[/magenta] [green]{excheader.WithWidth(expTypeWidth)}[/green] {expmsgheader.WithWidth(expmsgWidth)}");

            string previousExceptionType = null;
            string previousProcess = null;
            string previousMessage = null;

            foreach (var item in byFile.OrderBy(match => match.TimeStamp))
            {
                string timeStr = GetDateTimeString(item.TimeStamp, item.SessionStart, TimeFormatOption).WithWidth(timeWidth);
                string currentProcess = item.Process.GetProcessWithId(UsePrettyProcessName);
                string currentMessage = item.Message;
                string currentExceptiontype = item.Type;
                string tobePrintedMsg = currentMessage;
                string tobePrinted = currentProcess;
                string tobePrintedType = currentExceptiontype;
                string tobePrintedProcess = currentProcess;

                if (currentProcess == previousProcess)
                {
                    tobePrinted = "...";
                } 
                string replacedProcess = replaceToCurrentProcess(item.Process.GetProcessWithId(UsePrettyProcessName));
                previousProcess = currentProcess;

                if (currentMessage == previousMessage)
                {
                    tobePrintedMsg = "...";
                }
                string replacedMsg = replaceToCurrentMsg(item.Message);
                previousMessage = currentMessage;

                if (currentExceptiontype == previousExceptionType)
                {
                    tobePrintedType = "...";
                }
                string replacedType = replaceToCurrentType(item.Type);
                previousExceptionType = currentExceptiontype;

                if ((replacedType != "...") || (tobePrinted != "..."))
                {
                    replacedProcess = tobePrintedProcess;
                }
                else
                {
                    replacedProcess = "...";
                }

                ColorConsole.WriteEmbeddedColorLine($"{timeStr} [magenta]{replacedProcess,processWidth}[/magenta] [green]{replacedType,expTypeWidth}[/green] {replacedMsg,expmsgWidth}");

            }
        }
        string myPreviousProcess = null;
        string replaceToCurrentProcess(string currentProcess)
        {
            if (currentProcess == myPreviousProcess)
            {
                return "...";
            }
            else
            {
                myPreviousProcess = currentProcess;
                return currentProcess;
            }
        }
        string myPreviousMsg = null;
        string replaceToCurrentMsg(string currentMsg)
        {
            if (currentMsg == myPreviousMsg)
            {
                return "...";
            }
            else
            {
                myPreviousMsg = currentMsg;
                return currentMsg;
            }
        }
        string myPreviousType = null;
        string replaceToCurrentType(string currentType)
        {
            if (currentType == myPreviousType)
            {
                return "...";
            }
            else
            {
                myPreviousType = currentType;
                return currentType;
            }
        }

        private void PrintByFileAndProcess(List<MatchData> matches)
        {
            foreach (var processExceptions in matches.GroupBy(x => x.Process))
            {
                ColorConsole.WriteEmbeddedColorLine($"[magenta]{processExceptions.Key.GetProcessWithId(UsePrettyProcessName)} {processExceptions.Key.StartStopTags}[/magenta] {(NoCmdLine ? String.Empty : processExceptions.Key.CommandLineNoExe)}", ConsoleColor.DarkCyan);
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
