//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
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
        /// Ignore CPU sampling data. Used when CPU sampling data is corrupt
        /// </summary>
        public bool NoSampling { get; internal set; }

        /// <summary>
        /// Ignore Context Switch data. Can conserve memory during extraction at the cost of missing thread wait/ready data.
        /// </summary>
        public bool NoCSwitch { get; internal set; }

        /// <summary>
        /// True when it is not disabled and we have data
        /// </summary>
        bool CanUseCpuSamplingData { get => mySamplingData?.HasResult == true && !NoSampling; }

        /// <summary>
        /// True when it is not disabld and we have CSwitch data
        /// </summary>
        bool CanUseCPUCSwitchData { get => myCpuSchedlingData?.HasResult == true && !NoCSwitch; }

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
        ConcurrentDictionary<ProcessKey, Duration> myPerProcessCPU = new();

        /// <summary>
        /// Per Process Priority
        /// </summary>
        Dictionary<ETWProcessIndex, List<int>> myPerProcessPriority = new();

        public CPUExtractor()
        {
        }

        public override void RegisterParsers(ITraceProcessor processor)
        {
            if (!NoSampling)
            {
                mySamplingData = processor.UseCpuSamplingData();
            }

            if (!NoCSwitch)
            {
                myCpuSchedlingData = processor.UseCpuSchedulingData();
            }

            NeedsSymbols = true;
        }

        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            using var logger = new PerfLogger("Extract CPU");

            WarmupCSwitchData();

            if (TimelineDataExtractionIntervalS != null)
            {
                myTimelineExtractor = new TimelineExtractor(TimelineDataExtractionIntervalS.Value, results.SessionStart, results.SessionDuration);
            }

            if (Concurrency != null)
            {
                UsedConcurrency = Concurrency.Value;
            }

            // inspired by https://github.com/microsoft/eventtracing-processing-samples/blob/master/GetCpuSampleDuration/Program.cs
            ConcurrentDictionary<ProcessKey, ConcurrentDictionary<string, ExtractorCPUMethodData>> methodSamplesPerProcess = new(UsedConcurrency, 1000);
            ConcurrentDictionary<ProcessKey, List<KeyValuePair<int, TimeSpan>>> processKeyPrioritiesFromSwitch = new(UsedConcurrency, 1000);
            StackPrinter printer = new(StackFormat.DllAndMethod);

            ParallelOptions concurrency = new ParallelOptions
            {
                MaxDegreeOfParallelism = UsedConcurrency
            };

            bool hasSamplesWithStacks = false;
            Dictionary<ProcessKey, ETWProcessIndex> indexCache = BuildProcessIndexCache(results);

            if (CanUseCpuSamplingData)
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

                    var process = new ProcessKey(sample.Process.ImageName, sample.Process.Id, sample.Process.CreateTime.HasValue ? sample.Process.CreateTime.Value.ConvertToTime() : default(DateTimeOffset));

                    AddTotalCPUAndPriority(process, indexCache, sample, myPerProcessCPU, myPerProcessPriority);
                    AddPerMethodAndProcessCPU(results, process, sample, methodSamplesPerProcess, printer);
                    if (myTimelineExtractor != null)
                    {
                        myTimelineExtractor.AddSample(process, sample);
                    }
                });
            }

            // When we have context switch data recorded we can also calculate the thread wait time stacktags
            if (CanUseCPUCSwitchData)
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

                    var process = new ProcessKey(slice.Process.ImageName, slice.Process.Id, slice.Process.CreateTime.HasValue ? slice.Process.CreateTime.Value.ConvertToTime() : default(DateTimeOffset));
                    AddPerMethodAndProcessWaits(results, process, slice, methodSamplesPerProcess, printer, hasSamplesWithStacks);
                    AddProcessPriority(results, process, slice, processKeyPrioritiesFromSwitch);
                });
            }

            // convert dictionary with kvp to format ProcessName(pid) and sample count in ms
            Dictionary<ProcessKey, uint> perProcessSamplesInMs = new();
            // sort by cpu then by process name and then by pid to get stable sort order
            foreach (var kvp in myPerProcessCPU.OrderByDescending(x => x.Value).ThenBy(x => x.Key.Name).ThenBy(x => x.Key.Pid))
            {
                perProcessSamplesInMs.Add(kvp.Key, (uint)kvp.Value.TotalMilliseconds);
            }

            CPUPerProcessMethodList inclusiveSamplesPerMethod = new()
            {
                ContainsAllCPUData = ExtractAllCPUData,
                HasCPUSamplingData = hasSamplesWithStacks,
                HasCSwitchData = CanUseCPUCSwitchData && myCpuSchedlingData.Result.ThreadActivity.Count > 0,
            };

            // speed up time range calculation
            Parallel.ForEach(methodSamplesPerProcess, concurrency, (process) =>
            {
                foreach (KeyValuePair<string, ExtractorCPUMethodData> methodCPU in process.Value)
                {
                    methodCPU.Value.ReadyTimeRange.Freeze();
                    var tmp = methodCPU.Value.ReadyTimeRange.GetDuration();
                    tmp = methodCPU.Value.WaitTimeRange.GetDuration();
                }
            });

            int cutOffMs = ExtractAllCPUData ? 0 : CutOffMs;

            foreach (KeyValuePair<ProcessKey, ConcurrentDictionary<string, ExtractorCPUMethodData>> process2Method in methodSamplesPerProcess)
            {
                foreach (KeyValuePair<string, ExtractorCPUMethodData> methodToMs in process2Method.Value)
                {

                    MethodIndex methodIndex = inclusiveSamplesPerMethod.AddMethod(process2Method.Key, methodToMs.Key, methodToMs.Value, cutOffMs);
                    if (methodIndex != MethodIndex.Invalid && results?.CPU?.HasECores == true)
                    {
                        if (process2Method.Key.Pid > WindowsConstants.IdleProcessId)
                        {
                            CPUUsage[] perCoreTypeCPUUsage = methodToMs.Value.GetAverageFrequenciesPerEfficiencyClass(results.CPU);
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

            Dictionary<ETWProcessIndex, float> processPriorities = GetProcessPriorities(indexCache, myPerProcessPriority, processKeyPrioritiesFromSwitch);

            results.CPU = new CPUStats(perProcessSamplesInMs, processPriorities, inclusiveSamplesPerMethod, myTimelineExtractor?.Timeline, results.CPU?.Topology, results.CPU?.ExtendedCPUMetrics);

            ReleaseMemory();
        }

        private void ReleaseMemory()
        {
            // Try to release memory as early as possible
            mySamplingData = null;
            myCpuSchedlingData = null;
            myPerProcessCPU = new();
            myPerProcessPriority = new();
            myTimelineExtractor = null;
        }

        private void WarmupCSwitchData()
        {
            if (CanUseCPUCSwitchData)
            {
                // access CSwitch data while we are processing CPU sampling data
                Task.Run(() => { var _ = myCpuSchedlingData.Result?.ThreadActivity?.FirstOrDefault(); });
            }
        }

        private void AddProcessPriority(ETWExtract results, ProcessKey process, ICpuThreadActivity slice, ConcurrentDictionary<ProcessKey, List<KeyValuePair<int, TimeSpan>>> processKeyPrioritiesFromSwitch)
        {
            if( slice.SwitchIn.Priority != null)
            {
                int prio = slice.SwitchIn.Priority.Value;
                List<KeyValuePair<int, TimeSpan>> list = processKeyPrioritiesFromSwitch.GetOrAdd(process, (key) => new List<KeyValuePair<int, TimeSpan>>());
                Duration runDuration = slice.SwitchOut.ContextSwitch.Timestamp -  slice.SwitchIn.ContextSwitch.Timestamp;
                lock (list)
                {
                    list.Add(new KeyValuePair<int, TimeSpan>(prio, runDuration.TimeSpan));
                }
            }
        }

        private static Dictionary<ProcessKey, ETWProcessIndex> BuildProcessIndexCache(ETWExtract results)
        {
            Dictionary<ProcessKey, ETWProcessIndex> indexCache = new();
            foreach (var process in results.Processes)
            {
                if (process.ProcessID > WindowsConstants.IdleProcessId && !String.IsNullOrEmpty(process.ProcessName)) 
                {
                    ETWProcessIndex idx = results.GetProcessIndexByPID(process.ProcessID, process.StartTime);
                    indexCache[process.ToProcessKey()] = idx;
                }
                else
                {
                    if (process.ProcessName != null)  // name can be null but pid can exist.
                    {
                        indexCache[process.ToProcessKey()] = ETWProcessIndex.Invalid;
                    }
                }
            }

            return indexCache;
        }

        /// <summary>
        /// Merge process priorities from Context switch and CPU sampling data
        /// </summary>
        /// <param name="indexCache"></param>
        /// <param name="perProcessPriority">Process priorities from CPU sampling data</param>
        /// <param name="processKeyPrioritiesFromSwitch">Raw data from all context switch events in a process with the assigned priority and duration.</param>
        /// <returns>Dictionary which is stored in extract.</returns>
        private Dictionary<ETWProcessIndex, float> GetProcessPriorities(Dictionary<ProcessKey, ETWProcessIndex> indexCache, Dictionary<ETWProcessIndex, List<int>> perProcessPriority, ConcurrentDictionary<ProcessKey, List<KeyValuePair<int, TimeSpan>>> processKeyPrioritiesFromSwitch)
        {
            Dictionary<ETWProcessIndex, float> lret = new();
            foreach (var kvp in perProcessPriority)
            {
                lret[kvp.Key] = (float)Math.Round(kvp.Value.Average(), 1);
            }

            foreach (var process2Prio in processKeyPrioritiesFromSwitch)
            {
                decimal totalWeight = 0.0m;
                decimal totalTime = 0.0m;
                foreach(KeyValuePair<int, TimeSpan> prioTime in process2Prio.Value)
                {
                    totalWeight += ((decimal) prioTime.Value.TotalMilliseconds) * prioTime.Key;
                    totalTime += (decimal)prioTime.Value.TotalMilliseconds;
                }

                decimal averagePrio = totalWeight / totalTime;


                if( indexCache.TryGetValue(process2Prio.Key, out ETWProcessIndex procIndex) )
                {
                    if (!lret.ContainsKey(procIndex))
                    {
                        lret[procIndex] = (float)Math.Round(averagePrio, 1);
                    }
                }
            }


            return lret;
        }

        private void AddPerMethodAndProcessWaits(ETWExtract extract, ProcessKey process, ICpuThreadActivity slice, ConcurrentDictionary<ProcessKey, ConcurrentDictionary<string, ExtractorCPUMethodData>> methodSamplesPerProcess, StackPrinter printer, bool hasCpuSampleData)
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
            ExtractorCPUMethodData.ReadyEvent readyEv = null;

            for (int i=0;i<frames.Count;i++)
            {
                Microsoft.Windows.EventTracing.Symbols.StackFrame frame = frames[i];
                ConcurrentDictionary<string, ExtractorCPUMethodData> methods = null;

                if (!methodSamplesPerProcess.TryGetValue(process, out methods))
                {
                    // do not waste too much memory when many ConcurrentDictionaries are created which creates by default
                    // as many internal arrays as you have cores on server CPUs very wasteful
                    methods = new ConcurrentDictionary<string, ExtractorCPUMethodData>(UsedConcurrency, EstimatedMethodCount);
                    if( !methodSamplesPerProcess.TryAdd(process, methods) )
                    {
                        methodSamplesPerProcess.TryGetValue(process, out methods);
                    }
                }

                string method = printer.GetPrettyMethod(frame.Symbol?.FunctionName, frame);
                string methodWithRva = StackPrinter.AddRva(method, frame.RelativeVirtualAddress);

                if (recursionCountGuard.Add(methodWithRva) == false)
                {
                    // do not attribute the same sample to the same method again or 
                    // we will count some methods twice or even more often!
                    // Some methods show up in case of recursion up multiple times in a stacktrace!
                    continue;
                }


                ExtractorCPUMethodData stats = null;

                if (!methods.TryGetValue(methodWithRva, out stats))
                {
                    stats = new ExtractorCPUMethodData();
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
                    Timestamp waitStart = slice.SwitchIn.ContextSwitch.Timestamp - slice.WaitingDuration.Value;
                    stats.WaitTimeRange.Add(waitStart, slice.WaitingDuration.Value);
                    Interlocked.Increment(ref stats.WaitMsCount);
                }

                if( slice.ReadyDuration.HasValue && slice?.SwitchIn?.ContextSwitch != null)
                {
                    Timestamp readyStart = slice.SwitchIn.ContextSwitch.Timestamp - slice.ReadyDuration.Value;
                    stats.ReadyTimeRange.Add(readyStart, slice.ReadyDuration.Value);
                    readyEv = stats.AddExtendedReadyMetrics(slice, readyEv);
                }

                decimal time = slice.StopTime.TotalSeconds;
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
                        stats.AddForExtendedMetrics(extract.CPU, (CPUNumber) slice.Processor, (float) slice.StartTime.TotalSeconds, (float) slice.StopTime.TotalSeconds, slice.Thread.Id, slice.Thread.ProcessorAffinity, debugData);
                        stats.CpuDuration += slice.Duration;
                    }

                    UpdateMethodTimingAndThreadId(stats, time, slice.Thread.Id);
                }
            }
        }

        private void AddPerMethodAndProcessCPU(ETWExtract extract, ProcessKey process, ICpuSample sample, ConcurrentDictionary<ProcessKey, ConcurrentDictionary<string, ExtractorCPUMethodData>> methodSamplesPerProcess, StackPrinter printer)
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
                ConcurrentDictionary<string, ExtractorCPUMethodData> methods = null;

                if (!methodSamplesPerProcess.TryGetValue(process, out methods))
                {
                    // do not waste too much memory when many ConcurrentDictionaries are created which creates by default
                    // as many internal arrays as you have cores on server CPUs very wasteful
                    methods = new ConcurrentDictionary<string, ExtractorCPUMethodData>(UsedConcurrency, EstimatedMethodCount);
                    if( !methodSamplesPerProcess.TryAdd(process, methods) )
                    {
                        methodSamplesPerProcess.TryGetValue(process, out methods);
                    }
                }
                
                string method = printer.GetPrettyMethod(frame.Symbol?.FunctionName, frame);
                string rvaMethod = StackPrinter.AddRva(method, frame.RelativeVirtualAddress);

                if (recursionCountGuard.Add(rvaMethod) == false)
                {
                    // do not attribute the same sample to the same method again or 
                    // we will count some methods twice or even more often!
                    // Some methods show up in case of recursion up multiple times in a stacktrace!
                    continue;
                }

                if (!methods.TryGetValue(rvaMethod, out ExtractorCPUMethodData stats))
                {
                    stats = new ExtractorCPUMethodData();
                    if( !methods.TryAdd(rvaMethod, stats) ) // was already added
                    {
                        methods.TryGetValue(rvaMethod, out stats); // get added value
                    }
                }

                Interlocked.Increment(ref stats.CpuInMsCount);
                decimal time = sample.Timestamp.TotalSeconds;
                lock (myLock)
                {
                    stats.CpuDuration += sample.Weight;
                    stats.DepthFromBottom.Add((ushort)i);

                    string debugData=null;
#if DEBUG
                    debugData = $"Profiling: {process} {rvaMethod}";
#endif

                    stats.AddForExtendedMetrics(extract.CPU, (CPUNumber) sample.Processor, (float) sample.Timestamp.TotalSeconds, ((float) sample.Timestamp.TotalSeconds)+(float) sample.Weight.TotalSeconds, sample.Thread.Id, sample.Thread.ProcessorAffinity, debugData);
                    UpdateMethodTimingAndThreadId(stats, time, sample.Thread.Id);
                }
            }
        }

        private static void UpdateMethodTimingAndThreadId(ExtractorCPUMethodData stats, decimal time, uint threadId)
        {
            stats.FirstOccurrenceSeconds = Math.Min(stats.FirstOccurrenceSeconds, time);
            stats.LastOccurrenceSeconds = Math.Max(stats.LastOccurrenceSeconds, time);
            stats.ThreadIds.Add(threadId);
        }


        /// <summary>
        /// Update per process CPU usage from CPU Sampling data. This is preferred.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="indexCache">map of processKey instances to ETWProcessIndex</param>
        /// <param name="sample"></param>
        /// <param name="perProcessCPU"></param>
        /// <param name="perProcessPriority"></param>
        private static void AddTotalCPUAndPriority(ProcessKey process, Dictionary<ProcessKey, ETWProcessIndex> indexCache, ICpuSample sample, ConcurrentDictionary<ProcessKey, Duration> perProcessCPU, Dictionary<ETWProcessIndex, List<int>> perProcessPriority)
        {
            Duration duration = default;
            do
            {
                if (!perProcessCPU.TryGetValue(process, out duration))
                {
                    perProcessCPU.TryAdd(process, default);
                }
            } while (!perProcessCPU.TryUpdate(process, sample.Weight + duration, duration));

            lock(perProcessPriority)
            {
                if (indexCache.TryGetValue(process, out ETWProcessIndex idx))
                {
                    if (!perProcessPriority.TryGetValue(idx, out List<int> prios))
                    {
                        prios = new();
                        perProcessPriority.Add(idx, prios);
                    }

                    if (sample.Priority != null)
                    {
                        prios.Add(sample.Priority.Value);
                    }
                }
            }
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
