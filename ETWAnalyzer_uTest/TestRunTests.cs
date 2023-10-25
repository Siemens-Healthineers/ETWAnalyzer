//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Helper;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ETWAnalyzer.Analyzers.Infrastructure;
using ETWAnalyzer.Configuration;
using ETWAnalyzer_uTest.TestInfrastructure;
using Xunit.Abstractions;

namespace ETWAnalyzer_uTest
{

    public class TestRunTests
    {
        private ITestOutputHelper myWriter;

        public TestRunTests(ITestOutputHelper myWriter)
        {
            this.myWriter = myWriter;
        }

        static readonly string[] TestCaseNames = new string[]
        {
                "SSTUaPMapWorkitemFromRTC2",
                "CallupClaimWarmReadingMR",
                "CallupClaimWarmReadingCT",
                "CallupClaimWarmReadingCR",
                "CallupClaimColdReadingMR",
                "CallupClaimColdReadingCT",
                "CallupClaimColdReadingCR",
                "CallupAdhocWarmReadingMR",
                "CallupAdhocWarmReadingCT",
                "CallupAdhocWarmReadingCR",
                "CallupAdhocColdReadingMR",
                "CallupAdhocColdReadingCT",
                "CallupAdhocColdReadingCR",
        };


        [Fact]
        public void Can_Create_TestRun_List_From_Directory()
        {
            using var printer = new ExceptionalPrinter(myWriter);
            DataOutput<string> testrunDirectory = TestData.TestRunDirectory;
            printer.Add(testrunDirectory.Output);
            TestRun[] runs = TestRun.CreateFromDirectory(testrunDirectory.Data, SearchOption.TopDirectoryOnly, null);

            Assert.NotNull(runs);

            foreach (var testName in TestCaseNames)
            {
                Assert.True(runs.Any(x => x.Tests.ContainsKey(testName)), $"Test case {testName} not found in TestRun array!");
            }

            Assert.True(runs.Length > 0);

            Assert.True(runs[0].Tests.ContainsKey("CallupAdhocColdReadingCR"));

            Assert.True(runs[0].Tests["CallupAdhocColdReadingCR"].Length > 0);

        }

        [Fact]
        public void GetDirPatternMatcher_Adds_Dot_ToPlainFile()
        {
            TestRun.GetDirPatternMatcher("a.json", out string dir,　out Func<string, bool> matcher);
            Assert.Equal(@".\a.json", dir);
            Assert.True(matcher(null));
            Assert.True(matcher(""));
            Assert.True(matcher("abc"));
        }

        [Fact]
        public void GetDirPatternMatcher_MatchAnything()
        {
            TestRun.GetDirPatternMatcher(@"C:\temp\a.json", out string dir, out Func<string, bool> matcher);
            Assert.Equal(@"C:\temp\a.json", dir);
            Assert.True(matcher(null));
            Assert.True(matcher(""));
            Assert.True(matcher("abc"));
        }

        [Fact]
        public void GetDirPatternMatcher_MatchWith_WildCards()
        {
            TestRun.GetDirPatternMatcher(@"C:\temp\*a*.json", out string dir, out Func<string, bool> matcher);
            Assert.Equal(@"C:\temp", dir);
            Assert.False(matcher(null));
            Assert.False(matcher(""));
            Assert.False(matcher("abc"));
            Assert.True(matcher(@"C:\temp\A.json"));
            Assert.False(matcher(@"C:\temp\B.json"));
        }

        [Fact]
        public void GetDirPatternMatcher_MatchWith_WildCards_SecondaryExclusionFilter()
        {
            TestRun.GetDirPatternMatcher(@"C:\temp\*a*.json;!*abc*.json", out string dir, out Func<string, bool> matcher);
            Assert.Equal(@"C:\temp", dir);
            Assert.True(matcher("a.json"));
            Assert.False(matcher("abc.json"));
            Assert.False(matcher("abcd.json"));
            Assert.True(matcher("azbc.json"));
        }

        [Fact]
        public void Can_Create_TestRun_List_From_List_Of_Paths()
        {
            using var printer = new ExceptionalPrinter(myWriter);
            DataOutput<string> testrunDirectory = TestData.TestRunDirectory;
            printer.Add(testrunDirectory.Output);
            List<ETWAnalyzer.Extract.TestDataFile> files = new List<ETWAnalyzer.Extract.TestDataFile>();
            TestRun[] runs = TestRun.CreateFromDirectory(testrunDirectory.Data, SearchOption.TopDirectoryOnly, null);
            files = TestRun.ExistingSingleTestsIncludeComputerAndTestNameAndDateFilter(null, null, new KeyValuePair<DateTime, DateTime>(DateTime.MinValue, DateTime.MaxValue), runs , false).ToTestDataFiles();
            runs = TestRun.CreateForSpecifiedFiles(files);

            int count = 0;
            foreach (var run in runs)
            {
                count += run.GetTotalNumberOfTestDataFilesInTestRun();
            }
            Assert.Equal(TestData.TestRunDirectoryFileCount, count);

        }

        [Fact]
        public void Can_Create_TestRun_From_Two_SingleTests()
        {
            using var printer = new ExceptionalPrinter(myWriter);
            DataOutput<string> testrunDirectory = TestData.TestRunDirectory;
            printer.Add(testrunDirectory.Output);
            List<List<TestDataFile>> tempGroup = new List<List<TestDataFile>>();
            List<TestDataFile> tempTestData = new List<TestDataFile>
            {
                new TestDataFile(Path.Combine(testrunDirectory.Data, "CallupClaimWarmReadingMR_2286msDEFOR09T121SRV.7z")),
                new TestDataFile(Path.Combine(testrunDirectory.Data, "CallupClaimWarmReadingMR_2286msFO9DE01T0166PC.7z")),
                new TestDataFile(Path.Combine(testrunDirectory.Data, "CallupAdhocColdReadingCT_44043msDEFOR09T121SRV.7z")),
                new TestDataFile(Path.Combine(testrunDirectory.Data, "CallupAdhocColdReadingCT_44043msFO9DE01T0166PC.7z"))
            };

            List<SingleTest> allSingleTests=TestRun.ConvertTestDataFilesToSingleTests(TestRun.GroupTestDataFilesByTests(tempTestData));
            TestRun[] Run = TestRun.ConvertTestToTestRuns(allSingleTests);

            Assert.True(Run.Length > 1);
        }


        internal static string CreateEmptyFile(string dir, string fileName)
        {
            string path = Path.Combine(dir, fileName);
            File.WriteAllBytes(path, new byte[0]);
            return path;
        }


        [Fact]
        public void Ignores_ETL_If_Zip_Exists()
        {
            using var tmp = TempDir.Create();
            string serverEtL = CreateEmptyFile(tmp.Name, Path.GetFileName(TestData.ServerEtlFileNameNoPath));
            string clientETL = CreateEmptyFile(tmp.Name, Path.GetFileName(TestData.ClientEtlFileNameNoPath));
            string clientZip = CreateEmptyFile(tmp.Name, Path.GetFileName(TestData.ClientZipFileNameNoPath));

            TestRunData data = new TestRunData(tmp.Name);

            IReadOnlyList<TestDataFile> files = data.AllFiles;
            Assert.Equal(2, files.Count);
            Assert.DoesNotContain(files, x => x.FileName == clientETL);
            Assert.Contains(files, x => x.FileName == clientZip);
            Assert.Contains(files, x => x.FileName == serverEtL);
        }

        [Fact]
        public void InputNumber_Of_TestDataFiles_Match_Number()
        {
            using var printer = new ExceptionalPrinter(myWriter);
            DataOutput<string> testrunDirectory = TestData.TestRunDirectory;
            printer.Add(testrunDirectory.Output);
            int numberOfTestdata = 0;
            foreach(var element in TestRun.GroupFilesByTests(testrunDirectory.Data, SearchOption.TopDirectoryOnly, testrunDirectory.Data))
            {
                numberOfTestdata += element.Count;
            }

            Assert.Equal(TestData.TestRunDirectoryFileCount, numberOfTestdata);

        }

        [Fact]
        public void InputNumber_Of_SingleTests_Match_NumberOf_TestRun()
        {
            using var printer = new ExceptionalPrinter(myWriter);
            DataOutput<string> testrunDirectory = TestData.TestRunDirectory;
            printer.Add(testrunDirectory.Output);
            TestRunData runData = new TestRunData(testrunDirectory.Data);

            int dataFiles = 0;

            foreach (var item in runData.Runs)
            {
               dataFiles += item.GetTotalNumberOfTestDataFilesInTestRun();
            }

            // The TestDataFiles in Directory without the extracted etl files which are already covered by the compressed files
            Assert.Equal(TestData.TestRunDirectoryFileCount, dataFiles);
        }


        [Fact]
        public void Throw_DirectoryNotFoundExcepton_On_NotExisting_Directory()
        {
            ExceptionAssert.Throws<DirectoryNotFoundException>( () => TestRun.CreateFromDirectory("C:\\NotExistingDir\\adfasdfasdf", SearchOption.TopDirectoryOnly, null));
        }

        [Fact]
        public void Return_Empty_List_On_Empty_Directory()
        {
            TestRun[] list = null;
            using (var tmp = TempDir.Create())
            {
                list = TestRun.CreateFromDirectory(tmp.Name, SearchOption.TopDirectoryOnly, null);
            }

            Assert.Empty(list);
        }

        [Fact]
        public void GroupTestDataFilesByTests_SingleFile()
        {
            var file = new TestDataFile("TestCase", "NonName.etl", new DateTime(2015, 1, 1), 500, 20, "TestMachine1",null);
            var files = new List<TestDataFile> { file };
            List<List<TestDataFile>> tests =  TestRun.GroupTestDataFilesByTests(files);
            Assert.Single(tests);
            Assert.Single(tests[0]);
            Assert.Equal("TestCase", tests[0][0].TestName);
            Assert.Equal(500, tests[0][0].DurationInMs);
        }

        [Fact]
        public void GroupTestCaseByTests_Two_Files_Different_TestNames()
        {
            var file1 = new TestDataFile("TestCase1", "NonName.etl", new DateTime(2015, 1, 1), 500, 20, "TestMachine1", null);
            var file2 = new TestDataFile("TestCase2", "NonName.etl", new DateTime(2015, 1, 1), 500, 20, "TestMachine1", null);
            var files = new List<TestDataFile> { file1, file2 };
            List<List<TestDataFile>> tests = TestRun.GroupTestDataFilesByTests(files);
            Assert.Equal(2, tests.Count);
            Assert.Single(tests[0]);
            Assert.Equal("TestCase1", tests[0][0].TestName);
            Assert.Single(tests[1]);
            Assert.Equal("TestCase2", tests[1][0].TestName);
        }

        [Fact]
        public void GroupTestCaseByTest_Two_Files_DifferentDuration()
        {
            var file1 = new TestDataFile("TestCase", "NonName.etl", new DateTime(2015, 1, 1), 500, 20, "TestMachine1", null);
            var file2 = new TestDataFile("TestCase", "NonName.etl", new DateTime(2015, 1, 1), 600, 20, "TestMachine1", null);
            var files = new List<TestDataFile> { file1, file2 };
            List<List<TestDataFile>> tests = TestRun.GroupTestDataFilesByTests(files);
            Assert.Equal(2, tests.Count);
            Assert.Single(tests[0]);
            Assert.Equal("TestCase", tests[0][0].TestName);
            Assert.Equal(500, tests[0][0].DurationInMs);
            Assert.Single(tests[1]);
            Assert.Equal("TestCase", tests[1][0].TestName);
            Assert.Equal(600, tests[1][0].DurationInMs);
        }

        [Fact]
        public void GroupTestCaseByTest_Two_Files_DifferentTimeStampOff_By_One_Day()
        {
            var file1 = new TestDataFile("TestCase", "NonName.etl", new DateTime(2015, 1, 1), 500, 20, "TestMachine1", null);
            var file2 = new TestDataFile("TestCase", "NonName.etl", new DateTime(2015, 1, 2), 500, 20, "TestMachine1", null);
            var files = new List<TestDataFile> { file1, file2 };
            List<List<TestDataFile>> tests = TestRun.GroupTestDataFilesByTests(files);
            Assert.Equal(2, tests.Count);
            Assert.Single(tests[0]);
            Assert.Equal("TestCase", tests[0][0].TestName);
            Assert.Equal(500, tests[0][0].DurationInMs);
            Assert.Single(tests[1]);
            Assert.Equal("TestCase", tests[1][0].TestName);
            Assert.Equal(500, tests[0][0].DurationInMs);
        }

        [Fact]
        public void GroupTestCaseByTest_Two_Files_DifferentTimeStampOff_By_Ten_Minutes()
        {
            var file1 = new TestDataFile("TestCase", "NonName.etl", new DateTime(2015, 1, 1),                          500, 20, "TestMachine1", null);
            var file2 = new TestDataFile("TestCase", "NonName.etl", new DateTime(2015, 1, 1)+TimeSpan.FromMinutes(10), 500, 20, "TestMachine1", null);
            var files = new List<TestDataFile> { file1, file2 };
            List<List<TestDataFile>> tests = TestRun.GroupTestDataFilesByTests(files);
            Assert.Equal(2, tests.Count);
            Assert.Single(tests[0]);
        }


        [Fact]
        public void GroupTestCaseByTests_Three_Files_Different_TestNames()
        {
            var file1 = new TestDataFile("TestCase1", "NonName.etl", new DateTime(2015, 1, 1), 500, 20, "TestMachine1", null);
            var file2 = new TestDataFile("TestCase2", "NonName.etl", new DateTime(2015, 1, 1), 500, 20, "TestMachine1", null);
            var file3 = new TestDataFile("TestCase3", "NonName.etl", new DateTime(2015, 1, 1), 500, 20, "TestMachine1", null);
            var files = new List<TestDataFile> { file1, file2, file3 };
            List<List<TestDataFile>> tests = TestRun.GroupTestDataFilesByTests(files);
            Assert.Equal(3, tests.Count);
            Assert.Single(tests[0]);
            Assert.Equal("TestCase1", tests[0][0].TestName);
            Assert.Single(tests[1]);
            Assert.Equal("TestCase2", tests[1][0].TestName);
        }

        [Fact]
        public void GroupTestCaseByTests_Three_Files_Equal_Names()
        {
            var file1 = new TestDataFile("TestCase", "NonName.etl", new DateTime(2015, 1, 1), 500, 20, "TestMachine1", "20001212_1200");
            var file2 = new TestDataFile("TestCase", "NonName.etl", new DateTime(2015, 1, 1), 500, 20, "TestMachine2", "20001212_1200");
            var file3 = new TestDataFile("TestCase", "NonName.etl", new DateTime(2015, 1, 1), 500, 20, "TestMachine3", "20001212_1200");
            var files = new List<TestDataFile> { file1, file2, file3 };
            List<List<TestDataFile>> tests = TestRun.GroupTestDataFilesByTests(files);
            Assert.Equal(1, tests.Count);
            Assert.Equal(3, tests[0].Count);
            
        }

        [Fact]
        public void GroupTestCaseByTests_Four_Files_Equal_Names_but_same_machine_within_10_minutes()
        {
            var file1 = new TestDataFile("TestCase", "NonName.etl", new DateTime(2015, 1, 1,1,1,0), 500, 20, "TestMachine1", "20001212_1200");
            var file2 = new TestDataFile("TestCase", "NonName.etl", new DateTime(2015, 1, 1, 1, 1, 0), 500, 20, "TestMachine2", "20001212_1200");
            var file3 = new TestDataFile("TestCase", "NonName.etl", new DateTime(2015, 1, 1, 1, 5, 0), 500, 20, "TestMachine1", "20001212_1200");
            var file4 = new TestDataFile("TestCase", "NonName.etl", new DateTime(2015, 1, 1, 1, 5, 0), 500, 20, "TestMachine2", "20001212_1200");
            var files = new List<TestDataFile> { file1, file2, file3, file4 };
            List<List<TestDataFile>> tests = TestRun.GroupTestDataFilesByTests(files);
            Assert.Equal(2, tests.Count);
            Assert.Equal(2, tests[0].Count);
            Assert.Equal(2, tests[1].Count);
        }


        [Fact]
        public void TestRunConfig_Test()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            TestRunConfiguration Config = new TestRunConfiguration();

            Assert.Equal(13, Config.ExpectedRun.TestCases.Count);
            Assert.True(Config.ExpectedRun.TestCases.All(x => x.IterationCount != 0));
            Assert.True(Config.ExpectedRun.TestCases.All(x => x.TestCaseName != null));
         
        }

        [Fact]
        public void Can_Read_List_Of_Files_With_StartAndStopDates()
        {
            using var tmp = TempDir.Create();
            string jsonFiles = TestData.TestRunDirectoryJson(tmp);

            TestRun[] runs = TestRun.CreateFromDirectory(jsonFiles, SearchOption.TopDirectoryOnly, null);
            List<TestDataFile> allQueryFiles = TestRun.ExistingSingleTestsIncludeComputerAndTestNameAndDateFilter(null, null, new KeyValuePair<DateTime, DateTime>( new DateTime(2018, 09, 12), new DateTime(2018, 09, 14)), runs, false).ToTestDataFiles();

            runs = TestRun.CreateForSpecifiedFiles(allQueryFiles);

            int counter = 0;
            foreach (var run in runs)
            {
                counter += run.GetTotalNumberOfTestDataFilesInTestRun();
            }

            Assert.Equal(106, counter);
        }

        [Fact]
        public void Can_Find_All_DoubleFiles_And_Include_TimeRange()
        {
            using var printer = new ExceptionalPrinter(myWriter);
            DataOutput<string> testrunDirectory = TestData.TestRunDirectory;
            printer.Add(testrunDirectory.Output);
            string pcEtl = Path.Combine(testrunDirectory.Data, "CallupClaimWarmReadingMR_2275msFO9DE01T0166PC.etl");
            if( !File.Exists(pcEtl) )
            {
                File.Copy(Path.Combine(testrunDirectory.Data,"CallupClaimWarmReadingMR_2275msFO9DE01T0166PC.7z"), pcEtl);
            }
            string srvEtl = Path.Combine(testrunDirectory.Data, "CallupClaimWarmReadingMR_2275msDEFOR09T121SRV.etl");
            if( !File.Exists(srvEtl) ) 
            {
                File.Copy(Path.Combine(testrunDirectory.Data, "CallupClaimWarmReadingMR_2275msDEFOR09T121SRV.7z"), srvEtl);
            }
            string extractEtl = Path.Combine(testrunDirectory.Data, "CallupClaimWarmReadingMR_2128msFO9DE01T0166PC.etl");
            if (!File.Exists(extractEtl))
            {
                File.Copy(Path.Combine(testrunDirectory.Data, "CallupClaimWarmReadingMR_2128msFO9DE01T0166PC.7z"), extractEtl);
            }

            TestRunData data = new TestRunData(testrunDirectory.Data);

            var duplicates = data.AllFiles.Where(x => x.EtlFileNameIfPresent != null).ToArray();

            Assert.Equal(3, duplicates.Length);

            string[] mainFiles = duplicates.Select(x => x.FileName).ToArray();

            string mrClient7z = Path.Combine(testrunDirectory.Data, "CallupClaimWarmReadingMR_2275msFO9DE01T0166PC.7z");
            string mrServer7z = Path.Combine(testrunDirectory.Data, "CallupClaimWarmReadingMR_2275msDEFOR09T121SRV.7z");
            string mrClient7z2 = Path.Combine(testrunDirectory.Data, "CallupClaimWarmReadingMR_2128msFO9DE01T0166PC.7z");

            Assert.Contains(mrClient7z, mainFiles);
            Assert.Contains(mrServer7z, mainFiles);
            Assert.Contains(mrClient7z2, mainFiles);

            foreach(var etlFile in duplicates)
            {
                File.Delete(etlFile.EtlFileNameIfPresent);
            }
        }

        [Fact]
        public void DoNotGroupTestDataFilesWith_Same_Duration_Into_SameBucket()
        {
            DateTime performedAt1 = new DateTime(2020, 1, 1, 1, 1, 1);
            DateTime performedAt2 = new DateTime(2020, 1, 1, 1, 1, 2);
            DateTime performedAt3 = new DateTime(2020, 1, 1, 1, 1, 3);
            DateTime performedAt4= new DateTime(2020, 1, 1, 1, 1, 4);

            TestDataFile file1 = new TestDataFile("Test1", "", performedAt1, 2500, 0, "SomeMachine1", "");
            TestDataFile file2 = new TestDataFile("Test1", "", performedAt2, 2500, 0, "SomeMachine2", "");
            TestDataFile file3 = new TestDataFile("Test1", "", performedAt3, 2500, 0, "SomeMachine1", "");
            TestDataFile file4 = new TestDataFile("Test1", "", performedAt4, 2500, 0, "SomeMachine2", "");

            TestDataFile[] files = new TestDataFile[] { file1, file2, file3, file4 };

            List<List<TestDataFile>> groups = TestRun.GroupTestDataFilesByTests(files);

            Assert.Equal(2, groups.Count);

        }

        [Fact]
        public void GroupTests_Which_Are_Longer_Than10MinutesApart_But_Have_Same_SpecificCreationDate_AreGroupedTogether()
        {
            DateTime performedAt1 = new DateTime(2020, 1, 1, 1, 1, 1);
            DateTime performedAt2 = new DateTime(2021, 1, 1, 1, 1, 2);
            TestDataFile file1 = new TestDataFile("Test1", "", performedAt1, 100, 0, "SomeMachine", "20201002-121114");
            TestDataFile file2 = new TestDataFile("Test1", "", performedAt2, 100, 0, "SomeMachine2", "20201002-121114");

            List<List<TestDataFile>> groups = TestRun.GroupTestDataFilesByTests(new TestDataFile[] { file1, file2 });
            Assert.Single(groups);
        }

        [Fact]
        public void GroupTests_Which_Are_Longer_Than10MinutesApart_But_Have_Different_SpecificCreationDate_AreSeparate()
        {
            DateTime performedAt1 = new DateTime(2020, 1, 1, 1, 1, 1);
            DateTime performedAt2 = new DateTime(2021, 1, 1, 1, 1, 2);
            TestDataFile file1 = new TestDataFile("Test1", "", performedAt1, 100, 0, "SomeMachine", "20201002-121114");
            TestDataFile file2 = new TestDataFile("Test1", "", performedAt2, 100, 0, "SomeMachine", "20201002-121115");

            List<List<TestDataFile>> groups = TestRun.GroupTestDataFilesByTests(new TestDataFile[] { file1, file2 });
            Assert.Equal(2,groups.Count);
        }

        /// <summary>
        /// This one is important to not degroup tests which should belong together based on timing
        /// </summary>
        [Fact]
        public void GroupTests_Within_10_Minutes_But_Have_Different_SpecificModifyDate()
        {
            DateTime performedAt1 = new DateTime(2000, 1, 1, 1, 1, 1);
            DateTime performedAt2 = new DateTime(2000, 1, 1, 1, 1, 2);
            TestDataFile file1 = new TestDataFile("Test1", "", performedAt1, 100, 0, "SomeMachine1", "20201002-121114");
            TestDataFile file2 = new TestDataFile("Test1", "", performedAt2, 100, 0, "SomeMachine2", "20201002-121115");

            List<List<TestDataFile>> groups = TestRun.GroupTestDataFilesByTests(new TestDataFile[] { file1, file2 });
            Assert.Single(groups);
        }

        [Fact]
        public void Ensure_Splitting_works_also_with_data_from_three_machines()
        {
            using var printer = new ExceptionalPrinter(myWriter);
            DataOutput<string> testrunDirectory = TestData.TestRunDirectoryMultiMachines;
            printer.Add(testrunDirectory.Output);

            TestRun[] runs = TestRun.CreateFromDirectory(testrunDirectory.Data, SearchOption.TopDirectoryOnly, null);
            List<SingleTest> tests = runs.SelectMany(x => x.Tests).SelectMany(x => x.Value).ToList();

            Assert.All(tests,
                t => Assert.True(t.Files.Count.Equals(3), $"we are expecting that each Test has 3 files, but here we only have {t.Files.Count}")
            );
        }
    }
}
