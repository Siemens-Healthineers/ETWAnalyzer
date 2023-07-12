using ETWAnalyzer.EventDump;
using System;
using Xunit;
using ETWAnalyzer.Commands;

namespace ETWAnalyzer_uTest.EventDump
{
    public class DumpTcpTests
    {
        [Fact]
        public void MinMaxConnectionDurationSFilterTest()
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
                var args = new string[] { "-dump", "TCP", "-MinMaxConnectionDurationS", input.Item1, input.Item2};
                DumpCommand dump = (DumpCommand)CommandFactory.CreateCommand(args);
                dump.Parse();
                dump.Run();
                DumpTcp tcpDumper = (DumpTcp)dump.myCurrentDumper;

                Assert.Equal(input.Item3.Item1, tcpDumper.MinMaxConnectionDurationS.Min);
                Assert.Equal(input.Item3.Item2, tcpDumper.MinMaxConnectionDurationS.Max);

            }

        }

    }
}

