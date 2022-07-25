using System.Collections.Generic;

namespace ETWAnalyzer.Extract.CPU
{
    /// <summary>
    /// Contains CPU time of a process split by DeltaTime time slices
    /// </summary>
    public interface IProcessTimeLine
    {
        /// <summary>
        /// Contains for given process the CPU time in ms for the duration of DeltaTime.
        /// </summary>
        IReadOnlyList<decimal> CPUMs { get;  }
    }
}