//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers.Infrastructure;
using ETWAnalyzer.Commands;
using ETWAnalyzer.EventDump;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// This the main entry point to query profiling test data from a flat directory into an object structure which allows
    /// later higher level queries and dependencies.
    /// The entry point can be 
    ///  a) A directory which contains compressed and or uncompressed ETL files
    ///  b) A directory which contains extracted Json files from a previous extract operation
    ///  c) A single ETL/Zip/7z file
    ///  d) A single Json file representing from an extract operation
    /// This supports later analysis operations which try to detect anomalies between test runs in time series data
    /// </summary>
    public class TestRunData
    {
        /// <summary>
        /// Get all Test Runs
        /// </summary>
        public IReadOnlyList<TestRun> Runs { get; private set; }

        /// <summary>
        /// Get all TestDataFiles from all TestRuns as flat list (unsorted)
        /// </summary>
        public IReadOnlyList<TestDataFile> AllFiles
        {
            get
            {
                return TestRun.ExistingSingleTestsIncludeComputerAndTestNameAndDateFilter(null, null, new KeyValuePair<DateTime, DateTime>(DateTime.MinValue, DateTime.MaxValue), Runs.ToArray()).ToTestDataFiles();
            }
        }

        /// <summary>
        /// Set during extraction to aid deserialization of extracted Json files
        /// </summary>
        internal OutDir OutputDirectory
        {
            get;
        } = new OutDir();

        /// <summary>
        /// Construct a TestRunData out of a collection of TestRuns
        /// </summary>
        /// <param name="runs"></param>
        internal TestRunData(TestRun[] runs):this(runs, null)
        {
        }

        /// <summary>
        /// Construct a TestRunData out of a collection of TestRuns
        /// </summary>
        /// <param name="runs"></param>
        /// <param name="outputDirectory">Set the Input </param>
        internal TestRunData(TestRun[] runs, OutDir outputDirectory)
        {
            Runs = runs;
            foreach (var run in Runs)
            {
                run.Parent = this;
            }
            OutputDirectory = outputDirectory;
        }


        /// <summary>
        /// Create from a directory which contains compressed or extracted profiling data or from a directory with extracted Json data
        /// the corresponding TestRunData structure.
        /// A single file is also possible to specify.
        /// </summary>
        /// <param name="inputFileOrDirectory">Directory which contains compressed and or extracted data</param>
        public TestRunData(string inputFileOrDirectory):this(inputFileOrDirectory, SearchOption.TopDirectoryOnly, inputFileOrDirectory)
        {
        }

        /// <summary>
        /// Create from a directory which contains compressed or extracted profiling data or from a directory with extracted Json data
        /// the corresponding TestRunData structure.
        /// A single file is also possible to specify.
        /// </summary>
        /// <param name="inputFileOrDirectory">A directory which contains ETL/Zip/Json files or an ETL/Zip/Json filename</param>
        /// <param name="searchOption">If AllDirectories is specified we search recursively for test runs</param>
        public TestRunData(string inputFileOrDirectory, SearchOption searchOption) : this(inputFileOrDirectory, searchOption, inputFileOrDirectory)
        {
        }

        /// <summary>
        /// Create from a directory which contains compressed or extracted profiling data or from a directory with extracted Json data
        /// the corresponding TestRunData structure.
        /// A single file is also possible to specify.
        /// </summary>
        /// <param name="inputFileOrDirectory">A directory which contains ETL/Zip/Json files or an ETL/Zip/Json filename</param>
        /// <param name="searchOption">If AllDirectories is specified we search recursively for test runs</param>
        /// <param name="outputDirectory">Output directory where extracted Json files and ETL files from previous runs can already exist</param>
        public TestRunData(string inputFileOrDirectory, SearchOption searchOption, string outputDirectory):this(new List<string> { inputFileOrDirectory }, searchOption, outputDirectory)
        {
        }

        /// <summary>
        /// Create from a list of directory queries which contains compressed or extracted profiling data or from a directory with extracted Json data
        /// the corresponding TestRunData structure.
        /// </summary>
        /// <param name="inputFileOrDirectories">A list of directory/file queries which contain ETL/Zip/Json files or an ETL/Zip/Json filename</param>
        /// <param name="searchOption">If AllDirectories is specified we search recursively for test runs</param>
        /// <param name="outputDirectory">Output directory where extracted Json files and ETL files from previous runs can already exist</param>
        public TestRunData(List<string> inputFileOrDirectories, SearchOption searchOption, string outputDirectory)
        {
            if(inputFileOrDirectories == null || inputFileOrDirectories.Count == 0)
            {
                throw new ArgumentException($"{nameof(inputFileOrDirectories)} was null or empty");
            }

            List<TestRun> runs = new List<TestRun>();
            foreach (var query in inputFileOrDirectories)
            {
                TestRun[] singleRun = TestRun.CreateFromDirectory(query, searchOption, this);
                runs.AddRange(singleRun);
            }

            Runs = runs.ToArray();
            OutputDirectory.OutputDirectory = outputDirectory;
        }
    }
}
