//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using System;
using System.Collections.Generic;

namespace ETWAnalyzer.Extract.Network
{
    /// <summary>
    /// Contains data about a process querying one host via one or several DNS server and/or interfaces
    /// </summary>
    public class DnsEvent : IDnsEvent
    {
        /// <summary>
        /// Process index which can be resolved via <see cref="IProcessExtract.GetProcess(ETWProcessIndex)"/>
        /// </summary>
        public ETWProcessIndex ProcessIdx { get; set; }

        /// <summary>
        /// This is set during querying and not part of extracted data
        /// </summary>
        internal ETWProcess Process { get; set; }

        /// <summary>
        /// Name which was tried to resolve via DNS
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Start Time
        /// </summary>
        public DateTimeOffset Start { get; set; }

        /// <summary>
        /// Duration until query did finish
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Returned resolved IP addresses
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// Used DNS servers
        /// </summary>
        public string ServerList { get; set; }

        /// <summary>
        /// True if one or all queries did time out
        /// </summary>
        public bool TimedOut { get;  set; }

        /// <summary>
        /// Used network adapter names for the query. This can be interesting if e.g. a network is first tried which has no route 
        /// to the queried host which will then time out.
        /// </summary>
        public string Adapters { get; set; }

        /// <summary>
        /// Win32 return code DNS query. DNS specific return codes are in the range 9000-9999
        /// </summary>
        public int QueryStatus { get; set; }


        static readonly char[] DnsResultSep = new char[] { ';' };

        internal string GetNonAliasResult()
        {

            // CNAME	5	RFC 1035[1]	Canonical name record	Alias of one name to another: the DNS lookup will continue by retrying the lookup with the new name.
            const string AliasRecord = "type:  5";

            List<string> nonAliasResults = new();

            if( Result != null)
            {
                string[] results = Result.Split(DnsResultSep, StringSplitOptions.RemoveEmptyEntries);
                foreach(var result in results)
                {
                    if( result.StartsWith(AliasRecord))
                    {
                        continue;
                    }

                    nonAliasResults.Add(result);
                }
            }

            string lret = String.Join(";", nonAliasResults);

            return lret;
        }
    }
}
