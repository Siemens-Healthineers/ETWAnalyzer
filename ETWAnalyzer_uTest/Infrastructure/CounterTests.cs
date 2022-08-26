//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Infrastructure;
using Xunit;

namespace ETWAnalyzer_uTest.Infrastructure
{
    public class CounterTests
    {
        [Fact]
        public void Empty_Count_Has_ZeroLength_Counts()
        {
            var counter = new Counter<string>();
            Assert.Empty(counter.Counts);
        }

        [Fact]
        public void Count_IsPreserved_After_One_Increment()
        {
            Counter<string> counter = new Counter<string>();
            counter.Increment("Test");
            Assert.Equal(1, counter["Test"]);
            Assert.Single(counter.Counts);
        }

        [Fact]
        public void Count_IsPreserved_After_Two_Increment()
        {
            Counter<string> counter = new Counter<string>();
            counter.Increment("Test");
            counter.Increment("Test");
            Assert.Equal(2, counter["Test"]);
            Assert.Single(counter.Counts);
        }
    }
}
