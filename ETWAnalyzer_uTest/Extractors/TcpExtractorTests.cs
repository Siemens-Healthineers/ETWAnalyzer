using ETWAnalyzer.Extract;
using ETWAnalyzer.Extractors.TCP;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing.Events;
using Microsoft.Windows.EventTracing;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Net;
using Microsoft.Windows.EventTracing.Processes;
using System.Security.Cryptography;
using ETWAnalyzer.Extract.Network.Tcp;
using System.ComponentModel.DataAnnotations;
using ETWAnalyzer_uTest.Extract;

namespace ETWAnalyzer_uTest.Extractors
{
    public class TcpExtractorTests
    {
        enum TCB : long
        {
            One=100,
            Two,
            Three,
            Four
        }

        enum Pid
        {
            OneDrive=1,
            SettingsHost,
            CCMExec
        }

        /// <summary>
        /// Time in 100 ns
        /// </summary>
        enum Time : long
        {
            T0_1 =   100_0000,
            T0_2 =   200_0000,
            T0_3 =   300_0000,
            T0_4 =   400_0000,
            T0_5 =   500_0000,
            T0_9 =   900_0000,
            T1_0 = 1_000_0000,
            T1_1 = 1_100_0000,
            T1_5 = 1_500_0000,
            T1_9 = 1_900_0000,
            T2_0 = 2_000_0000,
            T2_1 = 2_100_0000,
            T2_5 = 2_500_0000,
            T2_8 = 2_800_0000,
            T2_9 = 2_900_0000,
            T3_0 = 3_000_0000,
            T3_1 = 3_100_0000,
            T3_2 = 3_200_0000,
            T3_3 = 3_300_0000,
            T3_5 = 3_500_0000,
            T4_0 = 4_000_0000,
        }

        enum SequenceNr
        {
            S_1000 = 1000,
            S_1001 = 1001,
            S_1003 = 1003,
            S_1500 = 1500,
            S_2000 = 2000,
            S_3000 = 3000,
        }

        IGenericEvent CreateConnect(Time timeStampTicks, Pid pid, TCB tcb, string srcIpPort, string dstIpPort)
        {
            Mock<IGenericEvent> connectEv = CreateEvent(timeStampTicks, TcpETWConstants.TcpRequestConnect, pid, tcb, srcIpPort, dstIpPort, out Mock<IGenericEventFieldList> fields);
            return connectEv.Object;
        }

        IGenericEvent CreateDisconnect(Time timeStampTicks, Pid pid, TCB tcb, string srcIpPort, string dstIpPort)
        {
            Mock<IGenericEvent>  disconnectEv = CreateEvent(timeStampTicks, TcpETWConstants.TcpCloseTcbRequest, pid, tcb, srcIpPort, dstIpPort, out Mock<IGenericEventFieldList> fields);
            return disconnectEv.Object;
        }

        IGenericEvent CreateFailedConnect(Time timeStampTicks, Pid pid, TCB tcb, string srcIpPort, string dstIpPort)
        {
            Mock<IGenericEvent> failedEv = CreateEvent(timeStampTicks, TcpETWConstants.TcpConnectTcbFailedRcvdRst, pid, tcb, srcIpPort, dstIpPort, out Mock <IGenericEventFieldList> fields);

            var newStateField = new Mock<IGenericEventField>();
            newStateField.Setup(x => x.AsUInt32).Returns(4);

            fields.Setup( x => x[TcpETWConstants.NewStateField]).Returns(newStateField.Object);

            return failedEv.Object;
        }

        IGenericEvent CreateSend(Time timeStampTicks, Pid pid, TCB tcb, SequenceNr seq, int bytesSent)
        {
            Mock<IGenericEvent> sendEv = CreateEvent(timeStampTicks, TcpETWConstants.TcpDataTransferSendId, pid, tcb, out Mock<IGenericEventFieldList> fields);

            var bytesSentField = new Mock<IGenericEventField>();
            bytesSentField.Setup(x => x.AsUInt32).Returns((uint) bytesSent);

            fields.Setup(x => x[TcpETWConstants.BytesSentField]).Returns(bytesSentField.Object);


            var sequenceNrField = new Mock<IGenericEventField>();
            sequenceNrField.Setup(x => x.AsUInt32).Returns((uint) seq);

            fields.Setup(x => x[TcpETWConstants.SeqNoField]).Returns(sequenceNrField.Object);
           
            return sendEv.Object;
        }

        IGenericEvent CreatePost(Time timeStampTicks, Pid pid, TCB tcb, SequenceNr seq, int bytesPosted)
        {
            Mock<IGenericEvent> sendEv = CreateEvent(timeStampTicks, TcpETWConstants.TcpSendPosted, pid, tcb, out Mock<IGenericEventFieldList> fields);

            var bytesSentField = new Mock<IGenericEventField>();
            bytesSentField.Setup(x => x.AsUInt32).Returns((uint)bytesPosted);

            fields.Setup(x => x[TcpETWConstants.NumBytesField]).Returns(bytesSentField.Object);


            var sequenceNrField = new Mock<IGenericEventField>();
            sequenceNrField.Setup(x => x.AsUInt32).Returns((uint)seq);

            fields.Setup(x => x[TcpETWConstants.SndNxtField]).Returns(sequenceNrField.Object);

            var injectedField = new Mock<IGenericEventField>(); 
            injectedField.Setup(x => x.AsString).Returns("posted");
            fields.Setup(x => x[TcpETWConstants.InjectedField]).Returns(injectedField.Object);

            return sendEv.Object;
        }

        IGenericEvent CreateReceive(Time timeStampTicks, Pid pid, TCB tcb, SequenceNr seq, int bytesReceived)
        {
            Mock<IGenericEvent> recEv = CreateEvent(timeStampTicks, TcpETWConstants.TcpDataTransferReceive, pid, tcb, out Mock<IGenericEventFieldList> fields);

            var byteReceivedField = new Mock<IGenericEventField>();
            byteReceivedField.Setup(x => x.AsUInt32).Returns((uint) bytesReceived);

            fields.Setup(x => x[TcpETWConstants.NumBytesField]).Returns(byteReceivedField.Object);


            var sequenceNrField = new Mock<IGenericEventField>();
            sequenceNrField.Setup(x => x.AsUInt32).Returns((uint)seq);

            fields.Setup(x => x[TcpETWConstants.SeqNoField]).Returns(sequenceNrField.Object);

            return recEv.Object;
        }

        IGenericEvent CreateRetransmit(Time timeStampTicks, Pid pid, TCB tcb, SequenceNr seq)
        {
            Mock<IGenericEvent> retransEv = CreateEvent(timeStampTicks, TcpETWConstants.TcpDataTransferRetransmitRound, pid, tcb, out Mock<IGenericEventFieldList> fields);

            var sendUnackField = new Mock<IGenericEventField>();
            sendUnackField.Setup(x => x.AsUInt32).Returns((uint) seq);

            fields.Setup(x => x[TcpETWConstants.SndUnaField]).Returns(sendUnackField.Object);

            return retransEv.Object;
        }


        [Fact]
        public void CorrectlyAssignRetransmits_To_First_SentPacket()
        {
            TCPExtractor extractor = new TCPExtractor();
            ETWExtract extract = CreateExtract();

            const string Remote1 = "20.20.20.20:40000";
            const string SrcIpPort = "10.10.10.10:100";

            IGenericEvent[] events = new IGenericEvent[]
            {
                CreateConnect(   Time.T0_1, Pid.OneDrive,     TCB.One, SrcIpPort, Remote1),
                CreatePost(      Time.T0_2, Pid.OneDrive,     TCB.One, SequenceNr.S_1000, 100),
                CreateSend(      Time.T0_2, Pid.OneDrive,     TCB.One, SequenceNr.S_1000, 100),
                CreatePost(      Time.T0_3, Pid.OneDrive,     TCB.One, SequenceNr.S_2000, 100),
                CreateSend(      Time.T0_3, Pid.OneDrive,     TCB.One, SequenceNr.S_2000, 100),
                CreateRetransmit(Time.T0_3, Pid.OneDrive,     TCB.One, SequenceNr.S_2000),
                CreatePost(      Time.T0_4, Pid.OneDrive,     TCB.One, SequenceNr.S_2000, 100),
                CreateSend(      Time.T0_4, Pid.OneDrive,     TCB.One, SequenceNr.S_2000, 100),
                CreateRetransmit(Time.T0_4, Pid.OneDrive,     TCB.One, SequenceNr.S_2000),
                CreatePost(      Time.T0_5, Pid.OneDrive,     TCB.One, SequenceNr.S_2000, 100),
                CreateSend(      Time.T0_5, Pid.OneDrive,     TCB.One, SequenceNr.S_2000, 100),
                CreateRetransmit(Time.T0_5, Pid.OneDrive,     TCB.One, SequenceNr.S_2000),
                CreateDisconnect(Time.T4_0, Pid.OneDrive,     TCB.One, SrcIpPort, Remote1),
            };

            extractor.ExtractFromGenericEvents(extract, events);

            IETWExtract iExtract = extract;
            var tcpData = iExtract.Network.TcpData;

            Assert.Single(tcpData.Connections);
            Assert.Equal(3, tcpData.Retransmissions.Count);

            var retrans0 = tcpData.Retransmissions[0];
            Assert.Equal(100, retrans0.NumBytes);
            Assert.Equal((uint)SequenceNr.S_2000, retrans0.SequenceNumber);
            Assert.Equal((long)Time.T0_3, retrans0.RetransmitTime.Ticks);
            Assert.Equal((long)Time.T0_3, retrans0.SendTime.Ticks);

            var retrans1 = tcpData.Retransmissions[1];
            Assert.Equal(100, retrans1.NumBytes);
            Assert.Equal((uint)SequenceNr.S_2000, retrans1.SequenceNumber);
            Assert.Equal((long)Time.T0_4, retrans1.RetransmitTime.Ticks);
            Assert.Equal((long)Time.T0_4, retrans1.SendTime.Ticks);
        }


        [Fact]
        public void Can_ExtractConnection_With_Failed_Connects()
        {
            TCPExtractor extractor = new TCPExtractor();
            ETWExtract extract = CreateExtract();

            const string Remote1 = "20.20.20.20:40000";
            const string SrcIpPort = "10.10.10.10:100";
            const string SrcIpPort_1 = "10.10.10.10:101";
            const string SrcIpPort_2 = "10.10.10.10:102";

            IGenericEvent[] events = new IGenericEvent[]
            {
                CreateConnect(   Time.T0_1, Pid.OneDrive,     TCB.One, SrcIpPort, Remote1),
                CreateSend(      Time.T0_2, Pid.OneDrive,     TCB.One, SequenceNr.S_1000, 100),
                CreateSend(      Time.T0_2, Pid.OneDrive,     TCB.One, SequenceNr.S_2000, 100),
                CreateReceive(   Time.T0_2, Pid.OneDrive,     TCB.One, SequenceNr.S_1001, 400),
                CreateReceive(   Time.T0_2, Pid.OneDrive,     TCB.One, SequenceNr.S_1001, 400),
                CreateRetransmit(Time.T0_2, Pid.OneDrive,     TCB.One, SequenceNr.S_2000),
                CreateDisconnect(Time.T0_5, Pid.OneDrive,     TCB.One, SrcIpPort, Remote1),

                CreateConnect(   Time.T1_0, Pid.OneDrive,     TCB.One, SrcIpPort_1, Remote1),
                CreateSend(      Time.T1_5, Pid.OneDrive,     TCB.One, SequenceNr.S_1000, 100),
                CreateFailedConnect(Time.T3_0, Pid.OneDrive,  TCB.One, SrcIpPort_1, Remote1),

                CreateConnect(   Time.T3_1, Pid.SettingsHost, TCB.One, SrcIpPort_2, Remote1),
                CreateSend(      Time.T3_5, Pid.SettingsHost, TCB.One, SequenceNr.S_2000, 600),
                CreateSend(      Time.T3_5, Pid.SettingsHost, TCB.One, SequenceNr.S_3000, 600),
                CreateRetransmit(Time.T3_5, Pid.SettingsHost, TCB.One, SequenceNr.S_3000),
                CreateDisconnect(Time.T4_0, Pid.SettingsHost, TCB.One, SrcIpPort_2, Remote1),
            };

            extractor.ExtractFromGenericEvents(extract, events);

            IETWExtract iExtract = extract;
            Assert.Equal(3, iExtract.Network.TcpData.Connections.Count);

            ITcpRetransmission retrans0 = iExtract.Network.TcpData.Retransmissions[0];

            Assert.True(retrans0.IsClientRetransmission);
            Assert.Equal(400, retrans0.NumBytes);
            Assert.Equal((uint)SequenceNr.S_1001, retrans0.SequenceNumber);
        }
         
        [Fact]
        public void Can_ExtractConnection_With_Retransmits()
        {
            TCPExtractor extractor  = new TCPExtractor();
            ETWExtract extract = CreateExtract();

            const string Remote1 = "20.20.20.20:40000";
            const string SrcIpPort = "10.10.10.10:100";
            const string SrcIpPort_1 = "10.10.10.10:101";
            const string SrcIpPort_2 = "10.10.10.10:102";

            IGenericEvent[] events = new IGenericEvent[]
            {
                CreateConnect(   Time.T0_1, Pid.OneDrive,     TCB.One, SrcIpPort, Remote1),
                CreatePost(      Time.T0_2, Pid.OneDrive,     TCB.One, SequenceNr.S_1000, 100),
                CreateSend(      Time.T0_2, Pid.OneDrive,     TCB.One, SequenceNr.S_2000, 100),
                CreatePost(      Time.T0_2, Pid.OneDrive,     TCB.One, SequenceNr.S_2000, 100),
                CreateSend(      Time.T0_2, Pid.OneDrive,     TCB.One, SequenceNr.S_2000, 100),
                CreateReceive(   Time.T0_2, Pid.OneDrive,     TCB.One, SequenceNr.S_1001, 400),
                CreateReceive(   Time.T0_2, Pid.OneDrive,     TCB.One, SequenceNr.S_1001, 400),
                CreatePost(      Time.T0_3, Pid.OneDrive,     TCB.One, SequenceNr.S_2000, 100),
                CreateSend(      Time.T0_3, Pid.OneDrive,     TCB.One, SequenceNr.S_2000, 100),
                CreateRetransmit(Time.T0_5, Pid.OneDrive,     TCB.One, SequenceNr.S_2000),
                CreateDisconnect(Time.T0_5, Pid.OneDrive,     TCB.One, SrcIpPort_1, Remote1),

                CreateConnect(   Time.T1_0, Pid.OneDrive,     TCB.One, SrcIpPort_1, Remote1),
                CreatePost(      Time.T1_5, Pid.OneDrive,     TCB.One, SequenceNr.S_1000, 500),
                CreateSend(      Time.T1_5, Pid.OneDrive,     TCB.One, SequenceNr.S_1000, 500),
                CreatePost(      Time.T1_5, Pid.OneDrive,     TCB.One, SequenceNr.S_2000, 500),
                CreateSend(      Time.T1_5, Pid.OneDrive,     TCB.One, SequenceNr.S_2000, 500),
                CreateDisconnect(Time.T1_9, Pid.OneDrive,     TCB.One, SrcIpPort_1, Remote1),

                CreateConnect(   Time.T2_0, Pid.SettingsHost, TCB.One, SrcIpPort_2, Remote1),
                CreatePost(      Time.T2_5, Pid.SettingsHost, TCB.One, SequenceNr.S_2000, 600),
                CreateSend(      Time.T2_5, Pid.SettingsHost, TCB.One, SequenceNr.S_2000, 600),
                CreatePost(      Time.T2_5, Pid.SettingsHost, TCB.One, SequenceNr.S_3000, 600),
                CreateSend(      Time.T2_5, Pid.SettingsHost, TCB.One, SequenceNr.S_3000, 600),
                CreatePost(      Time.T2_8, Pid.SettingsHost, TCB.One, SequenceNr.S_3000, 600),
                CreateSend(      Time.T2_8, Pid.SettingsHost, TCB.One, SequenceNr.S_3000, 600),
                CreateRetransmit(Time.T2_8, Pid.SettingsHost, TCB.One, SequenceNr.S_3000),
                CreateDisconnect(Time.T3_0, Pid.SettingsHost, TCB.One, SrcIpPort_2, Remote1),
            };

            extractor.ExtractFromGenericEvents(extract, events);

            IETWExtract iExtract = extract;

            Assert.Equal(3, iExtract.Network.TcpData.Connections.Count);

            ITcpConnection con0 = iExtract.Network.TcpData.Connections[0];

            Assert.Equal(300ul, con0.BytesSent);
            Assert.Equal(Remote1, con0.RemoteIpAndPort.ToString());
            Assert.Equal(SrcIpPort, con0.LocalIpAndPort.ToString());
            Assert.Equal(800ul, con0.BytesReceived);
            Assert.Equal((ulong) TCB.One, con0.Tcb);

            ITcpConnection con1 = iExtract.Network.TcpData.Connections[1];

            Assert.Equal(1000ul, con1.BytesSent);
            Assert.Equal(Remote1, con1.RemoteIpAndPort.ToString());
            Assert.Equal(SrcIpPort_1, con1.LocalIpAndPort.ToString());
            Assert.Equal(0ul, con1.BytesReceived);
            Assert.Equal((ulong)TCB.One, con1.Tcb);

            ITcpConnection con2 = iExtract.Network.TcpData.Connections[2];

            Assert.Equal(1800ul, con2.BytesSent);
            Assert.Equal(Remote1, con2.RemoteIpAndPort.ToString());
            Assert.Equal(SrcIpPort_2, con2.LocalIpAndPort.ToString());
            Assert.Equal(0ul, con2.BytesReceived);
            Assert.Equal((ulong)TCB.One, con2.Tcb);

            Assert.Equal(3, iExtract.Network.TcpData.Retransmissions.Count);

            ITcpRetransmission retrans0 = iExtract.Network.TcpData.Retransmissions[0];

            Assert.True(retrans0.IsClientRetransmission);
            Assert.Equal(400, retrans0.NumBytes);
            Assert.Equal((uint) SequenceNr.S_1001, retrans0.SequenceNumber);

            ITcpRetransmission retrans1 = iExtract.Network.TcpData.Retransmissions[1];

            Assert.Null(retrans1.IsClientRetransmission);
            Assert.Equal(100, retrans1.NumBytes);
            Assert.Equal((uint) SequenceNr.S_2000, retrans1.SequenceNumber);

            ITcpRetransmission retrans2 = iExtract.Network.TcpData.Retransmissions[2];

            Assert.Null(retrans2.IsClientRetransmission);
            Assert.Equal((uint) SequenceNr.S_3000, retrans2.SequenceNumber);
            Assert.Equal(600, retrans2.NumBytes);
            Assert.Equal((long) Time.T2_8, retrans2.RetransmitTime.Ticks);
            Assert.Equal((long) Time.T2_8, retrans2.SendTime.Ticks);
        }



        static Mock<IGenericEvent> CreateEvent(Time timeStampTicks, int eventId, Pid pid, TCB tcb, string srcIpPort, string dstIpPort, out Mock<IGenericEventFieldList> fields)
        {
            Mock<IGenericEvent> ev = CreateEvent(timeStampTicks, eventId, pid, tcb, out fields);

            var srcAddressField = new Mock<IGenericEventField>();
            srcAddressField.Setup(x => x.AsSocketAddress).Returns(ParseIPAndPort(srcIpPort));

            var dstAddressField = new Mock<IGenericEventField>();
            dstAddressField.Setup(x => x.AsSocketAddress).Returns(ParseIPAndPort(dstIpPort));

            var pidField = new Mock<IGenericEventField>();
            pidField.Setup(x => x.AsUInt32).Returns((uint)pid);

            var compartmentField = new Mock<IGenericEventField>();
            compartmentField.Setup(x => x.AsUInt32).Returns(0); 

            fields.Setup(x => x[TcpETWConstants.CompartmentField]).Returns(compartmentField.Object);
            fields.Setup(x => x[TcpETWConstants.ProcessIdField]).Returns(pidField.Object);
            fields.Setup(x => x[TcpETWConstants.LocalAddressField]).Returns(srcAddressField.Object);
            fields.Setup(x => x[TcpETWConstants.RemoteAddressField]).Returns(dstAddressField.Object);

            return ev;
        }

        static Mock<IGenericEvent> CreateEvent(Time timeStampTicks, int eventId, Pid pid, TCB tcb, out Mock<IGenericEventFieldList> fields)
        {
            Mock<IGenericEvent> ev = CreateEvent(timeStampTicks, eventId, pid, out fields);

            var tcbField = new Mock<IGenericEventField>();
            tcbField.Setup(x => x.AsAddress).Returns(new Address((long)tcb));
           
            fields.Setup(x => x[TcpETWConstants.TcbField]).Returns(tcbField.Object);

            return ev;
        }

        static Mock<IGenericEvent> CreateEvent(Time timeStampTicks, int eventId, Pid pid, out Mock<IGenericEventFieldList> fields)
        {
            var mockEvent = new Mock<IGenericEvent>();
            fields = new Mock<IGenericEventFieldList>();

            var process = new Mock<IProcess>();
            process.Setup(x => x.Id).Returns((int)pid);

            mockEvent.Setup(x => x.Id).Returns(eventId);
            mockEvent.Setup(x => x.Timestamp).Returns(CreateTime(timeStampTicks));
            mockEvent.Setup(x => x.Process).Returns(process.Object);
            mockEvent.Setup(foo => foo.Fields).Returns(fields.Object);

            return mockEvent;
        }

        static SocketAddress ParseIPAndPort(string ipAndPort)
        {
            string[] parts = ipAndPort.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

            IPAddress ip = IPAddress.Parse(string.Join(":", parts.Take(parts.Length - 1)));
            int port = int.Parse(parts.Last());

            var endPoint = new IPEndPoint(ip, port);
            SocketAddress address = endPoint.Serialize();
            return address;
        }

        static TraceTimestamp CreateTime(Time timeStampTicks)
        {
            return new TraceTimestamp(new MockTraceTimestampContext(), new TraceTimestampValue((long)timeStampTicks));
        }

        ETWExtract CreateExtract()
        {
            ETWExtract extract = new ETWExtract();
            extract.Processes = new List<ETWProcess>
            {
                new ETWProcess
                {
                    ProcessID = (int) Pid.OneDrive,
                    ProcessName = "OneDrive.exe",
                    StartTime = DateTimeOffset.MinValue,
                    EndTime = DateTimeOffset.MaxValue,
                },
                new ETWProcess
                {
                    ProcessID = (int) Pid.SettingsHost,
                    ProcessName = "SettingsHost.exe",
                    StartTime = DateTimeOffset.MinValue,
                    EndTime = DateTimeOffset.MaxValue,
                },
                new ETWProcess
                {
                    ProcessID = (int) Pid.CCMExec,
                    ProcessName = "CCMExec.exe",
                    StartTime = DateTimeOffset.MinValue,
                    EndTime = DateTimeOffset.MaxValue,
                }
            };

            return extract;
        }
    }
}
