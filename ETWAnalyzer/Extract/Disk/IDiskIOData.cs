//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

namespace ETWAnalyzer.Extract.Disk
{

    /// <summary>
    /// Disk IO data
    /// </summary>
    public interface IDiskIOData
    {
        /// <summary>
        /// List of Disk IO aggregate events per File
        /// </summary>
        DiskIOEvent[] DiskIOEvents { get; }

        /// <summary>
        /// Total Disk Flush time in microseconds (us = 10^(-6)s) 
        /// </summary>
        ulong TotalDiskFlushTimeInus { get;}

        /// <summary>
        /// Total Disk Read Time in microseconds (us = 10^(-6)s)
        /// </summary>
        ulong TotalDiskReadTimeInus { get; }

        /// <summary>
        /// Sum of Read/Write/Flush Disk Times in microseconds (us = 10^(-6)s)
        /// </summary>
        ulong TotalDiskServiceTimeInus { get; }

        /// <summary>
        /// Total Disk Write Time in microseconds (us = 10^(-6)s) 
        /// </summary>
        ulong TotalDiskWriteTimeTimeInus { get; }
    }
}