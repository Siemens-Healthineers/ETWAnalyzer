using ETWAnalyzer.EventDump;
using System;
using Xunit;
using ETWAnalyzer.Commands;
using static ETWAnalyzer.EventDump.DumpTcp;
using ETWAnalyzer.Extract.Network.Tcp;
using ETWAnalyzer.Infrastructure;
using System.Collections.Generic;
using Xunit.Abstractions;
using ETWAnalyzer.Extract;

namespace ETWAnalyzer_uTest.EventDump
{
    public class DumpTcpTests
    {
        [Fact]
        public void MinMaxConnectionDurationSTest()
        {
            Tuple<string, string, Tuple<double, double>>[] RangeValues = new Tuple<string, string, Tuple<double, double>>[]
                {
                    new Tuple<string, string, Tuple<double, double>>("1", "5", new Tuple<double, double>(1, 5)),
                    new Tuple<string, string, Tuple<double, double>>("1s", "1000s", new Tuple<double, double>(1, 1000)),
                    new Tuple<string, string, Tuple<double, double>>("1000ms", "2000ms", new Tuple<double, double>(1, 2)),
                    new Tuple<string, string, Tuple<double, double>>("1000ms", "2000s", new Tuple<double, double>(1, 2000)),
                };

            foreach (var input in RangeValues)
            {
                var args = new string[] { "-dump", "TCP", "-MinMaxConnectionDurationS", input.Item1, input.Item2 };
                DumpCommand dump = (DumpCommand)CommandFactory.CreateCommand(args);
                dump.Parse();
                dump.Run();
                DumpTcp tcpDumper = (DumpTcp)dump.myCurrentDumper;

                Assert.Equal(input.Item3.Item1, tcpDumper.MinMaxConnectionDurationS.Min);
                Assert.Equal(input.Item3.Item2, tcpDumper.MinMaxConnectionDurationS.Max);

            }

        }

        [Theory]
        [InlineData("SourceIpPortRemoteIp")]
        [InlineData("SourceIpRemoteIp")]
        public void GroupBy_Argument_Is_Parsed(string value)
        {
            var args = new string[] { "-dump", "TCP", "-GroupBy", value };
            DumpCommand dump = (DumpCommand)CommandFactory.CreateCommand(args);
            dump.Parse();
            dump.Run();
            DumpTcp tcpDumper = (DumpTcp)dump.myCurrentDumper;

            Assert.Equal(value, tcpDumper.GroupBy.ToString());
        }

        public List<string> Messages { get; set; } = new List<string>();

        private ITestOutputHelper myWriter;

        public DumpTcpTests(ITestOutputHelper myWriter)
        {
            this.myWriter = myWriter;
        }

        internal void Add(string message)
        {
            Messages.Add(message);
        }

        
        static readonly DateTimeOffset connect_0s = new DateTimeOffset(2023, 8, 11, 10, 05, 00, TimeSpan.Zero);
        static readonly DateTimeOffset connect_5s = connect_0s + TimeSpan.FromSeconds(5);
        static readonly DateTimeOffset connect_10s = connect_0s + TimeSpan.FromSeconds(10);
        static readonly DateTimeOffset connect_11s = connect_0s + TimeSpan.FromSeconds(11);


        [Fact]
        public void MinMaxConnectionDurationFilterTest_Match()
        {
            DumpTcp tcpDumper = new();
            
            tcpDumper.MinMaxConnectionDurationS = new MinMaxRange<double>(0, 10);

            Assert.True(tcpDumper.MinMaxConnectionDurationFilter(connect_0s, connect_5s, DateTimeOffset.MaxValue));

            Assert.True(tcpDumper.MinMaxConnectionDurationFilter(connect_0s, connect_10s, DateTimeOffset.MaxValue));

        }

        [Fact]
        public void MinMaxConnectionDurationFilterTest_NoMatch()
        {
            DumpTcp tcpDumper = new();

            tcpDumper.MinMaxConnectionDurationS = new MinMaxRange<double>(0, 10);

            Assert.False(tcpDumper.MinMaxConnectionDurationFilter(connect_0s, connect_11s, DateTimeOffset.MaxValue));

        }

        [Fact]
        public void MinMaxConnectionDurationFilterTest()
        {
            DumpTcp tcpDumper = new();

            Assert.True(tcpDumper.MinMaxConnectionDurationFilter(connect_0s, connect_11s, DateTimeOffset.MaxValue));
        }

        [Fact]
        public void MinMaxConnectionDuration_NoEndTime_UsesSessionEnd()
        {
            DumpTcp tcpDumper = new();
            tcpDumper.MinMaxConnectionDurationS = new MinMaxRange<double>(10, null);

            Assert.True(tcpDumper.MinMaxConnectionDurationFilter(connect_0s, null, connect_11s));
            Assert.False(tcpDumper.MinMaxConnectionDurationFilter(connect_0s, null, connect_5s));

        }

        static MatchData CreateMatch(string remoteAddress, int remotePort, ulong bytesSent, ulong bytesReceived, int datagramsSent, int datagramsReceived, double? sendRate, double? receiveRate, int localPort = 5000,
            double? aggSendTargetIP = null, double? aggReceiveTargetIP = null, double? aggSendTargetSourceIP = null, double? aggReceiveTargetSourceIP = null)
        {
            TcpConnectionStatistics stats = new(null, null, null, null, null, null, null, null, null, null, null, null, sendRate, receiveRate,
                aggSendTargetIP, aggReceiveTargetIP, aggSendTargetSourceIP, aggReceiveTargetSourceIP);
            TcpConnection connection = new(0, new SocketConnection("10.0.0.1", localPort), new SocketConnection(remoteAddress, remotePort),
                null, null, null, bytesSent, datagramsSent, bytesReceived, datagramsReceived, ETWProcessIndex.Invalid, null, stats);

            return new MatchData
            {
                Connection = connection,
                Session = new ETWSession { FileName = "test.json7z" },
            };
        }

        [Fact]
        public void AggregateByTargetIP_GroupsByRemoteIP_Regardless_Of_Port()
        {
            DumpTcp dumper = new() { GroupBy = DumpCommand.GroupByModes.SourceIpPortRemoteIp };

            // aggregated rates are precomputed during extraction and shared by all connections of a group
            MatchData m1 = CreateMatch("1.2.3.4", 443, bytesSent: 100, bytesReceived: 1000, datagramsSent: 2, datagramsReceived: 4, sendRate: 10, receiveRate: 20, aggSendTargetIP: 25, aggReceiveTargetIP: 35);
            MatchData m2 = CreateMatch("1.2.3.4", 8080, bytesSent: 300, bytesReceived: 3000, datagramsSent: 6, datagramsReceived: 8, sendRate: 30, receiveRate: 40, aggSendTargetIP: 25, aggReceiveTargetIP: 35);
            MatchData m3 = CreateMatch("5.6.7.8", 443, bytesSent: 50, bytesReceived: 500, datagramsSent: 1, datagramsReceived: 3, sendRate: 5, receiveRate: 6, aggSendTargetIP: 5, aggReceiveTargetIP: 6);

            List<MatchData> result = dumper.AggregateConnections(new[] { m1, m2, m3 });

            Assert.Equal(2, result.Count);

            MatchData g1 = result.Find(x => x.Connection.RemoteIpAndPort.Address == "1.2.3.4");
            Assert.True(g1.IsAggregate);
            Assert.Equal(2, g1.InputConnectionCount);
            Assert.Equal(400UL, g1.Connection.BytesSent);
            Assert.Equal(4000UL, g1.Connection.BytesReceived);
            Assert.Equal(8, g1.Connection.DatagramsSent);
            Assert.Equal(12, g1.Connection.DatagramsReceived);

            // source port is preserved, target port is collapsed to 0
            Assert.Equal(5000, g1.Connection.LocalIpAndPort.Port);
            Assert.Equal(0, g1.Connection.RemoteIpAndPort.Port);

            // the row shows the aggregate rate computed during extraction (TargetIP)
            Assert.Equal(25.0d, g1.Connection.Statistics.AverageSendRate.Value, 3);
            Assert.Equal(35.0d, g1.Connection.Statistics.AverageReceiveRate.Value, 3);

            MatchData g2 = result.Find(x => x.Connection.RemoteIpAndPort.Address == "5.6.7.8");
            Assert.Equal(1, g2.InputConnectionCount);
            Assert.Equal(50UL, g2.Connection.BytesSent);
        }

        [Fact]
        public void AggregateByTargetIP_Different_Source_Ports_Are_Not_Merged()
        {
            DumpTcp dumper = new() { GroupBy = DumpCommand.GroupByModes.SourceIpPortRemoteIp };

            MatchData m1 = CreateMatch("1.2.3.4", 443, bytesSent: 100, bytesReceived: 1000, datagramsSent: 2, datagramsReceived: 4, sendRate: 10, receiveRate: 20, localPort: 40000);
            MatchData m2 = CreateMatch("1.2.3.4", 8080, bytesSent: 300, bytesReceived: 3000, datagramsSent: 6, datagramsReceived: 8, sendRate: 30, receiveRate: 40, localPort: 40001);

            List<MatchData> result = dumper.AggregateConnections(new[] { m1, m2 });

            // same remote IP but different source ports must stay separate
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Connection.LocalIpAndPort.Port == 40000);
            Assert.Contains(result, x => x.Connection.LocalIpAndPort.Port == 40001);
        }

        [Fact]
        public void AggregateByTargetSourceIP_Merges_Different_Source_And_Target_Ports()
        {
            DumpTcp dumper = new() { GroupBy = DumpCommand.GroupByModes.SourceIpRemoteIp };

            MatchData m1 = CreateMatch("1.2.3.4", 443, bytesSent: 100, bytesReceived: 1000, datagramsSent: 2, datagramsReceived: 4, sendRate: 10, receiveRate: 20, localPort: 40000, aggSendTargetSourceIP: 42, aggReceiveTargetSourceIP: 84);
            MatchData m2 = CreateMatch("1.2.3.4", 8080, bytesSent: 300, bytesReceived: 3000, datagramsSent: 6, datagramsReceived: 8, sendRate: 30, receiveRate: 40, localPort: 40001, aggSendTargetSourceIP: 42, aggReceiveTargetSourceIP: 84);
            MatchData m3 = CreateMatch("5.6.7.8", 443, bytesSent: 50, bytesReceived: 500, datagramsSent: 1, datagramsReceived: 3, sendRate: 5, receiveRate: 6, localPort: 40002, aggSendTargetSourceIP: 5, aggReceiveTargetSourceIP: 6);

            List<MatchData> result = dumper.AggregateConnections(new[] { m1, m2, m3 });

            // 1.2.3.4 connections merge regardless of source and target port
            Assert.Equal(2, result.Count);
            MatchData g1 = result.Find(x => x.Connection.RemoteIpAndPort.Address == "1.2.3.4");
            Assert.Equal(2, g1.InputConnectionCount);
            Assert.Equal(400UL, g1.Connection.BytesSent);
            // both source and target ports are collapsed
            Assert.Equal(0, g1.Connection.LocalIpAndPort.Port);
            Assert.Equal(0, g1.Connection.RemoteIpAndPort.Port);

            // the row shows the aggregate rate computed during extraction (TargetSourceIP)
            Assert.Equal(42.0d, g1.Connection.Statistics.AverageSendRate.Value, 3);
            Assert.Equal(84.0d, g1.Connection.Statistics.AverageReceiveRate.Value, 3);
        }

    }
}

