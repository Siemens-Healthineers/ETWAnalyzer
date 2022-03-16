//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System.Collections.Generic;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Read only part of CPU consumption metrics
    /// </summary>
    public interface ICPUPerProcessMethodList
    {
        /// <summary>
        /// If true no CPU threshold of 10ms was applied during extraction
        /// </summary>
        bool ContainsAllCPUData { get; }

        /// <summary>
        /// List of all methods found in ETL file
        /// </summary>
        IReadOnlyList<string> MethodNames { get; }

        /// <summary>
        /// Store per process per method the CPU in ms
        /// Index based structure to support compact serialization
        /// </summary>
        IReadOnlyList<MethodsByProcess> MethodStatsPerProcess { get; }
    }
}