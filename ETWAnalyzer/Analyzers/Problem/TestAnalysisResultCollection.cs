//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.JsonSerializing;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers
{
    /// <summary>
    /// Contains a collection of TestAnalysisResults
    /// </summary>
    class TestAnalysisResultCollection : IEnumerable<TestAnalysisResult>
    {
        Dictionary<TestRun, TestAnalysisResult> myRunResults =  new Dictionary<TestRun, TestAnalysisResult>();
        Dictionary<TestDataFile, TestAnalysisResult> myFileResults = new Dictionary<TestDataFile, TestAnalysisResult>();
        Dictionary<SingleTest, TestAnalysisResult> mySingleTestResults = new Dictionary<SingleTest, TestAnalysisResult>();

        /// <summary>
        /// List of analysis results for each test
        /// </summary>
        public HashSet<TestAnalysisResult> Results
        {
            get => myRunResults.Values.Concat(myFileResults.Values).Concat(mySingleTestResults.Values).ToHashSet();
        } 


        /// <summary>
        /// Add an issue for a test
        /// </summary>
        /// <param name="test"></param>
        /// <param name="issue"></param>
        public void AddIssue(SingleTest test, Issue issue)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }
            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            if (!mySingleTestResults.TryGetValue(test, out TestAnalysisResult existing))
            {
                existing = new TestAnalysisResult(test);
                mySingleTestResults[test] = existing;
            }

            existing.AddIssue(issue);
        }

        /// <summary>
        /// Add an issue to a TestRun
        /// </summary>
        /// <param name="run"></param>
        /// <param name="issue"></param>
        public void AddIssue(TestRun run, Issue issue)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }
            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }
            if (!myRunResults.TryGetValue(run, out TestAnalysisResult existing))
            {
                existing = new TestAnalysisResult(run);
                myRunResults[run] = existing;
            }

            existing.AddIssue(issue);
        }

        /// <summary>
        /// Add an issue to a TestdataFile
        /// </summary>
        /// <param name="file"></param>
        /// <param name="issue"></param>
        public void AddIssue(TestDataFile file, Issue issue)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            if (!myFileResults.TryGetValue(file, out TestAnalysisResult existing))
            {
                existing = new TestAnalysisResult(file);
                myFileResults[file] = existing;
            }

            existing.AddIssue(issue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<TestAnalysisResult> GetEnumerator()
        {
            return Results.GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Results: {Results.Count}";
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Results.GetEnumerator();
        }

    }

}
