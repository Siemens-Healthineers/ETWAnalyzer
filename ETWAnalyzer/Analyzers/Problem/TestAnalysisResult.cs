//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers
{
    /// <summary>
    /// Analysis Result for one Testcase
    /// </summary>
    class TestAnalysisResult
    {
        /// <summary>
        /// TestCase Name
        /// </summary>
        public string TestCase
        {
            get;
        }

        /// <summary>
        /// Date when it was performed
        /// </summary>
        public DateTime PerformedAt
        {
            get;
        }

        /// <summary>
        /// Path to Backend Json file which was used
        /// </summary>
        public string BackendJson
        {
            get;
        }

        /// <summary>
        /// Path to Frontend Json which was used
        /// </summary>
        public string FrontendJson
        {
            get;
        }

        /// <summary>
        /// Test Duration in ms
        /// </summary>
        public int DurationMs
        {
            get;
        }


        /// <summary>
        /// List of found issues
        /// </summary>
        public IReadOnlyList<Issue> Issues
        {
            get;
        } = new List<Issue>();


        /// <summary>
        /// Add another issue
        /// </summary>
        /// <param name="issue"></param>
        public void AddIssue(Issue issue)
        {
            if( issue == null )
            {
                throw new ArgumentNullException(nameof(issue));
            }
            ((List<Issue>)Issues).Add(issue);
            issue.Parent = this;
        }

        /// <summary>
        /// Create a new result for a given testcase which can have 0 or n issues.
        /// </summary>
        /// <param name="backendDataFile"></param>
        /// <param name="frontendDataFile"></param>
        public TestAnalysisResult(TestDataFile backendDataFile, TestDataFile frontendDataFile)
        {
            if( backendDataFile ==null && frontendDataFile == null )
            {
                throw new ArgumentNullException($"{nameof(backendDataFile)} and {nameof(frontendDataFile)} are null! At least one of them must not be null!");
            }

#pragma warning disable CA1062 // Validate arguments of public methods
            TestCase = (backendDataFile ?? frontendDataFile).TestName;
#pragma warning restore CA1062 // Validate arguments of public methods
            PerformedAt = (backendDataFile ?? frontendDataFile).PerformedAt;
            DurationMs = (backendDataFile ?? frontendDataFile).DurationInMs;
            BackendJson =  backendDataFile?.JsonExtractFileWhenPresent;
            FrontendJson = frontendDataFile?.JsonExtractFileWhenPresent;
        }

#pragma warning disable CS1591 // Fehledes XML-Kommentar für öffentlich sichtbaren Typ oder Element
        public TestAnalysisResult(TestDataFile dataFile)
#pragma warning restore CS1591 // Fehledes XML-Kommentar für öffentlich sichtbaren Typ oder Element
        {
            if(dataFile == null )
            {
                throw new ArgumentException($"BackendDataFile and FrontendDataFile are null! At least one of them must not be null!");
            }
            TestCase = dataFile.TestName;
            PerformedAt = dataFile.PerformedAt;
            DurationMs = dataFile.DurationInMs;
            BackendJson = dataFile.FileName.Contains("SRV") ? dataFile.JsonExtractFileWhenPresent : null;
            FrontendJson = dataFile.FileName.Contains("PC") ? dataFile.JsonExtractFileWhenPresent : null;
        }

        /// <summary>
        /// Create a AnalysisResult from a single test 
        /// </summary>
        /// <param name="test"></param>
        public TestAnalysisResult(SingleTest test)
        {
            if( test == null )
            {
                throw new ArgumentNullException(nameof(test));
            }

            this.DurationMs = test.DurationInMs;
            this.PerformedAt = test.PerformedAt;
            this.TestCase = test.Name;
            BackendJson = test.Backend?.JsonExtractFileWhenPresent;
            FrontendJson = test.Frontend?.JsonExtractFileWhenPresent;
        }


        /// <summary>
        /// Create a result from a testrun
        /// </summary>
        /// <param name="run"></param>
        public TestAnalysisResult(TestRun run)
        {
            if( run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            this.PerformedAt = run.TestRunStart;
            this.FrontendJson = run.AllTestFilesSortedAscendingByTime.FirstOrDefault()?.JsonExtractFileWhenPresent;
            this.TestCase = $"Testrun starting at {run.TestRunStart} - {run.TestRunEnd}, Duration {run.TestRunDuration}, Tests: {run.GetTotalNumberOfTestDataFilesInTestRun()}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string issues = String.Join(Environment.NewLine, Issues);

            return $"Test {TestCase} from {PerformedAt} {DurationMs} ms {issues}";
        }
    }

}
