//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


namespace TAU.Toolkit.Diagnostics.Profiling.Simplified
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>API:yes
    /// Configures settings when profilng is stopped.
    /// </summary>
    [Serializable]
    public class ProfilingStopArgs
    {

        /// <summary>API:yes
        /// Initializes a new instance of the <see cref="ProfilingStopArgs"/> class for the use case, that the test Duration is provided later
        /// </summary>
        /// <param name="testCaseName">name of the testcase, only letters, digits and - are allowed for best readability in further tool chain try to be as short as possible </param>
        public ProfilingStopArgs(string testCaseName)
        {
            CheckForValidTestCaseName(testCaseName);
            TestCaseName = testCaseName;
            StopTime = DateTime.Now;
        }


        /// <summary>
        /// checks for valid test case name
        /// </summary>
        /// <param name="testCaseName">name of the testcase, only letters, digits and - are allowed for best readability in further tool chain try to be as short as possible </param>
        internal static void CheckForValidTestCaseName(string testCaseName)
        {
            if (!testCaseName.All(letter => Char.IsLetterOrDigit(letter) || letter == '-'))
            {
                throw new ArgumentException("only letters, digits and - are allowed as testcase name", testCaseName);
            }
        }
        /// <summary>
        /// this is set by the infrastrucute the be able to determine, if the etl is from client/server or a single machine
        /// we need this as unfortunatly not all hostnames following the same naming schema
        /// </summary>
        internal GeneratedAt GeneratedAt { set; get; }


        /// <summary>
        /// Test case name is used as prefix for the output file name. Keep it short or you will not be able
        /// to read in the WPA tab all relevant information like testcase name and duration
        /// </summary>
        public string TestCaseName
        {
            private set;
            get;
        }

        /// <summary>API:yes
        /// If set the profiler will throw if marker events were missing in the ETL file.
        /// </summary>
        public int ExpectedMarkers
        {
            get;
            set;
        }

        internal DateTime StopTime
        {
            get;set;
        }

        internal TestStatus TestStatus { get; set; }

    }
}
