//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT
using ETWAnalyzer.Analyzers.Exception.ExceptionDifferenceAnalyzer;
using ETWAnalyzer.JsonSerializing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer
{
    /// <summary>
    /// The serialize updater can serialize into two files.
    /// 1. DetectionRelevantException_...    contains exceptions with no matching condition for irrelevance
    /// 2. DetectionIrrelevantException_...  contains exceptions with matching condition for irrelevance
    /// 
    /// During generation of each file, the already exisiting file is merged with the current detection results
    /// 
    /// The user can choose between conditions for exception irrelevance:
    /// Condition A - exceptionexpircydate greater/newer (>) than last active appearance date of an exception
    /// Condition B - exceptionexpircydate greater/newer (>) than first active appearance date of an exception
    /// </summary>
    class DetectionSerializeUpdater
    {
        const string PathsubstringRelevant = "DetectionRelevantException_";
        const string PathsubstringIrrelevant = "DetectionIrrelevantException_";
        const string PathsubstringForStillActiveExceptions = "StillActive";
        readonly DateTime myExceptionExpiryDate;

        /// <summary>
        /// Flags
        /// </summary>
        private bool IrrelevanceDependsOnFirstOccurrenceDate { get; set; }
        private bool OnlyStillActiveExceptions { get; set; }


        /// <summary>
        /// Contains exceptions which are categorized as relevant
        /// </summary>
        public ExceptionDataContainer RelevantExceptionData { get; private set; } = new();
        /// <summary>
        /// Contains exceptions which are categorized as irrelevant
        /// </summary>
        public ExceptionDataContainer IrrelevantExceptionData { get; set; } = new();
        /// <summary>
        /// Contains the exceptions of the current analysis
        /// </summary>
        private ExceptionDataContainer CurrentExceptionData { get; } = new();
        /// <summary>
        /// Path of the relevant exception serialize
        /// </summary>
        string DetectionResultFileOfRelevantExceptions { get; }
        /// <summary>
        /// Returns multiple paths of relevant exception files
        /// </summary>
        List<string> OldDetectionFilesOfRelevantExcepions =>
            Directory.GetFiles(Path.GetDirectoryName(DetectionResultFileOfRelevantExceptions))
            .Where(x => x.StartsWith(DetectionResultFileOfRelevantExceptions.Substring(0, DetectionResultFileOfRelevantExceptions.LastIndexOf('_'))))
            .OrderByDescending(x => x)
            .ToList();
            
        /// <summary>
        /// Path of the irrelevant exception serialize
        /// </summary>
        private string DetectionResultFileOfIrrelevantExceptions { get; }

        /// <summary>
        /// Return multiple paths of irrlevant exception files
        /// </summary>
        List<string> OldResultFilesIrrelevantExceptions => 
            Directory.GetFiles(Path.GetDirectoryName(DetectionResultFileOfIrrelevantExceptions))
            .Where(x => x.StartsWith(DetectionResultFileOfIrrelevantExceptions.Substring(0, DetectionResultFileOfIrrelevantExceptions.LastIndexOf('_'))))
            .OrderByDescending(x => x)
            .ToList();

        /// <summary>
        /// Date of the last testrun of the current analysis
        /// Used as timestamp for the serialized files
        /// </summary>
        DateTime TestRunEnd { get; set; }
        /// <summary>
        /// Constructor used by unit-tests
        /// </summary>
        internal DetectionSerializeUpdater() { }

        /// <summary>
        /// Initializes the updater with the analysis flags and sets the data for updating
        /// </summary>
        /// <param name="analyzer">updater reads public flags from analyzer</param>
        /// <param name="exceptionData">data for updating</param>
        public DetectionSerializeUpdater(ExceptionDifferencePersistentAnalyzer analyzer, ExceptionDataContainer exceptionData)
        {
            myExceptionExpiryDate = analyzer.ExceptionExpiryDateFlag;
            CurrentExceptionData = exceptionData;
            IrrelevanceDependsOnFirstOccurrenceDate = analyzer.IsIrrelevantMeasuredFromFirstExceptionOccFlag;
            OnlyStillActiveExceptions = analyzer.IsStillActiveExceptionDetectorFlag;
            TestRunEnd = analyzer.TestRunsForAnalysis.Last().TestRunEnd;

            string analyzerSubstring = OnlyStillActiveExceptions ? string.Concat(PathsubstringForStillActiveExceptions, analyzer.GetType().Name) : analyzer.GetType().Name;

            DetectionResultFileOfRelevantExceptions = Path.Combine(analyzer.OutdirFlag, String.Concat(analyzerSubstring, PathsubstringRelevant, TestRunEnd.ToString("yyyyMMdd-HHmmss"), ".json"));
            OldDetectionFilesOfRelevantExcepions.Add(DetectionResultFileOfRelevantExceptions);

            DetectionResultFileOfIrrelevantExceptions = Path.Combine(analyzer.OutdirFlag, String.Concat(analyzerSubstring, PathsubstringIrrelevant, TestRunEnd.ToString("yyyyMMdd-HHmmss"), ".json"));
            OldResultFilesIrrelevantExceptions.Add(DetectionResultFileOfIrrelevantExceptions);
        }

        /// <summary>
        /// Update process: 
        /// 1. Deserialize exception data
        /// 2. Update exception data
        /// 3. Serialize exception data
        /// </summary>
        public void Update()
        {
            var (detectionRelevantOfSerializeBefore, detectionUnRelevantOfSerializeBefore) = DeserializeExceptionData();
            UpdateExceptions(detectionRelevantOfSerializeBefore, detectionUnRelevantOfSerializeBefore);
            SerializeUpdatedExceptionData();
        }

        /// <summary>
        /// Deserializes the relevant and irrelevant exception-files
        /// </summary>
        /// <returns>return the newest deserialized file of exceptions classified as relevant and the newest deserialized file of exceptions cassified as irrelevant</returns>
        Tuple<ExceptionDataContainer,ExceptionDataContainer> DeserializeExceptionData()
        {
            return  Tuple.Create(DeserializeExistingResult(OldDetectionFilesOfRelevantExcepions.Count > 0 ? OldDetectionFilesOfRelevantExcepions.First() : null)
                    , DeserializeExistingResult(OldResultFilesIrrelevantExceptions.Count > 0 ? OldResultFilesIrrelevantExceptions.First() : null));
        }

        /// <summary>
        /// Deserializes a file with exception data
        /// </summary>
        /// <param name="resultFile">path of the exception file</param>
        /// <returns></returns>
        internal ExceptionDataContainer DeserializeExistingResult(string resultFile)
        {
            ExceptionDataContainer beforeDetectedExceptions = new();
            if (resultFile != null)
            {
                try
                {
                    using FileStream fs = File.Open(resultFile, FileMode.Open);
                    var deserialize = JsonCreationBase<KeyValuePair<ExceptionCharacteristic, Dictionary<string, List<KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>>>>.DeserializeJson(fs);
                    beforeDetectedExceptions = new ExceptionDataContainer(deserialize);
                }
                catch (SystemException e)
                {
                    throw new SerializationException($"Cannot deserialize {resultFile}.", e);
                }
            }
            return beforeDetectedExceptions;
        }

        /// <summary>
        /// Updates the serialized exceptions with the current exception data, considering the chosen conditions 
        /// </summary>
        /// <param name="relevantExceptionDataPrevious">serialized relevant exceptions</param>
        /// <param name="irrelevantExceptionDataPrevious">serialized irrelevant exceptions</param>
        private void UpdateExceptions(ExceptionDataContainer relevantExceptionDataPrevious, ExceptionDataContainer irrelevantExceptionDataPrevious)
        {
            var filteredJoinedExceptionData = GetExceptionStateDependingJoinedExceptionData(relevantExceptionDataPrevious);

            foreach (var testsWithExceptionsAndSources in filteredJoinedExceptionData)
            {
                var (tempRelevantExceptionsWithSources, tempIrrelevantExceptionWithSources) = GetExceptionRelevanceForEachTest(testsWithExceptionsAndSources, irrelevantExceptionDataPrevious);

                AddRelevanceSeparatedExceptionToGlobalLists(testsWithExceptionsAndSources.Key, tempRelevantExceptionsWithSources, tempIrrelevantExceptionWithSources);
            }

            //Joins old irrelevant exceptiondata with current irrelevant exceptiondata
            IrrelevantExceptionData = ExceptionDataContainer.JoinExceptionData(irrelevantExceptionDataPrevious, IrrelevantExceptionData);
        }

        /// <summary>
        /// Joins current relevant exception data with relevant exception data of the analyisis before
        /// </summary>
        /// <param name="relevantExceptionDataBefore"></param>
        /// <returns></returns>
        private Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> GetExceptionStateDependingJoinedExceptionData(ExceptionDataContainer relevantExceptionDataBefore)
        {
            //Join exceptiondata of the current analysis with the relevant old exception results
            ExceptionDataContainer joinedExceptionData =    relevantExceptionDataBefore.IsEmpty() ?
                                                            CurrentExceptionData :
                                                            ExceptionDataContainer.JoinExceptionData(new ExceptionDataContainer(relevantExceptionDataBefore.DictOfExceptionData), CurrentExceptionData);
            //Use only current present exception if the flag is true
            return (OnlyStillActiveExceptions ? joinedExceptionData.StillActiveExceptionData : joinedExceptionData.DictOfExceptionData).Value;
        }

        private (Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> relevantExceptionsWithSources, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> irrelevantExceptionsWithSources) GetExceptionRelevanceForEachTest(KeyValuePair<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> testsWithExceptionsAndSources, ExceptionDataContainer irrelevantExceptionDataBefore)
        {
            Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> tempRelevantExceptionsWithSources = new(), tempIrrelevantExceptionWithSources = new();
           
            foreach (var exceptionWithSources in testsWithExceptionsAndSources.Value)
            {
                if (HasNotExceededExceptionExpircyDate(exceptionWithSources))
                {
                    if (IsAlreadyKnownAsIrrelevantException(exceptionWithSources.Key, testsWithExceptionsAndSources, irrelevantExceptionDataBefore))
                    {
                        // is not already known as irrelevant - so add to relevant
                        tempRelevantExceptionsWithSources.Add(exceptionWithSources.Key, exceptionWithSources.Value);
                    }
                }
                else
                {
                    // add older than N days and still present exceptions to irrelevant
                    tempIrrelevantExceptionWithSources.Add(exceptionWithSources.Key, exceptionWithSources.Value);
                }
            }
            return (tempRelevantExceptionsWithSources, tempIrrelevantExceptionWithSources);
        }
        private bool HasNotExceededExceptionExpircyDate(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> exceptionWithSources)
        {
            var userSelectedRelevantExceptionDate = (this.IrrelevanceDependsOnFirstOccurrenceDate ? 
                                                     exceptionWithSources.Value.First() 
                                                     : exceptionWithSources.Value.Last())
                                                     .SourceOfActiveException.PerformedAt;

            bool isNotExceeded = userSelectedRelevantExceptionDate > myExceptionExpiryDate;
            return isNotExceeded;
        }

        private bool IsAlreadyKnownAsIrrelevantException(ExceptionKeyEvent forException, KeyValuePair<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> currAnalyzedTestWithExceptionData, ExceptionDataContainer irrelevantExceptionDataPrevious)
        {
            return  irrelevantExceptionDataPrevious.IsEmpty() || 
                    !irrelevantExceptionDataPrevious.DictOfExceptionData.Value.TryGetValue(currAnalyzedTestWithExceptionData.Key, out var previousIrrelevantExcesWithSources) || 
                    !previousIrrelevantExcesWithSources.TryGetValue(forException, out var alreadyIncludedIrrelevant);
        }

        private void AddRelevanceSeparatedExceptionToGlobalLists(string currTestname, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> relevantExceptions, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> irrelevantExceptions)
        {
            if (relevantExceptions.Count > 0)   RelevantExceptionData.DictOfExceptionData.Value.Add(currTestname, relevantExceptions);
            if (irrelevantExceptions.Count > 0) IrrelevantExceptionData.DictOfExceptionData.Value.Add(currTestname, irrelevantExceptions);
        }



        /// <summary>
        /// Serializes all (relevant and irrelevant) exception data
        /// </summary>
        private void SerializeUpdatedExceptionData()
        {
            SerializeRelevantUpdated();
            SerializeIrrelevantUpdated();
        }
        private void SerializeRelevantUpdated()
        {
            SerializeResults(RelevantExceptionData, DetectionResultFileOfRelevantExceptions);
            TryToDeleteOldResults(OldDetectionFilesOfRelevantExcepions);
        }
        private void SerializeIrrelevantUpdated()
        {
            SerializeResults(IrrelevantExceptionData, DetectionResultFileOfIrrelevantExceptions);
            TryToDeleteOldResults(OldResultFilesIrrelevantExceptions);
        }
        /// <summary>
        /// Serializes given exceptions
        /// </summary>
        /// <param name="toSerialize">exceptions to serialize</param>
        /// <param name="resultFile"></param>
        private void SerializeResults(ExceptionDataContainer toSerialize, string resultFile)
        {
            try
            {
                using FileStream fr = File.Create(resultFile);
                new JsonCreationBase<KeyValuePair<ExceptionCharacteristic, Dictionary<string, List<KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>>>>().SerializeToJson(toSerialize.SerializeableExceptionData, fr);
            }
            catch (SerializationException ex)
            {
                throw new SerializationException($"Serialize to file {resultFile} failed.", ex);
            }
        }

        /// <summary>
        /// Deletes all files except the newest 
        /// </summary>
        /// <param name="toDelete"></param>
        void TryToDeleteOldResults(List<string> toDelete)
        {
            foreach (var f in toDelete)
            {
                try
                {
                    if (IsNotCurrentResultFile(f))
                    { File.Delete(f); }
                }
                catch (SystemException e)
                {
                    throw new SystemException($"Can not delete existing file: {f}", e);
                }
            }
        }
        bool IsNotCurrentResultFile(string file)
            => !file.Equals(DetectionResultFileOfRelevantExceptions) && !file.Equals(DetectionResultFileOfIrrelevantExceptions);
    }



    /// <summary>
    /// Contains only one source of exception data
    /// Allows different possibilities for exception data creation and access 
    /// </summary>
    class ExceptionDataContainer
    {

        private readonly KeyValuePair<ExceptionCharacteristic, Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>> myExceptionDataOnlyAsDictionary;
        private ExceptionDataContainer(params ExceptionDataContainer[] toJoin) : this(toJoin.Select(x => x.DictOfExceptionData.Value)){}
        private ExceptionDataContainer(IEnumerable<Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>> unionThis)
        {
            myExceptionDataOnlyAsDictionary = JoinExceptionData(unionThis).DictOfExceptionData;
        }

        public bool IsEmpty() => myExceptionDataOnlyAsDictionary.Value.Count == 0;

        public ExceptionDataContainer()
        {
            myExceptionDataOnlyAsDictionary = new(ExceptionCharacteristic.Union, new Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>());
        }
        public ExceptionDataContainer(KeyValuePair<ExceptionCharacteristic, Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>> data)
        {
            var exceWithTests = data.Value.Where(x => x.Value.Count > 0).ToDictionary(x => x.Key, y => y.Value.OrderBy(z => z.Value.First().SourceOfActiveException.PerformedAt).ToDictionary(x => x.Key, y => y.Value));
            myExceptionDataOnlyAsDictionary = new(data.Key, exceWithTests.Count > 0 ? exceWithTests : new Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>());
        }
        public ExceptionDataContainer(KeyValuePair<ExceptionCharacteristic, Dictionary<string, List<KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>>> data)
        {
            var exceWithTests = data.Value.Where(x => x.Value.Count > 0).ToDictionary(x => x.Key, y => y.Value.OrderBy(z => z.Value.First().SourceOfActiveException.PerformedAt).ToDictionary(x => x.Key, y => y.Value));
            myExceptionDataOnlyAsDictionary = new(data.Key, exceWithTests.Count > 0 ? exceWithTests : new Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>());
        }

        /// <summary>
        /// Joins the exception data to a object
        /// </summary>
        /// <param name="toJoin"></param>
        /// <returns></returns>
        public static ExceptionDataContainer JoinExceptionData(params ExceptionDataContainer[] toJoin)
        {
            return new ExceptionDataContainer(toJoin);
        }
        /// <summary>
        /// Joins the exception data to a object
        /// </summary>
        /// <param name="unionThis"></param>
        /// <returns></returns>
        public static ExceptionDataContainer JoinExceptionData(IEnumerable<Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>> unionThis)
        {
            var dictUnion = unionThis.SelectMany(dict => dict)
                                     .GroupBy(kvp => kvp.Key)
                                     .ToDictionary(groupedByTest => groupedByTest.Key, 
                                                   groupedByTest => groupedByTest.SelectMany(x => x.Value)
                                                                                 .GroupBy(kvp => kvp.Key)
                                                                                 .ToDictionary(groupedByException => groupedByException.Key, 
                                                                                               groupedByException => ExceptionSourceFileWithNextNeighboursModuleVersion.MergeModulVersionDataIfSourcesAreEqual(groupedByException.SelectMany(x => x.Value).Distinct().ToList())));

            return new ExceptionDataContainer(new KeyValuePair < ExceptionCharacteristic, Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>>(ExceptionCharacteristic.Union, dictUnion));
        }


        /// <summary>
        /// Exceptions which are still active in the last analysis testrun
        /// </summary>
        public KeyValuePair<ExceptionCharacteristic, Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>> StillActiveExceptionData
                => new(ExceptionCharacteristic.Union, myExceptionDataOnlyAsDictionary.Value.ToDictionary(x => x.Key, y => y.Value.Where(x => x.Value.Last().IsExceptionCluster(ExceptionCluster.StartingException)).ToDictionary(x => x.Key, y => y.Value)));
        /// <summary>
        /// Exceptions which are inactive in the last analysis testrun
        /// </summary>
        public KeyValuePair<ExceptionCharacteristic, Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>> InActiveExceptionData
                => new(ExceptionCharacteristic.Union, myExceptionDataOnlyAsDictionary.Value.ToDictionary(x => x.Key, y => y.Value.Where(x => x.Value.Last().IsExceptionCluster(ExceptionCluster.EndingException) || x.Value.Last().IsExceptionCluster(ExceptionCluster.OutlierException)).ToDictionary(x => x.Key, y => y.Value)));

        /// <summary>
        /// Exceptions in a serializeable datastructure
        /// </summary>
        public KeyValuePair<ExceptionCharacteristic, Dictionary<string, List<KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>>> SerializeableExceptionData
                => new(myExceptionDataOnlyAsDictionary.Key, myExceptionDataOnlyAsDictionary.Value.ToDictionary(x => x.Key, z => z.Value.Select(v => v).OrderBy(o => o.Value.First().SourceOfActiveException.PerformedAt).ToList()));

        /// <summary>
        /// Exceptions as a Dictionary
        /// </summary>
        public KeyValuePair<ExceptionCharacteristic, Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>> DictOfExceptionData
                => myExceptionDataOnlyAsDictionary; 

    }
}

