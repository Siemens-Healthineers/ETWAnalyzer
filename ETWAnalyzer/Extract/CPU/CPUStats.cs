//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System.Collections.Generic;
using System.IO;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Part of ETWExtract which contains per process CPU consumption metrics
    /// </summary>
    public class CPUStats : ICPUStats
    {
        /// <summary>
        /// Simple stat which contains the total CPU in ms per process
        /// </summary>
        public Dictionary<ProcessKey, uint> PerProcessCPUConsumptionInMs
        {
            get;
        } = new Dictionary<ProcessKey, uint>();

        /// <summary>
        /// Simple stat which contains the total CPU in ms per process
        /// </summary>
        IReadOnlyDictionary<ProcessKey, uint> ICPUStats.PerProcessCPUConsumptionInMs => PerProcessCPUConsumptionInMs;


        /// <summary>
        /// Lists all methods which have > 10ms (default) 
        /// </summary>
        public CPUPerProcessMethodList PerProcessMethodCostsInclusive
        {
            get;
        } = new CPUPerProcessMethodList();

        /// <summary>
        /// Lists all methods which have > 10ms (default) 
        /// </summary>
        ICPUPerProcessMethodList ICPUStats.PerProcessMethodCostsInclusive => PerProcessMethodCostsInclusive;

        /// <summary>
        /// Ctor which fills the data. This is also used by Json.NET during deserialization.
        /// </summary>
        /// <param name="perProcessCPUConsumptionInMs"></param>
        /// <param name="perProcessMethodCostsInclusive"></param>
        public CPUStats(Dictionary<ProcessKey, uint> perProcessCPUConsumptionInMs, CPUPerProcessMethodList perProcessMethodCostsInclusive)
        {
            PerProcessCPUConsumptionInMs = perProcessCPUConsumptionInMs;
            PerProcessMethodCostsInclusive = perProcessMethodCostsInclusive;
        }
    }
}
