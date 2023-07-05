using ETWAnalyzer.EventDump;
using static ETWAnalyzer.EventDump.DumpTcp;
using ETWAnalyzer.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest.EventDump
{
    public class DumpTcpTest
    {
        [Fact]
        public void MinMaxConnectionDurationS()
        {
            DumpTcp dump = new();
            MatchData empty = new();

            Assert.True(dump.MinMaxFilter(empty));
            empty.Connection.TimeStampOpen.Value.AddSeconds(05);
            empty.Connection.TimeStampClose.Value.AddSeconds(10);
            Assert.True(dump.MinMaxFilter(empty));

            dump.MinMaxConnectionDurationS = new MinMaxRange<double>(30, 60);
            Assert.False(dump.MinMaxFilter(empty));
            empty.Connection.TimeStampOpen.Value.AddSeconds(1);
            Assert.True(dump.MinMaxFilter(empty));

        }
    }
}

