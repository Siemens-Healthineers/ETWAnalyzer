//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System.Collections.Generic;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Part of ETWExtract which contains per process CPU consumption metrics
    /// </summary>
    public interface ICPUStats
    {
        /// <summary>
        /// Simple stat which contains the total CPU in ms per process
        /// </summary>
        IReadOnlyDictionary<ProcessKey, uint> PerProcessCPUConsumptionInMs { get; }

        /// <summary>
        /// Lists all methods which have > 10ms (default) 
        /// </summary>
        ICPUPerProcessMethodList PerProcessMethodCostsInclusive { get; }
    }
}