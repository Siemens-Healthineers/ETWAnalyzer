//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract;
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
    /// TCP Retransmission event which contain the sequence nr which was sent again.
    /// </summary>
    internal class TcpRetransmit : IGenericTcpEvent
    {
        public ulong Tcb { get; set; }
        public uint SndUna { get; set; }

        public TcpRequestConnect Connection { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public TcpRetransmit(IGenericEvent ev)
        {
            Tcb = (ulong) ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            SndUna = ev.Fields["SndUna"].AsUInt32;
            Timestamp = ev.Timestamp.DateTimeOffset;
        }


        public TcpRetransmit(ulong tcb, uint sndUna, DateTimeOffset timestamp)
        {
            Tcb = tcb;
            SndUna = sndUna;
            Timestamp = timestamp; 
        }
    }
}
