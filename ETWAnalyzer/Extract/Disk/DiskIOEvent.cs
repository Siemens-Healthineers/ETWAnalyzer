//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.Disk
{
    /// <summary>
    /// Contains for a file all processes which were involved in IO and how long the disk was used for that file. 
    /// Time correlation is not contained to keep the extracted data size small.
    /// </summary>
    public class DiskIOEvent
    {
        /// <summary>
        /// Create a DiskIOEvent instance
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="readus"></param>
        /// <param name="writeus"></param>
        /// <param name="flushus"></param>
        /// <param name="writeSizeInBytes"></param>
        /// <param name="readSizeInBytes"></param>
        /// <param name="priorities"></param>
        /// <param name="processes"></param>
        public DiskIOEvent(string fileName, ulong readus, ulong writeus, ulong flushus, ulong writeSizeInBytes, ulong readSizeInBytes, DiskIOPriorities priorities, ProcessKey[] processes)
        {
            FileName = fileName;
            DiskReadTimeInus = readus;
            DiskWriteTimeInus = writeus;
            DiskFlushTimeInus = flushus;
            WriteSizeInBytes = writeSizeInBytes;
            ReadSizeInBytes = readSizeInBytes;
            WriteSizeInBytes = writeSizeInBytes;
            Priorities = priorities;
            InvolvedProcesses = processes;
        }

        /// <summary>
        /// File Name which was accessed
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Bit flag of Disk IO priorities. Usually it should be a single value but it can also be a mixture
        /// if e.g. the Search Indexing Service or Disk Prefetch Service is preloading dlls
        /// </summary>
        public DiskIOPriorities Priorities { get; } 

        /// <summary>
        /// Sum of Read/Write and Flush DiskServiceTimeInus
        /// </summary>
        public ulong DiskTotalTimeInus { get => DiskReadTimeInus + DiskWriteTimeInus + DiskFlushTimeInus; }

        /// <summary>
        /// Read Disk Service Time for this file in us
        /// </summary>
        public ulong DiskReadTimeInus { get; }

        /// <summary>
        /// Write Disk Service Time for this file in us
        /// </summary>
        public ulong DiskWriteTimeInus { get; }

        /// <summary>
        /// Flush Disk Service Time for this file in us
        /// </summary>
        public ulong DiskFlushTimeInus { get; } 

        /// <summary>
        /// Read number of bytes
        /// </summary>
        public ulong ReadSizeInBytes { get; }

        /// <summary>
        /// Write number of bytes
        /// </summary>
        public ulong WriteSizeInBytes { get; }


        /// <summary>
        /// List of involved processes which did touch this file
        /// </summary>
        private ProcessKey[] InvolvedProcesses { get; }

        /// <summary>
        /// Get ETW Processes which did touch this file. A per process metric cannot be made out of this sampled data!
        /// System process will always prefetch or lazy write data so it will take over most not visible IO although it is initiated
        /// by another process which can need to wait for the data to arrive. 
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public ETWProcess[] GetProcesses(IETWExtract source)
        {
           return  InvolvedProcesses.Select(x => x.Pid == 0 ? null : source.GetProcessByPID(x.Pid, x.StartTime)).Where( x=> x!= null ).ToArray();
        }



    }
}
