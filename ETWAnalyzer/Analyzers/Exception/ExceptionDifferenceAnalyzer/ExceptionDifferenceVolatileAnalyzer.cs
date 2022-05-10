//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers.Exception;
using ETWAnalyzer.Analyzers.Exception.ResultPrinter;
using ETWAnalyzer.Analyzers.Infrastructure;
using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;


namespace ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer
{
    // Sourcecode Entry - Bachelor thesis by Sebastian Scheller

    /// <summary>
    /// The occurrence of the exception over the timeseries is specified by the ExceptionCharacteristic enum
    /// </summary>
    internal enum ExceptionCharacteristic
    {
        Union,
        DisjointTrends,
        DisjointTrendsConsistentModVDiff,
        DisjointTrendsInconsistentModVDiff,

        DisjointOutliers,
        DisjointOutliersConsistentModVDiff,
        DisjointOutliersInconsistentModVDiff,

        DisjointSporadics,
        DisjointSporadicsConsistentModVDiff,
        DisjointSporadicsInconsistentModVDiff
    }


    /// <summary>
    /// 1.  testrun n
    /// 2.  Generate deep copy of relevant TestDataFile - Data of the testrun
    /// 3.  Assoziate emerging exceptions of a specific Testcase in the Testrun with their JSON-Sources
    /// 4.  Do the same things for testrun n+1
    /// 5.  Build the differences of the exceptions between testrun n and n+1
    /// 6.  Store the differences of all sequential testruns in a global list
    /// 7.  Aggregate equal exception of this list and associate them with all differences
    /// 8.  Gernerate Charakteristics depending on the associated differences (= components of each exception over the timeseries)
    /// </summary>
    class ExceptionDifferenceVolatileAnalyzer : ExceptionAnalyzerBase
    {
        //Flags from command
        public List<ExceptionCharacteristic> SelectedCharacteristicsFlag { get; set; } = new List<ExceptionCharacteristic>();
        public bool IsStillActiveExceptionDetectorFlag { get; set; }



        /// <summary>
        /// Used to determine the exception differences between two testruns
        /// Contains the aggregated and normalized exceptions with the association to their sources
        /// </summary>
        private TestSpecificCollectionOfUniqueExceptionsWithSource FirstRunInPair { get; set; }
        private TestSpecificCollectionOfUniqueExceptionsWithSource SecondRunInPair { get; set; }

        /// <summary>
        /// Collection of all testspecific exception differences between two sequencial runs 
        /// </summary>
        public List<TestSpecificCollectionOfUniqueExceptionsWithSource> AllDiffsBetweenTwoSequencialTestRuns { get; private set; } = new List<TestSpecificCollectionOfUniqueExceptionsWithSource>();

        /// <summary>
        /// Contains characterized timeseries exception data of relevant and irrelvant processes
        /// </summary>
        public TimeSeriesExceptionActivities ExceptionActivities { get; private set; }

        /// <summary>
        /// Complete characterized exceptions with associated data
        /// </summary>
        protected Dictionary<ExceptionCharacteristic, Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>> AllCharacterizedExceptionOfRelevantProcesses
            => ExceptionActivities.ExceptionCharacteristicDetector.ToDictionary(k => k.Key, v => v.Value.GetExceptionActivityCharacteristicsFor(RelevantProcessNames));

        /// <summary>
        /// Userselected characterized Exceptions with associated data
        /// </summary>
        protected Dictionary<ExceptionCharacteristic, Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>> SelectedCharacterizedExceptionsOfRelevantProcesses
            => ExceptionActivities.ExceptionCharacteristicDetector  .Where(x => SelectedCharacteristicsFlag.Contains(x.Key))
                                                                    .ToDictionary(k => k.Key, v => v.Value.GetExceptionActivityCharacteristicsFor(RelevantProcessNames));

        /// <summary>
        /// Selected exceptions with sources for console output
        /// </summary>
        protected Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> myExceptionsForConsoleOutput;

        protected Stopwatch StopWatch { get; set; } = new Stopwatch();

        /// <summary>
        /// Not Implemented
        /// </summary>
        /// <param name="issues"></param>
        /// <param name="backend"></param>
        /// <param name="frontend"></param>
        public override void AnalyzeTestsByTime(TestAnalysisResultCollection issues, TestDataFile backend, TestDataFile frontend) { }

        //====================================================================Analysis======================================================================

        /// <summary>
        /// 1. Excludes exception duplicates
        /// 2. Detects differences between two sequencial testruns
        /// 3. Add results to public difference collection
        /// </summary>
        /// <param name="issues"></param>
        /// <param name="run"></param>
        public override void AnalyzeTestRun(TestAnalysisResultCollection issues, TestRun run)
        {
            TestRun currRun = GenerateReducedDeepCopyOfSourceRun(run);
            TestRunsForAnalysis.Add(currRun);

            if (FirstRunInPair == null)
            {
                StopWatch.Start();
                FirstRunInPair = new TestSpecificCollectionOfUniqueExceptionsWithSource(currRun);
                TestAnalysisResults = issues;

                return;
            }

            SecondRunInPair = new TestSpecificCollectionOfUniqueExceptionsWithSource(currRun);

            TestSpecificCollectionOfUniqueExceptionsWithSource exceptionDiffsBetweenTwoSequencialRuns = TestSpecificCollectionOfUniqueExceptionsWithSource.GetDifferencesTo(FirstRunInPair, SecondRunInPair);
            AllDiffsBetweenTwoSequencialTestRuns.Add(exceptionDiffsBetweenTwoSequencialRuns);

            if (IsLastElement)
            {
                AnalyzeAllDiffsBetweenRuns();
                StopWatch.Stop();
            }
            FirstRunInPair = SecondRunInPair;
        }


        /// <summary>
        /// Finds interesting structures in the exception differences between the testruns
        /// </summary>
        internal void AnalyzeAllDiffsBetweenRuns()
        {
            //Aggregates equal exceptions and associates them with the sourcedata of alternating occurrence
            //Characterised exceptions depending on the alternating occurrence
            ExceptionActivities = new TimeSeriesExceptionActivities(AllDiffsBetweenTwoSequencialTestRuns);

            //Calls the specific result illustration
            DetectAnalyzerSpecificActivities();
        }



        /// <summary>
        /// Defines analyzer-specific results for console
        /// </summary>
        public virtual void DetectAnalyzerSpecificActivities()
        {
            var mergedRelevantExceptionCharacteristics = ExceptionDataContainer.JoinExceptionData(SelectedCharacterizedExceptionsOfRelevantProcesses.Values);

            myExceptionsForConsoleOutput = (IsStillActiveExceptionDetectorFlag ? 
                                           mergedRelevantExceptionCharacteristics.StillActiveExceptionData : 
                                           mergedRelevantExceptionCharacteristics.DictOfExceptionData)
                                           .Value;
        }

        /// <summary>
        /// Print:
        /// Prints an overview with exception occurrences
        /// Prins detailed exception properties with sources
        /// </summary>
        /// 
        public override void Print()
        {
            PrintOverview();
            PrintDetails();
            PrintAnalyzerSpecificOutputEnding();
        }
        private void PrintOverview()
        {
            ExceptionDifferenceAnalysisOverviewPrinter overviewPrinter = new(myExceptionsForConsoleOutput, TestRunsForAnalysis);
            overviewPrinter.PrintOverviewTableExceptionDetection();
        }
        private void PrintDetails()
        {
            ExceptionAnalysisDetailPrinter detailPrinter = new(myExceptionsForConsoleOutput, this);
            ConfigureDetailPrinter(detailPrinter);
            detailPrinter.PrintDetailedExceptionDetection();
        }
        virtual protected void ConfigureDetailPrinter(ExceptionAnalysisDetailPrinter detailPrinter)
        {
            detailPrinter.SetAdditionalInfoPerExceptionID(GetFormattedStringForLinearRegression);
            detailPrinter.SetFormattedExceptionSourceDescription(GetFormatedExceptionClusterDescriptionString);
        }
        private string GetFormattedStringForLinearRegression(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> exceptionWithSources)
        {
            (string regression, string yMean) = GetLinearRegression(exceptionWithSources);
            return $"\t{regression}\t\ty-mean = {yMean}";
        }
        private (string y, string yMean) GetLinearRegression(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> exceptionWithSource)
        {
            List<Point> function = TimeSeriesToMathematicalFunctionAdapter.GenerateTimeAndValueDiscretFunctionFromDifferentiatedExceptionData(exceptionWithSource.Value,TestRunsForAnalysis.ToList());
            var lr = new LinearRegression(function);

            return (lr.LinearEquation, Math.Round(lr.ArithmeticMeanOfYValues, 2).ToString());
        }
        protected string GetFormatedExceptionClusterDescriptionString(ExceptionSourceFileWithNextNeighboursModuleVersion source)
            => $"{(source.IsExceptionCluster(ExceptionCluster.OutlierException) ? "Outlier on".PadRight(15, ' ') : source.IsExceptionCluster(ExceptionCluster.StartingException) ? "Trend starts on" : "Trend ends on".PadRight(15, ' '))}: ";

        virtual protected void PrintAnalyzerSpecificOutputEnding()
        {
            PrintTimeSeriesActivitiesSummary();
            PrintAnalysisTimes();
        }

        /// <summary>
        /// Prints an overview
        /// </summary>
        private void PrintTimeSeriesActivitiesSummary()
        {
            Console.WriteLine("\n\nTime Series Activies - Overview:");
            foreach (var characteristic in ExceptionActivities.ExceptionCharacteristicDetector)
            {
                bool isSelected = SelectedCharacteristicsFlag.Contains(characteristic.Key);

                var allTestsWithExceptionsCollection = characteristic.Value.ExceptionActivityCharacteristics;

                ExceptionCharacteristic currChar = characteristic.Key;
                var relevantTestsWithExceptionsCollection = AllCharacterizedExceptionOfRelevantProcesses[currChar];

                ColorConsole.WriteLine($"\n{currChar} total Exceptioncount: { allTestsWithExceptionsCollection.SelectMany(x => x.Value).Count()} / from relevant Processes: { relevantTestsWithExceptionsCollection.SelectMany(x => x.Value).Count()}".PadRight(100, ' ') + 
                                       $"{(isSelected ? "\t\t(selected)" : "\t\t(unselected)")}", isSelected ? ConsoleColor.White : ConsoleColor.Gray);

                foreach (var testWithExceptions in characteristic.Value.ExceptionActivityCharacteristics)
                {
                    string currTestName = testWithExceptions.Key;
                    int absolutCount = testWithExceptions.Value.Count;
                    int relevantProcessExceptionCount = relevantTestsWithExceptionsCollection[currTestName].Values.Count;

                    Console.WriteLine($"{currTestName}: {absolutCount} (total) / {relevantProcessExceptionCount} (relevant)");
                }

            }
        }

        /// <summary>
        /// Prints elapsed analysistimes to console
        /// </summary>
        protected void PrintAnalysisTimes()
        {
            long elapsed = StopWatch.ElapsedMilliseconds;
            long elapsedTimeInSec = elapsed / 1000;
            long averageTimeInSecPerTestrun = (long)((double)elapsed / TestRunsForAnalysis.Count);
            Console.WriteLine($"\nElapsed time for analysis: { elapsedTimeInSec } seconds\nper TestRun: ~{averageTimeInSecPerTestrun} ms");
        }

        public override void TakePersistentFlagsFrom(AnalyzeCommand analyzeCommand)
        {
            base.TakePersistentFlagsFrom(analyzeCommand);

            IsStillActiveExceptionDetectorFlag = analyzeCommand.IsStillActiveExceptionDetector;

            if (analyzeCommand.ExceptionCharacteristicStrings.Count == 0)
            {
                AddDefaultAnalysisClusterToAnalyzer();
            }
            else
            {
                AddAnalysisClusterToAnalyzer(analyzeCommand);
            }
        }

        private void AddDefaultAnalysisClusterToAnalyzer()
        {
            SelectedCharacteristicsFlag.Add(ExceptionCharacteristic.DisjointOutliers);
            SelectedCharacteristicsFlag.Add(ExceptionCharacteristic.DisjointSporadics);
            SelectedCharacteristicsFlag.Add(ExceptionCharacteristic.DisjointTrends);
        }

        private void AddAnalysisClusterToAnalyzer(AnalyzeCommand analyzeCommand)
        {
            foreach (var enumCharacteristic in Enum.GetValues(typeof(ExceptionCharacteristic)).Cast<ExceptionCharacteristic>())
            {
                if (analyzeCommand.ExceptionCharacteristicStrings.Contains(enumCharacteristic.ToString().ToLowerInvariant()))
                {
                    SelectedCharacteristicsFlag.Add(enumCharacteristic);
                }
            }
        }
    }
}
