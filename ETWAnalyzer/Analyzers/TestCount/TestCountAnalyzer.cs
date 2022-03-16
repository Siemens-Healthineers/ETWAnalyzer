//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ETWAnalyzer.Analyzers
{
    /// <summary>
    /// Analyzer Instance - finds TestCount trends and ignores outliers
    /// </summary>
    class TestCountAnalyzer : AnalyzerBase
    {
        /// <summary>
        /// Expected Run
        /// </summary>
        TestRunConfiguration TestRunConfiguration { get; set; } = new TestRunConfiguration();
        private Dictionary<string, TrendCollection> TestCaseWithTestCountTrends { get; set; } = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="issues"></param>
        /// <param name="run"></param>
        public override void AnalyzeTestRun(TestAnalysisResultCollection issues, TestRun run)
        {
            TestRunsForAnalysis.Add(run);
            if(IsLastElement)
            {
                TestAnalysisResults = issues;
                Analyze();
            }
        }
        /// <summary>
        /// Defines the starts and ends of a TestCount - Trend
        /// Assigns the generated trends to the issues reference 
        /// </summary>
        private void Analyze()
        {
            foreach (var expectedTest in TestRunConfiguration.ExpectedRun.TestCases)
            {
                int currentDiff = 0;            // Difference between current TestCount and next TestCount
                int nextDiff = 0;               // Difference between the testcounts of the next run and the second following run
                bool IsNullStarting = false;

                TrendCollection trendsForEachTest = new();

                for (int i = 0; i < TestRunsForAnalysis?.Count - 2; i++)
                {
                    // Check the complete Run

                    // Set current TestCount value
                    int currentCount = GetTestCount(TestRunsForAnalysis[i], expectedTest, out SingleTest[] currentSingleTests);// Count of the Tests in the current run

                    // Set next TestCount value
                    int firstCount = GetTestCount(TestRunsForAnalysis[i + 1], expectedTest, out SingleTest[] firstSingleTests);// Count of the Tests in the next run

                    // Set second following TestCount value
                    int secondCount = GetTestCount(TestRunsForAnalysis[i + 2], expectedTest, out SingleTest[] secondSingleTests);// TestCount of the run after the next run

                    // Difference between current TestCount and next TestCount
                    currentDiff = currentCount - firstCount; // Missing: +  /  Add: -

                    // Difference between the testcounts of the next run and the second following run
                    nextDiff = firstCount - secondCount; // Missing: +   /  Add: -

                    //True if the first Tests in the TestRunData are null
                    if (i == 0 && currentCount == 0 && currentDiff == 0 && nextDiff == 0)
                    {
                        IsNullStarting = true;
                    }

                    //First TestRun Tests in TestRunData are null
                    if (currentCount == 0 && currentDiff < 0 && IsNullStarting)
                    {
                        // Missing test from beginning to firstSingleTests
                        trendsForEachTest.AddTrend(null, firstSingleTests, currentDiff, "No Tests at the beginning TestRuns");
                        IsNullStarting = false;
                        continue;
                    }

                    //Is a valid Trend:
                    //  -IsATrend:      Two or more sequential Tests have the same TestCount or continuous rising difference of the TestCout to the expected - must match
                    //  -IsAOutlier:    Just one TestCount lies out of the usual TestCount - must not match
                    if (Trend.IsValidTrend(currentCount, firstCount, secondCount))
                    {
                        //Check if the Trend already starts
                        //Close the Trend or Trends by defining the ending singleTest
                        if (!trendsForEachTest.TryToSetTrendEnding(currentDiff, firstSingleTests?.First()))
                        {
                            //Trend is not open yet - create a new Trend
                            if (firstSingleTests != null)
                            {
                                trendsForEachTest.AddTrend(firstSingleTests, null, currentDiff, "TestCount difference trend starts");
                            }
                            else
                            {
                                trendsForEachTest.AddTrend(currentSingleTests, null, currentDiff, "TestCount difference trend starts");
                            }
                        }
                    }
                }

                if(trendsForEachTest.Trends.Count>0)
                {
                    TestCaseWithTestCountTrends.Add(expectedTest.TestCaseName, trendsForEachTest);
                }
                AddAnalysisResultsForEachTest(trendsForEachTest);
            }
        }
        /// <summary>
        /// Gets the singletest and testcount of the expected test in the testrun
        /// </summary>
        /// <param name="testRun"></param>
        /// <param name="expectedTest"></param>
        /// <param name="singleTests">can be null when the test does not exist</param>
        /// <returns>cannot be null - returns an integer from 0...n-testcounts</returns>
        int GetTestCount(TestRun testRun, TestCase expectedTest, out SingleTest[] singleTests)
        {
            testRun.Tests.TryGetValue(expectedTest.TestCaseName, out singleTests);
            return singleTests != null ? singleTests.Length : 0;
        }

        /// <summary>
        /// Assigns the generated trends to the issues reference 
        /// Generates relevant details for the issues which include the test and duration of the trend
        /// </summary>
        /// <param name="trendsForEachTest">Trend information collection</param>
        void AddAnalysisResultsForEachTest(TrendCollection trendsForEachTest)
        {
            foreach (var trend in trendsForEachTest.Trends)
            {
                List<string> details = new()
                    {
                        "Start Frontend:    "+trend.StartsWithThisTest?.Frontend,
                        "Start Backend:     "+trend.StartsWithThisTest?.Backend,
                        "Performed At:      "+trend.StartsWithThisTest?.PerformedAt,
                        "End Frontend:      "+trend.EndsWithThisTests?.Frontend,
                        "End Backend:       "+trend.EndsWithThisTests?.Backend,
                        "PerformedAt:       "+trend.EndsWithThisTests?.PerformedAt
                    };
                TestAnalysisResults.AddIssue(trend.StartsWithThisTest ?? trend.EndsWithThisTests, new Issue(this, trend.Message, Classification.MissingETWData, Severities.Warning, details));

            }
        }
        /// <summary>
        /// 
        /// </summary>
        public override void Print()
        {
            foreach (var testCaseWithTestCountTrend in TestCaseWithTestCountTrends)
            {
                Console.Write(Environment.NewLine);
                Console.WriteLine($"TestCase: {testCaseWithTestCountTrend.Key}");
                Trace.Write(Environment.NewLine);
                Trace.WriteLine($"TestCase: {testCaseWithTestCountTrend.Key}");
                foreach (var trend in testCaseWithTestCountTrend.Value.Trends)
                {
                    Console.Write(Environment.NewLine);

                    Console.WriteLine(trend.Message);
                    Console.WriteLine("\tStart:");
                    PrintSourceFiles(trend.StartsWithThisTest);
                    Console.WriteLine("\tEnd: ");
                    PrintSourceFiles(trend.EndsWithThisTests);
                    Console.WriteLine("\tTestCount: " + trend.TestCount);
                }
            }
        }
        void PrintSourceFiles(SingleTest printForSingleTest)
        {
            if (printForSingleTest != null)
            {
                printForSingleTest.Files.ToList().ForEach(x => Console.WriteLine("\t\t" + Path.GetFileName(x.FileName)));
            }
            else
            {
                Console.WriteLine("\t\tNo Sources");
            }
        }
        /// <summary>
        /// Not implemented for this analyzer
        /// </summary>
        /// <param name="issues"></param>
        /// <param name="backend"></param>
        /// <param name="frontend"></param>
        public override void AnalyzeTestsByTime(TestAnalysisResultCollection issues, TestDataFile backend, TestDataFile frontend)
        {
        }

        public override void TakePersistentFlagsFrom(AnalyzeCommand analyzeCommand)
        {
            base.TakePersistentFlagsFrom(analyzeCommand);
        }



        /// <summary>
        /// Includes all detected Trends for one Testtype of the Runs in TestRunData 
        /// Can find the trendendings for multiple nested or sequiential testcount trends
        /// </summary>
        class TrendCollection
        {
            /// <summary>
            /// All detected Trends
            /// </summary>
            public List<Trend> Trends => myTrends.OrderBy(x => (x.StartsWithThisTest ?? x.EndsWithThisTests).Files[0].PerformedAt).ToList();
            readonly List<Trend> myTrends = new();

            /// <summary>
            /// Trends with defined start and undefined end
            /// </summary>
            public List<Trend> UnfinishedTrends { get => Trends.Where(x => x.StartsWithThisTest != null && x.EndsWithThisTests == null).ToList(); }
            /// <summary>
            /// Trend wit defined or undefined start und defined end
            /// </summary>
            public List<Trend> FinishedTrends { get => Trends.Where(x => x.EndsWithThisTests != null).ToList(); }

            /// <summary>
            /// Adds a Trend
            /// </summary>
            /// <param name="start">can be null</param>
            /// <param name="end">can be null</param>
            /// <param name="difference">the difference between current and next testcount of two sequential runs</param>
            /// <param name="message">for the analysis results</param>
            public void AddTrend(SingleTest[] start, SingleTest[] end, int difference, string message)
            {
                myTrends.Add(new Trend(start?.Last(), end?.First(), difference, start != null ? start.Length : 0, message));
            }
            /// <summary>
            /// Trys to find the end of any unfinshed trend
            /// Closes the newest trend first if the difference is equal.
            /// If multiple trends end at the same time, the function closes all that trends.
            /// </summary>
            /// <param name="diffBelongsToUnfinshedTrend">the difference between current and next testcount of two sequential runs</param>
            /// <param name="belongsToTrendAsEnding">could be the end file of the Trend</param>
            /// <returns></returns>
            public bool TryToSetTrendEnding(int diffBelongsToUnfinshedTrend, SingleTest belongsToTrendAsEnding)
            {
                if (UnfinishedTrends.Count > 0 && UnfinishedTrends.Last().IsTrendEnd(diffBelongsToUnfinshedTrend))
                {
                    UnfinishedTrends.Last().EndsWithThisTests = belongsToTrendAsEnding;
                    return true;
                }
                int sumOfDiffs = 0;
                for (int i = UnfinishedTrends.Count - 1; i >= 0; i--)
                {
                    sumOfDiffs += UnfinishedTrends[i].Difference;
                    if (sumOfDiffs + diffBelongsToUnfinshedTrend == 0)
                    {
                        List<Trend> unfinished = UnfinishedTrends;

                        // Is end of all Trends which are included to the sum
                        for (int b = i; b < unfinished.Count; b++)
                        {
                            unfinished[b].EndsWithThisTests = belongsToTrendAsEnding;
                        }
                        return true;
                    }
                }
                return false;
            }
        }
        /// <summary>
        /// Contains all relevant Information for a Trend
        /// End SingleTest is null      : Trend is not finished
        /// Start SingleTest is null    : The first Tests of the Testruns in the TestRunData do not exist
        /// </summary>
        class Trend
        {
            public Trend(SingleTest start, SingleTest end, int difference, int testCount, string message)
            {
                if ((start == null && end == null) || difference == 0 || message == null)
                {
                    throw new NullReferenceException("Cannot be null");
                }

                StartsWithThisTest = start;
                EndsWithThisTests = end;
                Difference = difference;
                TestCount = testCount;
                Message = message;
            }
            /// <summary>
            /// Tests break up after this file
            /// </summary>
            public SingleTest StartsWithThisTest { get; }
            /// <summary>
            /// This is the first file after the Trend ends
            /// Is null: the Trend is still open
            /// </summary>
            public SingleTest EndsWithThisTests { get; set; }
            /// <summary>
            /// difference between current and next testcount
            /// </summary>
            public int Difference { get; }
            /// <summary>
            /// Resultmessage for the analysis
            /// </summary>
            public string Message { get; }
            /// <summary>
            /// Testcount of the current Trend
            /// </summary>
            public int TestCount { get; }
            /// <summary>
            /// Check if the differences balance each other out
            /// </summary>
            /// <param name="currentDifference">difference between current and next testcount</param>
            /// <returns></returns>
            public bool IsTrendEnd(int currentDifference)
            {
                return currentDifference + Difference == 0;
            }
            /// <summary>
            /// It is a trend when static difference appears
            /// </summary>
            /// <param name="currentDiff">difference between the current and next Testrun</param>
            /// <param name="nextDiff">difference between the next and the next next Testrun</param>
            /// <returns>true by two sequiential equal TestCounts</returns>
            public static bool IsATrend(int currentDiff, int nextDiff)
            {
                int difference = currentDiff + nextDiff;
                return difference != 0 && currentDiff != 0;
            }
            /// <summary>
            /// Flag to ignore the outlier for one TestRun iteration in TestRunData
            /// </summary>
            static bool isOutlier;
            /// <summary>
            /// Sets the outlierflag.
            /// Stores the current outlier information to the outlierflag
            /// </summary>
            /// <param name="currentCount">outlier: equal to secoundCount</param>
            /// <param name="firstCount">outlier: not equal to currentCount or secondCount</param>
            /// <param name="secondCount">outlier: equal to currenCount</param>
            /// <returns>returns two times true if there is an outlier</returns>
            public static bool IsOulier(int currentCount, int firstCount, int secondCount)
            {
                if (isOutlier == true)
                {
                    isOutlier = false;
                    return true;
                }
                if ((currentCount == secondCount) && (firstCount != currentCount))
                {
                    isOutlier = true;
                }
                return isOutlier;
            }
            /// <summary>
            /// Checks if the Test is no outlier and a trend
            /// </summary>
            /// <param name="currentCount"></param>
            /// <param name="firstCount"></param>
            /// <param name="secondCount"></param>
            /// <returns>true by no outlier and trend(more than two sequential testcount) else false</returns>
            public static bool IsValidTrend(int currentCount, int firstCount, int secondCount)
            {
                bool isNoOulier = !IsOulier(currentCount, firstCount, secondCount);
                bool isATrend = IsATrend(currentCount - firstCount, firstCount - secondCount);
                return isNoOulier == true && isATrend == true;
            }
        }
    }
}
