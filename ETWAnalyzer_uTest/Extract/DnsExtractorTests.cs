using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Network;
using ETWAnalyzer.Extractors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ETWAnalyzer_uTest.Extract
{
    public class DnsExtractorTests
    {

        private readonly ITestOutputHelper myWriter;

        public DnsExtractorTests(ITestOutputHelper writer)
        {
            myWriter = writer;
        }

        [Fact]
        public void Can_Serialize_Deserialize_DnsExtract()
        {
            ETWExtract extract = new ETWExtract
            {
                Processes = new List<ETWProcess>
                {
                    new ETWProcess()
                    {
                        ProcessID = 1,
                        ProcessName = "test.exe",
                    },
                                        new ETWProcess()
                    {
                        ProcessID = 2,
                        ProcessName = "test2.exe",
                    },
                },
                Network = new Network()
                {
                    DnsClient = new DnsClient()
                    {
                        Events = new List<DnsEvent>
                        {
                            new DnsEvent()
                            {
                                ProcessIdx = (ETWProcessIndex) 1,
                                Duration = TimeSpan.FromSeconds(10),
                                Start = new DateTimeOffset(2000,1,1,1,1,1,TimeSpan.Zero),
                                Query = "www.google.com",
                                Result = "1.1.1.1",
                                TimedOut = true,
                                Adapters = "Ethernet",
                                ServerList = "8.8.8.8",
                            }
                        }
                    }
                }
            };

            var stream = new MemoryStream();
            ExtractSerializer.Serialize<ETWExtract>(stream, extract);

            stream.Position = 0;

            string str = Encoding.UTF8.GetString(stream.ToArray());
            myWriter.WriteLine($"Serialized: {str}");
            ETWExtract deser = ExtractSerializer.Deserialize<ETWExtract>(stream);

            Verify(deser.Network);
        }

        private void Verify(INetwork network)
        {
            Assert.Single(network.DnsClient.Events);
            IDnsEvent ev = network.DnsClient.Events[0];

            Assert.Equal((ETWProcessIndex)1, ev.ProcessIdx);
            Assert.Equal(TimeSpan.FromSeconds(10), ev.Duration);
            Assert.Equal(new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero), ev.Start);
            Assert.Equal("www.google.com", ev.Query);
            Assert.Equal("1.1.1.1", ev.Result);
            Assert.True(ev.TimedOut);
            Assert.Equal("Ethernet", ev.Adapters);
            Assert.Equal("8.8.8.8", ev.ServerList);
        }
    }
}
