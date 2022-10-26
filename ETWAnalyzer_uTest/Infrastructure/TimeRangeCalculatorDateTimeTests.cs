using ETWAnalyzer.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest.Infrastructure
{
    public class TimeRangeCalculatorDateTimeTests
    {
            [Fact]
            public void EmptyList()
            {
                TimeRangeCalculatorDateTime calc = new TimeRangeCalculatorDateTime();
                Assert.Equal(TimeSpan.Zero, calc.GetDuration());
            }

            [Fact]
            public void Single_Value()
            {
                TimeRangeCalculatorDateTime calc = new TimeRangeCalculatorDateTime();
                calc.Add(new DateTime(1_000_000), TimeSpan.FromTicks(100));
                Assert.Equal(100, calc.GetDuration().Ticks);
            }

            [Fact]
            public void Same_Values_Count_Only_Once()
            {
                TimeRangeCalculatorDateTime calc = new TimeRangeCalculatorDateTime();

                calc.Add(new DateTime(1_000_000), TimeSpan.FromTicks(100));
                calc.Add(new DateTime(1_000_000), TimeSpan.FromTicks(100));
                calc.Add(new DateTime(1_000_000), TimeSpan.FromTicks(100));

                Assert.Equal(100, calc.GetDuration().Ticks);
            }

            [Fact]
            public void Overlapping_Values_Are_CorrectlyCounted()
            {
                TimeRangeCalculatorDateTime calc = new TimeRangeCalculatorDateTime();

                calc.Add(new DateTime(1_000_000), TimeSpan.FromTicks(100_000));
                calc.Add(new DateTime(1_000_300), TimeSpan.FromTicks(100_000));
                calc.Add(new DateTime(1_000_400), TimeSpan.FromTicks(10_000));
                calc.Add(new DateTime(1_100_000), TimeSpan.FromTicks(100_000));

                Assert.Equal(200_000, calc.GetDuration().Ticks);
                Assert.Equal(20.0d, calc.GetDuration().TotalMilliseconds);
            }

            [Fact]
            public void MultiRanges_Are_Correctly_Counted()
            {
                TimeRangeCalculatorDateTime calc = new TimeRangeCalculatorDateTime();

                calc.Add(new DateTime(1_000_000), TimeSpan.FromTicks(100_000));
                calc.Add(new DateTime(1_100_000), TimeSpan.FromTicks(100_000));

                calc.Add(new DateTime(2_000_000), TimeSpan.FromTicks(100_000));
                calc.Add(new DateTime(3_000_000), TimeSpan.FromTicks(100_000));


                Assert.Equal(400_000, calc.GetDuration().Ticks);
                Assert.Equal(40.0d, calc.GetDuration().TotalMilliseconds);
            }

    }
}
