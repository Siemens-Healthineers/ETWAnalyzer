﻿//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Network.Tcp;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;


namespace ETWAnalyzer.Extractors.TCP
{
    /// <summary>
    /// Part of connection setup which also contains local/remote address in case we did miss some initial handshake event we can use this one
    /// to correlate the connection with IP/Port and TCB pairs.
    /// </summary>
    internal class TcpAcceptListenerComplete
    {
        // Fields: LocalAddress:Binary RemoteAddress:Binary Status:UInt32  ProcessId:UInt32  Compartment:UInt32  Tcb:Pointer
        public ulong Tcb { get; set; }
        public uint SndUna { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public SocketConnection LocalIpAndPort { get; private set; }
        public SocketConnection RemoteIpAndPort { get; private set; }

        public UInt32 Compartment { get; private set; }
        public UInt32 ProcessId { get; private set; }

        public TcpAcceptListenerComplete(IGenericEvent ev)
        {
            Tcb = (ulong) ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            LocalIpAndPort =  new SocketConnection(ev.Fields[TcpETWConstants.LocalAddressField].AsSocketAddress.ToIPEndPoint());
            RemoteIpAndPort = new SocketConnection(ev.Fields[TcpETWConstants.RemoteAddressField].AsSocketAddress.ToIPEndPoint());
            ProcessId = ev.Fields[TcpETWConstants.ProcessIdField].AsUInt32;
            Compartment = ev.Fields[TcpETWConstants.CompartmentField].AsUInt32;
            Timestamp = ev.Timestamp.DateTimeOffset;
        }
    }
}
