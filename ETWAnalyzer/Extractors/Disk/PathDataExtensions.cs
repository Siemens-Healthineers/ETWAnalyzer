//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract.Disk;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing.Disk;
using System;
using System.Collections.Generic;

namespace ETWAnalyzer.Extractors.Disk
{
    internal static class PathDataExtensions
    {
        static readonly Dictionary<Microsoft.Windows.EventTracing.Processes.IOPriority, DiskIOPriorities> IOToDiskIOProritiesMap = new Dictionary<Microsoft.Windows.EventTracing.Processes.IOPriority, DiskIOPriorities>
        {
            { Microsoft.Windows.EventTracing.Processes.IOPriority.VeryLow, DiskIOPriorities.VeryLow },
            { Microsoft.Windows.EventTracing.Processes.IOPriority.Low, DiskIOPriorities.Low },
            { Microsoft.Windows.EventTracing.Processes.IOPriority.Normal, DiskIOPriorities.Normal },
            { Microsoft.Windows.EventTracing.Processes.IOPriority.High, DiskIOPriorities.High },
            { Microsoft.Windows.EventTracing.Processes.IOPriority.Critical, DiskIOPriorities.Critical }
        };


        public static void Add(this IDiskActivity diskActivity, DiskIOData data)
        {
            DiskNrOrDrive drive = data.GetDiskKey(diskActivity.Path, diskActivity.Disk);

            if (!data.DriveToPath.TryGetValue(drive, out PathData pathData))
            {
                pathData = new PathData();
                data.DriveToPath[drive] = pathData;
            }

            pathData.Add(diskActivity);

            ulong diskIOTimeInUs = (ulong)diskActivity.DiskServiceDuration.TotalMicroseconds;
            data.TotalDiskServiceTimeInus += diskIOTimeInUs;
            switch (diskActivity.IOType)
            {
                case DiskIOType.Flush:
                    data.TotalDiskFlushTimeInus += diskIOTimeInUs;
                    break;
                case DiskIOType.Read:
                    data.TotalDiskReadTimeInus += diskIOTimeInUs;
                    break;
                case DiskIOType.Write:
                    data.TotalDiskWriteTimeTimeInus += diskIOTimeInUs;
                    break;
                default:
                    throw new NotSupportedException($"Unknown IOType {diskActivity.IOType} encountered.");
            }
        }

        /// <summary>
        /// Add disk activity event to this instance
        /// </summary>
        /// <param name="data"></param>
        /// <param name="diskActivity"></param>
        public static void Add(this PathData data, IDiskActivity diskActivity)
        {
            // When IO Type is flush we have not file name. In that case replace path with IOType as file name
            string pathOrIOType = diskActivity.Path ?? diskActivity.IOType.ToString();

            if (!data.FilePathToDiskEvents.TryGetValue(pathOrIOType, out Dictionary<DiskIOTypes, DiskActivity> activity))
            {
                activity = new Dictionary<DiskIOTypes, DiskActivity>();
                data.FilePathToDiskEvents[pathOrIOType] = activity;
            }

            if (!activity.TryGetValue((DiskIOTypes)diskActivity.IOType, out DiskActivity localDiskData))
            {
                localDiskData = new DiskActivity();
                activity[(DiskIOTypes)diskActivity.IOType] = localDiskData;
            }

            uint threadId = 0;
            uint processId = 0;
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
                    startTime = diskActivity.IssuingProcess.CreateTime.Value.ConvertToTime();
                }
            }

            localDiskData.Add(processId, startTime, threadId, IOToDiskIOProritiesMap[diskActivity.Priority], (ulong) diskActivity.DiskServiceDuration.TotalMicroseconds, (ulong)diskActivity.Size.Bytes);
        }
    }
}
