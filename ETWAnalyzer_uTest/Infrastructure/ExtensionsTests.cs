//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using ETWAnalyzer.Infrastructure;

namespace ETWAnalyzer_uTest.Infrastructure
{

    public class ExtensionsTests
    {
        [Fact]
        public void Can_Cut_Null()
        {
            string str = null;
            Assert.Null(str.CutMinMax(0, 10));
        }

        [Fact]
        public void Can_Cut_Substring()
        {
            string str = "abcde";
            Assert.Equal("...bcd", str.CutMinMax(1, 3));
        }

        [Fact]
        public void Can_Cut_Substring_Too_LargeStart()
        {
            string str = "abcde";
            Assert.Equal("...e", str.CutMinMax(4, 1));
            Assert.Equal("...e", str.CutMinMax(5, 1));
            Assert.Equal("...e", str.CutMinMax(6, 1));
            Assert.Equal("...e", str.CutMinMax(7, 1));
        }

        [Fact]
        public void Can_Cut_Substring_Too_Large_Length()
        {
            string str = "abcde";
            Assert.Equal("abcd", str.CutMinMax(0, 4));
            Assert.Equal("abcde", str.CutMinMax(0, 5));
            Assert.Equal("abcde", str.CutMinMax(0, 6));
        }

        [Fact]
        public void Can_Cut_End()
        {
            string str = "abcde";
            Assert.Equal("", str.CutMinMax(0, 0));
            Assert.Equal("...e", str.CutMinMax(0, -1));
            Assert.Equal("...de", str.CutMinMax(0, -2));
            Assert.Equal("...cde", str.CutMinMax(0, -3));
            Assert.Equal("...bcde", str.CutMinMax(0, -4));
            Assert.Equal("abcde", str.CutMinMax(0, -5));
            Assert.Equal("abcde", str.CutMinMax(0, -6));
        }


        [Fact]
        public void Can_GetMinMax()
        {
            string str = "-15";
            Assert.Equal(new KeyValuePair<int, int>(0, 15), str.GetMinMax());
        }

        [Fact]
        public void GetMinMax_Max_Can_Be_Negative()
        {
            string str = "-15";
            Assert.Equal(new KeyValuePair<int, int>(0, -15), str.GetMinMax(true));
        }

        [Fact]
        public void GetMinMax_Min_Only_Present()
        {
            string str = "15";
            Assert.Equal(new KeyValuePair<int, int>(15, int.MaxValue), str.GetMinMax());
        }

        [Fact]
        public void GetMinMax_Both()
        {
            string str = "15-30";
            Assert.Equal(new KeyValuePair<int, int>(15, 30), str.GetMinMax());
        }

        [Fact]
        public void ETWMaxBy()
        {
            int[] x = new int[] { 5, 4, 3, 2, 1 };
            Assert.Equal(5, x.ETWMaxBy(x => x));
            Assert.Equal(5, x.Reverse().ETWMaxBy(x => x));
        }

        [Fact]
        public void ETWMinBy()
        {
            int[] x = new int[] { 5, 4, 3, 2, 1 };
            Assert.Equal(1, x.ETWMinBy(x => x));
            Assert.Equal(1, x.Reverse().ETWMinBy(x => x));
        }
    }
}
