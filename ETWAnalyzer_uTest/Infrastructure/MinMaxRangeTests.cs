//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest.Infrastructure
{
    public class MinMaxRangeTests
    {
        [Fact]
        public void NoBoundaryMatchesEverything()
        {
            MinMaxRange<int> range = new MinMaxRange<int>(null, null);
            Assert.Null(range.Min);
            Assert.Null(range.Max);

            Assert.True(range.IsWithin(0));
            Assert.True(range.IsWithin(-1));
            Assert.True(range.IsWithin(1));
        }

        [Fact]
        public void Lower_Boundary_Defined()
        {
            MinMaxRange<int> range = new MinMaxRange<int>(0, null);
            Assert.Equal(0, range.Min);

            Assert.True(range.IsWithin(0));
            Assert.False(range.IsWithin(-1));
            Assert.True(range.IsWithin(1));
        }

        [Fact]
        public void Upper_Boundary_Defined()
        {
            MinMaxRange<int> range = new MinMaxRange<int>(null, 10);
            Assert.Equal(10, range.Max);

            Assert.True(range.IsWithin(0));
            Assert.True(range.IsWithin(-1));
            Assert.True(range.IsWithin(1));
            Assert.True(range.IsWithin(10));

            Assert.False(range.IsWithin(11));
        }

        [Fact]
        public void Upper_And_Lower_Boundary()
        {
            MinMaxRange<int> range = new MinMaxRange<int>(-10, 10);
            Assert.Equal(-10, range.Min);
            Assert.Equal(10, range.Max);

            Assert.True(range.IsWithin(0));
            Assert.True(range.IsWithin(-1));
            Assert.True(range.IsWithin(1));

            Assert.True(range.IsWithin(10));
            Assert.True(range.IsWithin(-10));
            Assert.False(range.IsWithin(11));
            Assert.False(range.IsWithin(-11));
        }
    }
}
