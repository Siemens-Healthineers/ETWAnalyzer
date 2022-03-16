//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers.Infrastructure
{
    class TestRunDataAccessor
    {
        public string[] ProcessNameFilter { get; internal set; }
        public bool UsePrettyProcessName { get; set; }

        public string CommandLineFilter { get; internal set; }
        public string TestNameFilter { get; internal set; }
        public SearchOption Recursive { get; internal set; }
        public string FileOrDirectory { get; internal set; }
        public int LastNDays { get; internal set; }
        public int TestRunIndex { get; internal set; } = -1;
        public int TestRunCount { get; internal set; }
        public int SkipNTests { get; internal set; }
        public int LastNDaysSafe { get => LastNDays == 0 ? int.MaxValue : LastNDays; }
        public string TestCase { get; internal set; }
        public string MachineFilter { get; internal set; }

        public int TestsPerRun { get; internal set; }

        public bool? NewProcessFilter { get; internal set; }

        /// <summary>
        /// Get Test Runs sorted by File Modify time ascending. Reading is done read ahead in up to 4 parallel TPL task. 
        /// If you have server GC enabled in your App.Config you should reach 100-150 MB/s read performance
        /// </summary>
        /// <param name="skipNonJsonTests">Skip files which are not from an automated trending test</param>
        /// <param name="testFilter">To prevent reading too much data it needs to now which tests are needed</param>
        /// <param name="fileFilter">Because each Test can have up to two files you can here also configure if anything should be prefetched</param>
        /// <returns>List of filtered tests which upon access trigger preloading of subsequent test data files to achieve high throughput.</returns>
        public Lazy<SingleTest>[] GetTestRuns(bool skipNonJsonTests, Func<SingleTest, bool> testFilter, Func<TestDataFile, bool> fileFilter)
        {
            TestRunData runs = new(FileOrDirectory, Recursive);
            SingleTest[] tests = GetTestRunsFromTestRunData(skipNonJsonTests, runs).Where(testFilter).ToArray();

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

                    if (pendingWork.Count == 0)
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
                            if (working.Count == MaxParallel || started - readIndex > 10)
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

            Lazy<SingleTest>[] sources = tests.Select((x, i) => new Lazy<SingleTest>(() =>
            {
                while (!(i < completedOrNot.Count)) // Check if prefetcher did already try to prefetch test
                {
                    Thread.Sleep(1);
                }
                readIndex = i;  // publish read index so we can read more data. We MUST never be behind the actual reader or we will prefetch data which will never be released
                                // resulting in multi GB Heaps because the reader will not null out the TestDataFile.Extract value anymore.
                return completedOrNot[i].Result;
            })).ToArray();


            return sources;
        }

        internal SingleTest[] GetTestRunsFromTestRunData(bool skipNonJsonTests, TestRunData runs)
        {
            var singleTests = new List<SingleTest>();

            if (TestRunIndex != -1)
            {
                if (TestRunIndex > runs.Runs.Count - 1)
                {
                    throw new ArgumentException($"Test Run Index is too large. Allowed values are 0 - {runs.Runs.Count - 1}");
                }
            }

            int maxIndex = (TestRunIndex != -1 && TestRunCount > 0) ? TestRunIndex + TestRunCount : runs.Runs.Count;
            maxIndex = Math.Min(maxIndex, runs.Runs.Count); // Clamp value so we do not get out of bounds exceptions
            int startIndex = TestRunIndex == -1 ? 0 : TestRunIndex;
            startIndex = Math.Max(0, Math.Min(startIndex, runs.Runs.Count));  // clamp start index between 0 and Count-1

            for (int i = startIndex; i < maxIndex; i++)
            {
                TestRun run = runs.Runs[i];

                foreach (var test in run.Tests)
                {
                    var tests = test.Value.Skip(SkipNTests).Take(TestsPerRun == 0 ? int.MaxValue : TestsPerRun);
                    if (skipNonJsonTests)
                    {
                        tests = tests.Where(x => x.Files.All(x => x.JsonExtractFileWhenPresent != null));
                    }
                    singleTests.AddRange(tests);
                }
            }

            var now = DateTime.Now;

            var testsOrderedByTime = singleTests.Where(x => (now - x.PerformedAt).TotalDays < LastNDaysSafe).OrderBy(GetTestTime).ToArray();

            return testsOrderedByTime;
        }

        protected bool IsMatchingProcessAndCmdLine(TestDataFile file, ProcessKey process)
        {
            ETWProcess proc = file.FindProcessByKey(process);
            if (proc == null)
            {
                return false;
            }

            // filter by process name like cmd.exe and with pid like cmd.exe(100)
            if (!Matcher.IsMatch(ProcessNameFilter, MatchingMode.CaseInsensitive, proc.GetProcessName(UsePrettyProcessName)) &&
                !Matcher.IsMatch(ProcessNameFilter, MatchingMode.CaseInsensitive, proc.GetProcessWithId(UsePrettyProcessName)))
            {
                return false;
            }


            bool lret = true;
            if (NewProcessFilter.HasValue)
            {
                lret = proc.IsNew == NewProcessFilter.Value;
            }
            if (lret)
            {
                lret = Matcher.IsMatch(CommandLineFilter, MatchingMode.CaseInsensitive, proc.CmdLine ?? "");
            }

            return lret;
        }


        protected DateTimeOffset GetTestTime(SingleTest test)
        {
            DateTimeOffset lret = test.PerformedAt;
            if (!test.Files[0].IsValidTest && test.Files[0].JsonExtractFileWhenPresent != null)
            {
                lret = test.Files[0].Extract.SessionStart;
            }

            return lret;
        }
    }
}
