using ETWAnalyzer.Extract.Network.Tcp;
using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Text;

namespace ETWAnalyzer.Extractor.Tcp
{
    internal interface ITcpRequestConnect
    {
        SocketConnection LocalIpAndPort { get; }
        ETWProcessIndex ProcessIdx { get; }
        SocketConnection RemoteIpAndPort { get; }
        ulong Tcb { get; }
        DateTimeOffset? TCPRetansmitTimeout { get; }
        DateTimeOffset? TimeStampClose { get; }
        DateTimeOffset? TimeStampOpen { get; }

        bool IsMatching(ulong tcb, DateTimeOffset time);

    }
}
