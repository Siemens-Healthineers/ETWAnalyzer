//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using ETWAnalyzer.Analyzers;
using System.IO;
using System.ComponentModel;
using ETWAnalyzer.Commands;

namespace ETWAnalyzer
{
    /// <summary>
    /// Analyzers can look at 
    /// a) A TestRun
    /// b) A Frontend/Backend file of a single test
    /// c) A Test Package which contains previous tests of previous test runs
    /// Depending on the needs of the analyzer different methods need to be overloaded
    /// </summary>
    abstract class AnalyzerBase
    {
        /// <summary>
        /// Includes all the issues of the AnalyzeTestsByTime function
        /// The modification changes the source issues by reference
        /// </summary>
        public TestAnalysisResultCollection TestAnalysisResults { get; set; } = new();

        /// <summary>
        /// These includes all data to analyze.
        /// The access to the complete and unfiltered data is possible by using the Parent property of a TestRun
        /// </summary>
        public List<TestRun> TestRunsForAnalysis { get; private set; } = new();
        /// <summary>
        /// If the Analyzer does an analysis over all given singletests the data stored here
        /// </summary>
        /// 
        /// <summary>
        /// Set true when the last Object is transferred
        /// Can be used to wait with the analysis till all data collected
        /// </summary>
        public bool IsLastElement { get; set; }

        /// <summary>
        /// When set Analyzer produces more detailed output
        /// </summary>
        public bool VerboseOutput { get; internal set; }

        /// <summary>
        /// Iterate over all test by time in ascending order and call this analyzer back for reach SingleTest which matches the preconditions set by NeedsBackend/Frontend.
        /// </summary>
        /// <param name="issues">When issues are identified they can be added here</param>
        /// <param name="backend">Frontend Json file</param>
        /// <param name="frontend">Backend Json file</param>
        /// true:   analysis the given data by every call
        /// false:  stores all data till startAnalyzing is true
        public abstract void AnalyzeTestsByTime(TestAnalysisResultCollection issues, TestDataFile backend, TestDataFile frontend);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="issues"></param>
        /// <param name="run"></param>
        public abstract void AnalyzeTestRun(TestAnalysisResultCollection issues, TestRun run);

        /// <summary>
        /// Prints analysis results
        /// </summary>
        public abstract void Print();

        public virtual void TakePersistentFlagsFrom(AnalyzeCommand analyzeCommand)
        {
            if (analyzeCommand == null) throw new ArgumentNullException($"Parameter {analyzeCommand} can not be null");

            VerboseOutput = analyzeCommand.VerboseOutput;
        }
        
        /// <summary>
        /// Format a DateTimeOffset to seconds with 3 decimal places like 1.123
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        protected string FormatTimeToS(DateTimeOffset time)
        {
            return time.ToString("ss.fff", CultureInfo.InvariantCulture);
        }
    }
}
