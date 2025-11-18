//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.TraceProcessorHelpers
{
    /// <summary>
    /// Contains important event ids and field names of ETW provider Microsoft-Windows-TCPIP  which are used to parse ETW events for network troubleshooting.
    /// </summary>
    internal class TcpETWConstants
    {
        public const string ProviderName = "Microsoft-Windows-TCPIP";
        public static readonly Guid Guid = new Guid("2F07E2EE-15DB-40F1-90EF-9D7BA282188A");

        /// <summary>
        /// Fields: Tcb, NumBytes:UInt32, SeqNo:UInt32
        /// </summary>
        public const int TcpDataTransferReceive = 1074;

        /// <summary>
        /// Fields Tcb, Cwnd, SndWnd, BytesSent, SeqNo
        /// </summary>
        public const int TcpDataTransferSendId = 1332;

        /// <summary>
        /// Fields Tcb, Injected, NumBytes, SndNxt
        /// </summary>
        public const int TcpSendPosted = 1159;

        /// <summary>
        /// Tcb, Injected, NumBztes, SndNxt, 
        /// </summary>
        public const int TcpSendTransmitted = 1160;

        /// <summary>
        /// Fields LocalAddress, RemoteAddress, Status, ProcessId, Compartment, Tcb, ProcessStartKey,
        /// </summary>
        public const int TcpShutdownTcb = 1044;

        /// <summary>
        /// Fields: Tcb, SndUna, RexmitCount,SRTT,RTO 
        /// </summary>
        public const int TcpDataTransferRetransmitRound = 1351;

        /// <summary>
        /// Fields: Tcb, LocalAddress, RemoteAddress 
        /// </summary>
        public const int TcpRequestConnect = 1002;

        /// <summary>
        /// Fields: Tcb, TemplateType:UInt32 of TCPIP_TEMPLATE_TYPE_ValueMap, Context:String
        /// </summary>
        public const int TcpTemplateChanged = 1224;

        /// <summary>
        /// Fields: Tcb, SndUna, SndMax
        /// </summary>
        public const int TcpConnectionKeepAlive = 1188;

        public enum TCPIP_TEMPLATE_TYPES : UInt32
        {
            Internet = 0,
            DataCenter =1,
            Compat = 2,
            DataCenterCustom = 3,
            InternetCustom = 4,
            Default = 6,
            Automatic = 7,
        }
    
        /// <summary>
        /// Fields: Tcb, TemplateType:UInt32 of TCPIP_TEMPLATE_TYPE_ValueMap, MinRto:UInt32, EnableCwndRestart:UInt32, InitialCwnd:UInt32
        /// CongestionAlgorithm:UInt32 of TCP_CONGESTION_ALGORITHM_ValueMap, MaxDataRetransmissions:UInt32, DelayedAckTicks:UInt32
        /// DelayedAckFrequency:UInt32, Rack:UInt32, TailLossProbe:UInt32
        /// </summary>
        public const int TcpTemplateParameters = 1223;
        /// <summary>
        /// Fields: Tcb, LocalAddress, RemoteAddress 
        /// </summary>
        public const int TcpCloseTcbRequest = 1038;

        /// <summary>
        /// Fields: Tcb:Pointer, LocalAddressLength, LocalAddress:Binary, RemoteAddressLength, RemoteAddress:Binary, NewState, RexmitCount
        /// </summary>
        public const int TcpConnectTcbFailedRcvdRst = 1183;

        /// <summary>
        /// Fields: Tcb:Pointer, LocalAddressLength, LocalAddress:Binary, RemoteAddressLength, RemoteAddress:Binary, Status, ProcessId, Compartment, Tcb, ProcessStartKey
        /// </summary>
        public const int TcpAbortTcbRequest = 1039;
        /// <summary>
        /// Fields: Tcb, LocalAddress, RemoteAddress, NewState 
        /// </summary>
        public const int TcpDataTransferRestransmit = 1187;

        /// <summary>
        /// Fields: Tcb, DataBytesOut, DataBytesIn, DataSegmentsOut, DataSegmentsIn, DupAcksIn, BytesRetrans, Timeouts, FastRetran,   
        /// </summary>
        public const int TcpConnectionSummary = 1477;

        /// <summary>
        /// Fields: LocalAddress, RemoteAddress, Status, ProcessId, Compartment, Tcb, ProcessStartKey
        /// </summary>
        public const int TcpConnectTcbFailure = 1034;

        /// <summary>
        /// Fields: Tcb:Pointer,  SndUna:UInt32, SndMax:UInt32, SendAvailable:UInt32, TailProbeSeq:UInt32, TailProbeLast:UInt32, ControlsToSend:UInt32, ThFlags:UInt8 
        /// </summary>
        public const int TcpTailLossProbe = 1385;

        /// <summary>
        /// Fields: Tcb:Pointer, LocalAddress:Binary, RemoteAddress:Binary, State:UInt32, Pid:UInt32, ProcessStartKey 
        /// </summary>
        public const int TcpConnectionRundown = 1300;

        /// <summary>
        /// Fields: Interface, IpAddrLen, IpAddress DlAddrLen, DLAddress, Old Neighbor State, New Neighbor State, Neighbor  Event, Compartment, 
        /// </summary>
        public const int IpNeighborState = 1324;

        /// <summary>
        /// Fields: LocalAddress:Binary RemoteAddress:Binary Status:UInt32  ProcessId:UInt32  Compartment:UInt32  Tcb:Pointer 
        /// </summary>
        public const int TcpAcceptListenerComplete = 1017;

        /// <summary>
        /// Fields: LocalAddress:Binary RemoteAddress:Binary  Status:UInt32  ProcessId:UInt32  Compartment:UInt32  Tcb:Pointer 
        /// </summary>
        public const int TcpDisconnectTcbRtoTimeout = 1046;

        /// <summary>
        /// Transfer Control block
        /// </summary>
        public const string TcbField = "Tcb";

        /// <summary>
        /// Local address
        /// </summary>
        public const string LocalAddressField = "LocalAddress";

        /// <summary>
        /// New State field name
        /// </summary>
        public const string NewStateField = "NewState";

        /// <summary>
        /// Remote address
        /// </summary>
        public const string RemoteAddressField = "RemoteAddress";

        /// <summary>
        /// ProcessId
        /// </summary>
        public const string ProcessIdField = "ProcessId";

        /// <summary>
        /// TCP Template type value
        /// </summary>
        public const string TemplateTypeField = "TemplateType";

        /// <summary>
        /// Used for virtual networks in Docker/WSL environments
        /// </summary>
        public const string CompartmentField = "Compartment";

        /// <summary>
        /// Used for process correlation
        /// </summary>
        public const string ProcessStartKey = "ProcessStartKey";

        /// <summary>
        /// Process Id
        /// </summary>
        public const string PidField = "Pid";

        /// <summary>
        /// Bytes Sent field
        /// </summary>
        public const string BytesSentField = "BytesSent";

        /// <summary>
        /// Sequence Number field
        /// </summary>
        public const string SeqNoField = "SeqNo";

        /// <summary>
        /// Number of received bytes
        /// </summary>
        public const string NumBytesField = "NumBytes";

        /// <summary>
        /// Send Unacknowledged Data
        /// </summary>
        public const string SndUnaField = "SndUna";

        /// <summary>
        /// Connection NtStatus field
        /// </summary>
        public static string StatusField = "Status";

        /// <summary>
        /// Send Next field
        /// </summary>
        public static string SndNxtField = "SndNxt";

        /// <summary>
        /// Injected field string.
        /// </summary>
        public static string InjectedField = "Injected";
    }
}
