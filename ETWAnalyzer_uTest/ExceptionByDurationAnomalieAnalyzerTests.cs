using ETWAnalyzer;
using ETWAnalyzer.Analyzers;
using ETWAnalyzer.Analyzers.Exception.Duration;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Exceptions;
using ETWAnalyzer.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest
{
    public class ExceptionByDurationAnomalieAnalyzerTests
    {
        private SyntheticTestDataFileGenerator generator = new();

        class SyntheticTestDataFileGenerator
        {
            public TestDataFile GenerateSyntheticTestDataFileWithExceptions(string testName, string fileName, DateTime performedAt, int duration, string modulVersion, params string[] exceptionMessages)
            {
                return new TestDataFile(testName, fileName, performedAt, duration, 10, "Machinename", null)
                {
                    Extract = new ETWExtract()
                    {
                        MainModuleVersion = new ModuleVersion() { Version = modulVersion },
                        Exceptions = new ExceptionStats(exceptionMessages.Select(x => new ExceptionEventForQuery(x, "Type", new ETWProcess() { CmdLine = "\"syngo.Viewing.Shell.Host.exe\"  host /HostId 6c2c9591-b360-42cd-95dd-e3f0e5c2db17 /type \"MM Reading\" ", ProcessName = "syngo.Viewing.Shell.Host.exe" }, new DateTimeOffset(), "Stack")).ToList())
                    }
                };
            }
        }

        private ExceptionByDurationAnomalieAnalyzer SimulateAnalysis(ITempOutput temp, params TestDataFile[] runsWithOneTest)
        {
            TestRunData testRunData = new(TestRun.CreateForSpecifiedFiles(runsWithOneTest), new ETWAnalyzer.Commands.OutDir { OutputDirectory = temp.Name, IsDefault = true });
            ExceptionByDurationAnomalieAnalyzer analyzer = new();
            analyzer.OutdirFlag = temp.Name;
            var r = new TestAnalysisResultCollection();

            foreach (var run in testRunData.Runs)
            {
                if (run == testRunData.Runs[testRunData.Runs.Count - 1])
                { analyzer.IsLastElement = true; }
                analyzer.AnalyzeTestRun(r, run);
            }
            analyzer.IsLastElement = false;
            analyzer.Print();
            return analyzer;
        }
        (TestDataFile t1, TestDataFile t2, TestDataFile t3, TestDataFile t4, TestDataFile t5, TestDataFile t6, TestDataFile t7, TestDataFile t8, TestDataFile t9) CreateTestDataFileSoEachCorrespondToOneTestRun(string testCase)
        {
            Dictionary<string, int> orderedDurations = new()
            {
                { "1_lwAnomalie", 399 },
                { "2_25%", 470 },
                { "3_25%", 480 },
                { "4", 490 },
                { "5_Median", 500 },
                { "6", 510 },
                { "7_75%", 520 },
                { "8_75%", 530 },
                { "9_upAnomalie", 601 }
            };
            int quartildistance = 525 - 475;
            double lowerThreshold = 475 - 1.5 * quartildistance;//400
            double upperThreshold = 525 + 1.5 * quartildistance;//600

            var testOfRun1 = generator.GenerateSyntheticTestDataFileWithExceptions(testCase, "Test1.json", new DateTime(2000, 1, 1), orderedDurations["4"], "Version1", "ignorecase");
            var testOfRun2 = generator.GenerateSyntheticTestDataFileWithExceptions(testCase, "Test2.json", new DateTime(2000, 1, 2), orderedDurations["7_75%"], "Version2", "ignorecase");
            var testOfRun3 = generator.GenerateSyntheticTestDataFileWithExceptions(testCase, "Test3.json", new DateTime(2000, 1, 3), orderedDurations["1_lwAnomalie"], "Version3", "exceptionMsg1");
            var testOfRun4 = generator.GenerateSyntheticTestDataFileWithExceptions(testCase, "Test4.json", new DateTime(2000, 1, 4), orderedDurations["6"], "Version4", "ignorecase");
            var testOfRun5 = generator.GenerateSyntheticTestDataFileWithExceptions(testCase, "Test5.json", new DateTime(2000, 1, 5), orderedDurations["9_upAnomalie"], "Version5", "exceptionMsg2");
            var testOfRun6 = generator.GenerateSyntheticTestDataFileWithExceptions(testCase, "Test6.json", new DateTime(2000, 1, 6), orderedDurations["2_25%"], "Version6", "ignorecase");
            var testOfRun7 = generator.GenerateSyntheticTestDataFileWithExceptions(testCase, "Test7.json", new DateTime(2000, 1, 7), orderedDurations["8_75%"], "Version7", "ignorecase");
            var testOfRun8 = generator.GenerateSyntheticTestDataFileWithExceptions(testCase, "Test8.json", new DateTime(2000, 1, 8), orderedDurations["5_Median"], "Version8", "ignorecase");
            var testOfRun9 = generator.GenerateSyntheticTestDataFileWithExceptions(testCase, "Test9.json", new DateTime(2000, 1, 9), orderedDurations["3_25%"], "Version9", "ignorecase");
            return (testOfRun1, testOfRun2, testOfRun3, testOfRun4, testOfRun5, testOfRun6, testOfRun7, testOfRun8, testOfRun9);
        }
        [Fact]
        public void Can_Detect_Duration_Anomalie_TestSources()
        {
            const string TestCase = "CallupAdhocColdReadingCR";
            var (testOfRun1, testOfRun2, testOfRun3, testOfRun4, testOfRun5, testOfRun6, testOfRun7, testOfRun8, testOfRun9) = CreateTestDataFileSoEachCorrespondToOneTestRun(TestCase);

            ExceptionByDurationAnomalieAnalyzer analyzer = SimulateAnalysis(TempDir.Create(), testOfRun1, testOfRun2, testOfRun3, testOfRun4, testOfRun5, testOfRun6, testOfRun7, testOfRun8, testOfRun9);

            Assert.Single(analyzer.DetectedLowerValueAnomalieSources[TestCase]);
            Assert.Equal(testOfRun3, analyzer.DetectedLowerValueAnomalieSources[TestCase][0]);

            Assert.Single(analyzer.DetectedUpperValueAnomalieSources);
            Assert.Equal(testOfRun5, analyzer.DetectedUpperValueAnomalieSources[TestCase][0]);

            Assert.Equal(2, analyzer.DetectedAnomalieSources[TestCase].Count);
            Assert.Contains(testOfRun3, analyzer.DetectedAnomalieSources[TestCase]);
            Assert.Contains(testOfRun5, analyzer.DetectedAnomalieSources[TestCase]);
        }
        [Fact]
        public void Can_Detect_Duration_Anomalie_With_Exceptiondata_And_TestSources()
        {
            const string TestCase = "CallupAdhocColdReadingCR";
            var (testOfRun1, testOfRun2, testOfRun3, testOfRun4, testOfRun5, testOfRun6, testOfRun7, testOfRun8, testOfRun9) = CreateTestDataFileSoEachCorrespondToOneTestRun(TestCase);

            ExceptionByDurationAnomalieAnalyzer analyzer = SimulateAnalysis(TempDir.Create(), testOfRun1, testOfRun2, testOfRun3, testOfRun4, testOfRun5, testOfRun6, testOfRun7, testOfRun8, testOfRun9);

            var lowAnomalieSource = analyzer.DetectedLowerValueAnomalieSources[TestCase][0];
            Assert.Single(lowAnomalieSource.Extract.Exceptions.Exceptions);
            Assert.Equal("exceptionMsg1", lowAnomalieSource.Extract.Exceptions.Exceptions[0].Message);

            var upperAnomalieSource = analyzer.DetectedUpperValueAnomalieSources[TestCase][0];
            Assert.Single(upperAnomalieSource.Extract.Exceptions.Exceptions);
            Assert.Equal("exceptionMsg2", upperAnomalieSource.Extract.Exceptions.Exceptions[0].Message);
        }





    }
}
