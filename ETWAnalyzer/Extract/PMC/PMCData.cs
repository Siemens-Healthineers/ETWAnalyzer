using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.PMC
{

    /// <summary>
    /// Contains Performance Monitoring Counter data from CPU
    /// </summary>
    public class PMCData : IPMCData
    {
        /// <summary>
        /// Process Summary data
        /// </summary>
        public List<PMCCounter> Counters
        {
            get; set;
        } = new List<PMCCounter>();

        /// <summary>
        /// Process Summary data
        /// </summary>
        IReadOnlyList<IPMCCounter> IPMCData.Counters => Counters;

        /// <summary>
        /// Method call estimates based on LBR sampling data
        /// </summary>
        public LBRData LBRData
        {
            get; set;
        } = new LBRData();

        ILBRData IPMCData.LBRData => LBRData;
    }

}
