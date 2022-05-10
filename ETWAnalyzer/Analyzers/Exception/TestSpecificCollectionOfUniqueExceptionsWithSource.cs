//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer
{
    /// <summary>
    /// Collection of multiple tests assigned to the source files with exceptions - structured by the testspecification
    /// Could be used as relevant data for a testrun or difference between two testruns or difference of two differences...
    /// </summary>
    class TestSpecificCollectionOfUniqueExceptionsWithSource
    {        
        /// <summary>
        /// Contains testspecific duplicate excluded exceptions assigned to the sourcefiles (TestDataFile)
        /// </summary>
        public Dictionary<string, UniqueExceptionsWithSourceFiles> TestSpecificExceptionsWithSourceFile { get; private set; } = new Dictionary<string, UniqueExceptionsWithSourceFiles>();

        private TestSpecificCollectionOfUniqueExceptionsWithSource() { }
        /// <summary>
        /// Generates a testpecific exception-duplicate excluded collection with sourcefiles  
        /// </summary>
        /// <param name="testRun">Instance based on</param>
        public TestSpecificCollectionOfUniqueExceptionsWithSource(TestRun testRun)
        {
            foreach (var test in testRun.Tests)
            {
                TestSpecificExceptionsWithSourceFile.Add(test.Key,new UniqueExceptionsWithSourceFiles(test.Value));
            }
        }
        /// <summary>
        /// Detects all exception based differences between this object and object given as a parameter
        /// </summary>
        /// <param name="currRunInTimeSeries"></param>
        /// <param name="nextRunInTimeSeries">should be the next following object over a time series</param>
        /// <returns>Exception based differences</returns>
        public static TestSpecificCollectionOfUniqueExceptionsWithSource GetDifferencesTo(TestSpecificCollectionOfUniqueExceptionsWithSource currRunInTimeSeries,TestSpecificCollectionOfUniqueExceptionsWithSource nextRunInTimeSeries)
        {
            var testRunConfiguration = new TestRunConfiguration();
            TestSpecificCollectionOfUniqueExceptionsWithSource tempTestWithExceptionCollection = new();

            foreach (var test in testRunConfiguration.ExpectedRun.TestCases)
            {
                TestSpecificCollectionOfUniqueExceptionsWithSource runWithMissingTestCase = null;
                UniqueExceptionsWithSourceFiles nextTestsWithException = null;

                if (currRunInTimeSeries.TestSpecificExceptionsWithSourceFile.TryGetValue(test.TestCaseName, out UniqueExceptionsWithSourceFiles currTestsWithException))
                {
                    if(nextRunInTimeSeries.TestSpecificExceptionsWithSourceFile.TryGetValue(test.TestCaseName, out nextTestsWithException))
                    {
                        tempTestWithExceptionCollection.TestSpecificExceptionsWithSourceFile.Add(test.TestCaseName, UniqueExceptionsWithSourceFiles.GetDifferencesTo(currTestsWithException, nextTestsWithException));
                    }
                    else
                    {
                        runWithMissingTestCase = nextRunInTimeSeries;
                    }
                }
                else
                {
                    runWithMissingTestCase = currRunInTimeSeries;
                }
                if(currTestsWithException != nextTestsWithException && runWithMissingTestCase != null)
                {
                    TestRun temp = runWithMissingTestCase.TestSpecificExceptionsWithSourceFile.Values.First().ExceptionsWithSources.Values.First().SourceOfActiveException.ParentTest.Parent;
                    Console.WriteLine($"Missing Testcase { test.TestCaseName} in Testrun from {temp.TestRunStart} to {temp.TestRunEnd}");
                }
            }
            return tempTestWithExceptionCollection;
        }
    }
}
