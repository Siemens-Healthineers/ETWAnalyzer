//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.CPU;
using ETWAnalyzer.Extract.CPU.Extended;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Cpu;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.Extractors.CPU
{

    /// <summary>
    /// Intermediate data structure used during CPU extraction to calculate wait, ready and CPU consumption data 
    /// </summary>
    internal class ExtractorCPUMethodData : IExtractorCPUMethodData
    {
        /// <summary>
        /// We calculate metrics only if at least this number of CSwitch events are present to get meaningful data
        /// </summary>
        const int MinCSwitchCount = 50;

        /// <summary>
        /// Contains CPU data summed across all threads
        /// </summary>
        public Duration CpuDuration
        {
            get;
            set;
        }

        public decimal CpuInMs {  get => CpuDuration.TotalMilliseconds; }

        /// <summary>
        /// Number of used Sample profiling events processed. Used mainly for debugging here 
        /// </summary>
        public int CpuInMsCount;

        /// <summary>
        /// Contains all wait times from all threads. It is used to calculate from all threads the non overlapping wait time.
        /// </summary>
        public TimeRangeCalculator WaitTimeRange { get; set; } = new TimeRangeCalculator();

        ITimeRangeCalculator IExtractorCPUMethodData.WaitTimeRange  => WaitTimeRange; 

        ITimeRangeCalculator IExtractorCPUMethodData.ReadyTimeRange => ReadyTimeRange;

        /// <summary>
        /// Average CPU frequency from Context Switch/Sampling data grouped by CPU
        /// </summary>
        internal Dictionary<CPUNumber, ProfilingData> CPUToFrequencyDuration { get; set; } = new();

        /// <summary>
        /// CPU to Ready Duration
        /// </summary>
        internal Dictionary<CPUNumber, List<ReadyEvent>> CPUToReadyDuration { get; set; } = new();

        class CPUDetails
        {
            public int UsedCores { get; set; }
            public ProfilingData ProfilingData { get; set; } = new();
        }

        /// <summary>
        /// Calculate weighted average frequency which is summed across all cores per efficiency class.
        /// This enables metrics how much cpu time was spent and what the average frequency was for P and E-Cores.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Array of CPU Usage. Each array item represents the sum across all cores for one efficiency class.</returns>
        public CPUUsage[] GetAverageFrequenciesPerEfficiencyClass(CPUStats context)
        {

            Dictionary<EfficiencyClass, CPUDetails> efficiencyClassToFrequency = new();
#if DEBUG
            string debugData = "";
#endif
            foreach (var kvp in CPUToFrequencyDuration)
            {
                EfficiencyClass effClass = context.Topology[kvp.Key].EfficiencyClass;
                if (!efficiencyClassToFrequency.TryGetValue(effClass, out CPUDetails details))
                {
                    details = new();
                    efficiencyClassToFrequency[effClass] = details;
                }

                details.ProfilingData.Combine(kvp.Value);
                details.UsedCores++;
#if DEBUG
                debugData = kvp.Value.DebugData;
#endif
            }

            List<CPUUsage> lret = new();
            foreach (var kvp in efficiencyClassToFrequency)
            {
                lret.Add(new CPUUsage()
                {
                    EfficiencyClass = kvp.Key,
                    AverageMHz = kvp.Value.ProfilingData.WeightedFrequencyMHz,
                    CPUMs = (int)Math.Round(kvp.Value.ProfilingData.TotalDurationS * 1000, 0, MidpointRounding.AwayFromZero),
                    UsedCores = kvp.Value.UsedCores,
                    EnabledCPUsAvg = kvp.Value.ProfilingData.EnabledCPUs,
                    FirstS = (float)Math.Round(kvp.Value.ProfilingData.MinStartTimeS, 4, MidpointRounding.AwayFromZero),
                    LastS = (float)Math.Round(kvp.Value.ProfilingData.MaxStopTimeS, 4, MidpointRounding.AwayFromZero),
#if DEBUG
                        Debug = String.Join(" ", debugData),
#endif
                }
               );
            }

            // return null to save space in serialized json if we have no data
            return lret.Count == 0 ? null : lret.ToArray();
        }


        public ReadyTimes GetReadyMetrics(CPUStats context)
        {
            ReadyTimes lret = new ReadyTimes();
            List<long> deepSleepReadyDurationNanoS = [];
            List<long> interferenceReadyDurationNanoS = [];

            foreach (var ready in CPUToReadyDuration)
            {
                deepSleepReadyDurationNanoS.AddRange(ready.Value.Where(x => x.DeepSleepReady).Select(x => x.DurationNanoS));
                interferenceReadyDurationNanoS.AddRange(ready.Value.Where(x => !x.DeepSleepReady).Select(x => x.DurationNanoS));
            }

            if (deepSleepReadyDurationNanoS.Count > MinCSwitchCount)
            {
                deepSleepReadyDurationNanoS.Sort();

                lret.AddReadyTimes(deepSleep: true,
                    deepSleepReadyDurationNanoS.Count,
                    deepSleepReadyDurationNanoS.Min(),
                    deepSleepReadyDurationNanoS.Max(),
                    deepSleepReadyDurationNanoS.Percentile(0.05f),
                    deepSleepReadyDurationNanoS.Percentile(0.25f),
                    deepSleepReadyDurationNanoS.Percentile(0.50f),
                    deepSleepReadyDurationNanoS.Percentile(0.90f),
                    deepSleepReadyDurationNanoS.Percentile(0.95f),
                    deepSleepReadyDurationNanoS.Percentile(0.99f),
                    GetSum(deepSleepReadyDurationNanoS),
                    GetSumAbove99Percentile(deepSleepReadyDurationNanoS));
            }

            if (interferenceReadyDurationNanoS.Count > MinCSwitchCount)
            {
                interferenceReadyDurationNanoS.Sort();

                lret.AddReadyTimes(deepSleep: false,
                    interferenceReadyDurationNanoS.Count,
                    interferenceReadyDurationNanoS.Min(),
                    interferenceReadyDurationNanoS.Max(),
                    interferenceReadyDurationNanoS.Percentile(0.05f),
                    interferenceReadyDurationNanoS.Percentile(0.25f),
                    interferenceReadyDurationNanoS.Percentile(0.50f),
                    interferenceReadyDurationNanoS.Percentile(0.90f),
                    interferenceReadyDurationNanoS.Percentile(0.95f),
                    interferenceReadyDurationNanoS.Percentile(0.99f),
                    GetSum(interferenceReadyDurationNanoS),
                    GetSumAbove99Percentile(interferenceReadyDurationNanoS));
            }

            // return null in case we have no events to make serialized data more compact
            return (lret.DeepSleep.Count > 0 || lret.Other.Count > 0) ? lret : null;
        }

        /// <summary>
        /// Get sum of sorted list for all values which are above the 99% percentile.
        /// </summary>
        /// <param name="values"></param>
        /// <returns>99 Percentile sum.</returns>
        decimal GetSumAbove99Percentile(List<long> values)
        {
            decimal sum = 0.0m;
            int skip = (int)(values.Count * 0.99);
            for (int i = skip; i < values.Count; i++)
            {
                sum += (decimal)values[i];
            }

            return sum;
        }

        /// <summary>
        /// Get sum of long values with decimal precision.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        decimal GetSum(List<long> values)
        {
            decimal sum = 0.0m;
            foreach (var value in values)
            {
                sum += (decimal)value;
            }

            return sum;
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
        public HashSet<uint> ThreadIds
        {
            get;
        } = new HashSet<uint>();

        /// <summary>
        /// Get for each sample the call stack depth from the bottom frame
        /// </summary>
        public ConcurrentBag<ushort> DepthFromBottom
        {
            get;
        } = new ConcurrentBag<ushort>();

        /// <summary>
        /// Contains a merged view of overlapping time range.
        /// We use this to calculate the overall Ready time across all threads, where overlapping times are counted only once
        /// </summary>
        public TimeRangeCalculator ReadyTimeRange { get; internal set; } = new TimeRangeCalculator();

        /// <summary>
        /// Number of context switches as field because it is incremented from multiple threads so we use interlocked operations
        /// </summary>
        internal int myContextSwitchCountField;

        /// <summary>
        /// Number of context switches
        /// </summary>
        public uint ContextSwitchCount { get => (uint)myContextSwitchCountField; }

        /// <summary>
        /// Number of used Context Switch Events. Mainly used for debugging purposes
        /// </summary>
        public int WaitMsCount;


        internal ExtractorCPUMethodData()
        {
        }


        /// <summary>
        /// Calculate weighted frequency, min/max first/last seen time, used CPU time (duration) and used core count. 
        /// </summary>
        internal class ProfilingData
        {
            /// <summary>
            /// Add from Context Switch data star/stop time, affinity mask and estimated frequency
            /// </summary>
            /// <param name="frequency"></param>
            /// <param name="startTimeS"></param>
            /// <param name="stopTimeS"></param>
            /// <param name="affinityMask"></param>
            public void Add(int frequency, float startTimeS, float stopTimeS, long affinityMask)
            {
                AffinityMask |= affinityMask;

                MinStartTimeS = Math.Min(MinStartTimeS, startTimeS);
                MaxStopTimeS = Math.Max(MaxStopTimeS, stopTimeS);

                decimal durationS = (decimal)(stopTimeS - startTimeS);
                TotalDurationS += durationS;

                WeightedFrequency += (decimal)frequency * durationS;
            }

            internal void Combine(ProfilingData value)
            {
                AffinityMask |= value.AffinityMask;
                MinStartTimeS = Math.Min(MinStartTimeS, value.MinStartTimeS);
                MaxStopTimeS = Math.Max(MaxStopTimeS, value.MaxStopTimeS);
                TotalDurationS += value.TotalDurationS;
                WeightedFrequency += value.WeightedFrequency;
            }

            public float MinStartTimeS { get; private set; } = float.MaxValue;
            public float MaxStopTimeS { get; private set; }

            private decimal WeightedFrequency { get; set; }
            public decimal TotalDurationS { get; private set; }

            public int WeightedFrequencyMHz
            {
                get
                {

                    if (TotalDurationS == 0m)
                    {
                        return 0; // no data
                    }
                    else
                    {
                        return (int)(WeightedFrequency / TotalDurationS);
                    }
                }
            }


            public long AffinityMask { get; private set; }
            public long EnabledCPUs
            {
                get
                {
#if NET6_0_OR_GREATER
                    return AffinityMask == 0 ? 0 : (long)(System.Runtime.Intrinsics.X86.Popcnt.X64.PopCount((ulong)AffinityMask));
#else
                    return 0;
#endif
                }
            }
#if DEBUG
            public string DebugData { get; set; }
#endif
        }

        /// <summary>
        /// Add CPU frequency data along with CPU consumption per CPU.
        /// </summary>
        /// <param name="cpuStats">Getch CPU frequency and nominal Frequency between start/end time</param>
        /// <param name="cpu">CPU number</param>
        /// <param name="start">CPU consumption start time. Usually from CPU sampling or CSWitch data.</param>
        /// <param name="end">CPU consumption end time. Usually from CPU sampling or CSWitch data.</param>
        /// <param name="threadId"></param>
        /// <param name="cpuAffinityMask"></param>
        /// <param name="debugData"></param>
        public void AddForExtendedMetrics(CPUStats cpuStats, CPUNumber cpu, float start, float end, uint threadId, long cpuAffinityMask, string debugData)
        {
            if (cpuStats?.ExtendedCPUMetrics != null)
            {
                if (!CPUToFrequencyDuration.TryGetValue(cpu, out ProfilingData frequencyDurations))
                {
                    frequencyDurations = new();
                    CPUToFrequencyDuration[cpu] = frequencyDurations;
                }

                int averageFrequency = cpuStats.ExtendedCPUMetrics.GetFrequency(cpu, start);

                if (averageFrequency <= 0)
                {
                    // Use default frequency if we get no frequency data or CPU was turned off but this is not possible if we get sampling/CWSwitch data
                    // CPU Frequency data from ETW provider is sampled with a granularity of 15-30ms. 

                    if (cpuStats?.Topology?.TryGetValue(cpu, out var topology) == true)  // sometimes not all cores are covered by topology
                    {
                        averageFrequency = topology.NominalFrequencyMHz;
                    }
                }

                frequencyDurations.Add(averageFrequency, start, end, cpuAffinityMask);
#if DEBUG
                frequencyDurations.DebugData += debugData;
#endif
            }
        }

        /// <summary>
        /// Working structure to collect intermediate data from context switch events. Not serialized to Json
        /// </summary>
        internal class ReadyEvent
        {
            public long StartTimeNanoS { get; set; }

            public long DurationNanoS { get; set; }
            public uint ThreadId { get; set; }
            public bool DeepSleepReady { get; set; }
        }


        /// <summary>
        /// Collect extended Ready delays coming from processor deep sleep states and cross thread interference. 
        /// </summary>
        /// <param name="slice">Thread activity which contains context switch events.</param>
        /// <param name="existing">AddExtendedReadyMetrics will be called with the same slice for all frames in a stacktrace. We can reuse the instance to spare memory.</param>
        internal ReadyEvent AddExtendedReadyMetrics(ICpuThreadActivity slice, ReadyEvent existing)
        {
            lock (this)
            {
                CPUNumber processor = (CPUNumber)slice.Processor;
                if (!CPUToReadyDuration.TryGetValue(processor, out var readies))
                {
                    readies = [];
                    CPUToReadyDuration[processor] = readies;
                }

                if (slice?.PreviousActivityOnProcessor?.Process == null ||
                    slice?.Thread == null ||
                    slice?.SwitchIn?.ContextSwitch == null ||
                    slice?.ReadyDuration == null)
                {
                    return null;
                }

                ReadyEvent lret = existing;
                if (lret == null)
                {
                    lret = new ReadyEvent
                    {
                        StartTimeNanoS = (slice.SwitchIn.ContextSwitch.Timestamp - slice.ReadyDuration.Value).Nanoseconds,
                        DurationNanoS = slice.ReadyDuration.Value.Nanoseconds,
                        ThreadId = slice.Thread.Id,
                        // We are interested in the performance impact of deep sleep states which is the processor power up time.
                        // All other delays are the ready times from shallow sleep states (should be fast) and thread interference from other threads of the same or other processes.
                        // Windows abstracts shallow sleep states (C1/C1E) as CState = 0 and all deeper sleep states as CState > 0. Usual values are 0, 1, 2
                        DeepSleepReady = slice.PreviousActivityOnProcessor?.Process?.Id == WindowsConstants.IdleProcessId && slice?.SwitchIn?.ContextSwitch?.PreviousCState > 0,
                    };
                }

                // just record data from idle thread to calculate cpu wakeup time from sleep and deep sleep states
                readies.Add(lret);

                return lret; // reuse instance and spare some GB of memory
            }
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
        internal ExtractorCPUMethodData(Duration cpuMs, Duration waitMs, decimal firstOccurrence, decimal lastOccurrence, int threadCount, ushort depthFromBottom)
        {
            CpuDuration = cpuMs;
            WaitTimeRange.Add(Timestamp.Zero, waitMs);
            FirstOccurrenceSeconds = firstOccurrence;
            LastOccurrenceSeconds = lastOccurrence;
            for (uint i = 1; i <= threadCount; i++)
            {
                ThreadIds.Add(i);
            }

            for (int i = 0; i < 5; i++) // first sample gets some headstart to make mean calculation more stable
            {
                DepthFromBottom.Add(depthFromBottom);
            }
        }
    }
}
