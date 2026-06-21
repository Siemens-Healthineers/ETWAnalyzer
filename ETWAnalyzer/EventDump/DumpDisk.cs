//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Disk;
using ETWAnalyzer.Extract.FileIO;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ETWAnalyzer.Commands.DumpCommand;

namespace ETWAnalyzer.EventDump
{
    class DumpDisk : DumpFileDirBase<DumpDisk.MatchData>
    {
        /// <summary>
        /// File name filter
        /// </summary>
        public Func<string, bool> FileNameFilter { get; internal set; }

        /// <summary>
        /// Show aggregated data up to n directories. Files are omitted, unless DirectoryLevel is >= 100
        /// </summary>
        public int DirectoryLevel { get; internal set; }

        /// <summary>
        /// Show IO per process
        /// </summary>
        public bool IsPerProcess { get; internal set; }

        /// <summary>
        /// Show more columns
        /// </summary>
        public bool ShowDetails { get; internal set; }

        /// <summary>
        /// Merge multiple files together
        /// </summary>
        public bool Merge { get; internal set; }

        /// <summary>
        /// Filter for data for specific file operations
        /// </summary>
        public FileIOStatistics.FileOperation FileOperationValue { get; internal set; }

        /// <summary>
        /// Show file name parts in reverse order
        /// </summary>
        public bool ReverseFileName { get; internal set; }

        /// <summary>
        /// Take topN files based on current sort order
        /// </summary>
        public SkipTakeRange TopN { get; internal set; }

        /// <summary>
        /// Sort files/directories on specific criteria
        /// </summary>
        public DumpCommand.SortOrders SortOrder { get; internal set; }

        /// <summary>
        /// Take top n processes based on current sort order when per process mode is enabled.
        /// </summary>
        public SkipTakeRange TopNProcesses { get; internal set; }
        public MinMaxRange<decimal> MinMaxReadSizeBytes { get; internal set; } = new();
        public MinMaxRange<decimal> MinMaxReadTimeS { get; internal set; } = new();
        public MinMaxRange<decimal> MinMaxWriteSizeBytes { get; internal set; } = new();
        public MinMaxRange<decimal> MinMaxWriteTimeS { get; internal set; } = new();
        public MinMaxRange<decimal> MinMaxTotalTimeS { get; internal set; } = new();
        public MinMaxRange<decimal> MinMaxTotalSizeBytes { get; internal set; } = new();

        const string Col_ReadTime = "ReadTime";
        const string Col_ReadSize = "ReadSize";
        const string Col_ReadThroughput = "ReadThroughput";
        const string Col_WriteTime = "WriteTime";
        const string Col_WriteSize = "WriteSize";
        const string Col_WriteThroughput = "WriteThroughput";
        const string Col_FlushTime = "FlushTime";
        const string Col_PercentActive = "PercentActive";
        const string Col_TotalSize = "TotalSize";
        const string Col_TotalTime = "TotalTime";
        const string Col_FileDir = "FileDir";

        /// <summary>
        /// Valid column names which can be enabled for more flexible output
        /// </summary>
        public static string[] ColumnNames =
        {
            Col_ReadTime, Col_ReadSize, Col_ReadThroughput,
            Col_WriteTime, Col_WriteSize, Col_WriteThroughput,
            Col_FlushTime, Col_PercentActive, Col_TotalSize, Col_TotalTime, Col_FileDir,
        };

        bool GetColumnEnable(string columnName)
        {
            return columnName switch
            {
                Col_ReadTime => GetOverrideFlag(Col_ReadTime, true),
                Col_ReadSize => GetOverrideFlag(Col_ReadSize, true),
                Col_ReadThroughput => GetOverrideFlag(Col_ReadThroughput, true),
                Col_WriteTime => GetOverrideFlag(Col_WriteTime, true),
                Col_WriteSize => GetOverrideFlag(Col_WriteSize, true),
                Col_WriteThroughput => GetOverrideFlag(Col_WriteThroughput, true),
                Col_FlushTime => GetOverrideFlag(Col_FlushTime, true),
                Col_PercentActive => GetOverrideFlag(Col_PercentActive, true),
                Col_TotalSize => GetOverrideFlag(Col_TotalSize, ShowDetails),
                Col_TotalTime => GetOverrideFlag(Col_TotalTime, ShowDetails),
                Col_FileDir => GetOverrideFlag(Col_FileDir, true),
                _ => throw new NotSupportedException($"Column {columnName} is not configurable."),
            };
        }

        internal List<MatchData> myUTestData;

        internal const decimal Million = (1000 * 1000.0m);

        public override List<MatchData> ExecuteInternal()
        {
            List<MatchData> data = ReadDiskData();

            if (IsCSVEnabled)
            {
                OpenCSVWithHeader(Col_CSVOptions, Col_Date, Col_TestCase, Col_TestTimeinms, Col_Baseline, Col_FileName, $"Level{DirectoryLevel}Directory",
                    "DiskTotalTimeIOInus", "DiskReadTimeInus", "DiskWriteTimeInus", "DiskFlushTimeInus", "DiskWrittenBytes", "DiskReadBytes",
                    "DiskReadPerf MB/s", "DiskWritePerf MB/s", "% Active", "DiskNumber", "Processes", "SourceDirectory", Col_SourceJsonFile);
            }

            const int TotalHeadlineWidth = 15;
            const int PercentWidth = 8;

            // group by file or if merge is used do not group at all
            TestDataFile grouping(MatchData data) => Merge ? null : data.DataFile;

            foreach (var byFileOrNoGroup in data.GroupBy(grouping).OrderBy(x => x.Key?.PerformedAt))
            {

                decimal totalReadBytes = 0;
                decimal totalWriteBytes = 0;
                decimal totalReadTimeInus = 0;
                decimal totalWriteTimeInus = 0;
                decimal totalFlushtimeInus = 0;
                decimal percentActive = 0;

                List<MatchData> aggregatedByDirectory = AggregateByDirectory(byFileOrNoGroup.ToList(), DirectoryLevel);
                if (byFileOrNoGroup.Key != null && !IsCSVEnabled)
                {
                    PrintFileName(byFileOrNoGroup.Key.JsonExtractFileWhenPresent, null, byFileOrNoGroup.Key.PerformedAt, byFileOrNoGroup.Key.Extract?.MainModuleVersion?.ToString());
                }

                if(!IsCSVEnabled)
                {
                    bool anyRead = GetColumnEnable(Col_ReadTime) || GetColumnEnable(Col_ReadSize) || GetColumnEnable(Col_ReadThroughput);
                    bool anyWrite = GetColumnEnable(Col_WriteTime) || GetColumnEnable(Col_WriteSize) || GetColumnEnable(Col_WriteThroughput);
                    int readWidth = 1 + (GetColumnEnable(Col_ReadTime) ? 13 : 0) + (GetColumnEnable(Col_ReadSize) ? 11 : 0) + (GetColumnEnable(Col_ReadThroughput) ? 10 : 0) + 1;
                    int writeWidth = 1 + (GetColumnEnable(Col_WriteTime) ? 13 : 0) + (GetColumnEnable(Col_WriteSize) ? 11 : 0) + (GetColumnEnable(Col_WriteThroughput) ? 10 : 0) + 1;
                    string readH = !anyRead ? "" : $"[green]{"Read".WithWidth(-readWidth)}[/green]";
                    string writeH = !anyWrite ? "" : $"[yellow]{"Write".WithWidth(-writeWidth)}[/yellow]";
                    string flushH = !GetColumnEnable(Col_FlushTime) ? "" : "[cyan]Flush       [/cyan] ";
                    string pctH = !GetColumnEnable(Col_PercentActive) ? "" : "[magenta]% Active [/magenta]";
                    string totalH = $"[magenta]{GetTotalHeadline(-1*TotalHeadlineWidth)}[/magenta]";
                    string dirH = !GetColumnEnable(Col_FileDir) ? "" : "[Directory or File if -dirLevel 100 is used";
                    ColorConsole.WriteEmbeddedColorLine($"{readH}{writeH}{flushH}{pctH}{totalH}{dirH}");
                }

                foreach (MatchData group in aggregatedByDirectory.Where(MinMaxFilter).SortAscendingGetTopNLast(SortByValue, null, TopN))
                {
                    totalReadBytes     += group.DiskReadSizeInBytes;
                    totalWriteBytes    += group.DiskWriteSizeInBytes;
                    totalReadTimeInus  += group.DiskReadTimeInus;
                    totalWriteTimeInus += group.DiskWriteTimeInus;
                    totalFlushtimeInus += group.DiskFlushTimeInus;
                    percentActive = 100*((group.DiskReadTimeInus + group.DiskWriteTimeInus + group.DiskFlushTimeInus) / Million) / group.SessionDurationS;

                    if (IsCSVEnabled)
                    {
                        TestDataFile testDataFile = byFileOrNoGroup.Key;
                        WriteCSVLine(CSVOptions, testDataFile.PerformedAt, testDataFile.TestName, testDataFile.DurationInMs, group.BaseLine,
                                        DirectoryLevel >= 100 ? group.RootLevelDirectory : "",  // if DirectoryLevel >= 100 the full file name is RootLevelDirectory
                                        DirectoryLevel >= 100 ? MatchData.GetDirectoryNameSafe(group.RootLevelDirectory) : group.RootLevelDirectory, // Get Directory name if not already grouped by directory
                                        group.DiskTotalTimeInus, group.DiskReadTimeInus, group.DiskWriteTimeInus, group.DiskFlushTimeInus, group.DiskWriteSizeInBytes, group.DiskReadSizeInBytes,
                                        group.ReadMBPerSeconds, group.WriteMBPerSeconds,
                                        Math.Round((double)percentActive, 0, MidpointRounding.AwayFromZero),
                                        group.DiskNumber,
                                        String.Join(";", group.Processes.Select(x => x.GetProcessWithId(UsePrettyProcessName))),
                                        Path.GetDirectoryName(testDataFile.JsonExtractFileWhenPresent), Path.GetFileNameWithoutExtension(testDataFile.JsonExtractFileWhenPresent));
                    }
                    else
                    {
                        bool anyRead = GetColumnEnable(Col_ReadTime) || GetColumnEnable(Col_ReadSize) || GetColumnEnable(Col_ReadThroughput);
                        bool anyWrite = GetColumnEnable(Col_WriteTime) || GetColumnEnable(Col_WriteSize) || GetColumnEnable(Col_WriteThroughput);

                        string diskReadTime = $"{group.DiskReadTimeInus / Million:F5}";
                        string diskReadMB = $"{group.DiskReadSizeInBytes / Million:F0}";
                        string diskWriteTime = $"{group.DiskWriteTimeInus / Million:F5}";
                        string diskWriteMB = $"{group.DiskWriteSizeInBytes / Million:F0}";
                        string diskFlushTime = $"{group.DiskFlushTimeInus / Million:F3}";
                        string diskpercentActive = $"{percentActive:F0} %".WithWidth(PercentWidth);

                        string readPart = !anyRead ? "" : "[green]r" +
                            (GetColumnEnable(Col_ReadTime) ? $" {diskReadTime,10} s" : "") +
                            (GetColumnEnable(Col_ReadSize) ? $" {diskReadMB,7} MB" : "") +
                            (GetColumnEnable(Col_ReadThroughput) ? $" {group.ReadMBPerSeconds,4} MB/s" : "") +
                            "[/green] ";
                        string writePart = !anyWrite ? "" : "[yellow]w" +
                            (GetColumnEnable(Col_WriteTime) ? $" {diskWriteTime,10} s" : "") +
                            (GetColumnEnable(Col_WriteSize) ? $" {diskWriteMB,7} MB" : "") +
                            (GetColumnEnable(Col_WriteThroughput) ? $" {group.WriteMBPerSeconds,4} MB/s" : "") +
                            "[/yellow] ";
                        string flushPart = !GetColumnEnable(Col_FlushTime) ? "" : $"[cyan]f {diskFlushTime,8} s[/cyan] ";
                        string pctPart = !GetColumnEnable(Col_PercentActive) ? "" : $"[magenta]{diskpercentActive} [/magenta]";
                        string totalPart = $"[magenta]{GetTotalValue(group, TotalHeadlineWidth)}[/magenta]";
                        string dirPart = !GetColumnEnable(Col_FileDir) ? "" : $"{DumpFile.GetFileName(group.RootLevelDirectory, ReverseFileName)}";

                        ColorConsole.WriteEmbeddedColorLine($"{readPart}{writePart}{flushPart}{pctPart}{totalPart}{dirPart}");
                    }
                }

                if (!IsCSVEnabled)
                {
                    ColorConsole.WriteEmbeddedColorLine(
                        $"[magenta]Totals {(totalFlushtimeInus + totalReadTimeInus + totalWriteTimeInus) / Million:F2} s {((totalReadBytes + totalWriteBytes) / Million).WithDigitGrouping()} MB[/magenta] " +
                        $"[green]r {totalReadTimeInus / Million:F2} s {(totalReadBytes / Million).WithDigitGrouping()} MB[/green] " +
                        $"[yellow]w {totalWriteTimeInus / Million:F2} s {(totalWriteBytes / Million).WithDigitGrouping()} MB[/yellow] " +
                        $"[cyan]f {totalFlushtimeInus / Million:F2} s[/cyan] " +
                        $"{byFileOrNoGroup.Count()} accessed file/s. Process Count: {new HashSet<ETWProcess>(byFileOrNoGroup.SelectMany(x => x.Processes)).Count}"
                        );
                }
            }


            if (IsPerProcess && !IsCSVEnabled)
            {
                foreach (var byFileOrNoGroup in data.GroupBy(grouping).OrderBy(x => x.Key?.PerformedAt))
                {
                    Console.WriteLine();
                    if (byFileOrNoGroup.Key != null)
                    {
                        PrintFileName(byFileOrNoGroup.Key.JsonExtractFileWhenPresent, null, byFileOrNoGroup.Key.PerformedAt, byFileOrNoGroup.Key.Extract?.MainModuleVersion?.ToString());
                    }
                    {
                        bool anyReadH = GetColumnEnable(Col_ReadTime) || GetColumnEnable(Col_ReadSize) || GetColumnEnable(Col_ReadThroughput);
                        bool anyWriteH = GetColumnEnable(Col_WriteTime) || GetColumnEnable(Col_WriteSize) || GetColumnEnable(Col_WriteThroughput);
                        int readWidthH = 1 + (GetColumnEnable(Col_ReadTime) ? 11 : 0) + (GetColumnEnable(Col_ReadSize) ? 9 : 0) + (GetColumnEnable(Col_ReadThroughput) ? 10 : 0) + 1;
                        int writeWidthH = 1 + (GetColumnEnable(Col_WriteTime) ? 11 : 0) + (GetColumnEnable(Col_WriteSize) ? 9 : 0) + (GetColumnEnable(Col_WriteThroughput) ? 10 : 0) + 1;
                        string readH = !anyReadH ? "" : $"[green]{"Read".WithWidth(-readWidthH)}[/green]";
                        string writeH = !anyWriteH ? "" : $"[yellow]{"Write".WithWidth(-writeWidthH)}[/yellow]";
                        string flushH = !GetColumnEnable(Col_FlushTime) ? "" : "[cyan]Flush         [/cyan]";
                        string totalH = $"[magenta]{GetTotalHeadline(TotalHeadlineWidth)}[/magenta]";
                        string headline = $"{readH}{writeH}{flushH}{totalH}Involved Processes";
                        ColorConsole.WriteEmbeddedColorLine(headline);
                    }

                    List<MatchData> aggregatedByProcess = AggregateByProcess(byFileOrNoGroup.ToList(), UsePrettyProcessName);
                    foreach (var group in aggregatedByProcess.Where(MinMaxFilter).SortAscendingGetTopNLast(SortByValue, null, TopNProcesses))
                    {
                        string procs;
                        // On some files many processes are participating like page file, volume bitmap, ... 
                        // This is not really visible 
                        if (group.Processes.Count > 5)
                        {
                            ColorConsole.WriteEmbeddedColorLine($"Many processes({group.Processes.Count}) accessed the file/s. Cannot attribute to a specific process. Use WPA to get detailed metrics.");
                            continue;
                        }

                        if (group.Processes.Count > 3)
                        {
                            procs = String.Join(";", group.Processes.Select(x => x.ProcessID));
                        }
                        else
                        {
                            procs = String.Join(";", group.Processes.Select(x => x.GetProcessWithId(UsePrettyProcessName)));
                        }

                        string diskReadTime = $"{group.DiskReadTimeInus / Million:F3}";
                        string diskWriteTime = $"{group.DiskWriteTimeInus / Million:F3}";
                        string diskFlushTime = $"{group.DiskFlushTimeInus / Million:F3}";
                        string diskReadSizeInMB = $"{group.DiskReadSizeInBytes / Million:F0}";
                        string diskWriteSizeInMB = $"{group.DiskWriteSizeInBytes / Million:F0}";

                        {
                            bool anyRead = GetColumnEnable(Col_ReadTime) || GetColumnEnable(Col_ReadSize) || GetColumnEnable(Col_ReadThroughput);
                            bool anyWrite = GetColumnEnable(Col_WriteTime) || GetColumnEnable(Col_WriteSize) || GetColumnEnable(Col_WriteThroughput);

                            string readPart = !anyRead ? "" : "[green]r" +
                                (GetColumnEnable(Col_ReadTime) ? $" {diskReadTime,8} s" : "") +
                                (GetColumnEnable(Col_ReadSize) ? $" {diskReadSizeInMB,5} MB" : "") +
                                (GetColumnEnable(Col_ReadThroughput) ? $" {group.ReadMBPerSeconds,4} MB/s" : "") +
                                "[/green] ";
                            string writePart = !anyWrite ? "" : "[yellow]w" +
                                (GetColumnEnable(Col_WriteTime) ? $" {diskWriteTime,8} s" : "") +
                                (GetColumnEnable(Col_WriteSize) ? $" {diskWriteSizeInMB,5} MB" : "") +
                                (GetColumnEnable(Col_WriteThroughput) ? $" {group.WriteMBPerSeconds,4} MB/s" : "") +
                                "[/yellow] ";
                            string flushPart = !GetColumnEnable(Col_FlushTime) ? "" : $"[cyan]f {diskFlushTime,8}[/cyan] s  ";
                            string totalPart = $"[magenta]{GetTotalValue(group,TotalHeadlineWidth)}[/magenta]";

                            ColorConsole.WriteEmbeddedColorLine($"{readPart}{writePart}{flushPart}{totalPart}{procs}");
                        }
                    }
                }
            }

            return data;
        }



        /// <summary>
        /// Used by context sensitive help
        /// </summary>
        static internal readonly DumpCommand.SortOrders[] ValidSortOrders = new[]
        {
            SortOrders.ReadTime,
            SortOrders.WriteTime,
            SortOrders.FlushTime,
            SortOrders.ReadSize,
            SortOrders.WriteSize,
            SortOrders.TotalSize,
            SortOrders.TotalTime,
        };

        string GetTotalHeadline(int minWidth)
        {
            List<string> parts = new();
            if (GetColumnEnable(Col_TotalSize) || (ColumnConfiguration.Count == 0 && SortOrder is SortOrders.Default or SortOrders.TotalSize))
            {
                parts.Add("TotalSize".WithWidth(minWidth));
            }
            if (GetColumnEnable(Col_TotalTime) || (ColumnConfiguration.Count == 0 && SortOrder is SortOrders.TotalTime))
            {
                parts.Add("TotalTime".WithWidth(minWidth));
            }
            return parts.Count > 0 ? String.Join(" ", parts) + " " : "";
        }

        string GetTotalValue(MatchData data, int minWidth)
        {
            List<string> parts = new();
            if (GetColumnEnable(Col_TotalSize) || (ColumnConfiguration.Count == 0 && SortOrder is SortOrders.Default or SortOrders.TotalSize))
            {
                parts.Add($"{(data.DiskWriteSizeInBytes + data.DiskReadSizeInBytes) / Million:F0} MB".WithWidth(minWidth));
            }
            if (GetColumnEnable(Col_TotalTime) || (ColumnConfiguration.Count == 0 && SortOrder is SortOrders.TotalTime or SortOrders.FlushTime))
            {
                parts.Add($"{data.DiskTotalTimeInus / Million:F5} s".WithWidth(minWidth));
            }
            return parts.Count > 0 ? String.Join(" ", parts) + " " : "";
        }


        /// <summary>
        /// Define sort order by SortOrders enum common for all commands
        /// </summary>
        /// <param name="x">disk IO data to sort</param>
        /// <returns>value to sorty by</returns>
        private decimal SortByValue(MatchData x)
        {
            return SortOrder switch
            {
                SortOrders.ReadTime  =>  x.DiskReadTimeInus,
                SortOrders.WriteTime =>  x.DiskWriteTimeInus,
                SortOrders.ReadSize  =>  x.DiskReadSizeInBytes,
                SortOrders.WriteSize =>  x.DiskWriteSizeInBytes,
                SortOrders.Default   =>  (x.DiskWriteSizeInBytes + x.DiskReadSizeInBytes),
                SortOrders.TotalSize =>  (x.DiskWriteSizeInBytes + x.DiskReadSizeInBytes),
                SortOrders.TotalTime =>  x.DiskTotalTimeInus,
                SortOrders.FlushTime =>  x.DiskFlushTimeInus,
                _ =>                     x.DiskTotalTimeInus,
            };
        }

        bool MinMaxFilter(MatchData data)
        {
            bool lret = true;

            lret = MinMaxReadSizeBytes.IsWithin(data.DiskReadSizeInBytes) &&
                   MinMaxWriteSizeBytes.IsWithin(data.DiskWriteSizeInBytes) &&
                   MinMaxTotalSizeBytes.IsWithin((data.DiskWriteSizeInBytes + data.DiskReadSizeInBytes)) &&
                   MinMaxReadTimeS.IsWithin(data.DiskReadTimeInus / Million) &&
                   MinMaxWriteTimeS.IsWithin(data.DiskWriteTimeInus / Million) &&
                   MinMaxTotalTimeS.IsWithin((data.DiskWriteTimeInus + data.DiskReadTimeInus + data.DiskFlushTimeInus) / Million);

          
            return lret;
        }

        static internal List<MatchData> AggregateByDirectory(List<MatchData> data, int level)
        {
            List<MatchData> aggregatedByDirectory = new();

            foreach (var group in data.GroupBy(x => MatchData.GetDirectoryLevel(x.FileName, level)))
            {
                HashSet<ETWProcess> set = new(ETWProcess.CompareOnlyPidCmdLine);
                foreach (var g in group)
                {
                    foreach (var p in g.Processes)
                    {
                        set.Add(p);
                    }
                }

                MatchData groupedData = new()
                {
                    DiskFlushTimeInus = group.Select(x => (decimal)x.DiskFlushTimeInus).Sum(),
                    DiskWriteTimeInus = group.Select(x => (decimal)x.DiskWriteTimeInus).Sum(),
                    DiskReadTimeInus = group.Select(x => (decimal)x.DiskReadTimeInus).Sum(),
                    RootLevelDirectory = group.Key,
                    FileName = "Grouped Data",
                    DiskNumber = group.First().DiskNumber,
                    SourceFileName = String.Join(" ", group.Select(x => x.SourceFileName).ToHashSet().ToArray()),
                    DiskReadSizeInBytes = group.Select(x => (decimal)x.DiskReadSizeInBytes).Sum(),
                    DiskWriteSizeInBytes = group.Select(x => (decimal)x.DiskWriteSizeInBytes).Sum(),
                    SessionDurationS = group.ToLookup(x=>x.SourceFileName).Select(x=>x.First().SessionDurationS).Sum(),
                    Processes = set,
                    BaseLine = group.First().BaseLine,
                };
                aggregatedByDirectory.Add(groupedData);

            }

            return aggregatedByDirectory;
        }

        static internal List<MatchData> AggregateByProcess(List<MatchData> data, bool usePrettyProcessName)
        {
            List<MatchData> aggregatedByProcess = new();
            foreach (var group in data.ToLookup(x => String.Join(";", x.Processes.Select(x => x.GetProcessWithId(usePrettyProcessName)))) )
            {
                HashSet<ETWProcess> set = new(ETWProcess.CompareOnlyPidCmdLine);
                foreach(var g in group)
                {
                    foreach(var p in g.Processes )
                    {
                        set.Add(p);
                    }
                }

                aggregatedByProcess.Add(new MatchData
                {
                    DiskFlushTimeInus = group.Select(x => (decimal)x.DiskFlushTimeInus).Sum(),
                    DiskWriteTimeInus = group.Select(x => (decimal)x.DiskWriteTimeInus).Sum(),
                    DiskReadTimeInus = group.Select(x => (decimal)x.DiskReadTimeInus).Sum(),
                    RootLevelDirectory = group.Key,
                    FileName = "Grouped Data",
                    DiskNumber = group.First().DiskNumber,
                    SourceFileName = String.Join(" ", group.Select(x => x.SourceFileName).ToHashSet().ToArray()),
                    DiskReadSizeInBytes = group.Select(x => (decimal)x.DiskReadSizeInBytes).Sum(),
                    DiskWriteSizeInBytes = group.Select(x => (decimal)x.DiskWriteSizeInBytes).Sum(),
                    SessionDurationS = group.ToLookup(x => x.SourceFileName).Select(x => x.First().SessionDurationS).Sum(),
                    Processes = set
                });
            }

            return aggregatedByProcess;
        }


        List<MatchData> ReadDiskData()
        {
            if( myUTestData != null)
            {
                return myUTestData;
            }

            var lret = new List<MatchData>();

            Lazy<SingleTest>[] runData = GetTestRuns(true, SingleTestCaseFilter, TestFileFilter);
            WarnIfNoTestRunsFound(runData);
            foreach (var test in runData)
            {
                foreach (TestDataFile file in test.Value.Files)
                {
                    if (file.Extract == null || file.Extract.Disk == null)
                    {
                        ColorConsole.WriteError($"File {file.FileName} does not contain disk IO data");
                        continue;
                    }

                    Dictionary<char, int> driveLetterToDiskMap = new();
                    for (int i=0;i<file.Extract?.Disk?.DiskInformation?.Count;i++)
                    {
                        IDiskLayout diskInfo = file.Extract.Disk.DiskInformation[i];
                        foreach(var partition in diskInfo.Partitions)
                        {
                            driveLetterToDiskMap[partition.Drive[0]] = i;
                        }
                    }

                    foreach (var diskEvent in file.Extract.Disk.DiskIOEvents)
                    {
                        if (!FileNameFilter(diskEvent.FileName))
                        {
                            continue;
                        }

                        if ( !diskEvent.GetProcesses(file.Extract).Any(p => IsMatchingProcessAndCmdLine(file, p)))
                        {
                            continue;
                        }

                        if(this.FileOperationValue == FileIOStatistics.FileOperation.Read &&  diskEvent.ReadSizeInBytes == 0)
                        {
                            continue;
                        }

                        if(this.FileOperationValue == FileIOStatistics.FileOperation.Write && diskEvent.WriteSizeInBytes == 0)
                        {
                            continue;
                        }

                        if( !driveLetterToDiskMap.TryGetValue(diskEvent.FileName[0], out int diskNumber) )
                        {
                            if (diskEvent.FileName.StartsWith("Id") && diskEvent.FileName.Length > 2)
                            {
                                // file name contains disk id like Id0, Id1, ... mostly for flush events
                                if ( !int.TryParse(diskEvent.FileName[2..3], out diskNumber))
                                {
                                   diskNumber = -1; // unknown disk
                                }

                            }
                            else
                            {
                                diskNumber = -1; // unknown disk
                            }
                        }


                        lret.Add(new MatchData
                        {
                            SourceFileName = file.FileName,
                            DataFile = file,
                            FileName = diskEvent.FileName,
                            DiskNumber = diskNumber,
                            DiskReadSizeInBytes = diskEvent.ReadSizeInBytes,
                            DiskWriteSizeInBytes = diskEvent.WriteSizeInBytes,
                            DiskReadTimeInus = diskEvent.DiskReadTimeInus,
                            DiskWriteTimeInus = diskEvent.DiskWriteTimeInus,
                            DiskFlushTimeInus = diskEvent.DiskFlushTimeInus,
                            SessionDurationS = (decimal) file.Extract.SessionDuration.TotalSeconds,
                            Processes = diskEvent.GetProcesses(file.Extract).ToHashSet(),
                            BaseLine = file.Extract.MainModuleVersion != null ? file.Extract.MainModuleVersion.ToString() : "",
                        });
                    }
                }
            }
            
            return lret;
        }


        public class MatchData
        {
            static readonly char[] DirectorySeparators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

            public string SourceFileName;
            public string FileName;

            public decimal DiskWriteSizeInBytes;
            public decimal DiskReadSizeInBytes;
            public decimal DiskReadTimeInus;
            public decimal DiskWriteTimeInus;
            public decimal DiskFlushTimeInus;
            public HashSet<ETWProcess> Processes = new();
            public decimal SessionDurationS;

            public int ReadMBPerSeconds => DiskReadTimeInus > 0 ? (int)(DiskReadSizeInBytes / Million / (DiskReadTimeInus / Million)) : 0;
            public int WriteMBPerSeconds => DiskWriteTimeInus > 0 ? (int)(DiskWriteSizeInBytes / Million / (DiskWriteTimeInus / Million)) : 0;

            /// <summary>
            /// Sum of read+write+flush time
            /// </summary>
            public decimal DiskTotalTimeInus { get => DiskReadTimeInus + DiskWriteTimeInus + DiskFlushTimeInus; }


            string myDirectory;
            public string Directory
            {
                get
                {
                    if (myDirectory == null)
                    {
                        myDirectory = FileName.IndexOfAny(DirectorySeparators) == -1 ? FileName : Path.GetDirectoryName(FileName);
                    }

                    return myDirectory;
                }
            }


            /// <summary>
            /// Get the stored directory root which is usually the result of a call to GetDirectoryLevel with some n
            /// </summary>
            public string RootLevelDirectory
            {
                get;set;
            }
            public TestDataFile DataFile { get; internal set; }
            public string BaseLine { get; internal set; }
            public int DiskNumber { get; internal set; }

            internal static string GetDirectoryNameSafe(string filePath)
            {
                string root = filePath;
                try
                {
                    root = Path.GetDirectoryName(filePath); // some file names are invalid
                }
                catch (ArgumentException)
                {
                    root = "invalid file name";
                }
                return root;
            }

            /// <summary>
            /// Get from a file name the directory up to n levels. Level 0 is the drive name, 1 the first subfolder ... 
            /// </summary>
            /// <param name="filePath"></param>
            /// <param name="level">If Level is 100 or greater the file name is returned</param>
            /// <returns></returns>
            internal static string GetDirectoryLevel(string filePath, int level)
            {
                if( level >= 100 )
                {
                    return filePath;
                }

                string rootDirectory = filePath.IndexOfAny(DirectorySeparators) == -1 ? filePath : GetDirectoryNameSafe(filePath);
                string prevRoot = rootDirectory;

                while (rootDirectory != null && GetLevel(rootDirectory) > level)
                {
                    prevRoot = rootDirectory;
                    rootDirectory = Path.GetDirectoryName(rootDirectory);
                }

                return rootDirectory;
            }


            /// <summary>
            /// Get Directory level. 0 is root, c:\dir is 1, ... 
            /// Double \\ count as one level
            /// </summary>
            /// <param name="dir"></param>
            /// <returns></returns>
            internal static int GetLevel(string dir)
            {
                dir = dir.TrimEnd(DirectorySeparators);
                int count = 0;
                int startIdx = -1;
                do
                {
                    if (startIdx >= dir.Length)
                    {
                        break;
                    }

                    startIdx = dir.IndexOfAny(DirectorySeparators, startIdx + 1);
                    if (startIdx != -1)
                    {
                        count++;
                    }

                    // do not count subsequent \\ as extra level
                    while (startIdx != -1 && startIdx < dir.Length && DirectorySeparators.Contains(dir[startIdx]))
                    {
                        startIdx++;
                    }
                }
                while (startIdx != -1);

                return count;
            }
        }
    }
}
