using System.Collections.Generic;

namespace ETWAnalyzer.Extract.PMC
{
    /// <summary>
    /// PMC Data for a given counter per process
    /// </summary>
    public interface IPMCCounter
    {
        /// <summary>
        /// Performance Monitoring Counter Name
        /// </summary>
        string CounterName { get; }

        /// <summary>
        /// Map of process to Counter value which contains the total counter value for a process for the trace duration
        /// </summary>
        IReadOnlyDictionary<ETWProcessIndex, ulong> ProcessMap { get; }
    }
}