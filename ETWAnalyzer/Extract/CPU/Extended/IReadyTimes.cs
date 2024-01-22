//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Newtonsoft.Json;
using System.Collections.Generic;

namespace ETWAnalyzer.Extract.CPU.Extended
{
    /// <summary>
    /// Ready details which contains percentiles for CPU deep sleep states, Ready percentiles for process interference along with count and sum for total delay summed accross all threads inside one process for a given method in method inclusive ready time.
    /// </summary>
    public interface IReadyTimes
    {
        /// <summary>
        /// true when DeepSleep ready times are present. 
        /// </summary>
        bool HasDeepSleepTimes { get; }

        /// <summary>
        /// true when non DeepSleep ready times are present
        /// </summary>
        bool HasNonDeepSleepTimes { get; }

        /// <summary>
        /// Number of context switch events.
        /// Context Switch events did originate from idle or own process in Windows C-State 1 which allows to measure CPU unpark time without other process interference. 
        /// </summary>
        int CSwitchCountDeepSleep { get; }

        /// <summary>
        /// Number of context switch events which did not originate from an DeepSleep thread.
        /// </summary>
        int CSwitchCountNonDeepSleep { get; }

        /// <summary>
        /// Minimum Ready time in microseconds.
        /// </summary>
        double MinNonDeepSleepUs { get; }

        /// <summary>
        /// Minimum Ready time in microseconds.
        /// Context Switch events did originate from idle or own process in Windows C-State 1 which allows to measure CPU unpark time without other process interference. 
        /// </summary>
        double MinDeepSleepUs { get; }


        /// <summary>
        /// Maximum Ready time in microseconds.
        /// </summary>
        double MaxNonDeepSleepUs { get; }

        /// <summary>
        /// Maximum Ready time in microseconds.
        /// Context Switch events did originate from idle or own process in Windows C-State 1 which allows to measure CPU unpark time without other process interference. 
        /// </summary>
        double MaxDeepSleepUs { get; }


        /// <summary>
        /// 5% Percentile Ready time in microseconds.
        /// </summary>
        double Percentile5NonDeepSleepUs { get; }

        /// <summary>
        /// 5% Percentile Ready time in microseconds.
        /// Context Switch events did originate from idle or own process in Windows C-State 1 which allows to measure CPU unpark time without other process interference. 
        /// </summary>
        double Percentile5DeepSleepUs { get; }


        /// <summary>
        /// 25% Percentile Ready time in microseconds.
        /// Context Switch events did originate from idle or own process in Windows C-State 1 which allows to measure CPU unpark time without other process interference. 
        /// </summary>
        double Percentile25DeepSleepUs { get; }

        /// <summary>
        /// 25% Percentile Ready time in microseconds.
        /// </summary>
        double Percentile25NonDeepSleepUs { get; }


        /// <summary>
        /// Median = 50% Percentile Ready time in microseconds.
        /// Context Switch events did originate from idle or own process in Windows C-State 1 which allows to measure CPU unpark time without other process interference. 
        /// </summary>
        double Percentile50DeepSleepUs { get; }


        /// <summary>
        /// Median = 50% Percentile Ready time in microseconds.
        /// </summary>
        double Percentile50NonDeepSleepUs { get; }

        /// <summary>
        /// 90% Percentile Ready time in microseconds.
        /// </summary>
        double Percentile90NonDeepSleepUs { get; }

        /// <summary>
        /// 90% Percentile Ready time in microseconds.
        /// Context Switch events did originate from idle or own process in Windows C-State 1 which allows to measure CPU unpark time without other process interference. 
        /// </summary>
        double Percentile90DeepSleepUs { get; }


        /// <summary>
        /// 95% Percentile Ready time in microseconds.
        /// </summary>
        double Percentile95NonDeepSleepUs { get; }

        /// <summary>
        /// 95% Percentile Ready time in microseconds.
        /// Context Switch events did originate from idle or own process in Windows C-State 1 which allows to measure CPU unpark time without other process interference. 
        /// </summary>
        double Percentile95DeepSleepUs { get; }


        /// <summary>
        /// 99% Percentile Ready time in microseconds.
        /// </summary>
        double Percentile99NonDeepSleepUs { get; }

        /// <summary>
        /// 99% Percentile Ready time in microseconds.
        /// Context Switch events did originate from idle or own process in Windows C-State 1 which allows to measure CPU unpark time without other process interference. 
        /// </summary>
        double Percentile99DeepSleepUs { get; }

        /// <summary>
        /// Sum of DeepSleep Ready time in microseconds.
        /// Context Switch events did originate from idle or own process in Windows C-State 1 which allows to measure CPU unpark time without other process interference. 
        /// </summary>
        public double SumDeepSleepUs { get; }

        /// <summary>
        /// Sum of Non DeepSleep Ready time in microseconds.
        /// This sums the Ready time from other processes which interfere. Additionally all other events which are not summed by <see cref="SumDeepSleepUs"/> are part of this metric.
        /// </summary>
        public double SumNonDeepSleepUs { get; }
    }



    /// <summary>
    /// Ready time percentiles.
    /// This class is serialized to Json file
    /// </summary>
    public class ReadyTimes : IReadyTimes
    {
        /// <summary>
        /// Contains when filled Version, Count, Ready Min,Max,Percentiles 5,25,50,90,95,99,Sum
        /// </summary>
        public List<double> Other { get; set; } = [];

        /// <summary>
        /// DeepSleep Ready times
        /// </summary>
        public List<double> DeepSleep { get;set; } = [];

        /// <summary>
        /// Ready Time version
        /// </summary>
        const double Version = 1.0d;

        const double Thousand = 1_000.0d;

        const int VersionV1Idx = 0;
        const int CountV1Idx = 1;
        const int MinV1Idx = 2;
        const int MaxV1Idx = 3;
        const int Percentile5V1Idx = 4;
        const int Percentile25V1Idx = 5;
        const int Percentile50V1Idx = 6;
        const int Percentile90V1Idx = 7;
        const int Percentile95V1Idx = 8;
        const int Percentile99V1Idx = 9;
        const int SumV1Idx = 10;

        /// <summary>
        /// true when DeepSleep ready times are present. 
        /// </summary>
        [JsonIgnore]
        public bool HasDeepSleepTimes { get => DeepSleep.Count > 0; }

        /// <summary>
        /// true hwen non DeepSleep ready times are present
        /// </summary>
        [JsonIgnore]
        public bool HasNonDeepSleepTimes { get => Other.Count > 0; }

        /// <summary>
        /// Number of context switch events which did originate from an DeepSleep thread. 
        /// DeepSleep Ready thread events are useful to find how long a parked CPU did need to wake up from deep power states.
        /// </summary>
        [JsonIgnore]
        public int CSwitchCountDeepSleep { get => (int)DeepSleep[CountV1Idx]; }

        /// <summary>
        /// Number of context switch events which did not originate from an DeepSleep thread.
        /// </summary>
        [JsonIgnore]
        public int CSwitchCountNonDeepSleep { get => (int)Other[CountV1Idx]; }

        /// <summary>
        /// Minimum Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double MinNonDeepSleepUs { get => Other[MinV1Idx]; }

        /// <summary>
        /// Minimum Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double MinDeepSleepUs { get => DeepSleep[MinV1Idx]; }

        /// <summary>
        /// Maximum Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double MaxNonDeepSleepUs { get => Other[MaxV1Idx]; }

        /// <summary>
        /// Maximum Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double MaxDeepSleepUs { get => DeepSleep[MaxV1Idx]; }


        /// <summary>
        /// 5% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile5NonDeepSleepUs { get => Other[Percentile5V1Idx]; }

        /// <summary>
        /// 5% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile5DeepSleepUs { get => DeepSleep[Percentile5V1Idx]; }


        /// <summary>
        /// 25% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile25NonDeepSleepUs { get => Other[Percentile25V1Idx]; }

        /// <summary>
        /// 25% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile25DeepSleepUs { get => DeepSleep[Percentile25V1Idx]; }


        /// <summary>
        /// Median = 50% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile50NonDeepSleepUs { get => Other[Percentile50V1Idx]; }

        /// <summary>
        /// Median = 50% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile50DeepSleepUs { get => DeepSleep[Percentile50V1Idx]; }


        /// <summary>
        /// 90% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile90NonDeepSleepUs { get => Other[Percentile90V1Idx]; }

        /// <summary>
        /// 90% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile90DeepSleepUs { get => DeepSleep[Percentile90V1Idx]; }


        /// <summary>
        /// 95% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile95NonDeepSleepUs { get => Other[Percentile95V1Idx]; }

        /// <summary>
        /// 95% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile95DeepSleepUs { get => DeepSleep[Percentile95V1Idx]; }


        /// <summary>
        /// 99% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile99NonDeepSleepUs { get => Other[Percentile99V1Idx]; }

        /// <summary>
        /// 99% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile99DeepSleepUs { get => DeepSleep[Percentile99V1Idx]; }


        /// <summary>
        /// Sum of DeepSleep Ready time in seconds 
        /// </summary>
        [JsonIgnore]
        public double SumDeepSleepUs { get => DeepSleep[SumV1Idx]; }

        /// <summary>
        /// Sum of Non DeepSleep Ready time in seconds
        /// </summary>
        [JsonIgnore]
        public double SumNonDeepSleepUs { get => Other[SumV1Idx]; }

        /// <summary>
        /// Add Ready time details for a given method. The data must be supplied in nanoseconds. Internally it will be stored in microsecond precision to get short strings in the serialized format.
        /// </summary>
        /// <param name="deepSleep">if true DeepSleep ready times are added to Other times</param>
        /// <param name="count">count of cswitch events</param>
        /// <param name="sumNanoS">sum of all context switch events in Nanoseconds</param>
        /// <param name="minNanoS">Minimum Ready time in Nanoseconds</param>
        /// <param name="maxNanoS">Maximum Ready time in Nanoseconds</param>
        /// <param name="percentile5NanoS">5% Percentile Ready Time in Nanoseconds</param>
        /// <param name="percentile25NanoS">25% Percentile Ready Time in Nanoseconds</param>
        /// <param name="percentile50NanoS">50% Percentile Ready Time in Nanoseconds</param>
        /// <param name="percentile90NanoS">90% Percentile Ready Time in Nanoseconds</param>
        /// <param name="percentile95NanoS">95% Percentile Ready Time in Nanoseconds</param>
        /// <param name="percentile99NanoS">99% Percentile Ready Time in Nanoseconds</param>
        public void AddReadyTimes(bool deepSleep, int count, long minNanoS, long maxNanoS, long percentile5NanoS, long percentile25NanoS, long percentile50NanoS, long percentile90NanoS, long percentile95NanoS, long percentile99NanoS, decimal sumNanoS)
        {
            if (deepSleep)
            {
                DeepSleep.Add(Version);
                DeepSleep.Add(count);
                DeepSleep.Add((double) minNanoS / Thousand);
                DeepSleep.Add((double) maxNanoS / Thousand);
                DeepSleep.Add((double) percentile5NanoS / Thousand);
                DeepSleep.Add((double) percentile25NanoS / Thousand);
                DeepSleep.Add((double) percentile50NanoS / Thousand);
                DeepSleep.Add((double) percentile90NanoS / Thousand);
                DeepSleep.Add((double) percentile95NanoS / Thousand);
                DeepSleep.Add((double) percentile99NanoS / Thousand);
                DeepSleep.Add((double) sumNanoS / Thousand);
            }
            else
            {
                Other.Add(Version);
                Other.Add(count);
                Other.Add((double) minNanoS / Thousand);
                Other.Add((double) maxNanoS / Thousand);
                Other.Add((double) percentile5NanoS  / Thousand);
                Other.Add((double) percentile25NanoS /Thousand);
                Other.Add((double) percentile50NanoS /Thousand);
                Other.Add((double) percentile90NanoS /Thousand);
                Other.Add((double) percentile95NanoS /Thousand);
                Other.Add((double) percentile99NanoS / Thousand);
                Other.Add((double) sumNanoS / Thousand);
            }
        }

    }
}
