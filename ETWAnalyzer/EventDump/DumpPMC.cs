using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.PMC;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.EventDump
{
    /// <summary>
    /// Dump PMC events to console or CSV file
    /// </summary>
    class DumpPMC : DumpFileDirBase<DumpPMC.MatchData>
    {
        /// <summary>
        /// The ETW PMC names are a mixture of architecture independent counters which are also present on other CPU architectures (e.g. ARM)
        /// and some Intel/AMD specific CPU counters which are existing with the same semantics across CPU generations.
        /// </summary>
        public enum PMCNames
        {
            Timer,
            TotalIssues,
            InstructionRetired = TotalIssues,
            BranchInstructions,
            BranchInstructionRetired = BranchInstructions,
            CacheMisses,
            LLCMisses = CacheMisses,
            BranchMispredictions,
            BranchMispredictsRetired = BranchMispredictions,
            TotalCycles,
            UnhaltedCoreCycles = TotalCycles,
            UnhaltedReferenceCycles,
            LLCReference,
            LbrInserts,
            InstructionsRetiredFixed,
            UnhaltedCoreCyclesFixed,
            UnhaltedReferenceCyclesFixed,
            TimerFixed,
        }

        static readonly Dictionary<string, PMCNames> PMCNameMap = new()
        {
            { "Timer",                        PMCNames.Timer },
            { "TotalIssues",                  PMCNames.TotalIssues },
            { "InstructionRetired",           PMCNames.InstructionRetired  },
            { "BranchInstructions",           PMCNames.BranchInstructions },
            { "BranchInstructionRetired",     PMCNames.BranchInstructionRetired },
            { "CacheMisses",                  PMCNames.CacheMisses },
            { "LLCMisses",                    PMCNames.LLCMisses },
            { "BranchMispredictions",         PMCNames.BranchMispredictions },
            { "BranchMispredictsRetired",     PMCNames.BranchMispredictsRetired },
            { "TotalCycles",                  PMCNames.TotalCycles },
            { "UnhaltedCoreCycles",           PMCNames.UnhaltedCoreCycles },
            { "UnhaltedReferenceCycles",      PMCNames.UnhaltedReferenceCycles },
            { "LLCReference",                 PMCNames.LLCReference },
            { "LbrInserts",                   PMCNames.LbrInserts },
            { "InstructionsRetiredFixed",     PMCNames.InstructionsRetiredFixed },
            { "UnhaltedCoreCyclesFixed",      PMCNames.UnhaltedCoreCyclesFixed },
            { "UnhaltedReferenceCyclesFixed", PMCNames.UnhaltedReferenceCyclesFixed },
            { "TimerFixed",                   PMCNames.TimerFixed },
        };


        internal List<MatchData> myUTestData;

        public bool NoCmdLine { get; internal set; }
        public bool NoCounters { get; internal set; }

        public override List<MatchData> ExecuteInternal()
        {
            List<MatchData> data = ReadFileData();

            if (IsCSVEnabled)
            {
                OpenCSVWithHeader(Col_CSVOptions, "Directory", Col_FileName, Col_Date, Col_TestCase, Col_TestTimeinms, Col_Baseline, Col_Process, Col_ProcessName, 
                                  "Instructions", "Cycles", "CPI", "CacheMisses", "LLCReferences", "% Cache Misses", "Branches", "BranchMispreditions", "% BranchMispredictions", Col_CommandLine);

                foreach( var pmcEvent in data )
                {
                    WriteCSVLine(CSVOptions, Path.GetDirectoryName(pmcEvent.File.FileName),
                        Path.GetFileNameWithoutExtension(pmcEvent.File.FileName), pmcEvent.File.PerformedAt, pmcEvent.File.TestName, pmcEvent.File.DurationInMs, pmcEvent.BaseLine, 
                        pmcEvent.Process.ProcessWithID, pmcEvent.Process.ProcessNamePretty,
                        pmcEvent.Instructions, pmcEvent.Cycles, pmcEvent.CPI, pmcEvent.LLCMisses, pmcEvent.LLCReference, pmcEvent.CacheMissPercent,
                        pmcEvent.BranchInstructions, pmcEvent.BranchMispredictions, pmcEvent.BranchMispredictPercent,
                        NoCmdLine ? "" : pmcEvent.Process.CommandLineNoExe);
                }
                return data;
            }
            else
            {
                PrintSummary(data);
            }

            return data;
        }



        /// <summary>
        /// Get column formatter and headline depending on currently configured display settings
        /// </summary>
        /// <param name="data">Sample data to check which columns can be enabled.</param>
        /// <returns>List of formatters</returns>
        List<ColumnFormatter<MatchData>> GetFormatters(MatchData data)
        {
            const int NumberWidth = 18;

            List<ColumnFormatter<MatchData>> formatters = new();

            if( !NoCounters && data.Counters.ContainsKey(PMCNames.InstructionRetired) )
            {
                formatters.Add( new ColumnFormatter<MatchData>()
                {
                    Header = "Instructions".WithWidth(NumberWidth),
                    Formatter = x => "N0".WidthFormat(x.Instructions, NumberWidth)
                });
            }
            if( data.Counters.ContainsKey(PMCNames.TotalCycles) )
            {
                ColumnFormatter<MatchData> defaultFormatter = new()
                {
                    Header = "Cycles".WithWidth(NumberWidth),
                    Formatter = x => "N0".WidthFormat(x.Cycles, NumberWidth)
                };

                if (!NoCounters)
                {
                    formatters.Add(defaultFormatter);
                }

                // we can show CPI as well!
                if ( data.Counters.ContainsKey(PMCNames.InstructionRetired) )
                {
                    ColumnFormatter<MatchData> extended = new()
                    {
                        Color = ConsoleColor.Yellow,
                        Header = "CPI".WithWidth(7),
                        Formatter = x => "F2".WidthFormat(x.CPI, 7)
                    };
                    formatters.Add(extended);
                }
                
            }

            if (!NoCounters && data.Counters.ContainsKey(PMCNames.BranchInstructions))
            {
                formatters.Add(new ColumnFormatter<MatchData>
                {
                    Header = "Branches".WithWidth(NumberWidth),
                    Formatter = x => "N0".WidthFormat(x.BranchInstructions, NumberWidth)
                });
            }

            if ( data.Counters.ContainsKey(PMCNames.BranchMispredictions) )
            {
                if (!NoCounters)
                {
                    formatters.Add(new ColumnFormatter<MatchData>
                    {
                        Header = "BranchMispredicts".WithWidth(NumberWidth),
                        Formatter = x => "N0".WidthFormat(x.BranchMispredictions, NumberWidth)
                    });
                }

                // We can show Branch Mispredict %
                if(data.Counters.ContainsKey(PMCNames.BranchInstructions) )
                {
                    formatters.Add(new ColumnFormatter<MatchData>
                    {
                        Color = ConsoleColor.Magenta,
                        Header = "BrMispredict %".WithWidth(15),
                        Formatter = x => "F2".WidthFormat(x.BranchMispredictPercent, 15)
                    });
                }
            }


            if( !NoCounters &&  data.Counters.ContainsKey(PMCNames.CacheMisses))
            {
                formatters.Add( new ColumnFormatter<MatchData>
                {
                    Header = "CacheMisses".WithWidth(NumberWidth),
                    Formatter = x => "N0".WidthFormat(x.LLCMisses, NumberWidth)
                });
            }
            if( data.Counters.ContainsKey(PMCNames.LLCReference))
            {
                ColumnFormatter<MatchData> defaultFormatter =  new()
                {
                    Header = "LLCReferences".WithWidth(NumberWidth),
                    Formatter = x => "N0".WidthFormat(x.LLCReference, NumberWidth)
                };

                if (!NoCounters)
                {
                    formatters.Add(defaultFormatter);
                }

                // show cache miss rate
                if ( data.Counters.ContainsKey(PMCNames.CacheMisses) )
                {
                    ColumnFormatter<MatchData> extended = new()
                    {
                        Color = ConsoleColor.Green,
                        Header = "CacheMiss %".WithWidth(12),
                        Formatter = x => "F2".WidthFormat(x.CacheMissPercent, 12)
                    };
                    formatters.Add(extended);
                }
            }

            formatters.Add( new ColumnFormatter<MatchData>
            {
                Header = " Process Name",
                Formatter = x => " " + x.Process.GetProcessWithId(UsePrettyProcessName)
            });

            formatters.Add(new ColumnFormatter<MatchData>
            {
                Color = ConsoleColor.DarkCyan,
                Header = "",
                Formatter = x => NoCmdLine ? "" : x.Process.CommandLineNoExe,
            });

            return formatters;
        }

        private void PrintSummary(List<MatchData> data)
        {
            foreach (IGrouping<string, MatchData> timeGroup in data.GroupBy(x => $"{x.PerformedAt} {Path.GetFileNameWithoutExtension(x.SourceFile)}"))
            {
                List<ColumnFormatter<MatchData>> formatters = new();
                Func<MatchData, ulong> sorter = null;

                var firstMatch = timeGroup.FirstOrDefault();
                
                if (firstMatch != null)
                {
                    PrintFileName(firstMatch.SourceFile, null, firstMatch.PerformedAt, firstMatch.BaseLine);

                    formatters = GetFormatters(firstMatch);

                    if (firstMatch.Counters.ContainsKey(PMCNames.InstructionRetired))
                    {
                        sorter = x => x.Counters[PMCNames.InstructionRetired];
                    }
                    else if (firstMatch.Counters.ContainsKey(PMCNames.BranchMispredictions))
                    {
                        sorter = x => x.Counters[PMCNames.BranchMispredictions];
                    }
                    else if (firstMatch.Counters.ContainsKey(PMCNames.LLCMisses))
                    {
                        sorter = x => x.Counters[PMCNames.LLCMisses];
                    }
                }

                IEnumerable<MatchData> sorted = timeGroup;
                if (sorter != null)
                {
                    sorted = timeGroup.OrderBy(sorter);
                }

                foreach(var header in formatters)
                {
                    ColorConsole.Write(header.Header, header.Color);
                }

                Console.WriteLine();

                foreach (MatchData match in sorted)
                {
                    foreach (ColumnFormatter<MatchData> formatter in formatters)
                    {
                        ColorConsole.Write(formatter.Formatter(match), formatter.Color);
                    }
                    Console.WriteLine();
                }
            }
        }

        private List<MatchData> ReadFileData()
        {
            if (myUTestData != null)
            {
                return myUTestData;
            }

            var lret = new List<MatchData>();

            Lazy<SingleTest>[] runData = GetTestRuns(true, SingleTestCaseFilter, TestFileFilter);
            WarnIfNoTestRunsFound(runData);
            foreach (var test in runData)
            {
                foreach (TestDataFile file in test.Value.Files)
                {
                    if (file?.Extract?.PMC?.Counters?.Count == 0)
                    {
                        ColorConsole.WriteError($"Warning: File {Path.GetFileNameWithoutExtension(file.FileName)} does not contain PMC Count data.");
                        continue;
                    }


                    Dictionary<ETWProcessIndex, Dictionary<string, ulong>> counters = new();

                    foreach (IPMCCounter counter in file.Extract.PMC.Counters)
                    {
                        foreach(KeyValuePair<ETWProcessIndex, ulong> process2Counter in counter.ProcessMap)
                        {
                            if( !counters.TryGetValue(process2Counter.Key, out Dictionary<string, ulong> values) )
                            {
                                values = new Dictionary<string, ulong>();
                                counters[process2Counter.Key] = values;
                            }

                            values[counter.CounterName] = process2Counter.Value;
                        }
                    }

                    foreach(KeyValuePair<ETWProcessIndex, Dictionary<string, ulong>> counter in counters)
                    {
                        ETWProcess process = file.Extract.GetProcess(counter.Key);

                        if (process != null)
                        {
                            if (IsMatchingProcessAndCmdLine(file, process.ToProcessKey()))
                            {
                                MatchData data = new()
                                {
                                    SessionStart = file.Extract.SessionStart,
                                    Process = process,
                                    Counters = counter.Value.ToDictionary(pmcKeyValue => PMCNameMap[pmcKeyValue.Key],
                                                                           pmcKeyValue => pmcKeyValue.Value),
                                    File = file,
                                    PerformedAt = file.PerformedAt,
                                    SourceFile = file.JsonExtractFileWhenPresent,
                                    BaseLine = file.Extract?.MainModuleVersion?.ToString() ?? "",
                                };
                                lret.Add(data);
                            }
                        }
                    }
                }
            }

            return lret;
        }

        internal class MatchData
        {
            public DateTimeOffset SessionStart { get; internal set; }
            public TestDataFile File { get; internal set; }
            public string BaseLine { get; internal set; }
            public ETWProcess Process { get; internal set; }
            public Dictionary<PMCNames, ulong> Counters { get; internal set; } = new();

            /// <summary>
            /// Cycles Per Instruction
            /// </summary>
            public double CPI
            {
                get
                {
                    ulong cycles = Cycles;
                    ulong instructions = Instructions;

                    if( cycles > 0 && instructions > 0 )
                    {
                        return (double) cycles / (double) instructions;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }


            public ulong BranchMispredictions
            {
                get { return Counters.TryGetValue(PMCNames.BranchMispredictions, out ulong mispredictions) ? mispredictions : 0; }
            }

            public ulong BranchInstructions
            { 
                get { return Counters.TryGetValue(PMCNames.BranchInstructions, out ulong instructions) ? instructions : 0; } 
            }

            public double BranchMispredictPercent
            {
                get 
                {
                    double mispredicts = BranchMispredictions;
                    double branches = BranchInstructions;

                    double rate = 0.0d; 
                    if( branches > 0.0d)
                    {
                        rate = 100.0d * (mispredicts / branches);
                    }

                    return rate;
                }
            }


            public ulong Instructions
            {
                get { return Counters.TryGetValue(PMCNames.InstructionRetired, out ulong instructions) ? instructions : 0; }
            }

            public ulong Cycles
            {
                get {  return Counters.TryGetValue(PMCNames.TotalCycles, out ulong cycles) ? cycles : 0; }
            }

            public ulong LLCMisses
            {
                get {  return Counters.TryGetValue(PMCNames.LLCMisses, out ulong misses) ? misses : 0; }
            }

            public ulong LLCReference
            {
                get {  return Counters.TryGetValue(PMCNames.LLCReference, out ulong llcReference) ? llcReference : 0; }
            }

            public double CacheMissPercent
            {
                get
                {
                    double misses = LLCMisses;
                    double hits = LLCReference;
                    double rate = 0.0d;
                    if( hits > 0.0d )
                    {
                        rate = 100.0d * (misses / hits);
                    }

                    return rate;
                }
            }

            /// <summary>
            /// Instructions per Cycle
            /// </summary>
            public double IPC
            {
                get
                {
                    double cpi = CPI;
                    if( cpi == 0.0d )
                    {
                        return 0;
                    }

                    return 1.0d / cpi; 
                }

            }

            public DateTime PerformedAt { get; internal set; }
            public string SourceFile { get; internal set; }
        }
    }
}
