using Microsoft.Windows.EventTracing.Cpu;
using Microsoft.Windows.EventTracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.Extract;
using ETWAnalyzer.TraceProcessorHelpers;
using ETWAnalyzer.Extract.CPU;

namespace ETWAnalyzer.Extractors.CPU
{
    internal class CpuFrequencyExtractor : ExtractorBase
    {
        IPendingResult<Microsoft.Windows.EventTracing.Power.IProcessorFrequencyDataSource> myCpuFrequencies;
        public override void RegisterParsers(ITraceProcessor processor)
        {
            base.RegisterParsers(processor);
            myCpuFrequencies = processor.UseProcessorFrequencyData();
        }

        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            using var logger = new PerfLogger("Extract CPU Frequency");
           
            if( myCpuFrequencies.HasResult )
            {
                var groups = myCpuFrequencies.Result.Intervals.ToLookup(x => x.Processor);
                var frequencyData = results.CPU.Frequency ?? new Extract.CPU.CPUFrequency();

                foreach(var group in groups) 
                {
                    frequencyData.FrequencyData[(CPUNumber)group.Key] =  group.OrderBy(x => x.StartTime).Select(x => new CPUFrequencyDuration { Start = x.StartTime.DateTimeOffset, End = x.StopTime.DateTimeOffset }).ToArray();
                }
            }
        }
    }

    public class CPUFrequencyDuration
    {
        public int AverageFrequencyAsPercentOfBase { get; set; }
        public DateTimeOffset Start { get;  set; }
        public DateTimeOffset End { get; set; }
    }

}
