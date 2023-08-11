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

        
        static readonly DateTimeOffset connect_0s = new DateTimeOffset(2023, 8, 11, 10, 05, 00, TimeSpan.Zero);
        static readonly DateTimeOffset connect_5s = connect_0s + TimeSpan.FromSeconds(5);
        static readonly DateTimeOffset connect_10s = connect_0s + TimeSpan.FromSeconds(10);
        static readonly DateTimeOffset connect_11s = connect_0s + TimeSpan.FromSeconds(11);


        [Fact]
        public void MinMaxConnectionDurationFilterTest_Match()
        {
            DumpTcp tcpDumper = new();
            
            tcpDumper.MinMaxConnectionDurationS = new MinMaxRange<double>(0, 10);

            Assert.True(tcpDumper.MinMaxConnectionDurationFilter(connect_0s, connect_5s));

            Assert.True(tcpDumper.MinMaxConnectionDurationFilter(connect_0s, connect_10s));

        }

        [Fact]
        public void MinMaxConnectionDurationFilterTest_NoMatch()
        {
            DumpTcp tcpDumper = new();

            tcpDumper.MinMaxConnectionDurationS = new MinMaxRange<double>(0, 10);

            Assert.False(tcpDumper.MinMaxConnectionDurationFilter(connect_0s, connect_11s));
            Assert.Equal(11, connect_11s.Second - connect_0s.Second);

        }

    }
}

