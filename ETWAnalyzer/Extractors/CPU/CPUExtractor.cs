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

namespace ETWAnalyzer.Extractors
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
        Dictionary<ProcessKey, Duration> myPerProcessCPU = new Dictionary<ProcessKey, Duration>();

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
            // inspired by https://github.com/microsoft/eventtracing-processing-samples/blob/master/GetCpuSampleDuration/Program.cs

            Dictionary<ProcessKey, Dictionary<string, CpuData>> methodSamplesPerProcess = new Dictionary<ProcessKey, Dictionary<string, CpuData>>();
            StackPrinter printer = new StackPrinter(StackFormat.DllAndMethod);
            foreach (ICpuSample sample in mySamplingData.Result.Samples)
            {
                if (sample?.Process?.ImageName == null)
                {
                    continue;
                }

                var process = new ProcessKey(sample.Process.ImageName, sample.Process.Id, sample.Process.CreateTime.HasValue ? sample.Process.CreateTime.Value.DateTimeOffset : default(DateTimeOffset));

                AddTotalCPU(process, sample, myPerProcessCPU);
                AddPerMethodAndProcessCPU(process, sample, methodSamplesPerProcess, printer);
            }

            bool hasCpuSamples = mySamplingData.HasResult && mySamplingData.Result.Samples.Count > 0;

            // When we have context switch data recorded we can also calculate the thread wait time stacktags
            if (myCpuSchedlingData.HasResult)
            {
                foreach (ICpuThreadActivity slice in myCpuSchedlingData.Result.ThreadActivity)
                {
                    if (slice?.Process?.ImageName == null)
                    {
                        continue;
                    }
                    IReadOnlyList<StackFrame> frames = slice.SwitchIn.Stack?.Frames;
                    if (frames == null)
                    {
                        continue;
                    }

                    var process = new ProcessKey(slice.Process.ImageName, slice.Process.Id, slice.Process.CreateTime.HasValue ? slice.Process.CreateTime.Value.DateTimeOffset : default(DateTimeOffset));

                    AddPerMethodAndProcessWaits(process, slice, methodSamplesPerProcess, printer, hasCpuSamples);
                }
            }

            // convert dictionary with kvp to format ProcessName(pid) and sample count in ms
            Dictionary<ProcessKey, uint> perProcessSamplesInMs = new Dictionary<ProcessKey, uint>();
            foreach (var kvp in myPerProcessCPU.OrderByDescending(x => x.Value))
            {
                perProcessSamplesInMs.Add(kvp.Key, (uint)kvp.Value.TotalMilliseconds);
            }

            CPUPerProcessMethodList inclusiveSamplesPerMethod = new CPUPerProcessMethodList
            {
                ContainsAllCPUData = ExtractAllCPUData
            };

            foreach(var process2Method in methodSamplesPerProcess)
            {
                foreach(var methodToMs in process2Method.Value)
                {
                    inclusiveSamplesPerMethod.AddMethod(process2Method.Key, methodToMs.Key, methodToMs.Value, ExtractAllCPUData ? 0 : CutOffMs);
                }
            }

            inclusiveSamplesPerMethod.SortMethodsByNameAndCPU();

            results.CPU = new CPUStats(perProcessSamplesInMs, inclusiveSamplesPerMethod);
        }

        private void AddPerMethodAndProcessWaits(ProcessKey process, ICpuThreadActivity slice, Dictionary<ProcessKey, Dictionary<string, CpuData>> methodSamplesPerProcess, StackPrinter printer, bool hasCpuSampleData)
        {
            if (slice?.Process?.ImageName == null)  // Image Name can be null sometimes
            {
                return;
            }

            if( !hasCpuSampleData) // if we have no CPU sampling data use cpu from context switch 
            {
                AddTotalCPU(process, slice, myPerProcessCPU);
            }

            IReadOnlyList<StackFrame> frames = slice?.SwitchIn?.Stack?.Frames;
            if( frames == null)
            {
                return;
            }

            HashSet<string> recursionCountGuard = new HashSet<string>();

            for(int i=0;i<frames.Count;i++)
            {
                StackFrame frame = frames[i];

                if ( !methodSamplesPerProcess.TryGetValue(process, out Dictionary<string, CpuData> methods))
                {
                    methods = new Dictionary<string, CpuData>();
                    methodSamplesPerProcess.Add(process, methods);
                }

                string method = printer.GetPrettyMethod(frame.Symbol?.FunctionName, frame);
                if (recursionCountGuard.Add(method) == false)
                {
                    // do not attribute the same sample to the same method again or 
                    // we will count some methods twice or even more often!
                    // Some methods show up in case of recursion up multiple times in a stacktrace!
                    continue;
                }

                if ( !methods.TryGetValue( method, out CpuData stats))
                {
                    stats = new CpuData();
                    methods[method] = stats;
                }
                
                if (slice.WaitingDuration.HasValue)
                {
                    // we do not use the CSwitch timestamp because the wait times will not match with the sum in 
                    // a single threaded case which is wrong. Instead we subtract the wait time from the switch in event which solves the issue
                    TraceTimestamp waitStart = slice.SwitchIn.ContextSwitch.Timestamp - slice.WaitingDuration.Value;
                    stats.WaitTimeRange.Add(waitStart, slice.WaitingDuration.Value);
                    stats.WaitMsCount++;
                }

                if( slice.ReadyDuration.HasValue && slice?.SwitchIn?.ContextSwitch != null)
                {
                    TraceTimestamp readyStart = slice.SwitchIn.ContextSwitch.Timestamp - slice.ReadyDuration.Value;
                    stats.ReadyTimeRange.Add(readyStart, slice.ReadyDuration.Value);
                }
                    

                // if we have no CPU sampling data we use the context switch CPU time as rough estimate. This will always attribute to waiting methods all CPU
                // but that is the way how Context Switch data works
                if (!hasCpuSampleData)  
                {
                    stats.CpuInMs += slice.Duration;
                }

                stats.DepthFromBottom.Add((ushort)i);
                decimal time = slice.StopTime.RelativeTimestamp.TotalSeconds;
                UpdateMethodTimingAndThreadId(stats, time, slice.Thread.Id);
            }
        }

        private void AddPerMethodAndProcessCPU(ProcessKey process, ICpuSample sample, Dictionary<ProcessKey, Dictionary<string, CpuData>> methodSamplesPerProcess, StackPrinter printer)
        {
            IStackSnapshot stack = sample.Stack;
            if( stack?.Process?.ImageName == null)
            {
                return;
            }

            IReadOnlyList<StackFrame> frames = stack.Frames;
            HashSet<string> recursionCountGuard = new HashSet<string>();

            for(int i=0;i<frames.Count;i++)
            {
                StackFrame frame = frames[i];
                if (!methodSamplesPerProcess.TryGetValue(process, out Dictionary<string, CpuData> methods))
                {
                    methods = new Dictionary<string, CpuData>();
                    methodSamplesPerProcess.Add(process, methods);
                }

                string method = printer.GetPrettyMethod(frame.Symbol?.FunctionName, frame);
                if (recursionCountGuard.Add(method) == false)
                {
                    // do not attribute the same sample to the same method again or 
                    // we will count some methods twice or even more often!
                    // Some methods show up in case of recursion up multiple times in a stacktrace!
                    continue;
                }

                if (!methods.TryGetValue(method, out CpuData stats))
                {
                    stats = new CpuData();
                    methods[method] = stats;
                }

                stats.CpuInMs += sample.Weight;
                stats.DepthFromBottom.Add((ushort)i);
                stats.CpuInMsCount++;

                decimal time = sample.Timestamp.RelativeTimestamp.TotalSeconds;
                UpdateMethodTimingAndThreadId(stats, time, sample.Thread.Id);
            }

        }

        private static void UpdateMethodTimingAndThreadId(CpuData stats, decimal time, int threadId)
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
        private static void AddTotalCPU(ProcessKey process, ICpuSample sample, Dictionary<ProcessKey, Duration> perProcessCPU)
        {
            if (!perProcessCPU.TryGetValue(process, out Duration duration))
            {
                perProcessCPU.Add(process, default);
            }
            perProcessCPU[process] = duration + sample.Weight;
        }

        /// <summary>
        /// Update per process CPU usage data from CPU Context Switch Data. This is used when no CPU sampling data is present but context switches are recorded.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="slice"></param>
        /// <param name="perProcessCPU"></param>
        private static void AddTotalCPU(ProcessKey process, ICpuThreadActivity slice, Dictionary<ProcessKey, Duration> perProcessCPU)
        {
            if (!perProcessCPU.TryGetValue(process, out Duration duration))
            {
                perProcessCPU.Add(process, default);
            }
            perProcessCPU[process] = duration + slice.Duration;
        }
    }
}
