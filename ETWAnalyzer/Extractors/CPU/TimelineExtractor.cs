using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.CPU;
using Microsoft.Windows.EventTracing.Cpu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extractors.CPU
{
    internal class TimelineExtractor
    {
        /// <summary>
        /// Interval how long we sum up CPU time
        /// </summary>
        public float ExtractionInveralS { get; }

        /// <summary>
        /// ETW Session start time
        /// </summary>
        public DateTimeOffset SessionStart { get; }

        /// <summary>
        /// ETW Session duration
        /// </summary>
        public TimeSpan SessionDuration { get; }

        /// <summary>
        /// Number of entries the timeline has depending on ETW session duration.
        /// </summary>
        internal decimal[] myTimelineBuckets;

        /// <summary>
        /// Resulting timeline data which is stored to ETWExtract
        /// </summary>
        public CPUTimeLine Timeline  { get; }

        public TimelineExtractor(float extractionInveralS, DateTimeOffset sessionStart, TimeSpan sessionDuration)
        {
            ExtractionInveralS = extractionInveralS;
            SessionStart = sessionStart;
            SessionDuration = sessionDuration;

            // Create from session start until end the array which can be used to chart the data
            myTimelineBuckets = new decimal[(int)(SessionDuration.TotalSeconds / ExtractionInveralS) + 1];

            Timeline = new CPUTimeLine(ExtractionInveralS);
        }

        /// <summary>
        /// Add a CPU sample to timeline data
        /// </summary>
        /// <param name="process">Process to add</param>
        /// <param name="sample">CPU sample ETW event</param>
        public void AddSample(ProcessKey process, ICpuSample sample)
        {
            DateTimeOffset sampleTime = sample.Timestamp.DateTimeOffset;

            if (!Timeline.ProcessTimeLines.TryGetValue(process, out Extract.CPU.ProcessTimeLine processTimeLine))
            {
                processTimeLine = new Extract.CPU.ProcessTimeLine();
                processTimeLine.CPUMs.AddRange(myTimelineBuckets);
                Timeline.ProcessTimeLines[process] = processTimeLine;
            }

            processTimeLine.CPUMs[GetBucket(sampleTime)] += sample.Weight.TotalMilliseconds;
        }

        /// <summary>
        /// Determine which bucket we need to increment
        /// </summary>
        /// <param name="time">CPU sampling event time</param>
        /// <returns>Index to CPU array which contains aggregated CPU timings in <see cref="ExtractionInveralS"/> intervals.</returns>
        internal int GetBucket(DateTimeOffset time)
        {
            // first int cast is needed to get fractionless time in seconds (e.g. 0.5/1.0 would be 1 but the first bucket has index 0
            // second int cast will give then true bucket number
            return (int) (((int)(time - SessionStart).TotalSeconds) / ExtractionInveralS);
        }
    }
}
