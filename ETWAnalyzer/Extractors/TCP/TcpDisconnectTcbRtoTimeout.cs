//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.Network.Tcp;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing.Events;
using System;

namespace ETWAnalyzer.Extractors.TCP
{
    /// <summary>
    /// Fired when connection is reset after the maximum number of retransmissions Microsoft-Windows-TCPIP/TcpConnectRestransmit in case of
    /// connection establishment issues, or TcpDataTransferRetransmitRound after connection has been established have occurred. 
    /// </summary>
    internal class TcpDisconnectTcbRtoTimeout
    {
        // Fields: Tcb:Pointer, LocalIpAndPort, RemoteIpAndPort, Status, Compartment, ... 
        public ulong Tcb { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// not yet parsed.
        /// </summary>
        public int Compartment { get; private set; }

        public SocketConnection LocalIpAndPort { get; private set; }
        public SocketConnection RemoteIpAndPort { get; private set; }

        public TcpDisconnectTcbRtoTimeout(IGenericEvent ev)
        {
            Tcb = (ulong)ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            Timestamp = ev.Timestamp.ConvertToTime();
            LocalIpAndPort = ev.Fields[TcpETWConstants.LocalAddressField].GetSocketConnection();
            RemoteIpAndPort = ev.Fields[TcpETWConstants.RemoteAddressField].GetSocketConnection();
        }
    }
}
