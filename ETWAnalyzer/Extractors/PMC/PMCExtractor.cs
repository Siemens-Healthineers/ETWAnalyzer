using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.PMC;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Cpu;
using Microsoft.Windows.EventTracing.Processes;
using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.Extractors.PMC
{
    /// <summary>
    /// Extract PMC (Performance Monitoring Counter) data which is giving insights how efficient the CPU works.
    /// Currently only PMC counting mode is supported where the counter data is attached to Context Switch events.
    /// Sampling PMC data ETW traces are currently not supported by TraceProcessing library.
    /// 
    /// Second thing are Last Branch Record traces which allow us to estimate method call counts by sampling data.
    /// The LBR records are usually added after CSWITCH or PROFILE events. This extractor assumes you have filtered out all
    /// LBR records except NearReturns which give the best stack traces.
    /// Other LBR records like NearRelativeCalls, NearIndirectCalls are not that useful because for JIT compiled code a call instruction usually
    /// lands in a backpatched jump table which leads to a lot of missing symbols. A typical managed call is 
    ///                                   call    00007fff`8968f110 (FunctionCaller.Program.F1(), mdToken: 0000000006000005)
    /// 0:000> !IP2MD 00007fff`8968f110 
    /// Failed to request MethodData, not in JIT code range
    /// ...
    /// 00007fff`8968f110 e9abd20100      jmp     00007fff`896ac3c0
    /// ...
    /// 00007fff`896ac3c0 55              push rbp
    /// 00007fff`896ac3c1 4883ec20 sub    rsp,20h
    /// ...
    /// The call instruction points to a jmp which cannot be resolved by the current debuggers. But when we sample the ret calls we get good
    /// method resolution.
    /// </summary>
    internal class PMCExtractor : ExtractorBase
    {
        /// <summary>
        /// Read PMC Counters
        /// </summary>
        IPendingResult<Microsoft.Windows.EventTracing.Cpu.IProcessorCounterDataSource> myProcessorCounters;

        /// <summary>
        /// Read Last Branch Record traces
        /// </summary>
        IPendingResult<Microsoft.Windows.EventTracing.Cpu.ILastBranchRecordDataSource> myLBR;

        /// <summary>
        /// Processes are needed for loaded module address ranges to resolve symbols
        /// </summary>
        IPendingResult<IProcessDataSource> myProcesses;

        /// <summary>
        /// We store in Json Module!Method names
        /// </summary>
        readonly StackPrinter myPrinter = new(StackFormat.DllAndMethod);

        /// <summary>
        /// ctor
        /// </summary>
        public PMCExtractor()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="processor"></param>
        public override void RegisterParsers(ITraceProcessor processor)
        {
            myProcessorCounters = processor.UseProcessorCounters();
            myLBR = processor.UseLastBranchRecordData();
            myProcesses = processor.UseProcesses();
            
            NeedsSymbols = true;
        }

        /// <summary>
        /// Extract PMC data of ETW file
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="results"></param>
        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            ExtractPMC(results);
            ExtractLBR(results);
        }

        /// <summary>
        /// Extract Last Branch records which give us an estimate how often a specific method was called.
        /// </summary>
        /// <param name="results"></param>
        private void ExtractLBR(ETWExtract results)
        {
            if( !myLBR.HasResult )
            {
                Console.WriteLine("No LBR data present in trace.");
                return;
            }

            IStackSymbol sFrom, sTo;

            Dictionary<ETWProcessIndex, Dictionary<string, Counter<string>>> counts = new();

            foreach(ILastBranchRecordSnapshot shot in myLBR.Result.Snapshots)
            {
                IProcess process = myProcesses.Result.GetProcess(shot.Timestamp, shot.ProcessId);
                if( process ==  null || process.Images == null)
                {
                    continue;
                }

                foreach(ILastBranchRecordJump jump in shot.Jumps)
                {
                    sFrom = process.GetSymbolForAddress(jump.FromInstructionPointer);
                    sTo =  process.GetSymbolForAddress(jump.ToInstructionPointer);

                    if( AreDifferentMethods(sFrom, sTo, out string mFrom, out string mTo) )
                    {
                        ETWProcessIndex processIdx = results.GetProcessIndexByPID(process.Id, process.CreateTime.HasValue ? process.CreateTime.Value.DateTimeOffset : default);
                        if( !counts.TryGetValue(processIdx, out Dictionary < string, Counter<string>> from2MethodCount) )
                        {
                            from2MethodCount = new Dictionary<string, Counter<string>>();
                            counts[processIdx] = from2MethodCount;
                        }

                        if( !from2MethodCount.TryGetValue(mFrom, out Counter<string> methodCount) )
                        {
                            methodCount = new Counter<string>();
                            from2MethodCount[mFrom] = methodCount;
                        }

                        methodCount.Increment(mTo);
                    }
                }
            }

            // After we have collected call counts store them in Json structure
            foreach(KeyValuePair<ETWProcessIndex, Dictionary<string, Counter<string>>> kvp in counts)
            {
                foreach(var from in kvp.Value)
                {
                    foreach(KeyValuePair<string, int> methodCounts in from.Value.Counts)
                    {
                        // only record counts which are bigger than statistical noise
                        if (methodCounts.Value > 1)
                        {
                            results.PMC.LBRData.SetCount(kvp.Key, from.Key, methodCounts.Key, methodCounts.Value);
                        }
                    }
                }
            }
        }

        bool AreDifferentMethods(IStackSymbol s1, IStackSymbol s2, out string method1, out string method2)
        {
            method1 = myPrinter.GetPrettyMethod(s1?.FunctionName, s1?.Image);
            method2 = myPrinter.GetPrettyMethod(s2?.FunctionName, s2?.Image);
            return method1 != method2;
        }

        private void ExtractPMC(ETWExtract results)
        {
            if (!myProcessorCounters.HasResult)
            {
                Console.WriteLine("No Counting PMC Data present in trace.");
                return;
            }

            Dictionary<string, Dictionary<ETWProcessIndex, ulong>> aggregatedDiffs = new();

            // In Counting mode ETW reads the PMC values on each Context Switch
            // we are interested only in the diff since the last context switch which is provided by TraceProcessing library already
            // To stay simple we simply add for a process all counters up so we can calculate totals and ratios for a process
            foreach (var diff in myProcessorCounters.Result.ContextSwitchCounterDeltas)
            {
                if (diff?.Process?.ImageName == null || diff.Process.Id == 0)
                {
                    continue;
                }


                ETWProcessIndex processIdx = results.GetProcessIndexByPID(diff.Process.Id, diff.Process.CreateTime.HasValue ? diff.Process.CreateTime.Value.DateTimeOffset : default);

                foreach (var rawDelta in diff.RawCounterDeltas)
                {
                    if (!aggregatedDiffs.TryGetValue(rawDelta.Key, out Dictionary<ETWProcessIndex, ulong> delta))
                    {
                        delta = new Dictionary<ETWProcessIndex, ulong>();
                        aggregatedDiffs.Add(rawDelta.Key, delta);
                    }

                    if (!delta.TryGetValue(processIdx, out ulong value))
                    {
                        // already present
                    }
                    delta[processIdx] = value + rawDelta.Value;
                }
            }

            // write per process aggregated counters into ETWExtract
            foreach (var aggDiff in aggregatedDiffs)
            {
                PMCCounter counter = results.PMC.Counters.FirstOrDefault(x => x.CounterName == aggDiff.Key);
                if (counter == null)
                {
                    // add new counter
                    counter = new()
                    {
                        CounterName = aggDiff.Key,
                    };
                    results.PMC.Counters.Add(counter);
                }

                // add per process counter values
                foreach (KeyValuePair<ETWProcessIndex, ulong> kvp in aggDiff.Value)
                {
                    counter.ProcessMap[kvp.Key] = kvp.Value;
                }
            }
        }
    }
}
