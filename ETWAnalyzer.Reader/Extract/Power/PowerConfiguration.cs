//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Extract.Power
{
    /// <summary>
    /// Type safe enum wrapper around percent values
    /// </summary>
    public enum FrequencyValueMHz
    {
        /// <summary>
        /// Invalid frequency
        /// </summary>
        Invalid = -1,
    }

    /// <summary>
    /// Windows Power Profile settings
    /// </summary>
    public class PowerConfiguration : IPowerConfiguration, IEquatable<PowerConfiguration>
    {
        /// <summary>
        /// Time stamp when this snapshot was taken.
        /// </summary>
        public float TimeSinceTraceStartS { get; set; }

        /// <summary>
        /// Gets a value that indicates how aggressive the performance states should be changed
        /// when increasing the processor performance state.
        /// </summary>
        public ProcessorPerformanceChangePolicy IncreasePolicy { get; set; }

        /// <summary>
        /// Gets a value that indicates how aggressive the performance states should be changed
        /// when increasing the processor performance state.
        /// </summary>
        public Nullable<uint> IncreasePolicyClass1 { get; set;  }

        /// <summary>
        /// Gets a value that indicates the busy percentage threshold that must be met before
        /// increasing the processor performance state.
        /// </summary>
        public PercentValue IncreaseThresholdPercent { get; set; }

        /// <summary>
        /// Gets a value that indicates how a processor opportunistically increases frequency
        /// above the maximum when operating conditions allow it to do so safely.
        /// </summary>
        public ProcessorPerformanceBoostMode BoostMode { get; set; }

        /// <summary>
        /// Gets a value that indicates the bias in percentage used in managing performance
        /// and efficiency tradeoffs when boosting frequency above the maximum.
        /// Greater value biases more toward performance.
        /// </summary>
        public PercentValue BoostPolicyPercent { get; set; }

        /// <summary>
        /// Power profiles inherit settings from their base profile
        /// </summary>
        public BasePowerProfile BaseProfile { get; set; }

        /// <summary>
        /// Gets a value that indicates how aggressive the performance states should be changed
        /// when decreasing the processor performance state.
        /// </summary>
        public ProcessorPerformanceChangePolicy DecreasePolicy { get; set; }

        /// <summary>
        /// Gets a value that indicates the amount of time that must elapse after the last
        /// processor performance state changes before decreasing the processor performance state.
        /// </summary>
        public TimeSpan DecreaseStabilizationInterval { get; set; }

        /// <summary>
        /// Gets a value that indicates the busy percentage threshold that must be met before decreasing the processor performance state.
        /// </summary>
        public PercentValue DecreaseThresholdPercent { get; set; }

        /// <summary>
        /// Gets a value that indicates the amount of time that must elapse after the last
        /// processor performance state changes before increasing the processor performance state.
        /// </summary>
        public TimeSpan IncreaseStabilizationInterval { get; set; }
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
        public PercentValue LatencySensitivityPerformancePercent { get; set; }

        /// <summary>
        /// Gets a value that indicates the maximum percentage of the processor frequency.
        /// For example, if this value is set to 80, then the processor frequency will never
        /// be throttled above 80 percent of its maximum frequency by the system.
        /// </summary>
        public PercentValue MaxThrottlingFrequencyPercent { get; set; }

        /// <summary>
        /// Gets a value that indicates the maximum processor frequency in efficiency class 1.
        /// Processor Power Efficiency Class describes the relative power efficiency of the
        /// associated processor. Lower efficiency class numbers are more efficient than
        /// higher ones (e.g.efficiency class 0 should be treated as more efficient than
        /// efficiency class 1). However, absolute values of this number have no meaning:
        /// 2 isn't necessarily half as efficient as 1.
        /// </summary>
        public FrequencyValueMHz MaxEfficiencyClass1Frequency { get; set; }

        /// <summary>
        /// Gets a value that indicates the minimum percentage of the processor frequency.
        /// For example, if this value is set to 50, then the processor frequency will never
        /// be throttled below 50 percent of its maximum frequency by the system.
        /// </summary>
        public PercentValue MinThrottlingFrequencyPercent { get; set; }

        /// <summary>
        /// Gets a value that indicates the minimum percentage of the processor frequency.
        /// For example, if this value is set to 50, then the processor frequency will never
        /// be throttled below 50 percent of its maximum frequency by the system.
        /// </summary>
        public PercentValue MinThrottlingFrequencyPercentClass1 { get; set;  }

        /// <summary>
        /// Maximum throttle frequency Class 1
        /// </summary>
        public Nullable<PercentValue> MaxThrottlingFrequencyClass1Percent { get; set; }

        /// <summary>
        /// Gets a value that indicates the maximum processor frequency in efficiency class 0.
        /// Processor Power Efficiency Class describes the relative power efficiency of the
        /// associated processor. Lower efficiency class numbers are more efficient than
        /// higher ones (e.g.efficiency class 0 should be treated as more efficient than
        /// efficiency class 1). However, absolute values of this number have no meaning:
        /// 2 isn't necessarily half as efficient as 1.
        /// </summary>
        public FrequencyValueMHz MaxEfficiencyClass0Frequency { get; set; }

        /// <summary>
        /// Gets a value that indicates the time that must expire before considering a change
        /// in the processor performance states or parked core set.
        /// </summary>
        public TimeSpan StabilizationInterval { get; set; }

        /// <summary>
        /// Gets a value that indicates the system cooling policy (active or passive cooling).
        /// Although this is not directly related to processor settings, passive cooling
        /// results in slowing down the processor speed, which is controlled by throttling
        /// processor frequency as-needed, so it is fairly strongly related.
        /// </summary>
        public SystemCoolingPolicy SystemCoolingPolicy { get; set; }

        /// <summary>
        /// Gets a value that indicates whether throttle states are allowed to be used even when performance states are available
        /// </summary>
        public ProcessorThrottlePolicy ThrottlePolicy { get; set; }

        /// <summary>
        /// Gets a value that indicates the number of perf time check intervals to include
        /// when calculating a processor's busy percentage.
        /// </summary>
        public int TimeWindowSize { get; set; }

        /// <summary>
        /// Idle Configuration parameters
        /// </summary>
        public IdleConfiguration IdleConfiguration { get; set; } = new();

        IIdleConfiguration IPowerConfiguration.IdleConfiguration => IdleConfiguration;


        /// <summary>
        /// Processor Parking Configuration
        /// </summary>
        public ProcessorParkingConfiguration ProcessorParkingConfiguration { get; set; } = new();

        /// <summary>
        /// Hetero Policy which is active.
        /// </summary>
        public int HeteroPolicyInEffect { get; set; }

        /// <summary>
        /// Short running thread scheduling policy
        /// </summary>
        public HeteroThreadSchedulingPolicy HeteroPolicyThreadSchedulingShort { get; set; }

        /// <summary>
        /// Long running thread scheduling policy
        /// </summary>
        public HeteroThreadSchedulingPolicy HeteroPolicyThreadScheduling { get; set; }

        /// <summary>
        /// When true CPU manages frequency on its own.
        /// </summary>
        public bool AutonomousMode { get; set; }

        /// <summary>
        /// Currently active Power profile
        /// </summary>
        public BasePowerProfile ActivePowerProfile { get; set; }

        /// <summary>
        /// Active Power profile Guid
        /// </summary>
        public Guid ActivePowerProfileGuid { get; set; }

        /// <summary>
        /// Processor performance level increase threshold for Processor Power Efficiency Class 2 processor count increase
        /// </summary>
        public Nullable<MultiHexValue> DecreaseLevelThresholdClass2 { get; set; }

        /// <summary>
        /// Processor performance level increase threshold for Processor Power Efficiency Class 1 processor count increase
        /// </summary>
        public MultiHexValue DecreaseLevelThresholdClass1 { get; set; }

        /// <summary>
        /// Short vs. long running thread threshold
        /// </summary>
        public Nullable<uint> ShortVsLongThreadThresholdUs { get; set; }

        /// <summary>
        /// Short running threads' processor architecture lower limit
        /// </summary>
        public Nullable<uint> LongRunningThreadsLowerArchitectureLimit { get; set; }

        /// <summary>
        /// Processor energy performance preference policy
        /// </summary>
        public PercentValue EnergyPreferencePercent { get; set; }

        /// <summary>
        /// Processor energy performance preference policy for Processor Power Efficiency Class 1
        /// </summary>
        public PercentValue EnergyPreferencePercentClass1 { get; set; }

        /// <summary>
        /// Processor performance increase time for Processor Power Efficiency Class 1
        /// </summary>
        public uint IncreaseStabilizationIntervalClass1 { get; set; }

        /// <summary>
        /// Processor performance level increase threshold for Processor Power Efficiency Class 2 processor count increase
        /// </summary>
        public Nullable<MultiHexValue> IncreaseThresholdPercentClass2 { get; set; }

        /// <summary>
        /// Processor performance level increase threshold for Processor Power Efficiency Class 1 processor count increase
        /// </summary>
        public MultiHexValue IncreaseThresholdPercentClass1 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as PowerConfiguration);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(PowerConfiguration other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return
                this.ActivePowerProfile == other.ActivePowerProfile &&
                this.AutonomousMode == other.AutonomousMode &&
                this.BaseProfile == other.BaseProfile &&
                this.BoostMode == other.BoostMode &&
                this.BoostPolicyPercent == other.BoostPolicyPercent &&
                this.DecreasePolicy == other.DecreasePolicy &&
                this.DecreaseLevelThresholdClass1 == other.DecreaseLevelThresholdClass1 &&
                this.DecreaseLevelThresholdClass2 == other.DecreaseLevelThresholdClass2 &&
                this.DecreaseStabilizationInterval == other.DecreaseStabilizationInterval &&
                this.DecreaseThresholdPercent == other.DecreaseThresholdPercent &&
                this.EnergyPreferencePercent     == other.EnergyPreferencePercent &&
                this.EnergyPreferencePercentClass1 == other.EnergyPreferencePercentClass1 &&
                this.HeteroPolicyInEffect == other.HeteroPolicyInEffect &&
                this.HeteroPolicyThreadScheduling == other.HeteroPolicyThreadScheduling &&
                this.HeteroPolicyThreadSchedulingShort == other.HeteroPolicyThreadSchedulingShort &&
                this.IncreasePolicy == other.IncreasePolicy &&
                this.IncreasePolicyClass1 == other.IncreasePolicyClass1 &&
                this.IncreaseStabilizationInterval == other.IncreaseStabilizationInterval &&
                this.IncreaseStabilizationIntervalClass1 == other.IncreaseStabilizationIntervalClass1 &&
                this.IncreaseThresholdPercent == other.IncreaseThresholdPercent &&
                this.IncreaseThresholdPercentClass1 == other.IncreaseThresholdPercentClass1 &&
                this.IncreaseThresholdPercentClass2 == other.IncreaseThresholdPercentClass2 &&
                this.LatencySensitivityPerformancePercent == other.LatencySensitivityPerformancePercent &&
                this.LongRunningThreadsLowerArchitectureLimit == other.LongRunningThreadsLowerArchitectureLimit &&
                this.MaxEfficiencyClass0Frequency == other.MaxEfficiencyClass0Frequency &&
                this.MaxEfficiencyClass1Frequency == other.MaxEfficiencyClass1Frequency &&
                this.MaxThrottlingFrequencyPercent == other.MaxThrottlingFrequencyPercent &&
                this.MaxThrottlingFrequencyClass1Percent == other.MaxThrottlingFrequencyClass1Percent &&
                this.MinThrottlingFrequencyPercent == other.MinThrottlingFrequencyPercent &&
                this.MinThrottlingFrequencyPercentClass1 == other.MinThrottlingFrequencyPercentClass1 &&
                this.ShortVsLongThreadThresholdUs == other.ShortVsLongThreadThresholdUs &&
                this.StabilizationInterval == other.StabilizationInterval &&
                this.SystemCoolingPolicy == other.SystemCoolingPolicy &&
                this.ThrottlePolicy == other.ThrottlePolicy &&
                this.TimeWindowSize == other.TimeWindowSize &&
                this.ProcessorParkingConfiguration.Equals(other.ProcessorParkingConfiguration) &&
                this.IdleConfiguration.Equals(other.IdleConfiguration);
        }

        /// <summary>
        /// Should not be used. 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
