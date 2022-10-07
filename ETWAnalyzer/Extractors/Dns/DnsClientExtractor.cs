//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Network;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Events;
using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Collections.Generic;

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
        /// State object per DNS query to capature start/stop/timeouts for each query
        /// </summary>
        Dictionary<string, QueryState> myQueryState = new Dictionary<string, QueryState>();

        private const string PropertyQueryName = "QueryName";
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
            
            foreach(var ev in myGenericEvents.Result.Events)
            {
                if (ev.Process?.ImageName == null) // do not allow dead or exiting processes
                {
                    continue;
                }

                if ( ev.ProviderId == DnsClientETWConstants.Guid)
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

            myQueryState[ev.Fields[PropertyQueryName].AsString] = new QueryState
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
            string dnsQuery = ev.Fields[PropertyQueryName].AsString;
            if (myQueryState.TryGetValue(dnsQuery, out QueryState state))
            {
                state.Duration = ev.Timestamp.DateTimeOffset - state.Start;

                var dns = new DnsEvent()
                {
                    ProcessIdx = state.ProcessIndex,
                    Query = dnsQuery,
                    Result = ev.Fields[PropertyQueryResults].AsString,
                    QueryStatus = (int)ev.Fields[PropertyQueryStatus].AsUInt32,
                    Start = state.Start,
                    Duration = state.Duration,
                    ServerList = String.Join(";", state.DnsServerList),
                    TimedOut = state.TimedOut,
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
            if (myQueryState.TryGetValue(dnsQuery, out QueryState state))
            {
                state.DnsServerList.Add( ev.Fields[PropertyDnsServerIpAddress].AsString);
            }
        }
        
        /// <summary>
        /// Upon DNS query start by DNS service we get here the used network adapter name
        /// </summary>
        /// <param name="ev"></param>
        private void OnDnsServerQueryStart(IGenericEvent ev)
        {
            string dnsQuery = ev.Fields[PropertyQueryName].AsString;
            if (myQueryState.TryGetValue(dnsQuery, out QueryState state))
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
            if (myQueryState.TryGetValue(dnsQuery, out QueryState state))
            {
                state.TimedOut = true;
                state.DnsServer = ev.Fields[PropertyAddress].ToString();
            }
        }
    }
}
