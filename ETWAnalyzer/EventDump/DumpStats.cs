//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.EventDump;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.CPU.Extended;
using ETWAnalyzer.Extract.Disk;
using ETWAnalyzer.Extract.Network;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ETWAnalyzer
{

    /// <summary>
    /// Count all events in etl file and  print a summary a sorted by count ascending
    /// ETWAnalyzer -dump etlFile 
    /// </summary>
    class DumpStats : DumpFileDirBase<string>
    {
        /// <summary>
        /// Key is event name, value is count of events
        /// </summary>
        readonly Dictionary<string, EventMetaData> EventCount = new();

        /// <summary>
        /// State for CSV file output
        /// </summary>
        bool myHeaderWritten;

        static readonly internal string[] PropertyNames = new string[]
        {
            "TestCase",
            "PerformedAt",
            "Source",
            "Machine",
            "SourceETLFileName",
            "UsedExtractOptions",
            "OSName",
            "OSBuild",
            "OSVersion",
            "MemorySizeMB",
            "NumberOfProcessors",
            "CPUSpeedMHz",
            "CPUVendor",
            "CPUName",
            "HyperThreading",
            "Disk",
            "SessionStart",
            "SessionEnd",
            "SessionDurationS",
            "BootTime",
            "BootToSessionStart",
            "Model",
            "AdDomain",
            "IsDomainJoined",
            "MainModuleVersion",
            "Displays",
            "Network",
        };

        /// <summary>
        /// This is the list of all supported properties on the command line switch -properties of -dump stats
        /// </summary>
        static internal string AllProperties = String.Join(",", PropertyNames.Take(10)) + Environment.NewLine +
                                               "                                                    " +
                                               String.Join(",", PropertyNames.Skip(10));


        /// <summary>
        /// This list defines the order of how properties are printed into one or several lines
        /// </summary>
        readonly List<KeyValuePair<string, Func<Match, object>>> myFormatters = new();

        /// <summary>
        /// List of properties to print for each Json file
        /// </summary>
        public string Properties { get; internal set; }

        /// <summary>
        /// If true all properties are printed into a single line is Json file stats are printed.
        /// </summary>
        public bool OneLine { get; internal set; }

        /// <summary>
        /// Extraction mapping for property names and corresponding delegate which is used to fill myFormatters
        /// </summary>
        readonly Dictionary<string, Func<Match, object>> myFieldPropertyExtractors = new()
        {
            { "TestCase",           m => m.TestCase },
            { "PerformedAt",        m => m.PerformedAt },
            { "Source",             m => m.Source },
            { "Machine",            m => m.Machine },
            { "SourceETLFileName",  m => m.SourceETLFileName },
            { "UsedExtractOptions", m => m.UsedExtractOptions },
            { "OSName",             m => m.OSName },
            { "OSBuild",            m => m.OSBuild },
            { "OSVersion",          m => m.OSVersion },
            { "MemorySizeMB",       m => m.MemorySizeMB },
            { "NumberOfProcessors", FormatNumberOfProcessors },
            { "CPUSpeedMHz",        FormatSpeedMHz },
            { "CPUVendor",          m => m.CPUVendor },
            { "CPUName",            m => m.CPUName },
            { "Disk",               m => FormatDisks(m.Disks) },
            { "HyperThreading",     m => m.CPUHyperThreadingEnabled?.ToString() },
            { "SessionStart",       m => m.SessionStart },
            { "SessionEnd",         m => m.SessionEnd},
            { "SessionDurationS",    m => (int) (m.SessionEnd-m.SessionStart).TotalSeconds },
            { "BootTime", m => m.BootTime },
            { "BootToSessionStart", m => m.BootTime == default ? TimeSpan.Zero :  m.SessionStart - m.BootTime},
            { "Model",              m => m.Model },
            { "AdDomain",           m => m.AdDomain },
            { "IsDomainJoined",     m => m.IsDomainJoined },
            { "MainModuleVersion",  m => m.MainModuleVersion },
            { "Displays",           m => $"Horizontal: {m.DisplaysHorizontalResolution} Vertical: {m.DisplaysVerticalResolution} MemoryMiB: {m.DisplaysMemoryMiB} Name: {m.DisplaysNames}" },
            { "Network",            m => String.Join(Environment.NewLine, m.NetworkInterfaces.Select(x=> $"{x.NicDescription,-50}  {x.PhysicalAddress,-17} Address: {x.IpAddresses,-20} DNS: {x.DnsServerAddresses}")) },

        };

        /// <summary>
        /// Print when existing number of P and E Cores separately.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        static string FormatNumberOfProcessors(Match data)
        {
            string lret = data.NumberOfProcessors.ToString();
            if( data?.Topology?.Count > 0)
            {
                var lookup = data.Topology.Values.ToLookup(x => x.EfficiencyClass);
                if (lookup.Count > 1) // we have P and E Cores at all
                {
                    foreach (var group in lookup.OrderBy(x => x.First().EfficiencyClass))
                    {
                        lret += $" Efficiency Class {group.Key}: {group.Count(),5}    ";
                    }
                }
            }
            return lret.TrimEnd();
        }

        /// <summary>
        /// Print for P and E-Cores nominal CPU Frequency 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        static string FormatSpeedMHz(Match data)
        {
            string lret = data.CPUSpeedMHz.ToString();
            if (data?.Topology?.Count > 0)
            {
                var lookup = data.Topology.Values.ToLookup(x => x.EfficiencyClass);
 
                if (lookup.Count > 1) // just print when we have P and E Cores
                {
                    lret = "".WithWidth(3);

                    foreach (var group in lookup.OrderBy(x => x.First().EfficiencyClass))
                    {
                        lret += $"Efficiency Class {group.Key}: {group.First().NominalFrequencyMHz,5} MHz ";
                    }
                }
            }

            return lret.TrimEnd(); ;
        }

        private static string FormatDisks(IReadOnlyList<IDiskLayout> disks)
        {
            if( disks == null )
            {
                return null;
            }

            string lret = null;

            foreach (var disk in disks)
            {
                foreach (var partition in disk.Partitions)
                {
                    lret += $"Drive {partition.Drive}, {disk.Model}, WriteCache: {disk.IsWriteCachingEnabled}, Free {"F4".WidthFormat(partition.FreeSizeGiB,10)} GiB/{"F4".WidthFormat(partition.TotalSizeGiB,10)} GiB, DiskSize: {"F4".WidthFormat(disk.CapacityGiB, 10)} GiB" + Environment.NewLine;
                }
            }


            return lret;
        }

        class Match
        {
            public string TestCase;
            public DateTime PerformedAt;
            public string Source;
            public string Machine;
            public int DurationMs;
            public string SourceETLFileName;
            public string UsedExtractOptions;
            public string OSName;
            public string OSBuild;
            public string OSVersion;
            public int MemorySizeMB;
            public int NumberOfProcessors;
            public int CPUSpeedMHz;
            public DateTimeOffset SessionStart;
            public DateTimeOffset SessionEnd;
            public DateTimeOffset BootTime;
            public string Model;
            public string AdDomain;
            public bool IsDomainJoined;
            public IReadOnlyList<IDiskLayout> Disks;
            public string DisplaysHorizontalResolution;
            public string DisplaysVerticalResolution;
            public string DisplaysNames;
            public string DisplaysMemoryMiB;
            public string MainModuleVersion;

            public string CPUVendor { get; internal set; }
            public string CPUName { get; internal set; }
            public bool? CPUHyperThreadingEnabled { get; internal set; }
            public string BaseLine { get; internal set; }
            public IReadOnlyDictionary<CPUNumber, ICPUTopology> Topology { get; internal set; }

            public IReadOnlyList<INetworkInterface> NetworkInterfaces { get; internal set; } = new List<INetworkInterface>();
        }


        class EventMetaData
        {
            public ushort Id { get; set; }
            public long Count { get; set; }
            public string ProviderGUID { get; set; }
            public string TaskGuid { get; set; }
            public string ProviderName { get; set; }
            public TraceEventKeyword Keywords { get; set; }
            public long Size { get; set; }

            public TraceEventOpcode OpCode { get; set; }
        }

        public override List<string> ExecuteInternal()
        {
            string[] properties =  Properties != null ? Properties.Split(new char[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries) : DumpStats.PropertyNames;

            if( properties.Any(x=>x.StartsWith("!")) )
            {
                List<string> forbidden = properties.Select(x => x.Substring(1)).ToList();
                properties = DumpStats.PropertyNames.Where(x => !forbidden.Contains(x)).ToArray();
            }

            foreach(var prop in properties)
            {
                KeyValuePair<string,Func<Match,object>> extractor = myFieldPropertyExtractors.FirstOrDefault(x => String.Compare(x.Key, prop, StringComparison.OrdinalIgnoreCase) == 0);
                if( extractor.Key == null)
                {
                    throw new NotSupportedException($"The property {prop} is not one of the supported properties. Valid ones are {AllProperties}");
                }

                myFormatters.Add(extractor);
            }


            if( ETLFile != null || Path.GetExtension(FileOrDirectoryQueries.FirstOrDefault() ?? "").ToLowerInvariant() == ".etl"  )
            {
                return PrintEventStatistics(ETLFile ?? FileOrDirectoryQueries.First());
            }
            else
            {
                Lazy<SingleTest>[] tests = GetTestRuns(true, SingleTestCaseFilter, TestFileFilter);
                WarnIfNoTestRunsFound(tests);
                List<string> matches = new();

                foreach (var test in tests)
                {
                    using (test.Value) // Release deserialized ETWExtract to keep memory footprint in check
                    {
                        foreach (TestDataFile file in test.Value.Files.Where(TestFileFilter))
                        {
                            if( file.Extract.SessionStart == DateTimeOffset.MinValue) // we got some random json file which is not an ETWExtract ignore it
                            {
                                continue;
                            }

                            var cpuToplogy = file.Extract?.CPU?.Topology;


                            Match m = new()
                            {
                                TestCase = file.TestName,
                                PerformedAt = file.PerformedAt,
                                BaseLine = file.Extract?.MainModuleVersion?.ToString() ?? "",
                                DurationMs = file.DurationInMs,
                                Source = file.FileName,
                                Machine = file.Extract.ComputerName ?? file.MachineName,
                                SourceETLFileName = file.Extract.SourceETLFileName,
                                UsedExtractOptions = file.Extract.UsedExtractOptions,
                                OSName = file.Extract.OSName,
                                OSBuild = file.Extract.OSBuild,
                                OSVersion = file.Extract.OSVersion.ToString(),
                                MemorySizeMB = file.Extract.MemorySizeMB,
                                Topology = cpuToplogy,
                                NumberOfProcessors = file.Extract.NumberOfProcessors,
                                CPUSpeedMHz = file.Extract.CPUSpeedMHz,
                                CPUName = file.Extract.CPUName,
                                CPUVendor = file.Extract.CPUVendor,
                                Disks = file.Extract.Disk.DiskInformation,
                                CPUHyperThreadingEnabled = file.Extract.CPUHyperThreadingEnabled,
                                SessionStart = file.Extract.SessionStart,
                                SessionEnd = file.Extract.SessionEnd,
                                BootTime = file.Extract.BootTime,
                                Model = file.Extract.Model,
                                AdDomain = file.Extract.AdDomain,
                                IsDomainJoined = file.Extract.IsDomainJoined,
                                DisplaysHorizontalResolution = String.Join("~", (file.Extract.Displays ?? Enumerable.Empty<Display>()).Select(x=>x.HorizontalResolution.ToString())),
                                DisplaysVerticalResolution = String.Join("~", (file.Extract.Displays ?? Enumerable.Empty<Display>()).Select(x => x.VerticalResolution.ToString())),
                                DisplaysNames = String.Join("~", (file.Extract.Displays ?? Enumerable.Empty<Display>()).Select(x => x.DisplayName.ToString())),
                                DisplaysMemoryMiB = String.Join("~", (file.Extract.Displays ?? Enumerable.Empty<Display>()).Select(x => x.GraphicsCardMemorySizeMiB.ToString())),
                                MainModuleVersion = file.Extract.MainModuleVersion?.ToString() ?? "",
                                NetworkInterfaces = file.Extract.NetworkInterfaces,
                            };

                            Write(m);
                        }
                    }
                }

                return matches;
            }
        }


        void Write(Match m)
        {
            if (IsCSVEnabled)
            {
                if (!myHeaderWritten)
                {
                    myHeaderWritten = true;
                    OpenCSVWithHeader(Col_CSVOptions, Col_TestCase, "TestDate", Col_TestTimeinms, "SourceFile", Col_Machine, "SourceETLFileName", Col_Baseline, "UsedExtractOptions", "OSName", "OSBuild", "OSVersion", "MemorySizeMB", "NumberOfProcessors", "CPUSpeedMHz", "SessionStart", "SessionEnd", "BootTime", "Model",
                                      "AdDomain", "IsDomainJoined", "DisplaysHorizontalResolution", "DisplaysVerticalResolution", "DisplayNames", "MainModuleVersion", "Network", "DisplaysMemoryMiB");
                }

                string networkStr = String.Join(Environment.NewLine, m.NetworkInterfaces.Select(n => $"Address: {n.IpAddresses}, Desc: {n.NicDescription}, PhysicalAddress: {n.PhysicalAddress}, DNS: {n.DnsServerAddresses}"));
                WriteCSVLine(CSVOptions, m.TestCase, m.PerformedAt, m.DurationMs, m.Source, m.Machine, m.SourceETLFileName, m.BaseLine,  m.UsedExtractOptions, m.OSName, m.OSBuild, m.OSVersion, m.MemorySizeMB, m.NumberOfProcessors, m.CPUSpeedMHz, m.SessionStart, m.SessionEnd, m.BootTime, m.Model,
                             m.AdDomain, m.IsDomainJoined, m.DisplaysHorizontalResolution, m.DisplaysVerticalResolution, m.DisplaysNames, m.MainModuleVersion, networkStr, m.DisplaysMemoryMiB);
            }
            else
            {
                PrintFileName(m.Source, null, m.PerformedAt, m.BaseLine);
                foreach (var format in myFormatters)
                {
                    string alignment = OneLine ? "0" : "-20";
                    string fmtString = "\t[yellow]{0," + alignment + "}[/yellow]: {1}";
                    object value = format.Value(m);
                    if ((value is string strValue && !String.IsNullOrEmpty(strValue)) || (value != null && value is not string) ) 
                    {
                        if( (value is DateTimeOffset) && (((DateTimeOffset)value) == default) )
                        {
                            continue;
                        }
                        if ((value is TimeSpan) && (((TimeSpan)value) == default))
                        {
                            continue;
                        }

                        string[] lines = value.ToString().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        for(int i=0;i < lines.Length;i++)
                        {
                            ColorConsole.WriteEmbeddedColorLine(String.Format(fmtString, format.Key, lines[i]), null, OneLine);
                        }
                    }
                }

                if (OneLine)
                {
                    Console.WriteLine();
                }
            }
                
        }

        public List<string> PrintEventStatistics(string path)
        {
            using (var source = new ETWTraceEventSource(path))
            {
                const string UnnamedEvent = "Unnamed Event";

                ColorConsole.WriteLine($"Session Start {source.SessionStartTime} - End: {source.SessionEndTime}, Duration: {source.SessionDuration}, OSVersion: {source.OSVersion}, PtrSize: {source.PointerSize}");
                ColorConsole.WriteLine($"Cores: {source.NumberOfProcessors} Speed: {source.CpuSpeedMHz} MHz");
                ColorConsole.WriteLine($"Lost Events: {source.EventsLost}");

                source.Dynamic.All += delegate (Microsoft.Diagnostics.Tracing.TraceEvent data)
                {
                    if( !EventCount.TryGetValue(data.EventName ?? UnnamedEvent, out EventMetaData counter) ) // Returns the counter of the current Event
                    {
                        FieldInfo taskGuidField = data.GetType().GetField("taskGuid", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        counter = new EventMetaData()
                        {
                            ProviderGUID = data.ProviderGuid.ToString(),
                            ProviderName = data.ProviderName,
                            OpCode = data.Opcode,
                            Keywords = data.Keywords,
                            Id = (ushort)data.ID,
                        };

                        if( taskGuidField != null )
                        {
                            object taskGuid = taskGuidField.GetValue(data) ?? "TaskGuid(unknown)";
                            counter.TaskGuid = taskGuid.ToString();
                        }
                        EventCount[data.EventName ?? UnnamedEvent] = counter;
                    }

                    counter.Count++;
                    counter.Size += data.EventDataLength;
                };
                source.Process(); //calls source.Dynamic.All every Event               
            }

            var sorted = EventCount.OrderBy(x => x.Value.Count);

            // https://docs.microsoft.com/en-us/dotnet/standard/base-types/composite-formatting
            return PrintStats(sorted);
        }

        /// <summary>
        /// Prints a summary sorted by count ascending
        /// </summary>
        /// <param name="sorted"></param>
        static private List<string> PrintStats(IOrderedEnumerable<KeyValuePair<string, EventMetaData>> sorted)
        {
            List<string> lret = new();

            foreach (var item in sorted)
            {
                string id = item.Value.Id == 0xffff ? "" : item.Value.Id.ToString(CultureInfo.InvariantCulture); // for classic events that is 0xffff and clutters up the output
                string str = $"{item.Key,-55} Count: {item.Value.Count,13:N0} SizeInBytes: {item.Value.Size,14:N0} Id: {id,5} Name: {item.Value.ProviderName,-47} PGUID: {item.Value.ProviderGUID} Task: {item.Value.TaskGuid} OpCode: {((int)item.Value.OpCode),4} Keywords: 0x{((long)item.Value.Keywords):X0}";
                lret.Add(str);
                ColorConsole.WriteLine(str);
            }

            return lret;
        }

    }

}
