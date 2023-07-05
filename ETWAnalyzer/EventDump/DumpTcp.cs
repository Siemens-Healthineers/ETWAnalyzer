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
        public MinMaxRange<int> MinMaxConnectionDurationS { get; internal set; } = new();
        public bool ShowRetransmit { get; internal set; }
        public KeyValuePair<string, Func<string, bool>> TcbFilter { get; internal set; } = new KeyValuePair<string, Func<string, bool>>(null, x => true);
        public SortOrders RetransSortOrder { get; internal set; }
        public SkipTakeRange TopNRetrans { get; internal set; } = new SkipTakeRange();
        public MinMaxRange<ulong> MinMaxSentBytes { get; internal set; } = new();
        public MinMaxRange<ulong> MinMaxReceivedBytes { get; internal set; } = new();
        public bool OnlyClientRetransmit { get; internal set; }
        public MinMaxRange<int> MinMaxRetransCount { get; internal set; } = new();

        public TotalModes? ShowTotal { get; internal set; }

        /// <summary>
        /// Show per file totals
        /// </summary>
        bool IsSummary => ShowTotal switch
        {
            TotalModes.None => false,
            _ => true,
        };


        /// <summary>
        /// Unit testing only. ReadFileData will return this list instead of real data
        /// </summary>
        internal List<MatchData> myUTestData = null;

        public override List<MatchData> ExecuteInternal()
        {
            List<MatchData> lret = ReadFileData();
            if (IsCSVEnabled)
            {
                string[] columms = new string[]
                    {   Col_CSVOptions, "Directory", Col_FileName, Col_Date, Col_TestCase, Col_TestTimeinms, Col_Baseline, Col_Process, Col_ProcessName, "Start Time", "End Time", "Duration in s",
                        "SourceIP","Source Port", "DestinationIP", "Destination Port", "TCB", "ConnectionIdx", "Sent Packets (Total per connection)", "Sent Bytes (Total per connection)", "Received Packets (Total per connection)", "Received Bytes (Total per connection)",
                        "Retransmitted Packets (Total per connection)", "% Retransmitted Packets (Total per connection)", "TCP Template", "Connection Open Time", "Connection Close Time",
                    };

                string[] additionalColumns = new string[] { Col_CommandLine };

                if (ShowRetransmit || ShowDetails)
                {
                    additionalColumns = new string[]
                    {
                        "Retrans Time", "Retrans Delay (ms)", "Retrans Size", "Retrans SequenceNr", "IsClientRetransmission", Col_CommandLine
                    };
                }

                columms = columms.Concat(additionalColumns).ToArray();

                OpenCSVWithHeader(columms);
                                  

                foreach (MatchData tcpEvent in lret)
                {
                    string tcb = "0x" + tcpEvent.Connection.Tcb.ToString("X");
                    int retransPercent = tcpEvent.Retransmissions.Count > 0 ? (int)( (100.0f * tcpEvent.Retransmissions.Count/ tcpEvent.Connection.DatagramsSent)) : 0;
                   
                    if (ShowRetransmit || ShowDetails)
                    {
                        bool first = true;
                        // write data for all retransmit events 
                        // this repeats total columns!
                        foreach (var retrans in tcpEvent.Retransmissions)
                        {
                            WriteCSVLine(CSVOptions, Path.GetDirectoryName(tcpEvent.Session.FileName),
                                Path.GetFileNameWithoutExtension(tcpEvent.Session.FileName), tcpEvent.Session.SessionStart, tcpEvent.Session.TestName, tcpEvent.Session.TestDurationInMs, tcpEvent.Session.Baseline,
                                tcpEvent.Process.ProcessWithID, 
                                tcpEvent.Process.ProcessNamePretty, 
                                tcpEvent.Process.IsNew ? GetDateTimeString(tcpEvent.Process.StartTime, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false) : "",
                                tcpEvent.Process.HasEnded ?  GetDateTimeString(tcpEvent.Process.EndTime, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false) : "",
                                (tcpEvent.Process.IsNew && tcpEvent.Process.HasEnded) ? (tcpEvent.Process.EndTime - tcpEvent.Process.StartTime).TotalSeconds : "",
                                tcpEvent.Connection.LocalIpAndPort.Address,
                                tcpEvent.Connection.LocalIpAndPort.Port,
                                tcpEvent.Connection.RemoteIpAndPort.Address,
                                tcpEvent.Connection.RemoteIpAndPort.Port,
                                tcb,
                                tcpEvent.ConnectionIndex,
                                (first ? tcpEvent.Connection.DatagramsSent : 0),      // print only once per connection to get better pivot chart data which will sum the connection summary per retransmit event which would result in huge numbers
                                (first ? tcpEvent.Connection.BytesSent : 0),
                                (first ? tcpEvent.Connection.DatagramsReceived : 0),
                                (first ? tcpEvent.Connection.BytesReceived : 0),  
                                (first ? tcpEvent.Retransmissions.Count : 0),     
                                (first ? retransPercent : 0),                                
                                tcpEvent.Connection.LastTcpTemplate,
                                GetDateTimeString(tcpEvent.Connection.TimeStampOpen, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false),
                                GetDateTimeString(tcpEvent.Connection.TimeStampClose, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false),
                                GetDateTimeString(retrans.RetransmitTime, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false),
                                (int) retrans.RetransmitDiff().TotalMilliseconds,
                                retrans.NumBytes,
                                retrans.SequenceNumber,
                                retrans.IsClientRetransmission.GetValueOrDefault(),
                                NoCmdLine ? "" : tcpEvent.Process.CommandLineNoExe);

                            first = false;
                        }
                    }
                    else
                    {
                       WriteCSVLine(CSVOptions, Path.GetDirectoryName(tcpEvent.Session.FileName),
                       Path.GetFileNameWithoutExtension(tcpEvent.Session.FileName), tcpEvent.Session.SessionStart, tcpEvent.Session.TestName, tcpEvent.Session.TestDurationInMs, tcpEvent.Session.Baseline,
                       tcpEvent.Process.ProcessWithID, tcpEvent.Process.ProcessNamePretty,
                       tcpEvent.Process.IsNew ? GetDateTimeString(tcpEvent.Process.StartTime, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false) : "",
                       tcpEvent.Process.HasEnded ? GetDateTimeString(tcpEvent.Process.EndTime, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false) : "",
                       (tcpEvent.Process.IsNew && tcpEvent.Process.HasEnded) ? (tcpEvent.Process.EndTime - tcpEvent.Process.StartTime).TotalSeconds : "",
                       tcpEvent.Connection.LocalIpAndPort.Address,
                       tcpEvent.Connection.LocalIpAndPort.Port,
                       tcpEvent.Connection.RemoteIpAndPort.Address,
                       tcpEvent.Connection.RemoteIpAndPort.Port,
                       tcb,
                       tcpEvent.ConnectionIndex,
                       tcpEvent.Connection.DatagramsSent, 
                       tcpEvent.Connection.BytesSent, 
                       tcpEvent.Connection.DatagramsReceived, 
                       tcpEvent.Connection.BytesReceived,
                       tcpEvent.Retransmissions.Count,
                       retransPercent,
                       tcpEvent.Connection.LastTcpTemplate,
                       GetDateTimeString(tcpEvent.Connection.TimeStampOpen, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false),
                       GetDateTimeString(tcpEvent.Connection.TimeStampClose, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false),
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

            if ( data.Count == 0 )
            {
                return;
            }

            var byFile = data.Where(MinMaxFilter).GroupBy(x => x.Session.FileName).OrderBy(x => x.First().Session.SessionStart);

            MatchData[] allPrinted = byFile.SelectMany(x => x.SortAscendingGetTopNLast(SortBy, x=>x.Connection.BytesReceived+x.Connection.BytesSent, null, TopN)).ToArray(); 

            int localIPLen = allPrinted.Max(x => x.Connection.LocalIpAndPort.ToString().Length);
            int remoteIPLen = allPrinted.Max(x => x.Connection.RemoteIpAndPort.ToString().Length);
            int tcpTemplateLen = allPrinted.Max(x => x.Connection.LastTcpTemplate?.Length) ?? 8;

            const string ConnectionHeadlineStr = "Source IP/Port -> Destination IP/Port";
            int totalIPLen = localIPLen + remoteIPLen + 4;

            if (totalIPLen < ConnectionHeadlineStr.Length ) // increase minimum width if headline is longer than local and remote ip
            {
                remoteIPLen += ConnectionHeadlineStr.Length - totalIPLen;
            }

            string connectionHeadline = ConnectionHeadlineStr.WithWidth(localIPLen+remoteIPLen+4);
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
            string detailsConnectionTimes = ShowDetails ? "Connect/Disconnect Time".WithWidth(2 * timeWidth+2) : "";
            string detailsTCB = ShowDetails ? "TCB".WithWidth(PointerWidth + 3) : "";

            string headline = $"[yellow]{connectionHeadline}[/yellow] [green]{receivedHeadline}[/green] [red]{sentHeadline}[/red] [magenta]{GetTotalColumnHeader(TotalColumnWidth)}[/magenta][yellow]{retransmissionHeadline}[/yellow][yellow]{detailsMinMaxMedian}[/yellow]"+
                              $"{detailsTemplate}{detailsConnectionTimes}{detailsTCB} [magenta]Process[/magenta]";

            ColorConsole.WriteEmbeddedColorLine(headline);

            foreach (var file in byFile)
            {
                ColorConsole.WriteEmbeddedColorLine($"{file.First().Session.SessionStart,-22} {GetPrintFileName(file.Key)} {file.First().Session.Baseline}", ConsoleColor.Cyan);
                int printedFiles = 0;

                // for total calculations
                int totalDatagramsReceived = 0;
                int totalDatagramsSent = 0;
                ulong totalBytesReceived = 0;
                ulong totalBytesSent = 0;
                int totalRetransmissionsCount = 0;
                double totalSumRetransDelay = 0;
                int totalConnectCounter = 0;

                foreach (var match in file.Where(MinMaxFilter).SortAscendingGetTopNLast(SortBy, x => x.Connection.BytesReceived + x.Connection.BytesSent, null, TopN) )
                {
                    totalDatagramsReceived += match.Connection.DatagramsReceived;
                    totalDatagramsSent += match.Connection.DatagramsSent;
                    totalBytesReceived += match.Connection.BytesReceived;
                    totalBytesSent += match.Connection.BytesSent;
                    totalRetransmissionsCount += match.Retransmissions.Count;
                    totalSumRetransDelay += match.Retransmissions.Sum(x => x.RetransmitDiff().TotalMilliseconds);
                    totalConnectCounter += match.InputConnectionCount;

                    // retransmission % can only be calculated by sent packets and retransmission events excluding client retransmissions
                    string retransPercent = "N0".WidthFormat(100.0f * match.Retransmissions.Where(x=>x.IsClientRetransmission.GetValueOrDefault() == false).Count() / match.Connection.DatagramsSent, PercentWidth);

                    // Delay on the other hand can be calculated by all Retransmit events.
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
                                      $"{GetDateTimeString(match.Connection.TimeStampOpen, match.Session.AdjustedSessionStart, TimeFormatOption, true).WithWidth(timeWidth)} {GetDateTimeString(match.Connection.TimeStampClose, match.Session.AdjustedSessionStart, TimeFormatOption, true).WithWidth(timeWidth)} " +
                                      $"0x{"X".WidthFormat(match.Connection.Tcb, PointerWidth)} "
                                : "") +                                      
                                      $"[magenta]{match.Process.GetProcessWithId(UsePrettyProcessName)}[/magenta][grey]{GetProcessTags(match.Process, match.Session.AdjustedSessionStart)}[/grey]", ConsoleColor.White, true);
                    ColorConsole.WriteLine(NoCmdLine ? "" : match.Process.CommandLineNoExe, ConsoleColor.DarkCyan);
                    printedFiles++;

                    if (ShowRetransmit)
                    {
                        foreach (ITcpRetransmission retrans in match.Retransmissions.SortAscendingGetTopNLast(SortRetransmit, null, TopNRetrans))
                        {
                            string clientTransmission = retrans.IsClientRetransmission == null ? "" : $"ClientRetransmission: {retrans.IsClientRetransmission.Value}";
                            Console.WriteLine($"  {"F0".WidthFormat(retrans.RetransmitDiff().TotalMilliseconds, 10)} ms delay at {GetDateTimeString(retrans.RetransmitTime, match.Session.AdjustedSessionStart, TimeFormatOption, true)} {"N0".WidthFormat(retrans.NumBytes,7)} bytes " +
                                              $"SequenceNr: {retrans.SequenceNumber} {clientTransmission}");
                        }
                    }
                }

                //show per file totals always
                {
                    int emptyWidth = totalIPLen + 1; //hide the port data always
                    const int totalTotalColumnWidth = 23;
                    string fileDatagramsReceived = $"{"N0".WidthFormat(totalDatagramsReceived, PacketCountWidth)}";
                    string fileDatagramsSent = $"{"N0".WidthFormat(totalDatagramsSent, PacketCountWidth)}";
                    string fileBytesReceived = $"{"N0".WidthFormat(totalBytesReceived, BytesCountWidth)}";
                    string fileBytesSent = $"{"N0".WidthFormat(totalBytesSent, BytesCountWidth)}";
                    string fileRetransmissionsCount = $"{"N0".WidthFormat(totalRetransmissionsCount, PacketCountWidth)}";
                    string fileSumRetransDelay = $"{"N0".WidthFormat(totalSumRetransDelay, PacketCountWidth)}";
                    string totalGetTotalString(int width)
                    {
                        return SortOrder switch
                        {
                            SortOrders.TotalCount => "N0".WidthFormat(totalDatagramsReceived + totalDatagramsSent, width),
                            SortOrders.TotalSize => $"{totalBytesReceived + totalBytesSent:N0} Bytes".WithWidth(width),
                            _ => ""
                        };
                    }

                    if (IsSummary && printedFiles > 1)
                    {
                        ColorConsole.WriteEmbeddedColorLine(
                            $"{"N0".WidthFormat("", emptyWidth)}[green]Received Total's: [/green]" +
                            $"[cyan]{fileDatagramsReceived} {fileBytesReceived} Bytes [/cyan]" +
                            $"[red]Sent Total's: [/red]" +
                            $" [cyan]{fileDatagramsSent} {fileBytesSent} Bytes [/cyan]" +
                            $"[cyan]{totalGetTotalString(totalTotalColumnWidth)}[/cyan]" +
                            $"[yellow]Retrans Total's: [/yellow]" +
                            $" [cyan]{fileRetransmissionsCount} {"N0".WidthFormat("", 5)}- {fileSumRetransDelay} ms[/cyan]" +
                            $"[white] Total Connection's accessed:[/white]" +
                            $"[cyan]{totalConnectCounter}[/cyan]")
                            ;
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

                        if (IpPortFilter.Value?.Invoke( localIPAndPort.ToString() + remoteIPAndPort.ToString() ) == false )
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

                        var retransmissionsForConnection = retransByConnections[connection];

                        foreach (var retransmission in retransmissionsForConnection)
                        {
                            if (!MinMaxRetransDelayMs.IsWithin((int) retransmission.RetransmitDiff().TotalMilliseconds))
                            {
                                continue;
                            }

                            if( !MinMaxRetransBytes.IsWithin( retransmission.NumBytes))
                            {
                                continue;
                            }

                            if( OnlyClientRetransmit ) // only keep client retransmissions
                            {
                                if( retransmission.IsClientRetransmission == null || retransmission.IsClientRetransmission.Value == false)
                                {
                                    continue;
                                }
                            }

                            retransmissions.Add(retransmission);
                        }


                        if (!MinMaxRetransCount.IsWithin(retransmissions.Count))
                        {
                            continue;
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
                                ZeroTimeS = GetZeroTimeInS(file.Extract),
                            },
                            InputConnectionCount = 1,
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

        internal bool MinMaxFilter(MatchData data)
        {
            bool lret = true;
            long startTime = data.Connection.TimeStampOpen.HasValue ? data.Connection.TimeStampOpen.Value.ToUnixTimeSeconds() : 0;
            long endTime = data.Connection.TimeStampClose.HasValue ? data.Connection.TimeStampClose.Value.ToUnixTimeSeconds() : 0;
            if (startTime != 0 && endTime != 0 && endTime - startTime > 0)
            {
                lret = MinMaxConnectionDurationS.IsWithin((int)(endTime - startTime));
            }

            return lret;
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
            public int InputConnectionCount { get; internal set; }
        }

        public class ETWSession
        {
            public MatchData Parent { get; set; }
            public string FileName { get; set; }
            public DateTimeOffset SessionStart { get; set; }

            public DateTimeOffset AdjustedSessionStart { get => SessionStart.AddSeconds(ZeroTimeS); }
            public string Baseline { get; set; }
            public string TestName { get; internal set; }
            public int TestDurationInMs { get; internal set; }
            public double ZeroTimeS { get; internal set; }
        }
    }
}
