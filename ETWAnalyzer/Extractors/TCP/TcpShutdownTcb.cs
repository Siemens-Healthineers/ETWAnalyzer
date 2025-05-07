//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Network.Tcp;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing.Events;
using System;

namespace ETWAnalyzer.Extractors.TCP
{

    /// <summary>
    /// When a Tcb is released this one will always be called
    /// </summary>
    internal class TcpShutdownTcb
    {
        // Fields: LocalAddress, RemoteAddress, Status, ProcessId, Compartment, Tcb, ProcessStartKey,
        public ulong Tcb { get; set; }
        public uint SndUna { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public SocketConnection LocalIpAndPort { get; private set; }
        public SocketConnection RemoteIpAndPort { get; private set; }

        public UInt32 Compartment { get; private set; }
        public UInt32 ProcessId { get; private set; }

        public NtStatus Status { get; private set; }    
        public UInt64 ProcessStartKey { get; private set; }

        public TcpShutdownTcb(IGenericEvent ev)
        {
            Tcb = (ulong)ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            LocalIpAndPort = new SocketConnection(ev.Fields[TcpETWConstants.LocalAddressField].AsSocketAddress.ToIPEndPoint());
            RemoteIpAndPort = new SocketConnection(ev.Fields[TcpETWConstants.RemoteAddressField].AsSocketAddress.ToIPEndPoint());
            ProcessId = ev.Fields[TcpETWConstants.ProcessIdField].AsUInt32;
            Compartment = ev.Fields[TcpETWConstants.CompartmentField].AsUInt32;
            ProcessStartKey = ev.Fields[TcpETWConstants.ProcessStartKey].AsUInt64;
            Status = (NtStatus) ev.Fields[TcpETWConstants.StatusField].AsUInt32;
            Timestamp = ev.Timestamp.DateTimeOffset;
        }
    }
}
