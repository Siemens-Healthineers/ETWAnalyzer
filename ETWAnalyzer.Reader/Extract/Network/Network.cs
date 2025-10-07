//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.Network.Tcp;

namespace ETWAnalyzer.Extract.Network
{
    /// <summary>
    /// Contains Network data such as DNS
    /// </summary>
    public class Network : INetwork
    {
        /// <summary>
        /// DNS Events
        /// </summary>
        public DnsClient DnsClient { get; set; } = new();

        /// <summary>
        /// DNS Events
        /// </summary>
        IDnsClient INetwork.DnsClient => DnsClient;

        /// <summary>
        /// TCP data
        /// </summary>
        public TcpStatistics TcpData { get; set; } = new();

        ITcpStatistics INetwork.TcpData => TcpData;
    }
}
