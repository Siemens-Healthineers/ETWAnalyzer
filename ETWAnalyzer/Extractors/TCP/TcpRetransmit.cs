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

        /// <summary>
        /// TCP connection state at the time of the retransmit. When the state is
        /// <see cref="TcpETWConstants.TCPIP_STATE.SynSent"/> the retransmit is a resent connection request (SYN)
        /// and not a retransmit of payload data of an established connection.
        /// Null when the event did not contain a TcpState field.
        /// </summary>
        public TcpETWConstants.TCPIP_STATE? TcpState { get; set; }

        public TcpRequestConnect Connection { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public TcpRetransmit(IGenericEvent ev)
        {
            Tcb = (ulong) ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            SndUna = ev.Fields[TcpETWConstants.SndUnaField].AsUInt32;

            // older OS versions do not log the TcpState field
            IGenericEventField stateField = ev.Fields[TcpETWConstants.TcpStateField];
            if (stateField != null)
            {
                TcpState = (TcpETWConstants.TCPIP_STATE)stateField.AsUInt32;
            }

            Timestamp = ev.Timestamp.ConvertToTime();
        }


        public TcpRetransmit(ulong tcb, uint sndUna, DateTimeOffset timestamp)
        {
            Tcb = tcb;
            SndUna = sndUna;
            Timestamp = timestamp; 
        }
    }
}
