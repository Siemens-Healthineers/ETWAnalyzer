using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.PMC;
using Microsoft.Windows.EventTracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extractors.PMC
{
    /// <summary>
    /// Extract PMC (Performance Monitoring Counter) data which is giving insights how efficient the CPU works.
    /// Currently only PMC counting mode is supported where the counter data is attached to Context Switch events.
    /// Sampling PMC data ETW traces are currently not supported by TraceProcessing library.
    /// </summary>
    internal class PMCExtractor : ExtractorBase
    {
        IPendingResult<Microsoft.Windows.EventTracing.Cpu.IProcessorCounterDataSource> myProcessorCounters;

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
        }

        /// <summary>
        /// Extract PMC data of ETW file
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="results"></param>
        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            if( !myProcessorCounters.HasResult )
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
                    if( !aggregatedDiffs.TryGetValue(rawDelta.Key, out Dictionary<ETWProcessIndex, ulong> delta) )
                    {
                        delta = new Dictionary<ETWProcessIndex, ulong>();
                        aggregatedDiffs.Add(rawDelta.Key, delta);
                    }

                    if( !delta.TryGetValue(processIdx, out ulong value))
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
                if( counter == null )
                {
                    // add new counter
                    counter = new()
                    {
                        CounterName = aggDiff.Key,
                    };
                    results.PMC.Counters.Add(counter);
                }

                // add per process counter values
                foreach(KeyValuePair<ETWProcessIndex, ulong> kvp in aggDiff.Value)
                {
                    counter.ProcessMap[kvp.Key] = kvp.Value;
                }
            }
        }
    }
}
