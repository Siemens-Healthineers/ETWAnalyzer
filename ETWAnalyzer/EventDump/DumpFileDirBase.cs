//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers;
using ETWAnalyzer.Analyzers.Infrastructure;
using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Modules;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using ETWAnalyzer.TraceProcessorHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ETWAnalyzer.Commands.DumpCommand;
using static ETWAnalyzer.Extract.ETWProcess;

namespace ETWAnalyzer.EventDump
{
    internal abstract class DumpFileDirBase<T> : DumpBase<T>
    {
        public Func<string, bool> ProcessNameFilter { get; set; } = _ => true;
        public Func<string, bool> CommandLineFilter { get; set; } = _ => true;

        public List<MinMaxRange<int>> MinMaxMsTestTimes = new();

        public bool ShowFullFileName { get; set; }

        public ZeroTimeModes ZeroTimeMode { get; set; }
        public KeyValuePair<string, Func<string, bool>> ZeroTimeFilter { get; set; } = new KeyValuePair<string, Func<string, bool>>(null, _ => false);
        public Func<string, bool> ZeroTimeProcessNameFilter { get; set; } = (x) => true;

        public SearchOption Recursive { get; internal set; }
        public List<string> FileOrDirectoryQueries { get; internal set; } = new List<string>();
        public double LastNDays { get; internal set; } = double.MaxValue;
        public int TestRunIndex { get; internal set; } = -1;
        public int TestRunCount { get; internal set; }
        public int SkipNTests { get; internal set; }
        public string TestCase { get; internal set; }

        public int TestsPerRun { get; internal set; }

        public string CSVFile { get; internal set; }

        public bool NoCSVSeparator { get; internal set; }

        /// <summary>
        /// Show module file name and version. In cpu total mode also exe version.
        /// </summary>
        public bool ShowModuleInfo { get; internal set; }

        /// <summary>
        /// Module filter result cache because regular expression matching with many time of the same module will be costly
        /// </summary>
        Dictionary<ModuleDefinition, bool> myModuleFilterResult = new();

        /// <summary>
        /// Argument passed after -smi which filters on all properties of displayed modules
        /// </summary>
        public KeyValuePair<string, Func<string, bool>> ShowModuleFilter { get; internal set; }

        const char CSVSeparator = ';';
        protected string CSVSepStr = new(CSVSeparator, 1);
        protected StreamWriter myWriter;

        public bool IsCSVEnabled => !String.IsNullOrEmpty(CSVFile);
        
        public ProcessStates? NewProcessFilter { get; internal set; }

        string myCSVOptions = Environment.CommandLine;

        /// <summary>
        /// Used by GetZeroFromMethod to get the full qualified method name excluding dll name
        /// </summary>
        MethodFormatter myFormatter = new MethodFormatter();

        /// <summary>
        /// When multiple zero timepoints are matched we print a warning if the multiple definitions are off greater than this value
        /// </summary>
        const float ZeroDiffThresholdInS = 0.010f;

        /// <summary>
        /// Cache last retrieve ZeroTime because it can be expensive to lookup.
        /// Input args are ZeroTimeMode, Extract, ProcessFilter, ZeroFilter, last is the cached zerotime value
        /// </summary>
        Tuple<ZeroTimeModes, WeakReference, Func<string, bool>, string, double> myLastZeroTime;

        /// <summary>
        /// Used for unit testing
        /// </summary>
        internal Lazy<SingleTest>[] myPreloadedTests = null;


        /// <summary>
        /// Return on first read the current extract command line options so it can easily be added to CSV output once
        /// </summary>
        public string CSVOptions
        {
            get
            {
                string tmp = myCSVOptions;
                myCSVOptions = null;
                return tmp;
            }
                
        }

        /// <summary>
        /// Get printable file name depending on ShowFullFileName flag from full file path
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>full file path or just the file name.</returns>
        protected string GetPrintFileName(string fileName)
        {
            return ShowFullFileName ? fileName : Path.GetFileNameWithoutExtension(fileName);
        }

        /// <summary>
        /// Print file name string in a common format with the same colors
        /// </summary>
        /// <param name="fileName">full path to file name</param>
        /// <param name="totalString">totals per file if needed</param>
        /// <param name="performedAt">Time when test did run</param>
        /// <param name="baseline">Version which was running</param>
        protected void PrintFileName(string fileName, string totalString, DateTime performedAt, string baseline)
        {
            ColorConsole.WriteEmbeddedColorLine($"{performedAt,-22} {totalString}{GetPrintFileName(fileName)} {baseline}", ConsoleColor.Cyan);
        }

        protected void OpenCSVWithHeader(params string[] csvColumns)
        {
            FileStream csv = new(CSVFile, FileMode.Create, FileAccess.Write, FileShare.None); 
            myWriter = new StreamWriter(csv);
            if(csvColumns != null )
            {
                if (NoCSVSeparator == false)
                {
                    myWriter.WriteLine("sep = " + CSVSepStr);
                }
                myWriter.WriteLine(String.Join(CSVSepStr, csvColumns));
            }
        }


        protected void WriteCSVLine(params object[] columnArgs)
        {
            string line = String.Join(CSVSepStr, columnArgs.Select(x =>
                 {
                     if( x == null )
                     {
                         return "";
                     }
                     string nonSepStr = x.ToString().Replace(CSVSeparator, '_').Replace('"', '_');
                     // For Excel quote multiline strings
                     if ( nonSepStr.Contains('\n') )
                     {
                         nonSepStr = "\"" + nonSepStr + "\"";
                     }

                     return nonSepStr;
                }));
            if( myWriter == null)
            {
                throw new InvalidOperationException($"Output StreamWriter for CSV File is not initialized!");
            }
            myWriter.WriteLine(line);
        }

        /// <summary>
        /// Filter by test case time
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        protected bool SingleTestCaseFilter(SingleTest test)
        {
            return MinMaxMsTestTimes.Count == 0 || MinMaxMsTestTimes.Any(x => x.IsWithin(test.DurationInMs));
        }

        /// <summary>
        /// Filter by file name
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        protected bool TestFileFilter(TestDataFile file)
        {
            return true;
        }
		
        protected static void WarnIfNoTestRunsFound(Lazy<SingleTest>[] runs)
        {
            if (runs.Length == 0)
            {
                ColorConsole.WriteError("No files to process were found.");
            }
        }

        /// <summary>
        /// Get defined time zero point for current ETW file, if -zerotime was used. 
        /// </summary>
        /// <param name="extract"></param>
        /// <returns>Time of first matching marker, ETW Session start time otherwise.</returns>
        /// <exception cref="NotSupportedException"></exception>
        protected double GetZeroTimeInS(IETWExtract extract)
        {
            double ret = 0.0d;

            if (myLastZeroTime == null ||
                myLastZeroTime.Item1 != ZeroTimeMode ||
                myLastZeroTime.Item2.Target != extract ||
                myLastZeroTime.Item3 != ZeroTimeProcessNameFilter ||
                myLastZeroTime.Item4 != ZeroTimeFilter.Key
                )
            {
                ret = ZeroTimeMode switch
                {
                    ZeroTimeModes.None => 0.0d,
                    ZeroTimeModes.Marker => GetFirstMatchingMarker(extract),
                    ZeroTimeModes.First => GetZeroMethodOrStackTag(extract, useFirstOccurrence: true),
                    ZeroTimeModes.Last => GetZeroMethodOrStackTag(extract, useFirstOccurrence: false),
                    ZeroTimeModes.ProcessStart => GetZeroProcessTime(extract),
                    ZeroTimeModes.ProcessEnd => GetZeroProcessTime(extract, useProcessEnd: true),
                    _ => throw new NotSupportedException($"This ZeroTime mode is not implemented yet.")
                };

                myLastZeroTime = Tuple.Create(ZeroTimeMode, new WeakReference(extract), ZeroTimeProcessNameFilter, ZeroTimeFilter.Key, ret);
            }
            else
            {
                ret = myLastZeroTime.Item5;
            }

            return ret;
        }


        private double GetZeroProcessTime(IETWExtract extract, bool useProcessEnd= false)
        {
            double timeZeroInS = 0.0d;
            int matches = 0;
            foreach(ETWProcess process in extract.Processes.Where(x=> useProcessEnd ? x.HasEnded : x.IsNew))
            {
                if (!ZeroTimeProcessNameFilter(process.GetProcessWithId(UsePrettyProcessName)))
                {
                    continue;
                }

                if(ZeroTimeFilter.Key != null && !ZeroTimeFilter.Value(process.CmdLine))
                {
                    continue;
                }

                if (matches == 0)
                {
                    timeZeroInS = ( (useProcessEnd ? process.EndTime : process.StartTime) - extract.SessionStart).TotalSeconds;
                }

                if( Program.DebugOutput )
                {
                    ColorConsole.WriteLine($"Zero Process {(useProcessEnd ? "End" : "Start")}: {((useProcessEnd ? process.EndTime : process.StartTime) - extract.SessionStart).TotalSeconds:F3}s, End:  {process.GetProcessWithId(UsePrettyProcessName)} CommandLine: {process.CmdLine}", ConsoleColor.Yellow);
                }

                matches++;
            }

            if( matches > 1 )
            {
                ColorConsole.WriteLine($"Warning multiple processes ({matches}) did match for zero time filter. Filter is ambiguous! Add -debug switch to print all matching processes.", ConsoleColor.Yellow);
            }
            else if( matches == 0)
            {
                ColorConsole.WriteLine($"Warning zero time filter for processes did not match any process! No time shift is applied for this file!", ConsoleColor.Red);
            }

            return timeZeroInS;
        }

        protected override string GetAbbreviatedName(TimeFormats format)
        {
            string str = base.GetAbbreviatedName(format);
            // When zero time filter is active add a * to column to signal that we are dealing with shifted times here
            if (ZeroTimeMode != ZeroTimeModes.None)
            {
                str += "*";
            }

            return str;
        }


        /// <summary>
        /// Get timepoint which defines time zero. 
        /// </summary>
        /// <param name="extract"></param>
        /// <param name="useFirstOccurrence">If true use First occurrence. Otherwise the last occurrence is tried to locate.</param>
        /// <returns>If no match use Session start are zero point.</returns>
        protected double GetZeroMethodOrStackTag(IETWExtract extract, bool useFirstOccurrence)
        {
            double lret = 0.0d;
            int matches = 0;
            float firstZero = 0;
            float otherZero = 0;
            bool bFound = false;

            GetZeroFromMethod(extract, useFirstOccurrence, ref bFound, ref lret, ref matches, ref firstZero, ref otherZero);
            GetZeroFromStackTags(extract, useFirstOccurrence, ref bFound, ref lret, ref matches, ref firstZero, ref otherZero);

            // Print warning if multiple zero timepoint definitions are present
            if (matches > 1 && Math.Abs(otherZero - firstZero) > ZeroDiffThresholdInS)
            {
                ColorConsole.WriteLine($"Warning: Zero timepoint definition did match {matches} times. Zero Delta time is {(otherZero - firstZero):F3}s Use -debug switch to see all matched definitions.", ConsoleColor.Yellow);
            }
            else if( matches == 0 )
            {
                ColorConsole.WriteLine($"Warning: Zero timepoint definition did not match. No timepoint shift is applied to this file!", ConsoleColor.Red);
            }

            return lret;
        }

        private void GetZeroFromStackTags(IETWExtract extract, bool useFirstOccurrence, ref bool bFound, ref double lret, ref int matches, ref float firstZero, ref float otherZero)
        {
            if( extract?.SummaryStackTags?.Stats == null)
            {
                return;
            }

            foreach(KeyValuePair<ProcessKey, IReadOnlyList<IStackTagDuration>> process2Tags in extract.SummaryStackTags.Stats.Concat(extract.SpecialStackTags.Stats))
            {
                if( process2Tags.Key.Pid <= 0 )
                {
                    continue;
                }

                ETWProcess process = extract.TryGetProcessByPID(process2Tags.Key.Pid, process2Tags.Key.StartTime);
                if( process == null || !ZeroTimeProcessNameFilter( process.GetProcessWithId(UsePrettyProcessName) ) )
                {
                    continue;
                }
                
                foreach(IStackTagDuration tag in process2Tags.Value)
                {
                    if ( !ZeroTimeFilter.Value( tag.Stacktag ) )
                    {
                        continue;
                    }

                    matches++;
                    float zero = useFirstOccurrence ? (float) tag.GetFirstOccurrenceS(extract.SessionStart, 0.0d) : (float) tag.GetLastOccurrenceS(extract.SessionStart, 0.0d);

                    if( !bFound )
                    {
                        lret = zero;
                        firstZero = zero;
                    }
                    else
                    {
                        otherZero = Math.Max(zero, otherZero);
                    }

                    bFound = true;

                    string msg = $"\tZero stacktag First: {tag.GetFirstOccurrenceS(extract.SessionStart, 0.0d),-8} Last: {tag.GetLastOccurrenceS(extract.SessionStart, 0.0d),-8} process {process.GetProcessWithId(UsePrettyProcessName)} {tag.Stacktag}";
                    Logger.Instance.Write(msg);

                    if ( Program.DebugOutput )
                    {
                        ColorConsole.WriteLine(msg, ConsoleColor.Yellow);
                    }
                }
            }   
        }

        private void GetZeroFromMethod(IETWExtract extract, bool useFirstOccurence, ref bool bFound, ref double lret, ref int matches, ref float firstZero, ref float otherZero)
        {
            if (extract?.CPU?.PerProcessMethodCostsInclusive?.MethodStatsPerProcess?.Count == 0 )
            {
                return;
            }

            // Do a full loop over all matches to count potentially ambigious zero timepoint definitions
            foreach (var processMethods in extract.CPU.PerProcessMethodCostsInclusive.MethodStatsPerProcess)
            {
                var key = processMethods.Process;
                if (key.Pid <= 0)
                {
                    continue;
                }

                ETWProcess process = extract.TryGetProcessByPID(key.Pid, key.StartTime);
                if (process == null || !ZeroTimeProcessNameFilter(process.GetProcessWithId(UsePrettyProcessName)) ) 
                {
                    continue;
                }

                foreach (MethodCost cost in processMethods.Costs.OrderBy(x => useFirstOccurence ? x.FirstOccurenceInSecond : x.LastOccurenceInSecond))
                {
                    if ( !ZeroTimeFilter.Value( myFormatter.Format( cost.Method, noCut: true) ) )
                    {
                        continue;
                    }

                    matches++;
                    float zero = useFirstOccurence ? cost.FirstOccurenceInSecond : cost.LastOccurenceInSecond;

                    if ( !bFound )
                    {
                        // only set occurrence once
                        lret  = zero;
                        firstZero = zero;
                    }
                    else
                    {
                        otherZero = Math.Max(zero, otherZero);
                    }

                    bFound = true;

                    string msg = $"\tZero method First: {cost.FirstOccurenceInSecond,-8}s, Last: {cost.LastOccurenceInSecond,-8} process {process.GetProcessWithId(UsePrettyProcessName)} {myFormatter.Format(cost.Method, noCut: true)}";
                    Logger.Instance.Write(msg);

                    if (Program.DebugOutput)
                    {
                        ColorConsole.WriteLine(msg, ConsoleColor.Yellow);
                    }
                }
            }
        }

        /// <summary>
        /// Get first marker which matches marker filter
        /// </summary>
        /// <param name="extract">Extract to search in</param>
        /// <returns>Time of first matching marker, ETW Session start time otherwise.</returns>
        double GetFirstMatchingMarker(IETWExtract extract)
        {
            double lret = 0.0d;
            if( extract?.ETWMarks?.Count >  0)
            {
                foreach(var marker in extract.ETWMarks.Where(x=> ZeroTimeFilter.Value(x.MarkMessage)) )
                {
                    lret = (marker.Time - extract.SessionStart).TotalSeconds;
                    break;
                }
            }
            return lret;
        }

        /// <summary>
        /// Get Test Runs sorted by File Modify time ascending. Reading is done read ahead in up to 4 parallel TPL task. 
        /// If you have server GC enabled in your App.Config you should reach 100-150 MB/s read performance
        /// </summary>
        /// <param name="skipNonJsonTests">Skip files which are not from an automated trending test</param>
        /// <param name="testFilter">To prevent reading too much data it needs to now which tests are needed</param>
        /// <param name="fileFilter">Because each Test can have up to two files you can here also configure if anything should be prefetched</param>
        /// <returns>List of filtered tests which upon access trigger preloading of subsequent test data files to achieve high throughput.</returns>
        public Lazy<SingleTest>[] GetTestRuns(bool skipNonJsonTests, Func<SingleTest,bool> testFilter, Func<TestDataFile,bool> fileFilter)
        {
            if( myPreloadedTests != null )
            {
                return myPreloadedTests;
            }

            TestRunData runs = new(FileOrDirectoryQueries, Recursive, FileOrDirectoryQueries.First());
            SingleTest[] tests = GetTestRunsFromTestRunData(runs, skipNonJsonTests, false, TestRunIndex, TestRunCount, SkipNTests, TestsPerRun, LastNDays)
                                 .Where(testFilter).ToArray();

            Queue<SingleTest> pendingWork = new(tests);  // tests are prefetched in order because normally we read things from oldest to youngest
            Queue<Task<SingleTest>> working = new();
            List<Task<SingleTest>> completedOrNot = new();
            int readIndex = 0;
            int started = 0;
            int MaxParallel = 4;

            Task jsonReadingPrefetcher = Task.Run(() =>
            {
                while (true)
                {
                 //   Console.WriteLine($"ReadIndex {readIndex} Started: {started}");
                    while (working.Count <= MaxParallel && pendingWork.Count > 0)
                    {
                        started++;
                        SingleTest work = pendingWork.Dequeue();
                        Task<SingleTest> t = Task.Run<SingleTest>(() =>
                        {
                            // read in up to MaxParallel Tasks the Json which is read and deserialized when accessing the 
                            // file.Extract property
                            foreach (var file in work.Files.Where(fileFilter))
                            {
                                var tmp = file.Extract;
                            }
                            return work;
                        });

                        completedOrNot.Add(t);  // Needed for Lazy to check if already a task for prefetching exists
                        working.Enqueue(t);     // store pending work 
                    }

                    if( pendingWork.Count == 0 )
                    {
                        break;
                    }

                    // throttle reading when up to MaxParallel tasks are there or
                    // if the reader cannot keep up accessing new items while we are prefetching potentially too much data
                    while (working.Count > 0)
                    {
                        var tmp = working.Peek();
                        if (tmp.IsCompleted) // remove completed tasks from list so count only active tasks in enqueue order
                        {
                            working.Dequeue();
                        }
                        else
                        {
                            if (working.Count == MaxParallel || started-readIndex > 10)
                            {
                                Thread.Sleep(1);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            });

            Lazy<SingleTest>[] sources = tests.Select( (x,i) => new Lazy<SingleTest>( ()=>
            {
                while( !(i < completedOrNot.Count) ) // Check if prefetcher did already try to prefetch test
                {
                    Thread.Sleep(1);
                }
                readIndex = i;  // publish read index so we can read more data. We MUST never be behind the actual reader or we will prefetch data which will never be released
                                // resulting in multi GB Heaps because the reader will not null out the TestDataFile.Extract value anymore.
                return completedOrNot[i].Result;
            })).ToArray();
            

            return sources;
        }


        /// <summary>
        /// Get from different file queries the tests which are relevant for extraction (zip or etl files) or dumping (json files).
        /// To be useful the input files must adhere to our test case naming convention. If not only the lastNDays flag is useful.
        /// </summary>
        /// <param name="runs">Input files</param>
        /// <param name="skipNonJsonTests">If true compressed files are skipped (used during dump)</param>
        /// <param name="skipJsonTests">If true Json files are excluded (used during extract)</param>
        /// <param name="testRunIndex">Return only tests of the n-th TestRun</param>
        /// <param name="testRunCount">Return the next N TestRuns starting at testRunIndex</param>
        /// <param name="skipNTests">Skip the first n tests of a testcase</param>
        /// <param name="testsPerRun">Take N tests from a testcase per TestRun</param>
        /// <param name="lastNDays">Take only tests which are not older than N days</param>
        /// <returns>Filtered set of test files.</returns>
        /// <exception cref="ArgumentException">Test Run Index was out of bounds</exception>
        internal static SingleTest[] GetTestRunsFromTestRunData(TestRunData runs, bool skipNonJsonTests, bool skipJsonTests, int testRunIndex, int testRunCount, int skipNTests, int testsPerRun, double lastNDays)
        {
            var singleTests = new List<SingleTest>();

            TestRun[] filteredRuns = runs.Runs.ToArray();

            if (skipJsonTests)   // ignore json only files during extraction
            {
                filteredRuns = runs.Runs.Where(run =>
                                     run.Tests.Any(singleTests =>
                                        singleTests.Value.Any(singleTest =>
                                            singleTest.Files.Any(testDataFile =>
                                                Path.GetExtension(testDataFile.FileName) != TestRun.ExtractExtension)))).ToArray();
            }

            if (testRunIndex != -1)
            {
                if (testRunIndex > filteredRuns.Length - 1)
                {
                    throw new ArgumentException($"Test Run Index is too large. Allowed values are 0 - {filteredRuns.Length - 1}");
                }
            }

            int maxIndex = (testRunIndex != -1 && testRunCount > 0) ? testRunIndex + testRunCount : filteredRuns.Length;
            maxIndex = Math.Min(maxIndex, filteredRuns.Length); // Clamp value so we do not get out of bounds exceptions
            int startIndex = testRunIndex == -1 ? 0 : testRunIndex;
            startIndex = Math.Max(0, Math.Min(startIndex, filteredRuns.Length));  // clamp start index between 0 and Count-1

            for (int i = startIndex; i < maxIndex; i++)
            {
                TestRun run = filteredRuns[i];

                foreach (var test in run.Tests)
                {
                    var tests = test.Value.Skip(skipNTests).Take(testsPerRun == 0 ? int.MaxValue : testsPerRun);
                    if (skipNonJsonTests)
                    {
                        tests = tests.Where(x => x.Files.All(x => x.JsonExtractFileWhenPresent != null));
                    }
                    singleTests.AddRange(tests);
                }
            }

            var now = DateTime.Now;

            var testsOrderedByTime = singleTests.Where(x => (now - x.PerformedAt).TotalDays < lastNDays).OrderBy(GetTestTime).ToArray();

            return testsOrderedByTime;
        }

        protected bool IsMatchingProcessAndCmdLine(TestDataFile file, ProcessKey process)
        {
            ETWProcess proc = file.FindProcessByKey(process);
            if (proc == null)
            {
                return false;
            }

            // filter by process name with pid like cmd.exe(100)
            if (!ProcessNameFilter(proc.GetProcessWithId(UsePrettyProcessName)) )
            {
                return false;
            }


            bool lret = proc.IsMatch(NewProcessFilter);
            if( lret )
            {
                lret = CommandLineFilter(proc.CmdLine);
            }

            return lret;
        }

        /// <summary>
        /// Check if module matches curent -smi filter regular expression.
        /// </summary>
        /// <param name="module">Module (can be null)</param>
        /// <returns>true if module matches, false otherwise</returns>
        protected internal bool IsMatchingModule(ModuleDefinition module)
        {
            if (module == null)
            {
                return ShowModuleFilter.Key != null ? false : true; // When we have a filter we omit everything which has no module to get rid of not matching output
            }

            if (!myModuleFilterResult.TryGetValue(module, out bool lret))
            {
                string moduleString = GetModuleString(module, true);
                lret = ShowModuleFilter.Value.Invoke(moduleString);
                myModuleFilterResult[module] = lret;
            }

            return lret;
        }


        protected static DateTimeOffset GetTestTime(SingleTest test)
        {
            DateTimeOffset lret = test.PerformedAt;
            if ( !test.Files[0].IsValidTest && test.Files[0].JsonExtractFileWhenPresent != null)
            {
                lret = test.Files[0].Extract.SessionStart;
            }

            return lret;
        }

        protected override void Dispose(bool disposing)
        {
            if (myWriter != null)
            {
                myWriter.Dispose();
                myWriter = null;
                Console.WriteLine($"CSV export was successful. File is: {CSVFile}");
            }

            base.Dispose(disposing);

        }
    }
}
