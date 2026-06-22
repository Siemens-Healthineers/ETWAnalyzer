//// SPDX-FileCopyrightText:  © 2026 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using ETWAnalyzer.EventDump;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.TraceLogging;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer_uTest.TestInfrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ETWAnalyzer_uTest.EventDump
{
    public class DumpTraceLogTests
    {
        static readonly Guid ProviderGuid = new("11111111-1111-1111-1111-111111111111");
        const string ProviderName = "TestProvider";

        readonly ITestOutputHelper myWriter;

        public DumpTraceLogTests(ITestOutputHelper writer)
        {
            myWriter = writer;
        }

        TraceLoggingProvider CreateProvider()
        {
            TraceLoggingProvider provider = new()
            {
                ProviderName = ProviderName,
                ProviderId = ProviderGuid,
            };

            (int Id, string Name)[] descriptors = new[]
            {
                (10, "Alpha"),
                (20, "Zebra"),
                (5,  "Mango"),
            };

            foreach ((int id, string name) in descriptors)
            {
                TraceLoggingEventDescriptor desc = new()
                {
                    EventId = id,
                    Name = name,
                    Provider = provider,
                    FieldNames = new List<string> { "Payload" },
                };
                provider.EventDescriptors[id] = desc;
            }

            return provider;
        }

        List<DumpTraceLog.MatchData> CreateTestData()
        {
            TraceLoggingProvider provider = CreateProvider();

            TestDataFile file = new("Test", "C:\\temp\\Test.json", new DateTime(2000, 1, 1), 500, 1, "TestMachine", null)
            {
                Extract = new ETWExtract(),
            };

            // Counts: Mango(Id 5) x2, Alpha(Id 10) x3, Zebra(Id 20) x1
            int[] eventIds = new[] { 10, 10, 10, 20, 5, 5 };

            ETWProcess process = new() { ProcessID = 1234, ProcessName = "Test.exe" };

            List<DumpTraceLog.MatchData> lret = new();
            foreach (int id in eventIds)
            {
                TraceLoggingEvent ev = new()
                {
                    EventId = id,
                    ThreadId = 4711,
                    TimeStamp = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    TypeInformation = provider.EventDescriptors[id],
                    Fields = new Dictionary<string, string> { { "Payload", $"Value{id}" } },
                };

                lret.Add(new DumpTraceLog.MatchData
                {
                    File = file,
                    Event = ev,
                    Process = process,
                });
            }

            return lret;
        }

        /// <summary>
        /// Create one event per provider descriptor at distinct ETW session times so the -MinMaxTime filter can be exercised.
        /// The events are placed at 5s, 10s and 20s after the session start.
        /// </summary>
        List<DumpTraceLog.MatchData> CreateTimedTestData()
        {
            TraceLoggingProvider provider = CreateProvider();

            DateTimeOffset sessionStart = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            TestDataFile file = new("Test", "C:\\temp\\Test.json", new DateTime(2000, 1, 1), 500, 1, "TestMachine", null)
            {
                Extract = new ETWExtract { SessionStart = sessionStart },
            };

            // Event id -> offset in seconds from the session start
            (int Id, int OffsetSeconds)[] events = new[]
            {
                (5, 5),
                (10, 10),
                (20, 20),
            };

            ETWProcess process = new() { ProcessID = 1234, ProcessName = "Test.exe" };

            List<DumpTraceLog.MatchData> lret = new();
            foreach ((int id, int offsetSeconds) in events)
            {
                TraceLoggingEvent ev = new()
                {
                    EventId = id,
                    ThreadId = 4711,
                    TimeStamp = sessionStart.AddSeconds(offsetSeconds),
                    TypeInformation = provider.EventDescriptors[id],
                    Fields = new Dictionary<string, string> { { "Payload", $"Value{id}" } },
                };

                lret.Add(new DumpTraceLog.MatchData
                {
                    File = file,
                    Event = ev,
                    Process = process,
                });
            }

            return lret;
        }

        /// <summary>
        /// Create one event per provider descriptor on a distinct thread id so the -TID filter can be exercised.
        /// Alpha(Id 10) is logged on thread 100, Zebra(Id 20) on thread 200 and Mango(Id 5) on thread 300.
        /// </summary>
        List<DumpTraceLog.MatchData> CreateThreadTestData()
        {
            TraceLoggingProvider provider = CreateProvider();

            TestDataFile file = new("Test", "C:\\temp\\Test.json", new DateTime(2000, 1, 1), 500, 1, "TestMachine", null)
            {
                Extract = new ETWExtract(),
            };

            // Event id -> thread id which did log the event
            (int Id, uint ThreadId)[] events = new[]
            {
                (10, 100u),
                (20, 200u),
                (5, 300u),
            };

            ETWProcess process = new() { ProcessID = 1234, ProcessName = "Test.exe" };

            List<DumpTraceLog.MatchData> lret = new();
            foreach ((int id, uint threadId) in events)
            {
                TraceLoggingEvent ev = new()
                {
                    EventId = id,
                    ThreadId = threadId,
                    TimeStamp = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    TypeInformation = provider.EventDescriptors[id],
                    Fields = new Dictionary<string, string> { { "Payload", $"Value{id}" } },
                };

                lret.Add(new DumpTraceLog.MatchData
                {
                    File = file,
                    Event = ev,
                    Process = process,
                });
            }

            return lret;
        }

        static int IndexOfContains(IReadOnlyList<string> lines, string substring)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains(substring))
                {
                    return i;
                }
            }
            return -1;
        }

        [Fact]
        public void Provider_Without_EventList_Prints_Event_Summary()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider"),
                ShowTotal = DumpCommand.TotalModes.None,
            };

            dumper.ExecuteInternal();

            testOutput.Flush();
            IReadOnlyList<string> lines = testOutput.GetSingleLines();

            Assert.Contains(lines, x => x.Contains("Alpha") && x.Contains("Id: 10"));
            Assert.Contains(lines, x => x.Contains("Zebra") && x.Contains("Id: 20"));
            Assert.Contains(lines, x => x.Contains("Mango") && x.Contains("Id: 5"));

            // Summary shows the counts and not the individual events (6 events -> 3 summary lines)
            Assert.Contains(lines, x => x.Contains("Alpha") && x.Contains("3"));
            Assert.Contains(lines, x => x.Contains("Mango") && x.Contains("2"));
            Assert.Contains(lines, x => x.Contains("Zebra") && x.Contains("1"));
        }

        [Fact]
        public void Event_Summary_Default_Sorts_By_Count()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider"),
                ShowTotal = DumpCommand.TotalModes.None,
            };

            dumper.ExecuteInternal();

            testOutput.Flush();
            IReadOnlyList<string> lines = testOutput.GetSingleLines();

            // Ascending count: Zebra(1) < Mango(2) < Alpha(3)
            int zebra = IndexOfContains(lines, "Zebra");
            int mango = IndexOfContains(lines, "Mango");
            int alpha = IndexOfContains(lines, "Alpha");

            Assert.True(zebra < mango, "Zebra(count 1) should be before Mango(count 2)");
            Assert.True(mango < alpha, "Mango(count 2) should be before Alpha(count 3)");
        }

        [Fact]
        public void Event_Summary_Sorts_By_Name()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider"),
                ShowTotal = DumpCommand.TotalModes.None,
                SortOrder = DumpCommand.SortOrders.Name,
            };

            dumper.ExecuteInternal();

            testOutput.Flush();
            IReadOnlyList<string> lines = testOutput.GetSingleLines();

            // Ascending name: Alpha < Mango < Zebra
            int alpha = IndexOfContains(lines, "Alpha");
            int mango = IndexOfContains(lines, "Mango");
            int zebra = IndexOfContains(lines, "Zebra");

            Assert.True(alpha < mango, "Alpha should be before Mango");
            Assert.True(mango < zebra, "Mango should be before Zebra");
        }

        [Fact]
        public void Event_Summary_Sorts_By_Id()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider"),
                ShowTotal = DumpCommand.TotalModes.None,
                SortOrder = DumpCommand.SortOrders.Id,
            };

            dumper.ExecuteInternal();

            testOutput.Flush();
            IReadOnlyList<string> lines = testOutput.GetSingleLines();

            // Ascending id: Mango(5) < Alpha(10) < Zebra(20)
            int mango = IndexOfContains(lines, "Mango");
            int alpha = IndexOfContains(lines, "Alpha");
            int zebra = IndexOfContains(lines, "Zebra");

            Assert.True(mango < alpha, "Mango(Id 5) should be before Alpha(Id 10)");
            Assert.True(alpha < zebra, "Alpha(Id 10) should be before Zebra(Id 20)");
        }

        [Fact]
        public void Detailed_Output_Prints_All_Default_Columns()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
            };

            dumper.ExecuteInternal();

            testOutput.Flush();
            IReadOnlyList<string> lines = testOutput.GetSingleLines();

            // A header row is printed with all enabled column names. PID and TID are enabled by default, ProcessName is not.
            Assert.Contains(lines, x => x.Contains("Time") && x.Contains("PID") && x.Contains("TID") && x.Contains("Provider") && x.Contains("EventName") && x.Contains("Id") && x.Contains("Message"));
            Assert.DoesNotContain(lines, x => x.Contains("ProcessName"));

            // Each individual event is printed with pid, tid, provider, event name, id and message column
            Assert.Contains(lines, x => x.Contains("1234") && x.Contains("4711") && x.Contains("TestProvider") && x.Contains("Alpha") && x.Contains("Payload=Value10"));
            Assert.Contains(lines, x => x.Contains("TestProvider") && x.Contains("Zebra") && x.Contains("Payload=Value20"));
        }

        [Fact]
        public void Detailed_Output_Header_Is_Aligned_With_Values()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
            };

            dumper.ExecuteInternal();

            testOutput.Flush();
            IReadOnlyList<string> lines = testOutput.GetSingleLines();

            // The header is the only line containing the literal column header EventName
            string header = lines.First(x => x.Contains("EventName"));
            string dataRow = lines.First(x => x.Contains("Alpha"));

            // Left aligned columns must start at the same offset in the header and in the data rows
            Assert.Equal(header.IndexOf("Provider"), dataRow.IndexOf("TestProvider"));
            Assert.Equal(header.IndexOf("EventName"), dataRow.IndexOf("Alpha"));
        }

        [Fact]
        public void Detailed_Output_Can_Restrict_To_Single_Column()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
                ColumnConfiguration = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { { "EventName", true } },
            };

            dumper.ExecuteInternal();

            testOutput.Flush();
            IReadOnlyList<string> lines = testOutput.GetSingleLines();

            // Only the EventName column is enabled so neither the Provider nor the Message column are printed
            Assert.Contains(lines, x => x.Contains("Alpha"));
            Assert.DoesNotContain(lines, x => x.Contains("TestProvider"));
            Assert.DoesNotContain(lines, x => x.Contains("Payload"));
        }

        [Fact]
        public void Detailed_Output_Can_Exclude_Column()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
                ColumnConfiguration = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { { "Id", false } },
            };

            dumper.ExecuteInternal();

            testOutput.Flush();
            IReadOnlyList<string> lines = testOutput.GetSingleLines();

            // The Id column is excluded but the other default columns are still printed
            Assert.Contains(lines, x => x.Contains("Alpha") && x.Contains("Payload=Value10"));

            // Neither the Id header nor the Id values are printed any more
            string header = lines.First(x => x.Contains("EventName"));
            Assert.DoesNotContain("Id", header);
        }

        [Fact]
        public void Detailed_Output_Can_Add_ProcessName_Column()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
                // ProcessName is not enabled by default. Add it on top of the default columns.
                ColumnConfiguration = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { { "ProcessName", true } },
                MergeColumnConfig = true,
            };

            dumper.ExecuteInternal();

            testOutput.Flush();
            IReadOnlyList<string> lines = testOutput.GetSingleLines();

            // The ProcessName header and the process name value are now printed in addition to the default columns
            string header = lines.First(x => x.Contains("EventName"));
            Assert.Contains("ProcessName", header);
            Assert.Contains(lines, x => x.Contains("Test.exe") && x.Contains("Alpha"));
        }

        [Fact]
        public void Detailed_Output_Can_Exclude_PID_And_TID_Columns()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
                ColumnConfiguration = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { { "PID", false }, { "TID", false } },
            };

            dumper.ExecuteInternal();

            testOutput.Flush();
            IReadOnlyList<string> lines = testOutput.GetSingleLines();

            // The default columns except PID and TID are still printed
            Assert.Contains(lines, x => x.Contains("Alpha") && x.Contains("Payload=Value10"));

            // Neither the PID/TID headers nor their values are printed any more
            string header = lines.First(x => x.Contains("EventName"));
            Assert.DoesNotContain("PID", header);
            Assert.DoesNotContain("TID", header);
            Assert.DoesNotContain(lines, x => x.Contains("4711"));
        }

        [Fact]
        public void Message_Filter_Matches_Substring()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
                MessageFilter = new KeyValuePair<string, Func<string, bool>>("Value10", Matcher.CreateMatcher("*Value10*")),
            };

            List<DumpTraceLog.MatchData> matches = dumper.ExecuteInternal();

            // Plain tokens are matched as substrings so only the three Alpha(Id 10) events with Payload=Value10 remain
            Assert.Equal(3, matches.Count);
            Assert.All(matches, x => Assert.Equal("Alpha", x.Event.TypeInformation.Name));

            testOutput.Flush();
            IReadOnlyList<string> lines = testOutput.GetSingleLines();

            Assert.Contains(lines, x => x.Contains("Alpha") && x.Contains("Payload=Value10"));
            Assert.DoesNotContain(lines, x => x.Contains("Value20"));
            Assert.DoesNotContain(lines, x => x.Contains("Value5"));
        }

        [Fact]
        public void Message_Filter_Supports_Wildcards()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
                MessageFilter = new KeyValuePair<string, Func<string, bool>>("*Value2*", Matcher.CreateMatcher("*Value2*")),
            };

            List<DumpTraceLog.MatchData> matches = dumper.ExecuteInternal();

            // *Value2* matches only the single Zebra(Id 20) event with Payload=Value20
            Assert.Single(matches);
            Assert.Equal("Zebra", matches[0].Event.TypeInformation.Name);
        }

        [Fact]
        public void Message_Filter_Supports_Exclusion()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
                MessageFilter = new KeyValuePair<string, Func<string, bool>>("!Value10", Matcher.CreateMatcher("!*Value10*")),
            };

            List<DumpTraceLog.MatchData> matches = dumper.ExecuteInternal();

            // Everything except the three Alpha(Id 10) events remains (Zebra x1 + Mango x2)
            Assert.Equal(3, matches.Count);
            Assert.DoesNotContain(matches, x => x.Event.TypeInformation.Name == "Alpha");
        }

        [Fact]
        public void Message_Filter_Can_MatchFullString()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
                MessageFilter = new KeyValuePair<string, Func<string, bool>>("Payload=Value5", Matcher.CreateMatcher("Payload=Value5")),
            };

            List<DumpTraceLog.MatchData> matches = dumper.ExecuteInternal();

            // Just match two times the full message
            Assert.Equal(2, matches.Count);
        }

        [Fact]
        public void No_Message_Filter_Keeps_All_Events()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
            };

            List<DumpTraceLog.MatchData> matches = dumper.ExecuteInternal();

            Assert.Equal(6, matches.Count);
        }

        [Fact]
        public void MinMaxTime_Filters_Events_By_Session_Time()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTimedTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
                // Events are at 5s, 10s and 20s. Keep only events between 10s and 20s.
                MinMaxTime = new MinMaxRange<double>(10.0, 20.0),
            };

            List<DumpTraceLog.MatchData> matches = dumper.ExecuteInternal();

            // Only Alpha(10s) and Zebra(20s) are within the time range, Mango(5s) is filtered out.
            Assert.Equal(2, matches.Count);
            Assert.Contains(matches, x => x.Event.TypeInformation.Name == "Alpha");
            Assert.Contains(matches, x => x.Event.TypeInformation.Name == "Zebra");
            Assert.DoesNotContain(matches, x => x.Event.TypeInformation.Name == "Mango");
        }

        [Fact]
        public void MinMaxTime_Min_Only_Keeps_Later_Events()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTimedTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
                // Keep only events at or after 10s.
                MinMaxTime = new MinMaxRange<double>(10.0, null),
            };

            List<DumpTraceLog.MatchData> matches = dumper.ExecuteInternal();

            // Alpha(10s) and Zebra(20s) remain, Mango(5s) is removed.
            Assert.Equal(2, matches.Count);
            Assert.DoesNotContain(matches, x => x.Event.TypeInformation.Name == "Mango");
        }

        [Fact]
        public void MaxCount_Limits_Displayed_Events()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
                MaxCount = 2,
            };

            List<DumpTraceLog.MatchData> matches = dumper.ExecuteInternal();

            // The 6 events are capped to the first 2.
            Assert.Equal(2, matches.Count);
        }

        [Fact]
        public void MaxCount_Default_Keeps_All_Events()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
            };

            List<DumpTraceLog.MatchData> matches = dumper.ExecuteInternal();

            Assert.Equal(6, matches.Count);
        }

        [Fact]
        public void TID_Filter_Matches_Single_Thread()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateThreadTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
                TIDFilter = new KeyValuePair<string, Func<string, bool>>("200", Matcher.CreateMatcher("200")),
            };

            List<DumpTraceLog.MatchData> matches = dumper.ExecuteInternal();

            // Only the single Zebra(Id 20) event which was logged on thread 200 remains.
            Assert.Single(matches);
            Assert.Equal("Zebra", matches[0].Event.TypeInformation.Name);
            Assert.Equal(200u, matches[0].Event.ThreadId);
        }

        [Fact]
        public void TID_Filter_Matches_Multiple_Threads()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateThreadTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
                TIDFilter = new KeyValuePair<string, Func<string, bool>>("100;300", Matcher.CreateMatcher("100;300")),
            };

            List<DumpTraceLog.MatchData> matches = dumper.ExecuteInternal();

            // Threads 100 (Alpha) and 300 (Mango) remain, thread 200 (Zebra) is filtered out.
            Assert.Equal(2, matches.Count);
            Assert.Contains(matches, x => x.Event.ThreadId == 100u);
            Assert.Contains(matches, x => x.Event.ThreadId == 300u);
            Assert.DoesNotContain(matches, x => x.Event.ThreadId == 200u);
        }

        [Fact]
        public void TID_Filter_Supports_Exclusion()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateThreadTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
                TIDFilter = new KeyValuePair<string, Func<string, bool>>("!200", Matcher.CreateMatcher("!200")),
            };

            List<DumpTraceLog.MatchData> matches = dumper.ExecuteInternal();

            // Everything except the event logged on thread 200 (Zebra) remains.
            Assert.Equal(2, matches.Count);
            Assert.DoesNotContain(matches, x => x.Event.ThreadId == 200u);
        }

        [Fact]
        public void No_TID_Filter_Keeps_All_Events()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpTraceLog dumper = new()
            {
                myUTestData = CreateThreadTestData(),
                ProviderFilter = new TraceLoggingProviderFilter("TestProvider:*"),
                ShowTotal = DumpCommand.TotalModes.None,
            };

            List<DumpTraceLog.MatchData> matches = dumper.ExecuteInternal();

            Assert.Equal(3, matches.Count);
        }
    }
}