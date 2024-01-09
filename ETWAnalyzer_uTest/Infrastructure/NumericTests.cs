using ETWAnalyzer.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest.Infrastructure
{
    public class NumericTests
    {
        [Fact]
        public void PercentileEmptyList()
        {
            List<float> values = [];
            Assert.Equal(0, values.Percentile(0.5f));
        }

        [Fact]
        public void PercentileTwoItems_Zero()
        {
            List<float> floats = [1, 2];
            Assert.Equal(1, floats.Percentile(0));
            Assert.Equal(2, floats.Percentile(1));
        }

        [Fact]
        public void Percentile_Interpolate()
        {
            List<float> floats = [1, 2];
            Assert.Equal(1.5f, floats.Percentile(0.5f));
        }
    }
}
