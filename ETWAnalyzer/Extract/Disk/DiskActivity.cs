//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Microsoft.Windows.EventTracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.Disk
{

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum DiskIOPriorities
    {
        /// <summary>
        /// Used by indexing service
        /// </summary>
        VeryLow = 0,

        /// <summary>
        /// Default value
        /// </summary>
        None  = 0,

        /// <summary>
        /// Used by indexing service
        /// </summary>
        Low = 2,
        /// <summary>
        /// 
        /// </summary>
        Normal = 4,
        /// <summary>
        /// 
        /// </summary>
        High = 8,
        /// <summary>
        /// 
        /// </summary>
        Critical = 16
    }

    /// <summary>
    /// 
    /// </summary>
    public enum DiskIOTypes
    {
        /// <summary>
        /// 
        /// </summary>
        Read = 0,
        /// <summary>
        /// 
        /// </summary>
        Write = 1,
        /// <summary>
        /// 
        /// </summary>
        Flush = 2
    }

    /// <summary>
    /// 
    /// </summary>
    public class DiskActivity
    {
        /// <summary>
        /// Threads which did access this file
        /// </summary>
        public HashSet<uint> ThreadIDs { get; } = new HashSet<uint>();

        /// <summary>
        /// Processes which did issue disk IO requests
        /// </summary>
        public HashSet<KeyValuePair<uint,DateTimeOffset>> Processes { get; } = new HashSet<KeyValuePair<uint, DateTimeOffset>>();

        /// <summary>
        /// Bitmask with which IO priorities the file was accessed. Normally it should be only one prio Normal
        /// </summary>
        public DiskIOPriorities Priorities { get; set; }  // public set is needed for serializer!

        /// <summary>
        /// Time who long the Disk did need to service the request
        /// </summary>
        public ulong DiskServiceTimeInus { get; set; } // public set is needed for serializer!

        /// <summary>
        /// Read/Write number of bytes
        /// </summary>
        public ulong SizeInBytes { get; set; } // public set is needed for serializer!

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="startTime"></param>
        /// <param name="threadId"></param>
        /// <param name="priority"></param>
        /// <param name="diskServiceDuration"></param>
        /// <param name="sizeInBytes"></param>
        public void Add(uint pid, DateTimeOffset startTime, uint threadId, DiskIOPriorities priority, Duration diskServiceDuration, ulong sizeInBytes)
        {
            ThreadIDs.Add( threadId );
            Processes.Add( new KeyValuePair<uint,DateTimeOffset>(pid, startTime) );
            DiskServiceTimeInus += (ulong)diskServiceDuration.TotalMicroseconds;
            Priorities |= priority;
            SizeInBytes += (ulong)sizeInBytes;
        }
    }
}
