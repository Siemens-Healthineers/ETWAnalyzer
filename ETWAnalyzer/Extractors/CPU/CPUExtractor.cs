﻿//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Cpu;
using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using ETWAnalyzer.TraceProcessorHelpers;
using ETWAnalyzer.Infrastructure;
using System.Globalization;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using ETWAnalyzer.Extract.CPU;
using ETWAnalyzer.Extract.CPU.Extended;
using System.Diagnostics;

namespace ETWAnalyzer.Extractors.CPU
{
    class CPUExtractor : ExtractorBase
    {
        /// <summary>
        /// Only include methods in list which have a CPU sample duration > 10 ms or a Wait Duration > 10 ms
        /// </summary>
        const int CutOffMs = 10;

        /// <summary>
        /// When on command line -allCPU is specified we extract all CPU methods without any threshold
        /// </summary>
        public bool ExtractAllCPUData { get; internal set; }

        /// <summary>
        /// When not null we extract timeline data with given frequency
        /// </summary>
        public float? TimelineDataExtractionIntervalS { get; internal set; }

        /// <summary>
        /// defines how many parallel threads we use, if set.
        /// </summary>
        public int? Concurrency { get; internal set; }

        /// <summary>
        /// Actual number of used threads
        /// </summary>
        private int UsedConcurrency { get; set; } = 1;

        /// <summary>
        /// Do not extract extended Ready metrics when set to true
        /// </summary>
        public bool NoReady { get; internal set; }

        /// <summary>
        /// Just a rough guess for initial dictionary size
        /// </summary>
        const int EstimatedMethodCount = 2000;

        /// <summary>
        /// Some locking is needed
        /// </summary>
        object myLock = new object();


        /// <summary>
        /// When TimelineDataExtractionIntervalS is not null we will extract also CPU timeline data.
        /// </summary>
        TimelineExtractor myTimelineExtractor = null;

        /// <summary>
        /// CPU Sample data
        /// </summary>
        IPendingResult<ICpuSampleDataSource> mySamplingData;

        /// <summary>
        /// Context Switch data
        /// </summary>
        IPendingResult<ICpuSchedulingDataSource> myCpuSchedlingData;

        /// <summary>
        /// Per process CPU
        /// </summary>
        readonly ConcurrentDictionary<ProcessKey, Duration> myPerProcessCPU = new();

        public CPUExtractor()
        {
        }

        public override void RegisterParsers(ITraceProcessor processor)
        {
            mySamplingData = processor.UseCpuSamplingData();
            myCpuSchedlingData = processor.UseCpuSchedulingData();
            NeedsSymbols = true;
        }

        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            using var logger = new PerfLogger("Extract CPU");

            // access CSwitch data while we are processing CPU sampling data
            Task.Run(() => { var _ = myCpuSchedlingData.Result?.ThreadActivity?.FirstOrDefault(); });

            if (TimelineDataExtractionIntervalS != null)
            {
                myTimelineExtractor = new TimelineExtractor(TimelineDataExtractionIntervalS.Value, results.SessionStart, results.SessionDuration);
            }

            if (Concurrency != null)
            {
                UsedConcurrency = Concurrency.Value;
            }

            // inspired by https://github.com/microsoft/eventtracing-processing-samples/blob/master/GetCpuSampleDuration/Program.cs
            ConcurrentDictionary<ProcessKey, ConcurrentDictionary<string, CPUMethodData>> methodSamplesPerProcess = new(UsedConcurrency,1000);
            StackPrinter printer = new(StackFormat.DllAndMethod);

            ParallelOptions concurrency = new ParallelOptions
            {
                MaxDegreeOfParallelism = UsedConcurrency
            };

            bool hasSamplesWithStacks = false;

            if (mySamplingData.HasResult)
            {
                Parallel.ForEach(mySamplingData.Result.Samples, concurrency, (ICpuSample sample) =>
                {
                    if (sample?.Process?.ImageName == null)
                    {
                        return;
                    }

                    if (hasSamplesWithStacks == false)
                    {
                        hasSamplesWithStacks = sample.Stack != null;
                    }

                    var process = new ProcessKey(sample.Process.ImageName, sample.Process.Id, sample.Process.CreateTime.HasValue ? sample.Process.CreateTime.Value.DateTimeOffset : default(DateTimeOffset));

                    AddTotalCPU(process, sample, myPerProcessCPU);
                    AddPerMethodAndProcessCPU(results, process, sample, methodSamplesPerProcess, printer);
                    if (myTimelineExtractor != null)
                    {
                        myTimelineExtractor.AddSample(process, sample);
                    }
                });
            }

            // When we have context switch data recorded we can also calculate the thread wait time stacktags
            if (myCpuSchedlingData.HasResult)
            {
                Parallel.ForEach(myCpuSchedlingData.Result.ThreadActivity, concurrency, (ICpuThreadActivity slice) =>
                {
                    if (slice?.Process?.ImageName == null)
                    {
                        return;
                    }
                    IReadOnlyList<Microsoft.Windows.EventTracing.Symbols.StackFrame> frames = slice.SwitchIn.Stack?.Frames;
                    if (frames == null)
                    {
                        return;
                    }

                    var process = new ProcessKey(slice.Process.ImageName, slice.Process.Id, slice.Process.CreateTime.HasValue ? slice.Process.CreateTime.Value.DateTimeOffset : default(DateTimeOffset));
                    AddPerMethodAndProcessWaits(results, process, slice, methodSamplesPerProcess, printer, hasSamplesWithStacks);
                });
            }

            // convert dictionary with kvp to format ProcessName(pid) and sample count in ms
            Dictionary<ProcessKey, uint> perProcessSamplesInMs = new();
            // sort by cpu then by process name and then by pid to get stable sort order
            foreach (var kvp in myPerProcessCPU.OrderByDescending(x => x.Value).ThenBy(x => x.Key.Name).ThenBy( x=> x.Key.Pid) )
            {
                perProcessSamplesInMs.Add(kvp.Key, (uint)kvp.Value.TotalMilliseconds);
            }

            CPUPerProcessMethodList inclusiveSamplesPerMethod = new()
            {
                ContainsAllCPUData = ExtractAllCPUData,
                HasCPUSamplingData = hasSamplesWithStacks,
                HasCSwitchData = myCpuSchedlingData.HasResult && myCpuSchedlingData.Result?.ContextSwitches?.Count > 0,
            };

            // speed up time range calculation
            Parallel.ForEach(methodSamplesPerProcess, concurrency, (process) =>
            {
                foreach(KeyValuePair<string, CPUMethodData> methodCPU in process.Value)
                {
                    var tmp = methodCPU.Value.ReadyTimeRange.GetDuration();
                    tmp = methodCPU.Value.WaitTimeRange.GetDuration();
                }
            });

            int cutOffMs = ExtractAllCPUData ? 0 : CutOffMs;

            foreach (KeyValuePair<ProcessKey, ConcurrentDictionary<string, CPUMethodData>> process2Method in methodSamplesPerProcess)
            {
                foreach (KeyValuePair<string, CPUMethodData> methodToMs in process2Method.Value)
                {
                
                    MethodIndex methodIndex = inclusiveSamplesPerMethod.AddMethod(process2Method.Key, methodToMs.Key, methodToMs.Value, cutOffMs);
                    if (methodIndex != MethodIndex.Invalid && results?.CPU?.ExtendedCPUMetrics?.CPUToFrequencyDurations?.Count > 0)
                    {
                        if (process2Method.Key.Pid > WindowsConstants.IdleProcessId)
                        {
                            var perCoreTypeCPUUsage = methodToMs.Value.GetAverageFrequenciesPerEfficiencyClass(results.CPU);
                            if (perCoreTypeCPUUsage != null)
                            {
                                ETWProcessIndex processIdx = results.GetProcessIndexByPID(process2Method.Key.Pid, process2Method.Key.StartTime);
                                Debug.Assert(methodToMs.Key == inclusiveSamplesPerMethod.MethodNames[(int)methodIndex], $"Method Index {methodIndex} and name {methodToMs.Key} are different ({inclusiveSamplesPerMethod.MethodNames[(int)methodIndex]})!");
                                results.CPU.ExtendedCPUMetrics.AddMethodCostPerEfficiencyClass(processIdx, methodIndex, perCoreTypeCPUUsage);
                            }
                        }
                    }

                    if (NoReady == false)  // generate extended Ready metrics unless specified at command line
                    {
                        if (methodIndex != MethodIndex.Invalid && process2Method.Key.Pid > WindowsConstants.IdleProcessId)
                        {
                            ReadyTimes ready = methodToMs.Value.GetReadyMetrics(results.CPU);
                            if (ready != null)
                            {
                                ETWProcessIndex processIdx = results.GetProcessIndexByPID(process2Method.Key.Pid, process2Method.Key.StartTime);
                                if (results.CPU?.ExtendedCPUMetrics == null)
                                {
                                    results.CPU.ExtendedCPUMetrics = new();
                                }

                                Debug.Assert(methodToMs.Key == inclusiveSamplesPerMethod.MethodNames[(int)methodIndex], $"Method Index {methodIndex} and name {methodToMs.Key} are different ({inclusiveSamplesPerMethod.MethodNames[(int)methodIndex]})!");
                                results.CPU.ExtendedCPUMetrics.AddReadyMetrics(processIdx, methodIndex, ready);
                            }
                        }
                    }

                }
            }

            Dictionary<MethodIndex, MethodIndex> methodIndexMap = inclusiveSamplesPerMethod.SortMethodsByNameAndCPU();

            if (results.CPU?.ExtendedCPUMetrics?.MethodData.Count > 0)
            {
                results.CPU.ExtendedCPUMetrics.RemapMethodIndicies(methodIndexMap);
            }

            results.CPU = new CPUStats(perProcessSamplesInMs, inclusiveSamplesPerMethod, myTimelineExtractor?.Timeline, results.CPU?.Topology, results.CPU?.ExtendedCPUMetrics);

            // Try to release memory as early as possible
            mySamplingData = null;
            myCpuSchedlingData = null;
        }



        private void AddPerMethodAndProcessWaits(ETWExtract extract, ProcessKey process, ICpuThreadActivity slice, ConcurrentDictionary<ProcessKey, ConcurrentDictionary<string, CPUMethodData>> methodSamplesPerProcess, StackPrinter printer, bool hasCpuSampleData)
        {
            if (slice?.Process?.ImageName == null)  // Image Name can be null sometimes
            {
                return;
            }

            if( !hasCpuSampleData) // if we have no CPU sampling data use cpu from context switch 
            {
                AddTotalCPU(process, slice, myPerProcessCPU);
            }

            IReadOnlyList<Microsoft.Windows.EventTracing.Symbols.StackFrame> frames = slice?.SwitchIn?.Stack?.Frames;
            if( frames == null)
            {
                return;
            }

            HashSet<string> recursionCountGuard = new();

            for(int i=0;i<frames.Count;i++)
            {
                Microsoft.Windows.EventTracing.Symbols.StackFrame frame = frames[i];
                ConcurrentDictionary<string, CPUMethodData> methods = null;

                if (!methodSamplesPerProcess.TryGetValue(process, out methods))
                {
                    // do not waste too much memory when many ConcurrentDictionaries are created which creates by default
                    // as many internal arrays as you have cores on server CPUs very wasteful
                    methods = new ConcurrentDictionary<string, CPUMethodData>(UsedConcurrency, EstimatedMethodCount);
                    if( !methodSamplesPerProcess.TryAdd(process, methods) )
                    {
                        methodSamplesPerProcess.TryGetValue(process, out methods);
                    }
                }

                string method = printer.GetPrettyMethod(frame.Symbol?.FunctionName, frame);
                string methodWithRva = AddRva(method, frame.RelativeVirtualAddress);

                if (recursionCountGuard.Add(methodWithRva) == false)
                {
                    // do not attribute the same sample to the same method again or 
                    // we will count some methods twice or even more often!
                    // Some methods show up in case of recursion up multiple times in a stacktrace!
                    continue;
                }


                CPUMethodData stats = null;

                if (!methods.TryGetValue(methodWithRva, out stats))
                {
                    stats = new CPUMethodData();
                    if( !methods.TryAdd(methodWithRva, stats) )
                    {
                        methods.TryGetValue(methodWithRva, out stats);
                    }
                }

                Interlocked.Increment(ref stats.myContextSwitchCountField);
                
                if (slice.WaitingDuration.HasValue)
                {
                    // we do not use the CSwitch timestamp because the wait times will not match with the sum in 
                    // a single threaded case which is wrong. Instead we subtract the wait time from the switch in event which solves the issue
                    TraceTimestamp waitStart = slice.SwitchIn.ContextSwitch.Timestamp - slice.WaitingDuration.Value;
                    stats.WaitTimeRange.Add(waitStart, slice.WaitingDuration.Value);
                    Interlocked.Increment(ref stats.WaitMsCount);
                }

                if( slice.ReadyDuration.HasValue && slice?.SwitchIn?.ContextSwitch != null)
                {
                    TraceTimestamp readyStart = slice.SwitchIn.ContextSwitch.Timestamp - slice.ReadyDuration.Value;
                    stats.ReadyTimeRange.Add(readyStart, slice.ReadyDuration.Value);
                    stats.AddExtendedReadyMetrics(extract.CPU, (CPUNumber)slice.Processor, (float)readyStart.RelativeTimestamp.TotalSeconds, (float)slice.ReadyDuration.Value.TotalSeconds, slice.Thread.Id, slice.Thread.ProcessorAffinity);
                }

                decimal time = slice.StopTime.RelativeTimestamp.TotalSeconds;
                stats.DepthFromBottom.Add((ushort)i);

                lock (myLock)
                {
                    // if we have no CPU sampling data we use the context switch CPU time as rough estimate. This will always attribute to waiting methods all CPU
                    // but that is the way how Context Switch data works
                    if (!hasCpuSampleData)
                    {
                        string debugData = null;
#if DEBUG
                        debugData = $"CSwitch {process} {method} ";
#endif
                        stats.AddForExtendedMetrics(extract.CPU, (CPUNumber) slice.Processor, (float) slice.StartTime.RelativeTimestamp.TotalSeconds, (float) slice.StopTime.RelativeTimestamp.TotalSeconds, slice.Thread.Id, slice.Thread.ProcessorAffinity, debugData);
                        stats.CpuInMs += slice.Duration;
                    }

                    UpdateMethodTimingAndThreadId(stats, time, slice.Thread.Id);
                }
            }
        }

        private void AddPerMethodAndProcessCPU(ETWExtract extract, ProcessKey process, ICpuSample sample, ConcurrentDictionary<ProcessKey, ConcurrentDictionary<string, CPUMethodData>> methodSamplesPerProcess, StackPrinter printer)
        {
            IStackSnapshot stack = sample.Stack;
            if( stack?.Process?.ImageName == null)
            {
                return;
            }

            IReadOnlyList<Microsoft.Windows.EventTracing.Symbols.StackFrame> frames = stack.Frames;
            HashSet<string> recursionCountGuard = new();

            for(int i=0;i<frames.Count;i++)
            {
                Microsoft.Windows.EventTracing.Symbols.StackFrame frame = frames[i];
                ConcurrentDictionary<string, CPUMethodData> methods = null;

                if (!methodSamplesPerProcess.TryGetValue(process, out methods))
                {
                    // do not waste too much memory when many ConcurrentDictionaries are created which creates by default
                    // as many internal arrays as you have cores on server CPUs very wasteful
                    methods = new ConcurrentDictionary<string, CPUMethodData>(UsedConcurrency, EstimatedMethodCount);
                    if( !methodSamplesPerProcess.TryAdd(process, methods) )
                    {
                        methodSamplesPerProcess.TryGetValue(process, out methods);
                    }
                }
                
                string method = printer.GetPrettyMethod(frame.Symbol?.FunctionName, frame);
                string rvaMethod = AddRva(method, frame.RelativeVirtualAddress);

                if (recursionCountGuard.Add(rvaMethod) == false)
                {
                    // do not attribute the same sample to the same method again or 
                    // we will count some methods twice or even more often!
                    // Some methods show up in case of recursion up multiple times in a stacktrace!
                    continue;
                }

                if (!methods.TryGetValue(rvaMethod, out CPUMethodData stats))
                {
                    stats = new CPUMethodData();
                    if( !methods.TryAdd(rvaMethod, stats) ) // was already added
                    {
                        methods.TryGetValue(rvaMethod, out stats); // get added value
                    }
                }

                Interlocked.Increment(ref stats.CpuInMsCount);
                decimal time = sample.Timestamp.RelativeTimestamp.TotalSeconds;
                lock (myLock)
                {
                    stats.CpuInMs += sample.Weight;
                    stats.DepthFromBottom.Add((ushort)i);

                    string debugData=null;
#if DEBUG
                    debugData = $"Profiling: {process} {method}";
#endif

                    stats.AddForExtendedMetrics(extract.CPU, (CPUNumber) sample.Processor, (float) sample.Timestamp.RelativeTimestamp.TotalSeconds, ((float) sample.Timestamp.RelativeTimestamp.TotalSeconds)+(float) sample.Weight.Duration.TotalSeconds, sample.Thread.Id, sample.Thread.ProcessorAffinity, debugData);
                    UpdateMethodTimingAndThreadId(stats, time, sample.Thread.Id);
                }
            }
        }



        /// <summary>
        /// Add to method name if it was not resolved the RVA address. This is needed to later resolve the method
        /// name when matching symbols could be loaded.
        /// </summary>
        /// <param name="method">Method of the form xxxx.dll!method where method is xxx.dll if the symbol lookup did fail.</param>
        /// <param name="rva">Image Relative Virtual Address</param>
        /// <returns>For unresolved methods the image + Image Relative Virtual Address.</returns>
        string AddRva(string method, Address rva)
        {
            string lret = method;

            // do not try to resolve invalid RVAs (like 0)
            if(rva.Value > 0 && ( method.EndsWith(".exe", StringComparison.Ordinal) || method.EndsWith(".dll", StringComparison.Ordinal) || method.EndsWith(".sys", StringComparison.Ordinal) ) ) 
            {
                // Method could not be resolved. Use RVA
                lret = method + "+0x" + rva.Value.ToString("X", CultureInfo.InvariantCulture);
            }

            return lret;
        }

        private static void UpdateMethodTimingAndThreadId(CPUMethodData stats, decimal time, int threadId)
        {
            stats.FirstOccurrenceSeconds = Math.Min(stats.FirstOccurrenceSeconds, time);
            stats.LastOccurrenceSeconds = Math.Max(stats.LastOccurrenceSeconds, time);
            stats.ThreadIds.Add(threadId);
        }


        /// <summary>
        /// Update per process CPU usage from CPU Sampling data. This is preferred.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="sample"></param>
        /// <param name="perProcessCPU"></param>
        private static void AddTotalCPU(ProcessKey process, ICpuSample sample, ConcurrentDictionary<ProcessKey, Duration> perProcessCPU)
        {
            Duration duration = default;
            do
            {
                if (!perProcessCPU.TryGetValue(process, out duration))
                {
                    perProcessCPU.TryAdd(process, default);
                }
            } while (!perProcessCPU.TryUpdate(process, sample.Weight + duration, duration));
        }

        /// <summary>
        /// Update per process CPU usage data from CPU Context Switch Data. This is used when no CPU sampling data is present but context switches are recorded.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="slice"></param>
        /// <param name="perProcessCPU"></param>
        private static void AddTotalCPU(ProcessKey process, ICpuThreadActivity slice, ConcurrentDictionary<ProcessKey, Duration> perProcessCPU)
        {
            Duration duration = default;
            do
            {
                if (!perProcessCPU.TryGetValue(process, out duration))
                {
                    perProcessCPU.TryAdd(process, default);
                }
            }
            while( !perProcessCPU.TryUpdate(process, duration + slice.Duration, duration));
        }
    }
}
