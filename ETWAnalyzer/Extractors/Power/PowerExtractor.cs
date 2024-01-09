//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Power;
using ETWAnalyzer.Infrastructure;
using Microsoft.Windows.EventTracing;
using System;

namespace ETWAnalyzer.Extractors.Power
{
    /// <summary>
    /// Extract from ETW Provider Microsoft-Windows-Power the currently used power profile
    /// </summary>
    internal class PowerExtractor : ExtractorBase
    {
        /// <summary>
        /// Contains power data when present in trace.
        /// </summary>
        IPendingResult<Microsoft.Windows.EventTracing.Power.IPowerConfigurationDataSource> myPower;

        /// <summary>
        /// Register Power parser.
        /// </summary>
        /// <param name="processor"></param>
        public override void RegisterParsers(ITraceProcessor processor)
        {
            base.RegisterParsers(processor);
            myPower = processor.UsePowerConfigurationData();
        }

        /// <summary>
        /// Extract Power profile data from ETL file.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="results"></param>
        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            using var logger = new PerfLogger("Extract Power");

            if (myPower.HasResult)
            {
                PowerConfiguration previous = null;

                foreach (Microsoft.Windows.EventTracing.Power.IPowerConfigurationSnapshot power in myPower.Result.Snapshots)
                {
                    var powerData = new PowerConfiguration();
                    var processorConfig = power.ProcessorConfiguration;
                    var perfConfig = processorConfig.PerformanceConfiguration;
                    var idleConfig = processorConfig.IdleConfiguration;
                    var parkingConfig = processorConfig.ProcessorParkingConfiguration;

                    powerData.TimeSinceTraceStartS = (float)power.Timestamp.RelativeTimestamp.TotalSeconds;
                    powerData.MaxEfficiencyClass1Frequency = (FrequencyValueMHz) processorConfig.MaxEfficiencyClass1Frequency.GetValueOrDefault().TotalMegahertz;
                    powerData.MaxEfficiencyClass0Frequency = (FrequencyValueMHz) processorConfig.MaxEfficiencyClass0Frequency.GetValueOrDefault().TotalMegahertz;

                    powerData.BoostMode = (ProcessorPerformanceBoostMode) perfConfig.BoostMode;
                    powerData.BoostPolicyPercent = (PercentValue) perfConfig.BoostPolicy.Value;
                    powerData.DecreasePolicy = (ProcessorPerformanceChangePolicy) perfConfig.DecreasePolicy;
                    powerData.DecreaseStabilizationInterval = perfConfig.DecreaseStabilizationInterval;
                    powerData.DecreaseThresholdPercent = (PercentValue) perfConfig.DecreaseThreshold.Value;
                    powerData.IncreasePolicy = (ProcessorPerformanceChangePolicy) perfConfig.IncreasePolicy;
                    powerData.IncreaseStabilizationInterval = perfConfig.IncreaseStabilizationInterval;
                    powerData.IncreaseThresholdPercent = (PercentValue) perfConfig.IncreaseThreshold.Value;
                    powerData.LatencySensitivityPerformancePercent = (PercentValue) perfConfig.LatencySensitivityPerformance.Value;
                    powerData.MaxThrottlingFrequencyPercent = (PercentValue) perfConfig.MaxThrottlingFrequency.Value;
                    powerData.MinThrottlingFrequencyPercent = (PercentValue) perfConfig.MinThrottlingFrequency.Value;
                    powerData.StabilizationInterval = perfConfig.StabilizationInterval;
                    powerData.SystemCoolingPolicy = (SystemCoolingPolicy) perfConfig.SystemCoolingPolicy;
                    powerData.ThrottlePolicy = (ProcessorThrottlePolicy) perfConfig.ThrottlePolicy;
                    powerData.TimeWindowSize = perfConfig.TimeWindowSize;

                    powerData.IdleConfiguration.DeepestIdleState = idleConfig.DeepestIdleState;
                    powerData.IdleConfiguration.DemoteThresholdPercent = (PercentValue) idleConfig.DemoteThreshold.Value;
                    powerData.IdleConfiguration.Enabled = idleConfig.Enabled;
                    powerData.IdleConfiguration.MinimumDurationBetweenChecks = TimeSpan.FromTicks(idleConfig.MinimumDurationBetweenChecks.Ticks/1000);  // BUG in TraceProcessing where the duration is parsed as millisecond but the unit is microseconds!
                    powerData.IdleConfiguration.PromoteThresholdPercent = (PercentValue) idleConfig.PromoteThreshold.Value;
                    powerData.IdleConfiguration.ScalingEnabled = idleConfig.ScalingEnabled;

                    powerData.ProcessorParkingConfiguration.ConcurrencyHeadroomThresholdPercent = (PercentValue) parkingConfig.ConcurrencyHeadroomThreshold.Value;
                    powerData.ProcessorParkingConfiguration.ConcurrencyThresholdPercent = (PercentValue) parkingConfig.ConcurrencyThreshold.Value;
                    powerData.ProcessorParkingConfiguration.MaxEfficiencyClass1UnparkedProcessorPercent = (PercentValue) parkingConfig.MaxEfficiencyClass1UnparkedProcessor.Value;
                    powerData.ProcessorParkingConfiguration.MaxUnparkedProcessorPercent = (PercentValue) parkingConfig.MaxUnparkedProcessor.Value;
                    powerData.ProcessorParkingConfiguration.MinEfficiencyClass1UnparkedProcessorPercent = (PercentValue) parkingConfig.MinEfficiencyClass1UnparkedProcessor.Value;
                    powerData.ProcessorParkingConfiguration.MinParkedDuration = parkingConfig.MinParkedDuration;
                    powerData.ProcessorParkingConfiguration.MinUnparkedDuration = parkingConfig.MinUnparkedDuration;
                    powerData.ProcessorParkingConfiguration.MinUnparkedProcessorPercent = (PercentValue) parkingConfig.MinUnparkedProcessor.Value;
                    powerData.ProcessorParkingConfiguration.OverUtilizationThresholdPercent = (PercentValue) parkingConfig.OverUtilizationThreshold.Value;
                    powerData.ProcessorParkingConfiguration.ParkingPerformanceState = (ParkingPerformanceState) parkingConfig.ParkingPerformanceState;
                    powerData.ProcessorParkingConfiguration.ParkingPolicy = (ProcessorParkingPolicy) parkingConfig.ParkingPolicy;
                    powerData.ProcessorParkingConfiguration.UnparkingPolicy = (ProcessorParkingPolicy) parkingConfig.UnparkingPolicy;
                    powerData.ProcessorParkingConfiguration.UtilityDistributionEnabled = parkingConfig.UtilityDistributionEnabled;
                    powerData.ProcessorParkingConfiguration.UtilityDistributionThresholdPercent = (PercentValue) parkingConfig.UtilityDistributionThreshold.Value;

                    // We only save power profile data which is not the same over the duration of the recording.
                    // The default of the wpr Power profile is to record the profile at trace end two times.
                    // Since these are most often identical we spare the space and useless output in -Dump Power command 
                    // by not storing identical data in the resulting Json file.
                    if (!powerData.Equals(previous))
                    {
                        results.PowerConfiguration.Add(powerData);
                        previous = powerData;
                    }
                }
            }
        }
    }
}
