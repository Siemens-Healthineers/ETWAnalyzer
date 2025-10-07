using System.Collections.Generic;

namespace ETWAnalyzer.Extract.PMC
{
    /// <summary>
    /// Contains Performance Monitoring Counter data from CPU
    /// </summary>
    public interface IPMCData
    {
        /// <summary>
        /// Process Summary data
        /// </summary>
        IReadOnlyList<IPMCCounter> Counters { get; }

        /// <summary>
        /// Last Branch Record data which contains method call estimates based on the CPU LBR data.
        /// </summary>
        ILBRData LBRData { get; }
    }
}