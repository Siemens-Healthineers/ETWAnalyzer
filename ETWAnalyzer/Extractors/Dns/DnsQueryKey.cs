//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extractors.Dns
{
    internal class DnsQueryKey : IEquatable<DnsQueryKey>
    {
        public string DnsQuery { get; }
        public ETWProcessIndex Process { get; }

        public DnsQueryKey(string dnsQuery, ETWProcessIndex process)
        {
            DnsQuery = dnsQuery.ToLowerInvariant();
            Process = process;
        }

        public override int GetHashCode()
        {
            return DnsQuery.GetHashCode();
        }

        /// <summary>
        /// We group by query, but if in between the DNS Service queries on behalf of other processes we ignore the process to update the currently running query
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(DnsQueryKey other)
        {
            if (other == null)
            {
                return false;
            }

            return DnsQuery == other.DnsQuery && 
                ( (other.Process != ETWProcessIndex.Invalid && Process != ETWProcessIndex.Invalid && Process == other.Process) ||
                  (other.Process == ETWProcessIndex.Invalid || Process == ETWProcessIndex.Invalid )
                );

        }
    }
}
