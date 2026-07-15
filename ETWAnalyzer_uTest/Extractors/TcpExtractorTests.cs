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
using ETWAnalyzer.Infrastructure;
using System.IO;
using System.ComponentModel.DataAnnotations;
using ETWAnalyzer_uTest.Extract;
using ETWAnalyzer.Extractors;
using ETWAnalyzer_uTest.TestInfrastructure;

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

            StaticTraceProcessorContext.MetaData = new TraceMetaDataMock();
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

            StaticTraceProcessorContext.MetaData = new TraceMetaDataMock();

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

            StaticTraceProcessorContext.MetaData = new TraceMetaDataMock();

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
            process.Setup(x => x.Id).Returns((uint) pid);

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

        static Timestamp CreateTime(Time timeStampTicks)
        {
            return new Timestamp((long)timeStampTicks);
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

        static (DateTimeOffset Timestamp, long Bytes) Ev(double seconds, long bytes)
        {
            return (DateTimeOffset.MinValue.AddSeconds(seconds), bytes);
        }

        [Fact]
        public void TcpConnectionStatistics_Aggregate_Rates_Survive_JsonRoundTrip()
        {
            // Guards against the constructor parameter names drifting away from the property names which would
            // silently drop the aggregate rates on deserialization (Json.NET matches ctor parameters to property names).
            TcpConnectionStatistics stats = new(null, null, null, null, null, null, null, null, null, null, null, null,
                averageSendRate: 1000, averageReceiveRate: 2000,
                aggregatedSendRateBySourceIPPortTargetIP: 11, aggregatedReceiveRateBySourceIPPortTargetIP: 22,
                aggregatedSendRateBySourceIPTargetIP: 33, aggregatedReceiveRateBySourceIPTargetIP: 44);

            using MemoryStream stream = new();
            ExtractSerializer.Serialize(stream, stats);
            stream.Position = 0;
            TcpConnectionStatistics deser = ExtractSerializer.Deserialize<TcpConnectionStatistics>(stream);

            Assert.Equal(1000.0d, deser.AverageSendRate.Value, 3);
            Assert.Equal(2000.0d, deser.AverageReceiveRate.Value, 3);
            Assert.Equal(11.0d, deser.AggregatedSendRateBySourceIPPortTargetIP.Value, 3);
            Assert.Equal(22.0d, deser.AggregatedReceiveRateBySourceIPPortTargetIP.Value, 3);
            Assert.Equal(33.0d, deser.AggregatedSendRateBySourceIPTargetIP.Value, 3);
            Assert.Equal(44.0d, deser.AggregatedReceiveRateBySourceIPTargetIP.Value, 3);
        }

        [Fact]
        public void Extraction_Computes_Aggregate_SendRates_Per_GroupBy()
        {
            TCPExtractor extractor = new TCPExtractor();
            ETWExtract extract = CreateExtract();

            const string Remote = "20.20.20.20:40000";
            const string SrcA = "10.10.10.10:100";
            const string SrcB = "10.10.10.10:101";

            StaticTraceProcessorContext.MetaData = new TraceMetaDataMock();

            // Connection A sends 100+100 bytes at 0.2s/0.3s => burst 200 B / 0.1s = 2000 B/s
            // Connection B sends 300+300 bytes at 1.0s/1.1s => burst 600 B / 0.1s = 6000 B/s
            // Both share source IP and target IP, so TargetSourceIP combines them:
            //   weighted = (2000*200 + 6000*600) / (200+600) = 5000 B/s
            IGenericEvent[] events = new IGenericEvent[]
            {
                CreateConnect(   Time.T0_1, Pid.OneDrive, TCB.One, SrcA, Remote),
                CreatePost(      Time.T0_2, Pid.OneDrive, TCB.One, SequenceNr.S_1000, 100),
                CreateSend(      Time.T0_2, Pid.OneDrive, TCB.One, SequenceNr.S_1000, 100),
                CreatePost(      Time.T0_3, Pid.OneDrive, TCB.One, SequenceNr.S_2000, 100),
                CreateSend(      Time.T0_3, Pid.OneDrive, TCB.One, SequenceNr.S_2000, 100),
                CreateDisconnect(Time.T0_5, Pid.OneDrive, TCB.One, SrcA, Remote),

                CreateConnect(   Time.T0_9, Pid.OneDrive, TCB.One, SrcB, Remote),
                CreatePost(      Time.T1_0, Pid.OneDrive, TCB.One, SequenceNr.S_1000, 300),
                CreateSend(      Time.T1_0, Pid.OneDrive, TCB.One, SequenceNr.S_1000, 300),
                CreatePost(      Time.T1_1, Pid.OneDrive, TCB.One, SequenceNr.S_2000, 300),
                CreateSend(      Time.T1_1, Pid.OneDrive, TCB.One, SequenceNr.S_2000, 300),
                CreateDisconnect(Time.T1_5, Pid.OneDrive, TCB.One, SrcB, Remote),
            };

            extractor.ExtractFromGenericEvents(extract, events);
            IETWExtract iExtract = extract;

            Assert.Equal(2, iExtract.Network.TcpData.Connections.Count);
            ITcpConnection a = iExtract.Network.TcpData.Connections.Single(x => x.LocalIpAndPort.ToString() == SrcA);
            ITcpConnection b = iExtract.Network.TcpData.Connections.Single(x => x.LocalIpAndPort.ToString() == SrcB);

            // per connection burst rates
            Assert.Equal(2000.0d, a.Statistics.AverageSendRate.Value, 3);
            Assert.Equal(6000.0d, b.Statistics.AverageSendRate.Value, 3);

            // TargetIP groups by source port so it equals the per connection rate
            Assert.Equal(2000.0d, a.Statistics.AggregatedSendRateBySourceIPPortTargetIP.Value, 3);
            Assert.Equal(6000.0d, b.Statistics.AggregatedSendRateBySourceIPPortTargetIP.Value, 3);

            // TargetSourceIP combines both connections (same source and target IP)
            Assert.Equal(5000.0d, a.Statistics.AggregatedSendRateBySourceIPTargetIP.Value, 3);
            Assert.Equal(5000.0d, b.Statistics.AggregatedSendRateBySourceIPTargetIP.Value, 3);
        }

        [Fact]
        public void WeightedAverageBurstRate_Returns_Null_For_Less_Than_Two_Events()
        {
            Assert.Null(TCPExtractor.GetWeightedAverageBurstRate(new (DateTimeOffset, long)[0]));
            Assert.Null(TCPExtractor.GetWeightedAverageBurstRate(new[] { Ev(0, 1000) }));
        }

        [Fact]
        public void WeightedAverageBurstRate_Single_Burst_Is_Bytes_Divided_By_Duration()
        {
            // two events 100ms apart, 100 + 200 bytes => 300 bytes / 0.1s = 3000 B/s
            double? rate = TCPExtractor.GetWeightedAverageBurstRate(new[] { Ev(0.0, 100), Ev(0.1, 200) });
            Assert.Equal(3000.0d, rate.Value, 3);
        }

        [Fact]
        public void WeightedAverageBurstRate_Splits_Bursts_On_Gap_Above_350ms_And_Weights_By_Bytes()
        {
            // Burst A: t0, t0.1 with 100+100 bytes => 2000 B/s weight 200
            // gap of 900ms (> 350ms) starts a new burst
            // Burst B: t1.0, t1.1 with 300+300 bytes => 6000 B/s weight 600
            // weighted average = (2000*200 + 6000*600) / 800 = 5000 B/s
            double? rate = TCPExtractor.GetWeightedAverageBurstRate(new[]
            {
                Ev(0.0, 100), Ev(0.1, 100),
                Ev(1.0, 300), Ev(1.1, 300),
            });
            Assert.Equal(5000.0d, rate.Value, 3);
        }

        [Fact]
        public void WeightedAverageBurstRate_Ignores_Single_Event_Bursts()
        {
            // isolated event at t2.0 (gap > 350ms on both sides) has no duration and is ignored,
            // only the first burst t0..t0.1 with 100+100 bytes => 2000 B/s contributes
            double? rate = TCPExtractor.GetWeightedAverageBurstRate(new[]
            {
                Ev(0.0, 100), Ev(0.1, 100),
                Ev(2.0, 5000),
            });
            Assert.Equal(2000.0d, rate.Value, 3);
        }

        [Fact]
        public void WeightedAverageBurstRate_Gap_Exactly_350ms_Stays_In_Same_Burst()
        {
            // gap of exactly 350ms does not split the burst => 100+100+100 bytes / 0.7s
            double? rate = TCPExtractor.GetWeightedAverageBurstRate(new[]
            {
                Ev(0.0, 100), Ev(0.35, 100), Ev(0.70, 100),
            });
            Assert.Equal(300.0d / 0.70d, rate.Value, 3);
        }
    }
}
