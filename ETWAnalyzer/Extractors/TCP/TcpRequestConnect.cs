//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Network.Tcp;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extractors.TCP
{
    /// <summary>
    /// Written at start of trace for all already open connections
    /// </summary>
    internal class TcpConnectionRundown
    {
        // Fields: Tcb:Pointer, LocalAddress:Binary, RemoteAddress:Binary, State:UInt32, Pid:UInt32, ProcessStartKey

        public ulong Tcb { get; private set; }
        public SocketConnection LocalIpAndPort { get; private set; }
        public SocketConnection RemoteIpAndPort { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }

        public uint Pid { get; private set; }

        // Tcb, LocalAddress, RemoteAddress, State, Pid, ProcessStartKey
        public TcpConnectionRundown(IGenericEvent ev)
        {
            Tcb = (ulong) ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            LocalIpAndPort = new SocketConnection(ev.Fields[TcpETWConstants.LocalAddressField].AsSocketAddress.ToIPEndPoint());
            RemoteIpAndPort = new SocketConnection(ev.Fields[TcpETWConstants.RemoteAddressField].AsSocketAddress.ToIPEndPoint());
            Pid = ev.Fields[TcpETWConstants.PidField].AsUInt32;
            Timestamp = ev.Timestamp.DateTimeOffset;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Local: {LocalIpAndPort} Remote: {RemoteIpAndPort} TimeStamp: {Timestamp.ToString(TcpRequestConnect.TimeFmt)}, TCB: 0x{Tcb:X}";
        }
    }

    /// <summary>
    /// It is written when a connection is closed.
    /// </summary>
    internal class TcpConnectionSummary
    {
    //    Tcb, DataBytesOut, DataBytesIn, DataSegmentsOut, DataSegmentsIn, DupAcksIn, BytesRetrans, Timeouts, FastRetran,  

        public ulong Tcb { get; set; }
        public UInt64 DataBytesOut { get; set; }
        public UInt64 DataBytesIn { get; set; }
        public UInt64 DataSegmentsOut { get; set; }
        public UInt64 DataSegmentsIn { get; set;}
        public UInt32 DupAcksIn { get; set; }
        public UInt32 BytesRetrans { get; set; }
        public UInt32 Timeouts { get; set; }
        public UInt32 FastRetransmissions { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public TcpConnectionSummary(IGenericEvent ev)
        {
            Tcb = (ulong) ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            DataBytesOut = ev.Fields["DataBytesOut"].AsUInt64;
            DataBytesIn = ev.Fields["DataBytesIn"].AsUInt64;
            DataSegmentsOut = ev.Fields["DataSegmentsOut"].AsUInt64;
            DataSegmentsIn = ev.Fields["DataSegmentsIn"].AsUInt64;
            DupAcksIn = ev.Fields["DupAcksIn"].AsUInt32;
            BytesRetrans = ev.Fields["BytesRetrans"].AsUInt32;
            Timeouts = ev.Fields["Timeouts"].AsUInt32;
            FastRetransmissions = ev.Fields["FastRetran"].AsUInt32;
            Timestamp = ev.Timestamp.DateTimeOffset;
        }

    }

    internal class TcpRequestConnect : IEquatable<TcpRequestConnect>
    {
        // Tcb,  LocalAddress,  RemoteAddress

        /// <summary>
        /// Transmission Control Block kernel address which is used by other events are correlation identifier for a socket connection.
        /// </summary>
        public ulong Tcb { get;  }

        public DateTimeOffset? TimeStampOpen { get;  }
        public DateTimeOffset? TimeStampClose { get; internal set; }

        public SocketConnection LocalIpAndPort { get;  }
        public SocketConnection RemoteIpAndPort { get; }
        public ETWProcessIndex ProcessIdx { get;  }

        public TcpRequestConnect(IGenericEvent ev, ETWProcessIndex processIdx )
        {
            Tcb = (ulong) ev.Fields[TcpETWConstants.TcbField].AsAddress.Value;
            LocalIpAndPort = new SocketConnection( ev.Fields[TcpETWConstants.LocalAddressField].AsSocketAddress.ToIPEndPoint() );
            RemoteIpAndPort = new SocketConnection( ev.Fields[TcpETWConstants.RemoteAddressField].AsSocketAddress.ToIPEndPoint() );
            TimeStampOpen = ev.Timestamp.DateTimeOffset;
            ProcessIdx = processIdx;
        }

        public TcpRequestConnect(ulong tcb, SocketConnection localipandPort, SocketConnection remoteipAndPort, DateTimeOffset? timeStampOpen, DateTimeOffset? timeStampClose, ETWProcessIndex processIdx)
        {
            Tcb = tcb;
            LocalIpAndPort = localipandPort;
            RemoteIpAndPort = remoteipAndPort;
            TimeStampOpen = timeStampOpen;
            TimeStampClose = timeStampClose;
            ProcessIdx = processIdx;
        }

        /// <summary>
        /// Check if tcb value matches connection start/end times
        /// </summary>
        /// <param name="tcb">Transmission Control Block address</param>
        /// <param name="time"></param>
        /// <returns>true if connection matches tcp value or false otherwise.</returns>
        public bool IsMatching(ulong tcb, DateTimeOffset time)
        {
            return tcb == Tcb && 
                // time needs some wiggle room since we are using different events to detect a connection which are logged during connection handshake at  different times
                ( !TimeStampOpen.HasValue  ||  (time >= TimeStampOpen.Value-TimeSpan.FromMilliseconds(1)) )  && 
                ( !TimeStampClose.HasValue ||  (time <= TimeStampClose.Value + TimeSpan.FromMilliseconds(1)) ) ;
        }


        internal const string TimeFmt = "HH:mm:ss.fff";

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Local: {LocalIpAndPort} Remote: {RemoteIpAndPort} Open: {TimeStampOpen?.ToString(TimeFmt)} Close: {TimeStampClose?.ToString(TimeFmt)}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(TcpRequestConnect other)
        {
            if( other == null )
            {
                return false;
            }

            if( this.Tcb == other.Tcb &&
                this.TimeStampOpen == other.TimeStampOpen &&
                this.TimeStampClose == other.TimeStampClose &&
                this.LocalIpAndPort == other.LocalIpAndPort &&
                this.RemoteIpAndPort == other.RemoteIpAndPort
              )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hashCode = -1935525472;
            hashCode = hashCode * -1521134295 + EqualityComparer<ulong>.Default.GetHashCode(Tcb);
            hashCode = hashCode * -1521134295 + EqualityComparer<DateTimeOffset?>.Default.GetHashCode(TimeStampOpen);
            hashCode = hashCode * -1521134295 + EqualityComparer<DateTimeOffset?>.Default.GetHashCode(TimeStampClose);
            hashCode = hashCode * -1521134295 + EqualityComparer<SocketConnection>.Default.GetHashCode(LocalIpAndPort);
            hashCode = hashCode * -1521134295 + EqualityComparer<SocketConnection>.Default.GetHashCode(RemoteIpAndPort);
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TcpRequestConnect);
        }
    }
}
