//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Disk;
using Microsoft.Windows.EventTracing.Processes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        internal void Add(IDiskActivity diskActivity)
        {
            DiskNrOrDrive drive = GetDiskKey(diskActivity.Path, diskActivity.Disk);

            if (!DriveToPath.TryGetValue(drive, out PathData pathData))
            {
                pathData = new PathData();
                DriveToPath[drive] = pathData;
            }

            pathData.Add(diskActivity);

            ulong diskIOTimeInUs = (ulong)diskActivity.DiskServiceDuration.TotalMicroseconds;
            TotalDiskServiceTimeInus += diskIOTimeInUs;
            switch (diskActivity.IOType)
            {
                case DiskIOType.Flush:
                    TotalDiskFlushTimeInus += diskIOTimeInUs;
                    break;
                case DiskIOType.Read:
                    TotalDiskReadTimeInus += diskIOTimeInUs;
                    break;
                case DiskIOType.Write:
                    TotalDiskWriteTimeTimeInus += diskIOTimeInUs;
                    break;
                default:
                    throw new NotSupportedException($"Unknown IOType {diskActivity.IOType} encountered.");
            }
        }

        DiskNrOrDrive GetDiskKey(string fullFileName, int diskId)
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

    /// <summary>
    /// 
    /// </summary>
    public class PathData
    {
        static readonly Dictionary<IOPriority, DiskIOPriorities> IOToDiskIOProritiesMap = new Dictionary<IOPriority, DiskIOPriorities>
        {
            { IOPriority.VeryLow, DiskIOPriorities.VeryLow },
            { IOPriority.Low, DiskIOPriorities.Low },
            { IOPriority.Normal, DiskIOPriorities.Normal },
            { IOPriority.High, DiskIOPriorities.High },
            { IOPriority.Critical, DiskIOPriorities.Critical }
        };

        /// <summary>
        /// Map of file names which contains a dictionary of access types (Read/Write/Flush) to DiskActivity metrics (size, duration, processes ...)
        /// </summary>
        public Dictionary<string, Dictionary<DiskIOTypes, DiskActivity>> FilePathToDiskEvents
        {
            get;
        } = new Dictionary<string, Dictionary<DiskIOTypes, DiskActivity>>();

        /// <summary>
        /// Add disk activity event to this instance
        /// </summary>
        /// <param name="diskActivity"></param>
        internal void Add(IDiskActivity diskActivity)
        {
            // When IO Type is flush we have not file name. In that case replace path with IOType as file name
            string pathOrIOType = diskActivity.Path ?? diskActivity.IOType.ToString();

            if (!FilePathToDiskEvents.TryGetValue(pathOrIOType, out Dictionary<DiskIOTypes, DiskActivity> activity))
            {
                activity = new Dictionary<DiskIOTypes, DiskActivity>();
                FilePathToDiskEvents[pathOrIOType] = activity;
            }

            if (!activity.TryGetValue((DiskIOTypes)diskActivity.IOType, out DiskActivity localDiskData))
            {
                localDiskData = new DiskActivity();
                activity[(DiskIOTypes)diskActivity.IOType] = localDiskData;
            }

            int threadId = 0;
            int processId = 0;
            DateTimeOffset startTime = DateTimeOffset.MinValue;
            if (diskActivity.IssuingThread != null)
            {
                threadId = diskActivity.IssuingThread.Id;
            }
            if (diskActivity.IssuingProcess != null)
            {
                processId = diskActivity.IssuingProcess.Id;
                if (diskActivity.IssuingProcess.CreateTime.HasValue)
                {
                    startTime = diskActivity.IssuingProcess.CreateTime.Value.DateTimeOffset;
                }
            }

            localDiskData.Add(processId, startTime, threadId, IOToDiskIOProritiesMap[diskActivity.Priority], diskActivity.DiskServiceDuration, (ulong)diskActivity.Size.Bytes);
        }
    }


}
