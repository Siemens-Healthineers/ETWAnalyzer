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
    /// Fired when data is sent over the wire. Used by WPA to calculate send rate.
    /// </summary>
    internal class TcpDataSend : IGenericTcpEvent
    {
        public ulong Tcb { get; set; }

        public TcpRequestConnect Connection { get; set; }

        public int BytesSent { get; set; }
        public uint SequenceNr { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public TcpDataSend(ulong tcb, int bytesSent, uint sequenceNr, DateTimeOffset timestamp)
        {
            Tcb = tcb;
            BytesSent = bytesSent;
            SequenceNr = sequenceNr;
            Timestamp = timestamp;
        }


        public TcpDataSend(IGenericEvent ev)
        {
            
            Tcb = (ulong)ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            BytesSent = (int)ev.Fields[TcpETWConstants.BytesSentField].AsUInt32;
            SequenceNr = ev.Fields[TcpETWConstants.SeqNoField].AsUInt32;
            Timestamp = ev.Timestamp.DateTimeOffset;
        }
    }

}
