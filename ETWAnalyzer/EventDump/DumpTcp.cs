//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Network.Tcp;
using ETWAnalyzer.Extract.Network.Tcp.Issues;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            SortOrders.ClientResetTime,
            SortOrders.RetransmissionCount,
            SortOrders.RetransmissionTime,
            SortOrders.MaxRetransmissionTime,
            SortOrders.LastReceivedTime,
            SortOrders.LastSentTime,
            SortOrders.Post,
            SortOrders.Inject,
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
        public bool ShowStats { get; set; }

        public KeyValuePair<string, Func<string, bool>> IpPortFilter { get; internal set; } = new KeyValuePair<string, Func<string, bool>>(null, x => true);

        /// <summary>
        /// Filter for every Tcp Retransmission time
        /// </summary>
        public MinMaxRange<double> MinMaxRetransDelayS { get; internal set; }
        public MinMaxRange<int> MinMaxRetransBytes { get; internal set; }
        public MinMaxRange<double> MinMaxConnectionDurationS { get; internal set; } = new();
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

        public bool Reset { get; internal set; }

        public MinMaxRange<double> MinMaxConnect { get; internal set; } = new();
        public MinMaxRange<double> MinMaxDisconnect { get; internal set; } = new();
        public MinMaxRange<double> MinMaxReceivedS { get; internal set; } = new();
        public MinMaxRange<double> MinMaxSentS { get; internal set; } = new();
        public bool KeepAliveFilter { get; internal set; }
        public MinMaxRange<double> MinMaxSentDelayS { get; internal set; }
        public MinMaxRange<double> MinMaxReceiveDelayS { get; internal set; }
        public static IssueTypes ValidIssueTypes { get; internal set; }
        public IssueTypes IssueType { get; internal set; }
        public MinMaxRange<ulong> MinMaxInject { get; internal set; } = new();
        public MinMaxRange<ulong> MinMaxPost { get; internal set; } = new();
        public MinMaxRange<ulong> MinMaxBytes { get; internal set; } = new();
        public MinMaxRange<ulong> MinMaxReceivedPackets { get; internal set; } = new();
        public MinMaxRange<ulong> MinMaxSentPackets { get; internal set; } = new();
        public MinMaxRange<ulong> MinMaxPackets { get; internal set; } = new();

        // statistics filter ranges can be null because if any of them is set the connection must have been closed to have valid statistics
        public MinMaxRange<ulong> MinMaxStatPacketsOut { get; internal set; }
        public MinMaxRange<ulong> MinMaxStatPacketsIn { get; internal set; }
        public MinMaxRange<ulong> MinMaxStatBytesIn { get; internal set; }
        public MinMaxRange<ulong> MinMaxStatBytesOut { get; internal set; }
        public MinMaxRange<ulong> MinMaxStatBytes { get; internal set; }
        public MinMaxRange<ulong> MinMaxStatPackets { get; internal set; }
        public MinMaxRange<double> MinMaxClientResetS { get; internal set; }

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
                        "Retransmitted Packets (Total per connection)", "% Retransmitted Packets (Total per connection)", "TCP Template", "Connection Open Time", "Connection Close Time", Col_ClientResetTime,
                        Col_LastSentTime, Col_LastReceivedTime, Col_KeepAlive, Col_MaxReceiveDelay, Col_MaxSendDelay, Col_StatBytesIn, Col_StatBytesOut, Col_StatSegmentsIn, Col_StatSegmentsOut,
                        Col_ResetTime,
                        "Posted Packets", "Injected Packets",
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
                                tcpEvent.Connection?.Statistics?.RstReceivedTime == null ? "" : GetDateTimeString(tcpEvent.Connection.Statistics.RstReceivedTime, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false),
                                GetDateTimeString(tcpEvent.Connection.Statistics.LastSent, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false),
                                GetDateTimeString(tcpEvent.Connection.Statistics.LastReceived, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false),
                                tcpEvent.Connection?.Statistics.KeepAlive == null ? "" : tcpEvent.Connection.Statistics.KeepAlive.Value.ToString(),
                                tcpEvent.Connection?.Statistics.MaxReceiveDelayS == null ? "" : tcpEvent.Connection?.Statistics.MaxReceiveDelayS.Value.ToString($"F{base.OverridenOrDefaultTimePrecision}"),
                                tcpEvent.Connection?.Statistics.MaxSendDelayS == null ? "" : tcpEvent.Connection?.Statistics.MaxSendDelayS.Value.ToString($"F{base.OverridenOrDefaultTimePrecision}"),
                                tcpEvent.Connection?.Statistics.DataBytesIn == null ? 0 : tcpEvent.Connection?.Statistics.DataBytesIn.Value,
                                tcpEvent.Connection?.Statistics.DataBytesOut == null ? 0 : tcpEvent.Connection?.Statistics.DataBytesOut.Value,
                                tcpEvent.Connection?.Statistics.SegmentsIn == null ? 0 : tcpEvent.Connection?.Statistics.SegmentsIn.Value,
                                tcpEvent.Connection?.Statistics.SegmentsOut == null ? 0 : tcpEvent.Connection?.Statistics.SegmentsOut.Value,
                                tcpEvent.Connection?.RetransmitTimeout == null ? "" : GetDateTimeString(tcpEvent.Connection.RetransmitTimeout, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false),
                                tcpEvent.Connection?.Statistics.SendPostedPosted == null ? 0 : tcpEvent.Connection?.Statistics.SendPostedPosted.Value,  
                                tcpEvent.Connection?.Statistics.SendPostedInjected == null ? 0 : tcpEvent.Connection?.Statistics.SendPostedInjected.Value,

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
                       tcpEvent.Connection?.Statistics?.RstReceivedTime == null ? "" : GetDateTimeString(tcpEvent.Connection.Statistics.RstReceivedTime, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false),
                       GetDateTimeString(tcpEvent.Connection?.Statistics.LastSent, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false),
                       GetDateTimeString(tcpEvent.Connection?.Statistics.LastReceived, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false),
                       tcpEvent.Connection?.Statistics.KeepAlive == null ? "" : tcpEvent.Connection.Statistics.KeepAlive.Value.ToString(),
                       tcpEvent.Connection?.Statistics.MaxReceiveDelayS == null ? "" : tcpEvent.Connection?.Statistics.MaxReceiveDelayS.Value.ToString($"F{base.OverridenOrDefaultTimePrecision}"),
                       tcpEvent.Connection?.Statistics.MaxSendDelayS == null ? "" : tcpEvent.Connection?.Statistics.MaxSendDelayS.Value.ToString($"F{base.OverridenOrDefaultTimePrecision}"),
                       tcpEvent.Connection?.Statistics.DataBytesIn == null ? 0 : tcpEvent.Connection?.Statistics.DataBytesIn.Value,
                       tcpEvent.Connection?.Statistics.DataBytesOut == null ? 0 : tcpEvent.Connection?.Statistics.DataBytesOut.Value,
                       tcpEvent.Connection?.Statistics.SegmentsIn == null ? 0 : tcpEvent.Connection?.Statistics.SegmentsIn.Value,
                       tcpEvent.Connection?.Statistics.SegmentsOut == null ? 0 : tcpEvent.Connection?.Statistics.SegmentsOut.Value,
                       tcpEvent.Connection?.RetransmitTimeout == null ? "" : GetDateTimeString(tcpEvent.Connection.RetransmitTimeout, tcpEvent.Session.AdjustedSessionStart, TimeFormatOption, false),
                       tcpEvent.Connection?.Statistics.SendPostedPosted == null ? 0 : tcpEvent.Connection?.Statistics.SendPostedPosted.Value,
                       tcpEvent.Connection?.Statistics.SendPostedInjected == null ? 0 : tcpEvent.Connection?.Statistics.SendPostedInjected.Value,

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

        const string Col_Connection = "Connection";
        const string Col_ReceivedPackets = "ReceivedPackets";
        const string Col_ReceivedBytes = "ReceivedBytes";
        const string Col_SentPackets = "SentPackets";
        const string Col_SentBytes = "SentBytes";
        const string Col_Total = "Total";
        const string Col_RetransmitCount = "RetransCount";
        const string Col_RetransmitPercent = "RetransPercent";
        const string Col_RetransmitDelay = "RetransDelay";
        const string Col_RetransmitMin = "RetransMin";
        const string Col_RetransmitMedian = "RetransMedian";
        const string Col_RetransmitMax = "RetransMax";
        const string Col_Template = "Template";
        const string Col_ConnectTime = "ConnectTime";
        const string Col_DisconnectTime = "DisconnectTime";
        const string Col_ResetTime = "ResetTime";
        const string Col_ClientResetTime = "ClientResetTime";   
        const string Col_LastSentTime = "LastSentTime";
        const string Col_LastReceivedTime = "LastReceivedTime";
        const string Col_TCB = "TCB";
        const string Col_KeepAlive = "KeepAlive";
        const string Col_MaxSendDelay = "MaxSendDelay";
        const string Col_MaxReceiveDelay = "MaxReceiveDelay";
        const string Col_StatBytesIn = "StatBytesIn";
        const string Col_StatBytesOut = "StatBytesOut";
        const string Col_StatSegmentsIn = "StatPacketsIn";
        const string Col_StatSegmentsOut = "StatPacketsOut";

        const string Col_EventTime = "EventTime";
        const string Col_PostMode = "PostMode";
        const string Col_SndNext = "SndNext";
        const string Col_Issue = "Issue";


        const int PointerWidth = 16;

        bool GetEnable(string columnName)
        {
            bool lret = columnName switch
            {
                Col_Connection => GetOverrideFlag(Col_Connection,true),
                Col_ReceivedPackets => GetOverrideFlag(Col_ReceivedPackets, true),
                Col_ReceivedBytes => GetOverrideFlag(Col_ReceivedBytes, true),
                Col_SentPackets => GetOverrideFlag(Col_SentPackets, true),
                Col_SentBytes => GetOverrideFlag(Col_SentBytes, true),
                Col_Total => GetOverrideFlag(Col_Total, SortOrder == SortOrders.TotalCount || SortOrder == SortOrders.TotalSize),
                Col_RetransmitCount => GetOverrideFlag(Col_RetransmitCount, true),
                Col_RetransmitPercent => GetOverrideFlag(Col_RetransmitPercent, true),
                Col_RetransmitDelay => GetOverrideFlag(Col_RetransmitDelay, true),
                Col_RetransmitMin => GetOverrideFlag(Col_RetransmitMin, ShowDetails),
                Col_RetransmitMedian => GetOverrideFlag(Col_RetransmitMedian, ShowDetails),
                Col_RetransmitMax => GetOverrideFlag(Col_RetransmitMax, ShowDetails),
                Col_Template => GetOverrideFlag(Col_Template, ShowDetails),
                Col_ConnectTime => GetOverrideFlag(Col_ConnectTime, ShowDetails),
                Col_DisconnectTime => GetOverrideFlag(Col_DisconnectTime, ShowDetails),
                Col_ResetTime => GetOverrideFlag(Col_ResetTime, ShowDetails),
                Col_ClientResetTime => GetOverrideFlag(Col_ClientResetTime, ShowDetails),
                Col_LastSentTime => GetOverrideFlag(Col_LastSentTime, ShowDetails),
                Col_LastReceivedTime => GetOverrideFlag(Col_LastReceivedTime, ShowDetails),
                Col_KeepAlive => GetOverrideFlag(Col_KeepAlive, ShowDetails),
                Col_MaxReceiveDelay => GetOverrideFlag(Col_MaxReceiveDelay, ShowDetails),
                Col_MaxSendDelay => GetOverrideFlag(Col_MaxSendDelay, ShowDetails),
                Col_StatBytesIn => GetOverrideFlag(Col_StatBytesIn, ShowStats),
                Col_StatBytesOut => GetOverrideFlag(Col_StatBytesOut, ShowStats),
                Col_StatSegmentsIn => GetOverrideFlag(Col_StatSegmentsIn, ShowStats),
                Col_StatSegmentsOut => GetOverrideFlag(Col_StatSegmentsOut, ShowStats),
                Col_TCB => GetOverrideFlag(Col_TCB, ShowDetails || IssueType == IssueTypes.Post ? true : false),
                Col_EventTime => GetOverrideFlag(Col_EventTime,  true),
                Col_PostMode => GetOverrideFlag(Col_PostMode, ShowStats || IssueType == IssueTypes.Post ? true : false),
                Col_SndNext => GetOverrideFlag(Col_SndNext, true),
                Col_Issue => GetOverrideFlag(Col_Issue, true),
                _ => throw new NotSupportedException($"Column {columnName} is not configurable."),
            };
            return lret;
        }

        /// <summary>
        /// Valid column names which can be enabled for more flexible output
        /// </summary>
        public static string[] ColumnNames =
        {
            Col_Connection,Col_ReceivedPackets,Col_StatSegmentsIn,Col_SentPackets,Col_StatSegmentsOut,Col_ReceivedBytes,Col_StatBytesIn,Col_SentBytes,Col_StatBytesOut,  
            Col_Total,Col_RetransmitCount,Col_RetransmitPercent, 
            Col_RetransmitDelay,Col_RetransmitMin,Col_RetransmitMedian,Col_RetransmitMax,
            Col_Template,Col_ConnectTime,Col_DisconnectTime,Col_ResetTime,Col_ClientResetTime,Col_LastSentTime, Col_LastReceivedTime,Col_KeepAlive, Col_MaxReceiveDelay, Col_MaxSendDelay,
            Col_TCB,
            Col_EventTime,Col_PostMode,Col_SndNext,Col_Issue,
        };

        private void PrintMatches(List<MatchData> data)
        {
            if( data.Count == 0 )
            {
                ColorConsole.WriteEmbeddedColorLine("[yellow]No matching TCP connections found.[/yellow]");
                return;
            }

            if( IssueType != IssueTypes.None )
            {
                PrintIssues(data);
                return;
            }

            var byFile = data.GroupBy(x => x.Session.FileName).OrderBy(x => x.First().Session.SessionStart);

            MatchData[] allPrinted = byFile.SelectMany(x => x.SortAscendingGetTopNLast(SortBy, x=>x.Connection.BytesReceived+x.Connection.BytesSent, null, TopN)).ToArray(); 

            int localIPLen = allPrinted.Max(x => x.Connection.LocalIpAndPort.ToString().Length);
            int remoteIPLen = allPrinted.Max(x => x.Connection.RemoteIpAndPort.ToString().Length);
            int tcpTemplateLen = allPrinted.Max(x => x.Connection.LastTcpTemplate?.Length) ?? 8;

            const int PacketCountWidth = 9;
            const int BytesCountWidth = 15;
            const int PercentWidth = 4;
            const int RetransMsWidth = 7;
            int timeWidth = GetWidth(TimeFormatOption);
            const int TotalColumnWidth = 22;
            const int KeepAliveWidth = 4; // true or no string 


            MultiLineFormatter formatter = new(
            new()
            {
                Title = "Source IP/Port -> Destination IP/Port",
                Name = Col_Connection,
                Enabled = GetEnable(Col_Connection),
                DataWidth = localIPLen + remoteIPLen + " -> ".Length + 1,
                Color = ConsoleColor.Yellow,
            }, new()
            {
                Title = "Received Packets",
                Name = Col_ReceivedPackets,
                Enabled = GetEnable(Col_ReceivedPackets),
                DataWidth = PacketCountWidth,
                Color = ConsoleColor.Green,
            }, new()
            {
                Title = "Stat PacketsIn",
                Name = Col_StatSegmentsIn,
                Enabled = GetEnable(Col_StatSegmentsIn),
                DataWidth = PacketCountWidth,
                Color = ConsoleColor.Green,
            }, new()
            {
                Title = "Sent Packets",
                Name = Col_SentPackets,
                Enabled = GetEnable(Col_SentPackets),
                DataWidth = PacketCountWidth,
                Color = ConsoleColor.Red,
            },  new()
            {
                Title = "Posted Packets",
                Name = Col_PostMode,
                Enabled = GetEnable(Col_PostMode),
                DataWidth = PacketCountWidth,
                Color = ConsoleColor.Yellow,
            }, new()
            {
                Title = "Injected Packets",
                Name = Col_PostMode,
                Enabled = GetEnable(Col_PostMode),
                DataWidth = PacketCountWidth,
                Color = ConsoleColor.Yellow,
            },
            new()
            {
                Title = "Stat PacketsOut",
                Name = Col_StatSegmentsOut,
                Enabled = GetEnable(Col_StatSegmentsOut),
                DataWidth = PacketCountWidth,
                Color = ConsoleColor.Red,
            }, new()
            {
                Title = "Received Bytes",
                Name = Col_ReceivedBytes,
                Enabled = GetEnable(Col_ReceivedBytes),
                DataWidth = BytesCountWidth + " B".Length,
                Color = ConsoleColor.Green,
            }, new()
            {
                Title = "Stat BytesIn",
                Name = Col_StatBytesIn,
                Enabled = GetEnable(Col_StatBytesIn),
                DataWidth = BytesCountWidth + " B".Length,
                Color = ConsoleColor.Green,
            }, new()
            {
                Title = "Sent Bytes",
                Name = Col_SentBytes,
                Enabled = GetEnable(Col_SentBytes),
                DataWidth = BytesCountWidth + " B".Length,
                Color = ConsoleColor.Red,
            }, new()
            {
                Title = "Stat BytesOut",
                Name = Col_StatBytesOut,
                Enabled = GetEnable(Col_StatBytesOut),
                DataWidth = BytesCountWidth + " B".Length,
                Color = ConsoleColor.Red,
            }, new()
            {
                Title = "Total" + ((SortOrder == SortOrders.TotalCount) ? " Packets" : (SortOrder == SortOrders.TotalSize) ? " Bytes" : ""),
                Name = Col_Total,
                Enabled = GetEnable(Col_Total),
                DataWidth = TotalColumnWidth,
                Color = ConsoleColor.Red,
            },
            new()
            {
                Title = "Retransmit Count",
                Name = Col_RetransmitCount,
                Enabled = GetEnable(Col_RetransmitCount),
                DataWidth = PacketCountWidth,
                Color = ConsoleColor.Yellow,
            }, new()
            {
                Title = "%",
                Name = Col_RetransmitPercent,
                Enabled = GetEnable(Col_RetransmitPercent),
                DataWidth = PercentWidth,
                Color = ConsoleColor.Yellow,
            }, new()
            {
                Title = "Delay ms",
                Name = Col_RetransmitDelay,
                Enabled = GetEnable(Col_RetransmitDelay),
                DataWidth = RetransMsWidth + " ms".Length,
                Color = ConsoleColor.Yellow,
            }, new()
            {
                Title = "Min ms",
                Name = Col_RetransmitMin,
                Enabled = GetEnable(Col_RetransmitMin),
                DataWidth = RetransMsWidth + " ms".Length,
                Color = ConsoleColor.Yellow,
            }, new()
            {
                Title = "Max ms",
                Name = Col_RetransmitMax,
                Enabled = GetEnable(Col_RetransmitMax),
                DataWidth = RetransMsWidth + " ms".Length,
                Color = ConsoleColor.Yellow,

            }, new()
            {
                Title = "Median ms",
                Name = Col_RetransmitMedian,
                Enabled = GetEnable(Col_RetransmitMedian),
                DataWidth = RetransMsWidth + " ms".Length,
                Color = ConsoleColor.Yellow,
            }, new()
            {
                Title = "Template",
                Name = Col_Template,
                Enabled = GetEnable(Col_Template),
                DataWidth = tcpTemplateLen + 2,
            }, new()
            {
                Title = "Connect Time",
                Name = Col_ConnectTime,
                Enabled = GetEnable(Col_ConnectTime),
                DataWidth = timeWidth + 1,
            }, new()
            {
                Title = "Disconnect Time",
                Name = Col_DisconnectTime,
                Enabled = GetEnable(Col_DisconnectTime),
                DataWidth = timeWidth + 1,
            }, new()
            {
                Title = "ConnectionReset Time",
                Name = Col_ResetTime,
                Enabled = GetEnable(Col_ResetTime),
                DataWidth = timeWidth + 1,
                Color = ConsoleColor.Yellow,
            }, new()
            {
                Title = "Client RST Time",
                Name = Col_ClientResetTime,
                Enabled = GetEnable(Col_ClientResetTime),
                DataWidth = timeWidth + 1,
                Color = ConsoleColor.Yellow,
            }, new()
            {
                Title = "LastSent Time",
                Name = Col_LastSentTime,
                Enabled = GetEnable(Col_LastSentTime),
                DataWidth = timeWidth + 1,
                Color = ConsoleColor.Red,

            }, new()
            {
                Title = "LastReceived Time",
                Name = Col_LastReceivedTime,
                Enabled = GetEnable(Col_LastReceivedTime),
                DataWidth = timeWidth + 1,
                Color = ConsoleColor.Green,
            }, new()
            {
                Title = "Keep Alive",
                Name = Col_KeepAlive,
                Enabled = GetEnable(Col_KeepAlive),
                DataWidth = KeepAliveWidth + 1,
                Color = ConsoleColor.Green,
            }, new()
            {
                Title = "MaxSendDelay",
                Name = Col_MaxSendDelay,
                Enabled = GetEnable(Col_MaxSendDelay),
                DataWidth = OverridenOrDefaultTimePrecision + 4 + 1, // 3 digits before . + space 
                Color = ConsoleColor.Red,
            }, new()
            {
                Title = "MaxReceiveDelay",
                Name = Col_MaxReceiveDelay,
                Enabled = GetEnable(Col_MaxReceiveDelay),
                DataWidth = OverridenOrDefaultTimePrecision + 4 + 1, // 3 digits before . + space 
                Color = ConsoleColor.Green,
            }, new()
            {
                Title = "TCB",
                Name = Col_TCB,
                Enabled = GetEnable(Col_TCB),
                DataWidth = PointerWidth + 3,
                Color = ConsoleColor.Green,
            }, new()
            {
                Title = "Process ",
                Color = ConsoleColor.Magenta,
            }, new()
            {
                Title = "Tags ",
                Color = ConsoleColor.Gray,
            }, new()
            {
                Title = "CmdLine",
                Color = ConsoleColor.DarkCyan,
            });

            formatter.PrintHeader();

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

                foreach (var match in file.SortAscendingGetTopNLast(SortBy, x => x.Connection.BytesReceived + x.Connection.BytesSent, null, TopN))
                {
                    totalDatagramsReceived += match.Connection.DatagramsReceived;
                    totalDatagramsSent += match.Connection.DatagramsSent;
                    totalBytesReceived += match.Connection.BytesReceived;
                    totalBytesSent += match.Connection.BytesSent;
                    totalRetransmissionsCount += match.Retransmissions.Count;
                    totalSumRetransDelay += match.Retransmissions.Sum(x => x.RetransmitDiff().TotalMilliseconds);
                    totalConnectCounter += match.InputConnectionCount;

                    string connection = $"{match.Connection.LocalIpAndPort.ToString().WithWidth(localIPLen)} -> {match.Connection.RemoteIpAndPort.ToString().WithWidth(remoteIPLen)}";

                    // retransmission % can only be calculated by sent packets and retransmission events excluding client retransmissions
                    string retransPercent = (100.0f * match.Retransmissions.Where(x => x.IsClientRetransmission.GetValueOrDefault() == false).Count() / match.Connection.DatagramsSent).ToString("N0");

                    // Delay on the other hand can be calculated by all Retransmit events.
                    string totalRetransDelay = match.Retransmissions.Sum(x => x.RetransmitDiff().TotalMilliseconds).ToString("N0");

                    string[] lineData = new string[]
                    {
                        connection,
                        match.Connection.DatagramsReceived.ToString("N0"),
                        match.Connection?.Statistics.SegmentsIn == null ? "" : match.Connection?.Statistics.SegmentsIn.Value.ToString("N0"),
                        match.Connection.DatagramsSent.ToString("N0"),
                        match.Connection?.Statistics.SendPostedPosted == null ? "" : match.Connection?.Statistics.SendPostedPosted.Value.ToString("N0"),
                        match.Connection?.Statistics.SendPostedInjected == null ? "" : match.Connection?.Statistics.SendPostedInjected.Value.ToString("N0"),
                        match.Connection?.Statistics.SegmentsOut == null ? "" : match.Connection?.Statistics.SegmentsOut.Value.ToString("N0"),
                        match.Connection.BytesReceived.ToString("N0") + " B",
                        match.Connection?.Statistics.DataBytesIn == null ? "" : match.Connection?.Statistics.DataBytesIn.Value.ToString("N0") + " B",
                        match.Connection.BytesSent.ToString("N0") + " B",
                        match.Connection?.Statistics.DataBytesOut == null ? "" : match.Connection?.Statistics.DataBytesOut.Value.ToString("N0") + " B",
                        GetTotalString(match, TotalColumnWidth),
                        match.Retransmissions.Count.ToString("N0"),
                        retransPercent,
                        totalRetransDelay,
                        match.RetransMinMs.ToString("F0") +" ms",
                        match.RetransMaxms.ToString("F0") +" ms",
                        match.RetransMedianMs.ToString("F0") +" ms",
                        (match.Connection.LastTcpTemplate ?? "-"),
                        GetDateTimeString(match.Connection.TimeStampOpen, match.Session.AdjustedSessionStart, TimeFormatOption, false),
                        GetDateTimeString(match.Connection.TimeStampClose, match.Session.AdjustedSessionStart, TimeFormatOption, false),
                        match.Connection.RetransmitTimeout != null ? GetDateTimeString(match.Connection.RetransmitTimeout, match.Session.AdjustedSessionStart, TimeFormatOption, false) : "",
                        match.Connection?.Statistics.RstReceivedTime != null ? GetDateTimeString( match.Connection.Statistics.RstReceivedTime, match.Session.AdjustedSessionStart, TimeFormatOption, false) : "",
                        match.Connection?.Statistics.LastSent != null ? GetDateTimeString(match.Connection.Statistics.LastSent, match.Session.AdjustedSessionStart, TimeFormatOption, false) : "",
                        match.Connection?.Statistics.LastReceived != null ?  GetDateTimeString(match.Connection.Statistics.LastReceived, match.Session.AdjustedSessionStart, TimeFormatOption, false) : "",
                        match.Connection?.Statistics.KeepAlive == null ? "" : match.Connection.Statistics.KeepAlive.Value.ToString(),
                        match.Connection?.Statistics.MaxReceiveDelayS == null ? "" : match.Connection?.Statistics.MaxReceiveDelayS.Value.ToString($"F{base.OverridenOrDefaultTimePrecision}"),
                        match.Connection?.Statistics.MaxSendDelayS == null ? "" : match.Connection?.Statistics.MaxSendDelayS.Value.ToString($"F{base.OverridenOrDefaultTimePrecision}"),
                        $"0x{"X".WidthFormat(match.Connection.Tcb, PointerWidth)}",
                        match.Process.GetProcessWithId(UsePrettyProcessName),
                        GetProcessTags(match.Process, match.Session.AdjustedSessionStart),
                        NoCmdLine ? "" : match.Process.CommandLineNoExe,
                    };

                    if (ShowTotal != TotalModes.Total)  // omit connection details in total mode
                    {
                        formatter.Print(true, lineData);
                    }
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
                    int emptyWidth = formatter.Columns[0].DataWidth+ 1; //hide the port data always
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

                    if (IsSummary && printedFiles > 1 && totalConnectCounter > 1)
                    {
                        ColorConsole.WriteEmbeddedColorLine(
                            $"{"N0".WidthFormat("", emptyWidth-8)}[red]Total's:[/red]" +
                            $"[green]{fileDatagramsReceived} {fileBytesReceived} Bytes [/green]" +
                            $"[red]{fileDatagramsSent} {fileBytesSent} Bytes [/red]" +
                            $"[cyan]{totalGetTotalString(totalTotalColumnWidth)}[/cyan]" +
                            $"[yellow]{fileRetransmissionsCount} {"N0".WidthFormat("", 5)}- {fileSumRetransDelay} ms[/yellow]" +
                            $"[red] Total Connection's accessed: [/red]" +
                            $"[cyan]{totalConnectCounter}[/cyan]")
                            ;
                    }
                }
            }
        }
        

        void PrintIssues(List<MatchData> data)
        {
            switch(IssueType)
            {
                case IssueTypes.Post:
                    PrintPostIssues(data);
                    break;
                default:
                    throw new NotSupportedException($"Issue type {IssueType} is not supported.");
            }   
        }


        private void PrintPostIssues(List<MatchData> data)
        {
            var byFile = data.GroupBy(x => x.Session.FileName).OrderBy(x => x.First().Session.SessionStart);
            MatchData[] allPrinted = byFile.SelectMany(x => x.SortAscendingGetTopNLast(SortBy, null, null, TopN)).ToArray();


            int localIPLen = allPrinted.Max(x => x.Connection.LocalIpAndPort.ToString().Length);
            int remoteIPLen = allPrinted.Max(x => x.Connection.RemoteIpAndPort.ToString().Length);

            int timeWidth = GetWidth(TimeFormatOption);
            const int PostModeWidth = 8;
            const int BytesWidth = 7;
            const int SndNextWidth = 13;


            MultiLineFormatter formatter = new(
              new()
              {
                  Title = "Source IP/Port -> Destination IP/Port",
                  Name = Col_Connection,
                  Enabled = GetEnable(Col_Connection),
                  DataWidth = localIPLen + remoteIPLen + " -> ".Length + 1,
                  Color = ConsoleColor.Yellow,
              },
              new()
              {
                  Title = "Connect Time",
                  Name = Col_ConnectTime,
                  Enabled = GetEnable(Col_ConnectTime), 
                  DataWidth = timeWidth
              },
              new()
              {
                  Title = "Disconnect Time",
                  Name = Col_DisconnectTime,
                  Enabled = GetEnable(Col_DisconnectTime),
                  DataWidth = timeWidth
              },
              new()
              {
                  Title = "Event Time",
                  Name = Col_EventTime, 
                  Enabled = GetEnable(Col_EventTime),
                  DataWidth = timeWidth
              },
              new()
              {
                  Title = "PostMode",
                  Name = Col_PostMode,
                  Enabled = GetEnable(Col_PostMode),
                  DataWidth = PostModeWidth
              },
              new()
              {
                  Title = "Sent Bytes",
                  Name = Col_SentBytes,
                  Enabled = GetEnable(Col_SentBytes),
                  DataWidth = BytesWidth
              },
              new()
              {
                  Title = "SndNext",
                  Name = Col_SndNext,
                  Enabled = GetEnable(Col_SndNext),
                  DataWidth = SndNextWidth
              },
              new()
              {
                Title = "TCB",
                Name = Col_TCB,
                Enabled = GetEnable(Col_TCB),
                DataWidth = PointerWidth + 3,
                Color = ConsoleColor.Green,
              },
              new()
              {
                  Title = "Issue",
                  Name = Col_Issue,
                  Enabled = GetEnable(Col_Issue), 
                  DataWidth = 53,
                  Color = ConsoleColor.Red,
              }
              );

            formatter.PrintHeader();

            foreach (var oneFile in byFile)
            {
                ColorConsole.WriteEmbeddedColorLine($"{oneFile.First().Session.SessionStart,-22} {GetPrintFileName(oneFile.Key)} {oneFile.First().Session.Baseline}", ConsoleColor.Cyan);

                foreach (var issue in oneFile.SortAscendingGetTopNLast(SortBy, null, null, TopN))
                {
                    string connection = $"{issue.Connection.LocalIpAndPort.ToString().WithWidth(localIPLen)} -> {issue.Connection.RemoteIpAndPort.ToString().WithWidth(remoteIPLen)}";
                    string[] lineData = new string[]
                    {
                        connection,
                        GetDateTimeString(issue.Connection.TimeStampOpen, issue.Session.AdjustedSessionStart, TimeFormatOption, false),
                        GetDateTimeString(issue.Connection.TimeStampClose, issue.Session.AdjustedSessionStart, TimeFormatOption, false),
                        GetDateTimeString(issue.Issue.PreviousPosted.Time, issue.Session.AdjustedSessionStart, TimeFormatOption, false),
                        issue.Issue.PreviousPosted.InjectedReason.ToString(),
                        $"{"N0".WidthFormat(issue.Issue.PreviousPosted.NumBytes, BytesWidth)}",
                        $"{"N0".WidthFormat(issue.Issue.PreviousPosted.SndNext, SndNextWidth)}",
                        $"0x{"X".WidthFormat(issue.Connection.Tcb, PointerWidth)}",
                        "Captured by Firewall",
                    };

                    if (ShowTotal != TotalModes.Total)  // omit connection details in total mode
                    {
                        formatter.Print(true, lineData);
                    }


                    lineData = new string[]
                    {
                        "",
                        "",
                        "",
                        GetDateTimeString(issue.Issue.OutOfOrderPost.Time, issue.Session.AdjustedSessionStart, TimeFormatOption, false),
                        issue.Issue.OutOfOrderPost.InjectedReason.ToString(),
                        $"{"N0".WidthFormat(issue.Issue.OutOfOrderPost.NumBytes, BytesWidth)}",
                        $"{"N0".WidthFormat(issue.Issue.OutOfOrderPost.SndNext, SndNextWidth)}",
                        "",
                        $"Posted Packet"
                    };

                    if (ShowTotal != TotalModes.Total)  // omit connection details in total mode
                    {
                        formatter.Print(true, lineData);
                    }

                    lineData = new string[]
                    {
                        "",
                        "",
                        "",
                        GetDateTimeString(issue.Issue.Injected.Time, issue.Session.AdjustedSessionStart, TimeFormatOption, false),
                        issue.Issue.Injected.InjectedReason.ToString(),
                        $"{"N0".WidthFormat(issue.Issue.Injected.NumBytes, BytesWidth)}",
                        $"{"N0".WidthFormat(issue.Issue.Injected.SndNext, SndNextWidth)}",
                        "",
                        $"Out of Post Order injection. SequenceNr Diff:  {issue.Issue.Injected.SndNext-issue.Issue.PreviousPosted.SndNext}"
                    };

                    if (ShowTotal != TotalModes.Total)  // omit connection details in total mode
                    {
                        formatter.Print(true, lineData);
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
                    if (file?.Extract?.Network?.TcpData?.Connections?.Count == null)
                    {
                        ColorConsole.WriteError($"Warning: File {GetPrintFileName(file.FileName)} does not contain Tcp data.");
                        continue;
                    }

                    var connections = file.Extract.Network.TcpData.Connections;
                    var retransByConnections = file.Extract.Network.TcpData.Retransmissions.ToLookup(x => file.Extract.Network.TcpData.Connections[(int)x.ConnectionIdx]);

                    if (IssueType == IssueTypes.None)
                    {
                        ReadConnectionData(lret, file, retransByConnections);
                    }
                    else
                    {
                        ReadIssueData(lret, file);
                    }

                }
            }

            return lret;
        }

        private void ReadIssueData(List<MatchData> lret, TestDataFile file)
        {
            for (int i = 0; i < file.Extract.Network.TcpData.TcpIssues.PostIssues.Count; i++)
            {
                ITcpPostIssue postIssue = file.Extract.Network.TcpData.TcpIssues.PostIssues[i];
                ITcpConnection connection = postIssue.GetConnection(file.Extract);

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

                if (IpPortFilter.Value?.Invoke(localIPAndPort.ToString() + remoteIPAndPort.ToString()) == false)
                {
                    continue;
                }

                if (KeepAliveFilter)
                {
                    if (connection?.Statistics.KeepAlive != true)
                    {
                        continue;
                    }
                }


                if (!MinMaxConnect.IsWithin(((connection.TimeStampOpen ?? file.Extract.SessionStart) - file.Extract.SessionStart).TotalSeconds))
                {
                    continue;
                }

                if (!MinMaxDisconnect.IsWithin(((connection.TimeStampClose ?? file.Extract.SessionStart) - file.Extract.SessionStart).TotalSeconds))
                {
                    continue;
                }

                MatchData data = new()
                {
                    Connection = connection,
                    Issue = postIssue,

                    Session = new ETWSession
                    {
                        FileName = file.FileName,
                        TestName = file.TestName,
                        TestDurationInMs = file.DurationInMs,
                        SessionStart = file.Extract.SessionStart,
                        Baseline = file?.Extract?.MainModuleVersion?.ToString() ?? "",
                        ZeroTimeS = GetZeroTimeInS(file.Extract),
                    },
                };

                lret.Add(data);
            }
        }

        private void ReadConnectionData(List<MatchData> lret, TestDataFile file, ILookup<ITcpConnection, ITcpRetransmission> retransByConnections)
        {
            for (int i = 0; i < file.Extract.Network.TcpData.Connections.Count; i++)
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

                if (IpPortFilter.Value?.Invoke(localIPAndPort.ToString() + remoteIPAndPort.ToString()) == false)
                {
                    continue;
                }

                if (KeepAliveFilter)
                {
                    if (connection?.Statistics.KeepAlive != true)
                    {
                        continue;
                    }
                }


                if (!MinMaxConnect.IsWithin(((connection.TimeStampOpen ?? file.Extract.SessionStart) - file.Extract.SessionStart).TotalSeconds))
                {
                    continue;
                }

                if (!MinMaxDisconnect.IsWithin(((connection.TimeStampClose ?? file.Extract.SessionStart) - file.Extract.SessionStart).TotalSeconds))
                {
                    continue;
                }

                if (!MinMaxReceivedS.IsWithin(((connection?.Statistics.LastReceived ?? file.Extract.SessionStart) - file.Extract.SessionStart).TotalSeconds))
                {
                    continue;
                }

                if (!MinMaxSentS.IsWithin(((connection?.Statistics.LastSent ?? file.Extract.SessionStart) - file.Extract.SessionStart).TotalSeconds))
                {
                    continue;
                }

                if (!MinMaxSentDelayS.IsWithin(connection?.Statistics.MaxSendDelayS ?? 0.0d))
                {
                    continue;
                }

                if (!MinMaxReceiveDelayS.IsWithin(connection?.Statistics.MaxReceiveDelayS ?? 0.0d))
                {
                    continue;
                }

                // filter by connection reset
                if (Reset && connection.RetransmitTimeout == null)
                {
                    continue;
                }



                if (TcbFilter.Value?.Invoke("0x" + connection.Tcb.ToString("X")) == false)
                {
                    continue;
                }

                if( !MinMaxInject.IsWithin(connection?.Statistics.SendPostedInjected ?? 0))
                {
                    continue;
                }

                
                if (!MinMaxPost.IsWithin(connection?.Statistics.SendPostedPosted ?? 0))
                {
                    continue;
                }


                if (!MinMaxReceivedBytes.IsWithin(connection.BytesReceived))
                {
                    continue;
                }

                if( !MinMaxBytes.IsWithin(connection.BytesReceived + connection.BytesSent))
                {
                    continue;
                }

                if ( !MinMaxReceivedPackets.IsWithin((ulong) connection.DatagramsReceived))
                {
                    continue;
                }

                if( !MinMaxSentPackets.IsWithin((ulong) connection.DatagramsSent))
                {
                    continue;
                }

                if( !MinMaxPackets.IsWithin((ulong)(connection.DatagramsReceived + connection.DatagramsSent)))
                {
                    continue;
                }

                // when a Statistics filter is active, make sure statistics are present which is only true for closed connections or already existing
                // ones where at trace start/end a TCP Rundown was performed.
                if (MinMaxStatPacketsOut != null || 
                    MinMaxStatBytesOut != null ||
                    MinMaxStatBytes != null ||
                    MinMaxStatPacketsIn != null || 
                    MinMaxStatPacketsOut != null ||
                    MinMaxStatPackets != null
                    )
                {
                    if( connection.Statistics.DataBytesIn == null ||
                        connection.Statistics.DataBytesOut == null ||
                        connection.Statistics.SegmentsIn == null ||
                        connection.Statistics.SegmentsOut == null)
                    {
                        continue;
                    }
                }

                // connection statiscs filters. When active they apply to closed connections or tcp connection rundown data when trace was stopped.
                if ( MinMaxStatBytesIn?.IsWithin( connection.Statistics.DataBytesIn.Value) == false ||
                    MinMaxStatBytesOut?.IsWithin( connection.Statistics.DataBytesOut.Value) == false ||
                    MinMaxStatBytes?.IsWithin( connection.Statistics.DataBytesIn.Value + connection.Statistics.DataBytesOut.Value) == false ||
                    MinMaxStatPacketsIn?.IsWithin( connection.Statistics.SegmentsIn.Value) == false ||
                    MinMaxStatPacketsOut?.IsWithin( connection.Statistics.SegmentsOut.Value) == false ||
                    MinMaxStatPackets?.IsWithin( connection.Statistics.SegmentsIn.Value + connection.Statistics.SegmentsOut.Value) == false )
                {
                    continue;
                }


                if(MinMaxClientResetS != null && !MinMaxClientResetS.IsWithin( ((connection?.Statistics?.RstReceivedTime ?? file.Extract.SessionStart) - file.Extract.SessionStart).TotalSeconds))
                {
                    continue;
                }

                if (!MinMaxSentBytes.IsWithin(connection.BytesSent))
                {
                    continue;
                }

                if (!MinMaxConnectionDurationFilter(connection.TimeStampOpen, connection.TimeStampClose, file.Extract.SessionEnd))
                {
                    continue;
                }

                var retransmissionsForConnection = retransByConnections[connection];

                foreach (var retransmission in retransmissionsForConnection)
                {
                    if (!MinMaxRetransDelayS.IsWithin(retransmission.RetransmitDiff().TotalSeconds))
                    {
                        continue;
                    }

                    if (!MinMaxRetransBytes.IsWithin(retransmission.NumBytes))
                    {
                        continue;
                    }

                    if (OnlyClientRetransmit) // only keep client retransmissions
                    {
                        if (retransmission.IsClientRetransmission == null || retransmission.IsClientRetransmission.Value == false)
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
                double minMs = retransMs.Count > 0 ? retransMs.Min() : 0.0d;
                double maxMs = retransMs.Count > 0 ? retransMs.Max() : 0.0d;

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

                lret.Add(data);

            }
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
                SortOrders.ClientResetTime => connection?.Statistics?.RstReceivedTime != null ? connection.Statistics.RstReceivedTime.Value.Ticks : 0m,
                SortOrders.RetransmissionCount => match.Retransmissions.Count,
                SortOrders.RetransmissionTime => (decimal) match.Retransmissions.Sum(x=>x.RetransmitDiff().TotalSeconds),
                SortOrders.MaxRetransmissionTime => (decimal) match.RetransMaxms,
                SortOrders.LastReceivedTime => connection?.Statistics.LastReceived != null ? connection.Statistics.LastReceived.Value.Ticks : 0m,
                SortOrders.LastSentTime => connection?.Statistics.LastSent != null ? connection.Statistics.LastSent.Value.Ticks : 0m,
                SortOrders.MaxReceiveDelay => connection?.Statistics.MaxReceiveDelayS != null ? (decimal) connection.Statistics.MaxReceiveDelayS.Value : 0m,
                SortOrders.MaxSendDelay => connection?.Statistics.MaxSendDelayS != null ? (decimal)connection.Statistics.MaxSendDelayS.Value : 0m,
                SortOrders.Post => connection?.Statistics.SendPostedPosted != null ? connection.Statistics.SendPostedPosted.Value : 0m,
                SortOrders.Inject => connection?.Statistics.SendPostedInjected != null ? connection.Statistics.SendPostedInjected.Value : 0m,
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
                SortOrders.TotalCount => (data.Connection.DatagramsReceived + data.Connection.DatagramsSent).ToString("N0"),
                SortOrders.TotalSize => (data.Connection.BytesReceived + data.Connection.BytesSent).ToString("N0")+ " Bytes",
                _ => "",
            };
        }

        internal bool MinMaxConnectionDurationFilter(DateTimeOffset? connectTime, DateTimeOffset? closeTime, DateTimeOffset sessionEnd)
        {
            bool lret = false;
            DateTimeOffset startTime = connectTime.HasValue ? connectTime.Value : DateTimeOffset.MinValue;
            DateTimeOffset endTime = closeTime.HasValue ? closeTime.Value : sessionEnd;
            
            lret = MinMaxConnectionDurationS.IsWithin((endTime - startTime).TotalSeconds);
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
            public ITcpPostIssue Issue { get; internal set; }
        }

        public class ETWSession
        {
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
