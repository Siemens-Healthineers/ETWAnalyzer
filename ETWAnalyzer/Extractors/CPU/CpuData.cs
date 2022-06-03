//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extractors
{
    internal class CpuData
    {
        /// <summary>
        /// Contains CPU data summed across all threads
        /// </summary>
        public Duration CpuInMs
        {
            get;
            set;
        }

        /// <summary>
        /// Number of used Sample profiling events processed. Used mainly for debugging here 
        /// </summary>
        public int CpuInMsCount;

        /// <summary>
        /// Contains all wait times from all threads. It is used to calculate from all threads the non overlapping wait time.
        /// </summary>
        public TimeRangeCalculator WaitTimeRange {  get;  set; } = new TimeRangeCalculator();

        /// <summary>
        /// Relative time in seconds since Trace Start
        /// </summary>
        public decimal FirstOccurrenceSeconds
        {
            get;
            set;
        } = decimal.MaxValue;

        /// <summary>
        /// Relative time in seconds since Trace Start
        /// </summary>
        public decimal LastOccurrenceSeconds
        {
            get;
            set;
        }

        /// <summary>
        /// Unique thread Ids this method was running on
        /// </summary>
        public HashSet<int> ThreadIds
        {
            get;
        } = new HashSet<int>();

        /// <summary>
        /// Get for each sample the call stack depth from the bottom frame
        /// </summary>
        public List<ushort> DepthFromBottom
        {
            get;
        } = new List<ushort>();

        /// <summary>
        /// Contains a merged view of overlapping time range.
        /// We use this to calculate the overall Ready time across all threads, where overlapping times are counted only once
        /// </summary>
        public TimeRangeCalculator ReadyTimeRange { get; internal set; } = new TimeRangeCalculator();

        /// <summary>
        /// Number of used Context Switch Events. Mainly used for debugging purposes
        /// </summary>
        public int WaitMsCount;


        internal CpuData()
        {
        }

        /// <summary>
        /// Needed for unit testing only 
        /// </summary>
        /// <param name="cpuMs"></param>
        /// <param name="waitMs"></param>
        /// <param name="firstOccurrence"></param>
        /// <param name="lastOccurrence"></param>
        /// <param name="threadCount"></param>
        /// <param name="depthFromBottom"></param>
        internal CpuData(Duration cpuMs, Duration waitMs, decimal firstOccurrence, decimal lastOccurrence, int threadCount, ushort depthFromBottom)
        {
            CpuInMs = cpuMs;
            WaitTimeRange.Add(Timestamp.Zero, waitMs);
            FirstOccurrenceSeconds = firstOccurrence;
            LastOccurrenceSeconds = lastOccurrence;
            for (int i = 1; i <= threadCount; i++)
            {
                ThreadIds.Add(i);
            }

            DepthFromBottom.AddRange(Enumerable.Repeat<ushort>(depthFromBottom, 5));

        }
    }
}
