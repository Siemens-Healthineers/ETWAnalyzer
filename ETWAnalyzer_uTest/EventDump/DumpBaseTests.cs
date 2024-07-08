//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.EventDump;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static ETWAnalyzer.EventDump.DumpBase;

namespace ETWAnalyzer_uTest.EventDump
{

    class Dump : DumpBase
    {
        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }

    public class DumpBaseTests
    {
        [Fact]
        public void CanFormatUTCTimeDate()
        {
            var dmp = new Dump();
            var time         = new DateTimeOffset(2000, 1, 1, 1, 0, 0, new TimeSpan(5, 0, 0));
            var sessionStart = new DateTimeOffset(2000, 1, 1, 0, 56, 0, new TimeSpan(5, 0, 0));

            string utcDateTime = dmp.GetDateTimeString(time, sessionStart, TimeFormats.UTC);
            Assert.Equal("1999-12-31 20:00:00.000", utcDateTime);

            string utcTime = dmp.GetDateTimeString(time, sessionStart, TimeFormats.UTCTime);
            Assert.Equal("20:00:00.000", utcTime);
        }

        [Fact]
        public void CanFormatLocalTimeDate()
        {
            var dmp = new Dump();
            var time         = new DateTimeOffset(2000, 1, 1, 1, 0,  0, new TimeSpan(5, 0, 0));
            var sessionStart = new DateTimeOffset(2000, 1, 1, 0, 56, 0, new TimeSpan(5, 0, 0));

            string localDateTime = dmp.GetDateTimeString(time, sessionStart, TimeFormats.Local);
            Assert.Equal("2000-01-01 01:00:00.000", localDateTime);

            string localTime = dmp.GetDateTimeString(time, sessionStart, TimeFormats.LocalTime);
            Assert.Equal("01:00:00.000", localTime);
        }

        [Fact]
        public void CanFormatHereTimeDate()
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            
            var dmp = new Dump();
            TimeSpan utcOffset = TimeSpan.FromHours(5);
            var time         = new DateTimeOffset(2000, 1, 1, 1, 0,  0, utcOffset);
            var sessionStart = new DateTimeOffset(2000, 1, 1, 0, 56, 0, utcOffset);

            string hereDateTime = dmp.GetDateTimeString(time, sessionStart, TimeFormats.Here);

            var lTime = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2000, 1, 1, 1, 0, 0) - utcOffset, TimeZoneInfo.Local);

            Assert.Equal(lTime.ToString(DumpBase.DateTimeFormat3), hereDateTime);

            string hereTime = dmp.GetDateTimeString(time, sessionStart, TimeFormats.HereTime);
            Assert.Equal(lTime.ToString(DumpBase.TimeFormat3), hereTime);
        }

        [Fact]
        public void CanFormatSinceSessionStart()
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            var dmp = new Dump();
            var time         = new DateTimeOffset(2000, 1, 1, 1, 0,  0, new TimeSpan(5, 0, 0));
            var sessionStart = new DateTimeOffset(2000, 1, 1, 0, 56, 0, new TimeSpan(5, 0, 0));

            string s1 = dmp.GetDateTimeString(time, sessionStart, TimeFormats.s);
            string s2 = dmp.GetDateTimeString(time, sessionStart, TimeFormats.second);
            Assert.Equal(s1, s2);
            Assert.Equal("240.000", s2);
        }

        [Fact]
        public void NotExistingSessionStartTime()
        {
            var dmp = new Dump();
            TimeSpan dtoOffset = TimeSpan.FromHours(5);
            var time = new DateTimeOffset(2000, 1, 1, 1, 0, 0, dtoOffset);

            string hereSeconds = dmp.GetDateTimeString(time, default, TimeFormats.s);
            Assert.Equal("63082267200.000", hereSeconds);
        }

    }
}
