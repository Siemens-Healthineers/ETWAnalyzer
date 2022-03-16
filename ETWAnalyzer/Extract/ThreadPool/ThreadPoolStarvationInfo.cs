//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Extract.ThreadPool
{
    /// <summary>
    /// information about a dedicated thread pool starvation
    /// </summary>
    public class ThreadPoolStarvationInfo
    {
        /// <summary>
        /// number of new worker threads in the thread pool after the starvation was detected
        /// </summary>
        public uint NewWorkerThreadCount { set; get; }

        /// <summary>
        /// date time when the starvation was detected from the CLR
        /// </summary>
        public DateTimeOffset DateTime { set; get; }

        /// <summary>
        /// total seconds from start of this trace when the starvation was detected from the CLR
        /// </summary>
        public decimal TotalSeconds { set; get; }

    }
}