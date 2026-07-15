//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using System;

namespace ETWAnalyzer.Extract.Network.Tcp
{
    /// <summary>
    /// Contains per connection statistics like last sent/received time, maximum delay between packets,
    /// </summary>
    public class TcpConnectionStatistics : ITcpConnectionStatistics
    {
        /// <summary>
        /// TCP keep alive events were found. This will only be true for connections living long 
        /// enough after the TCP keepalive timer did fire.
        /// </summary>
        public bool? KeepAlive { get; internal set; }

        /// <summary>
        /// Maximum time between two sent packets in seconds to determine how long the connection was idle, or 
        /// if some keep alive mechanism was active.
        /// </summary>
        public double? MaxSendDelayS { get; internal set; }

        /// <summary>
        /// Maximum time between two received packets to determine how long the connection was idle in seconds.
        /// </summary>
        public double? MaxReceiveDelayS { get; internal set; }

        /// <summary>
        /// Time when data was last sent
        /// </summary>
        public DateTimeOffset? LastSent { get; internal set; }

        /// <summary>
        /// Time when data was last received
        /// </summary>
        public DateTimeOffset? LastReceived { get; }

        /// <summary>
        /// From OS Connection Summary when connection was closed.  
        /// </summary>
        public ulong? DataBytesIn { get; internal set; }

        /// <summary>
        /// From OS Connection Summary when connection was closed.  
        /// </summary>

        public ulong? DataBytesOut { get; internal set; }

        /// <summary>
        /// From OS Connection Summary when connection was closed.  
        /// </summary>

        public ulong? SegmentsIn { get; internal set; }

        /// <summary>
        /// From OS Connection Summary when connection was closed.  
        /// </summary>

        public ulong? SegmentsOut { get; internal set; }

        /// <summary>
        /// Send events which were posted to TCP send queue
        /// </summary>
        public ulong? SendPostedPosted { get; internal set; }

        /// <summary>
        /// Send events which were injected by firewall to TCP send queue
        /// </summary>
        public ulong? SendPostedInjected {  get; internal set; }

        /// <summary>
        /// Time when connection did receive a connection reset (RST) packet from client
        /// </summary>
        public DateTimeOffset? RstReceivedTime { get; internal set; }

        /// <summary>
        /// Average send rate in bytes/second calculated from individual send events. Send events are grouped
        /// into bursts (windows) where consecutive send events are at most 350ms apart. For each burst the average
        /// send rate is calculated and then a weighted average across all bursts weighted by the sent bytes per burst is returned.
        /// This gives the effective transfer rate while data was actively sent excluding idle times between bursts.
        /// </summary>
        public double? AverageSendRate { get; internal set; }

        /// <summary>
        /// Average receive rate in bytes/second calculated from individual receive events. Receive events are grouped
        /// into bursts (windows) where consecutive receive events are at most 350ms apart. For each burst the average
        /// receive rate is calculated and then a weighted average across all bursts weighted by the received bytes per burst is returned.
        /// This gives the effective transfer rate while data was actively received excluding idle times between bursts.
        /// </summary>
        public double? AverageReceiveRate { get; internal set; }

        /// <summary>
        /// Aggregated send rate in bytes/second across all connections which share the same source IP:port and target IP
        /// address (regardless of the target port). Shown by -Dump TCP -GroupBy SourceIpPortRemoteIp.
        /// </summary>
        public double? AggregatedSendRateBySourceIPPortTargetIP { get; internal set; }

        /// <summary>
        /// Aggregated receive rate in bytes/second across all connections which share the same source IP:port and target IP
        /// address (regardless of the target port). Shown by -Dump TCP -GroupBy SourceIpPortRemoteIp.
        /// </summary>
        public double? AggregatedReceiveRateBySourceIPPortTargetIP { get; internal set; }

        /// <summary>
        /// Aggregated send rate in bytes/second across all connections which share the same source IP and target IP address
        /// (regardless of source and target port). Shown by -Dump TCP -GroupBy SourceIpRemoteIp.
        /// </summary>
        public double? AggregatedSendRateBySourceIPTargetIP { get; internal set; }

        /// <summary>
        /// Aggregated receive rate in bytes/second across all connections which share the same source IP and target IP address
        /// (regardless of source and target port). Shown by -Dump TCP -GroupBy SourceIpRemoteIp.
        /// </summary>
        public double? AggregatedReceiveRateBySourceIPTargetIP { get; internal set; }

        /// <summary>
        /// Used by Json Serializer to create a new instance of this class.
        /// Each input value name must match the property name case insensitive, or we will get some properties not deserialized. 
        /// </summary>
        /// <param name="lastSent"></param>
        /// <param name="lastReceived"></param>
        /// <param name="keepAlive"></param>
        /// <param name="maxSendDelayS"></param>
        /// <param name="maxReceiveDelayS"></param>
        /// <param name="dataBytesIn"></param>
        /// <param name="dataBytesOut"></param>
        /// <param name="segmentsIn"></param>
        /// <param name="segmentsOut"></param>
        /// <param name="sendPostedInjected"></param>
        /// <param name="sendPostedPosted"></param>
        /// <param name="rstReceivedTime"></param>
        /// <param name="averageSendRate"></param>
        /// <param name="averageReceiveRate"></param>
        /// <param name="aggregatedSendRateBySourceIPPortTargetIP"></param>
        /// <param name="aggregatedReceiveRateBySourceIPPortTargetIP"></param>
        /// <param name="aggregatedSendRateBySourceIPTargetIP"></param>
        /// <param name="aggregatedReceiveRateBySourceIPTargetIP"></param>
        public TcpConnectionStatistics(DateTimeOffset? lastSent, DateTimeOffset? lastReceived, bool? keepAlive, double? maxSendDelayS, double? maxReceiveDelayS, 
              ulong? dataBytesIn, ulong? dataBytesOut, ulong? segmentsIn, ulong? segmentsOut, ulong? sendPostedPosted, ulong? sendPostedInjected, DateTimeOffset? rstReceivedTime,
              double? averageSendRate = null, double? averageReceiveRate = null,
              double? aggregatedSendRateBySourceIPPortTargetIP = null, double? aggregatedReceiveRateBySourceIPPortTargetIP = null,
              double? aggregatedSendRateBySourceIPTargetIP = null, double? aggregatedReceiveRateBySourceIPTargetIP = null)
        {
            LastSent = lastSent;
            LastReceived = lastReceived;
            KeepAlive = keepAlive;
            MaxSendDelayS = maxSendDelayS;
            MaxReceiveDelayS = maxReceiveDelayS;
            DataBytesIn = dataBytesIn;
            DataBytesOut = dataBytesOut;
            SegmentsIn = segmentsIn;
            SegmentsOut = segmentsOut;
            SendPostedPosted = sendPostedPosted;
            SendPostedInjected = sendPostedInjected;
            RstReceivedTime = rstReceivedTime;
            AverageSendRate = averageSendRate;
            AverageReceiveRate = averageReceiveRate;
            AggregatedSendRateBySourceIPPortTargetIP = aggregatedSendRateBySourceIPPortTargetIP;
            AggregatedReceiveRateBySourceIPPortTargetIP = aggregatedReceiveRateBySourceIPPortTargetIP;
            AggregatedSendRateBySourceIPTargetIP = aggregatedSendRateBySourceIPTargetIP;
            AggregatedReceiveRateBySourceIPTargetIP = aggregatedReceiveRateBySourceIPTargetIP;
        }
    }
}
