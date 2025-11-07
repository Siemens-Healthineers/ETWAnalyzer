//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.Network.Tcp;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing.Events;
using System;

namespace ETWAnalyzer.Extractors.TCP
{
    internal class TcpAbortTcbRequest
    {
        // Fields: Tcb:Pointer, LocalAddressLength, LocalAddress:Binary, RemoteAddressLength, RemoteAddress:Binary, Status, ProcessId, Compartment, Tcb, ProcessStartKey
        public ulong Tcb { get; set; }

        public TcpRequestConnect Connection { get; set; }

        public SocketConnection LocalIpAndPort { get; private set; }
        public SocketConnection RemoteIpAndPort { get; private set; }
        public uint Compartment { get; }
        public UInt32 NewState { get; private set; }

        public DateTimeOffset Timestamp { get; set; }

        public TcpAbortTcbRequest(IGenericEvent ev)
        {
            Tcb = (ulong)ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            Timestamp = ev.Timestamp.ConvertToTime();
            NewState = ev.Fields[TcpETWConstants.StatusField].AsUInt32;
            LocalIpAndPort = ev.Fields[TcpETWConstants.LocalAddressField].GetSocketConnection();
            RemoteIpAndPort = ev.Fields[TcpETWConstants.RemoteAddressField].GetSocketConnection();
            Compartment = ev.Fields[TcpETWConstants.CompartmentField].AsUInt32;
        }
    }
}