//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

namespace ETWAnalyzer.Extract.Power
{
    /// <summary>
    /// Defines a policy the system follows when making changes to processor frequency.
    /// </summary>
    public enum ProcessorPerformanceChangePolicy
    {
        /// <summary>
        ///  Try to achieve the midpoint busy-ness between the increase and the decrease thresholds.
        /// </summary>
        Ideal,

        /// <summary>
        /// Change by a fixed frequency step.
        /// </summary>
        Single,

        /// <summary>
        /// Jump directly to the extreme frequency.
        /// </summary>
        Rocket,

        /// <summary>
        /// Change more aggressively than ideal.
        /// </summary>
        IdealAggressive
    }
}
