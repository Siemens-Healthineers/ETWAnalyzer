using ETWAnalyzer.Extract.Network.Tcp;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extractors.TCP
{
    /// <summary>
    /// Represents a request to close a TCP connection.
    /// </summary>
    internal class TcpCloseTcbRequest
    {
        // Fields: Tcb:Pointer LocalAddress:Binary RemoteAddress:Binary Status:UInt32  ProcessId:UInt32  Compartment:UInt32  
        public ulong Tcb { get; set; }
        public SocketConnection LocalIpAndPort { get; private set; }
        public SocketConnection RemoteIpAndPort { get; private set; }

        public UInt32 Compartment { get; private set; }
        public UInt32 ProcessId { get; private set; }

        public DateTimeOffset Timestamp { get; set; }

        public TcpCloseTcbRequest(IGenericEvent ev)
        {
            Tcb = (ulong)ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            LocalIpAndPort = new SocketConnection(ev.Fields[TcpETWConstants.LocalAddressField].AsSocketAddress.ToIPEndPoint());
            RemoteIpAndPort = new SocketConnection(ev.Fields[TcpETWConstants.RemoteAddressField].AsSocketAddress.ToIPEndPoint());
            ProcessId = ev.Fields[TcpETWConstants.ProcessIdField].AsUInt32;
            Compartment = ev.Fields[TcpETWConstants.CompartmentField].AsUInt32;
            Timestamp = ev.Timestamp.DateTimeOffset;
        }
    }
}
