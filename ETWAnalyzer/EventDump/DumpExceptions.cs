//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Exceptions;
using ETWAnalyzer.Extract.Modules;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public bool ShowDetails { get; internal set; }

        public MinMaxRange<double> MinMaxExTimeS { get; internal set; }
        public int MaxMessage { get; internal set; } = DumpCommand.MaxMessageLength;


        /// <summary>
        /// Currently we only support Time as different sort order. 
        /// </summary>
        public DumpCommand.SortOrders SortOrder { get; internal set; }

        /// <summary>
        /// By default time is omitted. When you add -ShowTime exception time is printed.
        /// </summary>
        public bool ShowTime { get; internal set; }


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

            /// <summary>
            /// Show module file name and version. In cpu total mode also exe version.
            /// </summary>
            public ModuleDefinition Module { get; internal set; }

            public uint SessionId { get; set; }
            public uint ThreadId { get; internal set; }

            public MatchData Clone()
            {
                return new MatchData
                {
                    Message = Message,
                    Type = Type,
                    TimeStamp = TimeStamp,
                    Process = Process,
                    Stack = Stack,
                    Module = Module,
                    SourceFile = SourceFile,
                    ZeroTimeS = ZeroTimeS,
                    BaseLine = BaseLine,
                    SessionId = SessionId,
                    ThreadId = ThreadId,
                    PerformedAt = PerformedAt,
                    SessionStart = SessionStart,
                    TestCase = TestCase,
                };
            }
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
            OpenCSVWithHeader(Col_CSVOptions, Col_Time, "Exception Type", "Message", Col_Process, Col_ProcessName, "Thread", Col_Session, Col_StartTime, 
                Col_CommandLine, "StackTrace", Col_TestCase, Col_Baseline, "PerformedAt", Col_SourceJsonFile, 
                Col_FileVersion, Col_VersionString, Col_ProductVersion, Col_ProductName, Col_Description, Col_Directory);
            foreach(var match in matches)
            {
                string fileVersion = match.Module?.Fileversion?.ToString()?.Trim() ?? "";
                string versionString = match.Module?.FileVersionStr?.Trim() ?? "";
                string productVersion = match.Module?.ProductVersionStr?.Trim() ?? "";
                string productName = match.Module?.ProductName?.Trim() ?? "";
                string description = match.Module?.Description?.Trim() ?? "";
                string directory = match.Module?.ModulePath ?? "";
                WriteCSVLine(CSVOptions, GetDateTimeString(match.TimeStamp, match.SessionStart, TimeFormatOption), match.Type, match.Message, match.Process.GetProcessWithId(UsePrettyProcessName), 
                    match.Process.GetProcessName(UsePrettyProcessName), match.ThreadId, match.Process.SessionId, match.Process.StartTime,
                    match.Process.CmdLine, match.Stack, match.TestCase, match.BaseLine, match.PerformedAt, match.SourceFile,
                    fileVersion, versionString, productVersion, productName, description, directory);
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
                ModuleDefinition exceptionModule = ShowModuleInfo ? file.Extract.Modules.FindModule(ex.Process.ProcessName, ex.Process) : null;

                if (!IsMatchingModule(exceptionModule))
                {
                    continue;
                }
                var data = new MatchData
                {
                    Message = ex.Message,
                    Type = ex.Type,
                    Process = ex.Process,
                    SessionId = (uint) ex.Process.SessionId,
                    TimeStamp = ex.Time.AddSeconds(-1.0d*zeroTimeS),
                    ThreadId = ex.ThreadId,
                    Stack = ex.Stack,
                    SourceFile = file.JsonExtractFileWhenPresent,
                    TestCase = file.TestName,
                    PerformedAt = file.PerformedAt,
                    BaseLine = file.Extract?.MainModuleVersion?.ToString(),
                    SessionStart = file.Extract.SessionStart,
                    ZeroTimeS = zeroTimeS,
                    Module = exceptionModule
                };

                matches.Add(data);
            }
        }

        /// <summary>
        /// Used by context sensitive help
        /// </summary>
        static internal readonly DumpCommand.SortOrders[] ValidSortOrders = new[]
        {
            DumpCommand.SortOrders.Time,
            DumpCommand.SortOrders.Default,
        };

        internal void PrintMatches(List<MatchData> matches)
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
            const int expTypeWidth = 40;
            string excheader = "Exception Type";
            const int processWidth = 36;
            string processheader = "Process";
            const int sessionWidth = 7;
            string threadIdHeader = "Thread";
            const int threadIdWidth = 7;
            string sessionHeader = ShowDetails ?$"{"Session",sessionWidth}" : "";
            const int expmsgWidth = 90;
            string expmsgheader = "Exception Message";
            ColorConsole.WriteEmbeddedColorLine($"{timeHeader.WithWidth(timeWidth)} [magenta]{processheader.WithWidth(processWidth)} {threadIdHeader.WithWidth(threadIdWidth)}[/magenta] [yellow]{sessionHeader}[/yellow] [green]{excheader.WithWidth(expTypeWidth)}[/green] {expmsgheader.WithWidth(expmsgWidth)}");

            string previousExceptionType = null;
            string previousProcess = null;
            string previousMessage = null;
            string previousThreadId = null;

            const string SameString = "...";

            foreach (var item in byFile.OrderBy(match => match.TimeStamp))
            {
                string timeStr = GetDateTimeString(item.TimeStamp, item.SessionStart, TimeFormatOption).WithWidth(timeWidth);
                string currentProcess = item.Process.GetProcessWithId(UsePrettyProcessName);
                string currentThreadId = item.ThreadId.ToString();
                string currentSession = ShowDetails ? $"{item.Process.SessionId.ToString(),sessionWidth}" : "";
                string currentMessage = item.Message;
                string currentExceptiontype = item.Type;

                string tobePrintedMsg = currentMessage == previousMessage ? SameString : currentMessage;
                string tobePrintedType = currentExceptiontype == previousExceptionType ? SameString : item.Type;
                string tobePrintedProcess = currentProcess == previousProcess ? SameString : currentProcess;
                string tobePrintedThreadId = currentThreadId == previousThreadId ? SameString : currentThreadId;

                previousMessage = currentMessage;
                previousExceptionType = currentExceptiontype;
                previousProcess = currentProcess;

                ColorConsole.WriteEmbeddedColorLine($"{timeStr} [magenta]{tobePrintedProcess,processWidth} {tobePrintedThreadId,threadIdWidth}[/magenta] [yellow]{currentSession}[/yellow] [green]{tobePrintedType,expTypeWidth}[/green] {tobePrintedMsg,expmsgWidth}");
            }
        }
        
         private void PrintByFileAndProcess(List<MatchData> matches)
        {
            foreach (var processExceptions in matches.GroupBy(x => x.Process))
            {
                MatchData matchData = processExceptions.First();
                ModuleDefinition processModule = matchData.Module;
                string moduleInfo = processModule != null ? GetModuleString(processModule, true) : "";
                string sessionIdStr = ShowDetails ? $"{processExceptions.Key.SessionId, 2}" : "  ";


                ColorConsole.WriteEmbeddedColorLine($"[magenta]{processExceptions.Key.GetProcessWithId(UsePrettyProcessName)} {GetProcessTags(processExceptions.Key, matchData.SessionStart.AddSeconds(matchData.ZeroTimeS))}[/magenta] [yellow]{sessionIdStr}[/yellow] {(NoCmdLine ? String.Empty : processExceptions.Key.CommandLineNoExe)}", ConsoleColor.DarkCyan, true);
                ColorConsole.WriteEmbeddedColorLine($"[red] {moduleInfo}[/red]");

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

                            if (ShowTime)
                            {
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
