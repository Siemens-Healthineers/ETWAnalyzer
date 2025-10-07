//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.Extract.Disk
{
    /// <summary>
    /// We group the data in the serialized Json file for space efficiency which is not the most convenient way to query the data. But it can later at runtime
    /// converted into a flat list which then can be more easily processed.
    /// We group things in Json by 
    ///   DiskOrDrive
    ///     FullFileName
    ///         IOType (Read/Write/Flush)
    ///             DiskActivity (contains process, thread, IO prio, Disk Service Time and Size of accessed data)
    /// </summary>
    public class DiskIOData : IDiskIOData
    {
        /// <summary>
        /// Contains data about disk layout, partitions and drive letters
        /// </summary>
        public List<DiskLayout> DiskInformation { get; set; } = new();

        /// <summary>
        /// Contains data about disk layout, partitions and drive letters
        /// </summary>
        IReadOnlyList<IDiskLayout> IDiskIOData.DiskInformation
        {
            get { return DiskInformation; }
        }

        /// <summary>
        /// Dictionary where Key is Drive (C,D, ...) or Disk Id if Drive was not present e.g. for Flush operations
        /// Value is PathData which contains a the file name as key and then a nested dictionary for disk io access type
        /// and finally the summary stats.
        /// </summary>
        public Dictionary<DiskNrOrDrive, PathData> DriveToPath { get; } = new Dictionary<DiskNrOrDrive, PathData>();

        /// <summary>
        /// Sum of Read/Write/Flush Disk Times in microseconds (us = 10^(-6)s)
        /// </summary>
        public ulong TotalDiskServiceTimeInus { get; set; } // public set is needed for serializer!

        /// <summary>
        /// Total Disk Read Time in microseconds (us = 10^(-6)s)
        /// </summary>
        public ulong TotalDiskReadTimeInus { get; set; } // public set is needed for serializer!

        /// <summary>
        /// Total Disk Flush time in microseconds (us = 10^(-6)s) 
        /// </summary>
        public ulong TotalDiskFlushTimeInus { get; set; } // public set is needed for serializer!

        /// <summary>
        /// Total Disk Write Time in microseconds (us = 10^(-6)s) 
        /// </summary>
        public ulong TotalDiskWriteTimeTimeInus { get; set; } // public set is needed for serializer!


        DiskIOEvent[] myCachedEvents;

        /// <summary>
        /// List of Disk IO aggregate events per File
        /// </summary>
        public DiskIOEvent[] DiskIOEvents
        {
            get
            {
                if (myCachedEvents == null)
                {
                    myCachedEvents = CreateDiskIOEvents();
                }

                return myCachedEvents;
            }

        }

        private DiskIOEvent[] CreateDiskIOEvents()
        {
            List<DiskIOEvent> events = new List<DiskIOEvent>();
            foreach (var driveToPath in DriveToPath)
            {
                foreach (var path2Events in driveToPath.Value.FilePathToDiskEvents)
                {
                    string fileName = path2Events.Key;
                    ulong readus = 0;
                    ulong writeus = 0;
                    ulong flushus = 0;
                    ulong readSizeInBytes = 0;
                    ulong writeSizeInBytes = 0;
                    DiskIOPriorities prios = DiskIOPriorities.None;
                    HashSet<ProcessKey> processes = new HashSet<ProcessKey>();

                    foreach (var diskIOActivity in path2Events.Value)
                    {
                        DiskActivity activity = diskIOActivity.Value;
                        prios |= activity.Priorities;
                        foreach (var proc in activity.Processes)
                        {
                            processes.Add(new ProcessKey("NotSet. Query via Id and Start Time in ETWExtract", proc.Key, proc.Value));
                        }

                        switch (diskIOActivity.Key)
                        {
                            case DiskIOTypes.Read:
                                readus += activity.DiskServiceTimeInus;
                                readSizeInBytes += activity.SizeInBytes;
                                break;
                            case DiskIOTypes.Write:
                                writeus += activity.DiskServiceTimeInus;
                                writeSizeInBytes += activity.SizeInBytes;
                                break;
                            case DiskIOTypes.Flush:
                                flushus += activity.DiskServiceTimeInus;
                                fileName = driveToPath.Key.ToString() + " Flush";
                                break;
                            default:
                                throw new InvalidOperationException($"Disk IO type {diskIOActivity.Key} is not known.");
                        }
                    }

                    events.Add(
                        new DiskIOEvent(fileName, readus, writeus, flushus, writeSizeInBytes, readSizeInBytes, prios, processes.ToArray())
                    );
                }
            }

            return events.ToArray();
        }

        internal DiskNrOrDrive GetDiskKey(string fullFileName, uint diskId)
        {
            if (fullFileName?.Length > 0)
            {
                if (fullFileName.StartsWith("Unknown", StringComparison.Ordinal))
                {
                    return DiskNrOrDrive.Unknown;
                }
                else
                {
                    return (DiskNrOrDrive)(Char.ToLowerInvariant(fullFileName[0]) - 'a' + DiskNrOrDrive.A);
                }
            }
            else
            {
                return (DiskNrOrDrive)diskId;
            }
        }
    }
}
