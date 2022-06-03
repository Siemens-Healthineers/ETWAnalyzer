//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using System;
using Xunit;

namespace ETWAnalyzer_uTest.Infrastructure
{
    public class TimeRangeCalculatorTests
    {
        [Fact]
        public void EmptyList()
        {
            TimeRangeCalculator calc = new TimeRangeCalculator();
            Assert.Equal(TimeSpan.Zero, calc.GetDuration());
        }

        [Fact]
        public void Single_Value()
        {
            TimeRangeCalculator calc = new TimeRangeCalculator();
            calc.Add(new Timestamp(1_000_000), new Duration(100));
            Assert.Equal(1, calc.GetDuration().Ticks);
        }

        [Fact]
        public void Same_Values_Count_Only_Once()
        {
            TimeRangeCalculator calc = new TimeRangeCalculator();

            calc.Add(new Timestamp(1_000_000), new Duration(100));
            calc.Add(new Timestamp(1_000_000), new Duration(100));
            calc.Add(new Timestamp(1_000_000), new Duration(100));

            Assert.Equal(1, calc.GetDuration().Ticks);
        }

        [Fact]
        public void Overlapping_Values_Are_CorrectlyCounted()
        {
            TimeRangeCalculator calc = new TimeRangeCalculator();

            calc.Add(new Timestamp(1_000_000), new Duration(100_000));
            calc.Add(new Timestamp(1_000_300), new Duration(100_000));
            calc.Add(new Timestamp(1_000_400), new Duration(10_000));
            calc.Add(new Timestamp(1_100_000), new Duration(100_000));

            Assert.Equal(2000, calc.GetDuration().Ticks);
            Assert.Equal(0.2d, calc.GetDuration().TotalMilliseconds);
        }

        [Fact]
        public void MultiRanges_Are_Correctly_Counted()
        {
            TimeRangeCalculator calc = new TimeRangeCalculator();

            calc.Add(new Timestamp(1_000_000), new Duration(100_000));
            calc.Add(new Timestamp(1_100_000), new Duration(100_000));

            calc.Add(new Timestamp(2_000_000), new Duration(100_000));
            calc.Add(new Timestamp(3_000_000), new Duration(100_000));


            Assert.Equal(4000, calc.GetDuration().Ticks);
            Assert.Equal(0.4d, calc.GetDuration().TotalMilliseconds);

        }

    }
}
