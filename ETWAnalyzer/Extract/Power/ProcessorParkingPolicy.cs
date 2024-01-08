//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

namespace ETWAnalyzer.Extract.Power
{
    /// <summary>
    /// Processor Parking Policies
    /// </summary>
    public enum ProcessorParkingPolicy
    {
   
        /// <summary>
        /// Transition logical processors between the parked and unparked states such that
        /// the utilization is kept near the middle of the max and min thresholds.
        /// </summary>
        Ideal,

     
        /// <summary>
        /// Transition only one logical processor at a time between the parked and unparked
        /// states.
        /// </summary>
        Single,

        /// <summary>
        /// Transition all logical processors to the parked state or unparked state at the
        /// same time.
        /// </summary>
        Rocket,

        /// <summary>
        /// Transition more than one logical processor at a time between the parked and unparked
        /// states.
        /// </summary>
        Multistep
    }
}
