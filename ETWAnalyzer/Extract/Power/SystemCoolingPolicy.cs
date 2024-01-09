//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

namespace ETWAnalyzer.Extract.Power
{
    /// <summary>
    /// Defines the policy that determines how system cools down the processor.
    /// </summary>
    public enum SystemCoolingPolicy
    {
        /// <summary>
        /// The system increases the fan speed. If the temperature is still too high, it
        /// will slow down the processor. 
        /// </summary>
        Active,

        /// <summary>
        /// The system slows down the processor. If the temperature is still too high, it
        /// will increase the fan speed.
        /// </summary>
        Passive
    }
}
