using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.CPU.Extended;
using ETWAnalyzer.Infrastructure;
using Microsoft.Windows.EventTracing;
using System;

namespace ETWAnalyzer.Extractors.CPU
{
    /// <summary>
    /// Extract from ETW Provider Microsoft-Windows-Kernel-Processor-Power the sampled processor Frequency data over time.
    /// </summary>
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
                var frequencyData = new Extract.CPU.Extended.CPUExtended();

                foreach(Microsoft.Windows.EventTracing.Power.IProcessorFrequencyInterval cpu in myCpuFrequencies.Result.Intervals) 
                {
                    try
                    {
                        if (cpu.AverageFrequency == null)
                        {
                            Console.WriteLine($"Warning: File {results.SourceETLFileName} contains CPU frequency ETW data, but AverageFrequency is null. This happens when the CaptureState for the Microsoft-Windows-Kernel-Processor-Power provider is missing.");
                            break;
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Console.WriteLine($"Warning: File {results.SourceETLFileName} contains CPU frequency ETW data, but AverageFrequency is not accessible. This happens when the CaptureState for the Microsoft-Windows-Kernel-Processor-Power provider is missing.");
                        break;
                    }
                    catch (InvalidTraceDataException ex)
                    {
                        string msg  = $"File {results.SourceETLFileName} contains CPU frequency ETW data, but AverageFrequency is not accessible, because of a CPU count mismatch: ";
                        Console.WriteLine("Warning: " + msg + ex.Message);
                        Logger.Warn(msg + ex);
                        break;
                    }

                    if (cpu.AverageFrequency.Value.TotalMegahertz > 0.0m)  // sometimes we get 0 readings which are spurious events which are arriving also sometimes even if we did not record Frequency data
                    {
                        frequencyData.AddFrequencyDuration((CPUNumber)cpu.Processor, (float)cpu.StartTime.TotalSeconds, (float)cpu.StopTime.TotalSeconds, (int)cpu.AverageFrequency.Value.TotalMegahertz);
                    }
                }

                // Frequency Extractor comes always before CPU extractor
                results.CPU = new CPUStats(null, null, null, null, null, results?.CPU?.Topology, frequencyData);
            }
        }
    }

}
