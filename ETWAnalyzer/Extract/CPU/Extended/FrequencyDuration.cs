//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System.Collections.Generic;

namespace ETWAnalyzer.Extract.CPU.Extended
{

    /// <summary>
    /// Contains sampled frequency for a time range for one core.
    /// </summary>
    public struct FrequencyDuration : IComparer<FrequencyDuration>
    {
        /// <summary>
        /// Average CPU Frequency in MHz during that time range.
        /// </summary>
        public int FrequencyMHz { get; set; }

        /// <summary>
        /// Start time
        /// </summary>
        public float StartS { get; set; }

        /// <summary>
        /// End time 
        /// </summary>
        public float EndS { get; set; }

        /// <summary>
        /// Use for BinarySearch where as input one duration has same Start/End timings.
        /// Two durations are equal if the one with same Start/End time is in the range of the other range where Start/End.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(FrequencyDuration x, FrequencyDuration y)
        {
            if (x.StartS == x.EndS)
            {
                if (x.StartS >= y.StartS &&
                    x.StartS <= y.EndS)
                {
                    return 0;
                }
                else
                    return x.StartS.CompareTo(y.StartS);
            }
            else
            {
                if (y.StartS >= x.StartS &&
                    y.StartS <= x.EndS)
                {
                    return 0;
                }
                else
                    return x.StartS.CompareTo(y.StartS);
            }
        }
    }
}
