//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Extract.Power
{
    /// <summary>
    /// CPU Idle configuration settings
    /// </summary>
    public class IdleConfiguration : IIdleConfiguration
    {
        /// <summary>
        /// Gets a value that indicates the deepest idle state that should be used.
        ///  If this value is set to zero, this setting is ignored. If the value is higher
        ///  than that supported by the processor, this setting will also be ignored.
        /// </summary>
        public int DeepestIdleState { get; set; }

        /// <summary>
        /// Gets a value that indicates the upper busy threshold that must be met before
        /// demoting the processor to a shallower idle state.
        /// </summary>
        public PercentValue DemoteThresholdPercent { get; set; }

        /// <summary>
        /// Gets a value that indicates whether idle states should be enabled.
        /// If this value is false, no other properties on this object have any effect.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets a value that indicates the time that must have elapsed since the last idle
        /// state promotion or demotion before idle states may be promoted or demoted again.
        /// </summary>
        public TimeSpan MinimumDurationBetweenChecks { get; set; }

        /// <summary>
        /// Gets a value that indicates the lower busy threshold that must be met before
        /// promoting the processor to a deeper idle state.
        /// </summary>
        public PercentValue PromoteThresholdPercent { get; set; }

        /// <summary>
        /// Gets a value that indicates whether idle state promotion and demotion values
        /// should be scaled based on the current performance state.
        /// </summary>
        public bool ScalingEnabled { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IIdleConfiguration other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return this.DeepestIdleState == other.DeepestIdleState &&
                   this.DemoteThresholdPercent == other.DemoteThresholdPercent &&
                   this.Enabled == other.Enabled &&
                   this.MinimumDurationBetweenChecks == other.MinimumDurationBetweenChecks &&
                   this.PromoteThresholdPercent == other.PromoteThresholdPercent &&
                   this.ScalingEnabled == other.ScalingEnabled;
        }
    }
}
