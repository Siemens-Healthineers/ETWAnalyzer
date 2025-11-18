using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.PMC
{
    /// <summary>
    /// PMC Data for a given counter per process
    /// </summary>
    public class PMCCounter : IPMCCounter
    {
        /// <summary>
        /// Performance Monitoring Counter Name
        /// Set needed by Json.Net
        /// </summary>
        public string CounterName
        {
            get;
            set;
        }

        /// <summary>
        /// Map of process to Counter value which contains the total counter value for a process for the trace duration
        /// </summary>
        public Dictionary<ETWProcessIndex, ulong> ProcessMap { get; set; } = new Dictionary<ETWProcessIndex, ulong>();

        IReadOnlyDictionary<ETWProcessIndex, ulong> IPMCCounter.ProcessMap { get => ProcessMap; }
    }
}
