//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;

namespace ETWAnalyzer.Extract.CPU.Extended
{
    /// <summary>
    /// This class contains the sampled CPU frequencies for a single CPU core which are serialized in a compact format into Json 
    /// </summary>
    public class FrequencySource
    {
        /// <summary>
        /// List of start times where a frequency duration start.
        /// </summary>
        public List<float> StartTimesS { get; set; } = new();

        /// <summary>
        /// List of end times when a frequency duration ends.
        /// </summary>
        public List<float> EndTimesS { get; set; } = new();

        /// <summary>
        /// List of frequencyes which are the averaged values between each [start;end] times.
        /// </summary>
        public List<int> AverageFrequencyMHz { get; set; } = new();

        /// <summary>
        /// Expand data structure from flat list into something easier accessible when read.
        /// </summary>
        List<FrequencyDuration> myDurations;

        /// <summary>
        /// Round flaot to 4 decimal places which is by far enough precision since the CPU frequency is sampled only every 15-30ms.
        /// </summary>
        /// <param name="number">number to round.</param>
        /// <returns>rounded number to 4 decimal places.</returns>
        float Round(float number)
        {
            return (float)Math.Round(number, 4, MidpointRounding.AwayFromZero);
        }

        internal void Add(float startTimeS, float endTimeS, int frequencyMHz)
        {
            StartTimesS.Add(Round(startTimeS));
            EndTimesS.Add(Round(endTimeS));
            AverageFrequencyMHz.Add(frequencyMHz);
        }

        internal List<FrequencyDuration> GetSortedList()
        {
            List<FrequencyDuration> durations = new List<FrequencyDuration>();
            for (int i = 0; i < StartTimesS.Count; i++)
            {
                durations.Add(new FrequencyDuration()
                {
                    StartS = StartTimesS[i],
                    EndS = EndTimesS[i],
                    FrequencyMHz = AverageFrequencyMHz[i],
                }
                );
            }

            durations.Sort((a, b) => a.StartS.CompareTo(b.StartS));

            return durations;
        }

        /// <summary>
        /// Get for a given core the sampled CPU Frequency at a given time which is present in ETL when Microsoft-Windows-Kernel-Processor-Power provider is enabled.
        /// </summary>
        /// <param name="timeS">Time in WPA trace Time in seconds since Session start for which you want to get the current time.</param>
        /// <returns>Average CPU Frequency in MHz which was sampled in 15-30ms time slices.</returns>
        internal int GetFrequency(float timeS)
        {
            if (myDurations == null)
            {
                myDurations = GetSortedList();
            }

            int lret = -1;


            var search = new FrequencyDuration
            {
                StartS = timeS,
                EndS = timeS,
            };

            int idx = myDurations.BinarySearch(search, search);
            if (idx >= 0)
            {
                lret = myDurations[idx].FrequencyMHz;
            }

            return lret;
        }


    }
}
