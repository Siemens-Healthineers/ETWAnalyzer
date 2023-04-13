//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.FileIO;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ETWAnalyzer.Commands.DumpCommand;
using static ETWAnalyzer.Extract.FileIO.FileIOStatistics;

namespace ETWAnalyzer.EventDump
{
    /// <summary>
    /// Dump File IO Data to console or to a CSV
    /// </summary>
    class DumpFile : DumpFileDirBase<DumpFile.MatchData>
    {
        public Func<string, bool> FileNameFilter { get; internal set; }

        public int DirectoryLevel { get; internal set; }
        public bool IsPerProcess { get; internal set; }

        public bool Merge { get; internal set; }
        public bool ShowAllFiles { get; internal set; }
        public MinMaxRange<decimal> MinMaxReadSizeBytes { get; internal set; } = new();
        public MinMaxRange<decimal> MinMaxReadTimeS { get; internal set; } = new();
        public MinMaxRange<decimal> MinMaxWriteSizeBytes { get; internal set; } = new();
        public MinMaxRange<decimal> MinMaxWriteTimeS { get; internal set; } = new();
        public MinMaxRange<decimal> MinMaxTotalTimeS { get; internal set; } = new();
        public MinMaxRange<decimal> MinMaxTotalSizeBytes { get; internal set; } = new();
        public MinMaxRange<decimal> MinMaxTotalCount { get; internal set; } = new();
        public FileOperation FileOperationValue { get; internal set; }
        public DumpCommand.SortOrders SortOrder { get; internal set; }
        public bool NoCmdLine { get; internal set; }
        public bool ShowDetails { get; internal set; }
        public bool ReverseFileName { get; internal set; }
        public TotalModes ShowTotal { get; internal set; }

        /// <summary>
        /// Show per process totals but skip headers and grouped output
        /// </summary>
        bool IsPerProcessTotal
        {
            get => ShowTotal == TotalModes.Process;
        }

        /// <summary>
        /// Show only summary per file
        /// </summary>
        bool IsTotalMode
        {
            get => ShowTotal == TotalModes.Total;
        }

        /// <summary>
        /// Show everything, but show totals at the end
        /// </summary>
        bool IsFileTotalMode
        {
            get => ShowTotal == TotalModes.File;
        }

        /// <summary>
        /// Take topn files based on current sort order
        /// </summary>
        public SkipTakeRange TopN { get; internal set; }

        /// <summary>
        /// Take topn processes based on current sort order when per process mode is enabled where file IO is printed for each process
        /// </summary>
        public SkipTakeRange TopNProcesses { get; internal set; }

        /// <summary>
        /// Some file name events are not recorded. These files are normally from start/end of trace and usually of no interest and of low volume
        /// If you need to look at them anyway use the -ShowAllFiles switch to see unknown file events as well.
        /// </summary>
        const string UnknownFileName = "Unknown";


        static readonly char[] PathSplitChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        static readonly string PathSplitCharAsString = new(Path.DirectorySeparatorChar, 1);
        static readonly FileOperation[] ReadWriteFilter = new FileOperation[] { FileOperation.Read, FileOperation.Write };
        static readonly FileOperation[] ReadWriteOpenCloseFilter = new FileOperation[] { FileOperation.Read, FileOperation.Write, FileOperation.Open, FileOperation.Close };

        internal List<MatchData> myUTestData;

        internal const decimal Million = (1000 * 1000.0m);
        internal const decimal Kilo = 1024.0m;

        public override List<MatchData> ExecuteInternal()
        {
            List<MatchData> data = ReadFileData();

            if (IsCSVEnabled)
            {
                OpenCSVWithHeader(Col_CSVOptions, Col_Date, "InputDirectory", Col_SourceJsonFile, Col_TestCase, Col_TestTimeinms, Col_Baseline, Col_ProcessName, Col_Process, Col_StartTime, Col_CommandLine,
                                  "File Directory", Col_FileName,
                                  "Open Count", "Open Duration us", "Open Status",
                                  "Close Count", "Close Duration us",
                                  "Read Count", "Read Duration us", "Read Accessed Bytes", "Read MaxFilePosition",
                                  "Write Count", "Write Duration us", "Write Accessed Bytes", "Write MaxFilePosition",
                                  "SetSecurity Count",
                                  "SetSecurity Times",
                                  "File Delete Count",
                                  "File Rename Count",
                                  "File Open Close Time Duration (us)"
                                  );

                foreach (var fileIO in data)
                {
                    var stats = fileIO.Stats;
                    string times = String.Join(";", stats?.SetSecurity?.Times?.Select(x => base.GetDateTimeString(x, fileIO.SessionStart, TimeFormatOption)) ?? Array.Empty<string>());
                    WriteCSVLine(CSVOptions, fileIO.DataFile.PerformedAt, Path.GetDirectoryName(fileIO.DataFile.FileName), Path.GetFileNameWithoutExtension(fileIO.DataFile.FileName), fileIO.DataFile.TestName, fileIO.DataFile.DurationInMs,
                                fileIO.BaseLine, fileIO.Process.GetProcessName(UsePrettyProcessName), fileIO.Process.GetProcessWithId(UsePrettyProcessName), fileIO.Process.StartTime, NoCmdLine ? "" : fileIO.Process.CommandLineNoExe,
                                Path.GetDirectoryName(fileIO.FileName), Path.GetFileName(fileIO.FileName),
                                stats?.Open?.Count, stats?.Open?.Durationus, String.Join(" ", stats?.Open?.NtStatus?.Select(x => ((NtStatus)x).ToString()) ?? Enumerable.Empty<string>()),
                                stats?.Close?.Count, stats?.Close?.Durationus,
                                stats?.Read?.Count, stats?.Read?.Durationus, stats?.Read?.AccessedBytes, stats?.Read?.MaxFilePosition,
                                stats?.Write?.Count, stats?.Write?.Durationus, stats?.Write?.AccessedBytes, stats?.Write?.MaxFilePosition,
                                stats?.SetSecurity?.Times?.Count,
                                times,
                                stats?.Delete?.Count,
                                stats?.Rename?.Count,
                                stats?.Open.Durationus + stats?.Close.Durationus
                        );
                }
            }
            else
            {
                PrintSummary(data);
            }

            return data;
        }


        private void PrintSummary(List<MatchData> data)
        {
            // group by file or if merge is used do not group at all
            TestDataFile grouping(MatchData data) => Merge ? null : data.DataFile;

            foreach (var group in data.GroupBy(grouping).OrderBy(x => x?.Key?.PerformedAt))
            {
                if (group.Key != null)
                {
                    PrintFileName(group.Key.JsonExtractFileWhenPresent, null, group.Key.PerformedAt, group.Key.Extract.MainModuleVersion?.ToString());
                }

                List<MatchData> perFile = group.ToList();

                List<MatchData> aggregatedByDirectory = AggregateByDirectory(perFile, DirectoryLevel);
                PrintData(aggregatedByDirectory);
            }
        }

        void PrintData(List<MatchData> aggregatedByDirectory)
        {
            HashSet<ETWProcess> processes = new();

            // group by process if requested or put all processes into one group
            ETWProcess grouping(MatchData data) => IsPerProcess ?  data.Processes.Single() : null;

            // sort groups based on their ordering by summing up their totals first
            decimal ordering(IGrouping<ETWProcess, MatchData> data) => data.Sum(GetSortValue);
            bool bPrintHeader = true;

            const int sortOrderColumnWidth = 23;
            string sortOrderHeadline = GetSortHeadline(SortOrder);
            decimal dynamicColumnTotal = 0m;

            if(sortOrderHeadline != null)
            {
                sortOrderHeadline = sortOrderHeadline.WithWidth(sortOrderColumnWidth-1) + " ";
            }

            int printedFiles = 0;

            decimal totalFileReadTimeInus = 0.0m;
            decimal totalFileReadSizeInBytes = 0.0m;
            long totalFileReadCount = 0;

            decimal totalFileWriteTimeInus = 0.0m;
            decimal totalFileWriteSizeInBytes = 0.0m;
            decimal totalFileOpenCloseTimeInus = 0.0m;
            long totalFileWriteCount = 0;

            long totalFileOpenCount = 0;
            long totalFileCloseCount = 0;
            long totalFileRenameCount = 0;
            long totalFileDeleteCount = 0;
            long totalFileSetSecurityCount = 0;
            int totalFileCount = 0;

                // sort ascending by r+w Size by default or supplied filter and order
            foreach (var group in aggregatedByDirectory.Where(MinMaxFilter).GroupBy(grouping).SortAscendingGetTopNLast(ordering, null, TopNProcesses))
            {
                bool bPrintOnce = true;

                decimal totalPerProcessDynamicColumn = 0.0m;
                decimal totalPerProcessFileReadTimeInus = 0.0m;
                decimal totalPerProcessFileReadSizeInBytes = 0.0m;
                long totalPerProcessFileReadCount = 0;

                decimal totalPerProcessFileWriteTimeInus = 0.0m;
                decimal totalPerProcessFileWriteSizeInBytes = 0.0m;
                decimal totalPerProcessFileOpenCloseTimeInus = 0.0m;
                long totalPerProcessFileWriteCount = 0;

                long totalPerProcessFileOpenCount = 0;
                long totalPerProcessFileCloseCount = 0;
                long totalPerProcessFileRenameCount = 0;
                long totalPerProcessFileDeleteCount = 0;
                long totalPerProcessFileSetSecurityCount = 0;

                long totalPerProcessFileCount = 0;

                // then sort inside the grouping again
                foreach (var fileEvent in group.Where(MinMaxFilter).SortAscendingGetTopNLast(GetSortValue, null, TopN))
                {
                    KeyValuePair<string, decimal> columnData = ExtractSortValueAsString(sortOrderHeadline, fileEvent);

                    totalPerProcessFileReadTimeInus += fileEvent.FileReadTimeInus;
                    totalPerProcessFileReadSizeInBytes += fileEvent.FileReadSizeInBytes;
                    totalPerProcessFileReadCount += fileEvent.FileReadCount;
                    totalPerProcessFileWriteTimeInus += fileEvent.FileWriteTimeInus;
                    totalPerProcessFileWriteSizeInBytes += fileEvent.FileWriteSizeInBytes;
                    totalPerProcessFileOpenCloseTimeInus += fileEvent.FileOpenTimeInus + fileEvent.FileCloseTimeInus;
                    totalPerProcessFileWriteCount += fileEvent.FileWriteCount;
                    totalPerProcessFileOpenCount += fileEvent.FileOpenCount;

                    totalPerProcessFileCloseCount += fileEvent.FileCloseCount;
                    totalPerProcessFileRenameCount += fileEvent.FileRenameCount;
                    totalPerProcessFileDeleteCount += fileEvent.FileDeleteCount;
                    totalPerProcessFileSetSecurityCount += fileEvent.FileSetSecurityCount;
                    totalPerProcessDynamicColumn = AggregateDynamicColumn (totalPerProcessDynamicColumn, columnData.Value);
                    totalPerProcessFileCount += fileEvent.InputFileCountUsedForGrouping;
                    totalFileCount += fileEvent.InputFileCountUsedForGrouping;

                    if (bPrintHeader && !IsPerProcessTotal && !IsTotalMode)
                    {
                        if (ShowDetails)
                        {
                            ColorConsole.WriteEmbeddedColorLine($"[magenta]{sortOrderHeadline}[/magenta][green]Read (Size, MaxFilePos, Duration, Throughput, Count)[/green][yellow]              Write (Size, MaxFilePos, Duration, Throughput, Count)[/yellow][cyan]              Open+Close Duration, Open, Close, SetSecurity Count, Del Count, Rename Count[/cyan]          Directory or File if -dirLevel 100 is used");
                        }
                        else
                        {
                            ColorConsole.WriteEmbeddedColorLine($"[magenta]{sortOrderHeadline}[/magenta][green]Read (Size, Duration, Count)[/green][yellow]        Write (Size, Duration, Count)[/yellow][cyan]         Open+Close Duration, Open, Close[/cyan]        Directory or File if -dirLevel 100 is used");
                        }
                        bPrintHeader = false;
                    }

                    if (IsPerProcess && bPrintOnce && !IsTotalMode)
                    {
                        ColorConsole.WriteEmbeddedColorLine($"[grey]{group.Key.GetProcessWithId(UsePrettyProcessName)}{GetProcessTags(group.Key, group.First().SessionStart)}[/grey] {(NoCmdLine ? "" : group.Key.CmdLine)}", ConsoleColor.DarkCyan);
                        bPrintOnce = false;
                    }

                    string fileReadTime = $"{fileEvent.FileReadTimeInus / Million:F5}";
                    string fileReadKB = $"{fileEvent.FileReadSizeInBytes / Kilo:N0}";
                    string fileReadMaxPosKB = $"{fileEvent.FileReadMaxPos / Kilo:N0}";
                    string fileWriteTime = $"{fileEvent.FileWriteTimeInus / Million:F5}";
                    string fileWriteMaxPos = $"{fileEvent.FileWriteMaxFilePos / Kilo:N0}";
                    string fileWriteKB = $"{fileEvent.FileWriteSizeInBytes / Kilo:N0}";
                    string fileOpenCloseTime = $"{ (fileEvent.FileOpenTimeInus + fileEvent.FileCloseTimeInus) / Million:F5}";

                    decimal readMBPerSeconds = (fileEvent.FileReadSizeInBytes / Million) / ((fileEvent.FileReadTimeInus + 1.0m) / Million);
                    decimal writeMBPerSeconds = (fileEvent.FileWriteSizeInBytes / Million) / ((fileEvent.FileWriteTimeInus + 1.0m) / Million);


                    // suppress details when total mode is total which shows only per file totals, or per process totals
                    if ( ( !IsTotalMode && !IsPerProcessTotal ) || IsFileTotalMode )
                    {
                        string totalSortOrderCell = String.IsNullOrEmpty(columnData.Key) ? "" : $"{columnData.Key,sortOrderColumnWidth - 1} ";

                        dynamicColumnTotal = AggregateDynamicColumn(dynamicColumnTotal,columnData.Value);

                        if (ShowDetails)
                        {
                            ColorConsole.WriteEmbeddedColorLine($"[magenta]{totalSortOrderCell}[/magenta][green]r {fileReadKB,12} KB {fileReadMaxPosKB,12} KB {fileReadTime,10} s {(int)readMBPerSeconds,5} MB/s {fileEvent.FileReadCount,4} [/green] [yellow]w {fileWriteKB,12} KB {fileWriteMaxPos,12} KB {fileWriteTime,10} s {(int)writeMBPerSeconds,5} MB/s {fileEvent.FileWriteCount,4} [/yellow] [cyan] O+C {fileOpenCloseTime,10} s Open: {fileEvent.FileOpenCount,4} Close: {fileEvent.FileCloseCount,4} SetSecurity: {fileEvent.FileSetSecurityCount,3} Del: {fileEvent.FileDeleteCount,3}, Ren: {fileEvent.FileRenameCount,3}[/cyan] {GetFileName(fileEvent.RootLevelDirectory, ReverseFileName)}");
                        }
                        else
                        {
                            ColorConsole.WriteEmbeddedColorLine($"[magenta]{totalSortOrderCell}[/magenta][green]r {fileReadKB,12} KB {fileReadTime,10} s {fileEvent.FileReadCount,4}[/green] [yellow]w {fileWriteKB,12} KB {fileWriteTime,10} s {fileEvent.FileWriteCount,4} [/yellow] [cyan] O+C {fileOpenCloseTime,10} s Open: {fileEvent.FileOpenCount,4} Close: {fileEvent.FileCloseCount,4}[/cyan] {GetFileName(fileEvent.RootLevelDirectory, ReverseFileName)}");
                        }
                    }
                    printedFiles++;
                    processes.UnionWith(fileEvent.Processes);
                }

                // Show Process totals
                if( IsPerProcess && (IsPerProcessTotal || IsFileTotalMode) )
                {
                    string dynamicPerProcessColumntotalString = FormatDynamicColumnTotal(totalPerProcessDynamicColumn);
                    dynamicPerProcessColumntotalString = sortOrderHeadline != null ? $"{dynamicPerProcessColumntotalString,sortOrderColumnWidth-1} " : "";

                    string fileReadKB = $"{totalPerProcessFileReadSizeInBytes / Kilo:N0}";
                    string fileReadTimeS = $"{totalPerProcessFileReadTimeInus / Million:F5}";
                    string fileWriteKB = $"{totalPerProcessFileWriteSizeInBytes / Kilo:N0}";
                    string fileWriteTimeS = $"{totalPerProcessFileWriteTimeInus / Million:F5}";
                    string fileOpenCloseTimeS = $"{totalPerProcessFileOpenCloseTimeInus/ Million:F5}";
                    string fileTotalTimeS = $"{(totalPerProcessFileReadTimeInus + totalPerProcessFileWriteTimeInus + totalPerProcessFileOpenCloseTimeInus) / Million:F5}";

                    if (ShowDetails)
                    {
                        ColorConsole.WriteEmbeddedColorLine($"[cyan]{dynamicPerProcessColumntotalString}[/cyan][red]r {fileReadKB,12} KB {fileReadTimeS,10} s {totalPerProcessFileReadCount,4}[/red] [magenta]w {fileWriteKB,12} KB {fileWriteTimeS,10} s {totalPerProcessFileWriteCount,4} [/magenta] [yellow] O+C {fileOpenCloseTimeS,10} s Open: {totalPerProcessFileOpenCount,4} Close: {totalPerProcessFileCloseCount,4} SetSecurity: {totalPerProcessFileSetSecurityCount,4} Del: {totalPerProcessFileDeleteCount,3}, Ren: {totalPerProcessFileRenameCount,3}[/yellow] [magenta]TotalTime: {fileTotalTimeS} s[/magenta] Process Total with {totalPerProcessFileCount} accessed files");
                    }
                    else
                    {
                        ColorConsole.WriteEmbeddedColorLine($"[cyan]{dynamicPerProcessColumntotalString}[/cyan][red]r {fileReadKB,12} KB {fileReadTimeS,10} s {totalPerProcessFileReadCount,4}[/red] [magenta]w {fileWriteKB,12} KB {fileWriteTimeS,10} s {totalPerProcessFileWriteCount,4} [/magenta] [yellow] O+C {fileOpenCloseTimeS,10} s Open: {totalPerProcessFileOpenCount,4} Close: {totalPerProcessFileCloseCount,4}[/yellow] [magenta]TotalTime: {fileTotalTimeS} s[/magenta] Process Total with {totalPerProcessFileCount} accessed files");
                    }
                }

                totalFileReadTimeInus += totalPerProcessFileReadTimeInus;
                totalFileReadSizeInBytes += totalPerProcessFileReadSizeInBytes;
                totalFileReadCount += totalPerProcessFileReadCount;
                totalFileWriteTimeInus += totalPerProcessFileWriteTimeInus;
                totalFileWriteSizeInBytes += totalPerProcessFileWriteSizeInBytes;
                totalFileOpenCloseTimeInus += totalPerProcessFileOpenCloseTimeInus;
                totalFileWriteCount += totalPerProcessFileWriteCount;

                totalFileOpenCount += totalPerProcessFileOpenCount;
                totalFileCloseCount += totalPerProcessFileCloseCount;
                totalFileRenameCount += totalPerProcessFileRenameCount;
                totalFileDeleteCount += totalPerProcessFileDeleteCount;
                totalFileSetSecurityCount += totalPerProcessFileSetSecurityCount;
            }

            // Show per file totals always
            {
                string dynamicTotalString = FormatDynamicColumnTotal(dynamicColumnTotal);
                dynamicTotalString = sortOrderHeadline != null ?  $"{dynamicTotalString, sortOrderColumnWidth-1} " : "";

                string fileReadKB = $"{totalFileReadSizeInBytes / Kilo:N0}";
                string fileReadTimeS = $"{totalFileReadTimeInus / Million:F5}";
                string fileWriteKB = $"{totalFileWriteSizeInBytes / Kilo:N0}";
                string fileWriteTimeS = $"{totalFileWriteTimeInus / Million:F5}";
                string fileOpenCloseTimeS = $"{totalFileOpenCloseTimeInus / Million:F5}";
                string fileTotalTimeS = $"{(totalFileReadTimeInus + totalFileWriteTimeInus + totalFileOpenCloseTimeInus) / Million:F5}";

                if (ShowDetails)
                {
                    ColorConsole.WriteEmbeddedColorLine($"[cyan]{dynamicTotalString}[/cyan][red]r {fileReadKB,12} KB {fileReadTimeS,10} s {totalFileReadCount,6}[/red] [magenta]w {fileWriteKB,12} KB {fileWriteTimeS,10} s {totalFileWriteCount,6} [/magenta] [yellow] O+C {fileOpenCloseTimeS,10} s Open: {totalFileOpenCount,6} Close: {totalFileCloseCount,6} SetSecurity: {totalFileSetSecurityCount,6} Del: {totalFileDeleteCount,5}, Ren: {totalFileRenameCount,5}[/yellow] [magenta]TotalTime: {fileTotalTimeS} s[/magenta] File/s Total with {totalFileCount} accessed file/s. Process Count: {processes.Count}");
                }
                else
                {
                    ColorConsole.WriteEmbeddedColorLine($"[cyan]{dynamicTotalString}[/cyan][red]r {fileReadKB,12} KB {fileReadTimeS,10} s {totalFileReadCount,6}[/red] [magenta]w {fileWriteKB,12} KB {fileWriteTimeS,10} s {totalFileWriteCount,6} [/magenta] [yellow] O+C {fileOpenCloseTimeS,10} s Open: {totalFileOpenCount,6} Close: {totalFileCloseCount,6}[/yellow] [magenta]TotalTime: {fileTotalTimeS} s[/magenta] File/s Total with {totalFileCount} accessed file/s. Process Count: {processes.Count}");
                }
            }
        }

        /// <summary>
        /// Combine values from sort column. It is the sum for all sort orders except length
        /// where the maximum file position of the largest file is used.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <returns>Sum or max(oldValue,newValue) for Length sort order</returns>
        decimal AggregateDynamicColumn(decimal oldValue, decimal newValue)
        {
            decimal result = SortOrder switch
            {
                SortOrders.Length => Math.Max(oldValue, newValue),
                _ => oldValue + newValue,
            };

            return result;
        }

        /// <summary>
        /// Print totals for sort column if data is not already present in other columns
        /// </summary>
        /// <param name="totalValue">total value</param>
        /// <returns>stringified total with units attached or an empty string if the total is already part of other columns.</returns>
        string FormatDynamicColumnTotal(decimal totalValue)
        {
            string total = SortOrder switch
            {
                SortOrders.Length => $"{totalValue / Kilo:N0} KB",
                SortOrders.OpenCloseTime => "",  // already part of summary
                SortOrders.ReadSize => "",       // already part of summary
                SortOrders.ReadTime => "",       // already part of summary
                SortOrders.Size => $"{totalValue / Kilo:N0} KB",
                SortOrders.Time => "",          // already part of summary
                SortOrders.Count => $"{totalValue:N0}",
                SortOrders.TotalCount => $"{totalValue:N0}",
                SortOrders.TotalSize => $"{totalValue/ Kilo:N0} KB",
                SortOrders.TotalTime => "",      // already part of summary
                SortOrders.WriteSize => "",      // already part of summary
                SortOrders.WriteTime => "",      // already part of summary
                _ => ""
            };

            return total;
        }

        /// <summary>
        /// Get from MatchData dynamic column data by which is sorted.
        /// </summary>
        /// <param name="headline">If null then we have no data to return</param>
        /// <param name="data">Actua</param>
        /// <returns>KVP with the column data string as key and the raw numerical data as value.</returns>
        internal KeyValuePair<string, decimal> ExtractSortValueAsString(string headline, MatchData data)
        {
            if (headline == null) // no colum present we do not need to get any data
            {
                return new KeyValuePair<string, decimal>("", 0.0m);
            }

            // SortOrder Column strings, count, times, etc
            long? totalSortOrderCount = null;
            long? totalSortOrderPos = null;
            decimal? totalSortOrderTimeInus = null;
            decimal? totalSortOrderSizeInBytes = null;


            // switch case for SortOrder Column for console output for specific fileoperationvalue
            switch (SortOrder)
            {
                case SortOrders.Count:
                case SortOrders.TotalCount:
                    totalSortOrderCount = FileOperationValue switch
                    {
                        FileOperation.Read => 0u,   // already printed 
                        FileOperation.Write => 0u,  // already printed
                        FileOperation.Open => 0u,   // already printed
                        FileOperation.Close => 0u,  // already printed
                        FileOperation.SetSecurity => ShowDetails ? 0u : data.FileSetSecurityCount,   // do not show duplicate column data
                        FileOperation.Delete => ShowDetails ? 0u : data.FileDeleteCount,             // do not show duplicate column data
                        FileOperation.Rename => ShowDetails ? 0u : data.FileRenameCount,             // do not show duplicate column data
                        FileOperation.All => data.FileReadCount + data.FileWriteCount + data.FileOpenCount + data.FileCloseCount +
                                             data.FileSetSecurityCount + data.FileDeleteCount + data.FileRenameCount,
                        _ => 0u,
                    };
                    break;
                case SortOrders.Time:
                    totalSortOrderTimeInus = FileOperationValue switch
                    {
                        FileOperation.Read => 0u,  // already printed 
                        FileOperation.Write => 0u, // already printed 
                        FileOperation.Open => 0u,  // already printed 
                        FileOperation.Close => 0u, // already printed 
                        _ => // FileOperation.SetSecurity FileOperation.Delete FileOperation.Rename 
                            totalSortOrderTimeInus = data.FileReadTimeInus + data.FileWriteTimeInus + data.FileOpenTimeInus + data.FileCloseTimeInus,
                    };
                    break;
                case SortOrders.Size:
                    totalSortOrderSizeInBytes = FileOperationValue switch
                    {
                        FileOperation.Read => 0m,   // already printed 
                        FileOperation.Write => 0m,  // already printed 
                        FileOperation.Open => 0m,   // has no data
                        FileOperation.Close => 0m,  // has no data
                        FileOperation.SetSecurity => 0m, // has no data
                        FileOperation.Delete => 0m,      // has no data
                        FileOperation.Rename => 0m,      // has no data
                        FileOperation.All => data.FileReadSizeInBytes + data.FileWriteSizeInBytes,
                        _ => ThrowNotSupportedException(),
                    }; ;
                    break;
                case SortOrders.Length:
                    totalSortOrderPos = FileOperationValue switch
                    {
                        FileOperation.Read => ShowDetails ? 0u : data.FileReadMaxPos,       // do not show duplicate column data
                        FileOperation.Write => ShowDetails ? 0u : data.FileWriteMaxFilePos,  // do not show duplicate column data
                        FileOperation.Open => 0u,
                        FileOperation.Close => 0u,
                        FileOperation.SetSecurity => 0u,
                        FileOperation.Delete => 0u,
                        FileOperation.Rename => 0u,
                        FileOperation.All => Math.Max(data.FileReadMaxPos, data.FileWriteMaxFilePos),
                        _ => ThrowNotSupportedException(),
                    };
                    break;
                case SortOrders.ReadSize:
                    // always printed
                    break;
                case SortOrders.WriteSize:
                    // always printed
                    break;
                case SortOrders.TotalSize:
                    totalSortOrderSizeInBytes = data.FileReadSizeInBytes + data.FileWriteSizeInBytes;
                    break;
                case SortOrders.TotalTime:
                    totalSortOrderTimeInus = data.TotalFileTime;
                    break;
                case SortOrders.ReadTime:
                    // always printed
                    break;
                case SortOrders.WriteTime:
                    // always printed
                    break;
                case SortOrders.OpenCloseTime:
                    totalSortOrderTimeInus = data.FileReadTimeInus + data.FileWriteTimeInus;
                    break;
            }

            // sortorder column per file totals, or per process totals as a string
            string totalSortOrderCell = null;
            if (totalSortOrderTimeInus != null)
            {
                totalSortOrderCell = $"{totalSortOrderTimeInus.Value / Million:F5}" + " s";
            }
            else if (totalSortOrderSizeInBytes != null)
            {
                totalSortOrderCell = $"{totalSortOrderSizeInBytes.Value / Kilo:N0}" + " KB";
            }
            else if (totalSortOrderPos != null)
            {
                totalSortOrderCell = $"{totalSortOrderPos.Value / Kilo:N0}" + " KB";
            }
            else if (totalSortOrderCount != null)
            {
                totalSortOrderCell = $"{totalSortOrderCount.Value}";
            }

            return new KeyValuePair<string, decimal>(totalSortOrderCell, totalSortOrderTimeInus.GetValueOrDefault() + totalSortOrderSizeInBytes.GetValueOrDefault() + totalSortOrderPos.GetValueOrDefault() + totalSortOrderCount.GetValueOrDefault());
        }

        string GetSortHeadline(SortOrders sortOrder)
        {
            return sortOrder switch
            {
                SortOrders.Count => FileOperationValue switch
                {
                    FileOperation.All => "Total Count",
                    FileOperation.Close => null,  // already printed
                    FileOperation.Delete => ShowDetails ? null : "Delete Count",   // only visible when -Details is not used
                    FileOperation.Rename => ShowDetails ? null : "Rename Count", // only visible when -Details is not used
                    FileOperation.SetSecurity => ShowDetails ? null : "SetSecurity Count", // only visible when -Details is not used
                    FileOperation.Write => null,   // already printed
                    _ => null,
                },
                SortOrders.ReadSize => null, // already printed,
                SortOrders.Size => FileOperationValue switch
                {
                    FileOperation.All => "Read+Write Size",
                    FileOperation.Close => null,
                    FileOperation.Delete => null,
                    FileOperation.Open => null,
                    FileOperation.Read => null, // already printed
                    FileOperation.Rename => null,
                    FileOperation.SetSecurity => null,
                    FileOperation.Write => null, // already printed
                    _ => null,
                },
                SortOrders.Length => FileOperationValue switch
                {
                    FileOperation.All => "Max(Read,Write) Position",
                    FileOperation.Close => null,
                    FileOperation.Delete => null,
                    FileOperation.Open => null,
                    FileOperation.Read => ShowDetails ? null : "MaxReadPos",
                    FileOperation.Rename => null,
                    FileOperation.SetSecurity => null,
                    FileOperation.Write => ShowDetails ? null : "MaxWritePos",
                    _ => null,
                },
                SortOrders.OpenCloseTime => FileOperationValue switch
                {
                    FileOperation.All => "Open + Close Time",
                    _ => null,
                },
                SortOrders.ReadTime => null, // already printed
                SortOrders.Time => GetSortHeadline(SortOrders.TotalTime),
                SortOrders.TotalCount => GetSortHeadline(SortOrders.Count),
                SortOrders.TotalSize => GetSortHeadline(SortOrders.Size),
                SortOrders.TotalTime => FileOperationValue switch
                {
                    FileOperation.All => "Total Time",
                    _ => null,
                },
                SortOrders.WriteSize => null, // already printed
                SortOrders.WriteTime => null, // already printed
                _ => null,
            };
        }

        /// <summary>
        /// Reverse a file name by printing the file name and directories in reverse order
        /// </summary>
        /// <param name="fileName">Input file name</param>
        /// <param name="reverse">If true file name is printed in reverse order</param>
        /// <returns>Original file name if reverse is false, if true reversed string.</returns>
        internal static string GetFileName(string fileName, bool reverse)
        {
            if(fileName != null && reverse)
            {
                string[] parts = fileName.Split(PathSplitChars);
                return String.Join(PathSplitCharAsString, parts.Reverse());
            }
            else
            {
                return fileName;
            }
        }

        private List<MatchData> AggregateByDirectory(List<MatchData> data, int level)
        {
            List<MatchData> aggregatedByDirectory = new();

            ETWProcess noGrouping = null;
            ETWProcess byProcessOrNot(MatchData data) => IsPerProcess ? data.Process : noGrouping;
            foreach (var processGroup in data.GroupBy(byProcessOrNot))
            {
                HashSet<ETWProcess> processes = processGroup.Select(x => x.Process).ToHashSet();

                foreach (var group in processGroup.GroupBy(x => DumpDisk.MatchData.GetDirectoryLevel(x.FileName, level)))
                {
                    decimal fileOpenTimeInus = 0;
                    decimal fileCloseTimeInus = 0;
                    decimal fileWriteTimeInus = 0;
                    decimal fileReadTimeInus = 0;
                    decimal fileReadSizeInBytes = 0;
                    long fileReadMaxPos = 0;
                    decimal fileWriteSizeInBytes = 0;
                    long fileWriteMaxFilePos = 0;
                    long fileOpenCount = 0;
                    long fileCloseCount = 0;
                    long fileWriteCount = 0;
                    long fileReadCount = 0;
                    long fileSetSecurityCount = 0;
                    long fileDeleteCount = 0;
                    long fileRenameCount = 0;
                    FileIOStatistics stats = null;
                    long fileOpenCloseTimeInus = 0;

                    // calculate aggregates
                    foreach (var match in group)
                    {
                        stats = match.Stats;
                        if (stats != null)
                        {
                            fileOpenTimeInus += (decimal)     (stats.Open?.Durationus).GetValueOrDefault();
                            fileCloseTimeInus += (decimal)    (stats.Close?.Durationus).GetValueOrDefault();
                            fileWriteTimeInus += (decimal)    (stats.Write?.Durationus).GetValueOrDefault();
                            fileReadTimeInus += (decimal)     (stats.Read?.Durationus).GetValueOrDefault();
                            fileReadSizeInBytes += (decimal)  (stats.Read?.AccessedBytes).GetValueOrDefault();
                            fileReadMaxPos        = Math.Max( (long) (stats.Read?.MaxFilePosition).GetValueOrDefault(), fileReadMaxPos);
                            fileWriteMaxFilePos   = Math.Max((long)(stats.Write?.MaxFilePosition).GetValueOrDefault(), fileWriteMaxFilePos);
                            fileWriteSizeInBytes += (decimal) (stats.Write?.AccessedBytes).GetValueOrDefault();
                            fileOpenCount += (long)           (stats.Open?.Count).GetValueOrDefault();
                            fileCloseCount += (long)          (stats.Close?.Count).GetValueOrDefault();
                            fileWriteCount += (long)          (stats.Write?.Count).GetValueOrDefault();
                            fileReadCount += (long)           (stats.Read?.Count).GetValueOrDefault();
                            fileSetSecurityCount += (long)    (stats.SetSecurity?.Times?.Count).GetValueOrDefault();
                            fileDeleteCount += (long)         (stats.Delete?.Count).GetValueOrDefault();
                            fileRenameCount += (long)         (stats.Rename?.Count).GetValueOrDefault();
                            fileOpenCloseTimeInus += (long)   (stats.Open?.Durationus).GetValueOrDefault() + (stats.Close?.Durationus).GetValueOrDefault();
                        }
                    }

                    MatchData groupedData = new()
                    {
                        FileOpenTimeInus = fileOpenTimeInus,
                        FileCloseTimeInus = fileCloseTimeInus,
                        FileWriteTimeInus = fileWriteTimeInus,
                        FileReadTimeInus = fileReadTimeInus,
                        FileReadSizeInBytes = fileReadSizeInBytes,
                        FileReadMaxPos = fileReadMaxPos,
                        FileWriteSizeInBytes = fileWriteSizeInBytes,
                        FileWriteMaxFilePos = fileWriteMaxFilePos,
                        FileOpenCount = fileOpenCount,
                        FileCloseCount = fileCloseCount,
                        FileWriteCount = fileWriteCount,
                        FileReadCount = fileReadCount,
                        FileSetSecurityCount = fileSetSecurityCount,
                        FileDeleteCount = fileDeleteCount,
                        FileRenameCount = fileRenameCount,
                        Processes = processes,
                        RootLevelDirectory = group.Key,
                        FileName = "Grouped Data",
                        InputFileCountUsedForGrouping = group.Count(),
                        SourceFileName = String.Join(" ", group.Select(x => x.SourceFileName).ToHashSet().ToArray()),
                        BaseLine = String.Join(",", group.Select(x => x.BaseLine).ToHashSet()),
                        SessionStart = (group.FirstOrDefault()?.SessionStart).GetValueOrDefault(),
                    };

                   
                    aggregatedByDirectory.Add(groupedData);
                }
            }

            return aggregatedByDirectory;
        }

        internal FileIOStatistics RemoveStatsFromColumn(FileIOStatistics stats)
        {
            switch (FileOperationValue)
            {
                case FileOperation.Open:
                    stats.Close = null;
                    stats.Write = null;
                    stats.Read = null;
                    stats.SetSecurity = null;
                    stats.Delete = null;
                    stats.Rename = null;
                    break;
                case FileOperation.Close:
                    stats.Open = null;
                    stats.Write = null;
                    stats.Read = null;
                    stats.SetSecurity = null;
                    stats.Delete = null;
                    stats.Rename = null;
                    break;
                case FileOperation.SetSecurity:
                    stats.Close = null;
                    stats.Write = null;
                    stats.Read = null;
                    stats.Open = null;
                    stats.Delete = null;
                    stats.Rename = null;
                    break;
                case FileOperation.All:
                    break;
                case FileOperation.Write:
                    stats.Close = null;
                    stats.Open = null;
                    stats.Read = null;
                    stats.SetSecurity = null;
                    stats.Delete = null;
                    stats.Rename = null;
                    break;
                case FileOperation.Read:
                    stats.Close = null;
                    stats.Write = null;
                    stats.Open = null;
                    stats.SetSecurity = null;
                    stats.Delete = null;
                    stats.Rename = null;
                    break;
                case FileOperation.Delete:
                    stats.Close = null;
                    stats.Write = null;
                    stats.Read = null;
                    stats.SetSecurity = null;
                    stats.Open = null;
                    stats.Rename = null;
                    break;
                case FileOperation.Rename:
                    stats.Close = null;
                    stats.Write = null;
                    stats.Read = null;
                    stats.SetSecurity = null;
                    stats.Delete = null;
                    stats.Open = null;
                    break;
            }
            return stats;
        }


        /// <summary>
        /// Read FileIO data from ETWExtract from extracted data files.
        /// Filter by file name, process, cmd line and skip by default Unkown file names
        /// </summary>
        /// <returns></returns>
        List<MatchData> ReadFileData()
        {
            if (myUTestData != null)
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
                    if (file.Extract == null || file.Extract.FileIO == null)
                    {
                        ColorConsole.WriteError($"File {file.FileName} does not contain File IO data");
                        continue;
                    }

                    foreach (FileIOContainer fileEvent in file.Extract.FileIO.GetFileNameProcessStats(file.Extract).OrderByDescending(x=>x.TotalIODurationus))
                    {
                        if (!FileNameFilter(fileEvent.FileName))
                        {
                            continue;
                        }

                        if( fileEvent.Process.ProcessName == null )
                        {
                            continue;
                        }

                        // filter events by operation
                        if (FileOperationValue != FileOperation.All && !fileEvent.Stats.HasOperation(FileOperationValue))
                        {
                            continue;
                        }

                        if (!IsMatchingProcessAndCmdLine(file, fileEvent.Process.ToProcessKey()) )
                        {
                            continue;
                        }

                        if( !ShowAllFiles && fileEvent.FileName.StartsWith(UnknownFileName))
                        {
                            continue;
                        }

                        lret.Add(new MatchData
                        {
                            SourceFileName = file.FileName,
                            FileName = fileEvent.FileName,
                            Stats = RemoveStatsFromColumn(fileEvent.Stats),
                            Process = fileEvent.Process,
                            DataFile = file,
                            BaseLine = file.Extract.MainModuleVersion != null ? file.Extract.MainModuleVersion.ToString() : "",
                            SessionStart = file.Extract.SessionStart,
                        });
                    }
                }
            }

            return lret;
        }

        /// <summary>
        /// Get summary data based on filter and sort order to enable sorting based only on the filtered data
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Value on which data is summed up per grouping which is then used as sort order inside the grouping</returns>
        internal decimal GetSortValue(MatchData data)
        {
            decimal lret = 0M;

            switch (SortOrder)
            {
                case SortOrders.Count:
                case SortOrders.TotalCount:
                    lret = FileOperationValue switch
                    {
                        FileOperation.Close => data.FileCloseCount,
                        FileOperation.Open => data.FileOpenCount,
                        FileOperation.Read => data.FileReadCount,
                        FileOperation.Write => data.FileWriteCount,
                        FileOperation.SetSecurity => data.FileSetSecurityCount,
                        FileOperation.All => data.FileCloseCount + data.FileOpenCount + data.FileReadCount + data.FileWriteCount + data.FileSetSecurityCount + 
                                                data.FileDeleteCount + data.FileRenameCount,
                        FileOperation.Delete => data.FileDeleteCount,
                        FileOperation.Rename => data.FileRenameCount,
                        _ => throw new NotSupportedException($"File Operation sort not yet implemented for value: {FileOperationValue}"),
                    };
                    break;
                case SortOrders.ReadSize:
                    if( FileOperationValue != FileOperation.All && FileOperationValue != FileOperation.Read)
                    {
                        throw new ArgumentException($"The -FileOperation {FileOperationValue} is not valid. You can either use All or Read.");
                    }
                    lret = data.FileReadSizeInBytes;
                    break;
                case SortOrders.WriteSize:
                    if (FileOperationValue != FileOperation.All && FileOperationValue != FileOperation.Write)
                    {
                        throw new ArgumentException($"The -FileOperation {FileOperationValue} is not valid. You can either use All or Write.");
                        // throw  No -FileOperationValue is valid 
                    }

                    lret = data.FileWriteSizeInBytes;
                    break;
                case SortOrders.TotalSize:
                    if (FileOperationValue != FileOperation.All )
                    {
                        throw new ArgumentException($"The -FileOperation {FileOperationValue} is not valid. Allowed value must be All only.");
                        // throw  No -FileOperationValue is valid 
                    }

                    lret = data.FileWriteSizeInBytes + data.FileReadSizeInBytes;
                    break;
                case SortOrders.TotalTime:
                    if (FileOperationValue != FileOperation.All )
                    {
                        throw new ArgumentException($"The -FileOperation {FileOperationValue} is not valid. Allowed value must be All only.");
                        // throw  No -FileOperationValue is valid 
                    }

                    lret = data.TotalFileTime;
                    break;
                case SortOrders.ReadTime:
                    if (FileOperationValue != FileOperation.All && FileOperationValue != FileOperation.Read)
                    {
                        throw new ArgumentException($"The -FileOperation {FileOperationValue} is not valid. You can either use All or Read.");
                        // throw  No -FileOperationValue is valid 
                    }

                    lret = data.FileReadTimeInus;
                    break;
                case SortOrders.WriteTime:
                    if (FileOperationValue != FileOperation.All && FileOperationValue != FileOperation.Write)
                    {
                        throw new ArgumentException($"The -FileOperation {FileOperationValue} is not valid. You can either use All or Write.");
                        // throw  No -FileOperationValue is valid 
                    }

                    lret = data.FileWriteTimeInus;
                    break;
                case SortOrders.Time:
                    lret = FileOperationValue switch
                    {
                        FileOperation.Close => data.FileCloseTimeInus,
                        FileOperation.Open => data.FileOpenTimeInus,
                        FileOperation.Read => data.FileReadTimeInus,
                        FileOperation.Write => data.FileWriteTimeInus,
                        FileOperation.SetSecurity => ThrowArgumentException(ReadWriteOpenCloseFilter),// throw exception
                        FileOperation.All => data.FileCloseTimeInus + data.FileOpenTimeInus + data.FileReadTimeInus + data.FileWriteTimeInus,
                        FileOperation.Delete => ThrowArgumentException(ReadWriteOpenCloseFilter),// throw exception
                        FileOperation.Rename => ThrowArgumentException(ReadWriteOpenCloseFilter),// throw exception
                        _ => throw new NotSupportedException($"File Operation sort not yet implemented for value: {FileOperationValue}"),
                    };
                    break;
                case SortOrders.Size: // this is also  SortOrders.Default
                    switch (FileOperationValue)
                    {
                        case FileOperation.Read:
                            lret = data.FileReadSizeInBytes;
                            break;
                        case FileOperation.Write:
                            lret = data.FileWriteSizeInBytes;
                            break;
                        case FileOperation.SetSecurity:
                        case FileOperation.Close:
                        case FileOperation.Open:
                        case FileOperation.Delete:
                        case FileOperation.Rename:
                            SortOrder = SortOrders.Count;  // Size is default sort order. Switch to count if filter is one of these to sort in a meaningful way
                            return GetSortValue(data);
                        case FileOperation.All:  // by default we sort by total time to stay consistent with -Dump disk.
                            lret = data.FileReadSizeInBytes + data.FileWriteSizeInBytes;
                            break;
                        default:
                            throw new NotSupportedException($"File Operation sort not yet implemented for value: {FileOperationValue}");
                    }
                    break;
                case SortOrders.Length:
                    lret = FileOperationValue switch
                    {
                        FileOperation.Read => data.FileReadMaxPos,
                        FileOperation.Write => data.FileWriteMaxFilePos,
                        FileOperation.SetSecurity => ThrowArgumentException(ReadWriteFilter),
                        FileOperation.Close => ThrowArgumentException(ReadWriteFilter),
                        FileOperation.Open => ThrowArgumentException(ReadWriteFilter),
                        FileOperation.Delete => ThrowArgumentException(ReadWriteFilter),
                        FileOperation.Rename => ThrowArgumentException(ReadWriteFilter),
                        FileOperation.All => Math.Max(data.FileReadMaxPos, data.FileWriteMaxFilePos),
                        _ => ThrowArgumentException(ReadWriteFilter),
                    };
                    break;
                case SortOrders.OpenCloseTime:
                    lret = data.FileOpenTimeInus + data.FileCloseTimeInus;
                    break;
                default:
                    throw new InvalidOperationException($"There should be a sort order. SortOrder was: {SortOrder}. By default SortOrders.Size == SortOrders.Default = 0 so we should never get here.");
            }

            return lret;
        }


        decimal ThrowArgumentException(FileOperation[] allowedValues)
        {
            throw new ArgumentException($"You need to set -FileOperation to sort by a specific row. Entered value is {FileOperationValue}. " +
                                        $"Possible values are {string.Join(", ", allowedValues)}.");
        }

        long ThrowNotSupportedException()
        {
            throw new NotSupportedException($"Unsupported -FileOperation {FileOperationValue} for SortOrder {SortOrder} used.");
        }

        /// <summary>
        /// Used by context aware help
        /// </summary>
        static internal readonly SortOrders[] ValidSortOrders = new[]
        {
            SortOrders.ReadTime,
            SortOrders.WriteTime,
            SortOrders.TotalTime,
            
            SortOrders.ReadSize,
            SortOrders.WriteSize,
            SortOrders.TotalSize,

            SortOrders.Count,
            SortOrders.Length,
            SortOrders.Time,
            SortOrders.Default,
        };

        internal bool MinMaxFilter(MatchData data)
        {
            bool lret = true;

            lret = MinMaxReadSizeBytes.IsWithin(data.FileReadSizeInBytes) &&
                   MinMaxWriteSizeBytes.IsWithin(data.FileWriteSizeInBytes) &&
                   MinMaxTotalSizeBytes.IsWithin((data.FileWriteSizeInBytes + data.FileReadSizeInBytes)) &&
                   MinMaxReadTimeS.IsWithin(data.FileReadTimeInus / Million) &&
                   MinMaxWriteTimeS.IsWithin(data.FileWriteTimeInus / Million) &&
                   MinMaxTotalTimeS.IsWithin((data.FileWriteTimeInus + data.FileReadTimeInus) / Million) &&
                   MinMaxTotalCount.IsWithin(data.FileCloseCount + data.FileOpenCount + data.FileReadCount + data.FileWriteCount + data.FileSetSecurityCount +
                                                data.FileDeleteCount + data.FileRenameCount);


            return lret;
        }


        public class MatchData
        {
            static readonly char[] DirectorySeparators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

            /// <summary>
            /// Extracted Json file name/s
            /// </summary>
            public string SourceFileName;

            /// <summary>
            /// File Name from File IO Data which 
            /// </summary>
            public string FileName;


            string myDirectory;

            /// <summary>
            /// Directory of FileName
            /// </summary>
            public string Directory
            {
                get
                {
                    myDirectory ??= FileName.IndexOfAny(DirectorySeparators) == -1 ? FileName : Path.GetDirectoryName(FileName);
                    return myDirectory;
                }
            }

            /// <summary>
            /// Process specific FileIO Stats 
            /// </summary>
            public FileIOStatistics Stats { get; internal set; }


            /// <summary>
            /// Process dealing with that file <see cref="FileName"/> from FileIO Data
            /// </summary>
            public ETWProcess Process { get; internal set; }

            /// <summary>
            /// Input Json DataFile
            /// </summary>
            public TestDataFile DataFile { get; internal set; }

            /// <summary>
            /// Used software baseline
            /// </summary>
            public string BaseLine { get; internal set; }

            
            // The following properties are filled by AggregateByDirectory to add summary information by directories, after grouping by directory or by process and then by directory to get per process stats
            
            /// <summary>
            /// Get the stored directory root which is usually the result of a call to GetDirectoryLevel with some n
            /// </summary>
            public string RootLevelDirectory
            {
                get; set;
            }

            public decimal FileOpenTimeInus { get; internal set; }
            public decimal FileCloseTimeInus { get; internal set; }
            public decimal FileWriteTimeInus { get; internal set; }
            public decimal FileReadTimeInus { get; internal set; }
            public decimal FileReadSizeInBytes { get; internal set; }
            public decimal FileWriteSizeInBytes { get; internal set; }
            public long FileWriteMaxFilePos { get; internal set; }

            /// <summary>
            /// Sum of open+close+read+write time
            /// </summary>
            public decimal TotalFileTime { get => FileOpenTimeInus + FileCloseTimeInus + FileReadTimeInus + FileWriteTimeInus; }

            /// <summary>
            /// Used by <see cref="AggregateByDirectory(List{MatchData}, int)"/> to group summary information into MatchData by directory and all processes which did contribute.
            /// It is a single process if <see cref="IsPerProcess"/> is used to group data by process and directories.
            /// </summary>
            public HashSet<ETWProcess> Processes { get; internal set; }

            public long FileOpenCount { get; internal set; }
            public long FileCloseCount { get; internal set; }
            public long FileWriteCount { get; internal set; }
            public long FileReadCount { get; internal set; }
            public long FileReadMaxPos { get; internal set; }
            public long FileSetSecurityCount { get; internal set; }
            public long FileDeleteCount { get; internal set; }
            public long FileRenameCount { get; internal set; }
            public DateTimeOffset SessionStart { get; internal set; }
            public int InputFileCountUsedForGrouping { get; internal set; }
        }
    }
}
