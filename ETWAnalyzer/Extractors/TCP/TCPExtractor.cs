﻿//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Network.Tcp;
using ETWAnalyzer.Extractors.Dns;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        /// Send events
        /// </summary>
        readonly List<TcpDataSend> mySendEvents = new();


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

        /// <summary>
        /// Needed to resolve TCBs for connections which are missing the open event but are not part of TCP rundown data
        /// </summary>
        readonly List<TcpAcceptListenerComplete> myAcceptListenerCompletes = new();

        /// <summary>
        /// Prevent adding accept events more than once from <see cref="myAcceptListenerCompletes"/>. Otherwise we will duplicate keep adding duplicate connection with 
        /// same tcb, connect and disconnect time. 
        /// </summary>
        HashSet<TcpAcceptListenerComplete> myAlreadyAdded = new();

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

        }

        internal void ExtractFromGenericEvents(ETWExtract results, IGenericEvent[] events)
        {
            foreach (var ev in events)
            {
                switch (ev.Id)
                {
                    case TcpETWConstants.TcpDataTransferSendId:
                        OnTpcDataTransferSend(ev);
                        break;
                    case TcpETWConstants.TcpDataTransferReceive:
                        OnTcpDataTransferReceive(ev);
                        break;
                    case TcpETWConstants.TcpDataTransferRetransmitRound:
                        OnRetransmit(ev);
                        break;
                    case TcpETWConstants.TcpRequestConnect:
                        OnConnect(ev, results);
                        break;
                    case TcpETWConstants.TcpTemplateChanged:
                        OnTcpTemplateChanged(ev);
                        break;
                    case TcpETWConstants.TcpTailLossProbe:
                        OnTailLossProbe(ev);
                        break;
                    case TcpETWConstants.TcpConnectionSummary:
                        OnTcpConnectionSummary(ev);
                        break;
                    case TcpETWConstants.TcpConnectTcbFailedRcvdRst:
                        OnTcpConnectTcbFailedRcvdRst(ev);
                        break;
                    case TcpETWConstants.TcpConnectionRundown:
                        OnTcpConnectionRundown(ev);
                        break;
                    case TcpETWConstants.TcpAcceptListenerComplete:
                        OnTcpAcceptListenerComplete(ev);
                        break;
                    case TcpETWConstants.TcpCloseTcbRequest:
                        OnClose(ev, results);
                        break;
                    case TcpETWConstants.TcpDisconnectTcbRtoTimeout:
                        OnRetransmitTimeout(ev);
                        break;
                    default:
                        // Do nothing
                        break;
                }
            }


            ILookup<ulong, TcpRequestConnect> connectionsByTcb = myConnections.ToLookup(x => x.Tcb);


            foreach (TcpDataSend send in mySendEvents)
            {
                send.Connection = LocateConnection(send.Tcb, send.Timestamp, results, ref connectionsByTcb);
            }

            ILookup<TcpRequestConnect, TcpDataSend> sentByConnection = mySendEvents.ToLookup(x => x.Connection);

            foreach (TcpDataTransferReceive receive in myReceiveEvents)
            {
                receive.Connection = LocateConnection(receive.Tcb, receive.Timestamp, results, ref connectionsByTcb);
            }

            ILookup<TcpRequestConnect, TcpDataTransferReceive> receivedByConnection = myReceiveEvents.ToLookup(x => x.Connection);

            foreach (TcpTemplateChanged templateChange in myTemplateChangedEvents)
            {
                templateChange.Connection = LocateConnection(templateChange.Tcb, templateChange.Timestamp, results, ref connectionsByTcb);
            }

            ILookup<TcpRequestConnect, TcpTemplateChanged> byConnectionTemplateChanges = myTemplateChangedEvents.ToLookup(x => x.Connection);

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
                var lastreceiveTime = receivedByConnection[tcpconnection].LastOrDefault()?.Timestamp;

                ulong bytesSent = (ulong)sentByConnection[tcpconnection].Sum(x => (decimal)((TcpDataSend)x).BytesSent);
                int datagramsReceived = receivedByConnection[tcpconnection].Count();
                int datagramsSent = sentByConnection[tcpconnection].Count();

                TcpConnection connection = new(tcpconnection.Tcb, tcpconnection.LocalIpAndPort, tcpconnection.RemoteIpAndPort, tcpconnection.TimeStampOpen, tcpconnection.TimeStampClose,
                    templates.LastOrDefault(), bytesSent, datagramsSent, bytesReceived, datagramsReceived, tcpconnection.ProcessIdx, tcpconnection.TCPRetansmitTimeout, null, lastreceiveTime);
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
                List<TcpDataSend> retransmittedSent = sentByConnection[retrans.Connection].Where(x => x.SequenceNr == retrans.SndUna && x.Timestamp <= retrans.Timestamp)
                                                    .OrderBy(x => x.Timestamp).ToList();

                if( retransmittedSent.Count > 0)
                {
                    var lastSent = retransmittedSent.Last(); // this is the send which caused later retransmit events
                    // The application can issue further send events without canceling the current pending retransmissions

                    // store retransmit event with send time of first sent packet. It might be an ACK packet but for now the heuristics should be good enough
                    // to be useful.
                    results.Network.TcpData.Retransmissions.Add(new TcpRetransmission(connect2Idx[retrans.Connection], retrans.Timestamp,
                        lastSent.Timestamp, lastSent.SequenceNr, lastSent.BytesSent));

                    // set last send time before retransmissions did start
                    TcpConnection connection = results.Network.TcpData.Connections[(int)connect2Idx[retrans.Connection]];
                    if (connection.RetransmitTimeout != null) // connection was closed due to retransmit
                    {
                        connection.LastSent = retransmittedSent.Last().Timestamp; // last send event 
                    }
                }
            }
        }

        readonly List<TcpConnectTcbFailedRcvdRst> myTcpConnectTcbFailedRcvdRst = new();

        private void OnTcpConnectTcbFailedRcvdRst(IGenericEvent ev)
        {
            TcpConnectTcbFailedRcvdRst rst = new(ev);
            if (TCBFilter(rst.Tcb))
            {
                myTcpConnectTcbFailedRcvdRst.Add(rst);

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
  
        private void OnTcpAcceptListenerComplete(IGenericEvent ev)
        {
            TcpAcceptListenerComplete complete = new(ev);
            if (TCBFilter(complete.Tcb))
            {
                myAcceptListenerCompletes.Add(complete);
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
            myTcpConnectionRundowns.Add(rundown);
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
                bool bFound = false;
                foreach (var connect in myConnections.OrderByDescending(x => x.TimeStampOpen))
                {
                    if (connect.Tcb == close.Tcb)
                    {
                        connect.TimeStampClose = ev.Timestamp.DateTimeOffset;
                        bFound = true;
                        break;
                    }
                }

                if( !bFound)  // connections which are closed, but were not opened during the trace need to add a synthetic connection
                {
                    // normally close is issued by Idle which is not a real process. Use invalid here.
                    myConnections.Add( new TcpRequestConnect(close.Tcb, close.LocalIpAndPort, close.RemoteIpAndPort, null, close.Timestamp, ETWProcessIndex.Invalid));
                }

            }
        }

        private void OnConnect(IGenericEvent ev, ETWExtract extract)
        {
            ETWProcessIndex idx = extract.GetProcessIndexByPidAtTime(ev.Process.Id, ev.Timestamp.DateTimeOffset);
            TcpRequestConnect conn = new(ev, idx);
            if (TCBFilter(conn.Tcb))
            {
                myConnections.Add(conn);
            }
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
        /// Used for debugging a specific connection
        /// </summary>
        /// <param name="tcb"></param>
        /// <returns></returns>
        bool TCBFilter(ulong tcb)
        {
            return true;
        }

        private void OnTpcDataTransferSend(IGenericEvent ev)
        {
            TcpDataSend sentPacket = new(ev);
            if (TCBFilter(sentPacket.Tcb))
            {
                mySendEvents.Add(sentPacket);
            }
        }

        private bool IsValidTcpEvent(IGenericEvent ev)
        {
            return ev.ProviderId == TcpETWConstants.Guid && ev.Process?.ImageName != null && ev.Fields != null;
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

            // connection which did already exist before we did start are stored in TCP Rundown data
            foreach (var summary in myTcpConnectionRundowns)
            {
                // add existing connection 
                if (summary.Tcb == tcb)
                {
                    ETWProcessIndex processIdx = extract.GetProcessIndexByPidAtTime((int) summary.Pid, summary.Timestamp);
                    var connection = new TcpRequestConnect(summary.Tcb, summary.LocalIpAndPort, summary.RemoteIpAndPort, null, null, processIdx);
                    myConnections.Add(connection);
                    connectionsByTcb = myConnections.ToLookup(x => x.Tcb);
                    return connection;
                }
            }

            // Some connections are in connecting state but we have not got connect event anymore. Use This event instead
            foreach (var complete in myAcceptListenerCompletes)
            {
                if (complete.Tcb == tcb && !myAlreadyAdded.Contains(complete) )
                {
                    var processIdx = extract.GetProcessIndexByPidAtTime((int) complete.ProcessId, complete.Timestamp);
                    var connection = new TcpRequestConnect(complete.Tcb, complete.LocalIpAndPort, complete.RemoteIpAndPort, complete.Timestamp, null, processIdx);
                    myConnections.Add(connection);
                    connectionsByTcb = myConnections.ToLookup(x => x.Tcb);
                    myAlreadyAdded.Add(complete);
                    return connection;
                }
            }

            return null;
        }

    }
}
