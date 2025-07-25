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
    }
}


    
        
    
            














    


