//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Extract.Network.Tcp
{
    /// <summary>
    /// One TCP Retransmission event which is based on the TcpRetransmit and TcpTailLossProbe (id 1385) ETW event of Microsoft-Windows-TCPIP ETW provider.
    /// It uses other events like TcpDataSend to correlate the first send try time and how many bytes were tried to send.
    /// </summary>
    public interface ITcpRetransmission
    {
        /// <summary>
        /// Bytes which were tried to send. Windows get spurious retransmissions for 0 and 1 byte sized packets which look like ACK and keepalive pings.
        /// When <see cref="IsClientRetransmission"/> is true it is the number of received bytes.
        /// </summary>
        int NumBytes { get;  }

        /// <summary>
        /// Connection Index to retrieve connection later from TcpStatistics.Connections
        /// </summary>
        TcpStatistics.ConnectionIdx ConnectionIdx { get;  }
        /// <summary>
        /// Time when Retransmit did occur
        /// </summary>
        DateTimeOffset RetransmitTime { get;  }

        /// <summary>
        /// Time when packet was sent before retransmit was tried.
        /// </summary>
        DateTimeOffset SendTime { get;  }

        /// <summary>
        /// Sequence Number of TCP packet which is missing
        /// </summary>
        uint SequenceNumber { get;  }

        /// <summary>
        /// When true it indicates that retransmission was detected as duplicate client transmision which had a byte count > 1
        /// </summary>
        bool? IsClientRetransmission { get; }
    }
}