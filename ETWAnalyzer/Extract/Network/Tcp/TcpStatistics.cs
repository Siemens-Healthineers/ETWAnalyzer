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
    /// Contains data about all TCP connections and TCP Retransmissions which are a known source of problems in networks
    /// </summary>
    public class TcpStatistics : ITcpStatistics
    {
        /// <summary>
        /// Index to TcpStatistics.Connections array used by other classes to reference a TCP connection
        /// </summary>
        public enum ConnectionIdx
        {
            /// <summary>
            /// Invalid index
            /// </summary>
            Invalid = -1,
        }

        /// <summary>
        /// Connections
        /// </summary>
        public List<TcpConnection> Connections { get; set; } = new();

        Dictionary<TcpConnection, ConnectionIdx> myConnectionToIdx = new();

        internal ConnectionIdx AddConnection(TcpConnection connection)
        {
            Connections.Add(connection);
            ConnectionIdx lret = (ConnectionIdx)(Connections.Count - 1);
            myConnectionToIdx[connection] = lret;
            return lret;
        }

        internal ConnectionIdx GetConnectionIdx(TcpConnection connection)
        {
            return myConnectionToIdx[connection];
        }

        /// <summary>
        /// Retransmissions
        /// </summary>
        public List<TcpRetransmission> Retransmissions { get; set; } = new();

        IReadOnlyList<ITcpConnection> ITcpStatistics.Connections => Connections;
        IReadOnlyList<ITcpRetransmission> ITcpStatistics.Retransmissions => Retransmissions;
    }
}
