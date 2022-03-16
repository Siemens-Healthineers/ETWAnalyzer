//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ETWAnalyzer_uTest.Infrastructure
{
    public class SorterExtensionTests
    {
        private readonly ITestOutputHelper output;

        public SorterExtensionTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        void Print(int[] topn)
        {
            foreach (var n in topn)
            {
                output.WriteLine($"{n}");
            }
        }

        int[] Create(int start, int count)
        {
            return Enumerable.Range(start, count).Reverse().ToArray();
        }

        [Fact]
        public void SortNumbersTopNLast_MinMax_None()
        {
            int[] topn = Create(1,20).SortAscendingGetTopNLast(x => x, null, new SkipTakeRange());
            Print(topn);

            Assert.Equal(20, topn.Length);
            Assert.Equal(1, topn[0]);
            Assert.Equal(20, topn[19]);
        }

        [Fact]
        public void SortNumbersTopNLast_MinMax_Take10_Skip0()
        {
            int[] topn = Create(1, 20).SortAscendingGetTopNLast(x => x, null, new SkipTakeRange(10, null));
            Print(topn);

            Assert.Equal(10, topn.Length);
            Assert.Equal(11, topn[0]);
            Assert.Equal(20, topn[9]);
        }

        [Fact]
        public void SortNumbersTopNLast_MinMax_Take1_Skip1()
        {
            int[] topn = Create(1, 20).SortAscendingGetTopNLast(x => x, null, new SkipTakeRange(1, 1));
            Print(topn);

            Assert.Single(topn);
            Assert.Equal(19, topn[0]);
        }

        [Fact]
        public void SortNumbersTopNLast_MinMax_Take1_Skip0()
        {
            int[] topn = Create(1, 20).SortAscendingGetTopNLast(x => x, null, new SkipTakeRange(1, 0));
            Print(topn);

            Assert.Single(topn);
            Assert.Equal(20, topn[0]);
        }


        [Fact]
        public void SortNumbersTopNLast_MinMax_Take10_Skip5()
        {
            int[] topn = Create(1, 20).SortAscendingGetTopNLast(x => x, null, new SkipTakeRange(10, 5));
            Print(topn);
        }
    }
}
