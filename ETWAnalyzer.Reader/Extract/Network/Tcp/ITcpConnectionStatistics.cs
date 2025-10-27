//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Extract.Network.Tcp
{
    /// <summary>
    /// Contains per connection statistics like last sent/received time, maximum delay between packets,
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
        public ulong? DataBytesIn { get; }

        /// <summary>
        /// From OS Connection Summary when connection was closed.  
        /// </summary>

        public ulong? DataBytesOut { get; }

        /// <summary>
        /// From OS Connection Summary when connection was closed.  
        /// </summary>

        public ulong? SegmentsIn { get; }

        /// <summary>
        /// From OS Connection Summary when connection was closed.  
        /// </summary>

        public ulong? SegmentsOut { get; }

        /// <summary>
        /// Count of send events which were posted to TCP send queue
        /// </summary>
        public ulong? SendPostedPosted { get;  }

        /// <summary>
        /// Count of send events which were injected by firewall to TCP send queue
        /// </summary>
        public ulong? SendPostedInjected { get;  }
    }
}