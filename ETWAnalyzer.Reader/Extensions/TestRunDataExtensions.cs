//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extensions;
using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers.Infrastructure
{
    static class TestRunDataExtensions
    {

        public static TestRun[] RunsIncluding(this TestRunData runData, List<string> includeTests, List<string> includeMachines, int includeBetweenStartRunIndex, int includeToEndRunIndex)
        {
            return runData.RunsIncluding(includeTests, includeMachines, new KeyValuePair<DateTime, DateTime>(runData.Runs[includeBetweenStartRunIndex].TestRunStart, runData.Runs[includeToEndRunIndex].TestRunEnd));
        }
        public static TestRun[] RunsIncluding(this TestRunData runData, List<string> includeTests, List<string> includeMachines, KeyValuePair<DateTime, DateTime> includeBetween)
        {
            List<TestRun> filteredRuns = new List<TestRun>();
            foreach (var run in runData.Runs)
            {
                if(run.IsRunningBetween(includeBetween.Key,includeBetween.Value))
                {
                    TestRun filteredOrOriginal = HasAnyActiveExcludeUnselectedFilter(includeMachines, includeTests) ? ExcludeAllUnselected(includeTests, includeMachines, run) : run;
                    filteredRuns.Add(filteredOrOriginal);
                }
            }
            return filteredRuns.ToArray();
        }
        private static bool HasAnyActiveExcludeUnselectedFilter(List<string> includeTests, List<string> includeMachines) => includeTests.Count > 0 || includeMachines.Count > 0;
        private static TestRun ExcludeAllUnselected(List<string> includeTests, List<string> includeMachines,TestRun run)
        {
            List<SingleTest> specificSingleTests = run.Tests.Where(t => includeTests.Contains(t.Key)).SelectMany(x => x.Value).ToList();
            specificSingleTests = specificSingleTests   .SelectMany(x => x.Files)
                                                        .Where(testdatafile => includeMachines.Contains(testdatafile.MachineName))
                                                        .Select(x => new SingleTest(new TestDataFile[] { x }, run))
                                                        .ToList();
            return new TestRun(specificSingleTests, run.Parent, false);
        }
        public static TestRun[] RunsBetweenRunIndex(this TestRunData data, int includedStartRunIndex, int includedEndRunIndex)
        {
            //e.g. startindex = 4 endindex = 7
            // Run 4, 5, 6, 7 are included
            // Array size = 7 - 4 = 3 => 3+1 = 4
            TestRun[] runsBetween = new TestRun[includedEndRunIndex - includedStartRunIndex + 1];
            int index = 0;
            for (int i = includedStartRunIndex; i <= includedEndRunIndex; i++)
            {
                runsBetween[index++] = data.Runs[i];
            }
            return runsBetween;
        }
        public static TestRun[] RunsContainingTest(this TestRunData data, List<string> testsToAnalyze)
        {
            if (testsToAnalyze == null || testsToAnalyze.Count == 0) return data.Runs.ToArray();

            List<TestRun> containingTest = new List<TestRun>();
            foreach (var run in data.Runs)
            {
                List<SingleTest> specificSingleTests = run.Tests.Where(x => testsToAnalyze.Contains(x.Key)).SelectMany(x=>x.Value).ToList();
                if(specificSingleTests.Count > 0)
                {
                    containingTest.Add(new TestRun(specificSingleTests, data, false));
                }
            }
            return containingTest.ToArray();
        }

        public static List<TestDataFile> ToTestDataFiles(this List<SingleTest> convertThis)
        {
            return convertThis.SelectMany(x => x.Files).ToList();
        }
        public static List<TestDataFile> ToTestDataFiles(this List<TestRun>convertThis)
        {
            return convertThis.SelectMany(x => x.Tests.Values).SelectMany(singleTests => singleTests).SelectMany(s => s.Files).ToList();
        }
    }
}
