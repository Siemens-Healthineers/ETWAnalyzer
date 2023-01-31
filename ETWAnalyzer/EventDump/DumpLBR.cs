using ETWAnalyzer.Extract;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using ETWAnalyzer.TraceProcessorHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.EventDump
{
    internal class DumpLBR : DumpFileDirBase<DumpLBR.MatchData>
    {
        internal List<MatchData> myUTestData;

        public KeyValuePair<string, Func<string, bool>> MethodFilter { get; internal set; }

        /// <summary>
        /// -topn Processes are sorted by total call count, Take the top n processes for output which is a good default. 
        /// If you need different filters you can omit that filter
        /// </summary>
        public SkipTakeRange TopN { get; internal set; } = new SkipTakeRange();

        /// <summary>
        /// When not in process total mode select with -topnmethods the number of methods to print
        /// </summary>
        public SkipTakeRange TopNMethods { get; internal set; } = new SkipTakeRange();

        /// <summary>
        /// Configured by command line switches -includedll, -includeargs 
        /// </summary>
        public MethodFormatter MethodFormatter { get; internal set; } = new MethodFormatter();

        /// <summary>
        /// Do not display command line
        /// </summary>
        public bool NoCmdLine { get; internal set; }

        /// <summary>
        /// Show caller callee
        /// </summary>
        public bool ShowCaller { get; internal set; }

        /// <summary>
        /// Scale sampled called call counts with an experimentally determined scaling factor
        /// </summary>
        public int ScalingFactor { get; internal set; } = 1;

        /// <summary>
        /// Method call count filter
        /// </summary>
        public MinMaxRange<int> MinMaxCount { get; internal set; } = new MinMaxRange<int>();


        /// <summary>
        /// Cache results of filters
        /// </summary>
        readonly Dictionary<string, bool> myMethodFilterResultsCache = new();

        public override List<MatchData> ExecuteInternal()
        {
            List<MatchData> data = ReadFileData();

            if (IsCSVEnabled)
            {
                OpenCSVWithHeader(Col_CSVOptions, "Directory", Col_FileName, Col_Date, Col_TestCase, Col_TestTimeinms, Col_Baseline, Col_Process, 
                    Col_ProcessName, "Caller", "MethodName", "Count", Col_CommandLine);

                foreach (var lbrEvent in data)
                {
                    WriteCSVLine(CSVOptions, Path.GetDirectoryName(lbrEvent.File.FileName),
                        Path.GetFileNameWithoutExtension(lbrEvent.File.FileName), lbrEvent.File.PerformedAt, lbrEvent.File.TestName, lbrEvent.File.DurationInMs, lbrEvent.BaseLine,
                        lbrEvent.Process.ProcessWithID, lbrEvent.Process.ProcessNamePretty,
                        lbrEvent.Caller, lbrEvent.MethodName, lbrEvent.Count,
                        NoCmdLine ? "" : lbrEvent.Process.CommandLineNoExe);
                }
                return data;
            }
            else
            {
                PrintSummary(data);
            }

            return data;
        }


        private void PrintSummary(List<MatchData> data)
        {
            List<ColumnFormatter<MatchData>> columnPrinters = GetFormatters();
           
            foreach (IGrouping<string, MatchData> timeGroup in data.GroupBy(x => $"{x.PerformedAt} {Path.GetFileNameWithoutExtension(x.SourceFile)}"))
            {
                var firstMatch = timeGroup.FirstOrDefault();

                if (firstMatch != null)
                {
                    PrintFileName(firstMatch.SourceFile, null, firstMatch.PerformedAt, firstMatch.BaseLine);
                    PrintHeader(columnPrinters);
                }

                foreach (IGrouping<ETWProcess, MatchData> byProcess in timeGroup.GroupBy(x => x.Process).SortAscendingGetTopNLast(x => x.Sum(c=>c.Count), null, TopN) )
                {

                    if (!TopNMethods.IsEmpty || MethodFilter.Key != null)
                    {
                        ColorConsole.WriteEmbeddedColorLine($"{byProcess.Key.GetProcessWithId(UsePrettyProcessName)}");
                        foreach(var byMethod in byProcess.GroupBy(x=>x.MethodName).SortAscendingGetTopNLast(x=>x.Sum(c=>c.Count), null, TopNMethods))
                        {
                            MatchData sum = new MatchData
                            {
                                Count = byMethod.Sum(x => x.Count),
                                MethodName = byMethod.Key,
                                IsAggregate = true
                            };

                            if (MinMaxCount.IsWithin((int) sum.Count))
                            {
                                PrintRow(columnPrinters, sum);
                            }

                            if (ShowCaller)
                            {
                                foreach (MatchData caller in byMethod.OrderByDescending(x=>x.Count).Where(x=> MinMaxCount.IsWithin(((int) x.Count))) )
                                {
                                    PrintRow(columnPrinters, caller);
                                }
                            }
                        }
                    }
                    else
                    {
                        MatchData sum = new MatchData
                        {
                            Process = byProcess.Key,
                            Count = byProcess.Sum(x => x.Count),
                            IsAggregate = true
                        };
                        if (MinMaxCount.IsWithin((int)sum.Count))
                        {
                            PrintRow(columnPrinters, sum);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Get column formatter and headline depending on currently configured display settings
        /// </summary>
        /// <returns>List of formatters</returns>
        List<ColumnFormatter<MatchData>> GetFormatters()
        {
            const int NumberWidth = 18;
            const int ProcessNameWidth = -45;


            List<ColumnFormatter<MatchData>> formatters = new();

            if (!TopNMethods.IsEmpty || MethodFilter.Key != null)
            {
                // Per Process method mode
                formatters.Add(new ColumnFormatter<MatchData>
                {
                    Header = "Call Count".WithWidth(NumberWidth),
                    Color = ConsoleColor.Green,
                    Formatter = x => x.IsAggregate ? "N0".WidthFormat(x.Count, NumberWidth) : "",
                });
                formatters.Add(new ColumnFormatter<MatchData>
                {
                    Header = "Callee".WithWidth(NumberWidth),
                    Formatter = x => x.IsAggregate ? " " + x.MethodName : "",
                });

                formatters.Add(new ColumnFormatter<MatchData>
                {
                    Header = "",
                    Color = ConsoleColor.Magenta,
                    Formatter = x => !x.IsAggregate ? "   " + "N0".WidthFormat(x.Count, NumberWidth) : "",
                });
                formatters.Add(new ColumnFormatter<MatchData>
                {
                    Header = "",
                    Formatter = x => !x.IsAggregate ? " " + x.Caller : "",
                });
             }
            else // Process Total mode
            {
                formatters.Add(new ColumnFormatter<MatchData>()
                {
                    Header = "Call Count".WithWidth(NumberWidth),
                    Color = ConsoleColor.Green,
                    Formatter = x => "N0".WidthFormat(x.Count, NumberWidth)
                });
                formatters.Add(new ColumnFormatter<MatchData>
                {
                    Header = " Process Name".WithWidth(ProcessNameWidth),
                    Formatter = x => " " + x.Process.GetProcessWithId(UsePrettyProcessName),
                });

                formatters.Add(new ColumnFormatter<MatchData>
                {
                    Color = ConsoleColor.DarkCyan,
                    Header = "",
                    Formatter = x => NoCmdLine ? "" : " " + x.Process.CommandLineNoExe,
                });
            }

            return formatters;
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
                    var calls = file?.Extract?.PMC?.LBRData?.GetMethodCalls(file?.Extract);

                    if (calls == null || calls.Count == 0) 
                    {
                        ColorConsole.WriteError($"Warning: File {Path.GetFileNameWithoutExtension(file.FileName)} does not contain LBR call count data.");
                        continue;
                    }

                    foreach(var call in calls.OrderBy(x=>x.Count))
                    {
                        if (call.Process != null)
                        {
                            if( IsMatchingProcessAndCmdLine(file, call.Process.ToProcessKey()) &&
                                IsMethodMatching(myMethodFilterResultsCache, MethodFilter.Value, MethodFormatter.Format(call.MethodName, noCut: true)) )
                            {
                                MatchData data = new()
                                {
                                    File = file,
                                    PerformedAt = file.PerformedAt,
                                    SourceFile = file.JsonExtractFileWhenPresent,
                                    BaseLine = file.Extract?.MainModuleVersion?.ToString() ?? "",
                                    Process = call.Process,
                                    MethodName = MethodFormatter.Format(call.MethodName),
                                    Caller = MethodFormatter.Format(call.Caller),
                                    Count = call.Count * ScalingFactor,
                                };

                                lret.Add(data);
                            }
                        }
                    }
                }
            }

            return lret;
        }

        private bool IsMethodMatching(Dictionary<string, bool> cache, Func<string, bool> filter, string method)
        {
            if (filter == null) // no filter means that everything matches
            {
                return true;
            }

            if (!cache.TryGetValue(method, out bool lret))
            {
                lret = filter(method);
                cache[method] = lret;
            }

            return lret;
        }


        void PrintHeader(List<ColumnFormatter<MatchData>> formatters)
        {
            foreach (ColumnFormatter<MatchData> header in formatters)
            {
                ColorConsole.Write(header.Header, header.Color);
            }
            Console.WriteLine();
        }

        void PrintRow(List<ColumnFormatter<MatchData>> formatters, MatchData data)
        {
            foreach (ColumnFormatter<MatchData> row in formatters)
            {
                ColorConsole.Write(row.Formatter(data), row.Color);
            }
            Console.WriteLine();
        }

        internal class MatchData
        {
            public ETWProcess Process { get; internal set; }
            public string MethodName { get; internal set; }
            public string Caller { get; internal set; }
            public long Count { get; internal set; }
            public TestDataFile File { get; internal set; }
            public string BaseLine { get; internal set; }
            public DateTime PerformedAt { get; internal set; }
            public string SourceFile { get; internal set; }
            public bool IsAggregate { get; internal set; }
        }
    }
}
