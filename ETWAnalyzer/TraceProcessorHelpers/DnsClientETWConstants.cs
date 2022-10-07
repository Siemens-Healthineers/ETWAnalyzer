//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.TraceProcessorHelpers
{
    /// <summary>
    /// Key events which are logged by Microsoft-Windows-DNS-Client ETW Provider
    /// </summary>
    internal class DnsClientETWConstants
    {
        public const string ProviderName = "Microsoft-Windows-DNS-Client";
        public static readonly Guid Guid = new Guid("1c95126e-7eea-49a9-a3fe-a378b03ddb4d");

        /// <summary>
        /// DNS did not answer logged by DNS service
        /// Properties
        ///     QueryName       settings-win.data.microsoft.com
        ///     AddressLength   28
        ///     Address         1700003500000000FE8000000000000000000000000000010A000000
        /// </summary>
        public const int DnsServerTimeoutId = 1015;

        /// <summary>
        /// Process initiating DNS Query
        /// Properties
        ///     QueryName          cdn.xplosion.de
        ///     QueryType          28
        ///     QueryOptions       140738562252800
        ///     ServerList
        ///     IsNetworkQuery     0
        ///     NetworkQueryIndex  0
        ///     InterfaceIndex     0
        ///     IsAsyncQuery       0
        /// </summary>
        public const int DnsQueryClientStart = 3006;

        /// <summary>
        /// Process gets DNS Query results
        /// Properties
        ///     QueryName       cdn.xplosion.de
        ///     QueryType       28
        ///     QueryOptions    2392538375938048
        ///     QueryStatus     0 (Win32 return code)
        ///     QueryResults    2600:9000:20c3:3600:e:29d5:db00:93a1;2600:9000:20c3:cc00:e:29d5:db00:93a1;2600:9000:20c3:6800:e:29d5:db00:93a1;2600:9000:20c3:a200:e:29d5:db00:93a1;2600:9000:20c3:b600:e:29d5:db00:93a1;2600:9000:20c3:fc00:e:29d5:db00:93a1;2600:9000:20c3:c00:e:29d5:db00:93a1;2600:9000:20c3:aa00:e:29d5:db00:93a1;::ffff:18.66.192.123;::ffff:18.66.192.12;::ffff:18.66.192.73;::ffff:18.66.192.112;
        /// </summary>
        public const int DnsQueryClientCompleted = 3008;

        /// <summary>
        /// Query started by DNS Service which is later returned to client
        /// Properties
        /// 	 QueryName                  cdn.xplosion.de
        /// 	 IsParallelNetworkQuery     1
        /// 	 NetworkIndex               0
        /// 	 InterfaceCount             1
        /// 	 AdapterName                vEthernet (New Virtual Switch)
        /// 	 LocalAddress               2003:ee:b701:35e2:e440:4aab:776b:cdfa;2003:ee:b701:35e2:8147:7921:bcf2:2cb6;fe80::e440:4aab:776b:cdfa;192.168.2.103
        ///      DNSServerAddress           fe80::1;192.168.2.1
        /// </summary>
        public const int DnsQueryStarted = 3009;


        /// <summary>
        /// Start query for one DNS Server
        /// Properties
        ///         QueryName           settings-win.data.microsoft.com
        ///         QueryType           1
        ///         DnsServerIpAddress  192.169.0.1
        /// </summary>
        public const int DNSQueryOneDnsServer = 3010;

        /// <summary>
        /// DnsServerTimeout where given server did not respond to a DNS Query
        /// Properties
        ///     QueryName               string
        ///     AddressLength           int
        ///     Address                 bytes?
        /// </summary>
        public const int DnsServerTimeout = 1015;
    }
}
