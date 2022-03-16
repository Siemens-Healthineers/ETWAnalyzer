using ETWAnalyzer.Analyzers;
using ETWAnalyzer.Analyzers.Exception;
using ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer;
using ETWAnalyzer.Configuration;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest
{
    public class ExceptionDifferenceVolatileAnalyzerTests
    {
        const string TestCase = "CallupAdhocColdReadingCR";
        const string ModuleV = "VersionConstant";
        SyntheticTestDataFileGenerator generator = new();


        Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> GetExceptionsOf(ExceptionCharacteristic includeExceptionsWithCharacteristic, TestDataFile[] sourcesToAnalyze,bool onlyStillActive = false)
        {
            TimeSeriesExceptionActivities analysisResults = SimulateAnalysis(onlyStillActive, sourcesToAnalyze);
            return analysisResults.ExceptionCharacteristicDetector[includeExceptionsWithCharacteristic].ExceptionActivityCharacteristics[TestCase];
        }

        TimeSeriesExceptionActivities SimulateAnalysis(bool onlyStillActive, params TestDataFile[] runsWithOneTest)
        {
            var tempDir = TempDir.Create().Name;
            TestRunData testRunData = new(TestRun.CreateForSpecifiedFiles(runsWithOneTest), new ETWAnalyzer.Commands.OutDir { OutputDirectory = tempDir, IsDefault = true });

            ExceptionDifferenceVolatileAnalyzer analyzer = new();
            analyzer.OutdirFlag = tempDir;
            analyzer.IsStillActiveExceptionDetectorFlag = onlyStillActive;
            analyzer.SelectedCharacteristicsFlag = new List<ExceptionCharacteristic>() { ExceptionCharacteristic.DisjointOutliers, ExceptionCharacteristic.DisjointSporadics, ExceptionCharacteristic.DisjointTrends };

            var r = new TestAnalysisResultCollection();
            foreach (var run in testRunData.Runs)
            {
                if (run == testRunData.Runs[testRunData.Runs.Count - 1]) analyzer.IsLastElement = true;
                analyzer.AnalyzeTestRun(r, run);
            }
            analyzer.IsLastElement = false;
            return analyzer.ExceptionActivities;
        }


        private TestDataFile[] CreateEachTestDataFileForEachTestRun()
        {
            SyntheticTestDataFileGenerator generator = new();
            // Exception messages: 
            // oc is (o)utlier with (c)onsistent moduleversion-difference, sc is (sporactic) with (c)onsistent moduleversion-difference, t is (t)rend with (c)onsistent moduleversion-difference
            // oi is (o)utlier with (i)nconsistent moduleversion-difference, si is (s)poractic with (i)nconsistent moduleversion-difference, si is (s)poradic with (i)nconsistent moduleversion-difference
            TestDataFile[] tests =
            {
                generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, $"testOfRun1.json", new DateTime(2000, 1, 1), 1000, $"Version1",                "oc3",                             "sc2", "sc3",        "si2", "si3",        "tc2", "tc3",               "ti3", "ignoreCase"),//0 i=1
                generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, $"testOfRun2.json", new DateTime(2000, 1, 2), 1000, $"Version2",         "oc2",                                    "sc2",               "si2",        "tc1", "tc2",                             "ignoreCase"),//1 i=2
                generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, $"testOfRun3.json", new DateTime(2000, 1, 3), 1000, $"Version3",                                     "oi3", "sc1",                                    "tc1",        "tc3",                      "ignoreCase"),//2 i=3
                generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, $"testOfRun4.json", new DateTime(2000, 1, 4), 1000, $"Version4",  "oc1",                                    "sc1",                                           "tc2", "tc3",                      "ignoreCase"),//3 i=4
                generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, $"testOfRun5.json", new DateTime(2000, 1, 5), 1000, $"Version5",                "oc3",                             "sc2",                                    "tc2",                             "ignoreCase"),//4 i=5
                generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, $"testOfRun6.json", new DateTime(2000, 1, 6), 1000, $"Version6",                              "oi2",        "sc1",               "si1",                                           "ti2",        "ignoreCase"),//5 i=6
                generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, $"testOfRun7.json", new DateTime(2000, 1, 7), 1000, $"Version7",         "oc2",                                                  "si1",        "si3",                      "ti1", "ti2",        "ignoreCase"),//6 i=7
                generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, $"testOfRun8.json", new DateTime(2000, 1, 8), 1000, $"Version8",                                                                               "si3",                      "ti1",               "ignoreCase"),//7 i=8
             
                generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, $"testOfRun9.json", new DateTime(2000, 1, 9),   1000, ModuleV  ,                                                                 "si1",                                    "ti1",               "ignoreCase"),//8 i=9
                generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, $"testOfRun10.json", new DateTime(2000, 1, 10), 1000, ModuleV  ,                                                                        "si2",                                                  "ignoreCase"),//9 i=10
                generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, $"testOfRun11.json", new DateTime(2000, 1, 11), 1000, ModuleV  ,                                                                                                                                "ignoreCase"),//10 i=11
                generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, $"testOfRun12.json", new DateTime(2000, 1, 12), 1000, ModuleV  ,                       "oi1",                                                  "si3",                                           "ignoreCase"),//11 i=12
                generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, $"testOfRun13.json", new DateTime(2000, 1, 13), 1000, ModuleV  ,                              "oi2",                                                                                            "ignoreCase"),//12 i=13
                generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, $"testOfRun14.json", new DateTime(2000, 1, 14), 1000, ModuleV  ,                                                                                                                                "ignoreCase"),//13 i=14
                generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, $"testOfRun15.json", new DateTime(2000, 1, 15), 1000, ModuleV  ,                                                                        "si2",                                           "ti3", "ignoreCase"),//14 i=15
                generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, $"testOfRun16.json", new DateTime(2000, 1, 16), 1000, ModuleV  ,                                     "oi3",                             "si2", "si3",                             "ti2", "ti3", "ignoreCase"),//15 i=16
            };
            return tests;
        }
        private void AssertDiffCountOfConsistentOutliers(Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> messagedsWithExceptionsAndSources)
        {
            Assert.Single(messagedsWithExceptionsAndSources["oc1"].Value);
            Assert.Equal(2, messagedsWithExceptionsAndSources["oc2"].Value.Count());
            Assert.Equal(2, messagedsWithExceptionsAndSources["oc3"].Value.Count());
        }
        private void AssertDiffCountOfInonsistentOutliers(Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> messagedsWithExceptionsAndSources)
        {
            Assert.Single(messagedsWithExceptionsAndSources["oi1"].Value);
            Assert.Equal(2, messagedsWithExceptionsAndSources["oi2"].Value.Count());
            Assert.Equal(2, messagedsWithExceptionsAndSources["oi3"].Value.Count());
        }

        private void AssertDiffCountOfConsistentSporadics(Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> messagedsWithExceptionsAndSources)
        {
            Assert.Equal(3, messagedsWithExceptionsAndSources["sc1"].Value.Count());
            Assert.Equal(2, messagedsWithExceptionsAndSources["sc2"].Value.Count());
            Assert.Single(messagedsWithExceptionsAndSources["sc3"].Value);
        }
        private void AssertDiffCountOfInonsistentSporadics(Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> messagedsWithExceptionsAndSources)
        {
            Assert.Equal(3, messagedsWithExceptionsAndSources["si1"].Value.Count());
            Assert.Equal(3, messagedsWithExceptionsAndSources["si2"].Value.Count());
            Assert.Equal(5, messagedsWithExceptionsAndSources["si3"].Value.Count());
        }
        private void AssertDiffCountOfConsistentTrends(Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> messagedsWithExceptionsAndSources)
        {
            Assert.Equal(2, messagedsWithExceptionsAndSources["tc1"].Value.Count());
            Assert.Equal(3, messagedsWithExceptionsAndSources["tc2"].Value.Count());
            Assert.Equal(3, messagedsWithExceptionsAndSources["tc3"].Value.Count());
        }
        private void AssertDiffCountOfInonsistentTrends(Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> messagedsWithExceptionsAndSources)
        {
            Assert.Equal(2, messagedsWithExceptionsAndSources["ti1"].Value.Count());
            Assert.Equal(3, messagedsWithExceptionsAndSources["ti2"].Value.Count());
            Assert.Equal(2, messagedsWithExceptionsAndSources["ti3"].Value.Count());
        }



        [Fact]
        public void Can_Exclude_Duplicate_Exceptions()
        {
            //Run 0
            var t0 = generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, "test0Run0.json", new DateTime(2000, 1, 1), 1000, "Version1", "ignoreCase1", "ignoreCase2");
            var t1 = generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, "test1Run0.json", new DateTime(2000, 1, 1), 1000, "Version1", "ignoreCase1", "ignoreCase2");
            //Run 1
            var t2 = generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, "test0Run1.json", new DateTime(2000, 1, 2), 1000, "Version2", "ignoreCase2");
            var t3 = generator.GenerateSyntheticTestDataFileWithExceptions(TestCase, "test1Run1.json", new DateTime(2000, 1, 2), 1000, "Version2", "ignoreCase2");

            TimeSeriesExceptionActivities analysisResults = SimulateAnalysis(false, t0,t1,t2,t3);

            Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> exceptionsWithSources = analysisResults.AllTestSpecificExceptionsWithSourceFiles[TestCase];

            Assert.Single(exceptionsWithSources);
            Assert.Equal("ignoreCase1", exceptionsWithSources.First().Key.Message);

            Assert.Single(exceptionsWithSources.First().Value);
            Assert.Equal(t0, exceptionsWithSources.First().Value[0].SourceOfActiveException);
        }
            
        Dictionary<string,KeyValuePair<ExceptionKeyEvent,ExceptionSourceFileWithNextNeighboursModuleVersion[]>> GetMsgAssignedExceptionWithSourcesIfAssertSingle(Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> toInvestigate, params string[] messages)
        {
            var ret = new Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>();

            foreach (var msg in messages)
            {
                var equalToMsg = toInvestigate.Where(k => k.Key.Message.Equals(msg));
                Assert.Single(equalToMsg);
                ret.Add(msg, equalToMsg.First());
            }
            return ret;
        }
        private void AssertCurrentNextNeighboursModuleVersion(Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> messagesWithExceptionsAndSources, TestDataFile[] eachTestDataFileForEachRun)
            => messagesWithExceptionsAndSources.Values.ToList().ForEach(x => AssertCurrentNextNeighboursModuleVersion(x, eachTestDataFileForEachRun));

        private void AssertCurrentNextNeighboursModuleVersion(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> keyValuePair, TestDataFile[] eachTestDataFileForEachRun)
        {
            foreach (var source in keyValuePair.Value)
            {
                int idx = GetIdxOfFileName(source.SourceOfActiveException.FileName, eachTestDataFileForEachRun);

                var expectedPrevious = idx > 0 ? eachTestDataFileForEachRun[idx - 1].Extract.MainModuleVersion : null;
                var expectedCurrent = eachTestDataFileForEachRun[idx].Extract.MainModuleVersion;
                var expectedFollowing = idx < eachTestDataFileForEachRun.Length - 1 ? eachTestDataFileForEachRun[idx + 1].Extract.MainModuleVersion : null;

                Assert.Equal(expectedPrevious, source.CurrentAndNextNeighboursModuleVersion.PreviousModuleVersion);
                Assert.Equal(expectedCurrent, source.CurrentAndNextNeighboursModuleVersion.CurrentModuleVersion);
                Assert.Equal(expectedFollowing, source.CurrentAndNextNeighboursModuleVersion.FollowingModuleVersion);
            }
        }

        private int GetIdxOfFileName(string filename,TestDataFile[] inArray)
        {
            var result = inArray.FirstOrDefault(x => x.FileName.Equals(filename));
            int idx = inArray.ToList().IndexOf(result);
            return idx == -1 ? throw new ArgumentException($"{inArray} must contain {filename}") : idx;
        }
        [Fact]
        public void Can_Detect_Disjoint_Outliers()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            //Search for oc and oi
            TestDataFile[] eachTestDataFileForEachRun = CreateEachTestDataFileForEachTestRun();
            Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> outlierExceptionsWithSources = GetExceptionsOf(ExceptionCharacteristic.DisjointOutliers, eachTestDataFileForEachRun);
            Assert.Equal(6,outlierExceptionsWithSources.Count);

            Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> messagedsWithExceptionsAndSources = GetMsgAssignedExceptionWithSourcesIfAssertSingle(outlierExceptionsWithSources, "oc1","oc2","oc3","oi1","oi2","oi3");

            AssertDiffCountOfConsistentOutliers(messagedsWithExceptionsAndSources);
            AssertDiffCountOfInonsistentOutliers(messagedsWithExceptionsAndSources);
            AssertCurrentNextNeighboursModuleVersion(messagedsWithExceptionsAndSources, eachTestDataFileForEachRun);
        }

        [Fact]
        public void Can_Detect_Disjoint_Outliers_Consistent_VersionDifference()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;

            //Search for oc
            TestDataFile[] eachTestDataFileForEachRun = CreateEachTestDataFileForEachTestRun();
            Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> outlierExceptionsWithSources = GetExceptionsOf(ExceptionCharacteristic.DisjointOutliersConsistentModVDiff, eachTestDataFileForEachRun);
            Assert.Equal(3, outlierExceptionsWithSources.Count);

            Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> messagedsWithExceptionsAndSources = GetMsgAssignedExceptionWithSourcesIfAssertSingle(outlierExceptionsWithSources, "oc1", "oc2", "oc3");

            AssertDiffCountOfConsistentOutliers(messagedsWithExceptionsAndSources);
            AssertCurrentNextNeighboursModuleVersion(messagedsWithExceptionsAndSources, eachTestDataFileForEachRun);

        }
        [Fact]
        public void Can_Detect_Disjoint_Outliers_Inconsistent_VersionDifference()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            //Search for oi
            TestDataFile[] eachTestDataFileForEachRun = CreateEachTestDataFileForEachTestRun();
            Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> outlierExceptionsWithSources = GetExceptionsOf(ExceptionCharacteristic.DisjointOutliersInconsistentModVDiff, eachTestDataFileForEachRun);
            Assert.Equal(3, outlierExceptionsWithSources.Count);

            Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> messagedsWithExceptionsAndSources = GetMsgAssignedExceptionWithSourcesIfAssertSingle(outlierExceptionsWithSources, "oi1", "oi2", "oi3");

            AssertDiffCountOfInonsistentOutliers(messagedsWithExceptionsAndSources);
            AssertCurrentNextNeighboursModuleVersion(messagedsWithExceptionsAndSources, eachTestDataFileForEachRun);
        }

        [Fact]
        public void Can_Detect_Disjoint_Trends()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            //Search for tc and ti
            TestDataFile[] eachTestDataFileForEachRun = CreateEachTestDataFileForEachTestRun();
            Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> trendExceptionsWithSources = GetExceptionsOf(ExceptionCharacteristic.DisjointTrends, eachTestDataFileForEachRun);
            Assert.Equal(6, trendExceptionsWithSources.Count);

            Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> messagedsWithExceptionsAndSources = GetMsgAssignedExceptionWithSourcesIfAssertSingle(trendExceptionsWithSources, "tc1", "tc2", "tc3", "ti1", "ti2", "ti3");

            AssertDiffCountOfInonsistentTrends(messagedsWithExceptionsAndSources);
            AssertDiffCountOfConsistentTrends(messagedsWithExceptionsAndSources);
            AssertCurrentNextNeighboursModuleVersion(messagedsWithExceptionsAndSources, eachTestDataFileForEachRun);
        }

        [Fact]
        public void Can_Detect_Disjoint_Trends_Consistent_VersionDifference()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            //Search for tc
            TestDataFile[] eachTestDataFileForEachRun = CreateEachTestDataFileForEachTestRun();
            Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> trendExceptionsWithSources = GetExceptionsOf(ExceptionCharacteristic.DisjointTrendsConsistentModVDiff, eachTestDataFileForEachRun);
            Assert.Equal(3, trendExceptionsWithSources.Count);

            Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> messagedsWithExceptionsAndSources = GetMsgAssignedExceptionWithSourcesIfAssertSingle(trendExceptionsWithSources, "tc1", "tc2", "tc3");

            AssertDiffCountOfConsistentTrends(messagedsWithExceptionsAndSources);
            AssertCurrentNextNeighboursModuleVersion(messagedsWithExceptionsAndSources, eachTestDataFileForEachRun);

        }
        [Fact]
        public void Can_Detect_Disjoint_Trends_Inconsistent_VersionDifference()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            //Search for ti
            TestDataFile[] eachTestDataFileForEachRun = CreateEachTestDataFileForEachTestRun();
            Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> trendExceptionsWithSources = GetExceptionsOf(ExceptionCharacteristic.DisjointTrendsInconsistentModVDiff,eachTestDataFileForEachRun);
            Assert.Equal(3, trendExceptionsWithSources.Count);

            Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> messagedsWithExceptionsAndSources = GetMsgAssignedExceptionWithSourcesIfAssertSingle(trendExceptionsWithSources, "ti1", "ti2", "ti3");

            AssertDiffCountOfInonsistentTrends(messagedsWithExceptionsAndSources);
            AssertCurrentNextNeighboursModuleVersion(messagedsWithExceptionsAndSources, eachTestDataFileForEachRun);
        }

        [Fact]
        public void Can_Detect_Disjoint_Sporadics()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            //Search for sc and si
            TestDataFile[] eachTestDataFileForEachRun = CreateEachTestDataFileForEachTestRun();
            Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> sporadicExceptionsWithSources = GetExceptionsOf(ExceptionCharacteristic.DisjointSporadics, eachTestDataFileForEachRun);
            Assert.Equal(6, sporadicExceptionsWithSources.Count);

            Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> messagedsWithExceptionsAndSources = GetMsgAssignedExceptionWithSourcesIfAssertSingle(sporadicExceptionsWithSources, "sc1", "sc2", "sc3", "si1", "si2", "si3");
            AssertDiffCountOfConsistentSporadics(messagedsWithExceptionsAndSources);
            AssertDiffCountOfInonsistentSporadics(messagedsWithExceptionsAndSources);
            AssertCurrentNextNeighboursModuleVersion(messagedsWithExceptionsAndSources, eachTestDataFileForEachRun);
        }
        [Fact]
        public void Can_Detect_Disjoint_Sporadics_Consistent_VersionDifference()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            //Search for sc
            TestDataFile[] eachTestDataFileForEachRun = CreateEachTestDataFileForEachTestRun();
            Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> sporadicExceptionsWithSources = GetExceptionsOf(ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff, eachTestDataFileForEachRun);
            Assert.Equal(3, sporadicExceptionsWithSources.Count);

            Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> messagedsWithExceptionsAndSources = GetMsgAssignedExceptionWithSourcesIfAssertSingle(sporadicExceptionsWithSources, "sc1", "sc2", "sc3");
            AssertDiffCountOfConsistentSporadics(messagedsWithExceptionsAndSources);
            AssertCurrentNextNeighboursModuleVersion(messagedsWithExceptionsAndSources, eachTestDataFileForEachRun);
        }
        [Fact]
        public void Can_Detect_Disjoint_Sporadics_Inconsistent_VersionDifference()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            //Search for si
            TestDataFile[] eachTestDataFileForEachRun = CreateEachTestDataFileForEachTestRun();
            Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> sporadicExceptionsWithSources = GetExceptionsOf(ExceptionCharacteristic.DisjointSporadicsInconsistentModVDiff, eachTestDataFileForEachRun);
            Assert.Equal(3, sporadicExceptionsWithSources.Count);

            Dictionary<string, KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> messagedsWithExceptionsAndSources = GetMsgAssignedExceptionWithSourcesIfAssertSingle(sporadicExceptionsWithSources, "si1", "si2", "si3");
            AssertDiffCountOfInonsistentSporadics(messagedsWithExceptionsAndSources);
            AssertCurrentNextNeighboursModuleVersion(messagedsWithExceptionsAndSources, eachTestDataFileForEachRun);
        }

    }
}
