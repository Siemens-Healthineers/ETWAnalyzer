//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ETWAnalyzer.Extract.Network.Tcp.TcpStatistics;

namespace ETWAnalyzer.Extract.Network.Tcp
{
    /// <summary>
    /// One TCP Retransmission event which is based on the TcpRetransmit and TcpTailLossProbe (id 1385) ETW event of Microsoft-Windows-TCPIP ETW provider.
    /// It uses other events like TcpDataSend to correlate the first send try time and how many bytes were tried to send.
    /// </summary>
    public class TcpRetransmission : ITcpRetransmission
    {
        /// <summary>
        /// Connection Index to retrieve connection later from TcpStatistics.Connections
        /// </summary>
        public ConnectionIdx ConnectionIdx { get; set; }

        /// <summary>
        /// Time when Retransmit did occur
        /// </summary>
        public DateTimeOffset RetransmitTime { get; set; }

        /// <summary>
        /// Time when packet was sent before retransmit was tried.
        /// </summary>
        public DateTimeOffset SendTime { get; set; }

        /// <summary>
        /// Sequence Number of TCP packet which is missing
        /// </summary>
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// Bytes which were tried to send. Windows get spurious retransmissions for 0 and 1 byte sized packets which look like ACK and keepalive pings.
        /// When <see cref="IsClientRetransmission"/> is true it is the number of received bytes.
        /// </summary>
        public int NumBytes { get; set; }

        /// <summary>
        /// When true it indicates that retransmission was detected as duplicate client transmision which had a byte count > 1
        /// </summary>
        public bool? IsClientRetransmission { get; set; }

        /// <summary>
        /// Create a new TcpRetransmission instance
        /// </summary>
        /// <param name="connectionIdx"></param>
        /// <param name="retransmitTime"></param>
        /// <param name="sendTime"></param>
        /// <param name="sequenceNumber"></param>
        /// <param name="numBytes">Number of bytes sent/received</param>
        /// <param name="isClientRetransmission"></param>
        public TcpRetransmission(ConnectionIdx connectionIdx, DateTimeOffset retransmitTime, DateTimeOffset sendTime, uint sequenceNumber, int numBytes, bool? isClientRetransmission)
        {
            ConnectionIdx = connectionIdx;
            RetransmitTime = retransmitTime;
            SendTime = sendTime;
            SequenceNumber = sequenceNumber;
            NumBytes = numBytes;
            IsClientRetransmission = isClientRetransmission;
        }

        /// <summary>
        /// Create a new TcpRetransmission instance
        /// </summary>
        /// <param name="connectionIdx"></param>
        /// <param name="retransmitTime"></param>
        /// <param name="sendTime"></param>
        /// <param name="sequenceNumber"></param>
        /// <param name="numBytes"></param>
        internal TcpRetransmission(ConnectionIdx connectionIdx, DateTimeOffset retransmitTime, DateTimeOffset sendTime, uint sequenceNumber, int numBytes)
                           :this(connectionIdx,retransmitTime,sendTime,sequenceNumber, numBytes, null)
        {
        }
    }
}
