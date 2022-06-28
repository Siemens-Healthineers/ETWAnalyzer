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
    }
}