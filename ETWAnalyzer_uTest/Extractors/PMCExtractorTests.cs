using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.PMC;
using ETWAnalyzer.Extractors;
using ETWAnalyzer.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest.Extractors
{
    public class PMCExtractorTests
    {
        [Fact]
        public void Can_Serialize_DeserializePMCData()
        {
            ETWExtract extract = new();

            extract.Processes.Add(new ETWProcess()
            {
                ProcessID = 100,
                ProcessName = "test.exe",
            });

            extract.PMC.Counters = new List<PMCCounter>
            { 
                new PMCCounter()
                {
                    CounterName = "SampleCounter",
                    ProcessMap = new Dictionary<ETWProcessIndex, ulong>
                    {
                        {   0, 500 },
                    },
                }
            };

            // Check compile types that we access only interfaces
            IETWExtract iextract = extract;
            IPMCData data = iextract.PMC;
            IPMCCounter counter = data.Counters[0];
            IReadOnlyDictionary<ETWProcessIndex, ulong> map = counter.ProcessMap;


            MemoryStream stream = new();
            ExtractSerializer.Serialize(stream, extract);
            stream.Position = 0;
            ETWExtract deserialized = ExtractSerializer.Deserialize<ETWExtract>(stream);


            Assert.NotNull(deserialized.PMC.Counters);
            Assert.Single(deserialized.PMC.Counters);
            Assert.Equal("SampleCounter", deserialized.PMC.Counters[0].CounterName);
            Assert.Equal(500u, deserialized.PMC.Counters[0].ProcessMap[ 0]);
        }
    }
}
