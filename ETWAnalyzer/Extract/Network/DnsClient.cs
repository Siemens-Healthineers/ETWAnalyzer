//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using System.Collections.Generic;

namespace ETWAnalyzer.Extract.Network
{
    /// <summary>
    /// 
    /// </summary>
    public class DnsClient : IDnsClient
    {
        /// <summary>
        /// DNS Events
        /// </summary>

        public List<DnsEvent> Events { get; set; } = new List<DnsEvent>();


        IReadOnlyList<IDnsEvent> IDnsClient.Events => Events;
    }
}
