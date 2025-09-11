//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing.Events;
using System;

namespace ETWAnalyzer.Extractors.TCP
{
    /// <summary>
    /// Fired when data written to a socket for by firewall driver which can catch packets and inject rewritten ones.
    /// To calculate the actual number of bytes sent we should consider just the posted packets and not injected ones. 
    /// The Injected field is a string which does not contain any documentation so we might still to revisit our logic again. 
    ///  Tcb, Injected, NumBytes, SndNxt
    /// </summary>
    internal class TcpSendPosted : IGenericTcpEvent
    {
        /// <summary>
        /// Transfer Control Block pointer which is used to correlate all TCP events for a connection for the lifetime of the connection.
        /// For new connections this pointer can be reused. 
        /// </summary>
        public ulong Tcb { get; set; }

        public TcpRequestConnect Connection { get; set; }

        /// <summary>
        /// Number of sent bytes
        /// </summary>
        public uint NumBytes { get; set; }

        uint? myPostedBytes;

        /// <summary>
        /// Application sending data over socket calls send() or WriteFile() which results in a "posted" event.
        /// </summary>
        const string InjectReasonPost = "posted";

        /// <summary>
        /// This is usually done by firewall which will swallow the posted packet and inject a modified one.
        /// </summary>
        const string InjectReasonInjected = "injected";

        /// <summary>
        /// Just count the bytes which are posted by application and not the injected ones.
        /// </summary>
        public uint PostedBytes         
        {
            get
            {
                if (myPostedBytes == null)
                {
                    myPostedBytes = Injected == InjectReasonPost ? NumBytes : 0;
                }
                return myPostedBytes.Value;
            }
        }

        /// <summary>
        /// Use this to count packets which were sent by application. 
        /// </summary>
        public bool IsPosted => Injected == InjectReasonPost;

        /// <summary>
        /// Base sequence number. The actually sent sequence number is SndNext + NumBytes 
        /// </summary>
        public uint SndNext { get; set; }

        /// <summary>
        /// The sent sequence number is SndNext + NumBytes
        /// </summary>
        public uint SequenceNr => SndNext + NumBytes;

        /// <summary>
        /// Time of event
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Free text field which seems to contain either "posted" or "injected"
        /// </summary>
        public string Injected { get; set; }

        public TcpSendPosted(ulong tcb, uint numBytes, uint sndNext, string injected, DateTimeOffset timestamp)
        {
            Tcb = tcb;
            NumBytes = numBytes;
            SndNext = sndNext;
            Injected = injected;
            Timestamp = timestamp;
        }


        public TcpSendPosted(IGenericEvent ev)
        {
            Tcb = (ulong)ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            NumBytes = ev.Fields[TcpETWConstants.NumBytesField].AsUInt32;
            SndNext = ev.Fields[TcpETWConstants.SndNxtField].AsUInt32;
            Injected = ev.Fields[TcpETWConstants.InjectedField].AsString;
            Timestamp = ev.Timestamp.DateTimeOffset;
        }
    }
}