using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Infrastructure
{
    internal class TimeRangeCalculatorDateTime
    {
        List<KeyValuePair<DateTime, TimeSpan>> myTimeRanges = new List<KeyValuePair<DateTime, TimeSpan>>();

        /// <summary>
        /// Add a timepoint with a TimeSpan which will be used to calculate the total TimeSpan
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="timeSpan"></param>
        public void Add(DateTimeOffset startTime, TimeSpan timeSpan)
        {
            myTimeRanges.Add(new KeyValuePair<DateTime, TimeSpan>(startTime.Date, timeSpan));
        }

        /// <summary>
        /// Add a timepoint with a TimeSpan which will be used to calculate the total TimeSpan
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="timeSpan"></param>
        public void Add(DateTime startTime, TimeSpan timeSpan)
        {
            myTimeRanges.Add(new KeyValuePair<DateTime, TimeSpan>(startTime, timeSpan));
        }

        /// <summary>
        /// The TimeSpan is the sume of all time ranges where overlaps in the time ranges count only once.
        /// </summary>
        /// <returns>Total TimeSpan</returns>
        public TimeSpan GetDuration()
        {
            List<KeyValuePair<DateTime, TimeSpan>> sorted = myTimeRanges.OrderBy(x => x.Key).ToList();
            DateTime totalTimeSpan = DateTime.MinValue;

            DateTime previousEndTime = DateTime.MinValue;

            for (int i = 0; i < sorted.Count; i++)
            {
                var current = sorted[i];
                if (previousEndTime >= current.Key)
                {
                    if (previousEndTime.Ticks > current.Key.Ticks + current.Value.Ticks)
                    {
                        // ignore this one
                    }
                    else
                    {
                        long timeSpanns = current.Value.Ticks - (previousEndTime.Ticks - current.Key.Ticks);
                        totalTimeSpan += new TimeSpan(timeSpanns);
                        DateTime newEndtime = current.Key + current.Value;
                        previousEndTime = newEndtime;
                    }
                }
                else
                {
                    totalTimeSpan += current.Value;
                    previousEndTime = new DateTime(current.Key.Ticks + current.Value.Ticks);
                }
            }

            return TimeSpan.FromTicks(totalTimeSpan.Ticks);
        }
    }
}
