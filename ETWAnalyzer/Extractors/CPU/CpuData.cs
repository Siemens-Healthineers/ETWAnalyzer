//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

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
        public Duration CpuInMs
        {
            get;
            set;
        }

        /// <summary>
        /// Number of used Sample profiling events processed. Used mainly for debugging here 
        /// </summary>
        public int CpuInMsCount;

        public Duration WaitMs
        {
            get;
            set;
        }

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
            WaitMs = waitMs;
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
