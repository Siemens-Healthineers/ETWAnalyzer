//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

namespace ETWAnalyzer.Extract.Power
{
    /// <summary>
    /// Defines the policy the system uses to throttle a processor's frequency.
    /// </summary>
    public enum ProcessorThrottlePolicy
    {
        /// <summary>
        /// Processor throttling is disabled.
        /// </summary>
        Disabled,
        /// <summary>
        /// Processor throttling is enabled.
        /// </summary>
        Enabled,
        /// <summary>
        /// Processor throttling is enabled based on other factors.
        /// </summary>
        Automatic
    }
}
