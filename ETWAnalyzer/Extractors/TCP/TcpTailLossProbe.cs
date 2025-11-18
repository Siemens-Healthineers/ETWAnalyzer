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
    /// This event seems to resend pending data without causing a retransmit event. That comes usually later.
    /// By looking at Wireshark traces we seem to miss the first retransmission which seems to be correlated as Tail Loss probe event.
    /// </summary>
    internal class TcpTailLossProbe
    {
        // Fields: Tcb:Pointer,  SndUna:UInt32, SndMax:UInt32, SendAvailable:UInt32, TailProbeSeq:UInt32, TailProbeLast:UInt32, ControlsToSend:UInt32, ThFlags:UInt8
        public ulong Tcb { get; set; }
        public uint SndUna { get; set; }

        public TcpRequestConnect Connection { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public TcpTailLossProbe(IGenericEvent ev)
        {
            Tcb = (ulong) ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            SndUna = ev.Fields["SndUna"].AsUInt32;
            Timestamp = ev.Timestamp.ConvertToTime();
        }
    }
}
