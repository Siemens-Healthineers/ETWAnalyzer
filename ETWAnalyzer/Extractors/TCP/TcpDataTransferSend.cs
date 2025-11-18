//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing.Events;
using System;

namespace ETWAnalyzer.Extractors.TCP
{

    /// <summary>
    /// Fired when sending data after posting it to TCP send queue.
    /// </summary>
    internal class TcpDataTransferSend : IGenericTcpEvent
    {
        public ulong Tcb { get; set; }

        public TcpRequestConnect Connection { get; set; }

        public int BytesSent { get; set; }
        public uint SequenceNr { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public TcpDataTransferSend(ulong tcb, int bytesSent, uint sequenceNr, DateTimeOffset timestamp)
        {
            Tcb = tcb;
            BytesSent = bytesSent;
            SequenceNr = sequenceNr;
            Timestamp = timestamp;
        }


        public TcpDataTransferSend(IGenericEvent ev)
        {
            Tcb = (ulong)ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            BytesSent = (int)ev.Fields[TcpETWConstants.BytesSentField].AsUInt32;
            SequenceNr = ev.Fields[TcpETWConstants.SeqNoField].AsUInt32;
            Timestamp = ev.Timestamp.ConvertToTime();
        }
    }

}
