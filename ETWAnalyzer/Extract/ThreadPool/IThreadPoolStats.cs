//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using System.Collections.Generic;

namespace ETWAnalyzer.Extract.ThreadPool
{

    /// <summary>
    /// informations about the thread pool
    /// </summary>
    public interface IThreadPoolStats
    {
        /// <summary>
        /// Simple stat which contains the thread pool starvations per process
        /// </summary>
        IReadOnlyDictionary<ProcessKey, IList<ThreadPoolStarvationInfo>> PerProcessThreadPoolStarvations { get; }
    }
}
