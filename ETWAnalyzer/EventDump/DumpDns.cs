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

namespace ETWAnalyzer.EventDump
{
    class DumpDns : DumpFileDirBase<DumpDns.MatchData>
    {
        public bool NoCmdLine { get; set; }
        public SkipTakeRange TopN { get; set; }

        public bool ShowReturnCode { get; set; }
        public bool ShowAdapter { get; set; }
        public DumpCommand.SortOrders SortOrder { get; internal set; }

        List<MatchData> myUTestData = null;

        public override List<MatchData> ExecuteInternal()
        {
            List<MatchData> lret = ReadFileData();
            if (IsCSVEnabled)
            {
                OpenCSVWithHeader("CSVOptions", "Directory", "FileName", "Date", "Test Case", "Test Time in ms", "Baseline", "Process", "ProcessName",
                                  "DNS Query", "Query StatusCode", "TimedOut", "Start Time", "Duration in s", "Queried Network Adapters", "Server List", "DNS Result",
                                  "Command Line");

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
            foreach (var byFile in byFileGroups)
            {
                MatchData firstMatch = byFile.First();

                PrintFileName(firstMatch.File.FileName, null, firstMatch.File.PerformedAt, firstMatch.Baseline);

                MatchData[] sorted = byFile.GroupBy(x => x.Dns.Query).Select(x => new MatchData
                {
                    GroupQuery = x.Key,
                    GroupQueries = x.Count(),
                    GroupQueryTimeS = x.Sum(y => (decimal)y.Dns.Duration.TotalSeconds),
                    GroupMinQueryTimeS = x.Select(y => (decimal)y.Dns.Duration.TotalSeconds).Min(),
                    GroupMaxQueryTimeS = x.Select(y => (decimal)y.Dns.Duration.TotalSeconds).Max(),
                    GroupTimedOut = x.Any(x => x.Dns.TimedOut),
                    GroupAdapters = String.Join(" ", x.SelectMany(x => x.Dns.Adapters?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()).Distinct().OrderBy(x=>x)),
                    GroupProcesses = x.Select(x=>x.Process).Distinct().ToArray(),
                    GroupStatus = String.Join(";", x.Select(x=>x.Dns.QueryStatus)
                                        .Where(x => x != (int)Win32ErrorCodes.ERROR_INVALID_PARAMETER && x != (int)Win32ErrorCodes.SUCCESS).Distinct().OrderBy(x=>x).Select(x=> (Win32ErrorCodes)x).Select(x=>x.ToString())),
                })
                .SortAscendingGetTopNLast(x => SortOrder == DumpCommand.SortOrders.Count ? x.GroupQueries : x.GroupQueryTimeS, null, TopN).ToArray();

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

                ColorConsole.WriteEmbeddedColorLine($"     [green]Total[/green]       Min       [yellow]Max[/yellow]  Count TimeOut {dnsQueryHeadline}{returnCodeHeadline}{adapterHeadline}");
                ETWProcess[] previous = null;
                foreach (MatchData data in sorted)
                {
                    if (previous == null || !previous.SequenceEqual(data.GroupProcesses))
                    {
                        foreach (var proc in data.GroupProcesses)
                        {
                            ColorConsole.WriteEmbeddedColorLine($"[magenta]{proc.GetProcessWithId(UsePrettyProcessName)}[/magenta] {(NoCmdLine ? "" : proc.CommandLineNoExe)}");
                        }
                    }
                    previous = data.GroupProcesses;

                    string totalTime = $"{data.GroupQueryTimeS:F3}";
                    string minTime = $"{data.GroupMinQueryTimeS:F3}";
                    string maxTime = $"{data.GroupMaxQueryTimeS:F3}";
                    string timedOut = $"{(data.GroupTimedOut ? "1" : "")}";
                    string adapter = ShowAdapter ? $" {data.GroupAdapters.WithWidth(adapterWidth-1)}" : "";
                    string returnCode = ShowReturnCode ? $" {data.GroupStatus.WithWidth(returnCodeWidth-1)}" : "";

                    ColorConsole.WriteEmbeddedColorLine($"[green]{totalTime,8} s[/green]  {minTime,6} s  [yellow]{maxTime,7}s[/yellow] {data.GroupQueries,6} [red]{timedOut,7}[/red] {data.GroupQuery,dnsQueryWidth}{returnCode}{adapter}");
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
            public int GroupQueries;
            public decimal GroupMinQueryTimeS { get; set; }
            public decimal GroupMaxQueryTimeS { get; set; }
            public string GroupAdapters { get; set; }
            public bool GroupTimedOut { get; set; }
            public ETWProcess[] GroupProcesses { get; set; }
            public string GroupStatus { get; set; }

        }
    }
}
