//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.CPU
{
    /// <summary>
    /// Contains Nominal CPU Frequency and other CPU data
    /// </summary>
    public interface ICPUTopology
    {
        /// <summary>
        /// 100% CPU Frequency with no turbo boost
        /// </summary>
        
        int NominalFrequencyMHz { get; }

        /// <summary>
        /// This number is not based on actual measurements. It could be used as some sort criteria but not to judge CPU performance.
        /// </summary>
        int RelativePerformancePercentage { get; }

        /// <summary>
        /// EfficiencyClass starts at zero with most efficient but slowest CPUs. The highest number has the most performant (P) Cores.
        /// </summary>
        EfficiencyClass EfficiencyClass { get; }
    }

    /// <summary>
    /// Contains Nominal CPU Frequency and other CPU data
    /// </summary>
    public class CPUTopology : ICPUTopology
    {
        /// <summary>
        /// 100% CPU Frquency with no turbo boost
        /// </summary>
        public int NominalFrequencyMHz { get; set; }

        /// <summary>
        /// This number is not based on actual measurements. It could be used as some sort criteria but not to judge CPU performance.
        /// </summary>
        public int RelativePerformancePercentage { get; set; }

        /// <summary>
        /// EfficiencyClass starts at zero with most efficient but slowest CPUs. The highest number has the most performant (P) Cores.
        /// </summary>
        public EfficiencyClass EfficiencyClass { get; set; }
    }
}
