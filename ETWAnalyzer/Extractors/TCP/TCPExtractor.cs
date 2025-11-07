//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Network.Tcp;
using ETWAnalyzer.Extract.Network.Tcp.Issues;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using static ETWAnalyzer.Extract.Network.Tcp.TcpStatistics;

namespace ETWAnalyzer.Extractors.TCP
{
    internal class TCPExtractor : ExtractorBase
    {
        /// <summary>
        /// TCP data is stored as generic events which we parse
        /// </summary>
        IPendingResult<IGenericEventDataSource> myGenericEvents;

        /// <summary>
        /// TCP Post events
        /// </summary>
        readonly List<TcpSendPosted> mySendPostedEvents = new();

        /// <summary>
        /// TCP Send events
        /// </summary>
        readonly List<TcpDataTransferSend> myTcpDataSendEvents = new();

        /// <summary>
        /// Receive events
        /// </summary>
        readonly List<TcpDataTransferReceive> myReceiveEvents = new();

        /// <summary>
        /// Retransmit events
        /// </summary>
        readonly List<TcpRetransmit> myRetransmits = new();

        /// <summary>
        /// Connection requests
        /// </summary>
        readonly List<TcpRequestConnect> myConnections = new();

        public TCPExtractor()
        {
        }

        public override void RegisterParsers(ITraceProcessor processor)
        {
            myGenericEvents = processor.UseGenericEvents();
        }

        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            using var logger = new PerfLogger("Extract TCPExtractor");
            if (!myGenericEvents.HasResult)
            {
                return;
            }

            IGenericEvent[] events = myGenericEvents.Result.Events.Where(IsValidTcpEvent).OrderBy(x => x.Timestamp).ToArray();
            ExtractFromGenericEvents(results, events);

            ReleaseMemory();

        }

        /// <summary>
        /// Used for debugging a specific connection
        /// </summary>
        /// <param name="tcb"></param>
        /// <returns></returns>
        bool TCBFilter(ulong tcb)
        {
#if DEBUG
            return true;
#else
            return true;
#endif
        }


        internal void ExtractFromGenericEvents(ETWExtract results, IGenericEvent[] events)
        {
            ConvertTcpEventsFromGenericEvents(results, events);

            ILookup<ulong, TcpRequestConnect> connectionsByTcb = myConnections.ToLookup(x => x.Tcb);  // Get new connection from connection attempts

            AddExistingConnections(results, ref connectionsByTcb); // add already existing connections which are present during trace start

            UpdateConnectionResetTimeStampSentFromClient();

            (ILookup<TcpRequestConnect, TcpSendPosted>, ILookup < TcpRequestConnect, TcpDataTransferSend >) sentByConnection = UpdateConnectionForSentAndPostedPackets(results, ref connectionsByTcb);
            ILookup<TcpRequestConnect, TcpDataTransferReceive> receivedByConnection = UpdateConnectionForReceivedPackets(results, ref connectionsByTcb);
            ILookup<TcpRequestConnect, TcpTemplateChanged> byConnectionTemplateChanges = UpdateConnectionForTemplateChangeEvents(results, ref connectionsByTcb); 

            Dictionary<TcpRequestConnect, ConnectionIdx> connect2Idx = new();

            // Store connections in ETWExtract
            foreach (TcpRequestConnect tcpconnection in connectionsByTcb.SelectMany(x => x).OrderBy(x => x.RemoteIpAndPort.Address))
            {
                List<string> templates = new();
                foreach (var change in byConnectionTemplateChanges[tcpconnection].OrderBy(x => x.Timestamp))
                {
                    templates.Add(change.TemplateType.ToString());
                }

                ulong bytesReceived = (ulong)receivedByConnection[tcpconnection].Sum(x => (decimal)((TcpDataTransferReceive)x).NumBytes);
                var lastreceivedTime = receivedByConnection[tcpconnection].LastOrDefault()?.Timestamp;

                ulong bytesSent = (ulong)sentByConnection.Item1[tcpconnection].Sum(x => (decimal)((TcpSendPosted)x).PostedBytes);
                int datagramsReceived = receivedByConnection[tcpconnection].Count();
                int datagramsSent = sentByConnection.Item2[tcpconnection].Count();
                int datagramsPosted = sentByConnection.Item1[tcpconnection].Where(x=>x.IsPosted).Count();
                int datagramSentCorrected = Math.Max(datagramsSent, datagramsPosted); // in case of port forwarding we can see way too less send events, in that case use Posted count

                TcpConnectionStatistics statistics = new TcpConnectionStatistics(null, lastreceivedTime, null, null, null, null, null, null, null, null, null, tcpconnection.ResetTimeStamp);

                TcpConnection connection = new(tcpconnection.Tcb, tcpconnection.LocalIpAndPort, tcpconnection.RemoteIpAndPort, tcpconnection.TimeStampOpen, tcpconnection.TimeStampClose,
                    templates.LastOrDefault(), bytesSent, datagramSentCorrected, bytesReceived, datagramsReceived, tcpconnection.ProcessIdx, tcpconnection.TCPRetansmitTimeout, statistics);
                ConnectionIdx connIdx = results.Network.TcpData.AddConnection(connection);
                connect2Idx[tcpconnection] = connIdx;


                // Detect client side retransmissions which can be identified by packets > 1 bytes 
                // and which have the same sequence number. 
                // Todo: Sequence rollover is currently handled by a 3s timeout filter which should work up to 10 GBit uplinks before
                // sequence rollover kicks in.
                foreach (var clientRetransmissions in receivedByConnection[tcpconnection].Where(x => x.NumBytes > 1).ToLookup(x => x.SequenceNr).Where(x => x.Count() > 1))
                {
                    var candidates = clientRetransmissions.OrderBy(x => x.Timestamp).ToArray();
                    DateTimeOffset first = candidates[0].Timestamp;
                    foreach (var retransCandidate in candidates.Skip(1))
                    {
                        TimeSpan diff = retransCandidate.Timestamp - first;
                        if (diff.TotalSeconds < 3)
                        {
                            var clientRetrans = new TcpRetransmission(connIdx, retransCandidate.Timestamp, first, retransCandidate.SequenceNr, retransCandidate.NumBytes, true);
                            results.Network.TcpData.Retransmissions.Add(clientRetrans);
                        }
                    }
                }
            }

            foreach (var retrans in myRetransmits)
            {
                retrans.Connection = LocateConnection(retrans.Tcb, retrans.Timestamp, results, ref connectionsByTcb);
                if (retrans.Connection == null)
                {
                    continue;
                }

                // Get all sent packets just before the current retransmit event
                // we need the last sent packet because the first packet often was an ACK with 0 send size
                List<TcpDataTransferSend> retransmittedSent = (sentByConnection.Item2[retrans.Connection]).Where(x => x.SequenceNr == retrans.SndUna && x.Timestamp <= retrans.Timestamp)
                                                    .OrderBy(x => x.Timestamp).ToList();

                if (retransmittedSent.Count > 0)
                {
                    var lastSent = retransmittedSent.Last(); // this is the send which caused later retransmit events
                    // The application can issue further send events without canceling the current pending retransmissions

                    // store retransmit event with send time of first sent packet. It might be an ACK packet but for now the heuristics should be good enough
                    // to be useful.
                    results.Network.TcpData.Retransmissions.Add(new TcpRetransmission(connect2Idx[retrans.Connection], retrans.Timestamp,
                        lastSent.Timestamp, lastSent.SequenceNr, (int) lastSent.BytesSent));

                    // set last send time before retransmissions did start
                    TcpConnection connection = results.Network.TcpData.Connections[(int)connect2Idx[retrans.Connection]];
                    if (connection.RetransmitTimeout != null) // connection was closed due to retransmit
                    {
                        connection.Statistics.LastSent = retransmittedSent.Last().Timestamp; // last send event 
                    }
                }
            }

            UpdateLastSentAndMaxSendDelay(results, sentByConnection);
            UpdateMaxReceiveDelay(results, receivedByConnection);
            UpdateKeepAliveStatistics(results);
            UpdatePostedStatistics(results, sentByConnection.Item1);
            DetectSendOrderIssues(results, sentByConnection, connect2Idx);
            AddConnectionStatistics(results);
        }

         private ILookup<TcpRequestConnect, TcpTemplateChanged> UpdateConnectionForTemplateChangeEvents(ETWExtract results, ref ILookup<ulong, TcpRequestConnect> connectionsByTcb)
        {
            foreach (TcpTemplateChanged templateChange in myTemplateChangedEvents)
            {
                templateChange.Connection = LocateConnection(templateChange.Tcb, templateChange.Timestamp, results, ref connectionsByTcb);
            }

            return myTemplateChangedEvents.ToLookup(x => x.Connection); ;
        }

        private ILookup<TcpRequestConnect, TcpDataTransferReceive> UpdateConnectionForReceivedPackets(ETWExtract results, ref ILookup<ulong, TcpRequestConnect> connectionsByTcb)
        {
            foreach (TcpDataTransferReceive receive in myReceiveEvents)
            {
                receive.Connection = LocateConnection(receive.Tcb, receive.Timestamp, results, ref connectionsByTcb);
            }

            return myReceiveEvents.ToLookup(x=>x.Connection);
        }

        private (ILookup<TcpRequestConnect, TcpSendPosted>, ILookup<TcpRequestConnect, TcpDataTransferSend>) UpdateConnectionForSentAndPostedPackets(ETWExtract results,ref ILookup<ulong, TcpRequestConnect> connectionsByTcb)
        {
            foreach (TcpSendPosted send in mySendPostedEvents)
            {
                send.Connection = LocateConnection(send.Tcb, send.Timestamp, results, ref connectionsByTcb);
            }

            foreach(TcpDataTransferSend send in myTcpDataSendEvents)
            {
                send.Connection = LocateConnection(send.Tcb, send.Timestamp, results, ref connectionsByTcb);
            }

            ILookup<TcpRequestConnect, TcpSendPosted> lret =  mySendPostedEvents.Where(x => x.Connection != null).ToLookup(x => x.Connection);
            ILookup<TcpRequestConnect, TcpDataTransferSend> sends = myTcpDataSendEvents.Where(x=> x.Connection != null).ToLookup(x => x.Connection);
            return (lret, sends);
        }

        private void UpdatePostedStatistics(ETWExtract results, ILookup<TcpRequestConnect, TcpSendPosted> tcpSendPosted)
        {
            foreach (IGrouping<TcpRequestConnect, TcpSendPosted> posted in tcpSendPosted)
            {
                TcpSendPosted firstposted = posted.First();
                foreach (var resultConnection in results.Network.TcpData.Connections)
                {
                    if (resultConnection.IsMatching(firstposted.Tcb, firstposted.Timestamp) &&
                        resultConnection.Statistics.SendPostedInjected == null)
                    {
                        int postedCount = posted.Where(x => x.IsPosted).Count();
                        int injectedCount = posted.Where(x => x.IsInjected).Count();

                        resultConnection.Statistics.SendPostedInjected = (ulong) injectedCount;
                        resultConnection.Statistics.SendPostedPosted = (ulong) postedCount;
                    }
                }
            }
        }

        private void UpdateLastSentAndMaxSendDelay(ETWExtract results, (ILookup<TcpRequestConnect, TcpSendPosted>, ILookup<TcpRequestConnect, TcpDataTransferSend>) sentByConnection)
        {
            // update last sent time for all connections which are not already covered by retransmits
            foreach (IGrouping<TcpRequestConnect, TcpDataTransferSend> sent in sentByConnection.Item2)
            {
                TcpDataTransferSend firstSent = sent.First();
                foreach (var resultConnections in results.Network.TcpData.Connections)
                {
                    if (resultConnections.Statistics.LastSent == null &&
                        resultConnections.IsMatching(firstSent.Tcb, firstSent.Timestamp))
                    {
                        resultConnections.Statistics.LastSent = sent.Last().Timestamp;
                        resultConnections.Statistics.MaxSendDelayS = GetMaxDelayBetweenPackets(sent);
                        break;
                    }
                }
            }
        }

        private void UpdateMaxReceiveDelay(ETWExtract results, ILookup<TcpRequestConnect, TcpDataTransferReceive> receivedByConnection)
        {
            // Update MaxReceivedDelay per connection 
            foreach (IGrouping<TcpRequestConnect, TcpDataTransferReceive> received in receivedByConnection)
            {
                var firstReceived = received.First();
                var conn = Find(firstReceived.Tcb, firstReceived.Timestamp, results);
                if (conn != null)
                {
                    conn.Statistics.MaxReceiveDelayS = GetMaxDelayBetweenPackets(received);
                }
            }
        }

        private void UpdateKeepAliveStatistics(ETWExtract results)
        {
            // Update KeepAlive data per connection 
            foreach (var keepalive in myKeepAlives)
            {
                var conn = Find(keepalive.Tcb, keepalive.Timestamp, results);
                if (conn != null)
                {
                    conn.Statistics.KeepAlive = true;
                }
            }
        }

        /// <summary>
        /// Connection summaries are logged when a connection is closed, or when a TCP connection rundown is done. But during rundown
        /// we sometimes get lost events so we need to deal with potential missing events.
        /// We first try to find the connection close event which should contain accurate data for the connection lifetime after connection was closed.
        /// If the connection was not closed we try to use the summary event which was logged normally during trace start. 
        /// </summary>
        /// <param name="results"></param>
        private void AddConnectionStatistics(ETWExtract results)
        {
            var allConnections = results.Network.TcpData.Connections.ToLookup(x => x.Tcb);

            // Update per connection summaries from ETW summary events
            foreach (var summary in myTcpConnectionSummaries)
            {
                var allTcbConnections = allConnections[summary.Tcb].OrderBy(x => x.TimeStampClose).ToList();
                bool bFound = false;
                foreach (var connection in allTcbConnections)
                {
                    if (summary.Timestamp > connection.TimeStampClose) // Connection summary is logged after connection was closed
                    {
                        bFound = true;
                        connection.Statistics.DataBytesIn = summary.DataBytesIn;
                        connection.Statistics.DataBytesOut = summary.DataBytesOut;
                        connection.Statistics.SegmentsIn = summary.DataSegmentsIn;
                        connection.Statistics.SegmentsOut = summary.DataSegmentsOut;
                        break;
                    }
                }

                if (!bFound && allTcbConnections.Count == 1) // connection was not closed so add last stats
                {
                    allTcbConnections[0].Statistics.DataBytesIn = summary.DataBytesIn;
                    allTcbConnections[0].Statistics.DataBytesOut = summary.DataBytesOut;
                    allTcbConnections[0].Statistics.SegmentsIn = summary.SegmentsIn;
                    allTcbConnections[0].Statistics.SegmentsOut = summary.SegmentsOut;
                }
            }
        }

        /// <summary>
        /// We can have multiple rundown calls for the same TCB. Only consider one.
        /// Additionally when TCP Port Sharing is enabled
        /// we get connections with different TCB values for 
        ///      src:ports -> dst:portd  and 
        ///      dst:portd -> src:ports 
        /// where for localhost connections src and dst IP are identical.
        /// </summary>
        /// <param name="results"></param>
        /// <param name="connectionsByTcb"></param>
        private void AddExistingConnections(ETWExtract results, ref ILookup<ulong, TcpRequestConnect> connectionsByTcb)
        {
            List<TcpRequestConnect> notyetAdded = new();

            foreach (IGrouping<ulong, TcpConnectionRundown> rundowndownEvents in myTcpConnectionRundowns.ToLookup(x => x.Tcb))   // add existing connections, but only add it once
            {
                var rundownAtStart = rundowndownEvents.First();
                bool bFound = false;
                foreach (var conn in connectionsByTcb[rundownAtStart.Tcb])
                {
                    if (conn.TimeStampOpen == null || (conn.TimeStampOpen != null && conn.TimeStampClose== null ) )
                    {
                        bFound = true;  // connection was already added by other TCP events do not add it again
                        break;
                    }
                }


                if (!bFound)
                {
                    ETWProcessIndex pIdx = GetProcessIndex(results, rundownAtStart.Timestamp, rundownAtStart.Pid, 0);

                    var connection = new TcpRequestConnect(rundownAtStart.Tcb, rundownAtStart.LocalIpAndPort, rundownAtStart.RemoteIpAndPort, null, null, pIdx);

                    // take for existing localhost connections the remote visible port and not the other tuple which can be present when port forwarding is used.
                    if (rundownAtStart.LocalIpAndPort.Address == rundownAtStart.RemoteIpAndPort.Address && rundownAtStart.LocalIpAndPort.Port > rundownAtStart.RemoteIpAndPort.Port)
                    {
                        notyetAdded.Add(connection);
                        continue;
                    }

                    myConnections.Add(connection);
                }
            }

            // if we have omitted localhost connections which are not already present add them now to not skip true localhost connections
            foreach(var notAdded in notyetAdded)
            {
                if( myConnections.Where(x=> (notAdded.LocalIpAndPort == x.LocalIpAndPort && notAdded.RemoteIpAndPort == x.RemoteIpAndPort) ||
                                            (notAdded.LocalIpAndPort == x.RemoteIpAndPort && notAdded.LocalIpAndPort == x.RemoteIpAndPort)).Count() == 0)
                {
                    myConnections.Add(notAdded);
                }
            }

            connectionsByTcb = myConnections.ToLookup(x => x.Tcb);

        }

        TcpConnection Find(ulong tcb, DateTimeOffset time, ETWExtract results)
        {
            foreach (var resultConnection in results.Network.TcpData.Connections)
            {
                if (resultConnection.IsMatching(tcb, time))
                {
                    return resultConnection;
                }
            }
            return null;
        }

        private void ConvertTcpEventsFromGenericEvents(ETWExtract results, IGenericEvent[] events)
        {
            foreach (var ev in events)
            {
                switch (ev.Id)
                {
                    case TcpETWConstants.TcpDataTransferSendId:
                        OnTcpDataTransferSend(ev);           
                        break;
                    case TcpETWConstants.TcpSendPosted:
                        OnTcpSendPosted(ev);
                        break;
                    case TcpETWConstants.TcpDataTransferReceive:
                        OnTcpDataTransferReceive(ev);
                        break;
                    case TcpETWConstants.TcpDataTransferRetransmitRound:
                        OnRetransmit(ev);
                        break;
                    case TcpETWConstants.TcpTailLossProbe:
                        OnTailLossProbe(ev);
                        break;
                    case TcpETWConstants.TcpRequestConnect:  // used only for remote connection attempts
                        OnConnect(ev, results);
                        break;
                    case TcpETWConstants.TcpAcceptListenerComplete:  // used for localhost connection establishment calls
                        OnTcpAcceptListenerComplete(ev, results);
                        break;
                    case TcpETWConstants.TcpTemplateChanged:
                        OnTcpTemplateChanged(ev);        // Based on intial round trip time measurements the OS will choose either Internet or DataCenter with different TCP settings (e.g. MinRTO 300ms vs 20ms).
                        break;
                    case TcpETWConstants.TcpConnectionKeepAlive:
                        OnKeepAlive(ev);                 // When TCP Keepalive at socket level is enabled the OS will send zero sized keepalive packets. Routers tend to swallow them. It is not guaranteed to keep the connection alive.
                        break;
                    case TcpETWConstants.TcpConnectionSummary:
                        OnTcpConnectionSummary(ev);      // Connection summary is logged after close of connection or connection rundown like xperf/tracelog during trace start. Event loss is possible here.
                        break;
                    case TcpETWConstants.TcpConnectionRundown:
                        OnTcpConnectionRundown(ev);           // Connection rundown enumerates all existing connections. This happens usually during trace start and is issued by tracelog/xperf.
                        break;
                    case TcpETWConstants.TcpConnectTcbFailedRcvdRst:
                        OnTcpConnectTcbFailedRcvdRst(ev);     // Connection establishment failed. This is not a real connection close event.
                        break;
                    case TcpETWConstants.TcpAbortTcbRequest:
                        OnTcpAbortTcbRequest(ev);            // Connection Reset sent by client
                        break;
                    case TcpETWConstants.TcpCloseTcbRequest:
                        OnClose(ev, results);              // TCB will not be requested to close e.g. in case of connection failure this will not be called
                        break;
                    case TcpETWConstants.TcpShutdownTcb:
                        OnTcbShutdownTcb(ev, results);     // In all cases the TCB object will be teared down
                        break;
                    case TcpETWConstants.IpNeighborState:
                        try
                        {
                            OnIpNeighborState(ev, results);
                        }
                        catch (InvalidOperationException) // sometimes we get invalid MAC addresses
                        { }
                        break;
                    case TcpETWConstants.TcpDisconnectTcbRtoTimeout:
                        OnRetransmitTimeout(ev);
                        break;
                    default:
                        // Do nothing
                        break;
                }
            }
        }


        readonly List<TcpConnectionKeepAlive> myKeepAlives = new();

        private void OnKeepAlive(IGenericEvent ev)
        {
            TcpConnectionKeepAlive alive = new(ev);
            myKeepAlives.Add(alive);
        }

        double? GetMaxDelayBetweenPackets(IEnumerable<IGenericTcpEvent> events)
        {
            double? lret = null;

            IGenericTcpEvent previous = null;
            foreach(var ev in events)
            {
                if (previous != null)
                {
                    TimeSpan diff = ev.Timestamp - previous.Timestamp;
                    lret = diff.TotalSeconds > lret.GetValueOrDefault() ? diff.TotalSeconds : lret;
                }
                previous = ev;
            }

            return lret;
        }

        private void OnIpNeighborState(IGenericEvent ev, ETWExtract results)
        {
            var neighbor = new IpNeighborState(ev);
        }

        /// <summary>
        /// Connection attempt failed, send connection reset during handshake to client
        /// </summary>
        private void OnTcpConnectTcbFailedRcvdRst(IGenericEvent ev)
        {
            TcpConnectTcbFailedRcvdRst rst = new(ev);
            if (TCBFilter(rst.Tcb))
            {
                ulong tcb = (ulong)ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;

                // connection open did fail. There is no close event
                foreach (var connect in myConnections.OrderByDescending(x => x.TimeStampOpen))
                {
                    if (connect.Tcb == tcb && connect.TimeStampOpen < rst.Timestamp && connect.TimeStampClose == null)
                    {
                        connect.TimeStampClose = rst.Timestamp;
                        break;
                    }
                }
            }
        }

        List<TcpAbortTcbRequest> myTcpAbortRequests = new();

        /// <summary>
        /// Client did send connection reset packet
        /// </summary>
        /// <param name="ev"></param>
        void OnTcpAbortTcbRequest(IGenericEvent ev)
        {
            TcpAbortTcbRequest abort = new(ev);
            if (TCBFilter(abort.Tcb))
            {
                myTcpAbortRequests.Add(abort);
            }
        }

        void UpdateConnectionResetTimeStampSentFromClient()
        {
            foreach (var abort in myTcpAbortRequests.OrderByDescending(x=>x.Timestamp))
            {
                foreach (var connect in myConnections.OrderByDescending(x => x.TimeStampClose))
                {
                    if (connect.Tcb == abort.Tcb && connect.TimeStampClose > abort.Timestamp && connect.ResetTimeStamp == null &&
                        (connect.TimeStampOpen ?? DateTimeOffset.MinValue) < abort.Timestamp)
                    {
                        connect.ResetTimeStamp = abort.Timestamp;
                        break;
                    }
                }
            }
        }


        readonly List<TcpDisconnectTcbRtoTimeout> myTcpDisconnectTcbRtoTimeout = new();

        private void OnRetransmitTimeout(IGenericEvent ev)
        {
            TcpDisconnectTcbRtoTimeout rst = new(ev);
            if( TCBFilter(rst.Tcb)) 
            {
                myTcpDisconnectTcbRtoTimeout.Add(rst);

                ulong tcp = rst.Tcb;

                foreach(var connect in myConnections.OrderByDescending(x=> x.TimeStampOpen))
                {
                    if( connect.Tcb == tcp && connect.TimeStampOpen < rst.Timestamp && connect.TimeStampClose == null )
                    {
                        connect.TCPRetansmitTimeout = rst.Timestamp;
                        break;
                    }
                }
            }
        }

        readonly List<TcpTemplateChanged> myTemplateChangedEvents = new();

        private void OnTcpTemplateChanged(IGenericEvent ev)
        {
            TcpTemplateChanged changed = new(ev);
            if (TCBFilter(changed.Tcb))
            {
                myTemplateChangedEvents.Add(changed);
            }
        }

        private void OnTcpDataTransferReceive(IGenericEvent ev)
        {
            TcpDataTransferReceive receive = new(ev);
            if (TCBFilter(receive.Tcb))
            {
                myReceiveEvents.Add(receive);
            }
        }

        /// <summary>
        /// Called when a remote connection is established
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="extract"></param>
        private void OnConnect(IGenericEvent ev, ETWExtract extract)
        {
            ETWProcessIndex idx = GetProcessIndex(extract, ev.Timestamp.ConvertToTime(), ev.Process?.Id, ev.ProcessId);
            TcpRequestConnect conn = new(ev, idx);
            if (TCBFilter(conn.Tcb))
            {
                myConnections.Add(conn);
            }
        }

        /// <summary>
        /// Called when a localhost connection is established
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="extract"></param>
        private void OnTcpAcceptListenerComplete(IGenericEvent ev, ETWExtract extract)
        {
            ETWProcessIndex idx = GetProcessIndex(extract, ev.Timestamp.ConvertToTime(), ev.Process?.Id, ev.ProcessId);
            TcpRequestConnect conn = new(ev, idx);

            if (TCBFilter(conn.Tcb))
            {
                myConnections.Add(conn);
            }
        }

        private void OnTailLossProbe(IGenericEvent ev)
        {
            TcpTailLossProbe probe = new(ev);
            if (TCBFilter(probe.Tcb))
            {
                TcpRetransmit resend = new(probe.Tcb, probe.SndUna, probe.Timestamp);
                myRetransmits.Add(resend);
            }
        }

        readonly List<TcpConnectionRundown> myTcpConnectionRundowns = new();

        private void OnTcpConnectionRundown(IGenericEvent ev)
        {
            TcpConnectionRundown rundown = new(ev);
            if( TCBFilter(rundown.Tcb))
            {
                myTcpConnectionRundowns.Add(rundown);
            }
        }

        readonly List<TcpConnectionSummary> myTcpConnectionSummaries = new();

        private void OnTcpConnectionSummary(IGenericEvent ev)
        {
            TcpConnectionSummary summary = new(ev);
            if (TCBFilter(summary.Tcb))
            {
                myTcpConnectionSummaries.Add(summary);
            }
        }

        private void OnClose(IGenericEvent ev, ETWExtract extract)
        {
            TcpCloseTcbRequest close = new TcpCloseTcbRequest(ev);

            if (TCBFilter(close.Tcb))
            {
                bool bFound = UpdateConnectionCloseTime(close.Tcb, close.Timestamp);

                if ( !bFound)  // connections which are closed, but were not opened during the trace need to add a synthetic connection
                {
                    ETWProcessIndex processIdx = GetProcessIndex(extract, close.Timestamp, close.ProcessId, ev.ProcessId);
                    // normally close is issued by Idle which is not a real process. Use invalid here.
                    myConnections.Add( new TcpRequestConnect(close.Tcb, close.LocalIpAndPort, close.RemoteIpAndPort, null, close.Timestamp, processIdx));
                }

            }
        }

        private void OnTcbShutdownTcb(IGenericEvent ev, ETWExtract extract)
        {
            TcpShutdownTcb shutdown = new TcpShutdownTcb(ev);

            if (TCBFilter(shutdown.Tcb))
            {
                bool bFound = UpdateConnectionCloseTime(shutdown.Tcb, shutdown.Timestamp);

                if (!bFound)  // connections which are closed, but were not opened during the trace need to add a synthetic connection
                {
                    ETWProcessIndex processIdx = GetProcessIndex(extract, shutdown.Timestamp, shutdown.ProcessId, ev.ProcessId );
                    // normally close is issued by Idle which is not a real process. Use invalid here.
                    myConnections.Add(new TcpRequestConnect(shutdown.Tcb, shutdown.LocalIpAndPort, shutdown.RemoteIpAndPort, null, shutdown.Timestamp, processIdx));
                }

            }
        }

        bool UpdateConnectionCloseTime(ulong tcb, DateTimeOffset closeTime)
        {
            bool bFound = false;
            foreach (var connect in myConnections.OrderByDescending(x => x.TimeStampOpen))
            {
                if (connect.Tcb == tcb && (connect.TimeStampOpen ?? DateTimeOffset.MinValue) < closeTime)
                {
                    connect.TimeStampClose = closeTime;
                    bFound = true;
                    break;
                }
            }

            return bFound;
        }

        ETWProcessIndex GetProcessIndex(ETWExtract extract, DateTimeOffset time, uint? eventProcessId, uint etwProcessIdx)
        {
            if (eventProcessId != null && eventProcessId > 0 && eventProcessId != uint.MaxValue)
            {
                return extract.GetProcessIndexByPidAtTime(eventProcessId.Value, time);
            }

            if (etwProcessIdx > 0)
            {
                return extract.GetProcessIndexByPidAtTime(etwProcessIdx, time);
            }

            return ETWProcessIndex.Invalid;
        }

        private void OnRetransmit(IGenericEvent ev)
        {
            TcpRetransmit retrans = new(ev);
            if (TCBFilter(retrans.Tcb))
            {
                myRetransmits.Add(retrans);
            }
        }

        /// <summary>
        /// For remote connections BytesSent can be 0.
        /// </summary>
        /// <param name="ev"></param>
        private void OnTcpDataTransferSend(IGenericEvent ev)
        {
            TcpDataTransferSend sentPacket = new(ev);
            if (TCBFilter(sentPacket.Tcb))
            {
                myTcpDataSendEvents.Add(sentPacket);
            }
        }

        /// <summary>
        /// Local and remote connections are using this event to send data. 
        /// </summary>
        /// <param name="ev"></param>
        private void OnTcpSendPosted(IGenericEvent ev)
        {
            TcpSendPosted postedPacket = new(ev);
            if (TCBFilter(postedPacket.Tcb))
            {
                mySendPostedEvents.Add(postedPacket);
            }
        }

        private bool IsValidTcpEvent(IGenericEvent ev)
        {
            return ev.ProviderId == TcpETWConstants.Guid && ev.Fields != null;
        }



        TcpRequestConnect LocateConnection(ulong tcb, DateTimeOffset time, ETWExtract extract, ref ILookup<ulong, TcpRequestConnect> connectionsByTcb)
        {
            // Check if we have already a stored connection
            foreach (var connection in connectionsByTcb[tcb])
            {
                if (connection.IsMatching(tcb, time))
                {
                    return connection;
                }
            }

            return null;
        }




        /// <summary>
        /// Detect firewall issues where socket send order is not maintained. The TCP send queue will be altered by the firewall which removes posted packets and injects
        /// them later again. There are sometimes issues where the firewall stops inspecting packets and the first no longer inspected packet is followed by an injected packet
        /// leading to e.g. network streams where the payload is sent before the header data leading in the best case to protocol errors. In the worst case this leads
        /// to silent data corruption.
        /// This heuristic search works best when network packets with different sizes which are not repeated to often are sent. If size is the same and appears
        /// multiple times within the search window we cannot detect issues.    
        /// </summary>
        /// <param name="results"></param>
        /// <param name="sentByConnection"></param>
        /// <param name="connectionMap"></param>
        private void DetectSendOrderIssues(ETWExtract results, (ILookup<TcpRequestConnect, TcpSendPosted>, ILookup<TcpRequestConnect, TcpDataTransferSend>) sentByConnection, Dictionary<TcpRequestConnect, ConnectionIdx> connectionMap)
        {
            // For performance reasons we do only inspect ordering in a fixed window area. 
            // Based on past data no more than 30 should be needed. 
            const int WindowSize = 100;

            foreach (IGrouping<TcpRequestConnect, TcpSendPosted> posted in sentByConnection.Item1)
            {

                TcpSendPosted[] postedEvents = posted.ToArray();
                HashSet<TcpSendPosted> matched = new();

                // get all matched injected and posted packets and remove them, leaving just the packets 
                // which have changed sequence number, or no matching injected event does exist. 
                // The error manifests when an injected event comes after a posted packet for which no inject event exists
                // pp iippppp is ok because the first two posted packets are injected again and later injection stops
                // pp ipipppp is not ok because the second injected packet comes after the third posted packet changing the send order
                // since identity of packets is only based on sequence number (which can change when injected again) and number of bytes we can do only some heuristic search.
                int injectedIndex = 0;
                TcpSendPosted injected = null;
                while ((injected = GetNextInjected(ref injectedIndex, postedEvents.Length, postedEvents)) != null)
                {
                    for (int j = injectedIndex - 1; j >= Math.Max(0, injectedIndex - WindowSize); j--)
                    {
                        TcpSendPosted postedBefore = postedEvents[j];
                        if (!postedBefore.IsPosted)
                        {
                            continue;
                        }

                        if (postedBefore.NumBytes == injected.NumBytes && (postedBefore.SndNext == injected.SndNext))
                        {
                            matched.Add(postedBefore);
                            matched.Add(injected);
                        }
                    }
                }

                TcpSendPosted[] unmatched = posted.Where(x => !matched.Contains(x)).ToArray();
                HashSet<TcpSendPosted> examined = new();

                injectedIndex = 0;

                while ((injected = GetNextInjected(ref injectedIndex, unmatched.Length, unmatched)) != null)
                {
                    for (int p = injectedIndex - 1; p >= Math.Max(0, injectedIndex - WindowSize); p--)
                    {
                        TcpSendPosted previousPost = unmatched[p];   // [p,...,i]

                        // get previous posted event which matches the injected event by number of bytes
                        // we do not compare sequence number because it can change when packet is injected again (heuristic!)
                        if (!previousPost.IsPosted || previousPost.NumBytes != injected.NumBytes)
                        {
                            continue;
                        }

                        // p      i 
                        //    p       with no injected event following
                        // 
                        // we have found a matching posted packet which is most likely the one which was injected later
                        // When we now find another posted packet which was posted before the injected event but no matching
                        // injected event is found then we have potentially a send order issue
                        int nextPostedIndex = p + 1;
                        TcpSendPosted nextPosted = null;
                        while ((nextPosted = GetNextPosted(ref nextPostedIndex, injectedIndex, unmatched)) != null)
                        {
                            TcpSendPosted matchingInjected = null;
                            int nextInjectedIndex = nextPostedIndex;
                            bool bFound = false;
                            while ((matchingInjected = GetNextInjected(ref nextInjectedIndex, injectedIndex + WindowSize, unmatched)) != null)
                            {
                                if (matchingInjected.NumBytes == nextPosted.NumBytes)
                                {
                                    bFound = true;
                                    break;
                                }
                            }

                            if (injected.Timestamp - previousPost.Timestamp > TimeSpan.FromMilliseconds(10.0))
                            {
                                continue; // if packets are further away it is most likely a misclassification.
                            }

                            if (!bFound && !examined.Contains(nextPosted))
                            {
                                results.Network.TcpData.TcpIssues.PostIssues.Add(new TcpPostIssue(previousPost, injected, nextPosted, connectionMap[previousPost.Connection]));
                                //Console.WriteLine($"{nextPosted.Timestamp.ToString(DumpBase.DateTimeFormat6)} Found send order issue with connection TCB: {nextPosted.Tcb:X} Posted Message: SndNext: {nextPosted.SndNext} NumBytes: {nextPosted.NumBytes} {nextPosted.Connection}.");
                                //Console.WriteLine($"    {previousPost.Timestamp.ToString(DumpBase.DateTimeFormat6)} Previous posted: {previousPost.Tcb:X} SndNext: {previousPost.SndNext} NumBytes: {previousPost.NumBytes} {previousPost.Connection}");
                                //Console.WriteLine($"    {injected.Timestamp.ToString(DumpBase.DateTimeFormat6)    } Injected to    : {injected.Tcb:X} SndNext: {injected.SndNext} NumBytes: {injected.NumBytes} ByteDistance: {injected.SndNext-previousPost.SndNext} bytes {injected.Connection}");
                            }

                            // do not report packet more than once
                            examined.Add(nextPosted);
                        }

                    }

                }

            }
        }

        TcpSendPosted GetNextInjected(ref int injectIndex, int upperBound, TcpSendPosted[] events)
        {
            for (; injectIndex < Math.Min(upperBound, events.Length); injectIndex++)
            {
                if (events[injectIndex].IsInjected)
                {
                    return events[injectIndex++];
                }
            }
            return null;
        }

        TcpSendPosted GetNextPosted(ref int postedIndex, int upperBound, TcpSendPosted[] events)
        {
            for (; postedIndex < Math.Min(upperBound, events.Length); postedIndex++)
            {
                if (events[postedIndex].IsPosted)
                {
                    return events[postedIndex++];
                }
            }
            return null;
        }

        private void ReleaseMemory()
        {
            myTcpDataSendEvents.Clear();
            mySendPostedEvents.Clear();
            myReceiveEvents.Clear();
            myRetransmits.Clear();
            myConnections.Clear();
            myKeepAlives.Clear();
            myTemplateChangedEvents.Clear();
            myTcpConnectionRundowns.Clear();
            myTcpConnectionSummaries.Clear();
            myTcpDisconnectTcbRtoTimeout.Clear();
            myGenericEvents = null;
        }

    }
}
