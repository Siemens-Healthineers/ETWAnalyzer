//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Analyzers;
using ETWAnalyzer.Analyzers.Exception.ExceptionDifferenceAnalyzer;
using ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer;
using ETWAnalyzer.Commands;
using ETWAnalyzer.Configuration;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Exceptions;
using ETWAnalyzer.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ETWAnalyzer_uTest
{
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
    public class ExceptionDifferencePersistentAnalyzerTests
    {
        SyntheticTestDataFileGenerator generator = new();

        Dictionary<ExceptionCharacteristic, TimeSeriesDetector> SimulateAnalysis(ITempOutput temp, bool isIrrelevantMeasuredFromFirstExceptionOcc, DateTime exceptionExpiryDate, bool onlyStillActive, params TestDataFile[] runsWithOneTest)
        {
            TestRunData testRunData = new (TestRun.CreateForSpecifiedFiles(runsWithOneTest), new OutDir { OutputDirectory = temp.Name, IsDefault = true });
            ExceptionDifferencePersistentAnalyzer analyzer = new();
            analyzer.OutdirFlag = temp.Name;
            analyzer.IsIrrelevantMeasuredFromFirstExceptionOccFlag = isIrrelevantMeasuredFromFirstExceptionOcc;
            analyzer.ExceptionExpiryDateFlag = exceptionExpiryDate;
            analyzer.IsStillActiveExceptionDetectorFlag = onlyStillActive;
            analyzer.SelectedCharacteristicsFlag = new List<ExceptionCharacteristic>()
            { ExceptionCharacteristic.DisjointOutliers, ExceptionCharacteristic.DisjointSporadics, ExceptionCharacteristic.DisjointTrends };
            var r = new TestAnalysisResultCollection();
            foreach (var run in testRunData.Runs)
            {
                if (run == testRunData.Runs[testRunData.Runs.Count-1])
                { analyzer.IsLastElement = true; }
                analyzer.AnalyzeTestRun(r, run);
            }
            analyzer.IsLastElement = false;
            return analyzer.ExceptionActivities.ExceptionCharacteristicDetector;
        }

        [Fact]
        public void Can_Exclude_DuplicateExceptions()
        {
            List<ExceptionEventForQuery> exceptions = new()
            {
                new ExceptionEventForQuery("msg","type",new ETWProcess(), new DateTimeOffset(),"stack")
            };


            TestDataFile t1 = new("testname", "test1.json", new DateTime(2020, 1, 1), 1000, 1000, "machinename", "modified") { Extract = new ETWExtract() { Exceptions = new ExceptionStats(exceptions), MainModuleVersion = new ModuleVersion() { Version = "V1" } } };
            TestDataFile t2 = new("testname", "test2.json", new DateTime(2020, 1, 2), 1001, 1000, "machinename", "modified") { Extract = new ETWExtract() { Exceptions = new ExceptionStats(exceptions), MainModuleVersion = new ModuleVersion() { Version = "V2" } } };
            TestRunData testRunData = new(TestRun.CreateForSpecifiedFiles(new List<TestDataFile>() { t1, t2 }), new ETWAnalyzer.Commands.OutDir { OutputDirectory = "", IsDefault = true });


            UniqueExceptionsWithSourceFiles testDataFilesExcludedDuplicateExce = new(new List<TestDataFile>() { t1, t2 });
            Assert.Single(testDataFilesExcludedDuplicateExce.ExceptionsWithSources);

            exceptions.AddRange(exceptions);
            testDataFilesExcludedDuplicateExce = new UniqueExceptionsWithSourceFiles(new List<TestDataFile>() { t1, t2 });
            Assert.Single(testDataFilesExcludedDuplicateExce.ExceptionsWithSources);
        }
        [Fact]
        public void Can_Exclude_DuplicateExceptions_By_Similar_Stack()
        {
            string firstStackExecute = "System.Linq.Enumerable+WhereSelectArrayIterator`2[System.__Canon,System.__Canon].MoveNext()" +
                "\nSystem.Collections.Generic.List`1[System.__Canon]..ctor(System.Collections.Generic.IEnumerable`1 < System.__Canon >)" +
                "\nSystem.Linq.Enumerable.ToList[System.__Canon](System.Collections.Generic.IEnumerable`1 < System.__Canon >)" +
                "\nsyngo.MR.PreProcessing.Service.Host.Services.ResourceQueue.Components.CentralResourceAdministrator +<> c__DisplayClass72_0.< UpdateMonitoredProcesses > b__0()" +
                "\nSystem.Threading.Tasks.Task.Execute()";
            string secondStackExecute = firstStackExecute + "\nSystem.Threading.ExecutionContext.RunInternal(System.Threading.ExecutionContext, System.Threading.ContextCallback, System.Object, Boolean)";

            string firstStackIL_Throw = "RaiseException" +
                "\nRaiseTheExceptionInternalOnly" +
                "\nIL_Throw" +
                "\nSystem.Data.SQLite.SQLite3.Reset(System.Data.SQLite.SQLiteStatement)" +
                "\nSystem.Data.SQLite.SQLite3.Step(System.Data.SQLite.SQLiteStatement)" +
                "\nSystem.Data.SQLite.SQLiteDataReader.NextResult()" +
                "\nSystem.Data.SQLite.SQLiteDataReader..ctor(System.Data.SQLite.SQLiteCommand, System.Data.CommandBehavior)" +
                "\nSystem.Data.SQLite.SQLiteCommand.ExecuteReader(System.Data.CommandBehavior)" +
                "\nSystem.Data.SQLite.SQLiteCommand.ExecuteNonQuery(System.Data.CommandBehavior)";
            string secondStackIL_Throw = "RaiseTheExceptionInternalOnly" +
                "\nIL_Throw" +
                "\nSystem.Data.SQLite.SQLite3.Reset(System.Data.SQLite.SQLiteStatement)" +
                "\nSystem.Data.SQLite.SQLite3.Step(System.Data.SQLite.SQLiteStatement)" +
                "\nSystem.Data.SQLite.SQLiteDataReader.NextResult()" +
                "\nSystem.Data.SQLite.SQLiteDataReader..ctor(System.Data.SQLite.SQLiteCommand, System.Data.CommandBehavior)" +
                "\nSystem.Data.SQLite.SQLiteCommand.ExecuteReader(System.Data.CommandBehavior)" +
                "\nSystem.Data.SQLite.SQLiteCommand.ExecuteNonQuery(System.Data.CommandBehavior)" +
                "\nsyngo.Services.sDM.Engine.SimpleDataAdapter.AbstractSimpleDataAdapter.CommandExecuter(syngo.Services.sDM.Engine.SmartCommands.CommandCore.IDbCommandBuilder)";

            List<ExceptionEventForQuery> exceptions = new()
            {
                new ExceptionEventForQuery("ignore","SameType",new ETWProcess(), new DateTimeOffset(),firstStackExecute),
                new ExceptionEventForQuery("ignore","SameType",new ETWProcess(), new DateTimeOffset(),secondStackExecute),
                new ExceptionEventForQuery("ignore","SameType",new ETWProcess(), new DateTimeOffset(),firstStackIL_Throw),
                new ExceptionEventForQuery("ignore","SameType",new ETWProcess(), new DateTimeOffset(),secondStackIL_Throw)
            };

            TestDataFile t1 = new("testname", "test1.json", new DateTime(2020, 1, 1), 1001, 1000, "machinename", "modified") { Extract = new ETWExtract() { Exceptions = new ExceptionStats(exceptions), MainModuleVersion = new ModuleVersion() { Version = "V1" } } };
            TestDataFile t2 = new("testname", "test2.json", new DateTime(2020, 1, 2), 1002, 1000, "machinename", "modified") { Extract = new ETWExtract() { Exceptions = new ExceptionStats(exceptions), MainModuleVersion = new ModuleVersion() { Version = "V1" } } };

            TestRunData testRunData = new(TestRun.CreateForSpecifiedFiles(new List<TestDataFile>() { t1, t2 }), new ETWAnalyzer.Commands.OutDir { OutputDirectory = "" });


            UniqueExceptionsWithSourceFiles testDataFilesExcludedDuplicateExce = new(new List<TestDataFile>() { t1, t2 });
            Assert.Equal(2, testDataFilesExcludedDuplicateExce.ExceptionsWithSources.Count);
        }

        [Fact]
        public void Can_Detect_Differences_With_ModulVersion_Between_Runs()
        {
            TestDataFile tA = new("testname", "test1.json", new DateTime(2020,1,1), 1001, 1000, "machinename", "modified") { Extract = new ETWExtract() { MainModuleVersion = new ModuleVersion() { Version = "VersionRun1" } } };
            TestDataFile tB = new("testname", "test2.json", new DateTime(2020, 1, 2), 1002, 1000, "machinename", "modified") { Extract = new ETWExtract() { MainModuleVersion = new ModuleVersion() { Version = "VersionRun2" } } };
            TestRunData testRunData = new(TestRun.CreateForSpecifiedFiles(new List<TestDataFile>() { tA, tB }), new ETWAnalyzer.Commands.OutDir { OutputDirectory = "" });


            ExceptionSourceFileWithNextNeighboursModuleVersion t1 = new(tA);
            ExceptionSourceFileWithNextNeighboursModuleVersion t2 = new(tB);
            Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion> exceptionsOfRun = new();

            exceptionsOfRun.Add(new ExceptionKeyEvent("processname", "stack", "msg", "type", new DateTimeOffset()), t1);
            UniqueExceptionsWithSourceFiles firstRun = new(exceptionsOfRun);
            exceptionsOfRun.Clear();

            exceptionsOfRun.Add(new ExceptionKeyEvent("processname", "otherstack", "msg", "type", new DateTimeOffset()), t2);
            exceptionsOfRun.Add(new ExceptionKeyEvent("processname", "stack", "msg", "type", new DateTimeOffset()), t2);
            UniqueExceptionsWithSourceFiles secondRun = new(exceptionsOfRun);
            var diff = UniqueExceptionsWithSourceFiles.GetDifferencesTo(firstRun,secondRun);

            Assert.Equal("VersionRun1", diff.ExceptionsWithSources.First().Value.CurrentAndNextNeighboursModuleVersion.PreviousModuleVersion.Version);
            Assert.Equal("VersionRun2", diff.ExceptionsWithSources.First().Value.CurrentAndNextNeighboursModuleVersion.CurrentModuleVersion.Version);

            Assert.True(diff.ExceptionsWithSources.First().Value.IsExceptionCluster(ExceptionCluster.StartingException));
            Assert.Equal(ExceptionCluster.StartingException, diff.ExceptionsWithSources.First().Value.ExceptionStatePersistenceDependingCluster);
            exceptionsOfRun.Clear();


            exceptionsOfRun.Add(new ExceptionKeyEvent("processname", "otherstack", "msg", "type", new DateTimeOffset()), t1);
            exceptionsOfRun.Add(new ExceptionKeyEvent("processname", "stack", "msg", "type", new DateTimeOffset()), t1);
            firstRun = new UniqueExceptionsWithSourceFiles(exceptionsOfRun);
            exceptionsOfRun.Clear();

            exceptionsOfRun.Add(new ExceptionKeyEvent("processname", "stack", "msg", "type", new DateTimeOffset()), t2);
            secondRun = new UniqueExceptionsWithSourceFiles(exceptionsOfRun);
            diff = UniqueExceptionsWithSourceFiles.GetDifferencesTo(firstRun, secondRun);

            Assert.Equal("VersionRun1", diff.ExceptionsWithSources.First().Value.CurrentAndNextNeighboursModuleVersion.CurrentModuleVersion.Version);
            Assert.Equal("VersionRun2", diff.ExceptionsWithSources.First().Value.CurrentAndNextNeighboursModuleVersion.FollowingModuleVersion.Version);

            Assert.Equal(ExceptionCluster.EndingException, diff.ExceptionsWithSources.First().Value.ExceptionStatePersistenceDependingCluster);
        }

       // [Fact]
         void Can_Detect_DisjointTrend_Exceptions()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            using var temp = TempDir.Create();

            var testOfRun1 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test1.json", new DateTime(2000, 1, 1), 1000,"Version1", "trend3", "trend4", "trend6", "ignore");
            var testOfRun2 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test2.json", new DateTime(2000, 1, 2), 1000, "Version2", "trend1", "trend2", "trend4", "trend6", "ignore");
            var testOfRun3 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test3.json", new DateTime(2000, 1, 3), 1000, "Version3", "trend1", "trend2", "trend3", "trend6", "ignore", "ignore");
            var testOfRun4 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test4.json", new DateTime(2000, 1, 4), 1000, "Version4", "trend3", "ignore");
            var testOfRun5 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test5.json", new DateTime(2000, 1, 5), 1000, "Version5", "trend5", "trend6", "ignore");
            var testOfRun6 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test6.json", new DateTime(2000, 1, 6), 1000, "Version5", "trend2", "trend3", "trend4", "trend5", "trend6", "ignore");

            var detector = SimulateAnalysis(temp, true, DateTime.Now - TimeSpan.FromDays(14), true, testOfRun1, testOfRun2, testOfRun3, testOfRun4, testOfRun5, testOfRun6);

            var exceptionsWithSources = detector[ExceptionCharacteristic.DisjointTrends].ExceptionActivityCharacteristics.First().Value;

            Assert.True(exceptionsWithSources.All(x => x.Key.FlatMessage.Contains("trend")));

            Assert.Equal(6, exceptionsWithSources.Count);

            exceptionsWithSources = detector[ExceptionCharacteristic.DisjointTrendsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value;
            Assert.Equal(3, exceptionsWithSources.Count);
            Assert.Contains(exceptionsWithSources, x => x.Key.FlatMessage.Contains("trend1"));
            Assert.Contains(exceptionsWithSources, x => x.Key.FlatMessage.Contains("trend6"));
            Assert.Contains(exceptionsWithSources, x => x.Key.FlatMessage.Contains("trend5"));

            Assert.Equal(3, detector[ExceptionCharacteristic.DisjointTrendsInconsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Count);
            Assert.Contains(detector[ExceptionCharacteristic.DisjointTrendsInconsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("trend2"));
            Assert.Contains(detector[ExceptionCharacteristic.DisjointTrendsInconsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("trend3"));
            Assert.Contains(detector[ExceptionCharacteristic.DisjointTrendsInconsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("trend4"));

            Assert.Empty(detector[ExceptionCharacteristic.DisjointOutliers].ExceptionActivityCharacteristics.First().Value);
            Assert.Empty(detector[ExceptionCharacteristic.DisjointSporadics].ExceptionActivityCharacteristics.First().Value);
        }

      //  [Fact]
        void Can_Detect_DisjointOutlier_Exceptions()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            var temp = TempDir.Create();

            var testOfRun1 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test1.json", new DateTime(2000, 1, 1), 1000, "Version1", "outlier3", "outlier5", "ignore");
            var testOfRun2 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test2.json", new DateTime(2000, 1, 2), 1000, "Version1", "outlier2", "ignore");
            var testOfRun3 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test3.json", new DateTime(2000, 1, 3), 1000, "Version3", "outlier1", "outlier3", "outlier4", "outlier5", "ignore");
            var testOfRun4 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test4.json", new DateTime(2000, 1, 4), 1000, "Version4", "outlier2", "ignore");
            var testOfRun5 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test5.json", new DateTime(2000, 1, 5), 1000, "Version5", "outlier4", "outlier5", "ignore");

            var detector = SimulateAnalysis(temp, true, DateTime.Now - TimeSpan.FromDays(14), true, testOfRun1, testOfRun2, testOfRun3, testOfRun4, testOfRun5);

            Assert.True(detector[ExceptionCharacteristic.DisjointOutliers].ExceptionActivityCharacteristics.First().Value.All(x => x.Key.FlatMessage.Contains("outlier")));

            Assert.Equal(5, detector[ExceptionCharacteristic.DisjointOutliers].ExceptionActivityCharacteristics.First().Value.Count);
            Assert.Equal(2, detector[ExceptionCharacteristic.DisjointOutliersConsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Count);
            Assert.Equal(3, detector[ExceptionCharacteristic.DisjointOutliersInconsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Count);

            Assert.Contains(detector[ExceptionCharacteristic.DisjointOutliersConsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("outlier1"));
            Assert.Contains(detector[ExceptionCharacteristic.DisjointOutliersConsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("outlier4"));

            Assert.Contains(detector[ExceptionCharacteristic.DisjointOutliersInconsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("outlier3"));
            Assert.Contains(detector[ExceptionCharacteristic.DisjointOutliersInconsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("outlier5"));
            Assert.Contains(detector[ExceptionCharacteristic.DisjointOutliersInconsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("outlier2"));

            Assert.Empty(detector[ExceptionCharacteristic.DisjointTrends].ExceptionActivityCharacteristics.First().Value);
            Assert.Empty(detector[ExceptionCharacteristic.DisjointSporadics].ExceptionActivityCharacteristics.First().Value);
        }



      ///  [Fact]
        void Can_Detect_DisjointSporatic_Exceptions()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            using var temp = TempDir.Create();

            var testOfRun1 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test1.json", new DateTime(2000, 1, 1), 1000, "Version1", "sporatic2", "sporatic3", "sporatic4", "sporatic5", "ignore");
            var testOfRun2 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test2.json", new DateTime(2000, 1, 2), 1000, "Version2", "sporatic1", "sporatic2", "ignore");
            var testOfRun3 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test3.json", new DateTime(2000, 1, 3), 1000, "Version2", "sporatic3", "ignore");
            var testOfRun4 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test4.json", new DateTime(2000, 1, 4), 1000, "Version4", "ignore");
            var testOfRun5 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test5.json", new DateTime(2000, 1, 5), 1000, "Version5", "sporatic2", "sporatic3", "ignore");
            var testOfRun6 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test6.json", new DateTime(2000, 1, 6), 1000, "Version6", "sporatic1", "sporatic3", "ignore");
            var testOfRun7 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test7.json", new DateTime(2000, 1, 7), 1000, "Version7", "sporatic1", "ignore");
            var testOfRun8 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test8.json", new DateTime(2000, 1, 8), 1000, "Version8", "sporatic3", "sporatic4", "sporatic6", "ignore");

            var detector = SimulateAnalysis(temp, true, DateTime.Now - TimeSpan.FromDays(14), true, testOfRun1, testOfRun2, testOfRun3, testOfRun4, testOfRun5, testOfRun6, testOfRun7, testOfRun8);

            Assert.True(detector[ExceptionCharacteristic.DisjointSporadics].ExceptionActivityCharacteristics.First().Value.All(x => x.Key.FlatMessage.Contains("sporatic")));

            Assert.Equal(6, detector[ExceptionCharacteristic.DisjointSporadics].ExceptionActivityCharacteristics.First().Value.Count);

            Assert.Equal(3, detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Count);
            Assert.Contains(detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("sporatic4"));
            Assert.Contains(detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("sporatic5"));
            Assert.Contains(detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("sporatic6"));

            Assert.Equal(3, detector[ExceptionCharacteristic.DisjointSporadicsInconsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Count);
            Assert.Contains(detector[ExceptionCharacteristic.DisjointSporadicsInconsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("sporatic1"));
            Assert.Contains(detector[ExceptionCharacteristic.DisjointSporadicsInconsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("sporatic2"));
            Assert.Contains(detector[ExceptionCharacteristic.DisjointSporadicsInconsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("sporatic3"));

            Assert.Empty(detector[ExceptionCharacteristic.DisjointOutliers].ExceptionActivityCharacteristics.First().Value);
            Assert.Empty(detector[ExceptionCharacteristic.DisjointTrends].ExceptionActivityCharacteristics.First().Value);
        }



    //    [Fact]
        void Can_Detect_Sporatic_Trend_Outlier_As_Disjoint_Exceptions()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            using var temp = TempDir.Create();

            var testOfRun1 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test1.json", new DateTime(2000, 1, 1), 1000, "Version1", "sporatic1", "outlier2", "ignore");
            var testOfRun2 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test2.json", new DateTime(2000, 1, 2), 1000, "Version2", "outlier1", "trend1", "sporatic2", "sporatic3", "ignore");
            var testOfRun3 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test3.json", new DateTime(2000, 1, 3), 1000, "Version3", "trend1", "trend2", "sporatic2", "ignore");
            var testOfRun4 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test4.json", new DateTime(2000, 1, 4), 1000, "Version4", "trend2", "outlier2", "sporatic3", "ignore");
            var testOfRun5 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test5.json", new DateTime(2000, 1, 5), 1000, "Version5", "outlier1", "sporatic2", "sporatic3", "ignore");
            var testOfRun6 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test6.json", new DateTime(2000, 1, 6), 1000, "Version6", "outlier2", "trend2", "ignore");

            var detector = SimulateAnalysis(temp, true, DateTime.Now - TimeSpan.FromDays(14), true, testOfRun1, testOfRun2, testOfRun3, testOfRun4, testOfRun5, testOfRun6);

            Assert.True(detector[ExceptionCharacteristic.DisjointSporadics].ExceptionActivityCharacteristics.First().Value.All(x => x.Key.FlatMessage.Contains("sporatic")));
            Assert.True(detector[ExceptionCharacteristic.DisjointOutliers].ExceptionActivityCharacteristics.First().Value.All(x => x.Key.FlatMessage.Contains("outlier")));
            Assert.True(detector[ExceptionCharacteristic.DisjointTrends].ExceptionActivityCharacteristics.First().Value.All(x => x.Key.FlatMessage.Contains("trend")));

            Assert.Equal(3, detector[ExceptionCharacteristic.DisjointSporadics].ExceptionActivityCharacteristics.First().Value.Count);
            Assert.Equal(detector[ExceptionCharacteristic.DisjointSporadics].ExceptionActivityCharacteristics.First().Value.Count,
                detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Count + detector[ExceptionCharacteristic.DisjointSporadicsInconsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Count);

            Assert.Equal(2, detector[ExceptionCharacteristic.DisjointOutliers].ExceptionActivityCharacteristics.First().Value.Count);
            Assert.Equal(detector[ExceptionCharacteristic.DisjointOutliers].ExceptionActivityCharacteristics.First().Value.Count,
                detector[ExceptionCharacteristic.DisjointOutliersConsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Count + detector[ExceptionCharacteristic.DisjointOutliersInconsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Count);

            Assert.Equal(2, detector[ExceptionCharacteristic.DisjointTrends].ExceptionActivityCharacteristics.First().Value.Count);
            Assert.Equal(detector[ExceptionCharacteristic.DisjointTrends].ExceptionActivityCharacteristics.First().Value.Count,
                detector[ExceptionCharacteristic.DisjointTrendsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Count + detector[ExceptionCharacteristic.DisjointTrendsInconsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Count);
        }

     //   [Fact]
        void Can_Serialize_Still_Active_Trends()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            using var temp = TempDir.Create();

            var testOfRun1 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test1.json", new DateTime(2000, 1, 1), 1000, "Version1", "ignore");
            var testOfRun2 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test2.json", new DateTime(2000, 1, 2), 1000, "Version2", "trend1", "ignore");
            var testOfRun3 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test3.json", new DateTime(2000, 1, 3), 1000, "Version3", "trend1", "trend2", "ignore");
            var detector = SimulateAnalysis(temp, true, new DateTime(1999, 1, 1), true, testOfRun1, testOfRun2, testOfRun3);
            var files = Directory.GetFiles(temp.Name);

            Assert.Equal(Path.Combine(temp.Name, "StillActiveExceptionDifferencePersistentAnalyzerDetectionRelevantException_20000103-000000.json"), files[1]);
            var deserialized = new DetectionSerializeUpdater().DeserializeExistingResult(files[1]).DictOfExceptionData;
            Assert.Equal("CallupAdhocColdReadingCR", deserialized.Value.First().Key);
            Assert.Equal("trend1", deserialized.Value.First().Value.First().Key.FlatMessage);
            Assert.Equal("trend2", deserialized.Value.First().Value.Last().Key.FlatMessage);

            Assert.True(deserialized.Value.First().Value.First().Value.First().IsExceptionCluster(ExceptionCluster.StartingException));
            Assert.True(deserialized.Value.First().Value.Last().Value.First().IsExceptionCluster(ExceptionCluster.StartingException));

            Assert.Equal("Test2.json", deserialized.Value.First().Value.First().Value.First().SourceOfActiveException.FileName);
            Assert.Equal("Test3.json", deserialized.Value.First().Value.Last().Value.First().SourceOfActiveException.FileName);
        }

        // [Fact]
        void Can_Update_Still_Active_Serialized_Trends()
        {

            DateTime dateKey;
            using var temp = TempDir.Create();

            var testOfRun1 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test1.json", DateTime.Now - TimeSpan.FromDays(6), 1000, "Version1", "ignore");
            var testOfRun2 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test2.json", DateTime.Now - TimeSpan.FromDays(5), 1000, "Version2", "trend1", "ignore");
            var testOfRun3 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test3.json", dateKey = DateTime.Now - TimeSpan.FromDays(4), 1000, "Version3", "trend1", "trend2", "ignore");
            SimulateAnalysis(temp, true, DateTime.Now - TimeSpan.FromDays(14), true, testOfRun1, testOfRun2, testOfRun3);

            var files = Directory.GetFiles(temp.Name);
            string expectedPath = Path.Combine(temp.Name, "StillActiveExceptionDifferencePersistentAnalyzerDetectionRelevantException_" + dateKey.ToString("yyyyMMdd-HHmmss") + ".json");
            Assert.Equal(expectedPath, files[1]);

            var deserializedRelevant = new DetectionSerializeUpdater().DeserializeExistingResult(files[1]).SerializeableExceptionData;
            Assert.Equal("CallupAdhocColdReadingCR", deserializedRelevant.Value.First().Key);
            Assert.Equal(2, deserializedRelevant.Value.First().Value.Count);

            Assert.Equal("trend1", deserializedRelevant.Value.First().Value[0].Key.FlatMessage);
            Assert.Equal("Test2.json", deserializedRelevant.Value.First().Value[0].Value[0].SourceOfActiveException.FileName);

            Assert.Equal("trend2", deserializedRelevant.Value.First().Value[1].Key.FlatMessage);
            Assert.Equal("Test3.json", deserializedRelevant.Value.First().Value[1].Value[0].SourceOfActiveException.FileName);

            Assert.True(deserializedRelevant.Value.All(x => x.Value.First().Value[0].IsExceptionCluster(ExceptionCluster.StartingException)));

            // Sequencial testrunpackages are analyzed. After one package is analyzed we have to include the last testrun of the package before to cover all results.
            // So testOfRun3 appears in the package before and in this package
            testOfRun3 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test3.json", dateKey, 1000, "Version3", "trend1", "trend2", "ignore");
            var testOfRun4 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test4.json", DateTime.Now - TimeSpan.FromDays(3), 1000, "Version4", "trend1", "trend2", "trend3", "ignore");
            var testOfRun5 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test5.json", DateTime.Now - TimeSpan.FromDays(2), 1000, "Version5", "trend3", "trend4", "ignore");
            var testOfRun6 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test6.json", dateKey = DateTime.Now - TimeSpan.FromDays(1), 1000, "Version6", "trend2", "trend3", "trend4", "ignore");

            SimulateAnalysis(temp, true, DateTime.Now - TimeSpan.FromDays(14), true, testOfRun3, testOfRun4, testOfRun5, testOfRun6);

            files = Directory.GetFiles(temp.Name);
            expectedPath = Path.Combine(temp.Name, "StillActiveExceptionDifferencePersistentAnalyzerDetectionRelevantException_" + dateKey.ToString("yyyyMMdd-HHmmss") + ".json");
            Assert.Equal(expectedPath, files[1]);

            deserializedRelevant = new DetectionSerializeUpdater().DeserializeExistingResult(files[1]).SerializeableExceptionData;
            Assert.Equal("CallupAdhocColdReadingCR", deserializedRelevant.Value.First().Key);
            Assert.Equal(3, deserializedRelevant.Value.First().Value.Count);

            Assert.Equal("trend2", deserializedRelevant.Value.First().Value[0].Key.FlatMessage);
            Assert.Equal("Test3.json", deserializedRelevant.Value.First().Value[0].Value[0].SourceOfActiveException.FileName);
            Assert.Equal("Test4.json", deserializedRelevant.Value.First().Value[0].Value[1].SourceOfActiveException.FileName);
            Assert.Equal("Test6.json", deserializedRelevant.Value.First().Value[0].Value[2].SourceOfActiveException.FileName);

            Assert.Equal("trend3", deserializedRelevant.Value.First().Value[1].Key.FlatMessage);
            Assert.Equal("Test4.json", deserializedRelevant.Value.First().Value[1].Value[0].SourceOfActiveException.FileName);

            Assert.Equal("trend4", deserializedRelevant.Value.First().Value[2].Key.FlatMessage);
            Assert.Equal("Test5.json", deserializedRelevant.Value.First().Value[2].Value[0].SourceOfActiveException.FileName);

            Assert.True(deserializedRelevant.Value.All(x => x.Value.First().Value[0].IsExceptionCluster(ExceptionCluster.StartingException)));

        }

      //  [Fact]
         void Can_Detect_And_Serialize_Old_Irrelevant_Trends()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;

            DateTime dateKey;
            var temp = TempDir.Create();

            TestDataFile testOfRun1 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test1.json", DateTime.Now - TimeSpan.FromDays(16), 1000, "Version1", "ignore");
            TestDataFile testOfRun2 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test2.json", DateTime.Now - TimeSpan.FromDays(15), 1000, "Version2", "trend1", "trend3", "ignore");
            TestDataFile testOfRun3 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test3.json", dateKey = DateTime.Now - TimeSpan.FromDays(14), 1000, "Version3", "trend2", "trend1", "trend3", "ignore");
            SimulateAnalysis(temp, true, DateTime.Now - TimeSpan.FromDays(14), true, testOfRun1, testOfRun2, testOfRun3);

            testOfRun3 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test3.json", dateKey, 1000, "Version3", "trend2", "trend1", "trend3", "ignore");
            TestDataFile testOfRun4 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test4.json", DateTime.Now - TimeSpan.FromDays(13), 1000, "Version4", "trend2", "trend2", "trend3", "ignore");
            TestDataFile testOfRun5 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test5.json", DateTime.Now - TimeSpan.FromDays(12), 1000, "Version5", "trend2", "trend3", "ignore");
            TestDataFile testOfRun6 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test6.json", dateKey = DateTime.Now - TimeSpan.FromDays(11), 1000, "Version6", "trend2", "trend3", "ignore");
            SimulateAnalysis(temp, true, DateTime.Now - TimeSpan.FromDays(14), true, testOfRun3, testOfRun4, testOfRun5, testOfRun6);

            var files = Directory.GetFiles(temp.Name);
            Assert.Equal(2, files.Length);
            string path = Path.Combine(temp.Name, "StillActiveExceptionDifferencePersistentAnalyzerDetectionIrrelevantException_" + dateKey.ToString("yyyyMMdd-HHmmss") + ".json");
            Assert.Equal(files[0], Path.Combine(temp.Name, "StillActiveExceptionDifferencePersistentAnalyzerDetectionIrrelevantException_" + dateKey.ToString("yyyyMMdd-HHmmss") + ".json"));
            var deserializedIrrelevant = new DetectionSerializeUpdater().DeserializeExistingResult(files[0]).SerializeableExceptionData;

            //Irrelevant
            Assert.Equal("CallupAdhocColdReadingCR", deserializedIrrelevant.Value.First().Key);
            Assert.Equal(3, deserializedIrrelevant.Value.Values.First().Count);
            Assert.True(deserializedIrrelevant.Value.Values.First().All(x => x.Value.Length == 1));
            Assert.Equal("trend1", deserializedIrrelevant.Value.First().Value[0].Key.FlatMessage);
            Assert.Equal("Test2.json", deserializedIrrelevant.Value.First().Value[0].Value[0].SourceOfActiveException.FileName);
            Assert.True(deserializedIrrelevant.Value.First().Value[0].Value[0].IsExceptionCluster(ExceptionCluster.StartingException));


            Assert.Equal("trend3", deserializedIrrelevant.Value.First().Value[1].Key.FlatMessage);
            Assert.Equal("Test2.json", deserializedIrrelevant.Value.First().Value[1].Value[0].SourceOfActiveException.FileName);
            Assert.True(deserializedIrrelevant.Value.First().Value[1].Value[0].IsExceptionCluster(ExceptionCluster.StartingException));

            Assert.Equal("trend2", deserializedIrrelevant.Value.First().Value[2].Key.FlatMessage);
            Assert.Equal("Test3.json", deserializedIrrelevant.Value.First().Value[2].Value[0].SourceOfActiveException.FileName);
            Assert.True(deserializedIrrelevant.Value.First().Value[2].Value[0].IsExceptionCluster(ExceptionCluster.StartingException));
        }

      ///  [Fact]
        void Can_Update_Old_Irrelevant_And_New_Relevant_Serialized_Trends()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            DateTime dateKey;
            using var temp = TempDir.Create();

            var testOfRun1 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test1.json", DateTime.Now - TimeSpan.FromDays(16), 1000, "Version1", "ignore");
            var testOfRun2 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test2.json", DateTime.Now - TimeSpan.FromDays(15), 1000, "Version2", "trend1", "ignore");
            var testOfRun3 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test3.json", dateKey = DateTime.Now - TimeSpan.FromDays(14), 1000, "Version3", "trend1", "trend2", "ignore");
            SimulateAnalysis(temp, true, DateTime.Now - TimeSpan.FromDays(14), true, testOfRun1, testOfRun2, testOfRun3);

            testOfRun3 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test3.json", dateKey, 1000, "Version3", "trend1", "trend2", "ignore");
            var testOfRun4 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test4.json", DateTime.Now - TimeSpan.FromDays(13), 1000, "Version4", "trend1", "trend2", "trend3", "ignore");
            var testOfRun5 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test5.json", DateTime.Now - TimeSpan.FromDays(12), 1000, "Version5", "trend2", "trend3", "ignore");
            var testOfRun6 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test6.json", dateKey = DateTime.Now - TimeSpan.FromDays(11), 1000, "Version6", "trend2", "trend3", "ignore");
            SimulateAnalysis(temp, true, DateTime.Now - TimeSpan.FromDays(14), true, testOfRun3, testOfRun4, testOfRun5, testOfRun6);

            var files = Directory.GetFiles(temp.Name);
            Assert.Equal(2, files.Length);
            Assert.Equal(files[1], Path.Combine(temp.Name, "StillActiveExceptionDifferencePersistentAnalyzerDetectionRelevantException_" + dateKey.ToString("yyyyMMdd-HHmmss") + ".json"));
            Assert.Equal(files[0], Path.Combine(temp.Name, "StillActiveExceptionDifferencePersistentAnalyzerDetectionIrrelevantException_" + dateKey.ToString("yyyyMMdd-HHmmss") + ".json"));

            var deserializedRelevant = new DetectionSerializeUpdater().DeserializeExistingResult(files[1]).SerializeableExceptionData;
            var deserializedIrrelevant = new DetectionSerializeUpdater().DeserializeExistingResult(files[0]).SerializeableExceptionData;

            //Relevant
            Assert.Equal("CallupAdhocColdReadingCR", deserializedRelevant.Value.First().Key);
            Assert.Single(deserializedRelevant.Value);
            Assert.Equal("trend3", deserializedRelevant.Value.First().Value[0].Key.FlatMessage);
            Assert.True(deserializedRelevant.Value.All(x => x.Value.First().Value[0].IsExceptionCluster(ExceptionCluster.StartingException)));
            Assert.Equal("Test4.json", deserializedRelevant.Value.First().Value[0].Value[0].SourceOfActiveException.FileName);

            //Irrelevant
            Assert.Equal("CallupAdhocColdReadingCR", deserializedIrrelevant.Value.First().Key);
            Assert.Equal(2, deserializedIrrelevant.Value.First().Value.Count);
            Assert.Equal("trend1", deserializedIrrelevant.Value.First().Value[0].Key.FlatMessage);
            Assert.True(deserializedIrrelevant.Value.Values.First()[0].Value.All(x => x.IsExceptionCluster(ExceptionCluster.StartingException)));
            Assert.Equal("Test2.json", deserializedIrrelevant.Value.First().Value[0].Value[0].SourceOfActiveException.FileName);

            Assert.Equal("trend2", deserializedIrrelevant.Value.First().Value[1].Key.FlatMessage);
            Assert.True(deserializedIrrelevant.Value.Values.First()[1].Value.All(x => x.IsExceptionCluster(ExceptionCluster.StartingException)));
            Assert.Equal("Test3.json", deserializedIrrelevant.Value.First().Value[1].Value[0].SourceOfActiveException.FileName);
        }


    //    [Fact]
        void Can_Update_Old_Irrelevant_And_New_Relevant_Serialized_MultipleTrends()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            DateTime dateKey;
            using var temp = TempDir.Create();

            var testOfRun1 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test1.json", DateTime.Now - TimeSpan.FromDays(20), 1000, "Version1", "sporatic1", "sporatic3", "ignore");
            var testOfRun2 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test2.json", DateTime.Now - TimeSpan.FromDays(19), 1000, "Version2", "sporatic1", "sporatic2", "ignore");
            var testOfRun3 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test3.json", DateTime.Now - TimeSpan.FromDays(18), 1000, "Version3", "ignore");
            var testOfRun4 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test4.json", DateTime.Now - TimeSpan.FromDays(17), 1000, "Version4", "sporatic1", "sporatic2", "ignore");
            var testOfRun5 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test5.json", dateKey = DateTime.Now - TimeSpan.FromDays(16), 1000, "Version5", "sporatic2", "sporatic3", "ignore");

            var detector = SimulateAnalysis(temp, true, DateTime.Now - TimeSpan.FromDays(21), true, testOfRun1, testOfRun2, testOfRun3, testOfRun4, testOfRun5);

            var files = Directory.GetFiles(temp.Name);
            Assert.Equal(files[1], Path.Combine(temp.Name, "StillActiveExceptionDifferencePersistentAnalyzerDetectionRelevantException_" + dateKey.ToString("yyyyMMdd-HHmmss") + ".json"));

            var deserializedRelevant = new DetectionSerializeUpdater().DeserializeExistingResult(files[1]).SerializeableExceptionData;

            Assert.Equal(3, detector[ExceptionCharacteristic.DisjointSporadics].ExceptionActivityCharacteristics.First().Value.Count);
            Assert.Equal(2, deserializedRelevant.Value.First().Value.Count);

            Assert.Equal(3, detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Count);
            Assert.Contains(detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("sporatic1"));
            var sporaticSources = detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Where(x => x.Key.FlatMessage.Contains("sporatic1")).Select(x => x.Value).First();
            Assert.Equal("Test2.json", sporaticSources[0].SourceOfActiveException.FileName);
            Assert.Equal("Test4.json", sporaticSources[1].SourceOfActiveException.FileName);
            Assert.Empty(deserializedRelevant.Value.First().Value.Where(x => x.Key.FlatMessage.Contains("sporatic1")));


            Assert.Contains(detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("sporatic2"));
            sporaticSources = detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Where(x => x.Key.FlatMessage.Contains("sporatic2")).Select(x => x.Value).First();
            Assert.Equal("Test2.json", sporaticSources[0].SourceOfActiveException.FileName);
            Assert.Equal("Test4.json", sporaticSources[1].SourceOfActiveException.FileName);
            sporaticSources = deserializedRelevant.Value.First().Value.Where(x => x.Key.FlatMessage.Contains("sporatic2")).Select(x => x.Value).First();
            Assert.Equal("Test2.json", sporaticSources[0].SourceOfActiveException.FileName);
            Assert.Equal("Test4.json", sporaticSources[1].SourceOfActiveException.FileName);

            Assert.Contains(detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("sporatic3"));
            sporaticSources = detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Where(x => x.Key.FlatMessage.Contains("sporatic3")).Select(x => x.Value).First();
            Assert.Equal("Test1.json", sporaticSources[0].SourceOfActiveException.FileName);
            Assert.Equal("Test5.json", sporaticSources[1].SourceOfActiveException.FileName);
            sporaticSources = deserializedRelevant.Value.First().Value.Where(x => x.Key.FlatMessage.Contains("sporatic3")).Select(x => x.Value).First();
            Assert.Equal("Test1.json", sporaticSources[0].SourceOfActiveException.FileName);
            Assert.Equal("Test5.json", sporaticSources[1].SourceOfActiveException.FileName);

            Assert.Empty(detector[ExceptionCharacteristic.DisjointOutliers].ExceptionActivityCharacteristics.First().Value);

            testOfRun5 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test5.json", dateKey, 1000, "Version5", "sporatic2", "sporatic3", "ignore");
            var testOfRun6 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test6.json", DateTime.Now - TimeSpan.FromDays(15), 1000, "Version6", "sporatic2", "sporatic4", "ignore");
            var testOfRun7 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test7.json", DateTime.Now - TimeSpan.FromDays(14), 1000, "Version7", "ignore");
            var testOfRun8 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test8.json", DateTime.Now - TimeSpan.FromDays(13), 1000, "Version8", "sporatic2", "sporatic4", "ignore");
            var testOfRun9 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test9.json", dateKey = DateTime.Now - TimeSpan.FromDays(12), 1000, "Version9", "sporatic3", "sporatic4", "ignore");

            detector = SimulateAnalysis(temp, true, DateTime.Now - TimeSpan.FromDays(14), true, testOfRun5, testOfRun6, testOfRun7, testOfRun8, testOfRun9);

            files = Directory.GetFiles(temp.Name);
            Assert.Equal(files[0], Path.Combine(temp.Name, "StillActiveExceptionDifferencePersistentAnalyzerDetectionIrrelevantException_" + dateKey.ToString("yyyyMMdd-HHmmss") + ".json"));

            var deserializedIrrelevant = new DetectionSerializeUpdater().DeserializeExistingResult(files[0]).SerializeableExceptionData;

            Assert.Equal(3, detector[ExceptionCharacteristic.DisjointSporadics].ExceptionActivityCharacteristics.First().Value.Count);
            Assert.Equal(3, detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Count);

            Assert.Contains(detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("sporatic2"));
            sporaticSources = detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Where(x => x.Key.FlatMessage.Contains("sporatic2")).Select(x => x.Value).First();
            Assert.Equal("Test6.json", sporaticSources[0].SourceOfActiveException.FileName);
            Assert.Equal("Test8.json", sporaticSources[1].SourceOfActiveException.FileName);

            Assert.Contains(detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("sporatic3"));
            sporaticSources = detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Where(x => x.Key.FlatMessage.Contains("sporatic3")).Select(x => x.Value).First();
            Assert.Equal("Test5.json", sporaticSources[0].SourceOfActiveException.FileName);
            Assert.Equal("Test9.json", sporaticSources[1].SourceOfActiveException.FileName);

            sporaticSources = deserializedIrrelevant.Value.First().Value.Where(x => x.Key.FlatMessage.Contains("sporatic3")).Select(x => x.Value).First();
            Assert.Equal("Test1.json", sporaticSources[0].SourceOfActiveException.FileName);
            Assert.Equal("Test5.json", sporaticSources[1].SourceOfActiveException.FileName);
            Assert.Equal("Test9.json", sporaticSources[2].SourceOfActiveException.FileName);

            Assert.Contains(detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value, x => x.Key.FlatMessage.Contains("sporatic4"));
            sporaticSources = detector[ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff].ExceptionActivityCharacteristics.First().Value.Where(x => x.Key.FlatMessage.Contains("sporatic4")).Select(x => x.Value).First();
            Assert.Equal("Test6.json", sporaticSources[0].SourceOfActiveException.FileName);
            Assert.Equal("Test8.json", sporaticSources[1].SourceOfActiveException.FileName);

            sporaticSources = deserializedIrrelevant.Value.First().Value.Where(x => x.Key.FlatMessage.Contains("sporatic4")).Select(x => x.Value).First();
            Assert.Equal("Test6.json", sporaticSources[0].SourceOfActiveException.FileName);
            Assert.Equal("Test8.json", sporaticSources[1].SourceOfActiveException.FileName);

            Assert.Empty(detector[ExceptionCharacteristic.DisjointOutliers].ExceptionActivityCharacteristics.First().Value);

            testOfRun9 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test9.json", dateKey, 1000, "Version9", "sporatic3", "sporatic4", "ignore");
            var testOfRun10 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test10.json", DateTime.Now - TimeSpan.FromDays(11), 1000, "Version10", "ignore");
            var testOfRun11 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test11.json", DateTime.Now - TimeSpan.FromDays(10), 1000, "Version11", "sporatic4", "sporatic2", "ignore");
            var testOfRun12 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test12.json", DateTime.Now - TimeSpan.FromDays(9), 1000, "Version12", "ignore");
            var testOfRun13 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test13.json", DateTime.Now - TimeSpan.FromDays(8), 1000, "Version13", "sporatic4", "sporatic2", "ignore");
            var testOfRun14 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test14.json", dateKey = DateTime.Now - TimeSpan.FromDays(7), 1000, "Version14", "sporatic4", "sporatic2", "ignore");

            detector = SimulateAnalysis(temp, true, DateTime.Now - TimeSpan.FromDays(14), true, testOfRun9, testOfRun10, testOfRun11, testOfRun12, testOfRun13, testOfRun14);

            files = Directory.GetFiles(temp.Name);
            Assert.Equal(2, files.Length);
            Assert.Equal(files[0], Path.Combine(temp.Name, "StillActiveExceptionDifferencePersistentAnalyzerDetectionIrrelevantException_" + dateKey.ToString("yyyyMMdd-HHmmss") + ".json"));
            deserializedRelevant = new DetectionSerializeUpdater().DeserializeExistingResult(files[1]).SerializeableExceptionData;
            deserializedIrrelevant = new DetectionSerializeUpdater().DeserializeExistingResult(files[0]).SerializeableExceptionData;

            sporaticSources = deserializedRelevant.Value.First().Value.Where(x => x.Key.FlatMessage.Contains("sporatic2")).Select(x => x.Value).First();
            Assert.Equal("Test11.json", sporaticSources[0].SourceOfActiveException.FileName);
            Assert.Equal("Test13.json", sporaticSources[1].SourceOfActiveException.FileName);

            sporaticSources = deserializedIrrelevant.Value.First().Value.Where(x => x.Key.FlatMessage.Contains("sporatic3")).Select(x => x.Value).First();
            Assert.Equal("Test1.json", sporaticSources[0].SourceOfActiveException.FileName);
            Assert.Equal("Test5.json", sporaticSources[1].SourceOfActiveException.FileName);

            sporaticSources = deserializedIrrelevant.Value.First().Value.Where(x => x.Key.FlatMessage.Contains("sporatic4")).Select(x => x.Value).First();
            Assert.Equal("Test6.json", sporaticSources[0].SourceOfActiveException.FileName);
            Assert.Equal("Test8.json", sporaticSources[1].SourceOfActiveException.FileName);
        }
      //  [Fact]
        void Can_Update_Irrelevant_Exception_Serialize_With_Sources()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            DateTime dateKey;
            using var temp = TempDir.Create();

            var testOfRun1 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test1.json", DateTime.Now - TimeSpan.FromDays(20), 1000, "Version1", "ignore");
            var testOfRun2 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test2.json", DateTime.Now - TimeSpan.FromDays(19), 1000, "Version2", "trend1", "ignore");
            var testOfRun3 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test3.json", dateKey = DateTime.Now - TimeSpan.FromDays(18), 1000, "Version3", "trend1", "trend2", "ignore");
            SimulateAnalysis(temp, true, DateTime.Now - TimeSpan.FromDays(21), true, testOfRun1, testOfRun2, testOfRun3);

            var files = Directory.GetFiles(temp.Name);
            string expectedPath = Path.Combine(temp.Name, "StillActiveExceptionDifferencePersistentAnalyzerDetectionRelevantException_" + dateKey.ToString("yyyyMMdd-HHmmss") + ".json");
            Assert.Equal(expectedPath, files[1]);

            var deserializedRelevant = new DetectionSerializeUpdater().DeserializeExistingResult(files[1]).SerializeableExceptionData;
            Assert.Equal("CallupAdhocColdReadingCR", deserializedRelevant.Value.First().Key);
            Assert.Equal(2, deserializedRelevant.Value.First().Value.Count);
            Assert.Equal("trend1", deserializedRelevant.Value.First().Value[0].Key.FlatMessage);
            Assert.Equal("Test2.json", deserializedRelevant.Value.First().Value[0].Value[0].SourceOfActiveException.FileName);

            Assert.Equal("trend2", deserializedRelevant.Value.First().Value[1].Key.FlatMessage);
            Assert.Equal("Test3.json", deserializedRelevant.Value.First().Value[1].Value[0].SourceOfActiveException.FileName);

            Assert.True(deserializedRelevant.Value.All(x => x.Value.First().Value[0].IsExceptionCluster(ExceptionCluster.StartingException)));

            testOfRun3 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test3.json", dateKey, 1000, "Version3", "trend1", "trend2", "ignore");
            var testOfRun4 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test4.json", DateTime.Now - TimeSpan.FromDays(17), 1000, "Version4", "trend1", "trend2", "ignore");
            var testOfRun5 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test5.json", dateKey = DateTime.Now - TimeSpan.FromDays(16), 1000, "Version5", "trend1", "trend2", "ignore");
            SimulateAnalysis(temp, true, DateTime.Now - TimeSpan.FromDays(14), true, testOfRun3, testOfRun4, testOfRun5);

            files = Directory.GetFiles(temp.Name);
            Assert.Equal(files[1], Path.Combine(temp.Name, "StillActiveExceptionDifferencePersistentAnalyzerDetectionRelevantException_" + dateKey.ToString("yyyyMMdd-HHmmss") + ".json"));
            Assert.Equal(files[0], Path.Combine(temp.Name, "StillActiveExceptionDifferencePersistentAnalyzerDetectionIrrelevantException_" + dateKey.ToString("yyyyMMdd-HHmmss") + ".json"));

            deserializedRelevant = new DetectionSerializeUpdater().DeserializeExistingResult(files[1]).SerializeableExceptionData;
            var deserializedIrrelevant = new DetectionSerializeUpdater().DeserializeExistingResult(files[0]).SerializeableExceptionData;

            var sources = deserializedIrrelevant.Value.First().Value.Where(x => x.Key.FlatMessage.Contains("trend1")).Select(x => x.Value).First();
            Assert.Equal("Test2.json", sources[0].SourceOfActiveException.FileName);
            sources = deserializedIrrelevant.Value.First().Value.Where(x => x.Key.FlatMessage.Contains("trend2")).Select(x => x.Value).First();
            Assert.Equal("Test3.json", sources[0].SourceOfActiveException.FileName);

            testOfRun5 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test5.json", dateKey, 1000, "Version5", "trend1", "trend2", "ignore");
            var testOfRun6 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test6.json", DateTime.Now - TimeSpan.FromDays(15), 1000, "Version6", "trend2", "ignore");
            var testOfRun7 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test7.json", DateTime.Now - TimeSpan.FromDays(14), 1000, "Version7", "trend1", "ignore");
            var testOfRun8 = generator.GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test8.json", dateKey = DateTime.Now - TimeSpan.FromDays(13), 1000, "Version8", "trend1", "trend2", "ignore");
            SimulateAnalysis(temp, true, DateTime.Now - TimeSpan.FromDays(14), true, testOfRun5, testOfRun6, testOfRun7, testOfRun8);

            files = Directory.GetFiles(temp.Name);
            Assert.Equal(files[1], Path.Combine(temp.Name, "StillActiveExceptionDifferencePersistentAnalyzerDetectionRelevantException_" + dateKey.ToString("yyyyMMdd-HHmmss") + ".json"));
            Assert.Equal(files[0], Path.Combine(temp.Name, "StillActiveExceptionDifferencePersistentAnalyzerDetectionIrrelevantException_" + dateKey.ToString("yyyyMMdd-HHmmss") + ".json"));

            deserializedRelevant = new DetectionSerializeUpdater().DeserializeExistingResult(files[1]).SerializeableExceptionData;
            deserializedIrrelevant = new DetectionSerializeUpdater().DeserializeExistingResult(files[0]).SerializeableExceptionData;

            sources = deserializedIrrelevant.Value.First().Value.Where(x => x.Key.FlatMessage.Contains("trend1")).Select(x => x.Value).First();
            Assert.Equal("Test2.json", sources[0].SourceOfActiveException.FileName);
            Assert.Equal("Test5.json", sources[1].SourceOfActiveException.FileName);
            Assert.Equal("Test7.json", sources[2].SourceOfActiveException.FileName);

            sources = deserializedIrrelevant.Value.First().Value.Where(x => x.Key.FlatMessage.Contains("trend2")).Select(x => x.Value).First();
            Assert.Equal("Test3.json", sources[0].SourceOfActiveException.FileName);
            Assert.Equal("Test6.json", sources[1].SourceOfActiveException.FileName);
            Assert.Equal("Test8.json", sources[2].SourceOfActiveException.FileName);
        }

    }
}


    
        
    
            














    


