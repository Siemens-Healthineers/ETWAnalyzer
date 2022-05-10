//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer
{
    /// <summary>
    /// Time series Differences Container
    /// Processing Interface to detect exception charakteristics
    /// </summary>
    class TimeSeriesExceptionActivities
    {
        /// <summary>
        /// Detects exceptions of a specififc characteristic - Trend, outlier, sporatic...
        /// </summary>
        public Dictionary<ExceptionCharacteristic, TimeSeriesDetector> ExceptionCharacteristicDetector { get; }

        /// <summary>
        /// Testspecific unique exceptions without characterization
        /// Base to get more specificed results e.g. outliers,trends...
        /// </summary>
        public Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> AllTestSpecificExceptionsWithSourceFiles { get; private set; } = new();

        /// <summary>
        /// Generates a testpecific unique exception list and detector Factory
        /// </summary>
        /// <param name="diffsBetweenTestRunsOverTimeSeries"></param>
        public TimeSeriesExceptionActivities(List<TestSpecificCollectionOfUniqueExceptionsWithSource> diffsBetweenTestRunsOverTimeSeries)
        {
            Dictionary<string, UniqueExceptionsWithSourceFiles[]> joinedSubsets = diffsBetweenTestRunsOverTimeSeries      .SelectMany(diff => diff.TestSpecificExceptionsWithSourceFile)
                                                                                                                          .GroupBy(testspecificDiff => testspecificDiff.Key)
                                                                                                                          .ToDictionary(groupedByTestCase => groupedByTestCase.Key, groupedByTestCase => groupedByTestCase.Select(x => x.Value)
                                                                                                                          .ToArray());

            AssoziateExceptionsWithSourcetestForAlternatingExceptionStates(joinedSubsets);

            ExceptionCharacteristicDetector = CreateCharacteristicDetection(AllTestSpecificExceptionsWithSourceFiles);

        }
        private void AssoziateExceptionsWithSourcetestForAlternatingExceptionStates(Dictionary<string, UniqueExceptionsWithSourceFiles[]> unmergedEqualExceptionsWithSources)
        {
            foreach (var testWithExceptionAndSource in unmergedEqualExceptionsWithSources)
            {
                var exceptionsWithSource = testWithExceptionAndSource.Value.SelectMany(uniqueExceptionsWithSourceFiles => uniqueExceptionsWithSourceFiles.ExceptionsWithSources);

                var groupedByEqualExceptions  = exceptionsWithSource.GroupBy(exceptionWithSource => exceptionWithSource.Key).ToDictionary(x=>x.Key, y=>y.Select(z=>z.Value));

                var uniqueExceptionsWithTimeSeriesSources = groupedByEqualExceptions.ToDictionary(
                    groupedByException => groupedByException.Key,
                    groupedByException => ExceptionSourceFileWithNextNeighboursModuleVersion.MergeModulVersionDataIfSourcesAreEqual(groupedByException.Value));

                AllTestSpecificExceptionsWithSourceFiles.Add(testWithExceptionAndSource.Key, uniqueExceptionsWithTimeSeriesSources);
            }
        }


        /// <summary>
        /// Creates a dictionary which groups the different exception disjoint exception characteristics
        /// </summary>
        /// <param name="toCharacterise"></param>
        /// <returns></returns>
        public static Dictionary<ExceptionCharacteristic, TimeSeriesDetector> CreateCharacteristicDetection(Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> toCharacterise)
        {
            return new Dictionary<ExceptionCharacteristic, TimeSeriesDetector>()
            {
                {ExceptionCharacteristic.DisjointOutliers,                       new DisjointOutliersDetector(toCharacterise)},
                    {ExceptionCharacteristic.DisjointOutliersConsistentModVDiff,     new DisjointOutliersDetectorWithConsistentModulVersionDiff(toCharacterise) },
                    {ExceptionCharacteristic.DisjointOutliersInconsistentModVDiff,   new DisjointOutliersDetectorWithInconsistentModulVersionDiff(toCharacterise)},

                //is disjoint to

                {ExceptionCharacteristic.DisjointTrends,                        new DisjointTrendsDetector(toCharacterise) },
                    {ExceptionCharacteristic.DisjointTrendsConsistentModVDiff,      new DisjointTrendsDetectorWithConsistentModulVersionDiff(toCharacterise) },
                    {ExceptionCharacteristic.DisjointTrendsInconsistentModVDiff,    new DisjointTrendsDetectorWithInconsistentModulVersionDiff(toCharacterise) },

                // is disjoint to

                {ExceptionCharacteristic.DisjointSporadics,                     new DisjointSporadicsDetector(toCharacterise) },
                    {ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff,   new DisjointSporadicsDetectorWithConsistentModulVersionDiff(toCharacterise) },
                    {ExceptionCharacteristic.DisjointSporadicsInconsistentModVDiff, new DisjointSporadicsDetectorWithInconsistentModulVersionDiff(toCharacterise) }

            };


        }


    }
}

