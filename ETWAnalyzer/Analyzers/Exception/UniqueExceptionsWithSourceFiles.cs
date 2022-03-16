//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer;
using ETWAnalyzer.Extensions;
using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer
{
    /// <summary>
    /// Relevant unique exception data assigned to the reduced source file data - contains only one test specification
    /// </summary>
    class UniqueExceptionsWithSourceFiles
    {
        /// <summary>
        /// Testspecific Property - only one specific test occurres
        /// Unique exceptions assigned to the reduced data sourcefile (no testdatafile.Extract access possible)
        /// </summary>
        public Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion> ExceptionsWithSources { get; private set; } = new Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion>();
        /// <summary>
        /// Generates unique exception assigned to sourcefile in a collection for the given test 
        /// </summary>
        /// <param name="singleTest">specific test</param>
        public UniqueExceptionsWithSourceFiles(SingleTest[] singleTest)
        {
            GenerateUniqueExceptionCollection(singleTest.SelectMany(x => x.Files).Where(x => x.FileName.EndsWith(".json")).ToList());
        }

        internal UniqueExceptionsWithSourceFiles(List<TestDataFile> testDataFiles)
        {
            GenerateUniqueExceptionCollection(testDataFiles);
        }
        /// <summary>
        /// Can be used for cloneing
        /// </summary>
        /// <param name="exceptionWithTestDataFile"></param>
        internal UniqueExceptionsWithSourceFiles(Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion> exceptionWithTestDataFile)
        {
            foreach (var element in exceptionWithTestDataFile)
            { 
                ExceptionsWithSources.Add(element.Key, element.Value);
            }
        }

        /// <summary>
        /// Can be used for cloning
        /// </summary>
        /// <param name="copyThis"></param>
        private UniqueExceptionsWithSourceFiles(UniqueExceptionsWithSourceFiles copyThis) : this(copyThis.ExceptionsWithSources) { }

        /// <summary>
        /// Generate the unique exception list assigned to the source files and removes extract data from source file
        /// Duplicate removing      : ~ 10x less exceptions
        /// Extract data removing   : ~ 1.6x less runtime and 40x less working storage capacity
        /// </summary>
        /// <param name="testdatafiles">takes relevant and deletes no more relevant data of this object - afterwards it is added as source file</param>
        void GenerateUniqueExceptionCollection(List<TestDataFile> testdatafiles)
        {
            foreach (var testDataFile in testdatafiles.OrderBy(x=>x.PerformedAt))
            {
                foreach (var e in testDataFile.Extract.Exceptions.Exceptions)
                {
                    var newkey = new ExceptionKeyEvent(e.Process.ProcessNamePretty,e.Stack, e.Message, e.Type, e.Time);
                    if (!ExceptionsWithSources.TryGetValue(newkey, out var value))
                    {
                        ExceptionsWithSources.Add(newkey, new ExceptionSourceFileWithNextNeighboursModuleVersion(testDataFile));
                    }
                    else
                    {
                        ExceptionsWithSources.FirstOrDefault(x => x.Key.Equals(newkey)).Key.Occurrence++;
                    }
                }
            }
        }

        /// <summary>
        /// Testspecific difference detection between this object and the given parameter
        /// </summary>
        /// <param name="currentRunInTimeSeries"></param>
        /// <param name="nextRunInTimeSeries">should be the next element in a time series</param>
        /// <returns>retruns the testspecific difference between this and the parameter</returns>
        public static UniqueExceptionsWithSourceFiles GetDifferencesTo(UniqueExceptionsWithSourceFiles currentRunInTimeSeries, UniqueExceptionsWithSourceFiles nextRunInTimeSeries)
        {
            UniqueExceptionsWithSourceFiles absDifference = currentRunInTimeSeries;
            AssumeThatAllExceptionsEndInNextRun(absDifference);

            foreach (var exceOfNextRun in nextRunInTimeSeries.ExceptionsWithSources)
            {
                if (IsStillActiveInNextRun(absDifference,exceOfNextRun.Key))
                {
                    absDifference.ExceptionsWithSources.Remove(exceOfNextRun.Key);
                }
                else
                {
                    AssumeThatExceptionStartsInNextRun(absDifference, exceOfNextRun);
                }
            }
            absDifference.ExceptionsWithSources = absDifference.ExceptionsWithSources.OrderBy(x => x.Value.SourceOfActiveException.PerformedAt).ToDictionary(x => x.Key, y => y.Value);
            return absDifference;
        }

        static void AssumeThatAllExceptionsEndInNextRun(UniqueExceptionsWithSourceFiles toSetAsEndingExceptionTrend)
        {
            toSetAsEndingExceptionTrend.ExceptionsWithSources.Values.ToList().ForEach(x => x.SetExceptionCluster(ExceptionCluster.EndingException));
        }
        static bool IsStillActiveInNextRun(UniqueExceptionsWithSourceFiles toEvaluate,ExceptionKeyEvent exceptionKeyOfNextRun)
        {
            return toEvaluate.ExceptionsWithSources.TryGetValue(exceptionKeyOfNextRun, out var value);
        }
        static void AssumeThatExceptionStartsInNextRun(UniqueExceptionsWithSourceFiles toEvaluate,KeyValuePair<ExceptionKeyEvent,ExceptionSourceFileWithNextNeighboursModuleVersion> nextRunsExceptionWithSource)
        {
            var source = nextRunsExceptionWithSource.Value;

            ExceptionSourceFileWithNextNeighboursModuleVersion deepCopyForModifiableCluster = new(source.SourceOfActiveException, source.CurrentAndNextNeighboursModuleVersion.GetDeepCopy());
            deepCopyForModifiableCluster.SetExceptionCluster(ExceptionCluster.StartingException);
            toEvaluate.ExceptionsWithSources.Add(nextRunsExceptionWithSource.Key, deepCopyForModifiableCluster);
        }
    }



}
