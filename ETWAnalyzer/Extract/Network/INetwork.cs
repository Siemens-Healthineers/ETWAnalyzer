//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


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
    }
}