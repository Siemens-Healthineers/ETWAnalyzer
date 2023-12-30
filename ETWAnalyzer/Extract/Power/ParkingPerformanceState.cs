//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


namespace ETWAnalyzer.Extract.Power
{
    /// <summary>
    /// Defines the performance state (target frequency) for parked processor cores.
    /// </summary>
    public enum ParkingPerformanceState
    {
        /// <summary>
        /// No specified target frequency. The target frequency will be determined based
        /// on other criteria.
        /// </summary>
        NoPreference,

        /// <summary>
        /// Specify minimum target frequency for parked cores.
        /// </summary>
        Deepest,

        /// <summary>
        /// Specify maximum target frequency for parked cores.
        /// </summary>
        Lightest
    }
}
