//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.Network.Tcp
{
    /// <summary>
    /// Contains information about one TCP connection (Tcb, open/close time, src/dst ip and ports). Fired during inital handshake.
    /// Serialized to Json
    /// </summary>
    public class TcpConnection : ITcpConnection
    {
        /// <summary>
        /// Transmission Control Block kernel address which is used by other events are correlation identifier for a socket connection.
        /// </summary>
        public ulong Tcb { get; }

        /// <summary>
        /// Local IP and port
        /// </summary>
        public SocketConnection LocalIpAndPort { get; }

        /// <summary>
        /// Remote IP and port
        /// </summary>
        public SocketConnection RemoteIpAndPort { get; }

        /// <summary>
        /// When connection was created
        /// </summary>
        public DateTimeOffset? TimeStampOpen { get; }

        /// <summary>
        /// When connection as closed
        /// </summary>
        public DateTimeOffset? TimeStampClose { get; }


        /// <summary>
        /// Last applied TCP template which defines RTO and many other TCP parameters. 
        /// </summary>
        public string LastTcpTemplate { get; }

        /// <summary>
        /// Received data over duration of trace
        /// </summary>
        public ulong BytesReceived { get; }

        /// <summary>
        /// Number of received network packets
        /// </summary>
        public int DatagramsReceived { get; }

        /// <summary>
        /// Sent data over duration of trace
        /// </summary>
        public ulong BytesSent { get; }

        /// <summary>
        /// Number of sent datagrams
        /// </summary>
        public int DatagramsSent { get; }

        /// <summary>
        /// Index to <see cref="IETWExtract.Processes"/> array or Invalid if no matching process could be found.
        /// </summary>
        public ETWProcessIndex ProcessIdx { get; }

        /// <summary>
        /// Connection specific data
        /// </summary>
        public TcpConnectionStatistics Statistics { get; }

        /// <summary>
        /// Interface implementation
        /// </summary>
        ITcpConnectionStatistics ITcpConnection.Statistics => Statistics;

        /// <summary>
        /// Time when connection was closed due to retransmission timeout
        /// </summary>
        public DateTimeOffset? RetransmitTimeout { get; }

        /// <summary>
        /// Socket connect/disconnect time format string
        /// </summary>
        internal const string TimeFmt = "HH:mm:ss.fff";


        /// <summary>
        /// Create an instance which is used also by Json.NET to deserialize this object.
        /// </summary>
        /// <param name="tcb"></param>
        /// <param name="localipandPort"></param>
        /// <param name="remoteipAndPort"></param>
        /// <param name="timeStampOpen"></param>
        /// <param name="timeStampClose"></param>
        /// <param name="lastTcpTemplate"></param>
        /// <param name="bytesReceived"></param>
        /// <param name="bytesSent"></param>
        /// <param name="datagramsSent"></param>
        /// <param name="datagramsReceived"></param>
        /// <param name="processIdx"></param>
        /// <param name="retransmitTimeout"></param>
        /// <param name="statistics"></param>
        /// <exception cref="ArgumentNullException">When localipandPort or remoteipAndPort are null.</exception>
        public TcpConnection(ulong tcb, SocketConnection localipandPort, SocketConnection remoteipAndPort, DateTimeOffset? timeStampOpen, DateTimeOffset? timeStampClose, string lastTcpTemplate,
            ulong bytesSent, int datagramsSent,
            ulong bytesReceived, int datagramsReceived, ETWProcessIndex processIdx, DateTimeOffset? retransmitTimeout, TcpConnectionStatistics statistics)
        {
            Tcb = tcb;
            LocalIpAndPort = localipandPort ?? throw new ArgumentNullException(nameof(localipandPort));
            RemoteIpAndPort = remoteipAndPort ?? throw new ArgumentNullException(nameof(remoteipAndPort));
            TimeStampOpen = timeStampOpen;
            TimeStampClose = timeStampClose;
            LastTcpTemplate = lastTcpTemplate;
            BytesReceived = bytesReceived;
            BytesSent = bytesSent;
            DatagramsSent = datagramsSent;
            DatagramsReceived = datagramsReceived;
            ProcessIdx = processIdx;
            RetransmitTimeout = retransmitTimeout;
            Statistics = statistics;
        }


        /// <summary>
        /// Check if tcb value matches connection start/end times
        /// </summary>
        /// <param name="tcb">Transmission Control Block address</param>
        /// <param name="time"></param>
        /// <returns>ture if connection matches tcp value or false otherwise.</returns>
        public bool IsMatching(ulong tcb, DateTimeOffset time)
        {
            return tcb == Tcb &&
                (!TimeStampOpen.HasValue || TimeStampOpen.Value < time) &&
                (!TimeStampClose.HasValue || time < TimeStampClose.Value);
        }



        string FormatTime(DateTimeOffset? time)
        {
            return time == null ? "" : time.Value.ToString(TimeFmt);
        }

        /// <summary>
        /// Get debugging string representation
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Local: {LocalIpAndPort} Remote: {RemoteIpAndPort} Open: {FormatTime(TimeStampOpen)} Close: {FormatTime(TimeStampClose)}";
        }
    }
}
