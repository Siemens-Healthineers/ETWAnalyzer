//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Analyzers;
using ETWAnalyzer.Analyzers.ExceptionOccurrence;
using ETWAnalyzer.Commands;
using ETWAnalyzer.Configuration;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Exceptions;
using ETWAnalyzer.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace ETWAnalyzer_uTest
{
    public class ExceptionOccurrenceAnalyzerTests
    {
        [Fact]
        public void Can_Count_TestRunIsolated_Exceptions()
        {
            var tmp = TempDir.Create();
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;

            var testAOfRun1 = GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test1.json", new DateTime(2000, 1, 1), "Version1", "ExceptionA");
            var testBOfRun1 = GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test2.json", new DateTime(2000, 1, 1), "Version1", "ExceptionB");

            var testAOfRun2 = GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test3.json", new DateTime(2000, 1, 2), "Version2", "ExceptionC");
            var testBOfRun2 = GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test4.json", new DateTime(2000, 1, 2), "Version2", "ExceptionC");
            var testCOfRun2 = GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test5.json", new DateTime(2000, 1, 2), "Version2", "ExceptionC");


            ExceptionOccurrenceAnalyzer analyzer = SimulateExceptionOccurrenceAnalysis(tmp, testAOfRun1, testBOfRun1, testAOfRun2, testBOfRun2, testCOfRun2);

            ulong occurrence = analyzer.ExceptionOrderedByOccurrenceCountWithSources["CallupAdhocColdReadingCR"].FirstOrDefault(x => x.Key.FlatMessage.Equals("ExceptionA")).Key.Occurrence;
            Assert.Equal(1, (int)occurrence);

            occurrence = analyzer.ExceptionOrderedByOccurrenceCountWithSources["CallupAdhocColdReadingCR"].FirstOrDefault(x => x.Key.FlatMessage.Equals("ExceptionB")).Key.Occurrence;
            Assert.Equal(1, (int)occurrence);

            occurrence = analyzer.ExceptionOrderedByOccurrenceCountWithSources["CallupAdhocColdReadingCR"].FirstOrDefault(x => x.Key.FlatMessage.Equals("ExceptionC")).Key.Occurrence;
            Assert.Equal(3, (int)occurrence);
        }

        [Fact]
        public void Can_Count_TestRunOverlapping_Exceptions()
        {
            var tmp = TempDir.Create();
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;

            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;

            var testAOfRun1 = GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test1.json", new DateTime(2000, 1, 1), "Version1", "ExceptionA");
            var testBOfRun1 = GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test2.json", new DateTime(2000, 1, 1), "Version1", "ExceptionA");
            var testCOfRun1 = GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test3.json", new DateTime(2000, 1, 1), "Version1", "ExceptionB");

            var testAOfRun2 = GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test4.json", new DateTime(2000, 1, 2), "Version2", "ExceptionA");
            var testBOfRun2 = GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test5.json", new DateTime(2000, 1, 2), "Version2", "ExceptionC");
            var testCOfRun2 = GenerateSyntheticTestDataFileWithExceptions("CallupAdhocColdReadingCR", "Test6.json", new DateTime(2000, 1, 2), "Version2", "ExceptionC");

            ExceptionOccurrenceAnalyzer analyzer = SimulateExceptionOccurrenceAnalysis(tmp, testAOfRun1, testBOfRun1,testCOfRun1, testAOfRun2, testBOfRun2, testCOfRun2);

            ulong occurrence = analyzer.ExceptionOrderedByOccurrenceCountWithSources["CallupAdhocColdReadingCR"].FirstOrDefault(x => x.Key.FlatMessage.Equals("ExceptionA")).Key.Occurrence;
            Assert.Equal(3, (int)occurrence);

            occurrence = analyzer.ExceptionOrderedByOccurrenceCountWithSources["CallupAdhocColdReadingCR"].FirstOrDefault(x => x.Key.FlatMessage.Equals("ExceptionB")).Key.Occurrence;
            Assert.Equal(1, (int)occurrence);

            occurrence = analyzer.ExceptionOrderedByOccurrenceCountWithSources["CallupAdhocColdReadingCR"].FirstOrDefault(x => x.Key.FlatMessage.Equals("ExceptionC")).Key.Occurrence;
            Assert.Equal(2, (int)occurrence);
        }
        [Fact]
        public void Can_Determine_Analyzers_Results()
        {
            var tmp = TempDir.Create();

            var runner = new AnalyzeCommand(new string[] { "-analyze", "ExceptionOccurrence", "-filedir", TestData.GetSampleDataJson, "-outdir", tmp.Name });
            runner.Parse();
            runner.Run();
            ExceptionOccurrenceAnalyzer analyzer = (ExceptionOccurrenceAnalyzer)runner.Analyzers[0];
            Assert.NotNull(analyzer.ExceptionOrderedByOccurrenceCountWithSources.First().Value.Keys);

            ulong occBefore = 0;
            foreach (var currExceptionKey in analyzer.ExceptionOrderedByOccurrenceCountWithSources.First().Value.Keys)
            {
                Assert.True(currExceptionKey.Occurrence >= occBefore);
                occBefore = currExceptionKey.Occurrence;
            }
        }

        TestDataFile GenerateSyntheticTestDataFileWithExceptions(string testName, string fileName, DateTime performedAt, string modulVersion, params string[] exceptionMessages)
        {
            return new TestDataFile(testName, fileName, performedAt, 1000, 10, "Machinename", null)
            {
                Extract = new ETWExtract()
                {
                    MainModuleVersion = new ModuleVersion() { Version = modulVersion },
                    Exceptions = new ExceptionStats(exceptionMessages.Select(x => new ExceptionEventForQuery(x, "Type", new ETWProcess() { CmdLine = "\"syngo.Viewing.Shell.Host.exe\"  host /HostId 6c2c9591-b360-42cd-95dd-e3f0e5c2db17 /type \"MM Reading\" ", ProcessName = "syngo.Viewing.Shell.Host.exe" }, new DateTimeOffset(), "Stack")).ToList())
                }
            };
        }
        ExceptionOccurrenceAnalyzer SimulateExceptionOccurrenceAnalysis(ITempOutput tmp, params TestDataFile[] tests)
        {
            TestRunData testRunData = new(TestRun.CreateForSpecifiedFiles(tests), new OutDir { OutputDirectory = tmp.Name });
            ExceptionOccurrenceAnalyzer analyzer = new();
            var r = new TestAnalysisResultCollection();

            // Set flags
            //....
            foreach (var run in testRunData.Runs)
            {
                if (run == testRunData.Runs[testRunData.Runs.Count - 1])
                { analyzer.IsLastElement = true; }
                analyzer.AnalyzeTestRun(r, run);
            }
            analyzer.IsLastElement = false;
            return analyzer;
        }


    }

}
