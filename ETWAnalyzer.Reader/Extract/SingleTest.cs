//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// One Test can contain multiple files e.g. one from the RTC and from the APS
    /// </summary>
    public class SingleTest : IDisposable
    {
        /// <summary>
        /// Assume that stopping two trace runs wont need longer than 10 minutes on both machines.
        /// This is only a heuristic which should work well in most cases except the first run which might take much longer 
        /// when the pdbs are created the first time. 
        /// </summary>
        public static readonly TimeSpan MaxTimeBetweenTests = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Optional parent node when constructing data from a flat directory
        /// </summary>
        public TestRun Parent
        {
            get; 
            internal set;
        }

        /// <summary>
        /// Get profiling data files. Normally we have two, one for the client and one for the server
        /// </summary>
        public IReadOnlyList<TestDataFile> Files
        {
            get;
        }

        /// <summary>
        /// Get Backend File from a single test. If the file name encodes not the origin (data was created with SimplifiedProfiling API)
        /// we look into Configuration/CategorizedMachines.json file to get configured FE/BE machine names. If that fails as well
        /// you will get null as a result
        /// </summary>
        public TestDataFile Backend { get => Files.ToList().Find(x => x.GeneratedAt == TAU.Toolkit.Diagnostics.Profiling.Simplified.GeneratedAt.SRV); }

        /// <summary>
        /// Get Frontend File from a single test. If the file name encodes not the origin (data was created with SimplifiedProfiling API)
        /// we look into Configuration/CategorizedMachines.json file to get hard configured FE/BE machine names. If that fails as well
        /// you will get null as a result
        /// </summary>
        public TestDataFile Frontend { get => Files.ToList().Find(x => x.GeneratedAt == TAU.Toolkit.Diagnostics.Profiling.Simplified.GeneratedAt.CLT); }

        /// <summary>
        /// If file name is in right format this contains the test duration in ms
        /// </summary>
        public int DurationInMs
        {
            get
            {
                return Files[0].DurationInMs;
            }
        }

        /// <summary>
        /// Return time when test was executed
        /// </summary>
        public DateTime PerformedAt
        {
            get => Files[0].PerformedAt;
        }

        /// <summary>
        /// Test Name
        /// </summary>
        public string Name
        {
            get => Files[0].TestName;
        }

        /// <summary>
        /// Constructs from a list of TestDataFiles a test with an option TestRun parent node to allow later navigating between tests and testruns
        /// </summary>
        /// <param name="files">List of files for this test</param>
        /// <param name="parent">Parent TestRun node (optional)</param>
        public SingleTest(TestDataFile[] files, TestRun parent)
        {
            if (files == null || files.Length == 0)
            {
                throw new ArgumentException($"{nameof(files)} needs to be not null and contain at least one item");
            }

            foreach(var file in files)
            {
                file.ParentTest = this;
            }

            Parent = parent;
            Files = files.OrderByDescending(x => x.PerformedAt).ToArray();
            ThrowIfFilesHaveDifferentTestCaseName(files);

        }

        /// <summary>
        /// One Test 
        /// </summary>
        /// <param name="files"></param>
        public SingleTest(TestDataFile[] files):this(files, null)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="files"></param>
        public SingleTest(IEnumerable<TestDataFile> files):this(CheckInput(files))
        {
        }

        private static TestDataFile[] CheckInput(IEnumerable<TestDataFile> files)
        {
            if (files == null)
            {
                throw new ArgumentException($"{nameof(files)} needs to be not null and contain at least one item");
            }

            return files.ToArray();
        }



        private void ThrowIfFilesHaveDifferentTestCaseName(TestDataFile[] files)
        {
            // Checking every single Test 
            var first = files[0].TestName;
            for(int i=1;i<files.Length;i++)
            {

                if (files[i].TestName != first)
                {
                    throw new ArgumentException($"Test {files[i].FileName} is not equivalent to {first}");
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Name} {PerformedAt} {DurationInMs}ms Files: {Files?.Count}";
        }


        /// <summary>
        /// Do not throw away deserialized data
        /// </summary>
        internal bool KeepExtract { get; set; }

        /// <summary>
        /// Release Extracted data from memory to
        /// </summary>
        public void Dispose()
        {
            if( !KeepExtract)
            {
                foreach (var file in Files)
                {
                    file.Extract = null;
                }
            }
        }
    }
}
