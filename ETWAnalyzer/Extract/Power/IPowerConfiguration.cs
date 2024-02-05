//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Extract.Power
{

    /// <summary>
    /// Windows Power Profile settings
    /// </summary>
    public interface IPowerConfiguration
    {
        /// <summary>
        /// Time stamp when this snapshot was taken.
        /// </summary>
        public float TimeSinceTraceStartS { get; }

        /// <summary>
        /// Gets a value that indicates how aggressive the performance states should be changed
        /// when increasing the processor performance state.
        /// </summary>
        ProcessorPerformanceChangePolicy IncreasePolicy { get; }

        /// <summary>
        /// Gets a value that indicates how aggressive the performance states should be changed
        /// when increasing the processor performance state.
        /// </summary>
        Nullable<uint> IncreasePolicyClass1 { get; }

        /// <summary>
        /// Gets a value that indicates the busy percentage threshold that must be met before
        /// increasing the processor performance state.
        /// </summary>
        PercentValue IncreaseThresholdPercent { get; }

        /// <summary>
        /// Gets a value that indicates how a processor opportunistically increases frequency
        /// above the maximum when operating conditions allow it to do so safely.
        /// </summary>
        ProcessorPerformanceBoostMode BoostMode { get; }

        /// <summary>
        /// Gets a value that indicates the bias in percentage used in managing performance
        /// and efficiency tradeoffs when boosting frequency above the maximum.
        /// Greater value biases more toward performance.
        /// </summary>
        PercentValue BoostPolicyPercent { get; }


        /// <summary>
        /// Gets a value that indicates how aggressive the performance states should be changed
        /// when decreasing the processor performance state.
        /// </summary>
        ProcessorPerformanceChangePolicy DecreasePolicy { get; }

        /// <summary>
        /// Gets a value that indicates the amount of time that must elapse after the last
        /// processor performance state changes before decreasing the processor performance state.
        /// </summary>
        TimeSpan DecreaseStabilizationInterval { get; }

        /// <summary>
        /// Gets a value that indicates the busy percentage threshold that must be met before decreasing the processor performance state.
        /// </summary>
        PercentValue DecreaseThresholdPercent { get; }

        /// <summary>
        /// Gets a value that indicates the amount of time that must elapse after the last
        /// processor performance state changes before increasing the processor performance state.
        /// </summary>
        TimeSpan IncreaseStabilizationInterval { get; }
        /// <summary>
        /// Gets a value that indicates how to set the processor frequency when the system
        /// detects a sensitivity to latency, as a percentage of the processor's maximum
        /// frequency.
        /// Latency sensitivity hints are generated when an event preceding an expected latency-sensitive
        /// operation is detected. Examples include mouse button up events (for all mouse
        /// buttons), touch gesture start and gesture stop (finger down and finger up), and
        /// keyboard enter key down.
        /// When set to 0, the processor performance engine does not take latency sensitivity
        /// hints into account when selecting a performance state. Otherwise, the performance
        /// is raised system-wide to the specified performance level
        /// </summary>
        PercentValue LatencySensitivityPerformancePercent { get; }

        /// <summary>
        /// Gets a value that indicates the maximum percentage of the processor frequency.
        /// For example, if this value is set to 80, then the processor frequency will never
        /// be throttled above 80 percent of its maximum frequency by the system.
        /// </summary>
        PercentValue MaxThrottlingFrequencyPercent { get; }

        /// <summary>
        /// Gets a value that indicates the maximum percentage of the processor frequency.
        /// For example, if this value is set to 80, then the processor frequency will never
        /// be throttled above 80 percent of its maximum frequency by the system.
        /// </summary>
        Nullable<PercentValue> MaxThrottlingFrequencyClass1Percent {get;}

        /// <summary>
        /// Processor performance level increase threshold for Processor Power Efficiency Class 2 processor count increase
        /// </summary>
        public Nullable<MultiHexValue> DecreaseLevelThresholdClass2 { get; }

        /// <summary>
        /// Processor performance level increase threshold for Processor Power Efficiency Class 1 processor count increase
        /// </summary>
        public MultiHexValue DecreaseLevelThresholdClass1 { get; }

        /// <summary>
        /// Short vs. long running thread threshold
        /// </summary>
        public Nullable<uint> ShortVsLongThreadThresholdUs { get; }

        /// <summary>
        /// Short running threads' processor architecture lower limit
        /// </summary>
        public Nullable<uint> LongRunningThreadsLowerArchitectureLimit { get; }

        /// <summary>
        /// Processor energy performance preference policy
        /// </summary>
        public PercentValue EnergyPreferencePercent { get; }

        /// <summary>
        /// Processor energy performance preference policy for Processor Power Efficiency Class 1
        /// </summary>
        public PercentValue EnergyPreferencePercentClass1 { get; }

        /// <summary>
        /// Processor performance increase time for Processor Power Efficiency Class 1
        /// </summary>
        public uint IncreaseStabilizationIntervalClass1 { get; }

        /// <summary>
        /// Processor performance level increase threshold for Processor Power Efficiency Class 2 processor count increase
        /// </summary>
        public Nullable<MultiHexValue> IncreaseThresholdPercentClass2 { get; }

        /// <summary>
        /// Processor performance level increase threshold for Processor Power Efficiency Class 1 processor count increase
        /// </summary>
        public MultiHexValue IncreaseThresholdPercentClass1 { get; }

        /// <summary>
        /// Gets a value that indicates the maximum processor frequency in efficiency class 1.
        /// Processor Power Efficiency Class describes the relative power efficiency of the
        /// associated processor. Lower efficiency class numbers are more efficient than
        /// higher ones (e.g.efficiency class 0 should be treated as more efficient than
        /// efficiency class 1). However, absolute values of this number have no meaning:
        /// 2 isn't necessarily half as efficient as 1.
        /// </summary>
        FrequencyValueMHz MaxEfficiencyClass1Frequency { get; }

        /// <summary>
        /// Gets a value that indicates the minimum percentage of the processor frequency.
        /// For example, if this value is set to 50, then the processor frequency will never
        /// be throttled below 50 percent of its maximum frequency by the system.
        /// </summary>
        PercentValue MinThrottlingFrequencyPercent { get; }

        /// <summary>
        /// Gets a value that indicates the minimum percentage of the processor frequency.
        /// For example, if this value is set to 50, then the processor frequency will never
        /// be throttled below 50 percent of its maximum frequency by the system.
        /// </summary>
        PercentValue MinThrottlingFrequencyPercentClass1 { get; }

        /// <summary>
        /// Gets a value that indicates the maximum processor frequency in efficiency class 0.
        /// Processor Power Efficiency Class describes the relative power efficiency of the
        /// associated processor. Lower efficiency class numbers are more efficient than
        /// higher ones (e.g.efficiency class 0 should be treated as more efficient than
        /// efficiency class 1). However, absolute values of this number have no meaning:
        /// 2 isn't necessarily half as efficient as 1.
        /// </summary>
        FrequencyValueMHz MaxEfficiencyClass0Frequency { get; }

        /// <summary>
        /// Gets a value that indicates the time that must expire before considering a change
        /// in the processor performance states or parked core set.
        /// </summary>
        TimeSpan StabilizationInterval { get; }

        /// <summary>
        /// Gets a value that indicates the system cooling policy (active or passive cooling).
        /// Although this is not directly related to processor settings, passive cooling
        /// results in slowing down the processor speed, which is controlled by throttling
        /// processor frequency as-needed, so it is fairly strongly related.
        /// </summary>
        SystemCoolingPolicy SystemCoolingPolicy { get; }

        /// <summary>
        /// Gets a value that indicates whether throttle states are allowed to be used even when performance states are available
        /// </summary>
        ProcessorThrottlePolicy ThrottlePolicy { get; }

        /// <summary>
        /// Gets a value that indicates the number of perf time check intervals to include
        /// when calculating a processor's busy percentage.
        /// </summary>
        int TimeWindowSize { get; }


        /// <summary>
        /// Power profiles inherit settings from their base profile
        /// </summary>
        public BasePowerProfile BaseProfile { get; }

        /// <summary>
        /// Hetero Policy which is active.
        /// </summary>
        public int HeteroPolicyInEffect { get; }

        /// <summary>
        /// Short running thread scheduling policy
        /// </summary>
        public HeteroThreadSchedulingPolicy HeteroPolicyThreadSchedulingShort { get; }

        /// <summary>
        /// Long running thread scheduling policy
        /// </summary>
        public HeteroThreadSchedulingPolicy HeteroPolicyThreadScheduling { get; }

        /// <summary>
        /// When true CPU manages frequency on its own.
        /// </summary>
        public bool AutonomousMode { get; }

        /// <summary>
        /// Currently active Power profile
        /// </summary>
        public BasePowerProfile ActivePowerProfile { get; }

        /// <summary>
        /// Active Power profile Guid
        /// </summary>
        public Guid ActivePowerProfileGuid { get; }

        /// <summary>
        /// Idle Configuration parameters
        /// </summary>
        IIdleConfiguration IdleConfiguration { get; }

        /// <summary>
        /// Processor Parking Configuration
        /// </summary>
        ProcessorParkingConfiguration ProcessorParkingConfiguration { get; }
    }
}