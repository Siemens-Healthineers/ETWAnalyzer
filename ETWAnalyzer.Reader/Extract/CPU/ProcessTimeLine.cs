using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.CPU
{
    /// <summary>
    /// Contains CPU time of a process split by DeltaTime time slices
    /// </summary>
    public class ProcessTimeLine : IProcessTimeLine
    {
        /// <summary>
        /// Contains for given process the CPU time in ms for the duration of DeltaTime.
        /// </summary>
        public List<decimal> CPUMs
        {
            get; set;
        } = new List<decimal>();

        IReadOnlyList<decimal> IProcessTimeLine.CPUMs => CPUMs;
    }
}

