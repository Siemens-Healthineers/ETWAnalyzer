//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer_uTest
{
    class TestRunCreator
    {
        static public TestRun FromDurationN(List<TestCase> testNames, DateTime start, int[] durations,out List<TestDataFile> allTestDataFilesOfRun, out DateTime end,TestRunData testrundata = null)
        {
            allTestDataFilesOfRun = new List<TestDataFile>();
            List<SingleTest> singleTests = new List<SingleTest>();
            List<TestDataFile> currentGroup = new List<TestDataFile>();

            foreach (var testName in testNames)
            {
                
                for (int i = 0; i < durations.Length; i++)
                {
                    string fullFileName = currentGroup.Count == 0 ? Path.Combine("dummyPath", string.Concat(testName.TestCaseName,"_",durations[i],"msDummyMachineSRV",start.Year,start.Month,start.Day,"-",start.Hour,start.Minute,start.Second,".7z"))
                                        : Path.Combine("dummyPath", string.Concat(testName.TestCaseName,"_",durations[i] + "msDummyMachinePC", start.Year, start.Month, start.Day, "-", start.Hour, start.Minute, start.Second, ".7z"));

                    TestDataFile file = new TestDataFile(testName.TestCaseName, fullFileName, start, durations[i], 0, "DummyMachine", null,true,currentGroup.Count == 0 ? TAU.Toolkit.Diagnostics.Profiling.Simplified.GeneratedAt.SRV:TAU.Toolkit.Diagnostics.Profiling.Simplified.GeneratedAt.CLT)
                    { JsonExtractFileWhenPresent = Path.ChangeExtension(fullFileName,".json") };

                    allTestDataFilesOfRun.Add(file);

                    currentGroup.Add(file);
                    if (currentGroup.Count == 2)
                    {
                        singleTests.Add(new SingleTest(currentGroup.ToArray()));
                        currentGroup.Clear();
                        start += TimeSpan.FromMilliseconds(durations[i] + 1000);

                    }
                }
                if(currentGroup.Count>0)
                {
                    singleTests.Add(new SingleTest(currentGroup.ToArray()));
                    currentGroup.Clear();
                }
            }
            TestRun lret = new TestRun(singleTests,testrundata);
            end = start;
            return lret;
        }


        /// <summary>
        /// To generate synthetic TestRuns defined by the programmer.
        /// Generates every Test which is defined in TestRunConfiguration
        /// Simulate No-Missing-SingleTests : Set numberOfValidSingleTests == numberOfNotVaildSingleTests
        /// Simulate Missing-SingleTests    : Set numberOfValidSingleTests >  numberOfNotVaildSingleTests by defining the the index in the TestRun by missingTestFirst, missingTestSecond and missingTestThird
        /// </summary>
        /// <param name="tmp">Temp-Output-Folder</param>
        /// <param name="jsonIndexFile">Generated file includes all synthetic runs</param>
        /// <param name="numberOfValidSingleTests">Number of the valid tests in the each run</param>
        /// <param name="numberOfNotValidSingleTests">Number of not valid tests in Run. The number of the tests exclude the missing tests.</param>
        /// <param name="numberOfRunsToCreate">The number of runs to create.</param>
        /// <param name="missingTestIndexWithTestCount">Missing test at run-index - Count of the Test</param>
        /// <returns>The deserialize of TestDataIndexFile.json</returns>
        static public TestRunData CreateSynteticRun(ITempOutput tmp, List<KeyValuePair<int, int>> missingTestIndexWithTestCount, int numberOfValidSingleTests = 10, int numberOfRunsToCreate = 20)
        {
            if (missingTestIndexWithTestCount?.Count > 0)
            {
                if (missingTestIndexWithTestCount.Max(x => x.Value) > numberOfValidSingleTests || missingTestIndexWithTestCount.All(x => x.Key > numberOfRunsToCreate))
                {
                    throw new ArgumentException("Cannot use these parameter to generate a synthetic run.");
                }
            }
            
            List<TestRun> syntheticTestRuns = new List<TestRun>();
            TestRunConfiguration testRunConfiguration = new TestRunConfiguration();

            int[] durationValid = new int[numberOfValidSingleTests * 2];
            int milliSec = 1000;
            for (int i = 0; i < durationValid.Length - 1; i += 2)
            {
                durationValid[i] = milliSec;
                durationValid[i + 1] = milliSec;
                milliSec += 1000;
            }

            DateTime startDate = new DateTime(2000, 1, 1);
            DateTime endOfRun = new DateTime();
            for (int i = 0; i < numberOfRunsToCreate; i++)
            {
                int count = 0;
                if (missingTestIndexWithTestCount?.Count > 0)
                {
                    count = missingTestIndexWithTestCount.Find(x => x.Key == i).Value;
                }

                milliSec = 1000;
                int[] durationNotValid = new int[count * 2];
                for (int a = 0; a < durationNotValid.Length - 1; a += 2)
                {
                    durationNotValid[a] = milliSec;
                    durationNotValid[a + 1] = milliSec;
                    milliSec += 1000;
                }
                List<TestDataFile> allTestDataFilesOfRun = new List<TestDataFile>();

                if (missingTestIndexWithTestCount != null && missingTestIndexWithTestCount.Exists(x => x.Key == i))
                {
                    syntheticTestRuns.Add(FromDurationN(testRunConfiguration.ExpectedRun.TestCases, startDate, durationNotValid, out allTestDataFilesOfRun, out endOfRun));
                }
                else
                {
                    syntheticTestRuns.Add(FromDurationN(testRunConfiguration.ExpectedRun.TestCases, startDate, durationValid, out allTestDataFilesOfRun, out endOfRun));
                }

                startDate = endOfRun + TimeSpan.FromHours(2);
            }

            //=====================TestRun - Creating completed=======================

            string jsonPath = Path.Combine(tmp.Name, Program.ExtractFolder);
            Directory.CreateDirectory(jsonPath);

            return new TestRunData(syntheticTestRuns.ToArray(), new OutDir { OutputDirectory = tmp.Name });
        }
        static public List<SingleTest> GetSingleTests(TestRunData rundata)
        {
            List<SingleTest> temp = new List<SingleTest>();
            foreach (var run in rundata.Runs)
            {
                foreach (var singleTests in run.Tests.Values)
                {
                    temp.AddRange(singleTests);
                }
            }
            return temp;
        }

    }
}
