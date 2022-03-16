//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers.Infrastructure;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Disk;
using ETWAnalyzer.Extract.FileIO;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETWAnalyzer.EventDump
{
    class DumpDisk : DumpFileDirBase<DumpDisk.MatchData>
    {
        public Func<string, bool> FileNameFilter { get; internal set; }

        public int DirectoryLevel { get; internal set; }
        public bool IsPerProcess { get; internal set; }
        public bool Merge { get; internal set; }
        public int Min { get; internal set; }
        public int Max { get; internal set; }
        public FileIOStatistics.FileOperation FileOperationValue { get; internal set; }
        public bool ReverseFileName { get; internal set; }

        internal List<MatchData> myUTestData;

        internal const decimal Million = (1000 * 1000.0m);
        internal const decimal MB = 1024 * 1024.0m;


        public override List<MatchData> ExecuteInternal()
        {
            List<MatchData> data = ReadDiskData();

            if( IsCSVEnabled )
            {
                OpenCSVWithHeader("CSVOptions", "Date", "Test Case", "Test Time in ms", "Baseline", "FileName", $"Level{DirectoryLevel}Directory", "DiskTotalTimeIOInus", "DiskReadTimeInus", "DiskWriteTimeInus", "DiskFlushTimeInus", "DiskWrittenBytes", "DiskReadBytes", 
                    "DiskReadPerf MB/s", "DiskWritePerf MB/s", "Processes", "SourceDirectory", "JsonSourceFile");

                foreach(var diskData in data)
                {
                    WriteCSVLine(CSVOptions, diskData.DataFile.PerformedAt, diskData.DataFile.TestName, diskData.DataFile.DurationInMs, diskData.BaseLine,
                                 diskData.FileName,  MatchData.GetDirectoryLevel(diskData.FileName, DirectoryLevel), diskData.DiskTotalTimeInus, diskData.DiskReadTimeInus, diskData.DiskWriteTimeInus, diskData.DiskFlushTimeInus, diskData.DiskWriteSizeInBytes, diskData.DiskReadSizeInBytes,
                                 diskData.ReadMBPerSeconds, diskData.WriteMBPerSeconds,  String.Join(";", diskData.Processes.Select(x => x.GetProcessWithId(UsePrettyProcessName))), 
                                 Path.GetDirectoryName(diskData.SourceFileName), Path.GetFileNameWithoutExtension(diskData.SourceFileName));
                }
                return data;
            }


            

            // group by file or if merge is used do not group at all
            TestDataFile grouping(MatchData data) => Merge ? null : data.DataFile;

            foreach (var byFileOrNoGroup in data.GroupBy(grouping).OrderBy(x => x.Key?.PerformedAt))
            {
                List<MatchData> aggregatedByDirectory = AggregateByDirectory(byFileOrNoGroup.ToList(), DirectoryLevel);
                if (byFileOrNoGroup.Key != null)
                {
                    Console.WriteLine($"{byFileOrNoGroup.Key.PerformedAt} {Path.GetFileNameWithoutExtension(byFileOrNoGroup.Key.JsonExtractFileWhenPresent)}");
                }

                ColorConsole.WriteEmbeddedColorLine("[green]Read                                [/green][yellow]Write                               [/yellow][cyan]Flush       [/cyan] Directory or File if -dirLevel 100 is used");
                foreach (var group in aggregatedByDirectory.OrderBy(x => x.DiskTotalTimeInus).Where(MinMaxFilter))
                {
                    string diskReadTime = $"{group.DiskReadTimeInus / Million:F5}";
                    string diskReadMB = $"{group.DiskReadSizeInBytes / MB:F0}";
                    string diskWriteTime = $"{group.DiskWriteTimeInus / Million:F5}";
                    string diskWriteMB = $"{group.DiskWriteSizeInBytes / MB:F0}";
                    string diskFlushTime = $"{group.DiskFlushTimeInus / Million:F3}";

                    ColorConsole.WriteEmbeddedColorLine($"[green]r {diskReadTime,10} s {diskReadMB,7} MB {group.ReadMBPerSeconds,4} MB/s[/green] [yellow]w {diskWriteTime,10} s {diskWriteMB,7} MB {group.WriteMBPerSeconds,4} MB/s[/yellow] [cyan]f {diskFlushTime,8} s[/cyan] {DumpFile.GetFileName(group.RootLevelDirectory, ReverseFileName)}");
                }
            }


            if (IsPerProcess)
            {
                foreach (var byFileOrNoGroup in data.GroupBy(grouping).OrderBy(x => x.Key?.PerformedAt))
                {
                    Console.WriteLine();
                    if (byFileOrNoGroup.Key != null)
                    {
                        Console.WriteLine($"{byFileOrNoGroup.Key.PerformedAt} {Path.GetFileNameWithoutExtension(byFileOrNoGroup.Key.JsonExtractFileWhenPresent)}");
                    }
                    ColorConsole.WriteEmbeddedColorLine("[green]Read                            [/green][yellow]Write                           [/yellow][cyan]Flush         [/cyan]Involved Processes");

                    List<MatchData> aggregatedByProcess = AggregateByProcess(byFileOrNoGroup.ToList(), UsePrettyProcessName);
                    foreach (var group in aggregatedByProcess.OrderBy(x => x.DiskTotalTimeInus).Where(MinMaxFilter))
                    {
                        string procs;
                        // On some files many processes are participating like page file, volume bitmap, ... 
                        // This is not really visible 
                        if (group.Processes.Count > 5)
                        {
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
                        string diskReadSizeInMB = $"{group.DiskReadSizeInBytes / (1024 * 1024.0m):F0}";
                        string diskWriteSizeInMB = $"{group.DiskWriteSizeInBytes / (1024 * 1024.0m):F0}";

                        ColorConsole.WriteEmbeddedColorLine($"[green]r {diskReadTime,8} s {diskReadSizeInMB,5} MB {group.ReadMBPerSeconds,4} MB/s[/green] [yellow]w {diskWriteTime,8} s {diskWriteSizeInMB,5} MB {group.WriteMBPerSeconds,4} MB/s[/yellow] [cyan]f {diskFlushTime,8}[/cyan] s  {procs}");
                    }
                }
            }

            return data;
        }

        bool MinMaxFilter(MatchData data)
        {
            bool lret = true;
            lret = FileOperationValue switch
            {
                FileIOStatistics.FileOperation.Read => data.DiskReadTimeInus >= Min && (Min >= Max || data.DiskReadTimeInus <= Max),
                FileIOStatistics.FileOperation.Write => data.DiskWriteTimeInus >= Min && (Min >= Max || data.DiskWriteTimeInus <= Max),
                _ => data.DiskTotalTimeInus >= Min && (Min >= Max || data.DiskTotalTimeInus <= Max),
            };
            return lret;
        }

        static internal List<MatchData> AggregateByDirectory(List<MatchData> data, int level)
        {
            List<MatchData> aggregatedByDirectory = new();

            foreach (var group in data.GroupBy(x => MatchData.GetDirectoryLevel(x.FileName, level)))
            {
                MatchData groupedData = new()
                {
                    DiskFlushTimeInus = group.Select(x => (decimal)x.DiskFlushTimeInus).Sum(),
                    DiskWriteTimeInus = group.Select(x => (decimal)x.DiskWriteTimeInus).Sum(),
                    DiskReadTimeInus = group.Select(x => (decimal)x.DiskReadTimeInus).Sum(),
                    RootLevelDirectory = group.Key,
                    FileName = "Grouped Data",
                    SourceFileName = String.Join(" ", group.Select(x => x.SourceFileName).ToHashSet().ToArray()),
                    DiskReadSizeInBytes = group.Select(x => (decimal)x.DiskReadSizeInBytes).Sum(),
                    DiskWriteSizeInBytes = group.Select(x => (decimal)x.DiskWriteSizeInBytes).Sum(),
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
                    SourceFileName = String.Join(" ", group.Select(x => x.SourceFileName).ToHashSet().ToArray()),
                    DiskReadSizeInBytes = group.Select(x => (decimal)x.DiskReadSizeInBytes).Sum(),
                    DiskWriteSizeInBytes = group.Select(x => (decimal)x.DiskWriteSizeInBytes).Sum(),
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

                        lret.Add(new MatchData
                        {
                            SourceFileName = file.FileName,
                            DataFile = file,
                            FileName = diskEvent.FileName,
                            DiskReadSizeInBytes = diskEvent.ReadSizeInBytes,
                            DiskWriteSizeInBytes = diskEvent.WriteSizeInBytes,
                            DiskReadTimeInus = diskEvent.DiskReadTimeInus,
                            DiskWriteTimeInus = diskEvent.DiskWriteTimeInus,
                            DiskFlushTimeInus = diskEvent.DiskFlushTimeInus,
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


            public int ReadMBPerSeconds => DiskReadTimeInus > 0 ? (int)(DiskReadSizeInBytes / MB / (DiskReadTimeInus / Million)) : 0;
            public int WriteMBPerSeconds => DiskWriteTimeInus > 0 ? (int)(DiskWriteSizeInBytes / MB / (DiskWriteTimeInus / Million)) : 0;

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

            static string GetDirectoryNameSafe(string filePath)
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
