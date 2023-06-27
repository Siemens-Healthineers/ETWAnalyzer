using ETWAnalyzer.Extract.Network.Tcp;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extractors.TCP
{
    /// <summary>
    /// When connection attempt fails Kernel logs this events that connection init did fail.
    /// </summary>
    internal class TcpConnectTcbFailedRcvdRst
    {
        // Fields: Tcb:Pointer, LocalIpAndPort, RemoteIpAndPort, NewState
        public ulong Tcb { get; set; }

        public TcpRequestConnect Connection { get; set; }

        public SocketConnection LocalIpAndPort { get; private set; }
        public SocketConnection RemoteIpAndPort { get; private set; }

        public UInt32 NewState { get; private set; }

        public DateTimeOffset Timestamp { get; set; }

        public TcpConnectTcbFailedRcvdRst(IGenericEvent ev)
        {
            Tcb = (ulong)ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            Timestamp = ev.Timestamp.DateTimeOffset;
            NewState = ev.Fields[TcpETWConstants.NewStateField].AsUInt32;
            LocalIpAndPort = new SocketConnection(ev.Fields[TcpETWConstants.LocalAddressField].AsSocketAddress.ToIPEndPoint());
            RemoteIpAndPort = new SocketConnection(ev.Fields[TcpETWConstants.RemoteAddressField].AsSocketAddress.ToIPEndPoint());
        }
    }
}
