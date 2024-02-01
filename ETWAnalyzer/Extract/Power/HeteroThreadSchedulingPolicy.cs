//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


namespace ETWAnalyzer.Extract.Power
{
    /// <summary>
    /// Defines how long/short running threads are scheduled on which core type
    /// </summary>
    public enum HeteroThreadSchedulingPolicy
    {
        /// <summary>
        ///  Schedule to any available processor.
        /// </summary>
        AllProcessors = 0,

        /// <summary>
        /// Schedule exclusively to more performant processors.
        /// </summary>
        PerformantProcessors = 1,

        /// <summary>
        /// Schedule to more performant processors when possible.
        /// </summary>
        PreferPerformantProcessors = 2,

        /// <summary>
        /// Schedule exclusively to more efficient processors.
        /// </summary>
        EfficientProcessors = 3,

        /// <summary>
        /// Schedule to more efficient processors when possible.
        /// </summary>
        PreferEfficientProcessors = 4,

        /// <summary>
        /// Let the system choose an appropriate policy.
        /// </summary>
        Automatic = 5,
    }
}
