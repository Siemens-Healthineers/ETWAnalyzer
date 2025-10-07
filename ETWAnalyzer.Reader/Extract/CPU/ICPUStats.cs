//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.CPU;
using ETWAnalyzer.Extract.CPU.Extended;
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
        /// Average process priority taken from CPU sampling data when present
        /// </summary>
        IReadOnlyDictionary<ETWProcessIndex, float> PerProcessAvgCPUPriority { get; }

        /// <summary>
        /// Contains methods which have CPU/Wait > 10ms (default) 
        /// </summary>
        ICPUPerProcessMethodList PerProcessMethodCostsInclusive { get; }

        /// <summary>
        /// When -timeline was used during extraction we generate CPU timeline data which can be used to 
        /// e.g. graph the data.
        /// </summary>
        ICPUTimeLine TimeLine { get; }

        /// <summary>
        /// Per core CPU Information
        /// </summary>
        public IReadOnlyDictionary<CPUNumber, ICPUTopology> Topology { get; }

        /// <summary>
        /// Get CPU Frequency related data
        /// </summary>
        public ICPUExtended ExtendedCPUMetrics { get; }
    }
}