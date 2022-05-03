using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAU.Toolkit.Diagnostics.Profiling.Simplified;
using Xunit;

namespace ETWAnalyzer_uTest.Extract
{
    public class OutputFileNameTests
    {
        [Fact]
        public void Can_Parse_Valid_Name()
        {
            var name = OutputFileName.ParseFromFileName("Load_2382ms_RN1884F4EB-386C_SRV_TestStatus-Passed_20220425-215857.7z");
            Assert.NotNull(name);

            Assert.Equal(2382, name.TestDurationinMS);
            Assert.Equal("Load", name.TestCaseName);
            Assert.Equal("RN1884F4EB-386C", name.MachineWhereResultsAreGeneratedOn);
            Assert.Equal(TestStatus.Passed, name.TestStatus);
            Assert.Equal(new DateTime(2022, 04, 25, 21, 58, 57), name.ProfilingStoppedTime);
        }

        [Fact]
        public void Invalid_FileName_Returns_Null()
        {
            var name = OutputFileName.ParseFromFileName("Load_2382asdfdasRN1884F4EB-386C_SRV_TestStatus-Passed_20220425-215857.7z");
            Assert.Null(name);
        }

        [Fact]
        public void HostName_Can_Contain_Underscores()
        {
            var name = OutputFileName.ParseFromFileName("Load_2624ms_PERF_100K20H2-V_CLT_TestStatus-Passed_20220425-213643.7z");
            Assert.NotNull(name);

            Assert.Equal(2624, name.TestDurationinMS);
            Assert.Equal("Load", name.TestCaseName);
            Assert.Equal("PERF_100K20H2-V", name.MachineWhereResultsAreGeneratedOn);
            Assert.Equal(TestStatus.Passed, name.TestStatus);
            Assert.Equal(new DateTime(2022, 04, 25, 21, 36, 43), name.ProfilingStoppedTime);

        }
    }
}
