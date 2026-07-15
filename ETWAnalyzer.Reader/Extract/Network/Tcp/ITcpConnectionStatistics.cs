//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Extract.Network.Tcp
{
    /// <summary>
    /// Contains per connection statistics like last sent/received time, maximum delay between packets and if connection was closed with a RST packet.
    /// </summary>
    public interface ITcpConnectionStatistics
    {
        /// <summary>
        /// TCP keep alive events were found. This will only be true for connections living long 
        /// enough after the TCP keepalive timer did fire.
        /// </summary>
        bool? KeepAlive { get; }

        /// <summary>
        /// Maximum time between two sent packets in seconds to determine how long the connection was idle, or 
        /// if some keep alive mechanism was active.
        /// </summary>
        double? MaxSendDelayS { get; }

        /// <summary>
        /// Maximum time between two received packets to determine how long the connection was idle in seconds.
        /// </summary>
        double? MaxReceiveDelayS { get; }

        /// <summary>
        /// Time when data was last sent
        /// </summary>
        DateTimeOffset? LastSent { get; }

        /// <summary>
        /// Time when data was last received
        /// </summary>
        DateTimeOffset? LastReceived { get; }

        /// <summary>
        /// From OS Connection Summary when connection was closed.  
        /// </summary>
        ulong? DataBytesIn { get; }

        /// <summary>
        /// From OS Connection Summary when connection was closed.  
        /// </summary>

        ulong? DataBytesOut { get; }

        /// <summary>
        /// From OS Connection Summary when connection was closed.  
        /// </summary>

        ulong? SegmentsIn { get; }

        /// <summary>
        /// From OS Connection Summary when connection was closed.  
        /// </summary>
        ulong? SegmentsOut { get; }

        /// <summary>
        /// Count of send events which were posted to TCP send queue
        /// </summary>
        ulong? SendPostedPosted { get;  }

        /// <summary>
        /// Count of send events which were injected by firewall to TCP send queue
        /// </summary>
        ulong? SendPostedInjected { get;  }

        /// <summary>
        /// Time when connection did receive a connection reset (RST) packet from client
        /// </summary>
        DateTimeOffset? RstReceivedTime { get; }

        /// <summary>
        /// Average send rate in bytes/second calculated from individual send events. Send events are grouped
        /// into bursts (windows) where consecutive send events are at most 350ms apart. For each burst the average
        /// send rate is calculated and then a weighted average across all bursts weighted by the sent bytes per burst is returned.
        /// This gives the effective transfer rate while data was actively sent excluding idle times between bursts.
        /// </summary>
        double? AverageSendRate { get; }

        /// <summary>
        /// Average receive rate in bytes/second calculated from individual receive events. Receive events are grouped
        /// into bursts (windows) where consecutive receive events are at most 350ms apart. For each burst the average
        /// receive rate is calculated and then a weighted average across all bursts weighted by the received bytes per burst is returned.
        /// This gives the effective transfer rate while data was actively received excluding idle times between bursts.
        /// </summary>
        double? AverageReceiveRate { get; }

        /// <summary>
        /// Aggregated send rate in bytes/second across all connections which share the same source IP:port and target IP
        /// address (regardless of the target port). Calculated during extraction from the combined send events of all these
        /// connections using the same burst based weighted average as <see cref="AverageSendRate"/>. Shown by -Dump TCP -GroupBy SourceIpPortRemoteIp.
        /// </summary>
        double? AggregatedSendRateBySourceIPPortTargetIP { get; }

        /// <summary>
        /// Aggregated receive rate in bytes/second across all connections which share the same source IP:port and target IP
        /// address (regardless of the target port). Shown by -Dump TCP -GroupBy SourceIpPortRemoteIp.
        /// </summary>
        double? AggregatedReceiveRateBySourceIPPortTargetIP { get; }

        /// <summary>
        /// Aggregated send rate in bytes/second across all connections which share the same source IP and target IP address
        /// (regardless of source and target port). Calculated during extraction from the combined send events of all these
        /// connections using the same burst based weighted average as <see cref="AverageSendRate"/>. Shown by -Dump TCP -GroupBy SourceIpRemoteIp.
        /// </summary>
        double? AggregatedSendRateBySourceIPTargetIP { get; }

        /// <summary>
        /// Aggregated receive rate in bytes/second across all connections which share the same source IP and target IP address
        /// (regardless of source and target port). Shown by -Dump TCP -GroupBy SourceIpRemoteIp.
        /// </summary>
        double? AggregatedReceiveRateBySourceIPTargetIP { get; }
    }
}