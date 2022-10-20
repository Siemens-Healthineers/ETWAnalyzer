//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Network;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.Extractors.Dns
{
    /// <summary>
    /// Parse Dns client events of Microsoft-Windows-DNS-Client ETW Provider to extract
    /// * All Dns Requests
    /// * Time 
    /// * Duration 
    /// * Used network interface to check for slow and inefficient DNS queries
    /// </summary>
    internal class DnsClientExtractor : ExtractorBase
    {
        /// <summary>
        /// DNS queries are stored as generic events which we parse
        /// </summary>
        IPendingResult<IGenericEventDataSource> myGenericEvents;

        /// <summary>
        /// State object per DNS query and process to capture start/stop/timeouts for each query
        /// </summary>
        Dictionary<DnsQueryKey, QueryState> myQueryState = new Dictionary<DnsQueryKey, QueryState>();

        private const string PropertyQueryName = "QueryName";
        private const string PropertyQueryType = "QueryType";
        private const string PropertyAddress = "Address";
        private const string PropertyDnsServerIpAddress = "DnsServerIpAddress";
        private const string PropertyAdapterName = "AdapterName";
        private const string PropertyQueryResults = "QueryResults";
        private const string PropertyQueryStatus = "QueryStatus";

        public DnsClientExtractor()
        {
        }

        public override void RegisterParsers(ITraceProcessor processor)
        {
            myGenericEvents = processor.UseGenericEvents();
        }

        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            using var logger = new PerfLogger("Extract DnsClient");
            if( !myGenericEvents.HasResult )
            {
                return;
            }
            
            foreach(var ev in myGenericEvents.Result.Events.Where(IsValidDnsEvent).OrderBy(x=>x.Timestamp).ToArray())
            {
                switch(ev.Id)
                {
                    case DnsClientETWConstants.DnsQueryClientStart:
                        OnClientQueryStart(results, ev);
                        break;
                    case DnsClientETWConstants.DnsQueryClientCompleted:
                        OnClientQueryEnd(results, ev);
                        break;
                    case DnsClientETWConstants.DnsServerTimeout:
                        OnDnsServerTimeout(results, ev);
                        break;
                    case DnsClientETWConstants.DnsQueryStarted:
                        OnDnsServerQueryStart(ev);
                        break;
                    case DnsClientETWConstants.DNSQueryOneDnsServer:
                        OnQueryOneDnsServer(ev);
                        break;
                    default:
                        break;
                }
            }
        }

        private bool IsValidDnsEvent(IGenericEvent ev)
        {
            return ev.ProviderId == DnsClientETWConstants.Guid && ev.Process?.ImageName != null;
        }

        /// <summary>
        /// When a new start query arrives we log time and overwrite any old state.
        /// This also means that we do not properly capture concurrent DNS requests for the same DNS entry,
        /// but that should be minor. We keep "just" the last query if multiple queries are issued.
        /// </summary>
        /// <param name="results"></param>
        /// <param name="ev"></param>
        private void OnClientQueryStart(ETWExtract results, IGenericEvent ev)
        {
            ETWProcessIndex idx = ev.GetProcessIndex(results);

            DnsQueryKey key = new DnsQueryKey(ev.Fields[PropertyQueryName].AsString, idx);
            myQueryState[key] = new QueryState
            {
                Start = ev.Timestamp.DateTimeOffset,
                ProcessIndex = idx,
            };
        }

        /// <summary>
        /// DNS request in client process has ended. Calculate duration and write data to Extract.
        /// </summary>
        /// <param name="results"></param>
        /// <param name="ev"></param>
        private void OnClientQueryEnd(ETWExtract results, IGenericEvent ev)
        {
            ETWProcessIndex idx = ev.GetProcessIndex(results);
            DnsQueryKey key = new DnsQueryKey(ev.Fields[PropertyQueryName].AsString, idx);
            if (myQueryState.TryGetValue(key, out QueryState state))
            {
                state.Duration = ev.Timestamp.DateTimeOffset - state.Start;

                var dns = new DnsEvent()
                {
                    ProcessIdx = state.ProcessIndex,
                    Query = key.DnsQuery,
                    Result = ev.Fields[PropertyQueryResults].AsString,
                    QueryStatus = (int)ev.Fields[PropertyQueryStatus].AsUInt32,
                    Start = state.Start,
                    Duration = state.Duration,
                    ServerList = String.Join(";", state.DnsServerList),
                    TimedOut = state.TimedOut,
                    TimedOutServer = state.TimedOutServer,
                    Adapters = state.AdapterName,
                };

                results.Network.DnsClient.Events.Add(dns);
            }
        }

        /// <summary>
        /// DNS Service will issue one or multiple concurrent DNS queries for IPV4/6 networks on one or several network interfaces.
        /// </summary>
        /// <param name="ev"></param>
        private void OnQueryOneDnsServer(IGenericEvent ev)
        {
            string dnsQuery = ev.Fields[PropertyQueryName].AsString;
            DnsQueryKey key = new DnsQueryKey(dnsQuery, ETWProcessIndex.Invalid);
            if (myQueryState.TryGetValue(key, out QueryState state))
            {
                string server = ev.Fields[PropertyDnsServerIpAddress].AsString;
                if (!state.DnsServerList.Contains(server))
                {
                    state.DnsServerList.Add(server);
                }
            }
        }
        
        /// <summary>
        /// Upon DNS query start by DNS service we get here the used network adapter name
        /// </summary>
        /// <param name="ev"></param>
        private void OnDnsServerQueryStart(IGenericEvent ev)
        {
            string dnsQuery = ev.Fields[PropertyQueryName].AsString;
            DnsQueryKey key = new DnsQueryKey(dnsQuery, ETWProcessIndex.Invalid);
            if (myQueryState.TryGetValue(key, out QueryState state))
            {
                string adapterName = ev.Fields[PropertyAdapterName].AsString.Replace(";", "_");

                if ( !String.IsNullOrEmpty(state.AdapterName) )
                {
                    state.AdapterName += ";" + adapterName;
                }
                else
                {
                    state.AdapterName = adapterName;
                }
            }
        }

        /// <summary>
        /// If during the overall client query some DNS sub query did time out we want to know about here
        /// </summary>
        /// <param name="results"></param>
        /// <param name="ev"></param>
        private void OnDnsServerTimeout(ETWExtract results, IGenericEvent ev)
        {
            string dnsQuery = ev.Fields[PropertyQueryName].AsString;
            DnsQueryKey key = new DnsQueryKey(dnsQuery, ETWProcessIndex.Invalid);
            if (myQueryState.TryGetValue(key, out QueryState state))
            {
                state.TimedOut = true;
                string server = ev.Fields[PropertyAddress].ToString();
                if (state.TimedOutServer != null)
                {
                    if (state.TimedOutServer.IndexOf(server) == -1)
                    {
                        state.TimedOutServer += ";" + server;
                    }
                }
                else
                {
                    state.TimedOutServer = ev.Fields[PropertyAddress].ToString();
                }
            }
        }
    }
}
