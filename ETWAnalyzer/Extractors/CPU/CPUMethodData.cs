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
    internal class CPUMethodData
    {
        /// <summary>
        /// We calculate metrics only if at least this number of CSwitch events are present to get meaningful data
        /// </summary>
        const int MinCSwitchCount = 50;

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
        public TimeRangeCalculator WaitTimeRange { get; set; } = new TimeRangeCalculator();


        /// <summary>
        /// Average CPU frequency from Context Switch/Sampling data grouped by CPU
        /// </summary>
        internal Dictionary<CPUNumber, List<ProfilingData>> CPUToFrequencyDuration { get; set; } = new();

        /// <summary>
        /// CPU to Ready Duration
        /// </summary>
        internal Dictionary<CPUNumber, List<ReadyEvent>> CPUToReadyDuration { get; set; } = new();  

        class CPUDetails
        {
            public int UsedCores { get; set; }
            public List<ProfilingData> ProfilingData { get; set; } = new(10);
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
            HashSet<string> debugData = new HashSet<string>();
#endif
            foreach(var kvp in CPUToFrequencyDuration)
            {
                EfficiencyClass effClass = context.Topology[kvp.Key].EfficiencyClass;
                if( !efficiencyClassToFrequency.TryGetValue(effClass, out CPUDetails details) )
                {
                    details = new();
                    efficiencyClassToFrequency[effClass] = details;
                }

                details.ProfilingData.AddRange(kvp.Value);
                details.UsedCores++;
#if DEBUG
                debugData = new HashSet<string>(kvp.Value.Select(x => x.DebugData));
#endif
            }

            List<CPUUsage> lret = new();
            long enabledCPUs = 0;
            foreach (var kvp in efficiencyClassToFrequency)
            {
                decimal weightedFrequency=0m;
                decimal totalTimeS = 0;
                float firstS = float.MaxValue;
                float lastS = 0;
                enabledCPUs = 0;

                foreach (var x in kvp.Value.ProfilingData)
                {
                    weightedFrequency += (decimal) x.DurationS * x.FrequencyMHz;
                    totalTimeS += (decimal) x.DurationS;
                    enabledCPUs += x.EnabledCPUs;
                    firstS = Math.Min(firstS, x.StartTimeS);
                    lastS = Math.Max(lastS, x.StartTimeS+x.DurationS);
                }

                int averageFrequencyMHz = (int) (weightedFrequency / totalTimeS);

                lret.Add(new CPUUsage()
                    {
                        EfficiencyClass = kvp.Key,
                        AverageMHz = averageFrequencyMHz,
                        CPUMs = (int) Math.Round(totalTimeS*1000, 0, MidpointRounding.AwayFromZero ),
                        UsedCores = kvp.Value.UsedCores,
                        EnabledCPUsAvg = enabledCPUs / kvp.Value.ProfilingData.Count,
                        FirstS = (float) Math.Round(firstS, 4, MidpointRounding.AwayFromZero),
                        LastS = (float) Math.Round(lastS, 4, MidpointRounding.AwayFromZero),
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
            List<long> idleReadyDurationNanoS = [];
            List<long> nonIdleReadyDurationNanoS = [];

            foreach(var ready in CPUToReadyDuration)
            {
                idleReadyDurationNanoS.AddRange(    ready.Value.Where(x =>  x.IsIdle).Select(x => x.DurationNanoS));
                nonIdleReadyDurationNanoS.AddRange( ready.Value.Where(x => !x.IsIdle).Select(x => x.DurationNanoS));
            }

            if (idleReadyDurationNanoS.Count > MinCSwitchCount)
            {
                idleReadyDurationNanoS.Sort();
                decimal idleSumNanoS = 0.0m;
                foreach(var idle in idleReadyDurationNanoS)
                {
                    idleSumNanoS += (decimal) idle;
                }

                lret.AddReadyTimes(idle: true,
                    idleReadyDurationNanoS.Count,
                    idleReadyDurationNanoS.Min(),
                    idleReadyDurationNanoS.Max(),
                    idleReadyDurationNanoS.Percentile(0.05f),
                    idleReadyDurationNanoS.Percentile(0.25f),
                    idleReadyDurationNanoS.Percentile(0.50f),
                    idleReadyDurationNanoS.Percentile(0.90f),
                    idleReadyDurationNanoS.Percentile(0.95f),
                    idleReadyDurationNanoS.Percentile(0.99f),
                    idleSumNanoS);
            }

            if (nonIdleReadyDurationNanoS.Count > MinCSwitchCount)
            {
                nonIdleReadyDurationNanoS.Sort();
                decimal nonIdleSumNanoS = 0.0m;
                foreach(var nonidle in nonIdleReadyDurationNanoS)
                {
                    nonIdleSumNanoS += (decimal)nonidle;
                }

                lret.AddReadyTimes(idle: false,
                    nonIdleReadyDurationNanoS.Count,
                    nonIdleReadyDurationNanoS.Min(),
                    nonIdleReadyDurationNanoS.Max(),
                    nonIdleReadyDurationNanoS.Percentile(0.05f),
                    nonIdleReadyDurationNanoS.Percentile(0.25f),
                    nonIdleReadyDurationNanoS.Percentile(0.50f),
                    nonIdleReadyDurationNanoS.Percentile(0.90f),
                    nonIdleReadyDurationNanoS.Percentile(0.95f),
                    nonIdleReadyDurationNanoS.Percentile(0.99f),
                    nonIdleSumNanoS);
            }

            // return null in case we have no events to make serialized data more compact
            return (lret.Idle.Count > 0 || lret.NonIdle.Count > 0) ? lret: null;
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
        public uint ContextSwitchCount { get => (uint) myContextSwitchCountField;  }

        /// <summary>
        /// Number of used Context Switch Events. Mainly used for debugging purposes
        /// </summary>
        public int WaitMsCount;


        internal CPUMethodData()
        {
        }

        internal struct ProfilingData
        {
            public float DurationS { get; set; }
            public float StartTimeS { get; set; }
            public int FrequencyMHz { get; set; }
            public int ThreadId { get; set; }
            public long EnabledCPUs { get; set; }
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
        public void AddForExtendedMetrics(CPUStats cpuStats, CPUNumber cpu, float start, float end, int threadId, long cpuAffinityMask, string debugData)
        {
            if (cpuStats.ExtendedCPUMetrics != null && cpuStats.ExtendedCPUMetrics.CPUToFrequencyDurations.Count > 0)
            {
                if( !CPUToFrequencyDuration.TryGetValue(cpu, out List<ProfilingData> frequencyDurations) )
                {
                    frequencyDurations = new();
                    CPUToFrequencyDuration[cpu] = frequencyDurations;
                }

                int averageFrequency = cpuStats.ExtendedCPUMetrics.GetFrequency(cpu, start);

                if( averageFrequency <= 0)
                {
                    // Use default frequency if we get no frequency data or CPU was turned off but this is not possible if we get sampling/CWSwitch data
                    // CPU Frequency data from ETW provider is sampled with a granularity of 15-30ms. 
                    averageFrequency = cpuStats.Topology[cpu].NominalFrequencyMHz; 
                }

                ulong enabledCPUs = 0;
#if NET6_0_OR_GREATER
                enabledCPUs = System.Runtime.Intrinsics.X86.Popcnt.X64.PopCount((ulong)cpuAffinityMask);
#endif
                frequencyDurations.Add(new ProfilingData
                {
                    StartTimeS = start,
                    DurationS = (end - start),
                    FrequencyMHz = averageFrequency,
                    ThreadId = threadId,
                    EnabledCPUs = (long) enabledCPUs,
#if DEBUG
                    DebugData = debugData,
#endif
                });
            }
        }
        
        /// <summary>
        /// Working structure to collect intermediate data from context switch events. Not serialied to Json
        /// </summary>
        internal class ReadyEvent
        {
            public long StartTimeNanoS { get; set; }

            public long DurationNanoS { get; set; }
            public int ThreadId { get; set; }
            public bool IsIdle { get; set; }
        }


        /// <summary>
        /// Collect extended Ready delays coming from processor deep sleep states and cross thread interference. 
        /// </summary>
        /// <param name="slice">Thread activity which contains context switch events.</param>
        internal void AddExtendedReadyMetrics(ICpuThreadActivity2 slice)
        {
            lock (this)
            {
                CPUNumber processor = (CPUNumber)slice.Processor;
                if ( !CPUToReadyDuration.TryGetValue(processor, out var readies) )
                {
                    readies = [];
                    CPUToReadyDuration[processor] = readies;
                }

                if( slice.PreviousActivityOnProcessor == null ) //
                {
                    return;
                }

                // just record data from idle thread to calculate cpu wakup time from sleep and deep sleep states
                readies.Add(new ReadyEvent
                {
                    StartTimeNanoS = (slice.SwitchIn.ContextSwitch.Timestamp - slice.ReadyDuration.Value).RelativeTimestamp.Nanoseconds,
                    DurationNanoS = slice.ReadyDuration.Value.Nanoseconds,
                    ThreadId = slice.Thread.Id,
                    // We are interested in the performance impact of deep sleep states which is the processor power up time.
                    // All other delays are the ready times from shallow sleep states (should be fast) and thread interference from other threads of the same or other processes.
                    // Windows abstracts shallow sleep states (C1/C1E) as CState = 0 and all deeper sleep states as CState = 1
                    IsIdle = slice?.Wait?.ReadyThreadEvent?.ReadyingProcess?.Id == WindowsConstants.IdleProcessId && slice?.SwitchIn?.ContextSwitch?.PreviousCState == 1,
                });
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
        internal CPUMethodData(Duration cpuMs, Duration waitMs, decimal firstOccurrence, decimal lastOccurrence, int threadCount, ushort depthFromBottom)
        {
            CpuInMs = cpuMs;
            WaitTimeRange.Add(Timestamp.Zero, waitMs);
            FirstOccurrenceSeconds = firstOccurrence;
            LastOccurrenceSeconds = lastOccurrence;
            for (int i = 1; i <= threadCount; i++)
            {
                ThreadIds.Add(i);
            }

            for(int i=0;i<5;i++) // first sample gets some headstart to make mean calculation more stable
            {
                DepthFromBottom.Add(depthFromBottom);
            }
        }
    }
}
