//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using System.Collections.Generic;

namespace ETWAnalyzer.Extract.ThreadPool
{
    /// <inheritdoc/>
    public class ThreadPoolStats: IThreadPoolStats

    {
        /// <inheritdoc/>
        public Dictionary<ProcessKey, IList<ThreadPoolStarvationInfo>> PerProcessThreadPoolStarvations
        {
            get;
        } = new Dictionary<ProcessKey, IList<ThreadPoolStarvationInfo>>();

        /// <summary>
        /// Total number of thread pool events in the trace.
        /// </summary>
        public int? ThreadPoolEventCount { get; set; }

        IReadOnlyDictionary<ProcessKey, IList<ThreadPoolStarvationInfo>> IThreadPoolStats.PerProcessThreadPoolStarvations => PerProcessThreadPoolStarvations;
    }


}
