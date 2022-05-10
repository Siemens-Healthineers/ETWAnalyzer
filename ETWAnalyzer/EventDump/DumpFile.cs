//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers.Infrastructure;
using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.FileIO;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ETWAnalyzer.Commands.DumpCommand;
using static ETWAnalyzer.Extract.FileIO.FileIOStatistics;

namespace ETWAnalyzer.EventDump
{
    /// <summary>
    /// Dump File IO Data to console or to a CSV
    /// </summary>
    class DumpFile : DumpFileDirBase<DumpFile.MatchData>
    {
        static readonly char[] PathSplitChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        static readonly string PathSplitCharAsString = new string(Path.DirectorySeparatorChar, 1);

        public Func<string, bool> FileNameFilter { get; internal set; }

        public int DirectoryLevel { get; internal set; }
        public bool IsPerProcess { get; internal set; }

        public bool Merge { get; internal set; }
        public bool ShowAllFiles { get; internal set; }
        public int Max { get; internal set; }
        public int Min { get; internal set; }
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

        internal List<MatchData> myUTestData;

        internal const decimal Million = (1000 * 1000.0m);
        internal const decimal MB = 1024 * 1024.0m;
        internal const decimal KB = 1024.0m;

        public override List<MatchData> ExecuteInternal()
        {
            List<MatchData> data = ReadFileData();

            if (IsCSVEnabled)
            {
                OpenCSVWithHeader("CSVOptions", "Date", "InputDirectory", "InputFileName", "Test Case", "Test Time in ms", "Baseline", "ProcessName", "Process", "Command Line", 
                                  "File Directory", "File Name",
                                  "Open Count", "Open Duration us", "Open Status",
                                  "Close Count", "Close Duration us",
                                  "Read Count", "Read Duration us", "Read Accessed Bytes", "Read MaxFilePosition",
                                  "Write Count", "Write Duration us", "Write Accessed Bytes", "Write MaxFilePosition", 
                                  "SetSecurity Count",
                                  "SetSecurity Times",
                                  "File Delete Count",
                                  "File Rename Count"
                                  );

                foreach (var fileIO in data)
                {
                    var stats = fileIO.Stats;
                    string times = String.Join(";", stats?.SetSecurity?.Times?.Select(x => base.GetDateTimeString(x, fileIO.SessionStart, TimeFormatOption)) ?? Array.Empty<string>());
                    WriteCSVLine(CSVOptions, fileIO.DataFile.PerformedAt, Path.GetDirectoryName(fileIO.DataFile.FileName), Path.GetFileNameWithoutExtension(fileIO.DataFile.FileName), fileIO.DataFile.TestName, fileIO.DataFile.DurationInMs,
                                fileIO.BaseLine, fileIO.Process.GetProcessName(UsePrettyProcessName), fileIO.Process.GetProcessWithId(UsePrettyProcessName), NoCmdLine ? "" : fileIO.Process.CommandLineNoExe,
                                Path.GetDirectoryName(fileIO.FileName), Path.GetFileName(fileIO.FileName),
                                stats?.Open?.Count, stats?.Open?.Durationus, String.Join(" ", stats?.Open?.NtStatus?.Select(x => ((NtStatus)x).ToString()) ?? Enumerable.Empty<string>()),
                                stats?.Close?.Count, stats?.Close?.Durationus,
                                stats?.Read?.Count,  stats?.Read?.Durationus, stats?.Read?.AccessedBytes, stats?.Read?.MaxFilePosition,
                                stats?.Write?.Count, stats?.Write?.Durationus, stats?.Write?.AccessedBytes, stats?.Write?.MaxFilePosition,
                                stats?.SetSecurity?.Times?.Count,
                                times,
                                stats?.Delete?.Count,
                                stats?.Rename?.Count
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
                    Console.WriteLine($"{group.Key.PerformedAt} {Path.GetFileNameWithoutExtension(group.Key.JsonExtractFileWhenPresent)}");
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
            foreach (var group in aggregatedByDirectory.GroupBy(grouping).SortAscendingGetTopNLast(ordering, null, TopNProcesses))
            {
                bool bPrintOnce = true;

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
                foreach (var fileEvent in group.Where( x => IsInRange(GetSortValue(x))).SortAscendingGetTopNLast(GetSortValue, null, TopN))
                {
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

                    totalPerProcessFileCount += fileEvent.InputFileCountUsedForGrouping;
                    totalFileCount += fileEvent.InputFileCountUsedForGrouping;

                    if (bPrintHeader && !IsPerProcessTotal && !IsTotalMode)
                    {
                        if (ShowDetails)
                        {
                            ColorConsole.WriteEmbeddedColorLine("[green]Read (Size, MaxFilePos, Duration, Throughput, Count)[/green][yellow]              Write (Size, MaxFilePos, Duration, Throughput, Count)[/yellow][cyan]              Open+Close Duration, Open, Close, SetSecurity Count, Del Count, Rename Count[/cyan]          Directory or File if -dirLevel 100 is used");
                        }
                        else
                        {
                            ColorConsole.WriteEmbeddedColorLine("[green]Read (Size, Duration, Count)[/green][yellow]        Write (Size, Duration, Count)[/yellow][cyan]         Open+Close Duration, Open, Close[/cyan]        Directory or File if -dirLevel 100 is used");
                        }
                        bPrintHeader = false;
                    }

                    if (IsPerProcess && bPrintOnce && !IsTotalMode)
                    {
                        ColorConsole.WriteLine($"{group.Key.GetProcessWithId(UsePrettyProcessName)}{group.Key.StartStopTags}", ConsoleColor.Yellow);
                        bPrintOnce = false;
                    }

                    string fileReadTime = $"{fileEvent.FileReadTimeInus / Million:F5}";
                    string fileReadKB = $"{fileEvent.FileReadSizeInBytes / KB:N0}";
                    string fileReadMaxPosKB = $"{fileEvent.FileReadMaxPos / KB:N0}";
                    string fileWriteTime = $"{fileEvent.FileWriteTimeInus / Million:F5}";
                    string fileWriteMaxPos = $"{fileEvent.FileWriteMaxFilePos / KB:N0}";
                    string fileWriteKB = $"{fileEvent.FileWriteSizeInBytes / KB:N0}";
                    string fileOpenCloseTime = $"{ (fileEvent.FileOpenTimeInus + fileEvent.FileCloseTimeInus) / Million:F5}";

                    decimal readMBPerSeconds = (fileEvent.FileReadSizeInBytes / MB) / ((fileEvent.FileReadTimeInus + 1.0m) / Million);
                    decimal writeMBPerSeconds = (fileEvent.FileWriteSizeInBytes / MB) / ((fileEvent.FileWriteTimeInus + 1.0m) / Million);

                    // suppress details when total mode is total which shows only per file totals, or per process totals
                    if ( ( !IsTotalMode && !IsPerProcessTotal ) || IsFileTotalMode )
                    {
                        if (ShowDetails)
                        {
                            ColorConsole.WriteEmbeddedColorLine($"[green]r {fileReadKB,12} KB {fileReadMaxPosKB,12} KB {fileReadTime,10} s {(int)readMBPerSeconds,5} MB/s {fileEvent.FileReadCount,4} [/green] [yellow]w {fileWriteKB,12} KB {fileWriteMaxPos,12} KB {fileWriteTime,10} s {(int)writeMBPerSeconds,5} MB/s {fileEvent.FileWriteCount,4} [/yellow] [cyan] O+C {fileOpenCloseTime,10} s Open: {fileEvent.FileOpenCount,4} Close: {fileEvent.FileCloseCount,4} SetSecurity: {fileEvent.FileSetSecurityCount,3} Del: {fileEvent.FileDeleteCount,3}, Ren: {fileEvent.FileRenameCount,3}[/cyan] {GetFileName(fileEvent.RootLevelDirectory, ReverseFileName)}");
                        }
                        else
                        {
                            ColorConsole.WriteEmbeddedColorLine($"[green]r {fileReadKB,12} KB {fileReadTime,10} s {fileEvent.FileReadCount,4}[/green] [yellow]w {fileWriteKB,12} KB {fileWriteTime,10} s {fileEvent.FileWriteCount,4} [/yellow] [cyan] O+C {fileOpenCloseTime,10} s Open: {fileEvent.FileOpenCount,4} Close: {fileEvent.FileCloseCount,4}[/cyan] {GetFileName(fileEvent.RootLevelDirectory, ReverseFileName)}");
                        }
                    }
                    printedFiles++;
                    processes.UnionWith(fileEvent.Processes);
                }

                // Show Process totals
                if( IsPerProcess && (IsPerProcessTotal || IsFileTotalMode) )
                {
                    string fileReadKB = $"{totalPerProcessFileReadSizeInBytes / KB:N0}";
                    string fileReadTime = $"{totalPerProcessFileReadTimeInus / Million:F5}";
                    string fileWriteKB = $"{totalPerProcessFileWriteSizeInBytes / KB:N0}";
                    string fileWriteTimeS = $"{totalPerProcessFileWriteTimeInus / Million:F5}";
                    string fileOpenCloseTimeS = $"{totalPerProcessFileOpenCloseTimeInus/ Million:F5}";

                    if (ShowDetails)
                    {
                        ColorConsole.WriteEmbeddedColorLine($"[red]r {fileReadKB,12} KB {fileReadTime,10} s {totalPerProcessFileReadCount,4}[/red] [magenta]w {fileWriteKB,12} KB {fileWriteTimeS,10} s {totalPerProcessFileWriteCount,4} [/magenta] [yellow] O+C {fileOpenCloseTimeS,10} s Open: {totalPerProcessFileOpenCount,4} Close: {totalPerProcessFileCloseCount,4} SetSecurity: {totalPerProcessFileSetSecurityCount,4} Del: {totalPerProcessFileDeleteCount,3}, Ren: {totalPerProcessFileRenameCount,3}[/yellow] Process Total with {totalPerProcessFileCount} accessed files");
                    }
                    else
                    {
                        ColorConsole.WriteEmbeddedColorLine($"[red]r {fileReadKB,12} KB {fileReadTime,10} s {totalPerProcessFileReadCount,4}[/red] [magenta]w {fileWriteKB,12} KB {fileWriteTimeS,10} s {totalPerProcessFileWriteCount,4} [/magenta] [yellow] O+C {fileOpenCloseTimeS,10} s Open: {totalPerProcessFileOpenCount,4} Close: {totalPerProcessFileCloseCount,4}[/yellow] Process Total with {totalPerProcessFileCount} accessed files");
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
                string fileReadKB = $"{totalFileReadSizeInBytes / KB:N0}";
                string fileReadTime = $"{totalFileReadTimeInus / Million:F5}";
                string fileWriteKB = $"{totalFileWriteSizeInBytes / KB:N0}";
                string fileWriteTimeS = $"{totalFileWriteTimeInus / Million:F5}";
                string fileOpenCloseTimeS = $"{totalFileOpenCloseTimeInus / Million:F5}";

                if (ShowDetails)
                {
                    ColorConsole.WriteEmbeddedColorLine($"[red]r {fileReadKB,12} KB {fileReadTime,10} s {totalFileReadCount,6}[/red] [magenta]w {fileWriteKB,12} KB {fileWriteTimeS,10} s {totalFileWriteCount,6} [/magenta] [yellow] O+C {fileOpenCloseTimeS,10} s Open: {totalFileOpenCount,6} Close: {totalFileCloseCount,6} SetSecurity: {totalFileSetSecurityCount,6} Del: {totalFileDeleteCount,5}, Ren: {totalFileRenameCount,5}[/yellow] File/s Total with {totalFileCount} accessed file/s. Process Count: {processes.Count}");
                }
                else
                {
                    ColorConsole.WriteEmbeddedColorLine($"[red]r {fileReadKB,12} KB {fileReadTime,10} s {totalFileReadCount,6}[/red] [magenta]w {fileWriteKB,12} KB {fileWriteTimeS,10} s {totalFileWriteCount,6} [/magenta] [yellow] O+C {fileOpenCloseTimeS,10} s Open: {totalFileOpenCount,6} Close: {totalFileCloseCount,6}[/yellow] File/s Total with {totalFileCount} accessed file/s. Process Count: {processes.Count}");
                }
            }
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
                            fileReadMaxPos += (long)          (stats.Read?.MaxFilePosition).GetValueOrDefault();
                            fileWriteSizeInBytes += (decimal) (stats.Write?.AccessedBytes).GetValueOrDefault();
                            fileWriteMaxFilePos += (long)     (stats.Write?.MaxFilePosition).GetValueOrDefault();
                            fileOpenCount += (long)           (stats.Open?.Count).GetValueOrDefault();
                            fileCloseCount += (long)          (stats.Close?.Count).GetValueOrDefault();
                            fileWriteCount += (long)          (stats.Write?.Count).GetValueOrDefault();
                            fileReadCount += (long)           (stats.Read?.Count).GetValueOrDefault();
                            fileSetSecurityCount += (long)    (stats.SetSecurity?.Times?.Count).GetValueOrDefault();
                            fileDeleteCount += (long)         (stats.Delete?.Count).GetValueOrDefault();
                            fileRenameCount += (long)         (stats.Rename?.Count).GetValueOrDefault();
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
                        ColorConsole.WriteError($"File {file.FileName} does not contain disk File IO data");
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
                        if (FileOperationValue != FileOperation.Invalid && !fileEvent.Stats.HasOperation(FileOperationValue))
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
                            Stats = fileEvent.Stats,
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
        /// Check if value is within size range
        /// </summary>
        /// <param name="sizeKB"></param>
        /// <returns></returns>
        bool IsInRange(decimal sizeKB)
        {
            bool lret = true;
            if ((Min > 0 && sizeKB < Min) ||
                (Max > 0 && sizeKB > Max))
            {
                lret = false;
            }
            return lret;
        }


        /// <summary>
        /// Get summary data based on filter and sort order to enable sorting based only on the filtered data
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Value on which data is summed up per grouping which is then used as sort order inside the grouping</returns>
        decimal GetSortValue(MatchData data)
        {
            decimal lret = 0M;

            switch (SortOrder)
            {
                case SortOrders.Count:
                    lret = FileOperationValue switch
                    {
                        FileOperation.Close => data.FileCloseCount,
                        FileOperation.Open => data.FileOpenCount,
                        FileOperation.Read => data.FileReadCount,
                        FileOperation.Write => data.FileWriteCount,
                        FileOperation.SetSecurity => data.FileSetSecurityCount,
                        FileOperation.Invalid => data.FileCloseCount + data.FileOpenCount + data.FileSetSecurityCount + data.FileWriteCount + data.FileReadCount,
                        FileOperation.Delete => data.FileDeleteCount,
                        FileOperation.Rename => data.FileRenameCount,
                        _ => throw new NotSupportedException($"File Operation sort not yet implemented for value: {FileOperationValue}"),
                    };
                    break;
                case SortOrders.ReadSize:
                    lret = data.FileReadSizeInBytes;
                    break;
                case SortOrders.WriteSize:
                    lret = data.FileWriteSizeInBytes;
                    break;
                case SortOrders.TotalSize:
                    lret = data.FileWriteSizeInBytes + data.FileReadSizeInBytes;
                    break;
                case SortOrders.TotalTime:
                    lret = data.TotalFileTime;
                    break;
                case SortOrders.ReadTime:
                    lret = data.FileReadTimeInus;
                    break;
                case SortOrders.WriteTime:
                    lret = data.FileWriteTimeInus;
                    break;
                case SortOrders.Time:
                    lret = FileOperationValue switch
                    {
                        FileOperation.Close => data.FileCloseTimeInus,
                        FileOperation.Open => data.FileOpenTimeInus,
                        FileOperation.Read => data.FileReadTimeInus,
                        FileOperation.Write => data.FileWriteTimeInus,
                        FileOperation.SetSecurity => data.FileSetSecurityCount,
                        FileOperation.Invalid => data.TotalFileTime,
                        FileOperation.Delete => data.TotalFileTime,
                        FileOperation.Rename => data.TotalFileTime,
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
                            break;
                        case FileOperation.Close:
                        case FileOperation.Open:
                            lret = data.FileWriteSizeInBytes + data.FileReadSizeInBytes;
                            break;
                        case FileOperation.Delete:
                            lret = data.FileDeleteCount;
                            break;
                        case FileOperation.Rename:
                            lret = data.FileRenameCount;
                            break;
                        case FileOperation.Invalid:  // by default we sort by total time to stay consistent with -Dump disk.
                            lret = data.TotalFileTime;
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
                        FileOperation.Delete => data.FileDeleteCount,
                        FileOperation.Rename => data.FileRenameCount,
                        _ => data.FileWriteMaxFilePos + data.FileWriteMaxFilePos,
                    };
                    break;
                default:
                    throw new InvalidOperationException($"There should be a sort order. By default SortOrders.Size == SortOrders.Default = 0 so we should never get here.");
            }

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
                    if (myDirectory == null)
                    {
                        myDirectory = FileName.IndexOfAny(DirectorySeparators) == -1 ? FileName : Path.GetDirectoryName(FileName);
                    }

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
