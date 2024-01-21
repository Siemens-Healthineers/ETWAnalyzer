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
        /// true when Idle ready times are present. 
        /// </summary>
        bool HasIdleTimes { get; }

        /// <summary>
        /// true hwen non Idle ready times are present
        /// </summary>
        bool HasNonIdleTimes { get; }

        /// <summary>
        /// Number of context switch events.
        /// Idle Context Switch events did originate from an idle thread in deep power state which allows to measure CPU unpark time without other process interference because the Idle thread was owning the CPU before. 
        /// </summary>
        int CSwitchCountIdle { get; }

        /// <summary>
        /// Number of context switch events which did not originate from an idle thread.
        /// </summary>
        int CSwitchCountNonIdle { get; }

        /// <summary>
        /// Minimum Ready time in microseconds.
        /// </summary>
        double MinNonIdleUs { get; }

        /// <summary>
        /// Minimum Ready time in microseconds.
        /// Idle Context Switch events did originate from an idle thread in deep power state which allows to measure CPU unpark time without other process interference because the Idle thread was owning the CPU before. 
        /// </summary>
        double MinIdleUs { get; }


        /// <summary>
        /// Maximum Ready time in microseconds.
        /// </summary>
        double MaxNonIdleUs { get; }

        /// <summary>
        /// Maximum Ready time in microseconds.
        /// Idle Context Switch events did originate from an idle thread in deep power state which allows to measure CPU unpark time without other process interference because the Idle thread was owning the CPU before. 
        /// </summary>
        double MaxIdleUs { get; }


        /// <summary>
        /// 5% Percentile Ready time in microseconds.
        /// </summary>
        double Percentile5NonIdleUs { get; }

        /// <summary>
        /// 5% Percentile Ready time in microseconds.
        /// Idle Context Switch events did originate from an idle thread in deep power state which allows to measure CPU unpark time without other process interference because the Idle thread was owning the CPU before. 
        /// </summary>
        double Percentile5IdleUs { get; }


        /// <summary>
        /// 25% Percentile Ready time in microseconds.
        /// Idle Context Switch events did originate from an idle thread in deep power state which allows to measure CPU unpark time without other process interference because the Idle thread was owning the CPU before. 
        /// </summary>
        double Percentile25IdleUs { get; }

        /// <summary>
        /// 25% Percentile Ready time in microseconds.
        /// </summary>
        double Percentile25NonIdleUs { get; }


        /// <summary>
        /// Median = 50% Percentile Ready time in microseconds.
        /// Idle Context Switch events did originate from an idle thread in deep power state which allows to measure CPU unpark time without other process interference because the Idle thread was owning the CPU before. 
        /// </summary>
        double Percentile50IdleUs { get; }


        /// <summary>
        /// Median = 50% Percentile Ready time in microseconds.
        /// </summary>
        double Percentile50NonIdleUs { get; }

        /// <summary>
        /// 90% Percentile Ready time in microseconds.
        /// </summary>
        double Percentile90NonIdleUs { get; }

        /// <summary>
        /// 90% Percentile Ready time in microseconds.
        /// Idle Context Switch events did originate from an idle thread in deep power state which allows to measure CPU unpark time without other process interference because the Idle thread was owning the CPU before. 
        /// </summary>
        double Percentile90IdleUs { get; }


        /// <summary>
        /// 95% Percentile Ready time in microseconds.
        /// </summary>
        double Percentile95NonIdleUs { get; }

        /// <summary>
        /// 95% Percentile Ready time in microseconds.
        /// Idle Context Switch events did originate from an idle thread in deep power state which allows to measure CPU unpark time without other process interference because the Idle thread was owning the CPU before. 
        /// </summary>
        double Percentile95IdleUs { get; }


        /// <summary>
        /// 99% Percentile Ready time in microseconds.
        /// </summary>
        double Percentile99NonIdleUs { get; }

        /// <summary>
        /// 99% Percentile Ready time in microseconds.
        /// Idle Context Switch events did originate from an idle thread in deep power state which allows to measure CPU unpark time without other process interference because the Idle thread was owning the CPU before. 
        /// </summary>
        double Percentile99IdleUs { get; }

        /// <summary>
        /// Sum of Idle Ready time in microseconds.
        /// Idle Context Switch events did originate from an idle thread in deep power state which allows to measure CPU unpark time without other process interference because the Idle thread was owning the CPU before. 
        /// </summary>
        public double SumIdleUs { get; }

        /// <summary>
        /// Sum of Non Idle Ready time in microseconds.
        /// This sums the idle time from process interference beacause we did need to wait for a CPU to become free. Additionally the shallow idle events which did not originate from deep sleep states are also part of this metric.
        /// </summary>
        public double SumNonIdleUs { get; }
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
        public List<double> NonIdle { get; set; } = [];

        /// <summary>
        /// Idle Ready times
        /// </summary>
        public List<double> Idle { get;set; } = [];

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
        /// true when Idle ready times are present. 
        /// </summary>
        [JsonIgnore]
        public bool HasIdleTimes { get => Idle.Count > 0; }

        /// <summary>
        /// true hwen non Idle ready times are present
        /// </summary>
        [JsonIgnore]
        public bool HasNonIdleTimes { get => NonIdle.Count > 0; }

        /// <summary>
        /// Number of context switch events which did originate from an idle thread. 
        /// Idle Ready thread events are useful to find how long a parked CPU did need to wake up from deep power states.
        /// </summary>
        [JsonIgnore]
        public int CSwitchCountIdle { get => (int)Idle[CountV1Idx]; }

        /// <summary>
        /// Number of context switch events which did not originate from an idle thread.
        /// </summary>
        [JsonIgnore]
        public int CSwitchCountNonIdle { get => (int)NonIdle[CountV1Idx]; }

        /// <summary>
        /// Minimum Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double MinNonIdleUs { get => NonIdle[MinV1Idx]; }

        /// <summary>
        /// Minimum Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double MinIdleUs { get => Idle[MinV1Idx]; }

        /// <summary>
        /// Maximum Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double MaxNonIdleUs { get => NonIdle[MaxV1Idx]; }

        /// <summary>
        /// Maximum Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double MaxIdleUs { get => Idle[MaxV1Idx]; }


        /// <summary>
        /// 5% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile5NonIdleUs { get => NonIdle[Percentile5V1Idx]; }

        /// <summary>
        /// 5% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile5IdleUs { get => Idle[Percentile5V1Idx]; }


        /// <summary>
        /// 25% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile25NonIdleUs { get => NonIdle[Percentile25V1Idx]; }

        /// <summary>
        /// 25% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile25IdleUs { get => Idle[Percentile25V1Idx]; }


        /// <summary>
        /// Median = 50% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile50NonIdleUs { get => NonIdle[Percentile50V1Idx]; }

        /// <summary>
        /// Median = 50% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile50IdleUs { get => Idle[Percentile50V1Idx]; }


        /// <summary>
        /// 90% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile90NonIdleUs { get => NonIdle[Percentile90V1Idx]; }

        /// <summary>
        /// 90% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile90IdleUs { get => Idle[Percentile90V1Idx]; }


        /// <summary>
        /// 95% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile95NonIdleUs { get => NonIdle[Percentile95V1Idx]; }

        /// <summary>
        /// 95% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile95IdleUs { get => Idle[Percentile95V1Idx]; }


        /// <summary>
        /// 99% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile99NonIdleUs { get => NonIdle[Percentile99V1Idx]; }

        /// <summary>
        /// 99% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public double Percentile99IdleUs { get => Idle[Percentile99V1Idx]; }


        /// <summary>
        /// Sum of Idle Ready time in seconds 
        /// </summary>
        [JsonIgnore]
        public double SumIdleUs { get => Idle[SumV1Idx]; }

        /// <summary>
        /// Sum of Non Idle Ready time in seconds
        /// </summary>
        [JsonIgnore]
        public double SumNonIdleUs { get => NonIdle[SumV1Idx]; }

        /// <summary>
        /// Add Ready time details for a given method. The data must be supplied in nanoseconds. Internally it will be stored in microsecond precision to get short strings in the serialized format.
        /// </summary>
        /// <param name="idle">if true idle ready times are added otherwise non idle times</param>
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
        public void AddReadyTimes(bool idle, int count, long minNanoS, long maxNanoS, long percentile5NanoS, long percentile25NanoS, long percentile50NanoS, long percentile90NanoS, long percentile95NanoS, long percentile99NanoS, decimal sumNanoS)
        {
            if (idle)
            {
                Idle.Add(Version);
                Idle.Add(count);
                Idle.Add((double) minNanoS / Thousand);
                Idle.Add((double) maxNanoS / Thousand);
                Idle.Add((double) percentile5NanoS / Thousand);
                Idle.Add((double) percentile25NanoS / Thousand);
                Idle.Add((double) percentile50NanoS / Thousand);
                Idle.Add((double) percentile90NanoS / Thousand);
                Idle.Add((double) percentile95NanoS / Thousand);
                Idle.Add((double) percentile99NanoS / Thousand);
                Idle.Add((double) sumNanoS / Thousand);
            }
            else
            {
                NonIdle.Add(Version);
                NonIdle.Add(count);
                NonIdle.Add((double) minNanoS / Thousand);
                NonIdle.Add((double) maxNanoS / Thousand);
                NonIdle.Add((double) percentile5NanoS  / Thousand);
                NonIdle.Add((double) percentile25NanoS /Thousand);
                NonIdle.Add((double) percentile50NanoS /Thousand);
                NonIdle.Add((double) percentile90NanoS /Thousand);
                NonIdle.Add((double) percentile95NanoS /Thousand);
                NonIdle.Add((double) percentile99NanoS / Thousand);
                NonIdle.Add((double) sumNanoS / Thousand);
            }
        }

    }
}
