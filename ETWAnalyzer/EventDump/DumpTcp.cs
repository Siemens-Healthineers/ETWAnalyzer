//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Network.Tcp;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ETWAnalyzer.Commands.DumpCommand;

namespace ETWAnalyzer.EventDump
{
    internal class DumpTcp : DumpFileDirBase<DumpTcp.MatchData>
    {
        /// <summary>
        /// Used by help to print only relevant sort values
        /// </summary>
        internal static SortOrders[] SupportedSortOrders = new SortOrders[]
        {
            SortOrders.ReceivedSize,
            SortOrders.ReceivedCount,
            SortOrders.SentSize,
            SortOrders.SentCount,
            SortOrders.TotalCount,
            SortOrders.TotalSize,
            SortOrders.ConnectTime,
            SortOrders.DisconnectTime,
            SortOrders.RetransmissionCount,
            SortOrders.RetransmissionTime,
            SortOrders.MaxRetransmissionTime,
        };

        /// <summary>
        /// Used by help to print only relevant sort values
        /// </summary>
        internal static SortOrders[] SupportRetransmitSortOrders = new SortOrders[]
        {
            SortOrders.Delay,
            SortOrders.Time,
        };


        public bool NoCmdLine { get; set; }
        public SkipTakeRange TopN { get; set; }

        public DumpCommand.SortOrders SortOrder { get; internal set; }

        public bool ShowDetails { get; set; }

        public KeyValuePair<string, Func<string, bool>> IpPortFilter { get; internal set; } = new KeyValuePair<string, Func<string, bool>>(null, x => true);

        /// <summary>
        /// Filter for every Tcp Retransmission time
        /// </summary>
        public MinMaxRange<int> MinMaxRetransDelayMs { get; internal set; }
        public MinMaxRange<int> MinMaxRetransBytes { get; internal set; }
        public bool ShowRetransmit { get; internal set; }
        public KeyValuePair<string, Func<string, bool>> TcbFilter { get; internal set; } = new KeyValuePair<string, Func<string, bool>>(null, x => true);
        public SortOrders RetransSortOrder { get; internal set; }
        public SkipTakeRange TopNRetrans { get; internal set; } = new SkipTakeRange();
        public MinMaxRange<ulong> MinMaxSentBytes { get; internal set; } = new();
        public MinMaxRange<ulong> MinMaxReceivedBytes { get; internal set; } = new();

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
                                  "SourceIP","Source Port", "DestinationIP", "Destination Port", "TCB", "ConnectionIdx", "Sent Packets (Total per connection)", "Sent Bytes (Total per connection)", "Received Packets (Total per connection)", "Received Bytes (Total per connection)", 
                                  "Retransmitted Packets (Total per connection)", "% Retransmitted Packets (Total per connection)", "TCP Template", "Connection Open Time", "Connection Close Time",
                                  "Retrans Time", "Retrans Delay (ms)", "Retrans Size", "Retrans SequenceNr",  Col_CommandLine);

                foreach (MatchData tcpEvent in lret)
                {
                    string tcb = "0x" + tcpEvent.Connection.Tcb.ToString("X");
                    int retransPercent = tcpEvent.Retransmissions.Count > 0 ? (int)( (100.0f * tcpEvent.Retransmissions.Count/ tcpEvent.Connection.DatagramsSent)) : 0;
                   
                    if (ShowRetransmit)
                    {
                        // write data for all retransmit events 
                        // this repeats total columns!
                        foreach (var retrans in tcpEvent.Retransmissions)
                        {
                            WriteCSVLine(CSVOptions, Path.GetDirectoryName(tcpEvent.Session.FileName),
                                Path.GetFileNameWithoutExtension(tcpEvent.Session.FileName), tcpEvent.Session.SessionStart, tcpEvent.Session.TestName, tcpEvent.Session.TestDurationInMs, tcpEvent.Session.Baseline,
                                tcpEvent.Process.ProcessWithID, tcpEvent.Process.ProcessNamePretty,
                                tcpEvent.Connection.LocalIpAndPort.Address,
                                tcpEvent.Connection.LocalIpAndPort.Port,
                                tcpEvent.Connection.RemoteIpAndPort.Address,
                                tcpEvent.Connection.RemoteIpAndPort.Port,
                                tcb,
                                tcpEvent.Connection.DatagramsSent, tcpEvent.Connection.BytesSent, tcpEvent.Connection.DatagramsReceived, tcpEvent.Connection.BytesReceived,
                                tcpEvent.Retransmissions.Count,
                                retransPercent,
                                tcpEvent.Connection.LastTcpTemplate,
                                GetDateTimeString(tcpEvent.Connection.TimeStampOpen, tcpEvent.Session.SessionStart, TimeFormatOption, false),
                                GetDateTimeString(tcpEvent.Connection.TimeStampClose, tcpEvent.Session.SessionStart, TimeFormatOption, false),
                                GetDateTimeString(retrans.RetransmitTime, tcpEvent.Session.SessionStart, TimeFormatOption, false),
                                (int) retrans.RetransmitDiff().TotalMilliseconds,
                                retrans.BytesSent,
                                retrans.SequenceNumber,
                                NoCmdLine ? "" : tcpEvent.Process.CommandLineNoExe);
                        }
                    }
                    else
                    {
                       WriteCSVLine(CSVOptions, Path.GetDirectoryName(tcpEvent.Session.FileName),
                       Path.GetFileNameWithoutExtension(tcpEvent.Session.FileName), tcpEvent.Session.SessionStart, tcpEvent.Session.TestName, tcpEvent.Session.TestDurationInMs, tcpEvent.Session.Baseline,
                       tcpEvent.Process.ProcessWithID, tcpEvent.Process.ProcessNamePretty,
                       tcpEvent.Connection.LocalIpAndPort.Address,
                       tcpEvent.Connection.LocalIpAndPort.Port,
                       tcpEvent.Connection.RemoteIpAndPort.Address,
                       tcpEvent.Connection.RemoteIpAndPort.Port,
                       tcb,
                       tcpEvent.ConnectionIndex,
                       tcpEvent.Connection.DatagramsSent, tcpEvent.Connection.BytesSent, tcpEvent.Connection.DatagramsReceived, tcpEvent.Connection.BytesReceived,
                       tcpEvent.Retransmissions.Count,
                       retransPercent,
                       tcpEvent.Connection.LastTcpTemplate,
                       GetDateTimeString(tcpEvent.Connection.TimeStampOpen, tcpEvent.Session.SessionStart, TimeFormatOption, false),
                       GetDateTimeString(tcpEvent.Connection.TimeStampClose, tcpEvent.Session.SessionStart, TimeFormatOption, false),
                       "", "", "", "",
                       NoCmdLine ? "" : tcpEvent.Process.CommandLineNoExe);
                    }
                }
            }
            else
            {
                PrintMatches(lret);
            }

            return lret;
        }


        private void PrintMatches(List<MatchData> data)
        {
            if( data.Count == 0 )
            {
                return;
            }

            var byFile = data.GroupBy(x => x.Session.FileName).OrderBy(x => x.First().Session.SessionStart);

            MatchData[] allPrinted = byFile.SelectMany(x => x.SortAscendingGetTopNLast(SortBy, null, TopN)).ToArray(); 

            int localIPLen = allPrinted.Max(x => x.Connection.LocalIpAndPort.ToString().Length);
            int remoteIPLen = allPrinted.Max(x => x.Connection.RemoteIpAndPort.ToString().Length);
            int tcpTemplateLen = allPrinted.Max(x => x.Connection.LastTcpTemplate?.Length).GetValueOrDefault();

            string connectionHeadline = "Source IP/Port -> Destination IP/Port".WithWidth(localIPLen+remoteIPLen+4);
            const int PacketCountWidth = 9;
            const int BytesCountWidth = 15;
            const int PercentWidth = 4;
            const int RetransMsWidth = 6;
            int timeWidth = GetWidth(TimeFormatOption);
            const int PointerWidth = 16;
            const int TotalColumnWidth = 22;

            string sentHeadline = "Sent Packets/Bytes".WithWidth(PacketCountWidth + BytesCountWidth+7);
            string receivedHeadline = "Received Packets/Bytes".WithWidth(PacketCountWidth + BytesCountWidth+7);
            string retransmissionHeadline = "Retrans Count/%/Delay".WithWidth(PacketCountWidth+PercentWidth+ PacketCountWidth+7);
            string detailsMinMaxMedian = ShowDetails ? "Max/Median/Min in ms".WithWidth(3 * (RetransMsWidth+4)+1) : " ";
            string detailsTemplate = ShowDetails ? "Template".WithWidth(tcpTemplateLen+2) : "";
            string detailsConnectionTimes = ShowDetails ? "Connect/Disconnect Time".WithWidth(2 * timeWidth+3) : "";
            string detailsTCB = ShowDetails ? "TCB".WithWidth(PointerWidth + 3) : "";

            string headline = $"[yellow]{connectionHeadline}[/yellow] [green]{receivedHeadline}[/green] [red]{sentHeadline}[/red] [magenta]{GetTotalColumnHeader(TotalColumnWidth)}[/magenta][yellow]{retransmissionHeadline}[/yellow][yellow]{detailsMinMaxMedian}[/yellow]"+
                              $"{detailsTemplate}{detailsConnectionTimes}{detailsTCB} [magenta]Process[/magenta]";

            ColorConsole.WriteEmbeddedColorLine(headline);

            foreach (var file in byFile)
            {
                ColorConsole.WriteEmbeddedColorLine($"{file.First().Session.SessionStart,-22} {GetPrintFileName(file.Key)} {file.First().Session.Baseline}", ConsoleColor.Cyan);
                foreach (var match in file.SortAscendingGetTopNLast(SortBy, null, TopN) )
                {
                    string retransPercent = "N0".WidthFormat(100.0f * match.Retransmissions.Count / match.Connection.DatagramsSent, PercentWidth);
                    string totalRetransDelay = "N0".WidthFormat(match.Retransmissions.Sum(x => x.RetransmitDiff().TotalMilliseconds), PacketCountWidth);

                    ColorConsole.WriteEmbeddedColorLine($"{match.Connection.LocalIpAndPort.ToString().WithWidth(localIPLen)} -> {match.Connection.RemoteIpAndPort.ToString().WithWidth(remoteIPLen)} ", ConsoleColor.Yellow, true);
                    ColorConsole.WriteEmbeddedColorLine(
                                      $"[green]{"N0".WidthFormat(match.Connection.DatagramsReceived, PacketCountWidth)} {"N0".WidthFormat(match.Connection.BytesReceived, BytesCountWidth)} Bytes[/green] " +
                                      $"[red]{"N0".WidthFormat(match.Connection.DatagramsSent, PacketCountWidth)} {"N0".WidthFormat(match.Connection.BytesSent, BytesCountWidth)} Bytes[/red] " +
                                      $"[magenta]{GetTotalString(match, TotalColumnWidth)}[/magenta]" +
                                      $"[yellow]{"N0".WidthFormat(match.Retransmissions.Count, PacketCountWidth)} {retransPercent} % {totalRetransDelay} ms [/yellow] " +
                        ( ShowDetails ? 
                                      $"[yellow]{"F0".WidthFormat(match.RetransMaxms, RetransMsWidth)} ms {"F0".WidthFormat(match.RetransMedianMs, RetransMsWidth)} ms {"F0".WidthFormat(match.RetransMinMs, RetransMsWidth)} ms [/yellow] " + 
                                      $"{(match.Connection.LastTcpTemplate ?? "-").WithWidth(tcpTemplateLen)} " +
                                      $"{GetDateTimeString(match.Connection.TimeStampOpen, match.Session.SessionStart, TimeFormatOption,true).WithWidth(timeWidth)} {GetDateTimeString(match.Connection.TimeStampClose, match.Session.SessionStart,TimeFormatOption,true).WithWidth(timeWidth)} " +
                                      $" 0x{"X".WidthFormat(match.Connection.Tcb, PointerWidth)} "
                                : "") +                                      
                                      $"[magenta]{match.Process.GetProcessWithId(UsePrettyProcessName)}[/magenta]", ConsoleColor.White, true);
                    ColorConsole.WriteLine(NoCmdLine ? "" : match.Process.CommandLineNoExe, ConsoleColor.DarkCyan);

                    if (ShowRetransmit)
                    {
                        foreach (ITcpRetransmission retrans in match.Retransmissions.SortAscendingGetTopNLast(SortRetransmit, null, TopNRetrans))
                        {
                            Console.WriteLine($"  {"F0".WidthFormat(retrans.RetransmitDiff().TotalMilliseconds, 10)} ms delay at {GetDateTimeString(retrans.RetransmitTime, match.Session.SessionStart, TimeFormatOption, true)} {"N0".WidthFormat(retrans.BytesSent,7)} bytes SequenceNr: {retrans.SequenceNumber} ");
                        }
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
                    if (file?.Extract?.Network?.TcpData?.Connections?.Count == null )
                    {
                        ColorConsole.WriteError($"Warning: File {GetPrintFileName(file.FileName)} does not contain Tcp data.");
                        continue;
                    }

                    var connections = file.Extract.Network.TcpData.Connections;
                    var retransByConnections = file.Extract.Network.TcpData.Retransmissions.ToLookup(x => file.Extract.Network.TcpData.Connections[(int)x.ConnectionIdx]);

                    for(int i=0;i< file.Extract.Network.TcpData.Connections.Count;i++)
                    {
                        ITcpConnection connection = file.Extract.Network.TcpData.Connections[i];
                        List<ITcpRetransmission> retransmissions = new();

                        var localIPAndPort = connection.LocalIpAndPort;
                        var remoteIPAndPort = connection.RemoteIpAndPort;

                        if (connection.ProcessIdx != ETWProcessIndex.Invalid)
                        {
                            var process = file.Extract.GetProcess(connection.ProcessIdx);

                            if (!base.IsMatchingProcessAndCmdLine(file, process.ToProcessKey()))
                            {
                                continue;
                            }
                        }

                        if (IpPortFilter.Value?.Invoke(localIPAndPort.ToString()) == false &&
                            IpPortFilter.Value?.Invoke(remoteIPAndPort.ToString()) == false)
                        {
                            continue;
                        }

                        if( TcbFilter.Value?.Invoke("0x"+connection.Tcb.ToString("X")) == false )
                        {
                            continue;
                        }

                        if( !MinMaxReceivedBytes.IsWithin(connection.BytesReceived))
                        {
                            continue;
                        }

                        if( !MinMaxSentBytes.IsWithin(connection.BytesSent))
                        {
                            continue;
                        }

                        foreach (var retransmission in retransByConnections[connection])
                        {
                            if (!MinMaxRetransDelayMs.IsWithin((int) retransmission.RetransmitDiff().TotalMilliseconds))
                            {
                                continue;
                            }

                            if( !MinMaxRetransBytes.IsWithin( retransmission.BytesSent))
                            {
                                continue;
                            }

                            retransmissions.Add(retransmission);
                        }

                        List<double> retransMs = retransmissions.Select(x => x.RetransmitDiff().TotalMilliseconds).ToList();

                        double medianMs = retransMs.Count > 0 ? retransMs.Median() : 0.0d;
                        double minMs =    retransMs.Count > 0 ? retransMs.Min()    : 0.0d;
                        double maxMs =    retransMs.Count > 0 ? retransMs.Max()    : 0.0d;

                        MatchData data = new()
                        {
                            RetransMedianMs = medianMs,
                            RetransMinMs = minMs,
                            RetransMaxms = maxMs,
                            Retransmissions = retransmissions,
                            Connection = connection,
                            ConnectionIndex = i + Math.Abs(file.FileName.GetHashCode()),  // make connection idx unique between files to keep % retransmit value in CSV <= 100%
                            Process = file.Extract.GetProcess(connection.ProcessIdx),
                            Session = new ETWSession
                            {
                                FileName = file.FileName,
                                TestName = file.TestName,
                                TestDurationInMs = file.DurationInMs,
                                SessionStart = file.Extract.SessionStart,
                                Baseline = file?.Extract?.MainModuleVersion?.ToString() ?? "",
                            }
                        };

                        data.Session.Parent = data;

                        lret.Add(data);

                    }

 
                }
            }

            return lret;
        }

        /// <summary>
        /// Sort order for single retransmission events when we are displaying them
        /// </summary>
        /// <param name="retrans"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        decimal SortRetransmit(ITcpRetransmission retrans)
        {
            decimal lret = 0;
            lret = RetransSortOrder switch
            {
                SortOrders.Delay => (decimal) retrans.RetransmitDiff().TotalSeconds,
                SortOrders.Time => retrans.RetransmitTime.Ticks,
                SortOrders.Default => retrans.RetransmitTime.Ticks,
                _ => throw new NotSupportedException($"Sort order {RetransSortOrder} is not suppported."),
            };

            return lret;
        }


        /// <summary>
        /// Sort by connection summaries
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        decimal SortBy(MatchData match)
        {
            var connection = match.Connection;

            decimal lret = 0;
            lret = SortOrder switch
            {
                SortOrders.ReceivedSize => connection.BytesReceived,
                SortOrders.ReceivedCount => connection.DatagramsReceived,
                SortOrders.SentSize => connection.BytesSent,
                SortOrders.SentCount => connection.DatagramsSent,
                SortOrders.TotalCount => connection.DatagramsReceived + connection.DatagramsSent,
                SortOrders.TotalSize => connection.BytesReceived + connection.BytesSent,
                SortOrders.ConnectTime => connection.TimeStampOpen.HasValue ? connection.TimeStampOpen.Value.Ticks : 0,
                SortOrders.DisconnectTime => connection.TimeStampClose.HasValue ? connection.TimeStampClose.Value.Ticks : 0,
                SortOrders.RetransmissionCount => match.Retransmissions.Count,
                SortOrders.RetransmissionTime => (decimal) match.Retransmissions.Sum(x=>x.RetransmitDiff().TotalSeconds),
                SortOrders.MaxRetransmissionTime => (decimal) match.RetransMaxms,
                _ => match.Retransmissions.Count,
            };
            return lret;
        }

        /// <summary>
        /// When data is sorted which is not printed by default we add it as dynamic column to make the sorted data visible.
        /// </summary>
        /// <param name="minWidth"></param>
        /// <returns></returns>
        string GetTotalColumnHeader(int minWidth)
        {
            return SortOrder switch
            {
                SortOrders.TotalCount => "Total Count".WithWidth(minWidth),
                SortOrders.TotalSize => "Total Size".WithWidth(minWidth),
                _ => ""
            };
        }

        /// <summary>
        /// Get Total string which is dynamically added to output to make sorted data visible.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="minWidth"></param>
        /// <returns></returns>
        string GetTotalString(MatchData data, int minWidth)
        {
            return SortOrder switch
            {
                SortOrders.TotalCount => "N0".WidthFormat(data.Connection.DatagramsReceived + data.Connection.DatagramsSent, minWidth),
                SortOrders.TotalSize => $"{data.Connection.BytesReceived + data.Connection.BytesSent:N0} Bytes".WithWidth(minWidth),
                _ => "",
            };
        }

        public class MatchData
        {
            public List<ITcpRetransmission> Retransmissions { get; internal set; } = new();
            public ETWSession Session { get; internal set; }
            public ITcpConnection Connection { get; internal set; }
            public ETWProcess Process { get; internal set; }
            public int ConnectionIndex { get; internal set; }
            public double RetransMedianMs { get; internal set; }
            public double RetransMinMs { get; internal set; }
            public double RetransMaxms { get; internal set; }
        }

        public class ETWSession
        {
            public MatchData Parent { get; set; }
            public string FileName { get; set; }
            public DateTimeOffset SessionStart { get; set; }
            public string Baseline { get; set; }
            public string TestName { get; internal set; }
            public int TestDurationInMs { get; internal set; }
        }
    }
}
