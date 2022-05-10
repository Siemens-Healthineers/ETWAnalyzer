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

namespace ETWAnalyzer.EventDump
{
    class DumpModuleVersions : DumpFileEtlBase<DumpModuleVersions.MatchData>
    {
        public KeyValuePair<string, Func<string, bool>> DllFilter { get; internal set; }

        public Func<string,bool> ModuleFilter { get; internal set; }
        public bool NoCmdLine { get; internal set; }
        public KeyValuePair<string, Func<string, bool>> VersionFilter { get; internal set; }

        string myCurrentFile;
        bool myNeedToWriteHeader = true;

        bool IsDllMode
        {
            get => DllFilter.Key != null;
        }

        protected override List<MatchData> DumpJson(TestDataFile file)
        {
            List<MatchData> lret = new();
            myCurrentFile = file.FileName;
            lret = GetAndPrintVersions(file.Extract, file);
            file.Extract = null;
            if( IsCSVEnabled)
            {
                if( myNeedToWriteHeader )
                {
                    if (IsDllMode)
                    {
                        OpenCSVWithHeader("CSVOptions", "TestName", "TestDuration ms", "Machine", "TestDate", "Process", "DllName", "Dll Path", "Versions", "SourceFile");
                    }
                    else
                    {
                        OpenCSVWithHeader("CSVOptions", "TestName", "TestDuration ms", "Machine", "TestDate", "Main Version", "Module", "Module Version", "ModuleBuildDate", "BuildDate(MainModule - Module) in days", "SourceFile");
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
                if(IsDllMode)
                {
                    WriteCSVLine(CSVOptions, data.TestName, data.Duration, data.Machine, data.TestDate, data.Process, data.DllName, data.DllPath, data.DllVersion, data.SourceFile);
                }
                else
                {
                    string moduleBuildData = GetDateTimeString(MachineDetailsExtractor.GetBuildDate(new Version(data.ModuleVersion.Version)));
                    int diffDays = (int) (MachineDetailsExtractor.GetBuildDate(new Version(data.MainModuleVersion.Version)) - MachineDetailsExtractor.GetBuildDate(new Version(data.ModuleVersion.Version))).TotalDays;
                    diffDays = Math.Abs(diffDays) > 400 ? 400 : diffDays;
                    WriteCSVLine(CSVOptions, data.TestName, data.Duration, data.Machine, data.TestDate, data.MainModuleVersion.Version, data.ModuleVersion.Module, data.ModuleVersion.Version, moduleBuildData, diffDays, data.SourceFile);
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

            if (DllFilter.Key != null )
            {
                if (!IsCSVEnabled)
                {
                    ColorConsole.WriteLine($"{myCurrentFile}", ConsoleColor.Cyan);
                }

                if ( data.Modules == null)
                {
                    ColorConsole.WriteError($"No extracted module data found for file {myCurrentFile}");
                    return lret;
                }

                foreach (var processGroup in data.Modules.Modules.Where( x=> DllFilter.Value(x.ModuleName))
                         .SelectMany(m => m.Processes.Select(p=> new KeyValuePair<ModuleDefinition,ETWProcess>(m,p))).ToLookup( x => x.Value, x=>x.Key ))
                {
                    if( !ProcessNameFilter(processGroup.Key.GetProcessWithId(UsePrettyProcessName)) )
                    {
                        continue;
                    }

                    bool bHeaderPrinted = false;
                    foreach (var module in processGroup.OrderBy(x => x.ModuleName))
                    {
                        string moduleString = $"{module.ModulePath} {GetModuleString(module)}";
                        if (VersionFilter.Key == null || VersionFilter.Value(moduleString))
                        {
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

                                ColorConsole.WriteLine($"    {module.ModuleName} {moduleString}");
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
                    }
                    
                }
            }
            else
            {

                if (data?.MainModuleVersion?.Version == null)
                {
                    ColorConsole.WriteError($"No module versions found in file {myCurrentFile}.");
                    return lret;
                }

                if (!IsCSVEnabled)
                {
                    ColorConsole.WriteLine($"{myCurrentFile}", ConsoleColor.Cyan);
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

            return lret;
        }




        bool ModuleMatch(ModuleVersion version)
        {
            return ModuleFilter(version.Module);
        }

        internal class MatchData : IEquatable<MatchData>
        {
            public string Line;
            public string SourceFile;
            public DateTimeOffset TestDate;
            public ModuleVersion ModuleVersion;
            public ModuleVersion MainModuleVersion;

            public int Duration { get; internal set; }
            public string TestName { get; internal set; }
            public string Machine { get; internal set; }
            public DateTimeOffset SessionStart { get; internal set; }
            public string Process { get; internal set; }
            public string DllName { get; internal set; }
            public string DllVersion { get; internal set; }
            public string DllPath { get; internal set; }

            public bool Equals(MatchData other)
            {
                return Line == other.Line &&
                       SourceFile == other.SourceFile &&
                       TestDate == other.TestDate &&
                       (ModuleVersion?.Equals(other.ModuleVersion)).GetValueOrDefault() &&
                       (MainModuleVersion?.Equals(other.MainModuleVersion)).GetValueOrDefault();
            }

            public override int GetHashCode()
            {
                // To combine hash codes from different fields see
                // https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
                int hash = 17 * 31 + (Line ?? "").GetHashCode();
                hash = hash * 31 + (SourceFile ?? "").GetHashCode();
                hash = hash * 31 + TestDate.GetHashCode();
                hash = hash * 31 + (ModuleVersion?.GetHashCode()).GetValueOrDefault();
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
