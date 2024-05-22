//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extractors;
using ETWAnalyzer.ProcessTools;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Processes;
using System;
using System.Collections.Generic;
using System.Linq;
using ETWAnalyzer.Extract.Modules;
using System.Diagnostics;
using ETWAnalyzer.Commands;
using static ETWAnalyzer.Commands.DumpCommand;
using System.IO;
using Microsoft.Windows.EventTracing.Symbols;

namespace ETWAnalyzer.EventDump
{
    class DumpModuleVersions : DumpFileEtlBase<DumpModuleVersions.MatchData>
    {
        public KeyValuePair<string, Func<string, bool>> DllFilter { get; internal set; }

        public Func<string,bool> ModuleFilter { get; internal set; }
        public bool NoCmdLine { get; internal set; }
        public KeyValuePair<string, Func<string, bool>> VersionFilter { get; internal set; }


        internal enum PrintMode
        {
            Module = 0,
            Dll,
            Pdb
        }

        public SkipTakeRange TopN { get; internal set; }

        public DumpCommand.SortOrders SortOrder { get; internal set; }


        public PrintMode Mode { get; set; }

        string myCurrentFile;
        bool myNeedToWriteHeader = true;

        /// <summary>
        /// 
        /// </summary>
        public KeyValuePair<string, Func<string, bool>> MissingPdbFilter { get; internal set; }
        public TotalModes? ShowTotal { get; internal set; }

        protected override List<MatchData> DumpJson(TestDataFile file)
        {
            List<MatchData> lret = new();
            myCurrentFile = file.FileName;
            lret = GetAndPrintVersions(file.Extract, file);
            if( IsCSVEnabled)
            {
                if( myNeedToWriteHeader )
                {
                    switch(Mode)
                    {
                        case PrintMode.Module:
                            OpenCSVWithHeader(Col_CSVOptions, Col_TestCase, Col_TestTimeinms, Col_Machine, "TestDate", "Main Version", "Module", "Module Version", "ModuleBuildDate", "BuildDate(MainModule - Module) in days", "SourceFile");
                            break;
                        case PrintMode.Dll:
                            OpenCSVWithHeader(Col_CSVOptions, Col_TestCase, Col_TestTimeinms, Col_Machine, "TestDate", Col_Process, "DllName", "Dll Path", "Versions", "SourceFile");
                            break;
                        case PrintMode.Pdb:
                            OpenCSVWithHeader(Col_CSVOptions, Col_TestCase, Col_TestTimeinms, Col_Machine, "TestDate", "PdbName", "PdbAge", "PdbId", "SourceFile");
                            break;
                        default:
                            throw new NotSupportedException($"Output mode {Mode} is not supported by CSV output yet.");
                    }
                    myNeedToWriteHeader = false;
                }
                WriteToCSV(lret);
            }
            return lret;
        }

        private void WriteToCSV(List<MatchData> rowData)
        {
            foreach (var data in rowData)
            {
                switch (Mode)
                {
                    case PrintMode.Module:
                        string moduleBuildData = GetDateTimeString(MachineDetailsExtractor.GetBuildDate(new Version(data.ModuleVersion.Version)));
                        int diffDays = (int)(MachineDetailsExtractor.GetBuildDate(new Version(data.MainModuleVersion.Version)) - MachineDetailsExtractor.GetBuildDate(new Version(data.ModuleVersion.Version))).TotalDays;
                        diffDays = Math.Abs(diffDays) > 400 ? 400 : diffDays;
                        WriteCSVLine(CSVOptions, data.TestName, data.Duration, data.Machine, data.TestDate, data.MainModuleVersion.Version, data.ModuleVersion.Module, data.ModuleVersion.Version, moduleBuildData, diffDays, data.SourceFile);
                        break;
                    case PrintMode.Dll:
                        WriteCSVLine(CSVOptions, data.TestName, data.Duration, data.Machine, data.TestDate, data.Process, data.DllName, data.DllPath, data.DllVersion, data.SourceFile);
                        break;
                    case PrintMode.Pdb:
                        WriteCSVLine(CSVOptions, data.TestName, data.Duration, data.Machine, data.TestDate, data.MissingPdb.Name, data.MissingPdb.Age, data.MissingPdb.Id, data.SourceFile);
                        break;
                    default:
                        throw new NotSupportedException($"Output mode {Mode} is not supported by CSV output yet.");
                }
            }
        }

        protected override List<MatchData> DumpETL(string etlFile)
        {
            using ITraceProcessor processor = TraceProcessor.Create(etlFile, new TraceProcessorSettings
            {
                AllowLostEvents = true,
                AllowTimeInversion = true,
                ToolkitPath = ETWAnalyzer.TraceProcessorHelpers.Extensions.GetToolkitPath()
            });

            myCurrentFile = etlFile;
            IPendingResult<IProcessDataSource> processes = processor.UseProcesses();

            processor.Process();


            var printStrings = new HashSet<MatchData>();
            foreach (var process in processes.Result.Processes)
            {
                foreach(var dll in process.Images)
                {
                    if (dll.FileName == null || dll.FileVersion == null)
                    {
                        continue;
                    }

                    string version = $"{dll.FileName}, {dll.Path}, {dll.FileVersion} {dll.ProductVersion} {dll.ProductName} {dll.FileVersionNumber}";

                    if ( (DllFilter.Key == null || DllFilter.Value(dll.FileName)) && (VersionFilter.Key == null || VersionFilter.Value(version)) )
                    {
                        printStrings.Add(
                            new MatchData
                            {
                                Line = version,
                            });
                    }
                }
            }

            var ordered = printStrings.OrderBy(x => x.Line).ToList();
            foreach(var print in ordered)
            {
                Console.WriteLine(print.Line);
            }

            return ordered;
        }

        List<MatchData> GetAndPrintVersions(IETWExtract data, TestDataFile sourceFile)
        {
            var lret = new List<MatchData>();
            
            if (data.Modules == null)
            {
                ColorConsole.WriteError($"No extracted module data found for file {myCurrentFile}");
                return lret;
            }

            if (!IsCSVEnabled)
            {
                PrintFileName(myCurrentFile, null, sourceFile.PerformedAt, data?.MainModuleVersion?.ToString());
            }

            switch (Mode)
            {
                case PrintMode.Module:
                    if (data?.MainModuleVersion?.Version == null)
                    {
                        ColorConsole.WriteError($"No module versions found in file {myCurrentFile}.");
                        return lret;
                    }
                    ExtractModuleData(data, sourceFile, lret);
                    break;
                case PrintMode.Dll:
                    ExtractDllData(data, sourceFile, lret);
                    break;
                case PrintMode.Pdb:
                    if( data.Modules.UnresolvedPdbs == null)
                    {
                        ColorConsole.WriteError($"No pdb information stored in file {myCurrentFile}");
                        return lret;
                    }
                    ExtractPdbData(data, sourceFile, lret);
                    break;
                default:
                    throw new NotSupportedException($"Print mode {Mode} not supported yet.");
            }

            return lret;
        }

        private void ExtractPdbData(IETWExtract data, TestDataFile sourceFile, List<MatchData> lret)
        {
            List<MatchData> added = new();

            HashSet<string> allPdbsWithCPUData = data.CPU?.PerProcessMethodCostsInclusive?.MethodNames?.Select(x =>
            {
                int sepIdx = x.IndexOf('!');
                string moduleName = x.Substring(0, sepIdx == -1 ? x.Length : sepIdx);
                if (moduleName.IndexOf('.') != -1)
                {
                    moduleName = Path.GetFileNameWithoutExtension(moduleName); // get rid of .dll, .sys, .exe extension
                }
                moduleName = moduleName + ".pdb";
                return moduleName;
            })?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

            // only report pdbs as missing which have CPU sampling/CSwitch data which would lead to unresolved methods 
            Func<IPdbIdentifier, bool> additionalFilter = allPdbsWithCPUData.Count == 0 ? 
                                                            (iPdb) => true :    // no CPU data present report all pdbs
                                                            (iPdb) => allPdbsWithCPUData.Contains(iPdb.Name);

            foreach (var pdb in data.Modules.UnresolvedPdbs.Where(PdbMatch).Where(additionalFilter))
            {
                added.Add(new MatchData
                {
                    SourceFile = sourceFile?.FileName,
                    MainModuleVersion = data.MainModuleVersion,
                    MissingPdb = pdb,
                    TestDate = new DateTimeOffset((sourceFile?.ParentTest?.PerformedAt).GetValueOrDefault()),
                    TestName = sourceFile?.TestName,
                    Duration = (sourceFile?.DurationInMs).GetValueOrDefault(),
                    Machine = sourceFile?.MachineName,
                    SessionStart = data.SessionStart,
                });
            }

            if (!IsCSVEnabled)
            {
                foreach (var missing in added.OrderBy(x=>x.MissingPdb.Name))
                {
                    ColorConsole.WriteEmbeddedColorLine($"  Missing Pdb {missing.MissingPdb.Name} {missing.MissingPdb.Age} {missing.MissingPdb.Id}");
                }
                ColorConsole.WriteEmbeddedColorLine($"[red]Total {data.Modules.UnresolvedPdbs.Count} pdbs missing, {added.Count} are matching filter[/red]");
            }

            lret.AddRange(added);
        }

        private void ExtractModuleData(IETWExtract data, TestDataFile sourceFile, List<MatchData> lret)
        {
            if (!IsCSVEnabled)
            {
                ColorConsole.WriteEmbeddedColorLine($"Main Version from [yellow]{MachineDetailsExtractor.GetBuildDate(new Version(data.MainModuleVersion.Version)).ToShortDateString()}[/yellow] [green]{data.MainModuleVersion}[/green]");
                Console.WriteLine("Module Versions");
            }

            // for unit testability
            foreach (ModuleVersion version in data.ModuleVersions.Where(ModuleMatch))
            {
                string str = $"\t{version.Module} {version.Version}, {MachineDetailsExtractor.GetBuildDate(new Version(version.Version)).ToShortDateString()}";
                lret.Add(
                    new MatchData
                    {
                        Line = str,
                        SourceFile = sourceFile?.FileName,
                        MainModuleVersion = data.MainModuleVersion,
                        ModuleVersion = version,
                        TestDate = new DateTimeOffset((sourceFile?.ParentTest?.PerformedAt).GetValueOrDefault()),
                        TestName = sourceFile?.TestName,
                        Duration = (sourceFile?.DurationInMs).GetValueOrDefault(),
                        Machine = sourceFile?.MachineName,
                        SessionStart = data.SessionStart,
                    });

                if (!IsCSVEnabled)
                {
                    ColorConsole.WriteLine(str, ConsoleColor.Blue);
                }
            }
        }

        private void ExtractDllData(IETWExtract data, TestDataFile sourceFile, List<MatchData> lret)
        {
            ILookup<ETWProcess, ModuleDefinition> allModulesByProcess = 
                                        data.Modules.Modules
                                       .SelectMany(m => 
                                                         m.Processes.Where(x => IsMatchingProcessAndCmdLine(x))                             // filter by process
                                                                    .Select(p => new KeyValuePair<ModuleDefinition, ETWProcess>(m, p)))    // flatten list of processes where this module was loaded
                                       .ToLookup(x => x.Value, x => x.Key);                                                         // group by process
                                                 

            IGrouping<ETWProcess, ModuleDefinition>[] filteredModulesByProcess = 
                                         data.Modules.Modules
                                         .Where(x => DllFilter.Value(x.ModuleName) &&                                             // filter by dll name
                                                     (VersionFilter.Key == null ? true : VersionFilter.Value($"{x.ModulePath} {GetModuleString(x)}")))  // filter for path and module version string
                                         .SelectMany(m => m.Processes.Where(x => IsMatchingProcessAndCmdLine(x))
                                                                     .Select(p => new KeyValuePair<ModuleDefinition, ETWProcess>(m, p)))   // flatten list of processes where this module was loaded and filter processes
                                         .ToLookup(x => x.Value, x => x.Key)                                                                   // group by process 
                                         .SortAscendingGetTopNLast(x => x.Key.ProcessName, null, TopN);                                        // sort results to allow -topn  filtering

            foreach (IGrouping<ETWProcess, ModuleDefinition> processGroup in filteredModulesByProcess)
            {
                bool bHeaderPrinted = false;
                int printed = 0;
                foreach (ModuleDefinition module in processGroup.ToHashSet().OrderBy(x => x.ModuleName))
                {
                    printed++;
                    if (!IsCSVEnabled)
                    {
                        if (!bHeaderPrinted)
                        {
                            if (NoCmdLine)
                            {
                                ColorConsole.WriteEmbeddedColorLine($"[yellow]  {processGroup.Key.GetProcessWithId(UsePrettyProcessName),-20}{GetProcessTags(processGroup.Key, data.SessionStart)}[/yellow]");
                            }
                            else
                            {
                                ColorConsole.WriteEmbeddedColorLine($"[yellow]  {processGroup.Key.GetProcessWithId(UsePrettyProcessName),-20}{GetProcessTags(processGroup.Key, data.SessionStart)}[/yellow] {processGroup.Key.CommandLineNoExe}", ConsoleColor.DarkCyan);
                            }
                            bHeaderPrinted = true;
                        }
                        if( ShowTotal != TotalModes.Total) 
                        { 
                            ColorConsole.WriteLine($"    {module.ModuleName} {module.ModulePath} {GetModuleString(module)}");
                        }
                    }
                    lret.Add(
                            new MatchData
                            {
                                DllName = module.ModuleName,
                                DllPath = module.ModulePath,
                                DllVersion = GetModuleString(module),
                                Process = processGroup.Key.GetProcessWithId(UsePrettyProcessName),
                                SourceFile = sourceFile?.FileName,
                                MainModuleVersion = data.MainModuleVersion,
                                ModuleVersion = null,
                                TestDate = new DateTimeOffset((sourceFile?.ParentTest?.PerformedAt).GetValueOrDefault()),
                                TestName = sourceFile?.TestName,
                                Duration = (sourceFile?.DurationInMs).GetValueOrDefault(),
                                Machine = sourceFile?.MachineName,
                                SessionStart = data.SessionStart,
                            });
                }

                if (!IsCSVEnabled && ShowTotal != TotalModes.None)
                {
                    ColorConsole.WriteEmbeddedColorLine($"[yellow]   Selected {printed}/{allModulesByProcess[processGroup.Key].ToHashSet().Count} of all loaded modules[/yellow]");
                }
            
            }
        }

        bool PdbMatch(IPdbIdentifier pdb)
        {
            return MissingPdbFilter.Key != null ? MissingPdbFilter.Value(pdb?.Name) : true;
        }

        bool ModuleMatch(ModuleVersion version)
        {
            return ModuleFilter(version.Module);
        }


        internal class MatchData : IEquatable<MatchData>
        {
            // Common Properties
            public string SourceFile;
            public DateTimeOffset TestDate;

            public int Duration { get; internal set; }
            public string TestName { get; internal set; }
            public string Machine { get; internal set; }
            public DateTimeOffset SessionStart { get; internal set; }

            /// <summary>
            /// In module mode we get modules
            /// </summary>
            public ModuleVersion ModuleVersion;

            /// <summary>
            /// In module mode we get modules
            /// </summary>
            public ModuleVersion MainModuleVersion;


            // In Dll Mode we get loaded dlls and proces 
            public string Process { get; internal set; }
            public string DllName { get; internal set; }
            public string DllVersion { get; internal set; }
            public string DllPath { get; internal set; }

            // In pdb mode we collect missing pdbs
            public IPdbIdentifier MissingPdb { get; set; }

            /// <summary>
            /// For unit testing
            /// </summary>
            public string Line;

            public bool Equals(MatchData other)
            {
                return Line == other.Line &&
                       SourceFile == other.SourceFile &&
                       TestDate == other.TestDate &&
                       (ModuleVersion?.Equals(other.ModuleVersion)).GetValueOrDefault() &&
                       (MainModuleVersion?.Equals(other.MainModuleVersion)).GetValueOrDefault() &&
                       (MissingPdb?.Equals(other.MissingPdb)).GetValueOrDefault();
            }

            public override int GetHashCode()
            {
                // To combine hash codes from different fields see
                // https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
                int hash = 17 * 31 + (Line ?? "").GetHashCode();
                hash = hash * 31 + (SourceFile ?? "").GetHashCode();
                hash = hash * 31 + TestDate.GetHashCode();
                hash = hash * 31 + (ModuleVersion?.GetHashCode()).GetValueOrDefault();
                hash = hash * 31 + (MissingPdb?.GetHashCode()).GetValueOrDefault();
                return hash;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as MatchData);
            }

            public override string ToString()
            {
                return $"{Line}";
            }
        }
    }
}
