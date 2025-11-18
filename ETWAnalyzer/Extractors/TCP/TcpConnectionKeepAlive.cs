using ETWAnalyzer.TraceProcessorHelpers;
//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Microsoft.Windows.EventTracing.Events;
using System;

namespace ETWAnalyzer.Extractors.TCP
{
    internal class TcpConnectionKeepAlive
    {
        // Fields:  Tcb, SndUna, SndMax
        public ulong Tcb { get; set; }
        public uint SndUna { get; set; }
        
        public DateTimeOffset Timestamp { get; set; }

        public TcpConnectionKeepAlive(IGenericEvent ev)
        {
            Tcb = (ulong)ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            SndUna = ev.Fields[TcpETWConstants.SndUnaField].AsUInt32;
            Timestamp = ev.Timestamp.ConvertToTime();
        }
    }
}
