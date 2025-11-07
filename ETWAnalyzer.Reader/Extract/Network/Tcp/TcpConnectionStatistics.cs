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
        public TcpConnectionStatistics(DateTimeOffset? lastSent, DateTimeOffset? lastReceived, bool? keepAlive, double? maxSendDelayS, double? maxReceiveDelayS, 
              ulong? dataBytesIn, ulong? dataBytesOut, ulong? segmentsIn, ulong? segmentsOut, ulong? sendPostedPosted, ulong? sendPostedInjected, DateTimeOffset? rstReceivedTime)
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
        }
    }
}
