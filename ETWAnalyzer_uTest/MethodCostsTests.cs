//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest
{
    public class MethodCostsTests
    {
        readonly MethodCost myZero = new MethodCost((MethodIndex)0, 0, 0, 0.0m, 0.0m, 0, 0);
        readonly MethodCost myHuge = new MethodCost((MethodIndex)1_000_000, 1_123_456_789, 2_123_456_789, 0.12345m, 9999.12345m, 12345, 33);

        [Fact]
        public void CanRead_ZeroValue()
        {
            var zeroStr = myZero.ToStringForSerialize();
            MethodCost deser = MethodCost.FromString(zeroStr);
            Compare(myZero, deser);
        }

        [Fact]
        public void CanRead_LargeValue()
        {
            var hugeStr = myHuge.ToStringForSerialize();
            MethodCost deser = MethodCost.FromString(hugeStr);
            Compare(myHuge, deser);
        }

        [Fact]
        public void Can_Read_Current_Format()
        {
            // this is myHuge serialized to check if, when MethodCost gets additional fields we still can read
            // the old data
            const string serialized = "1000000 1123456789 2123456789 0.1235 9999.1230 12345";
            var cost = MethodCost.FromString(serialized);

            var oldHuge = new MethodCost((MethodIndex)1_000_000, 1_123_456_789, 2_123_456_789, 0.12345m, 9999.12345m, 12345, 0);
            Compare(oldHuge, cost);
        }

        void Compare(MethodCost expected, MethodCost test)
        {
            Assert.Equal(expected.CPUMs, test.CPUMs);
            Assert.Equal((double)expected.FirstOccurenceInSecond, (double)test.FirstOccurenceInSecond, 4);
            Assert.Equal((double)expected.LastOccurenceInSecond, (double)test.LastOccurenceInSecond, 4);
            Assert.Equal(expected.MethodIdx, test.MethodIdx);
            Assert.Equal(expected.Threads, test.Threads);
            Assert.Equal(expected.WaitMs, test.WaitMs);
            Assert.Equal(expected.DepthFromBottom, test.DepthFromBottom);
        }

        [Fact(
#pragma warning disable xUnit1004 // Test methods should not be skipped
           Skip = "Perf Test" // comment this line to get perf data
#pragma warning restore xUnit1004 // Test methods should not be skipped
            )]
        public void Can_Deserialize_Data_Performance()
        {
            var hugeStr = myHuge.ToStringForSerialize();

            var sw = Stopwatch.StartNew();
            for (int i=0;i<1_000_000;i++)
            {
                MethodCost tmp = MethodCost.FromString(hugeStr);
            }
            sw.Stop();
            Assert.True(false, $"Did take {sw.Elapsed.TotalSeconds:F2}s: str: {hugeStr}");
        }
    }
}
