//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.FileIO
{
    /// <summary>
    /// Contains for each process the FileIO data
    /// </summary>
    public class PerProcessFileIOStats
    {
        /// <summary>
        /// Mapping a process to the corresponding per process metric
        /// </summary>
        public Dictionary<ETWProcessIndex, FileIOStatistics> Process
        {
            get;
            set;
        } = new();


        /// <summary>
        /// Add for a given process the corresponding file statistics
        /// </summary>
        /// <param name="idx">Index to Process list in ETWExtract to save space in serialized output</param>
        /// <param name="stats">Actual Metric data</param>
        public void Add(ETWProcessIndex idx, FileIOStatistics stats)
        {
            if (!Process.TryGetValue(idx, out FileIOStatistics fileStats))
            {
                Process[idx] = stats;
                return;
            }

            fileStats.Add(stats);
        }
    }
}
