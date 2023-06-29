//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


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
    /// Fired when TCP data is received. That is also used by WPA to calculate Receive rate.
    /// </summary>
    internal class TcpDataTransferReceive : IGenericTcpEvent
    {
        /// Fields: Tcb, NumBytes:UInt32, SeqNo:UInt32
        public ulong Tcb { get; set; }

        public TcpRequestConnect Connection { get; set; }

        public int NumBytes { get; set; }
        public uint SequenceNr { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public TcpDataTransferReceive(IGenericEvent ev)
        {
            Tcb = (ulong)ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            NumBytes = (int)ev.Fields[TcpETWConstants.NumBytesField].AsUInt32;
            SequenceNr = ev.Fields[TcpETWConstants.SeqNoField].AsUInt32;
            Timestamp = ev.Timestamp.DateTimeOffset;
        }
    }
}
