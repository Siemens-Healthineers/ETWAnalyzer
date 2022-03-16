//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers.Exception.ResultPrinter;
using ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer;
using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers.Exception.ExceptionDifferenceAnalyzer
{
    class ExceptionDifferencePersistentAnalyzer : ExceptionDifferenceVolatileAnalyzer
    {

        /// <summary>
        /// Defines a date that represents the expircy of an exception.
        /// If the first or last (depends on <see cref="IsIrrelevantMeasuredFromFirstExceptionOccFlag"/>) exception occurrence date exceeds the expircy date, the exception added to the irrelevant exception amount
        /// </summary>
        public DateTime ExceptionExpiryDateFlag { get; set; } = DateTime.Now - TimeSpan.FromDays(60);

        /// <summary>
        /// true: <see cref="ExceptionExpiryDateFlag"/> compared with the first detected exception date ever to define relevant and irrelevant exceptions.
        /// false: <see cref="ExceptionExpiryDateFlag"/> compared with the last active exception date ever to define relevant and irrelevant exceptions
        /// </summary>
        public bool IsIrrelevantMeasuredFromFirstExceptionOccFlag { get; set; }



        /// <summary>
        /// Enables de-/serialization of the detection in a JSON-format
        /// </summary>
        private DetectionSerializeUpdater CurrentSerializeUpdater { get; set; }

        /// <summary>
        /// Characterised (all characteristics) exceptions by considering present exceptionstates (~still active/ inactive exceptions)
        /// </summary>
        public Dictionary<ExceptionCharacteristic, TimeSeriesDetector> AllSerializeActivies =>
            TimeSeriesExceptionActivities.CreateCharacteristicDetection
            (
                (IsStillActiveExceptionDetectorFlag ?   CurrentSerializeUpdater.RelevantExceptionData.StillActiveExceptionData 
                                                        : CurrentSerializeUpdater.RelevantExceptionData.DictOfExceptionData
            ).Value);

        /// <summary>
        /// Characterised exceptions (only user-selected characteristics) by considering present exceptionstates (~still active/ inactive exceptions)
        /// </summary>
        public Dictionary<ExceptionCharacteristic, Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>> SelectedCharacteristicsExceptions
            => AllSerializeActivies.Where(x => SelectedCharacteristicsFlag.Contains(x.Key)).ToDictionary(k => k.Key, v => v.Value.ExceptionActivityCharacteristics);
        

        /// <summary>
        /// Relevant updated Exceptions in the user-selected timeseries by considering old already serialized detection results and current
        /// </summary>
        Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> UpdatedRelevantExceptionDataOfTimeSeriesSelection
        {
            get
            {
                DateTime selectionStart = TestRunsForAnalysis.First().TestRunStart;
                DateTime selectionEnd = TestRunsForAnalysis.Last().TestRunEnd;

                return  (IsStillActiveExceptionDetectorFlag ? 
                        ExceptionDataContainer.JoinExceptionData(SelectedCharacteristicsExceptions.Values).StillActiveExceptionData :
                        ExceptionDataContainer.JoinExceptionData(SelectedCharacteristicsExceptions
                        .Select(x => x.Value.ToDictionary(x => x.Key, y => y.Value  .Where(x => x.Value.Any(x =>x.IsInSelectedTimeSeriesPeriod(selectionStart,selectionEnd)))
                                                                                    .ToDictionary(x => x.Key, y => y.Value)))).DictOfExceptionData).Value;
            }
        }
        
        /// <summary>
        /// All relevant updated exceptions in the selected and already serialized timeseries by considering old already serialized detection results.
        /// Ignores whether sources exist in the user-selected timeseries-period
        /// </summary>
        Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> UpdatedRelevantExceptionDataOfTimeSeriesSerialize
        =>  (IsStillActiveExceptionDetectorFlag ? 
            ExceptionDataContainer.JoinExceptionData(SelectedCharacteristicsExceptions.Values).StillActiveExceptionData :
            ExceptionDataContainer.JoinExceptionData(SelectedCharacteristicsExceptions.Select(x => x.Value.ToDictionary(x => x.Key,y => y.Value)
                                                                                                          .ToDictionary(x => x.Key, y => y.Value))).DictOfExceptionData)
                                                                                                          .Value;
        
        /// <summary>
        /// Exceptions classified as irrelevant by their state persistent
        /// </summary>
        public Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> UpdatedIrrelevantExceptionData
            => CurrentSerializeUpdater.IrrelevantExceptionData.DictOfExceptionData.Value;

        /// <summary>
        /// Entry to update detection results
        /// Merges already analyzed detections with the current and classifies exceptions a relevant or irrelevant
        /// Finally the function serializes the classified exceptions
        /// </summary>
        public override void DetectAnalyzerSpecificActivities()
        {
            ExceptionDataContainer joinedCurrentRelevantSerializeableExceptionCharacteristics = ExceptionDataContainer.JoinExceptionData(AllCharacterizedExceptionOfRelevantProcesses.Values);

            UpdateSerializationByMergingCurrentWithOldSerializedResults(joinedCurrentRelevantSerializeableExceptionCharacteristics);

            myExceptionsForConsoleOutput = UpdatedRelevantExceptionDataOfTimeSeriesSelection;
        }
        /// <summary>
        /// Creates or updates a relevant and/or irrelevant exception-serialization as json
        /// </summary>
        /// <param name="joinedCurrentRelevantSerializeableExceptionCharacteristics"></param>
        private void UpdateSerializationByMergingCurrentWithOldSerializedResults(ExceptionDataContainer joinedCurrentRelevantSerializeableExceptionCharacteristics)
        {
            CurrentSerializeUpdater = new DetectionSerializeUpdater(this,joinedCurrentRelevantSerializeableExceptionCharacteristics);
            CurrentSerializeUpdater.Update();
        }

        /// <summary>
        /// prints results
        /// </summary>
        public override void Print()
        {
            base.Print();
        }
        protected override void ConfigureDetailPrinter(ExceptionAnalysisDetailPrinter detailPrinter)
        {
            detailPrinter.SetFormattedExceptionSourceDescription(GetFormatedExceptionClusterDescriptionString);
        }
        protected override void PrintAnalyzerSpecificOutputEnding()
        {
            PrintTimeSeriesActivitiesSummary();
            PrintAnalysisTimes();
        }

        /// <summary>
        /// Prints the overview for the persistent analyzer
        /// </summary>
        private void PrintTimeSeriesActivitiesSummary()
        {
            Console.WriteLine($"\n\n{this.GetType().Name} - Time Series Activies - Overview:");
            foreach (var characteristic in ExceptionActivities.ExceptionCharacteristicDetector)
            {
                bool isSelected = SelectedCharacteristicsFlag.Contains(characteristic.Key);

                var allTestsWithExceptionsCollection = characteristic.Value.ExceptionActivityCharacteristics;

                ExceptionCharacteristic currChar = characteristic.Key;
                var relevantTestsWithExceptionsCollection = AllCharacterizedExceptionOfRelevantProcesses[currChar];

                var allSerializedTestsWithExceptionsCollection = AllSerializeActivies[currChar].ExceptionActivityCharacteristics;

                string line = $"\n{currChar} current volatile total Exceptioncount: { allTestsWithExceptionsCollection.SelectMany(x => x.Value).Count()} / from relevant Processes: { relevantTestsWithExceptionsCollection.SelectMany(x => x.Value).Count()}";

                line = string.Concat(line, $" / relevant persistent serialize total Exceptioncount: {allSerializedTestsWithExceptionsCollection.SelectMany(x => x.Value).Count() }");
                line = string.Concat(line.PadRight(170, ' '), $"{(isSelected ? "\t\t(selected)" : "\t\t(unselected)")}");
                ColorConsole.WriteLine(line, isSelected ? ConsoleColor.White : ConsoleColor.Gray);

                foreach (var testWithExceptions in allTestsWithExceptionsCollection)
                {
                    string currTestName = testWithExceptions.Key;
                    int allSerializedExceptionCount = allSerializedTestsWithExceptionsCollection.ContainsKey(currTestName) ? allSerializedTestsWithExceptionsCollection[currTestName].Count : 0;

                    line = $"\t\t{currTestName}: {testWithExceptions.Value.Count} (total) / {relevantTestsWithExceptionsCollection[currTestName].Count} (relevant Processes)";
                    line = string.Concat(line, $" / relevant persistent serialize total Exceptioncount: {allSerializedExceptionCount}");
                    Console.WriteLine(line);
                }
            }
        }

        public override void TakePersistentFlagsFrom(AnalyzeCommand analyzeCommand)
        {
            base.TakePersistentFlagsFrom(analyzeCommand);

            ExceptionExpiryDateFlag = analyzeCommand.ExceptionExpircyDate;
            IsIrrelevantMeasuredFromFirstExceptionOccFlag = analyzeCommand.IsIrrelevantMeasuredFromFirstExceptionOcc;
            IsStillActiveExceptionDetectorFlag = analyzeCommand.IsStillActiveExceptionDetector;

        }
    }
}
