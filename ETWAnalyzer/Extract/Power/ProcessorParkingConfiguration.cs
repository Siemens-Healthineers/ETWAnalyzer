//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Extract.Power
{
    /// <summary>
    /// Type safe wrapper around percentage values
    /// </summary>
    public enum PercentValue
    {
        /// <summary>
        /// 
        /// </summary>
        Invalid = -1
    }


    /// <summary>
    /// The uint value is split into 4 byte sized values
    /// </summary>
    public enum MultiHexValue
    {
        /// <summary>
        /// 
        /// </summary>
        Invalid = -1,
    }

    /// <summary>
    /// Processor Parking Configuration.
    /// </summary>
    public class ProcessorParkingConfiguration : IProcessorParkingConfiguration
    {
        /// <summary>
        /// Gets a value that indicates how busy all unparked processors must be in order
        /// to unpark an additional processor.
        /// </summary>
        public PercentValue ConcurrencyHeadroomThresholdPercent { get; set; }

        /// <summary>
        /// Gets a value used for determining an ideal number of processors to keep unparked
        /// on the system, as a percentage of how busy all processors are.
        /// The ideal unparked processor count calculated from this percentage is the minimum
        /// number of processors that would have been needed to handle more than that percentage
        /// of the system's activity. A higher concurrency threshold results in a higher
        /// concurrency count and thus a larger number of processors being kept unparked.
        /// For the concurrency count to be N, the time spent with N or fewer processors
        /// must just exceed the threshold. For example, on a 4 processor system, if the
        /// threshold is 90% and 5% of the time is spent with 4 processors busy and 5% is
        /// spent with 3 processors busy and the remaining 90% is spent with 2 or fewer processors
        /// busy, the concurrency count will be 3, because the total percentage of time spent
        /// with 3 or fewer running is 95%, which is the smallest processor count that is
        /// busy above the 90% threshold.
        /// </summary>
        public PercentValue ConcurrencyThresholdPercent { get; set; }

        /// <summary>
        /// Gets a value that indicates, on a per processor group basis, the maximum percentage
        /// of processors in the group that may be kept unparked for Processor Power Efficiency
        /// Class 1.
        /// Processor Power Efficiency Class describes the relative power efficiency of the
        /// associated processor. Lower efficiency class numbers are more efficient than
        /// higher ones (e.g.efficiency class 0 should be treated as more efficient than
        /// efficiency class 1). However, absolute values of this number have no meaning:
        /// 2 isn't necessarily half as efficient as 1.
        /// </summary>
        public PercentValue MaxEfficiencyClass1UnparkedProcessorPercent { get; set; }

        /// <summary>
        /// Gets a value that indicates the maximum percentage of processors that may be
        /// kept unparked in each processor group.
        /// </summary>
        public PercentValue MaxUnparkedProcessorPercent { get; set; }

        /// <summary>
        /// Gets a value that indicates, on a per processor group basis, the minimum percentage
        /// of processors in the group that must be kept unparked in Processor Power Efficiency
        /// Class 1.
        /// Processor Power Efficiency Class describes the relative power efficiency of the
        /// associated processor. Lower efficiency class numbers are more efficient than
        /// higher ones (e.g.efficiency class 0 should be treated as more efficient than
        /// efficiency class 1). However, absolute values of this number have no meaning:
        /// 2 isn't necessarily half as efficient as 1.
        /// </summary>
        public PercentValue MinEfficiencyClass1UnparkedProcessorPercent { get; set; }

        /// <summary>
        /// Gets a value that indicates the minimum amount of time a processor must have
        /// been parked before it may be unparked.
        /// </summary>
        public TimeSpan MinParkedDuration { get; set; }

        /// <summary>
        /// Gets a value that indicates the minimum amount of time a processor must have
        /// been unparked before it may be parked.
        /// </summary>
        public TimeSpan MinUnparkedDuration { get; set; }

        /// <summary>
        /// Gets a value that indicates the minimum percentage of processors that must be
        /// kept unparked in each processor group.
        /// </summary>
        public PercentValue MinUnparkedProcessorPercent { get; set; }

        /// <summary>
        /// Gets a value that indicates how busy a processor must be in order to be considered
        /// over-utilized.
        /// A processor must be busier than the threshold in order to be considered over-utilized.
        /// The percentage of time a processor spends being active/utilized determines how
        /// busy it is. A parked processor can still be active due to affinitized work. An
        /// over-utilized parked processor is more likely to be moved to the unparked state.
        /// An over-utilized unparked processor is more likely to be kept unparked
        /// </summary>
        public PercentValue OverUtilizationThresholdPercent { get; set; }

        /// <summary>
        /// Gets a value that indicates the performance state that a processor should enter
        /// when it is parked.
        /// </summary>
        public ParkingPerformanceState ParkingPerformanceState { get; set; }

        /// <summary>
        /// Gets a value that indicates how aggressive processor parking is when processors
        /// must be parked.
        /// </summary>
        public ProcessorParkingPolicy ParkingPolicy { get; set; }

        /// <summary>
        /// Gets a value that indicates how aggressive processor parking is when processors
        /// must be unparked.
        /// </summary>
        public ProcessorParkingPolicy UnparkingPolicy { get; set; }

        /// <summary>
        /// Gets a value that indicates whether the Utility Distribution feature is enabled.
        /// Utility Distribution is an algorithmic optimization that is designed to improve
        /// power efficiency for some workloads. It tracks unmovable CPU activity (that is,
        /// DPCs, interrupts, or strictly affinitized threads), and it predicts the future
        /// work on each processor based on the assumption that any movable work can be distributed
        /// equally across all unparked processors.
        /// Utility Distribution is enabled by default for the Balanced power plan for some
        /// processors. It can reduce processor power consumption by lowering the requested
        /// CPU frequencies of workloads that are in a reasonably steady state. However,
        /// Utility Distribution is not necessarily a good algorithmic choice for workloads
        /// that are subject to high activity bursts or for programs where the workload quickly
        /// and randomly shifts across processors. For such workloads, we recommend disabling
        /// Utility Distribution.
        /// </summary>
        public bool UtilityDistributionEnabled { get; set; }

        /// <summary>
        /// Gets a value that indicates the threshold used when utility distribution is enabled
        /// to determine how many processors to have unparked.
        /// Documentation on exactly what this threshold is or how it is used is not currently
        /// available.
        /// </summary>
        public PercentValue UtilityDistributionThresholdPercent { get; set; }


        /// <summary>
        /// Initial performance for Processor Power Efficiency Class 1 when unparked
        /// </summary>
        public PercentValue InitialPerformancePercentClass1 { get; set; }

        /// <summary>
        /// Processor performance core parking soft park latency
        /// </summary>
        public uint SoftParkLatencyUs { get;  set; }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IProcessorParkingConfiguration other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return this.ConcurrencyHeadroomThresholdPercent == other.ConcurrencyHeadroomThresholdPercent &&
                   this.ConcurrencyThresholdPercent == other.ConcurrencyThresholdPercent &&
                   this.InitialPerformancePercentClass1 == other.InitialPerformancePercentClass1 &&
                   this.MaxEfficiencyClass1UnparkedProcessorPercent == other.MaxEfficiencyClass1UnparkedProcessorPercent &&
                   this.MaxUnparkedProcessorPercent == other.MaxUnparkedProcessorPercent &&
                   this.MinEfficiencyClass1UnparkedProcessorPercent == other.MinEfficiencyClass1UnparkedProcessorPercent &&
                   this.MinParkedDuration == other.MinParkedDuration &&
                   this.MinUnparkedDuration == other.MinUnparkedDuration &&
                   this.MinUnparkedProcessorPercent == this.MinUnparkedProcessorPercent &&
                   this.OverUtilizationThresholdPercent == other.OverUtilizationThresholdPercent &&
                   this.ParkingPerformanceState == this.ParkingPerformanceState &&
                   this.ParkingPolicy == this.ParkingPolicy &&
                   this.SoftParkLatencyUs == other.SoftParkLatencyUs &&
                   this.UnparkingPolicy == this.UnparkingPolicy &&
                   this.UtilityDistributionEnabled == this.UtilityDistributionEnabled &&
                   this.UtilityDistributionThresholdPercent == other.UtilityDistributionThresholdPercent;
        }
    }
}
