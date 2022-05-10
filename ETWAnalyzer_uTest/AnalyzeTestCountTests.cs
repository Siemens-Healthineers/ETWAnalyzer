//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ETWAnalyzer;
using ETWAnalyzer.Analyzers;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extractors;
using ETWAnalyzer.Helper;
using ETWAnalyzer.Configuration;

namespace ETWAnalyzer_uTest
{

    public class AnalyzeTestCountTests
    {
        private void SimulateTestCountAnalysis(TestRunData syntheticRunData, TestCountAnalyzer testCountAnalyzer, TestAnalysisResultCollection testAnalysisResults)
        {
            foreach (var run in syntheticRunData.Runs)
            {
                if (run == syntheticRunData.Runs[syntheticRunData.Runs.Count - 1])
                { testCountAnalyzer.IsLastElement = true; }
                testCountAnalyzer.AnalyzeTestRun(testAnalysisResults, run);
            }
        }

        [Fact]
        public void Can_Run_TestCountAnalyzer()
        {
            using var tmp = TempDir.Create();
            string extractFolder = Path.Combine(tmp.Name, "Extract");
            Directory.CreateDirectory(extractFolder);
            File.Copy(TestData.ClientJsonFileName, Path.Combine(extractFolder, Path.GetFileName(TestData.ClientJsonFileName)));
            File.Copy(TestData.ServerJsonFileName, Path.Combine(extractFolder, Path.GetFileName(TestData.ServerJsonFileName)));
            Program.MainCore(new string[] { "-analyze", "TestCount","-filedir", extractFolder,  "-outdir", tmp.Name });
        }
        [Fact]
        public void Can_Detect_TestRuns_Starting_With_Null_Tests()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            using var tmp = TempDir.Create();

            List<KeyValuePair<int, int>> indexWithTestCount = new()
            {
                new KeyValuePair<int, int>(0, 0),// Trend A
                new KeyValuePair<int, int>(1, 0),
                new KeyValuePair<int, int>(2, 0)
            };
            TestRunData syntheticRunData = TestRunCreator.CreateSynteticRun(tmp, indexWithTestCount, 10, 6);
            TestCountAnalyzer testCountAnalyzer = new();
            TestAnalysisResultCollection testAnalysisResults = new();

            SimulateTestCountAnalysis(syntheticRunData, testCountAnalyzer, testAnalysisResults);

            List<TestAnalysisResult> tempResult = testAnalysisResults.Where(x => x.Issues.Count > 0).ToList();

            Assert.Equal(13, tempResult.Count);
            Assert.Equal(13, tempResult.Where(x => x.Issues[0].Description == "No Tests at the beginning TestRuns").Count());
            Assert.Equal(syntheticRunData.Runs[3].Tests.Values.Last().First().PerformedAt, tempResult.Last().PerformedAt);
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x=>x.Contains(syntheticRunData.Runs[3].Tests.First().Value.First().PerformedAt.ToString())));

        }
        [Fact]
        public void Can_Detect_TestRuns_Ending_With_Null_Tests()
        {
            using var tmp = TempDir.Create();

            List<KeyValuePair<int, int>> indexWithTestCount = new()
            {
                new KeyValuePair<int, int>(3,0),// Trend A
                new KeyValuePair<int, int>(4,0),
                new KeyValuePair<int, int>(5,0)
            };

            TestRunData syntheticRunData = TestRunCreator.CreateSynteticRun(tmp, indexWithTestCount,10,6);
            TestCountAnalyzer testCountAnalyzer = new();
            TestAnalysisResultCollection testAnalysisResults = new();

            SimulateTestCountAnalysis(syntheticRunData, testCountAnalyzer, testAnalysisResults);

            List<TestAnalysisResult> tempResult = testAnalysisResults.Where(x => x.Issues.Count > 0).ToList();

            Assert.Equal(13, tempResult.Count);
            Assert.Equal(13, tempResult.Where(x=>x.Issues[0].Description == "TestCount difference trend starts").Count());
            Assert.Equal(syntheticRunData.Runs[2].Tests.Values.Last().Last().PerformedAt, tempResult.Last().PerformedAt);

        }
        [Fact]
        public void Can_Detect_TestRuns_StartingAndEnding_With_Null_Tests()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            using var tmp = TempDir.Create();

            List<KeyValuePair<int, int>> indexWithTestCount = new()
            {
                new KeyValuePair<int, int>(0,0),// Trend A
                new KeyValuePair<int, int>(1,0),
                new KeyValuePair<int, int>(2,0),

                new KeyValuePair<int, int>(7,0),// Trend B
                new KeyValuePair<int, int>(8,0),
                new KeyValuePair<int, int>(9,0)
            };

            TestRunData syntheticRunData = TestRunCreator.CreateSynteticRun(tmp, indexWithTestCount, 10, 9);
            TestCountAnalyzer testCountAnalyzer = new();
            TestAnalysisResultCollection testAnalysisResults = new ();

            SimulateTestCountAnalysis(syntheticRunData, testCountAnalyzer, testAnalysisResults);

            List<TestAnalysisResult> tempResult = testAnalysisResults.Where(x => x.Issues.Count > 0).ToList();

            Assert.Equal(26, tempResult.Count);
            Assert.Equal(13, tempResult.Where(x => x.Issues[0].Description == "TestCount difference trend starts").Count());
            Assert.Equal(13, tempResult.Where(x => x.Issues[0].Description == "No Tests at the beginning TestRuns").Count());
            Assert.Equal(syntheticRunData.Runs[3].Tests.Values.First().First().PerformedAt, tempResult.First().PerformedAt);
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[3].Tests.First().Value.First().PerformedAt.ToString())));

        }

        [Fact]
        public void Can_Detect_One_Trend_IsNull_In_TestRun()
        {
            using var tmp = TempDir.Create();

            List<KeyValuePair<int, int>> indexWithTestCount = new()
            {
                new KeyValuePair<int, int>(3,0),
                new KeyValuePair<int, int>(4,0)
            };
            TestRunData syntheticRunData = TestRunCreator.CreateSynteticRun(tmp, indexWithTestCount, 10, 8);
            TestCountAnalyzer testCountAnalyzer = new();
            TestAnalysisResultCollection testAnalysisResults = new();

            SimulateTestCountAnalysis(syntheticRunData, testCountAnalyzer, testAnalysisResults);

            List<TestAnalysisResult> tempResult = testAnalysisResults.Where(x => x.Issues.Count > 0).ToList();

            Assert.Equal(13, tempResult.Count);
            Assert.Equal(13, tempResult.Where(x => x.Issues[0].Description == "TestCount difference trend starts").Count());
            Assert.Equal(syntheticRunData.Runs[2].Tests.Values.Last().Last().PerformedAt, tempResult.Last().PerformedAt);
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[5].Tests.First().Value.First().PerformedAt.ToString())));

        }
        [Fact]
        public void Can_Detect_One_Trend_In_TestRun()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            using var tmp = TempDir.Create();

            List<KeyValuePair<int, int>> indexWithTestCount = new()
            {
                new KeyValuePair<int, int>(3,8),
                new KeyValuePair<int, int>(4,8)
            };
            TestRunData syntheticRunData = TestRunCreator.CreateSynteticRun(tmp, indexWithTestCount, 10, 8);
            TestCountAnalyzer testCountAnalyzer = new();
            TestAnalysisResultCollection testAnalysisResults = new();

            SimulateTestCountAnalysis(syntheticRunData, testCountAnalyzer, testAnalysisResults);

            List<TestAnalysisResult> tempResult = testAnalysisResults.Where(x => x.Issues.Count > 0).ToList();

            Assert.Equal(13, tempResult.Count);
            Assert.Equal(13, tempResult.Where(x => x.Issues[0].Description == "TestCount difference trend starts").Count());
            Assert.Equal(syntheticRunData.Runs[3].Tests.Values.Last().Last().PerformedAt, tempResult.Last().PerformedAt);
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[5].Tests.First().Value.First().PerformedAt.ToString())));

        }
        [Fact]
        public void Can_Detect_Two_Sequential_Trends()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            using var tmp = TempDir.Create();

            List<KeyValuePair<int, int>> indexWithTestCount = new()
            {
                new KeyValuePair<int, int>(2,8),// Trend A
                new KeyValuePair<int, int>(3,8),

                new KeyValuePair<int, int>(6,8),// Trend B
                new KeyValuePair<int, int>(7,8)
            };
            TestRunData syntheticRunData = TestRunCreator.CreateSynteticRun(tmp, indexWithTestCount, 10, 10);
            TestCountAnalyzer testCountAnalyzer = new();
            TestAnalysisResultCollection testAnalysisResults = new();

            SimulateTestCountAnalysis(syntheticRunData, testCountAnalyzer, testAnalysisResults);

            List<TestAnalysisResult> tempResult = testAnalysisResults.Where(x => x.Issues.Count > 0).ToList();

            Assert.Equal(26, tempResult.Count);
            Assert.Equal(26, tempResult.Where(x => x.Issues[0].Description == "TestCount difference trend starts").Count());
            Assert.Equal(syntheticRunData.Runs[6].Tests.Values.Last().Last().PerformedAt, tempResult.Last().PerformedAt);
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[4].Tests.First().Value.First().PerformedAt.ToString())));
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[8].Tests.First().Value.First().PerformedAt.ToString())));

        }
        [Fact]
        public void Can_Detect_Two_Sequential_IsNullTrends()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            using var tmp = TempDir.Create();

            List<KeyValuePair<int, int>> indexWithTestCount = new()
            {
                new KeyValuePair<int, int>(2,0),//Trend A
                new KeyValuePair<int, int>(3,0),

                new KeyValuePair<int, int>(6,0),//Trend B
                new KeyValuePair<int, int>(7,0)
            };
            TestRunData syntheticRunData = TestRunCreator.CreateSynteticRun(tmp, indexWithTestCount, 10, 10);
            TestCountAnalyzer testCountAnalyzer = new();
            TestAnalysisResultCollection testAnalysisResults = new();

            SimulateTestCountAnalysis(syntheticRunData, testCountAnalyzer, testAnalysisResults);

            List<TestAnalysisResult> tempResult = testAnalysisResults.Where(x => x.Issues.Count > 0).ToList();

            Assert.Equal(26, tempResult.Count);
            Assert.Equal(26, tempResult.Where(x => x.Issues[0].Description == "TestCount difference trend starts").Count());
            Assert.Equal(syntheticRunData.Runs[5].Tests.Values.Last().Last().PerformedAt, tempResult.Last().PerformedAt);
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[4].Tests.First().Value.First().PerformedAt.ToString())));
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[8].Tests.First().Value.First().PerformedAt.ToString())));

        }
        [Fact]
        public void Can_Detect_Two_Nested_Trends()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            using var tmp = TempDir.Create();

            List<KeyValuePair<int, int>> indexWithTestCount = new()
            {
                new KeyValuePair<int, int>(3,8),//Trend A starts
                new KeyValuePair<int, int>(4,8),
                    new KeyValuePair<int, int>(5,6),//Trend B starts
                    new KeyValuePair<int, int>(6,6),
                    new KeyValuePair<int, int>(7,6),//Trend B ends
                new KeyValuePair<int, int>(8,8),
                new KeyValuePair<int, int>(9,8),
                new KeyValuePair<int, int>(10,8)//Trend A ends
            };
            TestRunData syntheticRunData = TestRunCreator.CreateSynteticRun(tmp, indexWithTestCount, 10, 15);
            TestCountAnalyzer testCountAnalyzer = new();
            TestAnalysisResultCollection testAnalysisResults = new();

            SimulateTestCountAnalysis(syntheticRunData, testCountAnalyzer, testAnalysisResults);

            List<TestAnalysisResult> tempResult = testAnalysisResults.Where(x => x.Issues.Count > 0).ToList();

            Assert.Equal(26, tempResult.Count);
            Assert.Equal(26, tempResult.Where(x => x.Issues[0].Description == "TestCount difference trend starts").Count());
            Assert.Equal(syntheticRunData.Runs[5].Tests.Values.Last().Last().PerformedAt, tempResult.Last().PerformedAt);
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[8].Tests.First().Value.First().PerformedAt.ToString())));
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[11].Tests.First().Value.First().PerformedAt.ToString())));

        }

        [Fact]
        public void Can_Detect_Two_Nested_IsNullTrends()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            using var tmp = TempDir.Create();

            List<KeyValuePair<int, int>> indexWithTestCount = new()
            {
                new KeyValuePair<int, int>(3,8),//Trend A starts
                new KeyValuePair<int, int>(4,8),
                    new KeyValuePair<int, int>(5,0),//Trend B starts
                    new KeyValuePair<int, int>(6,0),
                    new KeyValuePair<int, int>(7,0),//Trend B ends
                new KeyValuePair<int, int>(8,8),
                new KeyValuePair<int, int>(9,8),
                new KeyValuePair<int, int>(10,8)//Trend A ends
            };
            TestRunData syntheticRunData = TestRunCreator.CreateSynteticRun(tmp, indexWithTestCount, 10, 15);
            TestCountAnalyzer testCountAnalyzer = new();
            TestAnalysisResultCollection testAnalysisResults = new();

            SimulateTestCountAnalysis(syntheticRunData, testCountAnalyzer, testAnalysisResults);

            List<TestAnalysisResult> tempResult = testAnalysisResults.Where(x => x.Issues.Count > 0).ToList();

            Assert.Equal(26, tempResult.Count);
            Assert.Equal(26, tempResult.Where(x => x.Issues[0].Description == "TestCount difference trend starts").Count());
            Assert.Equal(syntheticRunData.Runs[4].Tests.Values.Last().Last().PerformedAt, tempResult.Last().PerformedAt);
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[4].Tests.First().Value.Last().PerformedAt.ToString())));
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[11].Tests.First().Value.First().PerformedAt.ToString())));

        }
        [Fact]
        public void Can_Detect_Two_Nested_Trends_EndingTimeIsEqual()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            using var tmp = TempDir.Create();
            List<KeyValuePair<int, int>> indexWithTestCount = new()
            {
                new KeyValuePair<int, int>(3,8),//Trend A starts
                new KeyValuePair<int, int>(4,8),
                    new KeyValuePair<int, int>(5,6),//Trend B starts
                    new KeyValuePair<int, int>(6,6),
                    new KeyValuePair<int, int>(7,6),
                //Both Trends ending at the same time
            };
            TestRunData syntheticRunData = TestRunCreator.CreateSynteticRun(tmp, indexWithTestCount, 10, 15);
            TestCountAnalyzer testCountAnalyzer = new();
            TestAnalysisResultCollection testAnalysisResults = new();

            SimulateTestCountAnalysis(syntheticRunData, testCountAnalyzer, testAnalysisResults);

            List<TestAnalysisResult> tempResult = testAnalysisResults.Where(x => x.Issues.Count > 0).ToList();

            Assert.Equal(26, tempResult.Count);
            Assert.Equal(26, tempResult.Where(x => x.Issues[0].Description == "TestCount difference trend starts").Count());
            Assert.Equal(syntheticRunData.Runs[5].Tests.Values.Last().Last().PerformedAt, tempResult.Last().PerformedAt);
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[8].Tests.First().Value.First().PerformedAt.ToString())));

        }

        [Fact]
        public void Can_Detect_Two_Sequential_Trends_Nested()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            using var tmp = TempDir.Create();

            List<KeyValuePair<int, int>> indexWithTestCount = new()
            {
                new KeyValuePair<int, int>(3,8),        //Trend A starts
                    new KeyValuePair<int, int>(4,6),    //Trend B starts
                    new KeyValuePair<int, int>(5,6),    //Trend B ends
                new KeyValuePair<int, int>(6,8),        
                new KeyValuePair<int, int>(7,8),
                    new KeyValuePair<int, int>(8,6),    //Trend C starts
                    new KeyValuePair<int, int>(9,6),    //Trend C ends
                new KeyValuePair<int, int>(10,8)        //Trend A ends
            };
            TestRunData syntheticRunData = TestRunCreator.CreateSynteticRun(tmp, indexWithTestCount, 10, 15);
            TestCountAnalyzer testCountAnalyzer = new();
            TestAnalysisResultCollection testAnalysisResults = new();

            SimulateTestCountAnalysis(syntheticRunData, testCountAnalyzer, testAnalysisResults);

            List<TestAnalysisResult> tempResult = testAnalysisResults.Where(x => x.Issues.Count > 0).ToList();

            Assert.Equal(39, tempResult.Count);
            Assert.Equal(39, tempResult.Where(x => x.Issues[0].Description == "TestCount difference trend starts").Count());
            Assert.Equal(syntheticRunData.Runs[8].Tests.Values.Last().Last().PerformedAt, tempResult.Last().PerformedAt);
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[6].Tests.First().Value.First().PerformedAt.ToString())));
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[10].Tests.First().Value.First().PerformedAt.ToString())));
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[11].Tests.First().Value.First().PerformedAt.ToString())));

        }

        [Fact]
        public void Can_Detect_Multiple_Nested_Trends()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            using var tmp = TempDir.Create();

            List<KeyValuePair<int, int>> indexWithTestCount = new()
            {
                new KeyValuePair<int, int>(2,8),// Trend A starts
                new KeyValuePair<int, int>(3,8),               
                    new KeyValuePair<int, int>(4,5),// Trend B starts
                    new KeyValuePair<int, int>(5,5),
                        new KeyValuePair<int, int>(6,1),// Trend C starts
                        new KeyValuePair<int, int>(7,1),// Trend C ends
                    new KeyValuePair<int, int>(8,5),// Trend B ends
                new KeyValuePair<int, int>(9,8),// Trend A ends
            };
            TestRunData syntheticRunData = TestRunCreator.CreateSynteticRun(tmp, indexWithTestCount, 10, 15);
            TestCountAnalyzer testCountAnalyzer = new();
            TestAnalysisResultCollection testAnalysisResults = new();

            SimulateTestCountAnalysis(syntheticRunData, testCountAnalyzer, testAnalysisResults);

            testCountAnalyzer.Print();
            List<TestAnalysisResult> tempResult = testAnalysisResults.Where(x => x.Issues.Count > 0).ToList();

            Assert.Equal(39, tempResult.Count);
            Assert.Equal(39, tempResult.Where(x => x.Issues[0].Description == "TestCount difference trend starts").Count());
            Assert.Equal(syntheticRunData.Runs[6].Tests.Values.Last().Last().PerformedAt, tempResult.Last().PerformedAt);
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[8].Tests.First().Value.First().PerformedAt.ToString())));
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[9].Tests.First().Value.First().PerformedAt.ToString())));
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[10].Tests.First().Value.First().PerformedAt.ToString())));
        }

        [Fact]
        public void Ignore_Single_Outlier()
        {
            using var tmp = TempDir.Create();

            List<KeyValuePair<int, int>> indexWithTestCount = new()
            {
                new KeyValuePair<int, int>(3,7),
                new KeyValuePair<int, int>(7,3),
                new KeyValuePair<int, int>(9,4),
            };
            TestRunData syntheticRunData = TestRunCreator.CreateSynteticRun(tmp, indexWithTestCount, 10, 15);
            TestCountAnalyzer testCountAnalyzer = new();
            TestAnalysisResultCollection testAnalysisResults = new();

            SimulateTestCountAnalysis(syntheticRunData, testCountAnalyzer, testAnalysisResults);

            List<TestAnalysisResult> tempResult = testAnalysisResults.Where(x => x.Issues.Count > 0).ToList();
            Assert.Empty(tempResult);
            Assert.Empty(tempResult.Where(x => x.Issues[0].Description == "TestCount difference trend starts"));
        }

        [Fact]
        public void Ignore_Alternating_Outliers()
        {
            TestRunCreator creator = new();
            using var tmp = TempDir.Create();

            List<KeyValuePair<int, int>> indexWithTestCount = new()
            {
                new KeyValuePair<int, int>(1,5),
                new KeyValuePair<int, int>(3,3),
                new KeyValuePair<int, int>(5,6),
                new KeyValuePair<int, int>(7,8),
                new KeyValuePair<int, int>(9,3),
                new KeyValuePair<int, int>(11,3)
            };
            TestRunData syntheticRunData = TestRunCreator.CreateSynteticRun(tmp, indexWithTestCount, 10, 10);
            TestCountAnalyzer testCountAnalyzer = new();
            TestAnalysisResultCollection testAnalysisResults = new();

            SimulateTestCountAnalysis(syntheticRunData, testCountAnalyzer, testAnalysisResults);

            List<TestAnalysisResult> tempResult = testAnalysisResults.Where(x => x.Issues.Count > 0).ToList();
            Assert.Empty(tempResult);
            Assert.Empty(tempResult.Where(x => x.Issues[0].Description == "TestCount difference trend starts"));
        }
        [Fact]
        public void Ignore_Outlier_Nested_Trend()
        {
            ConfigFiles.Testability_AlternateExpectedTestConfigFile = TestData.TestRunConfigurationXml;
            TestRunCreator creator = new();
            using var tmp = TempDir.Create();

            List<KeyValuePair<int, int>> indexWithTestCount = new()
            {
                new KeyValuePair<int, int>(2,8),
                new KeyValuePair<int, int>(3,7),
                new KeyValuePair<int, int>(4,8),

                new KeyValuePair<int, int>(7,8),
                new KeyValuePair<int, int>(8,7),
            };

            TestRunData syntheticRunData = TestRunCreator.CreateSynteticRun(tmp, indexWithTestCount, 10, 11);
            TestCountAnalyzer testCountAnalyzer = new();
            TestAnalysisResultCollection testAnalysisResults = new();

            SimulateTestCountAnalysis(syntheticRunData, testCountAnalyzer, testAnalysisResults);

            List<TestAnalysisResult> tempResult = testAnalysisResults.Where(x => x.Issues.Count > 0).ToList();

            Assert.Equal(39, tempResult.Count);
            Assert.Equal(39, tempResult.Where(x => x.Issues[0].Description == "TestCount difference trend starts").Count());
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[5].Tests.First().Value.First().PerformedAt.ToString())));
            Assert.True(tempResult.SelectMany(x => x.Issues).SelectMany(x => x.Details).ToList().Exists(x => x.Contains(syntheticRunData.Runs[9].Tests.First().Value.First().PerformedAt.ToString())));
        }



    }
}
