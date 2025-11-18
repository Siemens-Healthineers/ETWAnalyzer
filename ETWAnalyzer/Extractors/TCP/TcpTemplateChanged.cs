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
using static ETWAnalyzer.TraceProcessorHelpers.TcpETWConstants;

namespace ETWAnalyzer.Extractors.TCP
{
    /// <summary>
    /// When a TCP connection is established it applies a TCP template which defines RTO and other things. Predefined are DataCenter and Internet Template
    /// </summary>
    internal class TcpTemplateChanged
    {
        /// Fields: Tcb, TemplateType:UInt32 of TCPIP_TEMPLATE_TYPE_ValueMap, Context:String
        public ulong Tcb { get; set; }

        public TCPIP_TEMPLATE_TYPES TemplateType { get; set; }

        public DateTimeOffset Timestamp { get; set; }
        public TcpRequestConnect Connection { get; internal set; }

        public TcpTemplateChanged(IGenericEvent ev)
        {
            Tcb = (ulong)ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            Timestamp = ev.Timestamp.ConvertToTime();
            TemplateType = (TCPIP_TEMPLATE_TYPES) ev.Fields[TemplateTypeField].AsUInt32;
        }
    }
}
