using ETWAnalyzer.Analyzers.Exception;
using ETWAnalyzer.Extract;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer
{

    enum ExceptionCluster
    {
        UndefineAble,
        StartingException,
        EndingException,
        OutlierException
    }
    /// <summary>
    /// Exception source file and exception nearest neighbours modulverions
    /// </summary>
    class ExceptionSourceFileWithNextNeighboursModuleVersion
    {

        /// <summary>
        /// Reduced source file the exception belongs to
        /// </summary>
        public TestDataFile SourceOfActiveException { get; private set; }
        public CurrentAndNextNeighboursModuleVersion CurrentAndNextNeighboursModuleVersion { get; private set; }
        public ExceptionCluster ExceptionStatePersistenceDependingCluster { get; private set; } = ExceptionCluster.UndefineAble;
        public string ClusterDefiningSubstring => ExceptionStatePersistenceDependingCluster.ToString().Replace("Exception", "");
        /// <summary>
        /// True: exception occurres in the last testrun of the analysis
        /// </summary>
        [JsonIgnore]
        public bool IsExceptionSourceFromFirstOrLastTestRun
            => CurrentAndNextNeighboursModuleVersion.IsVersionFromFirstRunInTimeSeries || CurrentAndNextNeighboursModuleVersion.IsVersionFromLastRunInTimeSeries;


            //=> SourceOfActiveException.PerformedAt >= start && SourceOfActiveException.PerformedAt <= end;
        /// <summary>
        /// Ctor for json deserialization
        /// </summary>
        /// <param name="sourceOfActiveException"></param>
        /// <param name="currentAndNextNeighboursModuleVersion"></param>
        /// <param name="exceptionStatePersistenceDependingCluster"></param>
        [JsonConstructor]
        public ExceptionSourceFileWithNextNeighboursModuleVersion(TestDataFile sourceOfActiveException, CurrentAndNextNeighboursModuleVersion currentAndNextNeighboursModuleVersion, ExceptionCluster exceptionStatePersistenceDependingCluster)
        {
            SourceOfActiveException = sourceOfActiveException;
            CurrentAndNextNeighboursModuleVersion = currentAndNextNeighboursModuleVersion;
            ExceptionStatePersistenceDependingCluster = exceptionStatePersistenceDependingCluster;
        }

        /// <summary>
        /// Sets exception source and the ModulVerion of the exception
        /// </summary>
        /// <param name="testDataFile">exception source</param>
        public ExceptionSourceFileWithNextNeighboursModuleVersion(TestDataFile testDataFile) : this(testDataFile, new CurrentAndNextNeighboursModuleVersion(testDataFile),ExceptionCluster.UndefineAble) { }
        public ExceptionSourceFileWithNextNeighboursModuleVersion(TestDataFile testDataFile,CurrentAndNextNeighboursModuleVersion currentAndNextNeighboursModuleVersion) : this(testDataFile, currentAndNextNeighboursModuleVersion, ExceptionCluster.UndefineAble) { }


        public void SetExceptionCluster(ExceptionCluster cluster)
        { 
            ExceptionStatePersistenceDependingCluster = cluster;
        }

        public bool HasModulVersionDiffByAlternatingException()
        {
            bool currentIsNotEqualPrevious = !CurrentAndNextNeighboursModuleVersion.CurrentEqualsPrevious;
            bool currentIsNotEqualFollowing = !CurrentAndNextNeighboursModuleVersion.CurrentEqualsFollowing;
            bool currentIsNotEqualPreviousAndFollowing = currentIsNotEqualPrevious && currentIsNotEqualFollowing;

            switch (ExceptionStatePersistenceDependingCluster)
            {
                case ExceptionCluster.OutlierException: return currentIsNotEqualPreviousAndFollowing;
                case ExceptionCluster.StartingException: return currentIsNotEqualPrevious;
                case ExceptionCluster.EndingException: return currentIsNotEqualFollowing;
                case ExceptionCluster.UndefineAble: return true;
                default: return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dateA"></param>
        /// <param name="dateB"></param>
        /// <returns></returns>
        public bool IsInSelectedTimeSeriesPeriod(DateTime dateA, DateTime dateB)
        {
            DateTime first = dateA <= dateB ? dateA : dateB;
            DateTime second = dateA <= dateB ? dateB : dateA;
            return SourceOfActiveException.PerformedAt >= first && SourceOfActiveException.PerformedAt <= second;
        }

        /// <summary>
        /// Finds the ModulVersions between the exception state alternates.
        /// The previous and/or following is not null if the exception is alternating between previous/following and current modulversion
        /// </summary>
        /// <returns>Modulversions depending on the <see cref="ExceptionCluster"/></returns>
        public (ModuleVersion previousIfRelevant, ModuleVersion current, ModuleVersion followingIfRelevant) GetResponsibleModulVersionsForAlternatingExceptionState()
        {
            var previous = IsExceptionCluster(ExceptionCluster.StartingException) || IsExceptionCluster(ExceptionCluster.OutlierException)
                            ? CurrentAndNextNeighboursModuleVersion.PreviousModuleVersion : null;
            var following = IsExceptionCluster(ExceptionCluster.EndingException) || IsExceptionCluster(ExceptionCluster.OutlierException)
                            ? CurrentAndNextNeighboursModuleVersion.FollowingModuleVersion : null;

            return (previous, CurrentAndNextNeighboursModuleVersion.CurrentModuleVersion, following);
        }
        public bool IsExceptionCluster(ExceptionCluster cluster) => ExceptionStatePersistenceDependingCluster.Equals(cluster);

        /// <summary>
        /// An Outlier exceptions belongs to the same testdatafile twice
        /// Or the analysis border cuts of the following modulversion or the modulversion before
        /// This Function merges the sourcefile data of  exceptions with ModulVersion
        /// -> Abs(FlatTestDataFileExceptions[n-1] - FlatTestDataFileExceptions[n]) => Exception assigned to FlatTestDataFileExceptions[n]
        /// -> Abs(FlatTestDataFileExceptions[n] - FlatTestDataFileExceptions[n+1]) => Exception assigned to FlatTestDataFileExceptions[n] again
        /// </summary>
        /// <param name="filesWithException">try to merge this data</param>
        /// <returns></returns>
        public static ExceptionSourceFileWithNextNeighboursModuleVersion[] MergeModulVersionDataIfSourcesAreEqual(IEnumerable<ExceptionSourceFileWithNextNeighboursModuleVersion> filesWithException)
        {
            List<ExceptionSourceFileWithNextNeighboursModuleVersion> mergedExceptionSources = new();
            List<ExceptionSourceFileWithNextNeighboursModuleVersion> rawExceptionSources = filesWithException.OrderBy(x => x.SourceOfActiveException.FileName).ToList();

            int penultimateIdx = rawExceptionSources.Count - 1;

            for (int i = 0; i < penultimateIdx; i++)
            {
                var source = rawExceptionSources[i];
                if (IsNecessaryToMergeByEqualSources(rawExceptionSources[i],rawExceptionSources[i + 1]))
                {
                    source = GetMergedAsOutlier(rawExceptionSources[i], rawExceptionSources[i + 1]);
                    ++i;//Skip next
                }
                mergedExceptionSources.Add(source);
            }
            AddLastRawExceptionSourceIfNecessary(mergedExceptionSources, rawExceptionSources);

            return mergedExceptionSources.OrderBy(x => x.SourceOfActiveException.PerformedAt).ToArray();
        }
        private static bool IsNecessaryToMergeByEqualSources(ExceptionSourceFileWithNextNeighboursModuleVersion sourceA, ExceptionSourceFileWithNextNeighboursModuleVersion sourceB)
            => sourceA.SourceOfActiveException.Equals(sourceB.SourceOfActiveException);        

        private static ExceptionSourceFileWithNextNeighboursModuleVersion GetMergedAsOutlier(ExceptionSourceFileWithNextNeighboursModuleVersion sourceA, ExceptionSourceFileWithNextNeighboursModuleVersion sourceB)
        {
            TestDataFile exceptionSourceRelevant = sourceA.SourceOfActiveException;
            CurrentAndNextNeighboursModuleVersion startingAndEndingModVMerged = GetOutliersRelevantModulVersions(sourceA, sourceB);
            return new ExceptionSourceFileWithNextNeighboursModuleVersion(exceptionSourceRelevant, startingAndEndingModVMerged, ExceptionCluster.OutlierException);
        }

        private static CurrentAndNextNeighboursModuleVersion GetOutliersRelevantModulVersions(ExceptionSourceFileWithNextNeighboursModuleVersion sourceA, ExceptionSourceFileWithNextNeighboursModuleVersion sourceB)
        {
            var (previousIfRelevantA, currentA, followingIfRelevantA) = sourceA.GetResponsibleModulVersionsForAlternatingExceptionState();
            var (previousIfRelevantB, currentB, followingIfRelevantB) = sourceB.GetResponsibleModulVersionsForAlternatingExceptionState();

            ModuleVersion previousRelevant = GetAnyModuleVersionNotEqualToNullOrThrowException(previousIfRelevantA, previousIfRelevantB);
            ModuleVersion currentRelevant = GetAnyModuleVersionNotEqualToNullOrThrowException(currentA, currentB);
            ModuleVersion followingRelevant = GetAnyModuleVersionNotEqualToNullOrThrowException(followingIfRelevantA, followingIfRelevantB);

            return new CurrentAndNextNeighboursModuleVersion(currentRelevant, previousRelevant, followingRelevant);
        }

        private static ModuleVersion GetAnyModuleVersionNotEqualToNullOrThrowException(ModuleVersion a, ModuleVersion b)
        {
            ModuleVersion relevant = a ?? b;
            return relevant == null ? throw new ArgumentNullException($"Both ModulVersions {a} and {b} cannot be null") : relevant;
        }

        private static void AddLastRawExceptionSourceIfNecessary(List<ExceptionSourceFileWithNextNeighboursModuleVersion> mergedExceptionSources, List<ExceptionSourceFileWithNextNeighboursModuleVersion> rawExceptionSources)
        {
            if (IsNecessaryToAddLastRawExceptionSource(mergedExceptionSources, rawExceptionSources))
            {
                mergedExceptionSources.Add(rawExceptionSources.Last());
            }
        }
        static bool IsNecessaryToAddLastRawExceptionSource(List<ExceptionSourceFileWithNextNeighboursModuleVersion> mergedCollection, List<ExceptionSourceFileWithNextNeighboursModuleVersion> rawCollection)
            => rawCollection.Count == 1 || LastSourceFilesAreNotEqual(mergedCollection, rawCollection);
        static bool LastSourceFilesAreNotEqual(List<ExceptionSourceFileWithNextNeighboursModuleVersion> collectionA, List<ExceptionSourceFileWithNextNeighboursModuleVersion> collectionB)
            => !LastSourceFilesAreEqual(collectionA, collectionB);
        static bool LastSourceFilesAreEqual(List<ExceptionSourceFileWithNextNeighboursModuleVersion> collectionA, List<ExceptionSourceFileWithNextNeighboursModuleVersion> collectionB)
            => collectionA.Last().SourceOfActiveException.Equals(collectionB.Last().SourceOfActiveException);


    }



}
