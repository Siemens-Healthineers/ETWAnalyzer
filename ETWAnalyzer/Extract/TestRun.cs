//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extractors;
using ETWAnalyzer.Infrastructure;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// A TestRun consists of multiple series of single tests with the same software baseline.
    /// Currently a new TestRun is generated when between to subsequent tests >1h is in between.
    /// </summary>
    public class TestRun
    {
        /// <summary>
        /// Extension of potential compressed ETL files
        /// </summary>
        internal const string SevenZExtension = ".7z";

        /// <summary>
        /// Extension of potential compressed ETL files
        /// </summary>
        internal const string ZipExtension = ".zip";

        /// <summary>
        /// ETL file extension
        /// </summary>
        internal const string ETLExtension = ".etl";

        /// <summary>
        /// Extracted ETL file extension
        /// </summary>
        internal const string ExtractExtension = ".json";

        /// <summary>
        /// Compressed Json extension
        /// </summary>
        internal const string CompressedExtractExtension = ".json7z";

        /// <summary>
        /// Valid files are compressed ETL files and extracted ETL files, as well as extracted Json files
        /// </summary>
        static readonly string[] ValidExtensions = new string[] { SevenZExtension, ZipExtension, ETLExtension, ExtractExtension };

        /// <summary>
        /// When between two tests a time gap > 1h exists then map it to a new TestRun
        /// </summary>
        static readonly TimeSpan myMaxTimeBetweenTests = TimeSpan.FromHours(1);

        /// <summary>
        /// Key: Name of the Testcase/SingleTest with Type(CR, MR, CT,...)
        /// Value: All Testcases/SingleTest with the same Type
        /// </summary>
        readonly Dictionary<string, SingleTest[]> myTests = new();

        /// <summary>
        /// Key is testcase name and value is an array of executed tests
        /// </summary>
        public IReadOnlyDictionary<string, SingleTest[]> Tests
        {
            get => myTests;
        }

        /// <summary>
        /// Navigate to TestRunDate object to which this TestRun belongs to
        /// </summary>
        public TestRunData Parent
        {
            get;internal set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<SingleTest> GetAllTests()
        {
            List<SingleTest> singleTests = new();
            foreach (var test in Tests)
            {
                singleTests.AddRange(test.Value.ToList());
            }
            return singleTests;
        }

        List<TestDataFile> myAllTestFiles;

        /// <summary>
        /// Get all files of the TestRun sorted ascending by time when the test was executed
        /// </summary>
        public IReadOnlyList<TestDataFile> AllTestFilesSortedAscendingByTime
        {
            get
            {
                if (myAllTestFiles == null)
                {
                    myAllTestFiles = new List<TestDataFile>();
                    foreach (var testKeyValue in Tests)
                    {
                        foreach (SingleTest test in testKeyValue.Value)
                        {
                            myAllTestFiles.AddRange(test.Files);
                        }
                    }
                    myAllTestFiles = myAllTestFiles.OrderBy(x => x.PerformedAt).ToList();
                }

                return myAllTestFiles;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public DateTime TestRunStart
        {
            get
            {
                DateTime lret = default;
                IReadOnlyList<TestDataFile> files = AllTestFilesSortedAscendingByTime;
                if( files.Count > 0 )
                {
                    lret = files[0].PerformedAt;
                }

                return lret;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public DateTime TestRunEnd
        {
            get
            {
                DateTime lret = DateTime.MaxValue;
                IReadOnlyList<TestDataFile> files = AllTestFilesSortedAscendingByTime;
                if( files.Count > 0)
                {
                    lret = files[files.Count - 1].PerformedAt;
                }
                return lret;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan TestRunDuration
        {
            get => TestRunEnd - TestRunStart;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Files: {AllTestFilesSortedAscendingByTime.Count}, Run Start: {TestRunStart} Duration: {TestRunDuration}, Tests: {String.Join(String.Empty, Tests.Keys)}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="singleTestName"></param>
        /// <returns></returns>
        public int GetNumberOfSingleTestInTestRun(string singleTestName)
        {
            int countOfSingleTests = 0;
            foreach (var singleTest in myTests)
            {
                if (singleTest.Key == singleTestName)
                { countOfSingleTests = singleTest.Value.Length; }
            }
            return countOfSingleTests;
        }

        /// <summary>
        /// Returns the absolute Number of TestDataFiles
        /// </summary>
        /// <returns></returns>
        public int GetTotalNumberOfTestDataFilesInTestRun()
        {
            List<SingleTest> valuesOfSingleTest = myTests.SelectMany(x => x.Value).ToList();

            int counter = 0;

            foreach (var element in valuesOfSingleTest)
            {
                counter += element.Files.Count;
            }
            return counter;
        }

        /// <summary>
        /// Create a new TestRun where the testcases time stamp between each testcase differs by no more than myTimeBetweenTests
        /// </summary>
        /// <param name="testRunTestCases">Collection of testcases which were executed during on testrun</param>
        /// <param name="parent">Parent node</param>
        /// <param name="throwExceByExistingSingleTestParent">By filtering the already existing TestRuns the parents may already be set - set this flag to false if necessary</param>
        public TestRun(List<SingleTest> testRunTestCases, TestRunData parent, bool throwExceByExistingSingleTestParent = true)
        {
            if( testRunTestCases == null )
            {
                throw new ArgumentNullException(nameof(testRunTestCases));
            }

            Parent = parent;

            // link tests to TestRun
            foreach (SingleTest test in testRunTestCases)
            {
                if ( test.Parent != null&& throwExceByExistingSingleTestParent)
                {
                    throw new NotSupportedException($"Test has already a parent.");
                }

                test.Parent = this; // link single tests with this test run 
            }
            myTests = GenerateRunSortedByPerformedAt(testRunTestCases);
        }

        /// <summary>
        /// Generates a TestRun sorted by PerformedAt of the Tests
        /// </summary>
        /// <param name="testRunTestCases">Collection of testcases which were executed during on testrun</param>
        /// <returns>sorted TestRun</returns>
        Dictionary<string, SingleTest[]> GenerateRunSortedByPerformedAt(List<SingleTest> testRunTestCases)
        {
            Dictionary<string, SingleTest[]> tests = new();

            // Group by name
            var groupedByName = testRunTestCases.GroupBy(x => x.Name);

            foreach(IGrouping<string,SingleTest> group in groupedByName)
            {
                // order tests within group by test time
                tests.Add(group.Key, group.OrderBy(x => x.PerformedAt).ToArray());
            }

            return tests;
        }


        /// <summary>
        /// Convert a list of test cases to a list of test runs where a new test run is created when between two tests the time 
        /// was greater than 1h.
        /// </summary>
        /// <param name="testRunTestCases">collection of tests</param>
        /// <param name="parent">Parent TestRunData if present. Can be null</param>
        /// <param name="throwExceByExistingSingleTestParent">by recreating a run with filterfunctions this flag can be set to false</param>
        /// <returns>Array of Testruns composed by Singletests</returns>
        internal static TestRun[] ConvertTestToTestRuns(List<SingleTest> testRunTestCases, TestRunData parent = null,bool throwExceByExistingSingleTestParent = true) 
        {            
            List<SingleTest> sortedByPerformedAt = testRunTestCases.OrderBy(x => x.PerformedAt).ToList();
            var currentgroup = new List<SingleTest>();
            var groups = new List<List<SingleTest>>();

            for (int i = 0; i < sortedByPerformedAt.Count; i++)
            {
                if (i == 0)
                {
                    currentgroup.Add(sortedByPerformedAt[i]);
                    continue;
                }

                if (sortedByPerformedAt[i].PerformedAt - sortedByPerformedAt[i - 1].PerformedAt > myMaxTimeBetweenTests)
                {
                    groups.Add(currentgroup);
                    currentgroup = new List<SingleTest>();
                }
                currentgroup.Add(sortedByPerformedAt[i]);
            }

            if (currentgroup.Count > 0)
            {
                groups.Add(currentgroup);
            }

            
            TestRun[] allTestRuns = new TestRun[groups.Count];

            int counter = 0;
            foreach (var item in groups)
            {
                allTestRuns[counter] = new TestRun(item, parent, throwExceByExistingSingleTestParent);
                counter++;
            }
            return allTestRuns;
        }



        /// <summary>
        /// Query a directory for zip or 7zip files which represent a list of testruns
        /// </summary>
        /// <param name="testRunDirectoryOrFile">Input directory</param>
        /// <param name="search">Directory search option</param>
        /// <param name="parent">Parent node</param>
        /// <returns>List of test runs or an empty directory.</returns>
        internal static TestRun[] CreateFromDirectory(string testRunDirectoryOrFile, SearchOption search, TestRunData parent)
        {
            // Creates a List of all SingleTests
            List<List<TestDataFile>> groups = GroupFilesByTests(testRunDirectoryOrFile, search, parent?.OutputDirectory?.OutputDirectory ?? testRunDirectoryOrFile);

            // Creates a List of SingleTest PC and SRV Pairs
            List<SingleTest> allSingleTests = ConvertTestDataFilesToSingleTests(groups);

            // Creates an Array of Testruns 
            TestRun[] testRuns = ConvertTestToTestRuns(allSingleTests, parent);

            return testRuns;
        }

        /// <summary>
        /// Creates TestRuns for a specified List of files
        /// </summary>
        /// <param name="testDataFiles">List of TestDataFiles</param>
        /// <returns>The Testruns which are generated for the given file list</returns>
        internal static TestRun[] CreateForSpecifiedFiles(IReadOnlyList<TestDataFile> testDataFiles)
        {
            // Creating Groups to generate SingleTest objects
            List<List<TestDataFile>>  groupedForSingleTest = GroupTestDataFilesByTests(testDataFiles);

            // Generate SingleTests
            List<SingleTest> allSingleTests = ConvertTestDataFilesToSingleTests(groupedForSingleTest);
            // Generate TestRuns
            TestRun[] specifiedTestRuns = ConvertTestToTestRuns(allSingleTests);

            return specifiedTestRuns;
        }


        /// <summary>
        /// Groups .7z and .zip files of the given path by TimeStamp, TestName and DurationInMs in a List
        /// </summary>
        /// <param name="testRunDirectoryOrFile">A directory path or a path of each file</param>
        /// <param name="searchOption">Directory search option</param>
        /// <param name="outputFolderWithTempFiles">Output folder where already extracted ETL files can be located so we do not need to extract them again.</param>
        /// <returns>List of list of data files where each sublist contains the files which belong to the execution of a single test which was run on one or more computers at the same time.</returns>
        internal static List<List<TestDataFile>> GroupFilesByTests(string testRunDirectoryOrFile, SearchOption searchOption, string outputFolderWithTempFiles)
        {
            List<TestDataFile> testDataList = new();
            
            List<List<TestDataFile>> groups;
            // support as input file also json files without extension
            string alternate = testRunDirectoryOrFile + TestRun.ExtractExtension;
            // Just a Single File is given
            if (File.Exists(testRunDirectoryOrFile))
            {
                testDataList.Add(new TestDataFile(testRunDirectoryOrFile));
            }
            else if( File.Exists(alternate) )
            {
                testDataList.Add(new TestDataFile(alternate));
            }
            else // A folder with files of .7z, .etl, .zip, .json is given 
            {
                HashSet<string> allFiles = GetFilesWithExtension(testRunDirectoryOrFile, searchOption);

                // include in fileset also all temp extracted etl files
                if( outputFolderWithTempFiles != null)
                {
                    allFiles.UnionWith(GetFilesWithExtension(outputFolderWithTempFiles, SearchOption.TopDirectoryOnly));
                }

                // The TestDataFile will reference mainly the zip file but have properties to find also 
                // the extracted files 
                RemoveAlreadyExtractedEtlFiles(allFiles);

                Parallel.ForEach(allFiles, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 8,
                },
                (element) =>
                {
                    var file = new TestDataFile(element);
                    lock (testDataList)
                    {
                        testDataList.Add(file);
                    }
                });
            }

            groups = GroupTestDataFilesByTests(testDataList);

            return groups;
        }


        /// <summary>
        /// Get from a directory or a directory query e.g. c:\temp\abc*.json all files with zip/etl/7z/.json extension which are all valid TestDataFile candidates
        /// </summary>
        /// <param name="directoryfileQuery">Directory from which the files are retrieved</param>
        /// <param name="recursive">Search recursively in all subdirectories</param>
        /// <returns>HashSet of all files which did match the allowed extensions</returns>
        internal static HashSet<string> GetFilesWithExtension(string directoryfileQuery, SearchOption recursive)
        {
            GetDirPatternMatcher(directoryfileQuery, out string dir, out Func<string, bool> matcher);

            string[] files = Directory.GetFiles(Path.GetFullPath(dir), "*.*", recursive)
                                      .Where(IsValidExtension).ToArray();

            string[] filteredFiles = files.Where(matcher).ToArray();
            return new HashSet<string>(filteredFiles);
        }

        static internal void  GetDirPatternMatcher(string directoryFileQuery, out string dir, out Func<string,bool> matcher)
        {
            dir = Path.GetDirectoryName(directoryFileQuery);
            if (String.IsNullOrEmpty(dir))
            {
                dir = ".";
            }

            string patternFileName = Path.GetFileName(directoryFileQuery);

            if (directoryFileQuery.Contains("*") || directoryFileQuery.Contains("?"))
            {
                matcher = Matcher.CreateMatcher(patternFileName);
            }
            else
            {
                dir = Path.Combine(dir, patternFileName);
                matcher = Matcher.CreateMatcher(null);
            }
        }

        static bool IsValidExtension(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ValidExtensions.Contains(ext);
        }

        /// <summary>
        /// Check if a file exists as .zip/.7z and .etl and chooses the etl to pass over the extraction.
        /// </summary>
        /// <param name="allFiles">The not filtered input of all files in folder</param>
        /// <returns>Array of .7z and .etl . Adds .etl when the compressed and decompressed file is given.</returns>
        internal static void RemoveAlreadyExtractedEtlFiles(HashSet<string> allFiles)
        {
            string[] derivedExtracted = ExtractSerializer.GetDerivedFileNameParts();

            foreach (var file in allFiles.ToArray())
            {
                // skip all external extracted Json files
                if( derivedExtracted.Any( x=> file.IndexOf(x) != -1))
                {
                    allFiles.Remove(file);
                    continue;
                }

                string withoutExt = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
                string zipFile = withoutExt + ZipExtension;
                string sevenZFile = withoutExt + SevenZExtension;
                string etlFile = withoutExt + ETLExtension;
                string extractedFile = withoutExt + ExtractExtension;
                string extractedSubFolder = Path.Combine(Path.GetDirectoryName(file), Program.ExtractFolder, Path.GetFileNameWithoutExtension(file)) + ExtractExtension;

                if (allFiles.Contains(etlFile) && ( allFiles.Contains(zipFile) || allFiles.Contains(sevenZFile) ) )
                {
                    allFiles.Remove(etlFile);
                }

                // remove side by side extracted file to prevent duplicate output for recursive queries
                if ((allFiles.Contains(sevenZFile) || allFiles.Contains(zipFile) || allFiles.Contains(etlFile)) && allFiles.Contains(extractedFile))
                {
                    allFiles.Remove(extractedFile);
                }

                // remove extracted file in extract folder to prevent duplicate output for recursive queries
                if ((allFiles.Contains(sevenZFile) || allFiles.Contains(zipFile) || allFiles.Contains(etlFile)) && allFiles.Contains(extractedSubFolder))
                {
                    allFiles.Remove(extractedSubFolder);
                }

            }
        }

        /// <summary>
        /// Group list of input TestDataFiles by testcase.
        /// </summary>
        /// <param name="inputFiles"></param>
        /// <returns>List of list of data files where each sublist contains the files which belong to the execution of a single test which was run on one or more computers at the same time.</returns>
        internal static List<List<TestDataFile>> GroupTestDataFilesByTests(IReadOnlyList<TestDataFile> inputFiles)
        {
            // List of .7z and .zip with all time stamps sorted
            List<TestDataFile> sortedTestDataList = inputFiles.OrderBy(x => x.PerformedAt).ToList();
            
            // Creating groups with similar timestamp and saving them in a List
            List<TestDataFile> currentGroup = new();
            List<List<TestDataFile>> groups = new();

            for (int i = 0; i < sortedTestDataList.Count; i++)
            {
                TestDataFile current = sortedTestDataList[i];

                if( currentGroup.Count == 0 || DoesBelongTogether(current, currentGroup[currentGroup.Count - 1]) )
                {
                    currentGroup.Add(current);
                }
                else
                {
                    ProcessGroups(currentGroup, groups);
                    currentGroup.Add(current);
                }

            }

            // Add outstanding last group
            ProcessGroups(currentGroup, groups);

            return groups;
        }

        private static void  ProcessGroups(List<TestDataFile> currentGroup, List<List<TestDataFile>> groups)
        {
            List<TestDataFile> mergedGroup = MergeJsonAndETLFiles(currentGroup);

            Queue<TestDataFile> tests = new(mergedGroup);

            // We expect at most two files in one test case. If more are there rip them apart again 
            // and put into separate groups so we do not end up with two subsequent tests with the same time and duration which did run 
            // within the 10 minutes window where we group things together.
            while (tests.Count > 0)
            {
                List<TestDataFile> subGroup = new()
                {
                    tests.Dequeue()
                };
                if (tests.Count > 0)
                {
                    subGroup.Add(tests.Dequeue());
                }
                groups.Add(subGroup);
            }

            currentGroup.Clear();
        }

        static bool DoesBelongTogether(TestDataFile now, TestDataFile previous)
        {
            // If test file are not within given time range treat them as different
            // Test name must match
            bool lret = now.TestName == previous.TestName &&
                        now.DurationInMs == previous.DurationInMs;
            if (lret)
            {
                TimeSpan diffTime = now.PerformedAt.Subtract(previous.PerformedAt);
                if (diffTime < SingleTest.MaxTimeBetweenTests)
                {
                    lret = true;
                }
                else
                {
                    if (String.IsNullOrEmpty(now.SpecificModifyDate))
                    {
                        lret = false;
                    }
                    else
                    {
                        lret = now.SpecificModifyDate == previous.SpecificModifyDate;
                    }
                }
            }

            return lret;
        }

        /// <summary>
        /// The extracted json files might show up besides the etl/zip or in a totally different directory. We need to merge
        /// the same TestDataFiles together which can result in some random extraction. Otherwise we end up with SingleTest instances
        /// with 4 TestDataFiles
        /// </summary>
        /// <param name="currentGroup"></param>
        private static List<TestDataFile> MergeJsonAndETLFiles(List<TestDataFile> currentGroup)
        {
            var jsons = currentGroup.Where(x => Path.GetExtension(x.FileName) == ExtractExtension ).ToArray();
            var compressed = currentGroup.Where( x=> Path.GetExtension(x.FileName) == SevenZExtension ||
                                                     Path.GetExtension(x.FileName) == ZipExtension);

            HashSet<TestDataFile> all = new(currentGroup);

            foreach(var json in jsons)
            {
                foreach(var comp in compressed)
                {
                    if( json.SpecificModifyDate == comp.SpecificModifyDate && json.MachineName == comp.MachineName)
                    {
                        // when ETL is found we can merge the data of Json and etl file name into one instance and remove the other
                        comp.JsonExtractFileWhenPresent = json.FileName;
                        all.Remove(json); 
                    }
                }
            }

            return all.ToList();
        }

        /// <summary>
        /// This method safes all files which exist in pairs in a List of SingleTest
        /// </summary>
        /// <param name="listOfGroups"></param>
        /// <returns></returns>
        internal static List<SingleTest> ConvertTestDataFilesToSingleTests(List<List<TestDataFile>> listOfGroups)
        {
            // Safe the Singletests as an Array in a list
            List<SingleTest> SingleTests = new();

            foreach (List<TestDataFile> element in listOfGroups)
            {
                TestDataFile[] testDataPair = element.ToArray();
                SingleTests.Add(new SingleTest(testDataPair));
            }

            return SingleTests;

        }
      

        /// <summary>
        /// The Method filters all TestDataFiles in a List which match to the given testcasename and timerange
        /// </summary>
        /// <param name="testCaseNames"></param>
        /// <param name="runs">Contains all Testrun which should be searched for matches</param>
        /// <param name="computerNames"></param>
        /// <param name="startStopDates"></param>
        /// <param name="excludeEmptyFiles">The parameter allows to test the methods filtering function by files with size zero</param>
        /// <returns>List of etl singletest files which can still be compressed</returns>
        internal static List<SingleTest> ExistingSingleTestsIncludeComputerAndTestNameAndDateFilter(IReadOnlyList<string> testCaseNames,IReadOnlyList<string> computerNames, KeyValuePair<DateTime,DateTime> startStopDates, IReadOnlyList<TestRun> runs, bool excludeEmptyFiles = false) 
        {
            List<TestDataFile> filesToAnalyze = new();
            List<SingleTest> singleTestsToAnalyze = new();
            if (testCaseNames?.All(x => x == null) == true) { testCaseNames = null; }
            if (computerNames?.All(x => x == null) == true) { computerNames = null; }

            for (int i = 0; i < runs.Count; i++)
            {
                IEnumerable singleTests = runs[i].Tests.SelectMany(x => x.Value);

                if (testCaseNames == null )
                {
                    foreach (SingleTest singleTest in singleTests)
                    {
                        if (startStopDates.Key < singleTest.PerformedAt && singleTest.PerformedAt < startStopDates.Value)
                        {
                            var files = ExcludeEmptyFilesAndNotRelevantMachineName(singleTest, computerNames, excludeEmptyFiles).ToArray();
                            if (files.Length > 0)
                                singleTestsToAnalyze.Add(new SingleTest(files, singleTest.Parent)); ;
                        }
                    }
                }
                else
                {
                    foreach (SingleTest singleTest in singleTests)
                    {
                        if (startStopDates.Key < singleTest.PerformedAt && singleTest.PerformedAt < startStopDates.Value && testCaseNames.Contains(singleTest.Name))
                        {
                            var files = ExcludeEmptyFilesAndNotRelevantMachineName(singleTest, computerNames, excludeEmptyFiles).ToArray();
                            if (files.Length > 0)
                                singleTestsToAnalyze.Add(new SingleTest(files, singleTest.Parent));
                        }
                    }
                }
            }
            return singleTestsToAnalyze;
        }


        /// <summary>
        /// Helping Method, adds all valid files to a list by matching file size (empty/not empty) and Computername/ Machinename
        /// SingleTestsIncludeComputerAndTestNameAndDateAndExisting checks valid Properties in SingleTest-Level. This Method uses TestDataFile-Level to get MachineNames and SizeInMB.
        /// </summary>
        /// <param name="singleTest">Contains one or two TestDataFiles</param>
        /// <param name="computers">Contains the valid computername after being searched</param>
        /// <param name="excludeEmptyFiles">true: empty files not valid / false: empty files valid</param>
        /// <returns>List of etl files which can still be compressed</returns>
        static List<TestDataFile> ExcludeEmptyFilesAndNotRelevantMachineName(SingleTest singleTest,IReadOnlyList<string> computers, bool excludeEmptyFiles)
        {
            List<TestDataFile> testDataFileObjects = new();

            foreach (var testDataFile in singleTest.Files)
            {
                if (testDataFile.SizeInMB != 0 && excludeEmptyFiles == true && computers == null)// all not empty files added
                {
                    testDataFileObjects.Add(testDataFile);
                }

                if (testDataFile.SizeInMB != 0 && excludeEmptyFiles == true && computers != null) // File is not empty and computername matches
                {
                    if (computers.Contains(testDataFile.MachineName))
                    {
                        testDataFileObjects.Add(testDataFile);
                    }
                }
                if (excludeEmptyFiles == false && computers == null) // all files added
                {
                    testDataFileObjects.Add(testDataFile);
                }
                if (excludeEmptyFiles == false && computers != null) // all files with matching computername added
                {
                    if (computers.Contains(testDataFile.MachineName))
                    {
                        testDataFileObjects.Add(testDataFile);
                    }
                }
            }
            return testDataFileObjects;
        }

    }
}
