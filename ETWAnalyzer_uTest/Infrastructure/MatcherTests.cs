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
    public class MatcherTests
    {
        [Fact]
        public void Can_Create_Filter_From_Null()
        {
            Func<string, bool> matcher = Matcher.CreateMatcher(null, MatchingMode.CaseInsensitive);
            Assert.True(matcher(""));
            Assert.True(matcher(null));
            Assert.True(matcher("abc"));
        }

        [Fact]
        public void Empty_Filter_Str_Passes_All()
        {
            Func<string, bool> matcher = Matcher.CreateMatcher("", MatchingMode.CaseInsensitive);
            Assert.True(matcher(""));
            Assert.True(matcher(null));
            Assert.True(matcher("abc"));
        }

        [Fact]
        public void SinglePositiveFilter()
        {
            Func<string, bool> matcher = Matcher.CreateMatcher("abc", MatchingMode.CaseInsensitive);
            Assert.False(matcher(""));
            Assert.False(matcher(null));
            Assert.False(matcher("a"));
            Assert.False(matcher("ab"));
            Assert.False(matcher("aabc"));
            Assert.False(matcher("abcc"));
            Assert.True(matcher("abc"));
        }

        [Fact]
        public void SinglePositiveFilter_Wildcard_Start()
        {
            Func<string, bool> matcher = Matcher.CreateMatcher("*abc", MatchingMode.CaseInsensitive);
            Assert.False(matcher(""));
            Assert.False(matcher(null));
            Assert.False(matcher("a"));
            Assert.False(matcher("ab"));
            Assert.True(matcher("aabc"));
            Assert.False(matcher("abcc"));
            Assert.True(matcher("abc"));
        }

        [Fact]
        public void SinglePositiveFilter_Wildcard_End()
        {
            Func<string, bool> matcher = Matcher.CreateMatcher("abc*", MatchingMode.CaseInsensitive);
            Assert.False(matcher(""));
            Assert.False(matcher(null));
            Assert.False(matcher("a"));
            Assert.False(matcher("ab"));
            Assert.False(matcher("aabc"));
            Assert.True(matcher("abcc"));
            Assert.True(matcher("abc"));
        }

        [Fact]
        public void SinglePositiveFilter_Wildcard_Middle()
        {
            Func<string, bool> matcher = Matcher.CreateMatcher("a*bc", MatchingMode.CaseInsensitive);
            Assert.False(matcher(""));
            Assert.False(matcher(null));
            Assert.False(matcher("a"));
            Assert.False(matcher("ab"));
            Assert.True(matcher("aabc"));
            Assert.False(matcher("abcc"));
            Assert.True(matcher("abc"));
        }

        [Fact]
        public void SinglePositiveFilter_MultiWildCards()
        {
            Func<string, bool> matcher = Matcher.CreateMatcher("*a*bc*", MatchingMode.CaseInsensitive);
            Assert.False(matcher(""));
            Assert.False(matcher(null));
            Assert.False(matcher("a"));
            Assert.False(matcher("ab"));
            Assert.True(matcher("aabc"));
            Assert.True(matcher("abcc"));
            Assert.True(matcher("abc"));
            Assert.True(matcher("xxxaxxxxbc"));
            Assert.True(matcher("xxxaxxxxbcxxxx"));
        }

        [Fact]
        public void Single_Negative_Filter()
        {
            Func<string, bool> matcher = Matcher.CreateMatcher("!abc", MatchingMode.CaseInsensitive);
            Assert.True(matcher(""));
            Assert.True(matcher(null));
            Assert.True(matcher("a"));
            Assert.True(matcher("ab"));
            Assert.True(matcher("aabc"));
            Assert.True(matcher("abcc"));
            Assert.False(matcher("abc"));
        }

        [Fact]
        public void Negative_Filter_Wins()
        {
            var matcher = Matcher.CreateMatcher("*;!*", MatchingMode.CaseInsensitive);
            Assert.False(matcher(""));
            Assert.False(matcher(null));
            Assert.False(matcher("x"));
        }

        [Fact]
        public void No_Positive_Filter_One_Neg()
        {
            var matcher = Matcher.CreateMatcher("!*x*", MatchingMode.CaseInsensitive);
            Assert.True(matcher(null));
            Assert.True(matcher(""));
            Assert.True(matcher("abc"));
            Assert.False(matcher("abxc"));
        }

        [Fact]
        public void Positive_And_Negative_Filter()
        {
            var matcher = Matcher.CreateMatcher("a.*.exe;!*av*", MatchingMode.CaseInsensitive);
            Assert.False(matcher(null));
            Assert.False(matcher(""));
            Assert.True(matcher("a..exe"));
            Assert.True(matcher("a.alsdkfja;.exe"));
            Assert.False(matcher("a.al av sdkfja;.exe"));
        }

        [Fact]
        public void Positive_And_Negative_FilterPidFormat_Omitted_Exe()
        {
            var matcher = Matcher.CreateMatcher("abc;!1234", MatchingMode.CaseInsensitive, pidFilterFormat:true);
            Assert.False(matcher(null));
            Assert.False(matcher(""));
            Assert.True(matcher("abc.exe"));
            Assert.True(matcher("abc"));
            Assert.False(matcher("abc "));
            Assert.True(matcher("abc.exe(12345)"));
            Assert.False(matcher("abc.exe(1234)"));
        }

        [Fact]
        public void ProcessFilter_Inclusive_Matches()
        {
            var matcher = Matcher.CreateMatcher("1234", MatchingMode.CaseInsensitive, pidFilterFormat: true);
            Assert.False(matcher(null));
            Assert.False(matcher(""));
            Assert.False(matcher("cmd.exe"));
            Assert.False(matcher("cmd.exe(12345)"));
            Assert.True(matcher("cmd.exe(1234)"));
            Assert.True(matcher("cmd(1234)"));
        }

        [Fact]
        public void ProcessFilter_Exclusive_Matches()
        {
            var matcher = Matcher.CreateMatcher("!1234", MatchingMode.CaseInsensitive, pidFilterFormat:true);
            Assert.True(matcher(null));
            Assert.True(matcher(""));
            Assert.True(matcher("cmd.exe"));
            Assert.True(matcher("cmd.exe(12345)"));
            Assert.False(matcher("cmd.exe(1234)"));

        }
    }
}