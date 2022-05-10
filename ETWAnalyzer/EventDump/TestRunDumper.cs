//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ETWAnalyzer.EventDump
{
    /// <summary>
    /// Dump a directory which contains ETL files or extracted Json files which contain profiling data of 
    /// previous test runs
    /// </summary>
    class TestRunDumper : DumpBase<string>
    {
        public List<string> Directories { get; set; }
        public SearchOption Recursive   { get; set;  } = SearchOption.TopDirectoryOnly;
        public bool PrintFiles { get; set; }

        public bool IsVerbose { get; set; }
        public int TestRunIndex { get; internal set; }
        public int TestRunCount { get; internal set; }
        public bool ValidTestsOnly { get; internal set; }
        public string CopyFilesTo { get; internal set; }
        public int TestsPerRun { get; internal set; }
        public Func<string,bool> TestCaseFilter { get; internal set; }
        public Func<string, bool> MachineFilter { get; internal set; }
        public int SkipNTests { get; internal set; }
        public bool WithETL { get; internal set; }
        public bool Overwrite { get; internal set; }
        

        /// <summary>
        /// Filter tests by name and if they are a properly formatted test case name
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        bool TestFilter(SingleTest test) => TestCaseFilter(test.Name) &&
                                            (ValidTestsOnly ? test.Files.Any(x => x.IsValidTest) : true);
        bool TestFilter(TestDataFile file) => TestCaseFilter(file.TestName) &&
                                              MachineFilter(file.MachineName) &&
                                              (ValidTestsOnly ? file.IsValidTest : true);
        bool TestFilter(TestRun run) => run.AllTestFilesSortedAscendingByTime.Any(TestFilter);
        bool TestFilter(KeyValuePair<string, SingleTest[]> kvp) => kvp.Value.Any(TestFilter);

        bool myNeedsNewLine;


        public override List<string> ExecuteInternal()
        {
            if (CopyFilesTo != null && !System.IO.Directory.Exists(CopyFilesTo))
            {
                throw new ArgumentException($"The directory -copyfilesto {CopyFilesTo} does not exist!");
            }

            // for future unit testing
            List<string> lret = new();

            TestRunData testRun = new(Directories, Recursive, Directories.First());
            Task<int> extractCount = Task.Run(() => testRun.AllFiles.Where(x => x.JsonExtractFileWhenPresent != null).Count());

            Counter<string> machineCounter = new();
            Counter<string> testCounter = new();
            foreach (var file in testRun.AllFiles.Where(TestFilter))
            {
                machineCounter.Increment(file.MachineName);
            }
            foreach (SingleTest test in testRun.Runs.SelectMany(x => x.GetAllTests().Where(TestFilter)))
            {
                testCounter.Increment(test.Name);
            }

            List<TestRun> runs = testRun.Runs.Where(TestFilter).ToList();

            if (TestRunIndex != -1)
            {
                if (TestRunIndex > runs.Count - 1)
                {
                    throw new ArgumentException($"Test Run Index is too large. Allowed values are 0 - {runs.Count - 1}");
                }
            }

            int maxIndex = (TestRunIndex != -1 && TestRunCount > 0) ? (TestRunIndex + TestRunCount) : runs.Count;
            maxIndex = Math.Min(maxIndex, runs.Count); // Clamp value so we do not get out of bounds exceptions
            int startIndex = TestRunIndex == -1 ? 0 : TestRunIndex;
            startIndex = Math.Max(0, Math.Min(startIndex, runs.Count - 1));  // clamp start index between 0 and Count-1

            for (int i = startIndex; i < maxIndex && i < runs.Count; i++)
            {
                TestRun run = runs[i];
                string testRunDuration = run.TestRunDuration.ToString("dd\\ hh\\:mm\\:ss", CultureInfo.InvariantCulture);
                var filteredTests = run.Tests.Where(TestFilter).ToArray();
                WriteLine($"Run[{i,2}] starts at {run.TestRunStart} duration: {testRunDuration}, TestCases: {filteredTests.Length,3} Tests: {filteredTests.SelectMany(x => x.Value).Where(TestFilter).Count(),3}");

                foreach (var testNameAndSingleTests in filteredTests)
                {
                    IfVerbose(() => WriteLine($"\t{testNameAndSingleTests.Key} {testNameAndSingleTests.Value.Length} "));
                    foreach (SingleTest test in testNameAndSingleTests.Value.OrderBy(x=>x.PerformedAt).Skip(SkipNTests).Take(TestsPerRun == 0 ? int.MaxValue : TestsPerRun))
                    {
                        IfVerbose(() =>
                        {
                            Write($"\t{test.DurationInMs,-7}ms {test.Name,-20}");
                            Write("  " + GetXString(test.DurationInMs, 200));
                        });


                        foreach (var file in test.Files)
                        {
                            if (CopyFilesTo != null)
                            {
                                if (file.JsonExtractFileWhenPresent != null)
                                {
                                    CopyFromTo(file.JsonExtractFileWhenPresent, Path.Combine(CopyFilesTo, Path.GetFileName(file.JsonExtractFileWhenPresent)));
                                }
                                if (WithETL)
                                {
                                    string ext = Path.GetExtension(file.FileName);
                                    if (ext == TestRun.SevenZExtension ||
                                        ext == TestRun.ZipExtension)
                                    {
                                        string targetFile = Path.Combine(CopyFilesTo, Path.GetFileName(file.FileName));
                                        CopyFromTo(file.FileName, targetFile);
                                    }
                                }
                            }

                            if (PrintFiles)
                            {
                                string ExtractStr = ( Path.GetExtension(file.FileName) == TestRun.ExtractExtension || file.JsonExtractFileWhenPresent == null) ? "" : "+Extract";
                                WriteLine($"\t\t\t{file.PerformedAt} {file.FileName} {ExtractStr}");
                            }
                        }

                        FlushNewLine();
                    }
                }
            }

            if (CopyFilesTo == null)
            {
                WriteLine("======================================");
                WriteLine("Summary");
                WriteLine("======================================");
                WriteLine($"Runs: {testRun.Runs.Count} First: {testRun.Runs?.FirstOrDefault()?.TestRunStart} Last: {testRun.Runs?.LastOrDefault()?.TestRunStart}, Files: {testRun.AllFiles.Count} Extracted: {extractCount.Result}");
                string machines = String.Join(Environment.NewLine + "\t", machineCounter.Counts.OrderBy(x => x.Value).Select(x => $"{x.Key,-15}: {x.Value,5}"));
                WriteLine($"Used Machines:");
                WriteLine($"\t{machines}");
                string tests = String.Join(Environment.NewLine + "\t", testCounter.Counts.OrderBy(x => x.Value).Select(x => $"{x.Key,-35}: {x.Value,5}"));
                WriteLine($"Total Tests: {testCounter.Counts.Sum(x => x.Value)}");
                WriteLine($"\t{tests}");
            }
            return lret;
        }

        void IfVerbose(Action acc)
        {
            if (IsVerbose)
            {
                acc();
            }
        }

        void CopyFromTo(string src, string target)
        {
            Write( $"\tCopy File {src} to {CopyFilesTo} ");
            if(!Overwrite && File.Exists(target) )
            {
                WriteLine("\tSkipping. Already there.");
                return;
            }
            FlushNewLine();
            File.Copy(src, target, true);
        }

        void Write(string str)
        {
            myNeedsNewLine = true;
            Console.Write(str);
        }

        void WriteLine(string str)
        {
            FlushNewLine();
            Console.WriteLine(str);
        }

        void FlushNewLine()
        {
            if (myNeedsNewLine)
            {
                Console.WriteLine();
                myNeedsNewLine = false;
            }
        }

        string GetXString(int value, int divisor)
        {
            return new string('x', value / divisor);
        }
    }
}
