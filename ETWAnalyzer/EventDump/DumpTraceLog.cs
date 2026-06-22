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
        public TraceLoggingProviderFilter ProviderFilter { get; internal set; }
        public DumpCommand.SortOrders SortOrder { get; internal set; }

        /// <summary>
        /// Filter for the event message (the payload field name=value pairs). Matches events whose message contains the substring.
        /// </summary>
        public KeyValuePair<string, Func<string, bool>> MessageFilter { get; internal set; } = new(null, _ => true);

        /// <summary>
        /// Remove all events which are not within this time filter. Time is specified in ETW session time in seconds.
        /// </summary>
        public MinMaxRange<double> MinMaxTime { get; internal set; } = new();

        /// <summary>
        /// Display at most this number of trace messages.
        /// </summary>
        public int MaxCount { get; internal set; } = int.MaxValue;

        /// <summary>
        /// Used by context sensitive help to print the allowed values for the -SortBy clause of the provider event summary.
        /// </summary>
        static internal readonly DumpCommand.SortOrders[] ValidSortOrders = new[]
        {
            DumpCommand.SortOrders.Count,
            DumpCommand.SortOrders.Name,
            DumpCommand.SortOrders.Id,
            DumpCommand.SortOrders.Default,
        };

        const string Col_Provider = "Provider";
        const string Col_EventName = "EventName";
        const string Col_Id = "Id";
        const string Col_Message = "Message";

        /// <summary>
        /// Valid column names which can be configured for the detailed (individual event) output via -Column.
        /// </summary>
        public static string[] ColumnNames =
        {
            Col_Time, Col_Provider, Col_EventName, Col_Id, Col_Message,
        };

        bool GetColumnEnable(string columnName)
        {
            return columnName switch
            {
                Col_Time => GetOverrideFlag(Col_Time, true),
                Col_Provider => GetOverrideFlag(Col_Provider, true),
                Col_EventName => GetOverrideFlag(Col_EventName, true),
                Col_Id => GetOverrideFlag(Col_Id, true),
                Col_Message => GetOverrideFlag(Col_Message, true),
                _ => throw new NotSupportedException($"Column {columnName} is not configurable."),
            };
        }

        internal class MatchData
        {
            public ITraceLoggingEvent Event { get; internal set; }
            public ETWProcess Process { get; internal set; }
            public TestDataFile File { get; internal set; }
        }

        public override List<MatchData> ExecuteInternal()
        {
            List<MatchData> lret = ReadFileData();

            if (!MinMaxTime.IsDefault)
            {
                lret = lret.Where(x => MinMaxTime.IsWithin((x.Event.TimeStamp - x.File.Extract.SessionStart).TotalSeconds)).ToList();
            }

            if (MessageFilter.Key != null)
            {
                lret = lret.Where(x => MessageFilter.Value(GetEventMessageText(x.Event).TrimEnd())).ToList();
            }

            if (lret.Count > MaxCount)
            {
                lret = lret.Take(MaxCount).ToList();
            }

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
            foreach (var fileGroup in matches.GroupBy(x => x.File).OrderBy(x => x.Key.PerformedAt))
            {
                TestDataFile file = fileGroup.Key;
                PrintFileName(file.FileName, null, file.PerformedAt, file.Extract.MainModuleVersion?.ToString());

                if (ProviderFilter != null)
                {
                    foreach (var providerGroup in fileGroup.GroupBy(x => x.Event.TypeInformation.ProviderName))
                    {
                        ITraceLoggingEventDescriptor typeInfo = providerGroup.First().Event.TypeInformation;

                        if (ProviderFilter.HasEventFilterForProvider(typeInfo.ProviderName, typeInfo.ProviderGuid))
                        {
                            PrintProviderEvents(providerGroup);
                        }
                        else
                        {
                            PrintProviderEventSummary(providerGroup);
                        }
                    }
                }

                if (ShowTotal != DumpCommand.TotalModes.None)
                {
                    Totals fileTotal = new();
                    foreach (var ev in fileGroup)
                    {
                        fileTotal.Add(ev.Event);
                    }
                    fileTotal.PrintTotals(ConsoleColor.Yellow, ShowTotal, TopN, UsePrettyProcessName);
                }
            }
        }

        /// <summary>
        /// Print all individual events of a provider which was selected with an explicit event name/id list.
        /// A header row is printed for the enabled columns and the values are aligned below their headers.
        /// The printed columns can be configured via -Column (Time, Provider, EventName, Id, Message).
        /// </summary>
        /// <param name="providerEvents">Events of a single provider.</param>
        private void PrintProviderEvents(IEnumerable<MatchData> providerEvents)
        {
            List<MatchData> events = providerEvents as List<MatchData> ?? providerEvents.ToList();
            if (events.Count == 0)
            {
                return;
            }

            bool showTime = GetColumnEnable(Col_Time);
            bool showProvider = GetColumnEnable(Col_Provider);
            bool showEventName = GetColumnEnable(Col_EventName);
            bool showId = GetColumnEnable(Col_Id);
            bool showMessage = GetColumnEnable(Col_Message);

            // Pre-render the time strings once and determine the column widths so the header aligns with the values below it.
            string[] times = new string[events.Count];
            int timeWidth = Col_Time.Length;
            int providerWidth = Col_Provider.Length;
            int eventNameWidth = Col_EventName.Length;
            int idWidth = Col_Id.Length;

            for (int i = 0; i < events.Count; i++)
            {
                ITraceLoggingEvent ev = events[i].Event;
                times[i] = GetDateTimeString(ev.TimeStamp, events[i].File.Extract.SessionStart, TimeFormatOption);
                timeWidth = Math.Max(timeWidth, times[i].Length);
                providerWidth = Math.Max(providerWidth, ev.TypeInformation.ProviderName.Length);
                eventNameWidth = Math.Max(eventNameWidth, ev.TypeInformation.Name.Length);
                idWidth = Math.Max(idWidth, ev.EventId.ToString().Length);
            }

            StringBuilder header = new();
            if (showTime)
            {
                header.Append($"{Col_Time.WithWidth(-timeWidth)} ");
            }
            if (showProvider)
            {
                header.Append($"[magenta]{Col_Provider.WithWidth(-providerWidth)}[/magenta] ");
            }
            if (showEventName)
            {
                header.Append($"[yellow]{Col_EventName.WithWidth(-eventNameWidth)}[/yellow] ");
            }
            if (showId)
            {
                header.Append($"{Col_Id.WithWidth(-idWidth)} ");
            }
            if (showMessage)
            {
                header.Append(Col_Message);
            }
            ColorConsole.WriteEmbeddedColorLine(header.ToString().TrimEnd());

            for (int i = 0; i < events.Count; i++)
            {
                ITraceLoggingEvent ev = events[i].Event;
                StringBuilder line = new();

                if (showTime)
                {
                    line.Append($"{times[i].WithWidth(-timeWidth)} ");
                }
                if (showProvider)
                {
                    line.Append($"[magenta]{ev.TypeInformation.ProviderName.WithWidth(-providerWidth)}[/magenta] ");
                }
                if (showEventName)
                {
                    line.Append($"[yellow]{ev.TypeInformation.Name.WithWidth(-eventNameWidth)}[/yellow] ");
                }
                if (showId)
                {
                    line.Append($"{ev.EventId.ToString().WithWidth(-idWidth)} ");
                }
                if (showMessage)
                {
                    line.Append(GetEventMessage(ev));
                }

                ColorConsole.WriteEmbeddedColorLine(line.ToString().TrimEnd());
            }
        }

        /// <summary>
        /// Build the message column of an event from its payload fields as a space separated list of name value pairs.
        /// </summary>
        /// <param name="ev">Event whose payload fields are concatenated.</param>
        /// <returns>Message string with embedded color markup for the field names.</returns>
        private static string GetEventMessage(ITraceLoggingEvent ev)
        {
            StringBuilder message = new();
            foreach (string field in ev.TypeInformation.FieldNames)
            {
                message.Append($"[green]{field}[/green]={ev.TryGetField(field)} ");
            }

            return message.ToString();
        }

        /// <summary>
        /// Build the plain text message of an event without color markup. Used by the -Message filter to match substring values.
        /// </summary>
        /// <param name="ev">Event whose payload fields are concatenated.</param>
        /// <returns>Message string without color markup.</returns>
        private static string GetEventMessageText(ITraceLoggingEvent ev)
        {
            StringBuilder message = new();
            foreach (string field in ev.TypeInformation.FieldNames)
            {
                message.Append($"{field}={ev.TryGetField(field)} ");
            }

            return message.ToString();
        }

        /// <summary>
        /// Print a summary of all events of a provider which was selected without an explicit event name/id list.
        /// Each distinct event is printed with its count, name and id. The sort order is controlled by <see cref="SortOrder"/>.
        /// </summary>
        /// <param name="providerEvents">Events of a single provider.</param>
        private void PrintProviderEventSummary(IEnumerable<MatchData> providerEvents)
        {
            ITraceLoggingEventDescriptor typeInfo = providerEvents.First().Event.TypeInformation;
            ColorConsole.WriteEmbeddedColorLine($"[yellow]{typeInfo.ProviderName}[/yellow]");

            IEnumerable<EventSummary> summary = providerEvents
                .GroupBy(x => new { x.Event.TypeInformation.Name, x.Event.EventId })
                .Select(g => new EventSummary(g.Key.Name, g.Key.EventId, g.Count()));

            foreach (EventSummary entry in SortEventSummary(summary))
            {
                ColorConsole.WriteEmbeddedColorLine($"\t[green]{entry.Count.WithDigitGrouping(),10}[/green] [yellow]{entry.Name}[/yellow] (Id: {entry.Id})");
            }
        }

        /// <summary>
        /// Sort the provider event summary by the configured <see cref="SortOrder"/>. Default is by ascending event count.
        /// </summary>
        /// <param name="summary">Unsorted event summary.</param>
        /// <returns>Sorted event summary.</returns>
        private IEnumerable<EventSummary> SortEventSummary(IEnumerable<EventSummary> summary)
        {
            return SortOrder switch
            {
                DumpCommand.SortOrders.Name => summary.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase),
                DumpCommand.SortOrders.Id => summary.OrderBy(x => x.Id),
                _ => summary.OrderBy(x => x.Count),
            };
        }

        /// <summary>
        /// One line of the provider event summary which describes a distinct event by its name, id and occurrence count.
        /// </summary>
        class EventSummary
        {
            public EventSummary(string name, int id, int count)
            {
                Name = name;
                Id = id;
                Count = count;
            }

            public string Name { get; }
            public int Id { get; }
            public int Count { get; }
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

        readonly Dictionary<StackIdx, string> myPrintedStacks = new();

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

                    foreach (KeyValuePair<string, ITraceLoggingProvider> eventByProvider in file.Extract.TraceLogging.EventsByProvider)
                    {
                        ITraceLoggingProvider provider = eventByProvider.Value;

                        if (ProviderFilter != null && !ProviderFilter.IsMatchingProvider(provider.ProviderName, provider.ProviderId))
                        {
                            continue;
                        }

                        foreach (ITraceLoggingEvent eventData in provider.Events.OrderBy(x=>x.TimeStamp))
                        {
                            if (ProviderFilter != null && !ProviderFilter.IsMatchingEvent(eventData.TypeInformation.ProviderName, eventData.TypeInformation.ProviderGuid, eventData.TypeInformation.Name, eventData.EventId))
                            {
                                continue;
                            }

                            if ( !IsMatchingProcessAndCmdLine(file, eventData.Process.ToProcessKey()) )
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
