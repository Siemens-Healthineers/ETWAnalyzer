using ETWAnalyzer.EventDump;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Network;
using ETWAnalyzer_uTest.TestInfrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ETWAnalyzer_uTest.EventDump
{
    public class DumpDnsTests
    {
        private ITestOutputHelper myWriter;

        const string Exe1 = "Exe1.exe";
        const string Exe1Arg = "-noarg";
        const int Exe1Pid = 1000;


        public DumpDnsTests(ITestOutputHelper myWriter)
        {
            this.myWriter = myWriter;
        }

        [Fact]
        public void Print_All_Without_Filter()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);

            var dumper = new DumpDns()
            {
                ShowDetails = true,
            };

            dumper.myUTestData = CreateTestData();
            var printed = dumper.ExecuteInternal();
            Assert.Equal(4, printed.Count);
        }

        [Fact]
        public void Can_Filter_LongerThan1s_Queries()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);

            var dumper = new DumpDns()
            {
                MinMaxTimeMs = new ETWAnalyzer.Infrastructure.MinMaxRange<double>(1, null),
                ShowDetails = true,
            };

            dumper.myPreloadedTests = new Lazy<SingleTest>[] { CreateDnsTestData() };
            var printed = dumper.ExecuteInternal();
            Assert.Single(printed);

        }

        [Fact]
        public void Can_Filter_LongerThanTotals2s()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);

            var dumper = new DumpDns()
            {
                MinMaxTotalTimeMs = new ETWAnalyzer.Infrastructure.MinMaxRange<double>(2, null),
                ShowDetails = true,
            };

            dumper.myPreloadedTests = new Lazy<SingleTest>[] { CreateDnsTestData() };
            var printed = dumper.ExecuteInternal();
            testOutput.Flush();
            var lines = testOutput.Messages[0].Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries); 
            Assert.Equal(8, lines.Length);
            Assert.Contains("Totals: 2.000 s Dns query time for 3 Dns queries", lines[7]);

        }

        Lazy<SingleTest>  CreateDnsTestData()
        {
            var process1 = new ETWProcess
            {
               CmdLine = Exe1Arg,
               ProcessID = Exe1Pid,
               ProcessName = Exe1,
            };

            ETWExtract extract = new ETWExtract
            {
               Processes = new List<ETWProcess>
               { 
                   process1,
               },
               Network = new Network()
               {
                   DnsClient = new DnsClient()
                   {
                       Events = new List<DnsEvent>
                       {
                           new DnsEvent
                           {
                               Duration = TimeSpan.Zero,
                               ProcessIdx = 0,
                               Process = process1,
                               Query = "Server1",
                           },
                           new DnsEvent
                           {
                               Duration = TimeSpan.FromMilliseconds(500),
                               ProcessIdx = 0,
                               Process = process1,
                               Query = "Server1",
                           },
                           new DnsEvent
                           {
                               Duration = TimeSpan.FromMilliseconds(2000),
                               ProcessIdx = 0,
                               Process = process1,
                               Query = "Server1",
                           },
                           new DnsEvent
                           {
                               Duration = TimeSpan.FromMilliseconds(150),
                               ProcessIdx = 0,
                               Process = process1,
                               Query = "Server2",
                           },

                       }
                   }
               },

            };

            TestDataFile file = new TestDataFile("Test1", "C:\\temp\\Test1.json", new DateTime(2000, 1, 1), 500, 1, "TestMachine", null)
            {
                Extract = extract,
            };

            SingleTest  singleTest = new SingleTest(new TestDataFile[] { file });

            return new Lazy<SingleTest>(singleTest);
        }

        List<DumpDns.MatchData> CreateTestData(int n = 1)
        {
            ETWProcess process1 = new ETWProcess
            {
                CmdLine = Exe1Arg,
                ProcessID = Exe1Pid,
                ProcessName = Exe1,
            };

            TestDataFile file = new TestDataFile("DnsTest", "C:\\temp\\DnsTest.json", new DateTime(2000, 1, 1), 500, 10, "TestAgent", "");


            List<DumpDns.MatchData> lret = new List<DumpDns.MatchData>()
            {
                new DumpDns.MatchData
                {
                    File = file,
                    Process = process1,
                    Dns = new DnsEvent
                    {
                        Process = process1,
                        Duration = TimeSpan.Zero, 
                        Query = "Server1",
                    },
                },
                new DumpDns.MatchData
                {
                    File = file,
                    Process = process1,
                    Dns = new DnsEvent
                    {
                        Process = process1,
                        Duration = TimeSpan.FromMilliseconds(500),
                        Query = "Server1",
                    },
                },
                new DumpDns.MatchData
                {
                    File = file,
                    Process = process1,
                    Dns = new DnsEvent
                    {
                        Process = process1,
                        Duration = TimeSpan.FromMilliseconds(2000),
                        Query = "Server1",
                    },
                },

                new DumpDns.MatchData
                {
                    File = file,
                    Process = process1,
                    Dns = new DnsEvent
                    {
                        Process = process1,
                        Duration = TimeSpan.FromMilliseconds(150),
                        Query = "Server2",
                    },
                },


            };

            return lret;
        }
    }
}
