//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

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
        public DnsClient DnsClient { get; set; } = new DnsClient();

        /// <summary>
        /// DNS Events
        /// </summary>
        IDnsClient INetwork.DnsClient => DnsClient;
    }
}
