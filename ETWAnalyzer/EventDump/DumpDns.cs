//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Network;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ETWAnalyzer.Commands.DumpCommand;

namespace ETWAnalyzer.EventDump
{
    class DumpDns : DumpFileDirBase<DumpDns.MatchData>
    {
        public bool NoCmdLine { get; set; }
        public SkipTakeRange TopN { get; set; }

        public SkipTakeRange TopNDetails { get; set; } = new();

        public bool ShowReturnCode { get; set; }
        public bool ShowAdapter { get; set; }
        public DumpCommand.SortOrders SortOrder { get; internal set; }

        /// <summary>
        /// Filter for total query time which is the sum of all DNS query times for one host.
        /// </summary>
        public MinMaxRange<double> MinMaxTotalTimeMs { get; set; } = new MinMaxRange<double>();

        /// <summary>
        /// Filter for every DNS query duration before they are aggregated
        /// </summary>
        public MinMaxRange<double> MinMaxTimeMs { get; set;  } = new MinMaxRange<double>();

        /// <summary>
        /// -Details flag to show query time for every DNS Request
        /// </summary>
        public bool ShowDetails { get; set; }

        public KeyValuePair<string, Func<string, bool>> DnsQueryFilter { get; internal set; } = new KeyValuePair<string, Func<string, bool>>(null, x => true);
        public bool ShowProcess { get; internal set; }

        /// <summary>
        /// Unit testing only. ReadFileData will return this list instead of real data
        /// </summary>
        internal List<MatchData> myUTestData = null;

        public override List<MatchData> ExecuteInternal()
        {
            List<MatchData> lret = ReadFileData();
            if (IsCSVEnabled)
            {
                OpenCSVWithHeader(Col_CSVOptions, "Directory", Col_FileName, Col_Date, Col_TestCase, Col_TestTimeinms, Col_Baseline, Col_Process, Col_ProcessName,
                                  "DNS Query", "Query StatusCode", "TimedOut", Col_StartTime, "Duration in s", "Queried Network Adapters", 
                                  "Server List", "DNS Result", Col_CommandLine);

                foreach (var dnsEvent in lret)
                {
                    WriteCSVLine(CSVOptions, Path.GetDirectoryName(dnsEvent.File.FileName),
                        Path.GetFileNameWithoutExtension(dnsEvent.File.FileName), dnsEvent.File.PerformedAt, dnsEvent.File.TestName, dnsEvent.File.DurationInMs, dnsEvent.Baseline,
                        dnsEvent.Process.ProcessWithID, dnsEvent.Process.ProcessNamePretty,
                        dnsEvent.Dns.Query, dnsEvent.Dns.QueryStatus, dnsEvent.Dns.TimedOut, GetDateTimeString(dnsEvent.Dns.Start, dnsEvent.SessionStart, TimeFormatOption,false) , dnsEvent.Dns.Duration.TotalSeconds, 
                        dnsEvent.Dns.Adapters, dnsEvent.Dns.ServerList, dnsEvent.Dns.Result,
                        NoCmdLine ? "" : dnsEvent.Process.CommandLineNoExe);
                }
            }
            else
            {
                PrintMatches(lret);
            }

            return lret;
        }

        private void PrintMatches(List<MatchData> lret)
        {
            List<IGrouping<string, MatchData>> byFileGroups = lret.GroupBy(x => x.File.FileName).OrderBy(x=>x.First().File.PerformedAt).ToList();

            decimal totalTimeS = 0;
            int totalQueries = 0;

            foreach (var byFile in byFileGroups)
            {
                MatchData firstMatch = byFile.First();

                PrintFileName(firstMatch.File.FileName, null, firstMatch.File.PerformedAt, firstMatch.Baseline);

                MatchData[] sorted = byFile.GroupBy(x => x.Dns.Query).Select(x => new MatchData
                {
                    SessionStart = firstMatch.SessionStart,

                    GroupQuery = x.Key,
                    GroupQueryCount = x.Count(),
                    GroupQueryTimeS = (decimal) CalulcateNonOverlappingTime(x.Select(x=>x.Dns)).TotalSeconds,
                    GroupMinQueryTimeS = x.Select(y => (decimal)y.Dns.Duration.TotalSeconds).Min(),
                    GroupMaxQueryTimeS = x.Select(y => (decimal)y.Dns.Duration.TotalSeconds).Max(),
                    GroupTimedOut = x.Any(x => x.Dns.TimedOut),
                    GroupAdapters = String.Join(";", x.SelectMany(x => x.Dns.Adapters?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()).Distinct().OrderBy(x=>x)),
                    GroupProcesses = x.Select(x=>x.Process).Distinct().ToArray(),
                    GroupStatus = String.Join(";", x.Select(x=>x.Dns.QueryStatus)
                                        .Where(x => x != (int)Win32ErrorCodes.SUCCESS).Distinct().OrderBy(x=>x).Select(x=> (Win32ErrorCodes)x).Select(x=>x.ToString())),
                    GroupQueries = x.Select(x=> (DnsEvent) x.Dns).ToArray(),
                })
                .SortAscendingGetTopNLast(x => SortOrder == DumpCommand.SortOrders.Count ? x.GroupQueryCount : x.GroupQueryTimeS, null, TopN).ToArray();

                string adapterHeadline = "";
                const int adapterWidth = 30;
                if( ShowAdapter )
                {
                    adapterHeadline = " Network Adapter".WithWidth(adapterWidth);
                }

                string returnCodeHeadline = "";
                const int returnCodeWidth = 30;
                if( ShowReturnCode )
                {
                    returnCodeHeadline = " Return Code".WithWidth(returnCodeWidth);
                }

                const int dnsQueryWidth = -70;
                string dnsQueryHeadline = "DNS Query".WithWidth(dnsQueryWidth);

                ColorConsole.WriteEmbeddedColorLine($"[green]NonOverlapping[/green]       Min        [yellow]Max[/yellow]  Count TimeOut [yellow]{dnsQueryHeadline}[/yellow]{returnCodeHeadline}{adapterHeadline}");
                ColorConsole.WriteEmbeddedColorLine($"       [green]Total s[/green]         s        [yellow]  s[/yellow]      #");

                ETWProcess[] previous = null;
                foreach (MatchData data in sorted)
                {
                    if( !MinMaxTotalTimeMs.IsWithin( (double) (data.GroupQueryTimeS*1000) ) )
                    {
                        continue;
                    }

                    if (!ShowDetails && ShowProcess) // when -Details is set we already show every query with process name
                    {
                        if (previous == null || !previous.SequenceEqual(data.GroupProcesses))
                        {
                            foreach (var proc in data.GroupProcesses)
                            {
                                ColorConsole.WriteEmbeddedColorLine($"[magenta]{proc.GetProcessWithId(UsePrettyProcessName)}[/magenta] {(NoCmdLine ? "" : proc.CommandLineNoExe)}");
                            }
                        }
                        previous = data.GroupProcesses;
                    }

                    totalTimeS += data.GroupQueryTimeS;
                    totalQueries += data.GroupQueryCount;

                    string dnsQueryTime = $"{data.GroupQueryTimeS:F3}";
                    string minTime = $"{data.GroupMinQueryTimeS:F3}";
                    string maxTime = $"{data.GroupMaxQueryTimeS:F3}";
                    string timedOut = $"{(data.GroupTimedOut ? "1" : "")}";
                    string adapter = ShowAdapter ? $" {data.GroupAdapters.WithWidth(adapterWidth-1)}" : "";
                    string returnCode = ShowReturnCode ? $" {data.GroupStatus.WithWidth(returnCodeWidth-1)}" : "";

                    ColorConsole.WriteEmbeddedColorLine($"[green]{dnsQueryTime,12} s[/green]  {minTime,6} s  [yellow]{maxTime,7} s[/yellow] {data.GroupQueryCount,6} [red]{timedOut,7}[/red] [yellow]{data.GroupQuery,dnsQueryWidth}[/yellow]{returnCode}{adapter}");
                    if (ShowDetails)
                    {
                        foreach (DnsEvent dnsEvent in data.GroupQueries.OrderBy(x => x.Start).Skip(TopNDetails.SkipN).Take(TopNDetails.TakeN))
                        {
                            string duration = $"{dnsEvent.Duration.TotalSeconds:F3}".WithWidth(6);
                            string timedOutServer = "";
                            if( dnsEvent.TimedOut )
                            {
                                timedOutServer = $"[red]TimedOut: {dnsEvent.TimedOutServer}[/red] Servers: {dnsEvent.ServerList} ";
                            }

                            ColorConsole.WriteEmbeddedColorLine($"\t{GetDateTimeString(dnsEvent.Start, data.SessionStart, TimeFormatOption),-7} Duration: [green]{duration} s[/green] [magenta]{dnsEvent.Process.GetProcessWithId(UsePrettyProcessName).WithWidth(-45)}[/magenta] {returnCode}{adapter}{timedOutServer}DnsAnswer: {dnsEvent.GetNonAliasResult()}");
                        }
                    }
                }
            }

            if( totalQueries > 0 )
            {
                ColorConsole.WriteEmbeddedColorLine($"Totals: [green]{totalTimeS:F3} s[/green] Dns query time for [magenta]{totalQueries}[/magenta] Dns queries");
            }
        }

        /// <summary>
        /// Some DNS queries are executed in parallel. Count overlapping parallel time ranges only once to give an estimate
        /// of the actual wall clock delay the user experiences. Summing up parallel queries for the same DNS Query leads 
        /// to wrong estimates which query the user did see as slowest.
        /// </summary>
        /// <param name="events">Collection of DNS events for a given DNS query</param>
        /// <returns>Non overlapping TimeSpan</returns>
        TimeSpan CalulcateNonOverlappingTime( IEnumerable<IDnsEvent> events)
        {
            TimeRangeCalculatorDateTime calc = new TimeRangeCalculatorDateTime();
            foreach(var ev in events)
            {
                calc.Add(ev.Start, ev.Duration);
            }

            return calc.GetDuration();
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
                    if (file?.Extract?.Network?.DnsClient?.Events?.Count == null)
                    {
                        ColorConsole.WriteError($"Warning: File {GetPrintFileName(file.FileName)} does not contain Dns data.");
                        continue;
                    }

                    foreach(IDnsEvent dns in file.Extract.Network.DnsClient.Events)
                    {
                        ETWProcess process = file.Extract.GetProcess(dns.ProcessIdx);

                        if( !IsMatchingProcessAndCmdLine(file, process) )
                        {
                            continue;
                        }

                        if( !MinMaxTimeMs.IsWithin( dns.Duration.TotalMilliseconds) )
                        {
                            continue;
                        }

                        if( DnsQueryFilter.Value?.Invoke(dns.Query) != true)
                        {
                            continue;
                        }

                        DnsEvent ev = (DnsEvent) dns;
                        ev.Process = file.Extract.GetProcess(ev.ProcessIdx);

                        MatchData data = new MatchData
                        {
                            Process = process,
                            Dns = dns,
                            File = file,
                            SessionStart = file.Extract.SessionStart,
                            Baseline = file?.Extract?.MainModuleVersion?.ToString() ?? "",
                        };

                        lret.Add(data);
                    }
                }
            }

            return lret;
        }

        /// <summary>
        /// Used by context sensitive help to print the allowed values.
        /// </summary>
        static internal readonly SortOrders[] ValidSortOrders = new[]
        {
            SortOrders.Count,
            SortOrders.Time,
            SortOrders.Default,
        };

        internal class MatchData
        {
            public ETWProcess Process;
            public IDnsEvent Dns;
            public TestDataFile File { get; set; }
            public string Baseline { get; set; }
            public DateTimeOffset SessionStart { get; set; }


            /// <summary>
            /// Grouped data
            /// </summary>
            public string GroupQuery;
            public decimal GroupQueryTimeS;
            public int GroupQueryCount;
            public decimal GroupMinQueryTimeS { get; set; }
            public decimal GroupMaxQueryTimeS { get; set; }
            public string GroupAdapters { get; set; }
            public bool GroupTimedOut { get; set; }
            public ETWProcess[] GroupProcesses { get; set; }
            public string GroupStatus { get; set; }

            /// <summary>
            /// All DNS Queries belonging to this GroupQuery Key
            /// </summary>
            public DnsEvent[] GroupQueries { get; internal set; }
        }
    }
}
