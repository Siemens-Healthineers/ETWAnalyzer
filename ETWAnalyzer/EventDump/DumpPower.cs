//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Power;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using Microsoft.Windows.EventTracing.Processes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.EventDump
{
    internal class DumpPower : DumpFileDirBase<DumpPower.MatchData>
    {
        /// <summary>
        /// Unit testing only. ReadFileData will return this list instead of real data
        /// </summary>
        internal List<MatchData> myUTestData = null;

        public bool ShowDetails { get; internal set; }
        public bool ShowDiff { get; internal set; }

        public override List<MatchData> ExecuteInternal()
        {
            List<MatchData> lret = ReadFileData();

            if (lret.Count > 0)
            {
                if (IsCSVEnabled)
                {
                    WriteToCSV(lret);
                }
                else
                {
                    PrintMatches(lret);
                }
            }

            return lret;
        }



        private List<MatchData> ReadFileData()
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
                    if (file?.Extract?.PowerConfiguration?.Count == 0)
                    {
                        ColorConsole.WriteError($"Warning: File {GetPrintFileName(file.FileName)} does not contain Power Profile data.");
                        continue;
                    }

                    foreach (var power in file.Extract.PowerConfiguration)
                    {
                        MatchData data = new MatchData
                        {
                            SourceFile = file.FileName,
                            TestName = file.TestName,
                            PerformedAt = file.PerformedAt,
                            DurationInMs = file.DurationInMs,
                            SessionStart = file.Extract.SessionStart,
                            Machine = file.Extract.ComputerName,
                            PowerConfiguration = power,
                        };

                        lret.Add(data);
                    }
                    
                }
            }

            List<IPowerConfiguration> skipped = new();

            if( ShowDiff )
            {
                for(int i=0;i<lret.Count;i++)
                {
                    if(skipped.Contains(lret[i].PowerConfiguration))
                    {
                        continue;
                    }

                    for(int k=i+1;k<lret.Count;k++)
                    {
                        if (lret[i].PowerConfiguration.Equals(lret[k].PowerConfiguration))
                        { 
                            skipped.Add(lret[i].PowerConfiguration);
                        }
                    }
                }

                // Keep of each skip group one file. Otherwise we would print of a large list of files with duplicates 0 entries. 
                List<IPowerConfiguration> alreadySkipped = new();
                int origCount = lret.Count;

                lret = lret.Where(x =>
                {
                    if (skipped.Contains(x.PowerConfiguration))
                    {
                        if (alreadySkipped.Contains(x.PowerConfiguration))
                        {
                            ColorConsole.WriteLine($"Skipped {x.SourceFile}");
                            return false;
                        }
                        else
                        {
                            alreadySkipped.Add(x.PowerConfiguration);
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }).ToList();
                Console.WriteLine($"Remaining {lret.Count}/{origCount} entries.");
            }

            return lret;
        }

        private void PrintMatches(List<MatchData> matches)
        {
            List<Formatter<IPowerConfiguration>> formatters = GetProcessorFormatters();

            const int ColumnWidth = -40;

            ColorConsole.WriteEmbeddedColorLine($"{"File Date ",ColumnWidth}: ", null, true);

            foreach (var match in matches)
            {
                ColorConsole.WriteEmbeddedColorLine($"{match.PerformedAt,ColumnWidth}", FileConsoleColor, true);
            }

            Console.WriteLine();

            ColorConsole.WriteEmbeddedColorLine($"{"File Name ",ColumnWidth}: ", null, true);

            PrintFileNameMultiLineIndented(matches, ColumnWidth);


            var powerList = formatters.Where(x => ShowOnlyDiffValuesWhenEnabled(matches, x)).ToList();

            if (powerList.Count > 0)
            {
                ColorConsole.WriteEmbeddedColorLine("[green]CPU Power Configuration[/green]");

                foreach (Formatter<IPowerConfiguration> formatter in powerList)
                {
                    PrintDetails(formatter);
                    ColorConsole.WriteEmbeddedColorLine($"{formatter.Header,ColumnWidth}: ", null, true);

                    foreach (MatchData data in matches)
                    {
                        ColorConsole.WriteEmbeddedColorLine($"{formatter.PrintNoDup(data.PowerConfiguration),-40}", null, true);
                    }

                    Console.WriteLine();
                }
            }

            List<Formatter<IPowerConfiguration>> idleFormatters = GetIdleFormatters();
            List<Formatter<IPowerConfiguration>> idleList = idleFormatters.Where(x => ShowOnlyDiffValuesWhenEnabled(matches, x)).ToList();
            if (idleList.Count > 0)
            {
                ColorConsole.WriteEmbeddedColorLine("[green]Idle Configuration[/green]");
                foreach (Formatter<IPowerConfiguration> idleformatter in idleList)
                {

                    PrintDetails(idleformatter);
                    ColorConsole.WriteEmbeddedColorLine($"{idleformatter.Header,ColumnWidth}: ", null, true);

                    foreach (MatchData data in matches)
                    {
                        ColorConsole.WriteEmbeddedColorLine($"{idleformatter.PrintNoDup(data.PowerConfiguration),ColumnWidth}", null, true);
                    }

                    Console.WriteLine();
                }
            }

            List<Formatter<IPowerConfiguration>> parkingFormatters = GetParkingFormatters();
            List<Formatter<IPowerConfiguration>> parkingList = parkingFormatters.Where(x => ShowOnlyDiffValuesWhenEnabled(matches, x)).ToList();
            if (parkingList.Count > 0)
            {
                ColorConsole.WriteEmbeddedColorLine("[green]Core Parking[/green]");
                foreach (Formatter<IPowerConfiguration> parkingformatter in parkingList)
                {
                    PrintDetails(parkingformatter);
                    ColorConsole.WriteEmbeddedColorLine($"{parkingformatter.Header,ColumnWidth}: ", null, true);

                    foreach (MatchData data in matches)
                    {
                        ColorConsole.WriteEmbeddedColorLine($"{parkingformatter.PrintNoDup(data.PowerConfiguration),ColumnWidth}", null, true);
                    }

                    Console.WriteLine();
                }
            }
        }


        /// <summary>
        /// Print a list of file names column wise to console with a given width.
        /// </summary>
        /// <param name="matches">List of matches</param>
        /// <param name="columnWidth"></param>
        private void PrintFileNameMultiLineIndented(List<MatchData> matches, int columnWidth)
        {
            List<string> fileNames = new();
            foreach (var match in matches)
            {
                fileNames.Add(GetPrintFileName(match.SourceFile));
            }

            if( fileNames.Count == 1)
            {
                columnWidth = -1000;
            }

            int iteration = 0;
            int filenameWidth = Math.Abs(columnWidth) - 3;

            while (true)
            {
                bool hasData = false;
                List<string> parts = new List<string>();
                foreach (var file in fileNames)
                {
                    var str = new string(file.Skip(iteration * filenameWidth).Take(filenameWidth).ToArray());
                    if (!hasData)
                    {
                        hasData = str.Length > 0;
                    }
                    parts.Add(str);
                }

                if( !parts.All(String.IsNullOrEmpty) )
                {
                    if (iteration > 0)  // print first column again but empty
                    {
                        ColorConsole.Write(" ".WithWidth(Math.Abs(columnWidth) + 2));
                    }

                    foreach (var part in parts)
                    {
                        ColorConsole.WriteEmbeddedColorLine(part.WithWidth(columnWidth), FileConsoleColor, true);
                    }
                    Console.WriteLine();
                }

                if (!hasData)
                {
                    break;
                }
               
                iteration++;
            }
        }

        /// <summary>
        /// Check if all values are equal 
        /// </summary>
        /// <param name="matches">Data to check</param>
        /// <param name="formatter">Property formatter which output is checked.</param>
        /// <returns>true if setting is not equal for all matches, false otherwise.</returns>
        private bool ShowOnlyDiffValuesWhenEnabled(List<MatchData> matches, Formatter<IPowerConfiguration> formatter)
        {
            if (ShowDiff && matches.Count > 1)
            {
                if (matches.Select(x => formatter.Print(x.PowerConfiguration)).ToHashSet().Count == 1)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// When ShowDetails is enabled formatter Description and Help is printed.
        /// </summary>
        /// <param name="parkingformatter"></param>
        private void PrintDetails(Formatter<IPowerConfiguration> parkingformatter)
        {
            if (ShowDetails)
            {
                ColorConsole.WriteEmbeddedColorLine($"{parkingformatter.GetIndented(parkingformatter.Description, 1)}{Environment.NewLine}" +
                                                    $"{parkingformatter.GetIndented(parkingformatter.Help, 1)}", ConsoleColor.Yellow, false);
            }
        }


        /// <summary>
        /// Get Idle loop specific formatters. The Header contains roughly the TraceProcessing property name while Description and Help is
        /// from the documentation of the Power Profile.
        /// </summary>
        /// <returns>List of formatters.</returns>
        private List<Formatter<IPowerConfiguration>> GetIdleFormatters()
        {
            List<Formatter<IPowerConfiguration>> formatters = [];

            var deepestIdleState = new Formatter<IPowerConfiguration>
            {
                Header = "DeepestIdleState",
                Description = "Processor idle state maximum",
                Help = "Specify the deepest idle state that should be used."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 20 State Type",
                Print = (power) => power.IdleConfiguration.DeepestIdleState.ToString(),
            };
            formatters.Add(deepestIdleState);

            var demoteThreshold = new Formatter<IPowerConfiguration>
            {
                Header = "DemoteThreshold",
                Description = "Processor idle demote threshold",
                Help = "Specify the upper busy threshold that must be met before demoting the processor to a lighter idle state (in percentage)."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 100 %"+Environment.NewLine+"Subgroup / Setting GUIDs:"+Environment.NewLine+"  54533251-82be-4824-96c1-47b60b740d00 / 4b92d758-5a24-4851-a470-815d78aee119",
                Print = (power) => power.IdleConfiguration.DemoteThresholdPercent.ToString(),
            };
            formatters.Add(demoteThreshold);

            var enabled = new Formatter<IPowerConfiguration>
            {
                Header = "Enabled",
                Description = "Processor idle disable",
                Help = "Specify if idle states should be disabled.",
                Print = (power) => power.IdleConfiguration.Enabled.ToString(),
            };
            formatters.Add(enabled);

            var minimumDurationBetweenChecks = new Formatter<IPowerConfiguration>
            {
                Header = "MinimumDurationBetweenChecks",
                Description = "Processor idle time check",
                Help = "Specify the time that elapsed since the last idle state promotion or demotion before idle states may be promoted or demoted again (in microseconds)."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  1 .. 200000 Microseconds",
                Print = (power) => Format(power.IdleConfiguration.MinimumDurationBetweenChecks),
            };
            formatters.Add(minimumDurationBetweenChecks);

            var promoteThresholdPercent = new Formatter<IPowerConfiguration>
            {
                Header = "PromoteThreshold %",
                Description = "Processor idle promote threshold",
                Help = "Specify the lower busy threshold that must be met before promoting the processor to a deeper idle state (in percentage)."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 100 %",
                Print = (power) => power.IdleConfiguration.PromoteThresholdPercent.ToString(),
            };
            formatters.Add(promoteThresholdPercent);

            var scalingEnabled = new Formatter<IPowerConfiguration>
            {
                Header = "ScalingEnabled",
                Help = "Specify if idle state promotion and demotion values should be scaled based on the current performance state.",
                Description = "Processor idle threshold scaling",

                Print = (power) => power.IdleConfiguration.ScalingEnabled.ToString(),
            };
            formatters.Add(scalingEnabled);

            return formatters;
        }

        /// <summary>
        /// Get Processor parking specific formatters. The Header contains roughly the TraceProcessing property name while Description and Help is
        /// from the documentation of the Power Profile.
        /// </summary>
        /// <returns>List of formatters.</returns>
        private List<Formatter<IPowerConfiguration>> GetParkingFormatters()
        {
            List<Formatter<IPowerConfiguration>> formatters = [];
            var concurrencyHeadroomThresholdPercent = new Formatter<IPowerConfiguration>
            {
                Header = "ConcurrencyHeadroomThreshold %",
                Description = "Processor performance core parking concurrency headroom threshold",
                Help = "Specify the busy threshold that must be met by all cores in a concurrency set to unpark an extra core."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 100 %",
                Print = (power) => power.ProcessorParkingConfiguration.ConcurrencyHeadroomThresholdPercent.ToString(),
            };
            formatters.Add(concurrencyHeadroomThresholdPercent);

            
            var concurrencyThresholdPercent = new Formatter<IPowerConfiguration>
            {
                Header = "ConcurrencyThreshold %",
                Description = "Processor performance core parking concurrency threshold",
                Help = "Specify the busy threshold that must be met when calculating the concurrency of a node."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 100 %"+Environment.NewLine+"Subgroup / Setting GUIDs:"+Environment.NewLine+"  54533251-82be-4824-96c1-47b60b740d00 / 2430ab6f-a520-44a2-9601-f7f23b5134b1",
                Print = (power) => power.ProcessorParkingConfiguration.ConcurrencyThresholdPercent.ToString(),
            };
            formatters.Add(concurrencyThresholdPercent);

            var maxEfficiencyClass1UnparkedProcessorPercent = new Formatter<IPowerConfiguration>
            {
                Header = "MaxEfficiencyClass1UnparkedProcessor %",
                Description = "Processor performance core parking max cores for Processor Power Efficiency Class 1",
                Help = "Specify the maximum number of unparked cores/packages allowed for Processor Power Efficiency Class 1 (in percentage)."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 100 %",
                Print = (power) => power.ProcessorParkingConfiguration.MaxEfficiencyClass1UnparkedProcessorPercent.ToString(),
            };
            formatters.Add(maxEfficiencyClass1UnparkedProcessorPercent);

            var maxUnparkedProcessorPercent = new Formatter<IPowerConfiguration>
            {
                Header = "MaxUnparkedProcessor %",
                Description = "Processor performance core parking max cores",
                Help = "Specify the maximum number of unparked cores/packages allowed (in percentage)."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 100 %",
                Print = (power) => power.ProcessorParkingConfiguration.MaxUnparkedProcessorPercent.ToString(),
            };
            formatters.Add(maxUnparkedProcessorPercent);

            var minParkedDuration = new Formatter<IPowerConfiguration>
            {
                Header = "MinParkedDuration",
                Description = "Processor performance core parking increase time",
                Help = "Specify the minimum number of perf check intervals that must elapse before more cores/packages can be unparked."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  1 .. 100 Time check intervals"+Environment.NewLine+"Subgroup / Setting GUIDs:"+Environment.NewLine+"  54533251-82be-4824-96c1-47b60b740d00 / 2ddd5a84-5a71-437e-912a-db0b8c788732",
                Print = (power) => Format(power.ProcessorParkingConfiguration.MinParkedDuration),
            };
            formatters.Add(minParkedDuration);


            var minUnparkedDuration = new Formatter<IPowerConfiguration>
            {
                Header = "MinUnparkedDuration",
                Description = "Processor performance core parking decrease time",
                Help = "Specify the minimum number of perf check intervals that must elapse before more cores/packages can be parked." + Environment.NewLine +
                       "Range, Units:" + Environment.NewLine + 
                       "  1 .. 100 Time check intervals",
                Print = (power) => Format(power.ProcessorParkingConfiguration.MinUnparkedDuration),
            };
            formatters.Add(minUnparkedDuration);

            var minUnparkedProcessorPercent = new Formatter<IPowerConfiguration>
            {
                Header = "MinUnparkedProcessor %",
                Description = "Processor performance core parking min cores",
                Help = "Specify the minimum number of unparked cores/packages allowed (in percentage)."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 100 %",
                Print = (power) => power.ProcessorParkingConfiguration.MinUnparkedProcessorPercent.ToString(),
            };
            formatters.Add(minUnparkedProcessorPercent);

            var minEfficiencyClass1UnparkedProcessorPercent = new Formatter<IPowerConfiguration>
            {
                Header = "MinEfficiencyClass1UnparkedProcessor %",
                Description = "Processor performance core parking min cores for Processor Power Efficiency Class 1",
                Help = "Specify the minimum number of unparked cores/packages allowed for Processor Power Efficiency Class 1 (in percentage)."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 100 %",
                Print = (power) => power.ProcessorParkingConfiguration.MinEfficiencyClass1UnparkedProcessorPercent.ToString()
            };
            formatters.Add(minEfficiencyClass1UnparkedProcessorPercent);

            var overUtilizationThresholdPercent = new Formatter<IPowerConfiguration>
            {
                Header = "OverUtilizationThreshold %",
                Description = "Processor performance core parking overutilization threshold",
                Help = "Specify the busy threshold that must be met before a parked core is considered overutilized (in percentage)."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  5 .. 100 %",
                Print = (power) => power.ProcessorParkingConfiguration.OverUtilizationThresholdPercent.ToString(),
            };
            formatters.Add(overUtilizationThresholdPercent);

            var parkingPerformanceState = new Formatter<IPowerConfiguration>
            {
                Header = "ParkingPerformanceState",
                Description = "Processor performance core parking parked performance state",
                Help = "Specify what performance state a processor enters when parked."+Environment.NewLine+"Possible values (index - hexadecimal or string value - friendly name - descr):"+Environment.NewLine+"  0 - 00000000 - No Preference - No Preference"+Environment.NewLine+"  1 - 00000001 - Deepest Performance State - Deepest Performance State"+Environment.NewLine+"  2 - 00000002 - Lightest Performance State - Lightest Performance State"+Environment.NewLine+"Subgroup / Setting GUIDs:"+Environment.NewLine+"  54533251-82be-4824-96c1-47b60b740d00 / 447235c7-6a8d-4cc0-8e24-9eaf70b96e2b",
                Print = (power) => power.ProcessorParkingConfiguration.ParkingPerformanceState.ToString(),
            };
            formatters.Add(parkingPerformanceState);

            var initialPerformancePercentClass1 = new Formatter<IPowerConfiguration>
            {
                Header = "InitialPerformanceClass1 %",
                Description = "Initial performance for Processor Power Efficiency Class 1 when unparked.",
                Help = "Initial performance state for Processor Power Efficiency Class 1 when woken from a parked state." + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  0 .. 100 %" + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / 1facfc65-a930-4bc5-9f38-504ec097bbc0",
                Print = (power) => power.ProcessorParkingConfiguration.InitialPerformancePercentClass1.ToString(),
            };
            formatters.Add(initialPerformancePercentClass1);

            var softParkLatencyUs = new Formatter<IPowerConfiguration>
            {
                Header = "SoftParkLatencyUs",
                Description = "Processor performance core parking soft park latency",
                Help = " Specify the anticipated execution latency at which a soft parked core can be used by the scheduler." + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  0 .. 4294967295 Microseconds" + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / 97cfac41-2217-47eb-992d-618b1977c907",
                Print = power => power.ProcessorParkingConfiguration.SoftParkLatencyUs.ToString() + " us",
            };
            formatters.Add(softParkLatencyUs);

            var parkingPolicy = new Formatter<IPowerConfiguration>
            {
                Header = "ParkingPolicy",
                Description = "Processor performance core parking decrease policy",
                Help = "Specify the number of cores/packages to park when fewer cores are required.",
                Print = (power) => power.ProcessorParkingConfiguration.ParkingPolicy.ToString(),
            };
            formatters.Add(parkingPolicy);

            var unparkingPolicy = new Formatter<IPowerConfiguration>
            {
                Header = "UnparkingPolicy",
                Description = "Processor performance core parking increase policy",
                Help = "Specify the number of cores/packages to unpark when more cores are required.",
                Print = (power) => power.ProcessorParkingConfiguration.UnparkingPolicy.ToString(),
            };
            formatters.Add(unparkingPolicy);

            var utilityDistributionEnabled = new Formatter<IPowerConfiguration>
            {
                Header = "UtilityDistributionEnabled",
                Description = "Processor performance core parking utility distribution",
                Help = "Specify whether the core parking engine should distribute utility across processors."+Environment.NewLine+"Possible values (index - hexadecimal or string value - friendly name - descr):"+Environment.NewLine+"  0 - 00000000 - Disabled - Disabled"+Environment.NewLine+"  1 - 00000001 - Enabled - Enabled"+Environment.NewLine+"Subgroup / Setting GUIDs:"+Environment.NewLine+"  54533251-82be-4824-96c1-47b60b740d00 / e0007330-f589-42ed-a401-5ddb10e785d3",
                Print = (power) => power.ProcessorParkingConfiguration.UtilityDistributionEnabled.ToString(),
            };
            formatters.Add(utilityDistributionEnabled);

            var utilityDistributionThreshold = new Formatter<IPowerConfiguration>
            {
                Header = "UtilityDistributionThreshold %",
                Description = "Processor performance core parking distribution threshold",
                Help= "Specify the percentage utilization used to calculate the distribution concurrency (in percentage)."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 100 %"+ Environment.NewLine +
                "Subgroup / Setting GUIDs:"+Environment.NewLine+"  54533251-82be-4824-96c1-47b60b740d00 / 4bdaf4e9-d103-46d7-a5f0-6280121616ef",
                Print = (power) => power.ProcessorParkingConfiguration.UtilityDistributionThresholdPercent.ToString(),
            };
            formatters.Add(utilityDistributionThreshold);

            return formatters;
        }


        /// <summary>
        /// Get Processor specific formatters. The Header contains roughly the TraceProcessing property name while Description and Help is
        /// from the documentation of the Power Profile.
        /// </summary>
        /// <returns>List of formatters.</returns>
        private List<Formatter<IPowerConfiguration>> GetProcessorFormatters()
        {
            List<Formatter<IPowerConfiguration>> formatters = new()
            {
                new Formatter<IPowerConfiguration>()
                {
                    Header = "ActiveProfile",
                    Description = "Currently active Power Profile",
                    Help = "",
                    Print = (power) => power.ActivePowerProfile == BasePowerProfile.Custom ? $"{power.ActivePowerProfile}: {power.ActivePowerProfileGuid}" : power.ActivePowerProfile.ToString(),
                },
                new Formatter<IPowerConfiguration>()
                {
                    Header = "Base Profile",
                    Description = "Used base profile from which not set settings are inherited.",
                    Help = "",
                    Print = (power) => power.BaseProfile.ToString(),
                },
                new Formatter<IPowerConfiguration>()
                {
                    Header = "Autonomous Mode",
                    Description = "Processor performance autonomous mode",
                    Help = "  Specify whether processors should autonomously determine their target performance state." + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Possible values (index - hexadecimal or string value - friendly name - descr):" + Environment.NewLine + "  0 - 00000000 - Disabled - Determine target performance state using operating system algorithms." + Environment.NewLine + "  1 - 00000001 - Enabled - Determine target performance state using autonomous selection." + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / 8baa4a8a-14c6-4451-8e8b-14bdbd197537",
                    Print = (power) => power.AutonomousMode.ToString(),
                },

                new Formatter<IPowerConfiguration>()
                {
                    Header = "HeteroPolicyInEffect",
                    Description = "Heterogeneous policy in effect",
                    Help = "Specify what policy to be used on systems with at least two different Processor Power Efficiency Classes." + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Possible values (index - hexadecimal or string value - friendly name - descr):" + Environment.NewLine + "  0 - 00000000 - Use heterogeneous policy 0 - Heterogeneous policy 0." + Environment.NewLine + "  1 - 00000001 - Use heterogeneous policy 1 - Heterogeneous policy 1." + Environment.NewLine + "  2 - 00000002 - Use heterogeneous policy 2 - Heterogeneous policy 2." + Environment.NewLine + "  3 - 00000003 - Use heterogeneous policy 3 - Heterogeneous policy 3." + Environment.NewLine + "  4 - 00000004 - Use heterogeneous policy 4 - Heterogeneous policy 4." + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / 7f2f5cfa-f10c-4823-b5e1-e93ae85f46b5",
                    Print = (power) => power.HeteroPolicyInEffect.ToString(),
                },

                new Formatter<IPowerConfiguration>()
                {
                    Header = "HeteroPolicyThreadScheduling",
                    Description = "Heterogeneous thread scheduling policy",
                    Help = "Specify what thread scheduling policy to use on heterogeneous systems." + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Possible values (index - hexadecimal or string value - friendly name - descr):" + Environment.NewLine + "  0 - 00000000 - All processors - Schedule to any available processor." + Environment.NewLine + "  1 - 00000001 - Performant processors - Schedule exclusively to more performant processors." + Environment.NewLine + "  2 - 00000002 - Prefer performant processors - Schedule to more performant processors when possible." + Environment.NewLine + "  3 - 00000003 - Efficient processors - Schedule exclusively to more efficient processors." + Environment.NewLine + "  4 - 00000004 - Prefer efficient processors - Schedule to more efficient processors when possible." + Environment.NewLine + "  5 - 00000005 - Automatic - Let the system choose an appropriate policy." + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / 93b8b6dc-0698-4d1c-9ee4-0644e900c85d",
                    Print = (power) => power.HeteroPolicyThreadScheduling.ToString(),
                },

                new Formatter<IPowerConfiguration>()
                {
                    Header = "HeteroPolicyThreadSchedulingShort",
                    Description = "Heterogeneous short running thread scheduling policy",
                    Help = "Specify what thread scheduling policy to use for short running threads on heterogeneous systems." + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Possible values (index - hexadecimal or string value - friendly name - descr):" + Environment.NewLine + "  0 - 00000000 - All processors - Schedule to any available processor." + Environment.NewLine + "  1 - 00000001 - Performant processors - Schedule exclusively to more performant processors." + Environment.NewLine + "  2 - 00000002 - Prefer performant processors - Schedule to more performant processors when possible." + Environment.NewLine + "  3 - 00000003 - Efficient processors - Schedule exclusively to more efficient processors." + Environment.NewLine + "  4 - 00000004 - Prefer efficient processors - Schedule to more efficient processors when possible." + Environment.NewLine + "  5 - 00000005 - Automatic - Let the system choose an appropriate policy." + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / bae08b81-2d5e-4688-ad6a-13243356654b",
                    Print = (power) => power.HeteroPolicyThreadSchedulingShort.ToString(),
                },

                new Formatter<IPowerConfiguration>()
                {
                    Header = "DecreaseLevelThreshold Class 1",
                    Description = "Processor performance level increase threshold for Processor Power Efficiency Class 1 processor count increase",
                    Help = "Specifies the performance level increase threshold at which the Processor Power Efficiency Class 1 processor count is increased (in units of Processor Power Efficiency Class 0 processor performance)." + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Possible values (index - hexadecimal or string value - friendly name - descr):" + Environment.NewLine + "  254 - 3C3C3C3C - 60,60,60,60 - Processor performance level threshold change for Processor Power Efficiency Class 1 processor count change relative to Processor Power Efficiency Class 0 performance level." + Environment.NewLine + "  255 - 5A5A5A5A - 90,90,90,90 - Processor performance level threshold change for Processor Power Efficiency Class 1 processor count change relative to Processor Power Efficiency Class 0 performance level." + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / b000397d-9b0b-483d-98c9-692a6060cfbf",
                    Print = power => Format(power.DecreaseLevelThresholdClass1),
                },

                new Formatter<IPowerConfiguration>()
                {
                    Header = "DecreaseLevelThreshold Class 2",
                    Description = "Processor performance level increase threshold for Processor Power Efficiency Class 2 processor count increase",
                    Help = "Specifies the performance level increase threshold at which the Processor Power Efficiency Class 2 processor count is increased (in units of Processor Power Efficiency Class 1 processor performance)." + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Possible values (index - hexadecimal or string value - friendly name - descr):" + Environment.NewLine + "  255 - 5A5A5A5A - 90,90,90,90 - Processor performance level threshold change for Processor Power Efficiency Class 2 processor count change relative to Processor Power Efficiency Class 1 performance level." + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / b000397d-9b0b-483d-98c9-692a6060cfc0",
                    Print = power => Format(power.DecreaseLevelThresholdClass2),
                },

                new Formatter<IPowerConfiguration>()
                {
                    Header = "ShortVsLongThreadThreshold us",
                    Description = "Short vs. long running thread threshold",
                    Help = "Specifies the global threshold that designates which threads have a short versus a long runtime." + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  0 .. 100000 Microseconds" + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / d92998c2-6a48-49ca-85d4-8cceec294570",
                    Print = power => Format(power.ShortVsLongThreadThresholdUs),
                },

                new Formatter<IPowerConfiguration>()
                {
                    Header = nameof(PowerConfiguration.LongRunningThreadsLowerArchitectureLimit),
                    Description = "Long running threads' processor architecture upper limit",
                    Help = "Specify the upper limit of processor architecture class for long running threads" + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  0 .. 255 Processor Architecture Class",
                    Print =  power => Format(power.LongRunningThreadsLowerArchitectureLimit),
                },

                new Formatter<IPowerConfiguration>
                {
                    Header = "EnergyPreference %",
                    Description = "Processor energy performance preference policy",
                    Help = "Specify how much processors should favor energy savings over performance when operating in autonomous mode." + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  0 .. 100 %" + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / 36687f9e-e3a5-4dbf-b1dc-15eb381c6863",
                    Print = power => Format(power.EnergyPreferencePercent),
                },
                new Formatter<IPowerConfiguration>
                {
                    Header = "EnergyPreference % Class 1",
                    Description = "Processor energy performance preference policy for Processor Power Efficiency Class 1",
                    Help = "Specify how much Processor Power Efficiency Class 1 processors should favor energy savings over performance when operating in autonomous mode." + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  0 .. 100 %" + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / 36687f9e-e3a5-4dbf-b1dc-15eb381c6864" + Environment.NewLine + "",
                    Print = power => Format(power.EnergyPreferencePercentClass1),
                },
                new Formatter<IPowerConfiguration>
                {
                    Header = nameof(PowerConfiguration.BoostMode),
                    Description = "Processor performance boost mode",
                    Help = "Specify how processors select a target frequency when allowed to select above maximum frequency by current operating conditions." + Environment.NewLine + "Possible values (index - hexadecimal or string value - friendly name - descr):" + Environment.NewLine + "  0 - 00000000 - Disabled - Don't select target frequencies above maximum frequency." + Environment.NewLine + "  1 - 00000001 - Enabled - Select target frequencies above maximum frequency." + Environment.NewLine + "  2 - 00000002 - Aggressive - Always select the highest possible target frequency above nominal frequency." + Environment.NewLine + "  3 - 00000003 - Efficient Enabled - Select target frequencies above maximum frequency if hardware supports doing so efficiently." + Environment.NewLine + "  4 - 00000004 - Efficient Aggressive - Always select the highest possible target frequency above nominal frequency if hardware supports doing so efficiently." + Environment.NewLine + "  5 - 00000005 - Aggressive At Guaranteed - Always select the highest possible target frequency above guaranteed frequency." + Environment.NewLine + "  6 - 00000006 - Efficient Aggressive At Guaranteed - Always select the highest possible target frequency above guaranteed frequency if hardware supports doing so efficiently.",
                    Print = (power) => power.BoostMode.ToString(),
                },

                new Formatter<IPowerConfiguration>
                {
                    Header = "BoostPolicy %",
                    Description = "Processor performance boost policy",
                    Help = "Specify how much processors may opportunistically increase frequency above maximum when allowed by current operating conditions." + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  0 .. 100 %" + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / 45bcc044-d885-43e2-8605-ee0ec6e96b59",
                    Print = (power) => power.BoostPolicyPercent.ToString(),
                },

                new Formatter<IPowerConfiguration>
                {
                    Header = "DecreasePolicy",
                    Description = "Processor performance decrease policy",
                    Help = "Specify the algorithm used to select a new performance state when the ideal performance state is lower than the current performance state." + Environment.NewLine + "Possible values (index - hexadecimal or string value - friendly name - descr):" + Environment.NewLine + "  0 - 00000000 - Ideal - Select the ideal processor performance state." + Environment.NewLine + "  1 - 00000001 - Single - Select the processor performance state one closer to ideal than the current processor performance state." + Environment.NewLine + "  2 - 00000002 - Rocket - Select the lowest speed/power processor performance state." + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / 40fbefc7-2e9d-4d25-a185-0cfd8574bac6",
                    Print = (power) => power.DecreasePolicy.ToString(),
                },

                new Formatter<IPowerConfiguration>
                {
                    Header = "DecreaseStabilizationInterval",
                    Description = "Short vs. long running thread threshold",
                    Help = "Specifies the global threshold that designates which threads have a short versus a long runtime." + Environment.NewLine +
                           "Range, Units:" + Environment.NewLine + "  0 .. 100000 Microseconds",

                    Print = (power) => Format(power.DecreaseStabilizationInterval),
                },

                new Formatter<IPowerConfiguration>
                {
                    Header = "DecreaseThreshold %",
                    Description = "Processor performance decrease threshold",
                    Help = "Specify the lower busy threshold that must be met before decreasing the processor's performance state (in percentage)." + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  0 .. 100 %",
                    Print = (power) => power.DecreaseThresholdPercent.ToString(),
                },

                new Formatter<IPowerConfiguration>
                {
                    Header = "IncreasePolicy",
                    Description = "Processor performance increase policy",
                    Help = "Specify the algorithm used to select a new performance state when the ideal performance state is higher than the current performance state." + Environment.NewLine + "Possible values (index - hexadecimal or string value - friendly name - descr):" + Environment.NewLine + "  0 - 00000000 - Ideal - Select the ideal processor performance state." + Environment.NewLine + "  1 - 00000001 - Single - Select the processor performance state one closer to ideal than the current processor performance state." + Environment.NewLine + "  2 - 00000002 - Rocket - Select the highest speed/power processor performance state." + Environment.NewLine + "  3 - 00000003 - IdealAggressive - Select the ideal processor performance state optimized for responsiveness" + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / 465e1f50-b610-473a-ab58-00d1077dc418",
                    Print = (power) => power.IncreasePolicy.ToString(),
                },
                new Formatter<IPowerConfiguration>
                {
                    Header = "IncreasePolicy Class 1",
                    Description = "Processor performance increase policy for Processor Power Efficiency Class 1",
                    Help = "Specify the algorithm used to select a new performance state when the ideal performance state is higher than the current performance state for Processor Power Efficiency Class 1." + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Possible values (index - hexadecimal or string value - friendly name - descr):" + Environment.NewLine + "  0 - 00000000 - Ideal - Select the ideal processor performance state." + Environment.NewLine + "  1 - 00000001 - Single - Select the processor performance state one closer to ideal than the current processor performance state." + Environment.NewLine + "  2 - 00000002 - Rocket - Select the highest speed/power processor performance state." + Environment.NewLine + "  3 - 00000003 - IdealAggressive - Select the ideal processor performance state optimized for responsiveness" + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / 465e1f50-b610-473a-ab58-00d1077dc419",
                    Print = power => Format(power.IncreasePolicyClass1),
                },
                new Formatter<IPowerConfiguration>
                {
                    Header = nameof(PowerConfiguration.IncreaseStabilizationInterval),
                    Description = "Processor performance increase time",
                    Help = "Specify the minimum number of perf check intervals since the last performance state change before the performance state may be increased." + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  1 .. 100 Time check intervals",
                    Print = (power) => Format(power.IncreaseStabilizationInterval),
                },

                new Formatter<IPowerConfiguration>
                {
                    Header = "IncreaseThreshold %",
                    Description = "Processor performance increase threshold",
                    Help = "Specify the upper busy threshold that must be met before increasing the processor's performance state (in percentage)." + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  0 .. 100 %",
                    Print = (power) => power.IncreaseThresholdPercent.ToString(),
                },
                new Formatter<IPowerConfiguration>
                {
                    Header = nameof(PowerConfiguration.IncreaseStabilizationIntervalClass1),
                    Description = "Processor performance increase time for Processor Power Efficiency Class 1",
                    Help = "Specify the minimum number of perf check intervals since the last performance state change before the performance state may be increased for Processor Power Efficiency Class 1." + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  1 .. 100 Time check intervals" + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / 984cf492-3bed-4488-a8f9-4286c97bf5ab",
                    Print = power => Format(power.IncreaseStabilizationIntervalClass1),
                },
                new Formatter<IPowerConfiguration>
                {
                    Header = nameof(PowerConfiguration.StabilizationInterval),
                    Description = "Processor performance time check interval",
                    Help = "Specify the amount that must expire before processor performance states and parked cores may be reevaluated (in milliseconds)." + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  1 .. 5000 Milliseconds",
                    Print = (power) => Format(power.StabilizationInterval),
                },
                new Formatter<IPowerConfiguration>
                {
                    Header = nameof(PowerConfiguration.IncreaseThresholdPercentClass1),
                    Description = "Processor performance level increase threshold for Processor Power Efficiency Class 1 processor count increase",
                    Help = "Specifies the performance level increase threshold at which the Processor Power Efficiency Class 1 processor count is increased (in units of Processor Power Efficiency Class 1 processor performance)." + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Possible values (index - hexadecimal or string value - friendly name - descr):" + Environment.NewLine + "  255 - 5A5A5A5A - 90,90,90,90 - Processor performance level threshold change for Processor Power Efficiency Class 2 processor count change relative to Processor Power Efficiency Class 1 performance level." + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / b000397d-9b0b-483d-98c9-692a6060cfc0",
                    Print = power => Format(power.IncreaseThresholdPercentClass1),
                },

                new Formatter<IPowerConfiguration>
                {
                    Header = nameof(PowerConfiguration.IncreaseThresholdPercentClass2),
                    Description = "Processor performance level increase threshold for Processor Power Efficiency Class 2 processor count increase",
                    Help = "Specifies the performance level increase threshold at which the Processor Power Efficiency Class 2 processor count is increased (in units of Processor Power Efficiency Class 1 processor performance)." + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Possible values (index - hexadecimal or string value - friendly name - descr):" + Environment.NewLine + "  255 - 5A5A5A5A - 90,90,90,90 - Processor performance level threshold change for Processor Power Efficiency Class 2 processor count change relative to Processor Power Efficiency Class 1 performance level." + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / b000397d-9b0b-483d-98c9-692a6060cfc0",
                    Print = power => Format(power.IncreaseThresholdPercentClass2),
                },
                new Formatter<IPowerConfiguration>
                {
                    Header = "LatencySensitivity %",
                    Description = "Latency sensitivity hint processor performance",
                    Help = "Specify the processor performance in response to latency sensitivity hints." + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  0 .. 100 %",
                    Print = (power) => power.LatencySensitivityPerformancePercent.ToString(),
                },

                new Formatter<IPowerConfiguration>
                {
                    Header = "MaxFrequency MHz Class 0",
                    Description = "Maximum processor frequency for Efficiency Cores.",
                    Help = "Description:" + Environment.NewLine + "  Specify the approximate maximum frequency of your Processor Power Efficiency Class 1 processor (in MHz)." + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  0 .. 4294967295 MHz",
                    Print = (power) => power.MaxEfficiencyClass0Frequency.ToString(),
                },

            
                new Formatter<IPowerConfiguration>
                {
                    Header = "MaxFrequency MHz Class1",
                    Description = "Maximum processor frequency for Processor Power Efficiency Class 1 (P-Cores).",
                    Help = "Description:" + Environment.NewLine + "  Specify the approximate maximum frequency of your Processor Power Efficiency Class 1 processor (in MHz)." + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  0 .. 4294967295 MHz",
                    Print = (power) => power.MaxEfficiencyClass1Frequency.ToString(),
                },

                new Formatter<IPowerConfiguration>
                {
                    Header = "MaxThrottleFrequency %",
                    Description = "Maximum processor state",
                    Help = "Specify the maximum performance state of your processor (in percentage). " + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  0 .. 100 %",
                    Print = (power) => power.MaxThrottlingFrequencyPercent.ToString(),
                },

                new Formatter<IPowerConfiguration>
                {
                    Header = "MaxThrottleFrequency % Class 1",
                    Description = "Maximum processor state",
                    Help = "Specify the maximum performance state of your processor (in percentage). " + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  0 .. 100 %",
                    Print = (power) => Format(power.MaxThrottlingFrequencyClass1Percent),
                },

                new Formatter<IPowerConfiguration>
                {
                    Header = "MinThrottleFrequency %",
                    Description = "Minimum processor state",
                    Help = "Specify the minimum performance state of your processor (in percentage). " + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  0 .. 100 %",
                    Print = (power) => power.MinThrottlingFrequencyPercent.ToString(),
                },
                new Formatter<IPowerConfiguration>
                {
                    Header = "MinThrottleFrequency Class 1 %",
                    Description = "Minimum processor state",
                    Help = "Specify the minimum performance state of your Processor Power Efficiency Class 1 processor (in percentage). " + Environment.NewLine + "Subgroup:" + Environment.NewLine + "  Processor power management" + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  0 .. 100 %" + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / 893dee8e-2bef-41e0-89c6-b55d0929964d" + Environment.NewLine + "",
                    Print = power => Format(power.MinThrottlingFrequencyPercentClass1),
                },
                new Formatter<IPowerConfiguration>
                {
                    Header = nameof(PowerConfiguration.SystemCoolingPolicy),
                    Description = "System cooling policy",
                    Help = "Possible values (index - hexadecimal or string value - friendly name - descr):" + Environment.NewLine + "  0 - 00000001 - Passive - Slow the processor before increasing fan speed" + Environment.NewLine + "  1 - 00000000 - Active - Increase fan speed before slowing the processor",
                    Print = (power) => power.SystemCoolingPolicy.ToString(),
                },

                new Formatter<IPowerConfiguration>
                {
                    Header = nameof(PowerConfiguration.ThrottlePolicy),
                    Description = "Allow Throttle States",
                    Help = "Allow processors to use throttle states in addition to performance states." + Environment.NewLine + "Possible values (index - hexadecimal or string value - friendly name - descr):" + Environment.NewLine + "  0 - 00000000 - Off - Off" + Environment.NewLine + "  1 - 00000001 - On - On" + Environment.NewLine + "  2 - 00000002 - Automatic - Automatically use throttle states when they are power efficient." + Environment.NewLine + "Subgroup / Setting GUIDs:" + Environment.NewLine + "  54533251-82be-4824-96c1-47b60b740d00 / 3b04d4fd-1cc7-4f23-ab1c-d1337819c4bb",
                    Print = (power) => power.ThrottlePolicy.ToString(),
                },

                new Formatter<IPowerConfiguration>
                {
                    Header = nameof(PowerConfiguration.TimeWindowSize),
                    Description = "Processor performance history count",
                    Help = "Specify the number of processor performance time check intervals to use when calculating the average utility." + Environment.NewLine + "Range, Units:" + Environment.NewLine + "  1 .. 128 Time check intervals",
                    Print = (power) => power.TimeWindowSize.ToString(),
                },
            };

            return formatters;
        }

        internal void WriteToCSV(List<MatchData> matches)
        {
            OpenCSVWithHeader(Col_CSVOptions, Col_TestCase, Col_TestTimeinms, Col_SourceJsonFile, Col_Machine,
                "Active Profile",
                "Active Profile Guid",
                "Base Profile",
                "Autonomous Mode",
                "Hetero Policy In Effect",
                "Hetero Policy Thread Scheduling",
                "Hetero Policy Thread Scheduling Short",
                "BoostMode","Boost Policy %", 
                "DecreasePolicy", "DecreaseStabilizationInterval", "DecreaseThreshold %", "DecreaseLevelThreshold Class 1 %", "DecreaseLevelThreshold Class 2 %", "IncreasePolicy", 
                "IncreasePolicy Class 1",
                "IncreaseStabilizationInterval", "IncreaseStabilizationInterval Class 1", "IncreaseThreshold %", "IncreaseThreshold % Class 1", "IncreaseThreshold % Class 2", "LatencySensitivityPerformance %", 
                "MaxEfficiencyFrequency MHz Class 0",
                "MaxEfficiencyFrequency MHz Class 1", "MaxThrottlingFrequency %", "MaxThrottlingFrequency % Class 1", "MinThrottlingFrequency %", "MinThrottlingFrequency Class 1 %",
                "StabilizationInterval", "SystemCoolingPolicy", "ThrottlePolicy", "TimeWindowSize",
                "EnergyPreference %", "EnergyPreference % Class 1",
                "Long Running Threads Lower Architecture Limit",
                "Short vs Long Thread Threshold us",

                "Parking - InitialPerformance % Class 1",
                "Parking - SoftParkLatency us",
                "Parking - ConcurrencyHeadroomThreshold %", "Parking - ConcurrencyThreshold %", "Parking - MaxEfficiencyUnparkedProcessor % Class 1",
                "Parking - MaxUnparkedProcessor %", "Parking - MinEfficiency UnparkedProcessor % Class 1", "Parking - MinParkedDuration",
                "Parking - MinUnparkedDuration", "Parking - MinUnparkedProcessor %", "Parking - OverUtilizationThreshold %",
                "Parking - ParkingPerformanceState", "Parking - ParkingPolicy", "Parking - UnparkingPolicy",
                "Parking - UtilityDistributionEnabled", "Parking - UtilityDistributionThreshold %",

                "Idle - DeepestIdleState", "Idle - DemoteThreshold %", "Idle - Enabled", "Idle - MinimumDurationBetweenChecks", "Idle - PromoteThreshold %",
                "Idle - ScalingEnabled"
                );

            foreach (var match in matches)
            {
                IPowerConfiguration power = match.PowerConfiguration;
                ProcessorParkingConfiguration parking = power.ProcessorParkingConfiguration;
                IIdleConfiguration idle = power.IdleConfiguration;

                WriteCSVLine(CSVOptions, match.TestName, match.SessionStart, GetPrintFileName(match.SourceFile), match.Machine, 
                    power.ActivePowerProfile,
                    power.ActivePowerProfileGuid,
                    power.BaseProfile,
                    power.AutonomousMode,
                    power.HeteroPolicyInEffect,
                    power.HeteroPolicyThreadScheduling,
                    power.HeteroPolicyThreadSchedulingShort,
                    power.BoostMode, power.BoostPolicyPercent,
                    power.DecreasePolicy, Format(power.DecreaseStabilizationInterval), 
                    power.DecreaseThresholdPercent, Format(power.DecreaseLevelThresholdClass1), Format(power.DecreaseLevelThresholdClass2),
                    power.IncreasePolicy, power.IncreasePolicyClass1,
                    Format(power.IncreaseStabilizationInterval), power.IncreaseStabilizationIntervalClass1, power.IncreaseThresholdPercent, Format(power.IncreaseThresholdPercentClass1), 
                    Format(power.IncreaseThresholdPercentClass2), power.LatencySensitivityPerformancePercent, power.MaxEfficiencyClass0Frequency,
                    power.MaxEfficiencyClass1Frequency, power.MaxThrottlingFrequencyPercent, power.MaxThrottlingFrequencyClass1Percent, power.MinThrottlingFrequencyPercent, power.MinThrottlingFrequencyPercentClass1,
                    Format(power.StabilizationInterval), power.SystemCoolingPolicy, power.ThrottlePolicy, power.TimeWindowSize,
                    power.EnergyPreferencePercent, power.EnergyPreferencePercentClass1,
                    power.LongRunningThreadsLowerArchitectureLimit,
                    power.ShortVsLongThreadThresholdUs,
                    parking.InitialPerformancePercentClass1,
                    parking.SoftParkLatencyUs,
                    parking.ConcurrencyHeadroomThresholdPercent, parking.ConcurrencyThresholdPercent, parking.MaxEfficiencyClass1UnparkedProcessorPercent, 
                    parking.MaxUnparkedProcessorPercent, parking.MinEfficiencyClass1UnparkedProcessorPercent, Format(parking.MinParkedDuration),
                    Format(parking.MinUnparkedDuration), parking.MinUnparkedProcessorPercent, parking.OverUtilizationThresholdPercent,
                    parking.ParkingPerformanceState, parking.ParkingPolicy, parking.UnparkingPolicy, 
                    parking.UtilityDistributionEnabled, parking.UtilityDistributionThresholdPercent,

                    idle.DeepestIdleState, idle.DemoteThresholdPercent,idle.Enabled, Format(idle.MinimumDurationBetweenChecks), idle.PromoteThresholdPercent,
                    idle.ScalingEnabled
                    ) ;
            }
        }


        string Format(TimeSpan timeSpan)
        {
            return timeSpan.TotalMilliseconds.ToString("F0") + " ms";
        }

        string Format(PercentValue value) => value.ToString();
        public bool IsHexMode { get; set; }

        string Format(uint value) => value.ToString();


        string Format<T>(Nullable<T> value) where T : struct
        {
            if (value == null)
            {
                return "-";
            }
            else if (value is PercentValue percent)
            {
                return value.ToString();
            }
            else if (value is MultiHexValue multi)
            {
                return Format(multi);
            }
            else
            {
                return value.ToString();
            }
        }

        string Format(MultiHexValue multi)
        {
            string lret = "";

            for (int i = 0; i < 4; i++)
            {
                if (IsHexMode)
                {
                    lret += "0x" + (((uint)multi >> i * 8) & 0xff).ToString("X") + ",";
                }
                else
                {
                    lret += (((uint)multi >> i * 8) & 0xff).ToString() + ",";
                }
            }
            return lret.TrimEnd(new char[] { ',' });
        }

        public class MatchData
        {
            public string SourceFile { get; internal set; }
            public string TestName { get; set; }
            public DateTime PerformedAt { get; set; }
            public int DurationInMs { get; set; }
            public DateTimeOffset SessionStart { get; internal set; }
            public IPowerConfiguration PowerConfiguration { get; internal set; }
            public string Machine { get; internal set; }
        }
    }

}
