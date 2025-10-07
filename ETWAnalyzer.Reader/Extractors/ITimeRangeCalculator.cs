using System;

namespace ETWAnalyzer.Extractors
{
    public interface ITimeRangeCalculator
    {
        void Freeze();
        long GetAverage();
        TimeSpan GetDuration();
    }
}
