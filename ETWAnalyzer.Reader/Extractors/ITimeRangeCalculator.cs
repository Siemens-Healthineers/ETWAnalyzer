using System;

namespace ETWAnalyzer.Extractors
{
    internal interface ITimeRangeCalculator
    {
        void Freeze();
        long GetAverage();
        TimeSpan GetDuration();
    }
}
