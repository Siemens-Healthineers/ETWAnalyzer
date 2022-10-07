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
    internal class DnsClientExtractor : ExtractorBase
    {
        IPendingResult<IGenericEventDataSource> myGenericEvents;
        IPendingResult<IStackDataSource> myStackSource;

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
            NeedsSymbols = true;
            myGenericEvents = processor.UseGenericEvents();
            myStackSource = processor.UseStacks();
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

        private void OnQueryOneDnsServer(IGenericEvent ev)
        {
            string dnsQuery = ev.Fields[PropertyQueryName].AsString;
            if (myQueryState.TryGetValue(dnsQuery, out QueryState state))
            {
                state.DnsServerList.Add( ev.Fields[PropertyDnsServerIpAddress].AsString);
            }
        }

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

        private void OnDnsServerTimeout(ETWExtract results, IGenericEvent ev)
        {
            string dnsQuery = ev.Fields[PropertyQueryName].AsString;
            if (myQueryState.TryGetValue(dnsQuery, out QueryState state))
            {
                state.TimedOut = true;
                state.DnsServer = ev.Fields[PropertyAddress].ToString();
            }
        }

        private void OnClientQueryEnd(ETWExtract results, IGenericEvent ev)
        {
            string dnsQuery = ev.Fields[PropertyQueryName].AsString;
            if ( myQueryState.TryGetValue(dnsQuery, out QueryState state) )
            {
                state.Duration = ev.Timestamp.DateTimeOffset - state.Start;

                var dns = new DnsEvent()
                {
                    ProcessIdx = state.ProcessIndex,
                    Query = dnsQuery,
                    Result = ev.Fields[PropertyQueryResults].AsString,
                    QueryStatus = (int) ev.Fields[PropertyQueryStatus].AsUInt32,
                    Start = state.Start,
                    Duration = state.Duration,
                    ServerList = String.Join(";", state.DnsServerList),
                    TimedOut = state.TimedOut,
                    Adapters = state.AdapterName,
                };

                results.Network.DnsClient.Events.Add(dns);
            }
        }

        private void OnClientQueryStart(ETWExtract results, IGenericEvent ev)
        {
            ETWProcessIndex idx = ev.GetProcessIndex(results);

            myQueryState[ev.Fields[PropertyQueryName].AsString] = new QueryState
            {
                Start = ev.Timestamp.DateTimeOffset,
                ProcessIndex = idx,
            };
        }
    }
}
