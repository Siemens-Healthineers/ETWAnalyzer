//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

namespace ETWAnalyzer.Extract.Power
{

    /// <summary>
    /// Defines the boost mode for processors to achieve additional performance when
    /// they are at high system loads.
    /// </summary>
    public enum ProcessorPerformanceBoostMode
    {
        /// <summary>
        /// The Corresponding P-state-based behaviour is Disabled. Collaborative Processor
        /// Performance Control (CPPC) behaviour is Disabled. 
        /// 
        /// Please see Power and performance tuning for more information.
        /// </summary>
        Disabled,

        /// <summary>
        /// The corresponding P-state-based behaviour is Enabled. Collaborative Processor
        /// Performance Control (CPPC) behaviour is Efficient Enabled.
        /// 
        /// Please see Power and performance tuning for more information.
        /// </summary>
        Enabled,

        /// <summary>
        /// The corresponding P-state-based behaviour is Enabled. Collaborative Processor
        /// Performance Control (CPPC) behaviour is Aggressive.
        /// 
        /// Please see Power and performance tuning for more information.
        /// </summary>
        Aggressive,

        /// <summary>
        /// The corresponding P-state-based behaviour is Efficient. Collaborative Processor
        /// Performance Control (CPPC) behaviour is Efficient Enabled.
        /// 
        /// Please see Power and performance tuning for more information.
        /// </summary>
        EfficientEnabled,

        /// <summary>
        /// The corresponding P-state-based behaviour is Efficient. Collaborative Processor
        /// Performance Control (CPPC) behaviour is Aggressive.
        /// 
        /// Please see Power and performance tuning for more information.
        /// </summary>
        EfficientAggressive,
 
        /// <summary>
        /// Windows calculates the desired extra performance above the guaranteed performance
        /// level, and asks the processor to deliver that specific performance level.
        /// 
        /// Intel processors can guarantee that the CPU runs at certain frequency. Usually,
        /// guaranteed frequency is the same as the nominal one, but in some cases such as
        /// in the presence of thermal limits, the guaranteed one will drop below the nominal.
        /// The amount of boost is a bias that is added on top of a certain threshold. Normally,
        /// the amount of turbo boost starts from a nominal threshold. In this mode, the
        /// boost starts from a guaranteed threshold.
        /// </summary>
        AggressiveAtGuaranteed,

        /// <summary>
        /// Windows always asks the processor to deliver the highest possible above the guaranteed
        /// performance level.
        /// 
        /// Intel processors can guarantee that the CPU runs at certain frequency. Usually,
        /// guaranteed frequency is the same as the nominal one, but in some cases such as
        /// in the presence of thermal limits, the guaranteed one will drop below the nominal.
        /// The amount of boost is a bias that is added on top of a certain threshold. Normally,
        /// the amount of turbo boost starts from a nominal threshold. In this mode, the
        /// boost starts from a guaranteed threshold.
        /// </summary>
        EfficientAggressiveAtGuaranteed
    }
}
