//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer
{
    abstract class TimeSeriesDetector
    {
        /// <summary>
        /// Checks if the exception belongs to the specific classes
        /// </summary>
        /// <param name="checkForThis">exception with data for alternating states</param>
        /// <returns></returns>
        abstract public bool IsCharacteristic(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> checkForThis);
        
        /// <summary>
        /// All exceptions to characterise 
        /// </summary>
        protected Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> AllTestSpecificExceptionsWithSourceFiles { get; private set; }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="allTestSpecificExceptionsWithSourceFiles">all exceptions to characterise</param>
        public TimeSeriesDetector(Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> allTestSpecificExceptionsWithSourceFiles)
        {
            AllTestSpecificExceptionsWithSourceFiles = allTestSpecificExceptionsWithSourceFiles;
        }

        /// <summary>
        /// Characteristic exceptions for the specific class
        /// </summary>
        public Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> ExceptionActivityCharacteristics
            => AllTestSpecificExceptionsWithSourceFiles .ToDictionary(k => k.Key, v => v.Value.Where(exceptionWithSources => IsCharacteristic(exceptionWithSources))
                                                        .ToDictionary(k => k.Key, v => v.Value));

        /// <summary>
        /// Characteristic exceptions for the specific class filtered by processes
        /// </summary>
        /// <param name="processNamesPretty"></param>
        /// <returns></returns>
        public Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> GetExceptionActivityCharacteristicsFor(params string[] processNamesPretty)
        {
            return AllTestSpecificExceptionsWithSourceFiles.ToDictionary(x => x.Key, y => y.Value.Where(x => IsCharacteristic(x) && processNamesPretty.Any(y => y.Contains(x.Key.ProcessNamePretty))).ToDictionary(x=>x.Key,y=>y.Value));
        }
        /// <summary>
        /// Checks if all alternating exception states have different modulversions
        /// </summary>
        /// <param name="checkForThis"></param>
        /// <returns></returns>
        protected bool HasConsistentModulVerDifferences(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> checkForThis)
        {
            return checkForThis.Value.All(x => x.HasModulVersionDiffByAlternatingException());
        }
    }
    
    //====================================================================Outlier Detection====================================================================================================================
    /// <summary>
    /// Detects outlier-characterisic exceptions consisting only of outliercluster/-components
    /// </summary>
    class DisjointOutliersDetector : TimeSeriesDetector
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="allTestSpecificExceWithSourceFiles"></param>
        public DisjointOutliersDetector(Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> allTestSpecificExceWithSourceFiles):base(allTestSpecificExceWithSourceFiles){ }
        /// <summary>
        /// Checks if the exception is characteristic for a outlier
        /// </summary>
        /// <param name="checkForThis"></param>
        /// <returns></returns>
        public override bool IsCharacteristic(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> checkForThis)
        {
            return IsDisjointOutlierException(checkForThis);
        }

        /// <summary>
        /// This Method detects if the exception only occurs as an Outlier
        /// Outlier-Exception is in FlatTestDataFileExceptions[n]
        /// The first and last exception kann apear once
        /// </summary>
        /// <param name="checkCharacteristic"></param>
        /// <returns></returns>
        bool IsDisjointOutlierException(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> checkCharacteristic)
        {
            var dataWithoutFirstAndLast = checkCharacteristic.Value.Where(x => !x.IsExceptionSourceFromFirstOrLastTestRun).ToList();
            return dataWithoutFirstAndLast.Count > 0 ? dataWithoutFirstAndLast.All(x =>x.IsExceptionCluster(ExceptionCluster.OutlierException)) : false;
        }
    }

    class DisjointOutliersDetectorWithConsistentModulVersionDiff : DisjointOutliersDetector
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="allTestSpecificExceWithSourceFiles"></param>
        public DisjointOutliersDetectorWithConsistentModulVersionDiff(Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> allTestSpecificExceWithSourceFiles) : base(allTestSpecificExceWithSourceFiles) { }

        /// <summary>
        /// Checks if the exception is an outlier with consistent modulversion difference
        /// </summary>
        /// <param name="checkForThis"></param>
        /// <returns></returns>
        public override bool IsCharacteristic(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> checkForThis)
        {
            return base.IsCharacteristic(checkForThis) && HasConsistentModulVerDifferences(checkForThis);
        }
    }
    class DisjointOutliersDetectorWithInconsistentModulVersionDiff: DisjointOutliersDetector
    {
        public DisjointOutliersDetectorWithInconsistentModulVersionDiff(Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> allTestSpecificExceWithSourceFiles) : base(allTestSpecificExceWithSourceFiles) { }

        /// <summary>
        /// Checks if the exception is an outlier with inconsistent modulversion difference
        /// </summary>
        /// <param name="checkForThis"></param>
        /// <returns></returns>
        public override bool IsCharacteristic(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> checkForThis)
        {
            return base.IsCharacteristic(checkForThis) && !HasConsistentModulVerDifferences(checkForThis);
        }
    }

    //====================================================================Trend Detection====================================================================================================================
    /// <summary>
    /// Detects trend-characterisic exceptions consisting only of trendcluster/-components
    /// </summary>
    class DisjointTrendsDetector : TimeSeriesDetector
    {
        public DisjointTrendsDetector(Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> allTestSpecificExceWithSourceFiles) : base(allTestSpecificExceWithSourceFiles) { }
        /// <summary>
        /// Checks if the exception is characteristic for a trend
        /// </summary>
        /// <param name="checkForThis"></param>
        /// <returns></returns>
        public override bool IsCharacteristic(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> checkForThis)
        {
            return IsDisjointTrendException(checkForThis);
        }
        bool IsDisjointTrendException(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> checkCharacteristic)
        {
            var dataWithoutFirstAndLast = checkCharacteristic.Value.Where(x => !x.IsExceptionSourceFromFirstOrLastTestRun).ToList();
            return dataWithoutFirstAndLast.Count > 0 ? dataWithoutFirstAndLast.All(x => !x.IsExceptionCluster(ExceptionCluster.OutlierException)) : false;
        }
    }
    class DisjointTrendsDetectorWithConsistentModulVersionDiff:DisjointTrendsDetector
    {
        public DisjointTrendsDetectorWithConsistentModulVersionDiff(Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> allTestSpecificExceWithSourceFiles) : base(allTestSpecificExceWithSourceFiles) { }
        /// <summary>
        /// Checks if the exception is an trend with consistent modulversion difference
        /// </summary>
        /// <param name="checkForThis"></param>
        /// <returns></returns>
        public override bool IsCharacteristic(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> checkForThis)
        {
            return base.IsCharacteristic(checkForThis) && HasConsistentModulVerDifferences(checkForThis);
        }
    }
    class DisjointTrendsDetectorWithInconsistentModulVersionDiff : DisjointTrendsDetector
    {
        public DisjointTrendsDetectorWithInconsistentModulVersionDiff(Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> allTestSpecificExceWithSourceFiles) : base(allTestSpecificExceWithSourceFiles) { }
        /// <summary>
        /// Checks if the exception is an trend with inconsistent modulversion difference
        /// </summary>
        /// <param name="checkForThis"></param>
        /// <returns></returns>
        public override bool IsCharacteristic(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> checkForThis)
        {
            return base.IsCharacteristic(checkForThis) && !HasConsistentModulVerDifferences(checkForThis);
        }
    }

    //====================================================================Sproatics Detection====================================================================================================================
    /// <summary>
    /// Detects sporadic-characterisic exceptions consisting only of outliercluster and trendcluster
    /// </summary>
    class DisjointSporadicsDetector : TimeSeriesDetector
    {
        public DisjointSporadicsDetector(Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> allTestSpecificExceWithSourceFiles) : base(allTestSpecificExceWithSourceFiles) { }
        /// <summary>
        /// Checks if the exception is a sporadic
        /// </summary>
        /// <param name="checkForThis"></param>
        /// <returns></returns>
        public override bool IsCharacteristic(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> checkForThis)
        {
            return IsDisjointSporaticException(checkForThis);
        }
        bool IsDisjointSporaticException(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> checkCharacteristic)
        {

            bool hasOutlier = checkCharacteristic.Value.Where(x => !x.IsExceptionSourceFromFirstOrLastTestRun).Any(x =>x.IsExceptionCluster(ExceptionCluster.OutlierException));
            bool hasTrend = checkCharacteristic.Value.Where(x => !x.IsExceptionSourceFromFirstOrLastTestRun).Any(x => !x.IsExceptionCluster(ExceptionCluster.OutlierException));
            bool hasOnlyUndefineable = checkCharacteristic.Value.All(x => x.IsExceptionSourceFromFirstOrLastTestRun);

            return (hasOutlier && hasTrend) || hasOnlyUndefineable;
        }
    }
    class DisjointSporadicsDetectorWithConsistentModulVersionDiff:DisjointSporadicsDetector
    {
        public DisjointSporadicsDetectorWithConsistentModulVersionDiff(Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> allTestSpecificExceWithSourceFiles) : base(allTestSpecificExceWithSourceFiles) { }
        /// <summary>
        /// Checks if the exception is a sporadic exception with consistent modulversion difference
        /// </summary>
        /// <param name="checkForThis"></param>
        /// <returns></returns>
        public override bool IsCharacteristic(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> checkForThis)
        {
            return base.IsCharacteristic(checkForThis) && HasConsistentModulVerDifferences(checkForThis);
        }

    }
    class DisjointSporadicsDetectorWithInconsistentModulVersionDiff:DisjointSporadicsDetector
    {
        public DisjointSporadicsDetectorWithInconsistentModulVersionDiff(Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> allTestSpecificExceWithSourceFiles) : base(allTestSpecificExceWithSourceFiles) { }
        /// <summary>
        /// Checks if the exception is a sporadic exception with inconsistent modulversion difference
        /// </summary>
        /// <param name="checkForThis"></param>
        /// <returns></returns>
        public override bool IsCharacteristic(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> checkForThis)
        {
            return base.IsCharacteristic(checkForThis) && !HasConsistentModulVerDifferences(checkForThis);
        }
    }
}
