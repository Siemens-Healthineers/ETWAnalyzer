using System;
using System.Collections.Generic;
using System.Text;

namespace ETWAnalyzer.Extractor.Tcp
{
    internal interface ITcpSendPosted
    {
        ITcpRequestConnect Connection { get; }
        string Injected { get; set; }
        bool IsInjected { get; }
        bool IsPosted { get; }
        uint NumBytes { get; set; }
        uint PostedBytes { get; }
        uint SndNext { get; set; }
        ulong Tcb { get; set; }
        DateTimeOffset Timestamp { get; set; }
    }
}
