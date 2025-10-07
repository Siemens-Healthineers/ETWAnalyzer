//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.FileIO
{
    /// <summary>
    /// 
    /// </summary>
    public class FileIOData : IFileIOData
    {
        /// <summary>
        /// Mapping of a file name to a PerProcess mapping
        /// </summary>
        public Dictionary<string, PerProcessFileIOStats> FileName2PerProcessMapping
        {
            get;
            set;
        } = new();

        List<FileIOContainer> myQueryData;

        /// <summary>
        /// Get File IO Data as flat list where for each file the process and its metrics are stored.
        /// </summary>
        /// <param name="processMapper">Usually the IETWExtract instance to map the process indices's to the actual process instance.</param>
        public IReadOnlyList<FileIOContainer> GetFileNameProcessStats(IProcessExtract processMapper)
        {
            if( myQueryData == null)
            {
                myQueryData = new List<FileIOContainer>();
                foreach (var kvp in FileName2PerProcessMapping)
                {
                    string file = kvp.Key;
                    foreach(var perProc in kvp.Value.Process)
                    {
                        myQueryData.Add(new FileIOContainer(kvp.Key, processMapper.GetProcess(perProc.Key), perProc.Value));
                    }
                }
            }

            return myQueryData;
        }

        /// <summary>
        /// Add for a given file with a process and process start time new file statistics data
        /// </summary>
        /// <param name="extract">Current instance of ETWExtract which is needed to find the corresponding ETWProcess index.</param>
        /// <param name="fileName">File name which for which statistics data is added</param>
        /// <param name="pid">Process Id of process which did perform File IO</param>
        /// <param name="startTime">Process start time of pid</param>
        /// <param name="stat">Additional file IO statistics data which is added/merged to existing summary data.</param>
        public void Add(IProcessExtract extract, uint pid, DateTimeOffset startTime, string fileName , FileIOStatistics stat)
        {
            if (extract == null)
            {
                throw new ArgumentNullException(nameof(extract));
            }

            if (stat == null)
            {
                throw new ArgumentNullException(nameof(stat));
            }

            ETWProcessIndex processIndex = extract.GetProcessIndexByPID(pid, startTime);
            if (!FileName2PerProcessMapping.TryGetValue(fileName, out PerProcessFileIOStats stats))
            {
                stats = new PerProcessFileIOStats();
                FileName2PerProcessMapping[fileName] = stats;
            }

            stats.Add(processIndex, stat);
        }

    }
}
