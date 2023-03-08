//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using System.Collections.Generic;

namespace ETWAnalyzer.Extract.Network.Tcp
{
    /// <summary>
    /// TCP metrics like connections, Retransmissions, ... 
    /// </summary>
    public interface ITcpStatistics
    {
        /// <summary>
        /// All active TCP connections
        /// </summary>
        IReadOnlyList<ITcpConnection> Connections { get;  }

        /// <summary>
        /// All TCP Retransmission events
        /// </summary>
        IReadOnlyList<ITcpRetransmission> Retransmissions { get;  }
    }
}