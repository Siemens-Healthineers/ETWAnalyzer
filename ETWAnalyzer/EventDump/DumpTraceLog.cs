//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Common;
using ETWAnalyzer.Extract.TraceLogging;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ETWAnalyzer.EventDump.DumpCPUMethod;

namespace ETWAnalyzer.EventDump
{
    class DumpTraceLog : DumpFileDirBase<DumpTraceLog.MatchData>
    {
        /// <summary>
        /// Unit testing only. ReadFileData will return this list instead of real data
        /// </summary>
        internal List<MatchData> myUTestData = null;

        public DumpCommand.TotalModes? ShowTotal { get; internal set; }
        public SkipTakeRange TopN { get; internal set; } = new();
        public KeyValuePair<string, Func<string, bool>> ProviderFilter { get; internal set; }

        internal class MatchData
        {
            public ITraceLoggingEvent Event { get; internal set; }
            public ETWProcess Process { get; internal set; }
            public TestDataFile File { get; internal set; }
        }

        public override List<MatchData> ExecuteInternal()
        {

            List<MatchData> lret = ReadFileData();
            if (IsCSVEnabled)
            {
                WriteCSVData(lret);
            }
            else
            {
                PrintMatches(lret);
            }

            return lret;
        }

        private void PrintMatches(List<MatchData> matches)
        {
            Totals fileTotal = new();
            Totals allFileTotal = new();
            string fileName = null;

            foreach (var ev in matches.OrderBy(x => x.File.PerformedAt))
            {
                fileTotal.Add(ev.Event);
                allFileTotal.Add(ev.Event);

                if (ev.File.FileName != fileName)
                {
                    if (ShowTotal != DumpCommand.TotalModes.None && fileName != null)
                    {
                        fileTotal.PrintTotals(ConsoleColor.Yellow, ShowTotal, TopN, UsePrettyProcessName);
                    }

                    PrintFileName(ev.File.FileName, null, ev.File.PerformedAt, ev.File.Extract.MainModuleVersion?.ToString());
                    fileName = ev.File.FileName;    
                }

                if( ProviderFilter.Key != null)
                {
                    KeyValuePair<string,string>[] fieldRow = ev.Event.TypeInformation.FieldNames.Select(field => new KeyValuePair<string,string>(field,ev.Event.TryGetField(field))).ToArray();
                    KeyValuePair<string, IReadOnlyList<string>>[] fieldList = ev.Event.TypeInformation.ListNames.Select(list => new KeyValuePair<string, IReadOnlyList<string>>(list, ev.Event.TryGetList(list))).ToArray();

                    foreach(var field in fieldRow)
                    {
                        ColorConsole.WriteEmbeddedColorLine($"{GetDateTimeString(ev.Event.TimeStamp, ev.File.Extract.SessionStart, TimeFormatOption)} [yellow]{field.Key}[/yellow] {field.Value} ", null, true);
                    }
                    ColorConsole.WriteLine("");
                }

            }

            if (ShowTotal != DumpCommand.TotalModes.None && fileName != null)
            {
                fileTotal.PrintTotals(ConsoleColor.Yellow, ShowTotal, TopN, UsePrettyProcessName);
            }

        }

        class Totals
        {
           public int EventCount { get; private set; }
           public HashSet<ETWProcess> Processes { get; private set; } = new();

            Dictionary<string, int> CountByProvider { get; } = new Dictionary<string, int>();
            Dictionary<ETWProcess, Counter<string>> CountByProcess { get; } = new Dictionary<ETWProcess, Counter<string>>();

            public void Add(ITraceLoggingEvent ev)
            {
                EventCount++;
                Processes.Add(ev.Process);
                if( !CountByProvider.TryGetValue(ev.TypeInformation.ProviderName, out int count) )
                {
                    CountByProvider.Add(ev.TypeInformation.ProviderName,0);
                }
                CountByProvider[ev.TypeInformation.ProviderName] = count + 1;

                if( !CountByProcess.TryGetValue(ev.Process, out var counter))
                {
                    counter = new();
                    CountByProcess.Add(ev.Process, counter);
                }

                counter.Increment(ev.TypeInformation.ProviderName);
            }

            internal void PrintTotals(ConsoleColor baseColor, DumpCommand.TotalModes? showTotal, SkipTakeRange topN, bool usePrettyProcessName)
            {
                foreach(var proc in CountByProcess.OrderBy(x=>x.Value.TotalCount))
                {
                    ColorConsole.WriteEmbeddedColorLine($"[magenta]{proc.Key.GetProcessWithId(usePrettyProcessName)}[/magenta]");
                    foreach (var kvp in proc.Value.Counts.OrderBy(x => x.Value))
                    {
                        ColorConsole.WriteEmbeddedColorLine($"\t[yellow]{kvp.Key} {kvp.Value}[/yellow]");
                    }
                }

                foreach (var prov in CountByProvider.OrderBy(x => x.Value))
                {
                    ColorConsole.WriteEmbeddedColorLine($"[yellow]{prov.Key}[/yellow] {prov.Value}", baseColor);
                }

                ColorConsole.WriteEmbeddedColorLine($"{CountByProvider.Count} provider/s did log {EventCount} events from {Processes.Count} processes", baseColor);
            }
        }

        private void WriteCSVData(List<MatchData> lret)
        {
            int maxFieldCount = lret.Max(x=>x.Event.TypeInformation.FieldNames.Count);

            var dynamicFields =  Enumerable.Range(1, maxFieldCount).Select(x => $"Field{x}").ToList();
            List<string> fixedFields = new List<string>
            {
                Col_CSVOptions,
                Col_FileName,
                Col_Date,
                Col_TestCase,
                Col_TestTimeinms,
                Col_Baseline,
                Col_Process,
                Col_ProcessName,
                Col_StartTime,
                Col_CommandLine,
                "Provider Name",
                "Provider Guid",
                "EventName",
                "Time",
                "Thread Id",
                "Stack",
            };

            fixedFields.AddRange(dynamicFields);

            OpenCSVWithHeader(fixedFields.ToArray());

            foreach (var ev in lret)
            {
                List<object> args = new List<object>
                {
                   CSVOptions, Path.GetFileNameWithoutExtension(ev.File.FileName), ev.File.PerformedAt, ev.File.TestName, ev.File.DurationInMs, ev.File.Extract.MainModuleVersion?.ToString(),
                   ev.Process.GetProcessWithId(UsePrettyProcessName), ev.Process.GetProcessName(UsePrettyProcessName),
                   GetTimeString(ev.Process.StartTime, ev.File.Extract.SessionStart, TimeFormatOption),
                   ev.Process.CommandLineNoExe,
                   ev.Event.TypeInformation.ProviderName,
                   ev.Event.TypeInformation.ProviderGuid.ToString(),
                   ev.Event.TypeInformation.Name,
                   GetTimeString(ev.Event.TimeStamp, ev.File.Extract.SessionStart, TimeFormatOption),
                   ev.Event.ThreadId,
                   GetStackOnce(ev.Event.StackIdx, ev.Event.StackTrace),
                };
                 
                foreach(var field in ev.Event.TypeInformation.FieldNames)
                {
                    args.Add(ev.Event.TryGetField(field));
                }

                foreach(var fieldList in ev.Event.TypeInformation.ListNames) 
                { 
                    IReadOnlyList<string> list = ev.Event.TryGetList(fieldList);
                    string concat = String.Join("; ", list);
                    args.Add($"[{concat}]");
                }

                WriteCSVLine(args.ToArray());

            }
        }

        Dictionary<StackIdx, string> myPrintedStacks = new();

        string GetStackOnce(StackIdx idx, string stackTrace)
        {
            string lret = "";
            if (!myPrintedStacks.TryGetValue(idx, out string stack) )
            {
                lret = $"StackId: {idx} " + stackTrace;
            }
            else
            {
                lret = $"StackId: {idx}";
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
                    if (file?.Extract?.TraceLogging == null || file?.Extract?.TraceLogging.EventsByProvider.Count == 0)
                    {
                        ColorConsole.WriteError($"Warning: File {GetPrintFileName(file.FileName)} does not contain TraceLog data.");
                        continue;
                    }

                    bool bFirst = true;
                    bool bCancel = false;

                    foreach (KeyValuePair<string, ITraceLoggingProvider> eventByProvider in file.Extract.TraceLogging.EventsByProvider)
                    {
                        if( bCancel )
                        {
                            break;
                        }

                        foreach (ITraceLoggingEvent eventData in eventByProvider.Value.Events.OrderBy(x=>x.TimeStamp))
                        {
                            if(bFirst && ProviderFilter.Key != null && ( !ProviderFilter.Value(eventData.TypeInformation.ProviderName) && !ProviderFilter.Value(eventData.TypeInformation.ProviderGuid.ToString())) )
                            {
                                bCancel = true;
                                continue;
                            }

                            bFirst = false;

                            if ( !ProcessNameFilter(eventData.Process.GetProcessName(UsePrettyProcessName)) )
                            {
                                continue;
                            }

                            MatchData match = new()
                            {
                                File = file,
                                Event = eventData,
                                Process = eventData.Process,
                            };
                            lret.Add(match);
                        }
                    }
                }
            }

            return lret;
        }
    }

}
