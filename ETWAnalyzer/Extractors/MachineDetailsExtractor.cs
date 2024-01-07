//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Configuration;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.CPU;
using ETWAnalyzer.Extract.CPU.Extended;
using ETWAnalyzer.Extract.Disk;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Symbol;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Metadata;
using Microsoft.Windows.EventTracing.Processes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace ETWAnalyzer.Extractors
{
    class MachineDetailsExtractor : ExtractorBase
    {
        IPendingResult<IProcessDataSource> myProcesses;
        IPendingResult<ISystemMetadata> myMetaData;
        ITraceMetadata myTraceMetaData;
        IPendingResult<IMarkDataSource> myETWMarks;
        SpecialEventsParser mySpecialEvents = new SpecialEventsParser();

        public MachineDetailsExtractor()
        {
        }



        public override void RegisterParsers(ITraceProcessor processor)
        {
            myProcesses = processor.UseProcesses();
            myMetaData = processor.UseSystemMetadata();
            myTraceMetaData = processor.UseMetadata();
            myETWMarks = processor.UseMarks();
            mySpecialEvents.RegisterSpecialEvents(processor);
        }



        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            var meta = myMetaData.Result;
            results.OSName = $"{meta?.BuildInfo?.ProductName}";
            results.OSVersion = myTraceMetaData?.OSVersion ?? new Version();
            results.OSBuild = meta?.BuildInfo?.Branch ?? "";
            results.MemorySizeMB = (int)(meta?.UsableMemorySize.TotalMegabytes ?? 0);

            results.NumberOfProcessors = meta?.ProcessorCount ?? 0;
            results.CPUSpeedMHz = (int)(meta?.ProcessorSpeed.TotalMegahertz ?? 0);


            Dictionary<CPUNumber, CPUTopology> cpuTopology = new();
            try
            {
                IProcessor proc = meta?.Processors?.FirstOrDefault();
                if (proc != null)
                {
                    results.CPUName = proc.Name?.Trim();
                    results.CPUVendor = proc.Vendor?.Trim();
                    results.CPUHyperThreadingEnabled = proc.IsSimultaneousMultithreadingEnabled;

                    
                    for(int i=0;i<meta.Processors.Count;i++) 
                    {
                        int nominalMHz = 0;
                        if( meta.Processors[i].NominalFrequency.HasValue )
                        {
                            nominalMHz = (int) meta.Processors[i].NominalFrequency.Value.TotalMegahertz;
                        }
                        cpuTopology[(CPUNumber)i] = new CPUTopology
                        {
                            NominalFrequencyMHz = nominalMHz,
                            RelativePerformancePercentage = (int) (meta.Processors[i]?.RelativePerformance?.Value ?? 100),
                            EfficiencyClass = (EfficiencyClass) ( meta.Processors[i]?.EfficiencyClass ?? 0),
                        };
                    }

                    results.CPU = new CPUStats(results?.CPU?.PerProcessCPUConsumptionInMs, results?.CPU?.PerProcessMethodCostsInclusive, results?.CPU?.TimeLine, cpuTopology, null);
                }
            }
            catch (InvalidTraceDataException ex)
            {
                Logger.Warn($"Could not determine number of Processors. This most likely happens when in VMs less cores are enabled then the CPU model has. Exception was: {ex}");
            }

            // Microsoft.Windows.EventTracing.Interop.Metadata.NativeTraceLogfileHeader exposes BootTime but the API of TraceProcessor does not expose this ...
            // Asked at https://stackoverflow.com/questions/61996791/expose-boottime-in-traceprocessor
            //results.BootTimeMachine = 

            results.ComputerName = meta?.Name ?? "";
            results.Model = meta?.Model ?? "";
            results.AdDomain = meta?.DomainName ?? "";
            results.IsDomainJoined = meta?.IsDomainJoined ?? false;
            results.UsedExtractOptions = Environment.CommandLine;
            results.SourceETLFileName = myTraceMetaData?.TracePath ?? "";
            results.SessionStart = myTraceMetaData?.StartTime ?? DateTimeOffset.MinValue;
            results.SessionEnd = myTraceMetaData?.StopTime ?? DateTimeOffset.MaxValue;

            if (mySpecialEvents.BootTimeUTC.HasValue && results.SessionStart != default)
            {
                DateTime localTime = new DateTime(mySpecialEvents.BootTimeUTC.Value.Ticks, DateTimeKind.Unspecified) + results.SessionStart.Offset;
                results.BootTime = new DateTimeOffset(localTime, results.SessionStart.Offset);
                results.TraceHeader = new TraceHeader
                {
                    BufferSize = mySpecialEvents.BufferSize,
                    BuffersLost = mySpecialEvents.BuffersLost,
                    BuffersWritten = mySpecialEvents.BuffersWritten,
                    EventsLost = mySpecialEvents.EventsLost,
                    LogFileMode = mySpecialEvents.LogFileModeNice,
                    MajorVersion = mySpecialEvents.MajorVersion,
                    MaximumFileSizeMB = mySpecialEvents.MaximumFileSizeMB,
                    MinorVersion = mySpecialEvents.MinorVersion,
                    ProviderVersion = mySpecialEvents.ProviderVersion,
                    SubMinorVersion = mySpecialEvents.SubMinorVersion,
                    SubVersion = mySpecialEvents.SubVersion,
                    TimerResolution = mySpecialEvents.TimerResolution,
                };
            }

            ExtractDisplayInformation(meta, results);
            ExtractRunningProcesses(myProcesses.Result, results);
            ExtractBuildVersions(myProcesses.Result, results);
            ExtractDiskInfo(meta.Disks, results);

            ExtractMarks(results);
        }

        private void ExtractDiskInfo(IReadOnlyList<IDisk> disks, ETWExtract extract)
        {
            if( disks == null )
            {
                return;
            }

            extract.Disk = new DiskIOData();
            DiskLayout currentDisk = new DiskLayout();

           

            foreach(IDisk disk in disks)
            {
                DiskType? type = null;
                try
                {
                    type = disk.Type;
                }
                catch(InvalidOperationException)  // When Disk Rundowndata is missing it can throw: InvalidOperationException: The item does not have any data. from Microsoft.Windows.EventTracing.Metadata.UnknownDisk.get_Type()
                {
                    continue;
                }

                currentDisk.Type = disk.Type switch
                {
                    null => DiskTypes.Unknown,
                    DiskType.HDD => DiskTypes.HDD,
                    DiskType.SSD => DiskTypes.SSD,
                    _ => DiskTypes.Unknown,
                };

                currentDisk.TracksPerCylinder = disk.TracksPerCylinder;
                currentDisk.SectorsPerTrack = disk.SectorsPerTrack;
                currentDisk.CapacityGiB =  disk.Capacity.TotalGibibytes;
                currentDisk.CylinderCount = disk.CylinderCount;
                currentDisk.SectorSizeBytes = disk.SectorSize.Bytes;
                currentDisk.Model = disk.Model;
                currentDisk.IsWriteCachingEnabled = disk.IsWriteCachingEnabled;

                DiskPartition currentPartition = new();

                foreach (var partition in disk.Partitions)
                {
                    if( !partition.HasData )
                    {
                        continue;
                    }

                    currentPartition.FileSystem = (FileSystemFormat) partition.FileSystem;
                    currentPartition.Drive = partition.DriveLetter.ToString();
                    currentPartition.FreeSizeGiB = partition.FreeCapacity.TotalGibibytes;
                    currentPartition.TotalSizeGiB = partition.UsedCapacity.TotalGibibytes + partition.FreeCapacity.TotalGibibytes;
                    currentDisk.Partitions.Add(currentPartition);
                    currentPartition = new DiskPartition();
                }

                extract.Disk.DiskInformation.Add(currentDisk);

                currentDisk = new DiskLayout();
            }
        }

        private void ExtractDisplayInformation(ISystemMetadata system, ETWExtract results)
        {
            if (system.DisplayAdapters != null)
            {
                foreach (var disp in system.DisplayAdapters)
                {
                    results.Displays.Add(new Display
                    {
                        ColorDepth = (disp?.Display?.ColorDepth).GetValueOrDefault(),
                        HorizontalResolution = (disp?.Display?.HorizontalResolution).GetValueOrDefault(),
                        VerticalResolution = (disp?.Display?.VerticalResolution).GetValueOrDefault(),
                        IsPrimaryDevice = (disp?.Display?.IsPrimaryDevice).GetValueOrDefault(),
                        RefreshRateHz = (disp?.Display?.RefreshRate).GetValueOrDefault().Hertz,
                        DisplayName = disp.DisplayName,
                        GraphicsCardMemorySizeMiB = (long)disp.MemorySize.TotalMebibytes,
                        GraphicsCardChipName = disp.ChipName,
                    });
                }
            }
        }

        private void ExtractMarks(ETWExtract results)
        {
            List<ETWMark> marks = new();
            if (myETWMarks.HasResult)
            {
                foreach (IMark mark in myETWMarks.Result.Marks)
                {
                    marks.Add(new ETWMark(mark.Timestamp.DateTimeOffset, mark.Label));
                }
            }
            results.ETWMarks = marks;
        }

        class DateTimeFileVersion
        {
            public string FileName { get; set; }

            public Version FileVersion { get; set; }

            public override string ToString()
            {
                return $"{FileName} {FileVersion}";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileVersionTraceData"></param>
        /// <returns></returns>
        static DateTimeFileVersion ConvertToDateTimeFileVersion(IImage fileVersionTraceData)
        {
            string fileVersion = fileVersionTraceData.FileVersion;
            if (String.IsNullOrWhiteSpace(fileVersion))
            {
                return null;
            }

            try
            {
                // major, minor, build, and revision
                Version v = new(fileVersion.Split(new char[] { ' ' })[0]);
                int buildYear = GetHundredThousandDigitsAsNumber(v.Build);
                int buildMonth = GetLastTwoDigits(v.Build);
                int buildDay = GetHundredThousandDigitsAsNumber(v.Revision);

                if (v.Major >= 0 && v.Major < 20 &&   // We start with version 1 and expect as long as this software is alive no more than 20 versions
                    buildYear >= 0 && buildYear < 40 &&  // As year we expect > 2000 as start year and no more than 2040.
                    buildMonth <= 12 &&   // Day and Month must be valid values
                    buildDay <= 31)
                {
                    return new DateTimeFileVersion
                    {
                        FileName = fileVersionTraceData.OriginalFileName.Replace(".ni.", "."),
                        FileVersion = fileVersionTraceData.FileVersionNumber,
                    };
                }
            }
            catch (Exception)
            { }

            return null;
        }

        /// <summary>
        /// Get from a Version object the build data which is encoded in the FileVersion. 
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        internal static DateTime GetBuildDate(Version version)
        {
            int buildYear = 2000 + GetHundredThousandDigitsAsNumber(version.Build);
            int buildMonth = GetLastTwoDigits(version.Build);
            int buildDay = GetHundredThousandDigitsAsNumber(version.Revision);
            if (buildYear > 2000 && buildMonth > 0 && buildMonth < 13 && buildDay > 0 && buildDay < 32)
            {
                return new DateTime(buildYear, buildMonth, buildDay);
            }
            else
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Shave off of a number abcd the digits cd
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static int GetLastTwoDigits(int value)
        {
            return value % 10 + 10 * ((value / 10) % 10);
        }

        /// <summary>
        /// Shave off of a number abcd the digits and return ab as value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static int GetHundredThousandDigitsAsNumber(int value)
        {
            return (value / 100);
        }

        /// <summary>
        /// Ensure that mapping table is only read once from disk to prevent file io issues due to concurrent access to the file.
        /// </summary>
        static readonly Lazy<DllToBuildMappings> myMappings = new(LoadBuildMappings);

        static DllToBuildMappings LoadBuildMappings()
        {
            using var cfgFile = File.OpenRead(ConfigFiles.DllToBuildMapFile);
            DllToBuildMappings mappings = DllToBuildMappings.Deserialize(cfgFile);
            return mappings;
        }

        /// <summary>
        /// Extract from the loaded dlls the File versions which encodes the build date.
        /// The file version format is MainVersion.Minor.yymm.ddbb
        /// where yy is the last two digits of the year, mm is the month, dd is the day and bb is the build number
        /// </summary>
        /// <param name="processes">data source</param>
        /// <param name="results">Will fill ModuleVersions array</param>
        internal void ExtractBuildVersions(IProcessDataSource processes, ETWExtract results)
        {
            DateTimeFileVersion[] syngoVersions = processes.Processes.SelectMany(x => x.Images)
                .Where(i => (i.FileName?.Contains("syngo")).GetValueOrDefault())
                .Select(ConvertToDateTimeFileVersion)
                .Where(x => x != null)
                .ToArray();

            ILookup<Version, DateTimeFileVersion> versions = syngoVersions.ToLookup(x => x.FileVersion);
            List<ModuleVersion> modules = new();

            foreach (var file in syngoVersions.OrderBy(x => x.FileVersion))
            {
                string module = myMappings.Value.GetModulePath(file.FileName);
                if (module != null)
                {
                    // if .ni.dll and .dll are checked we can end up for one dll with multiple module version entries
                    // skip module version if we have already one with the same version
                    // Still allow multiple versions if they are different which indicates that multiple
                    // executables were running code from different directories in different versions
                    if (!modules.Any(x => x.Module == module && x.Version == file.FileVersion.ToString()))
                    {
                        modules.Add(new ModuleVersion
                        {
                            Version = file.FileVersion.ToString(),
                            Module = module,
                            ModuleFile = file.FileName,
                        });
                    }
                }
            }

            results.MainModuleVersion = modules.OrderByDescending(x => GetBuildDate(new Version(x.Version))).FirstOrDefault();
            results.ModuleVersions = modules.ToArray();
        }

        /// <summary>
        /// Get relevant process information from etl file
        /// </summary>
        /// <param name="processes"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        private void ExtractRunningProcesses(IProcessDataSource processes, ETWExtract results)
        {
            var _listProcess = new List<ETWProcess>();

            Dictionary<string, string> translateMap = new();

            foreach (var data in processes.Processes)
            {
#pragma warning disable CA1416
                string userSid = data.User?.Value ?? "";
                if (!translateMap.TryGetValue(userSid, out string userName))
                {
                    try
                    {
                        userName = new System.Security.Principal.SecurityIdentifier(userSid).Translate(typeof(System.Security.Principal.NTAccount)).ToString();
#pragma warning restore CA1416
                    }
                    catch // this works only for well known sids and local users if extractions is done on same machine, but not for sids of other machines.
                    {
                        userName = userSid;  // if user name could not be resolved we use the sid 
                    }

                    translateMap[userSid] = userName;
                }

                var process = new ETWProcess()
                {
                    ProcessID = data.Id,
                    ProcessName = data.ImageName,
                    CmdLine = data.CommandLine,
                    StartTime = data.CreateTime != null ? data.CreateTime.Value.DateTimeOffset : DateTimeOffset.MinValue,
                    EndTime = data.ExitTime != null ? data.ExitTime.Value.DateTimeOffset : DateTimeOffset.MaxValue,
                    IsNew = data.CreateTime != null,
                    ReturnCode = data.ExitCode,
                    ParentPid = data.ParentId,
                    SessionId = data.SessionId,
                    HasEnded = data.ExitTime != null,
                    Identity = userName,
                };
                _listProcess.Add(process);
            }

            results.Processes = _listProcess;
        }
    }
}
