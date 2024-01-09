using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.CPU;
using ETWAnalyzer.Extract.CPU.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest.Extract
{
    public class CPUFrequencyTests
    {
        [Fact]
        public void Can_Add_And_GetFrequencies()
        {
            var frequencies = new CPUExtended();

            float startTime = 1.0f;

            frequencies.AddFrequencyDuration(0, startTime, startTime + 1, 5000);
            frequencies.AddFrequencyDuration(0, startTime + 3, startTime + 15, 2000);
            frequencies.AddFrequencyDuration(0, startTime + 2, startTime + 3, 3000);
            frequencies.AddFrequencyDuration(0, startTime + 1, startTime + 2, 4000);

            Assert.Equal(5000, frequencies.GetFrequency(0, startTime));
            Assert.Equal(4000, frequencies.GetFrequency(0, startTime + 1.500f));
            Assert.Equal(3000, frequencies.GetFrequency(0, startTime + 3.000f));
            Assert.Equal(2000, frequencies.GetFrequency(0, startTime + 5.000f));
            Assert.Equal(-1, frequencies.GetFrequency(0, startTime + 50.000f));
        }

        [Fact] 
        public void Process_MethodIndex_Works()
        {
            ETWProcessIndex idx = (ETWProcessIndex) 1000;
            MethodIndex method = (MethodIndex) 15000;

            ProcessMethodIdx merged = idx.Create(method);
            Assert.Equal(15000, (int) merged.MethodIndex());
            Assert.Equal(1000, (int) merged.ProcessIndex());
        }
    }
}
