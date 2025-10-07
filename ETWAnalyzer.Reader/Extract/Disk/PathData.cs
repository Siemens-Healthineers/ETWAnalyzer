using ETWAnalyzer.Extract.Disk;
using System;
using System.Collections.Generic;
using System.Text;

namespace ETWAnalyzer.Extract.Disk
{
    /// <summary>
    /// 
    /// </summary>
    public class PathData
    {
        /// <summary>
        /// Map of file names which contains a dictionary of access types (Read/Write/Flush) to DiskActivity metrics (size, duration, processes ...)
        /// </summary>
        public Dictionary<string, Dictionary<DiskIOTypes, DiskActivity>> FilePathToDiskEvents
        {
            get;
        } = new Dictionary<string, Dictionary<DiskIOTypes, DiskActivity>>();

    }
}
