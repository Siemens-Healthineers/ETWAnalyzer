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

        static readonly SocketConnection ipRemPort = new("123.205.91.10", 10000);
        static readonly SocketConnection ipSrcPort = new("123.456.789.10", 100);

        static readonly DateTimeOffset timeStampOpen = new DateTimeOffset(2023, 8, 11, 10, 05, 00, TimeSpan.Zero);
        static readonly DateTimeOffset timeStampClose = new DateTimeOffset(2023, 8, 11, 10, 05, 00, TimeSpan.Zero);
        static readonly DateTimeOffset timeStampClose1 = new DateTimeOffset(2023, 8, 11, 10, 05, 10, TimeSpan.Zero);
        static readonly DateTimeOffset timeStampClose2 = new DateTimeOffset(2023, 8, 11, 10, 05, 20, TimeSpan.Zero);
        static readonly TcpConnection tcpConnection = new(123, ipRemPort, ipSrcPort, timeStampOpen, timeStampClose, "", 100, 120, 100, 100, ETWProcessIndex.Invalid);
        static readonly TcpConnection tcpConnection1 = new(456, ipRemPort, ipSrcPort, timeStampOpen, timeStampClose1, "", 100, 120, 100, 100, ETWProcessIndex.Invalid);
        static readonly TcpConnection tcpConnection2 = new(789, ipRemPort, ipSrcPort, timeStampOpen, timeStampClose2, "", 100, 120, 100, 100, ETWProcessIndex.Invalid);
        
        

        List<DumpTcp.MatchData> CreateTestData()
        {
            List<DumpTcp.MatchData> data = new List<DumpTcp.MatchData>
            { 
                new DumpTcp.MatchData
                {
                    Connection = tcpConnection,
                },
                new DumpTcp.MatchData
                {
                    Connection = tcpConnection1,
                },
                new DumpTcp.MatchData
                {
                    Connection = tcpConnection2,
                }
            };

            return data;
        }

        [Fact]
        public void MinMaxConnectionDurationFilterTest()
        {
            DumpTcp tcpDumper = new();
            
            var data = CreateTestData();
         
            tcpDumper.MinMaxConnectionDurationS = new MinMaxRange<double>(0, 10);

            Assert.True(tcpDumper.MinMaxConnectionDurationFilter(data[0].Connection.TimeStampOpen, data[0].Connection.TimeStampClose));
            Assert.Equal(0, data[0].Connection.TimeStampClose.Value.Second - data[0].Connection.TimeStampOpen.Value.Second);

            Assert.True(tcpDumper.MinMaxConnectionDurationFilter(data[1].Connection.TimeStampOpen, data[1].Connection.TimeStampClose));
            Assert.Equal(10, data[1].Connection.TimeStampClose.Value.Second - data[1].Connection.TimeStampOpen.Value.Second);

            Assert.False(tcpDumper.MinMaxConnectionDurationFilter(data[2].Connection.TimeStampOpen, data[2].Connection.TimeStampClose));
            Assert.Equal(20, data[2].Connection.TimeStampClose.Value.Second - data[2].Connection.TimeStampOpen.Value.Second);

        }

    }
}

