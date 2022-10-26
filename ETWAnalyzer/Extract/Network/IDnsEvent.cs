//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Extract.Network
{
    /// <summary>
    /// Contains data about a process querying one host via one or several DNS server and/or interfaces
    /// </summary>
    public interface IDnsEvent
    {
        /// <summary>
        /// Process index which can be resolved via <see cref="IProcessExtract.GetProcess(ETWProcessIndex)"/>
        /// </summary>
        ETWProcessIndex ProcessIdx { get; }

        /// <summary>
        /// Name which was tried to resolve via DNS
        /// </summary>
        string Query { get; }

        /// <summary>
        /// Start Time
        /// </summary>
        DateTimeOffset Start { get; }

        /// <summary>
        /// Duration until query did finish
        /// </summary>
        TimeSpan Duration { get; }

        /// <summary>
        /// Returned resolved IP addresses
        /// </summary>
        string Result { get; }

        /// <summary>
        /// Used DNS servers
        /// </summary>
        string ServerList { get; }

        /// <summary>
        /// True if one or all queries did time out
        /// </summary>
        bool TimedOut { get; }

        /// <summary>
        /// Contains the servers which did time out separated by ;
        /// </summary>
        string TimedOutServer { get; }

        /// <summary>
        /// Used network adapter names for the query. This can be interesting if e.g. a network is first tried which has no route 
        /// to the queried host which will then time out.
        /// </summary>
        string Adapters { get; }

        /// <summary>
        /// Win32 return code DNS query. DNS specific return codes are in the range 9000-9999
        /// </summary>
        public int QueryStatus { get; }
    }
}