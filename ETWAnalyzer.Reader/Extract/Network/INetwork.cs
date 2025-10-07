//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract.Network.Tcp;

namespace ETWAnalyzer.Extract.Network
{
    /// <summary>
    /// Contains Network data such as DNS
    /// </summary>
    public interface INetwork
    {
        /// <summary>
        /// DNS Events
        /// </summary>
        IDnsClient DnsClient { get;  }

        /// <summary>
        /// 
        /// </summary>
        ITcpStatistics TcpData { get; }
    }
}