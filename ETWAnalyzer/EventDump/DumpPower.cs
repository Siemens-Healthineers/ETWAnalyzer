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
                            TestDurationInMs = file.DurationInMs,
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

                Console.WriteLine($"Skipped {skipped.Count} entries with identical power configurations.");
                lret = lret.Where(x => !skipped.Contains(x.PowerConfiguration)).ToList();
                Console.WriteLine($"Remaining {lret.Count} entries.");
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

            ColorConsole.WriteEmbeddedColorLine("[green]CPU Power Configuration[/green]");


            foreach (Formatter<IPowerConfiguration> formatter in formatters.Where(x => ShowOnlyDiffValuesWhenEnabled(matches, x)))
            {
                PrintDetails(formatter);
                ColorConsole.WriteEmbeddedColorLine($"{formatter.Header,ColumnWidth}: ", null, true);

                foreach (MatchData data in matches)
                {
                    ColorConsole.WriteEmbeddedColorLine($"{formatter.PrintNoDup(data.PowerConfiguration),-40}", null, true);
                }

                Console.WriteLine();
            }

            List<Formatter<IPowerConfiguration>> idleFormatters = GetIdleFormatters();

            ColorConsole.WriteEmbeddedColorLine("[green]Idle Configuration[/green]");
            foreach (Formatter<IPowerConfiguration> idleformatter in idleFormatters.Where(x => ShowOnlyDiffValuesWhenEnabled(matches, x)))
            {

                PrintDetails(idleformatter);
                ColorConsole.WriteEmbeddedColorLine($"{idleformatter.Header,ColumnWidth}: ", null, true);

                foreach (MatchData data in matches)
                {
                    ColorConsole.WriteEmbeddedColorLine($"{idleformatter.PrintNoDup(data.PowerConfiguration),ColumnWidth}", null, true);
                }

                Console.WriteLine();
            }

            List<Formatter<IPowerConfiguration>> parkingFormatters = GetParkingFormatters();
            ColorConsole.WriteEmbeddedColorLine("[green]Core Parking[/green]");
            foreach (Formatter<IPowerConfiguration> parkingformatter in parkingFormatters.Where(x => ShowOnlyDiffValuesWhenEnabled(matches, x)))
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
                fileNames.Add(match.SourceFile);
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
                Print = (power) => power.IdleConfiguration.MinimumDurationBetweenChecks.ToString(),
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
                Print = (power) => power.ProcessorParkingConfiguration.MinParkedDuration.ToString(),
            };
            formatters.Add(minParkedDuration);


            var minUnparkedDuration = new Formatter<IPowerConfiguration>
            {
                Header = "MinUnparkedDuration",
                Description = "Processor performance core parking decrease time",
                Help = "Specify the minimum number of perf check intervals that must elapse before more cores/packages can be parked." + Environment.NewLine +
                       "Range, Units:" + Environment.NewLine + 
                       "  1 .. 100 Time check intervals",
                Print = (power) => power.ProcessorParkingConfiguration.MinUnparkedDuration.ToString(),
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
            List<Formatter<IPowerConfiguration>> formatters = [];

            var baseProfile = new Formatter<IPowerConfiguration>()
            {
                Header = "Base Profile",
                Description = "Used base profile from which not set settings are inherited.",
                Help = "",
                Print = (power) => power.BaseProfile.ToString(),
            };
            formatters.Add(baseProfile);

            var autonomous = new Formatter<IPowerConfiguration>()
            {
                Header = "Autonomous Mode",
                Description = "Processor performance autonomous mode",
                Help = "  Specify whether processors should autonomously determine their target performance state."+Environment.NewLine+"Subgroup:"+Environment.NewLine+"  Processor power management"+Environment.NewLine+"Possible values (index - hexadecimal or string value - friendly name - descr):"+Environment.NewLine+"  0 - 00000000 - Disabled - Determine target performance state using operating system algorithms."+Environment.NewLine+"  1 - 00000001 - Enabled - Determine target performance state using autonomous selection."+Environment.NewLine+"Subgroup / Setting GUIDs:"+Environment.NewLine+"  54533251-82be-4824-96c1-47b60b740d00 / 8baa4a8a-14c6-4451-8e8b-14bdbd197537",
                Print = (power) => power.AutonomousMode.ToString(),
            };
            formatters.Add(autonomous);

            var heteroPolicyInEffect = new Formatter<IPowerConfiguration>()
            {
                Header = "HeteroPolicyInEffect",
                Description = "Heterogeneous policy in effect",
                Help = "Specify what policy to be used on systems with at least two different Processor Power Efficiency Classes."+Environment.NewLine+"Subgroup:"+Environment.NewLine+"  Processor power management"+Environment.NewLine+"Possible values (index - hexadecimal or string value - friendly name - descr):"+Environment.NewLine+"  0 - 00000000 - Use heterogeneous policy 0 - Heterogeneous policy 0."+Environment.NewLine+"  1 - 00000001 - Use heterogeneous policy 1 - Heterogeneous policy 1."+Environment.NewLine+"  2 - 00000002 - Use heterogeneous policy 2 - Heterogeneous policy 2."+Environment.NewLine+"  3 - 00000003 - Use heterogeneous policy 3 - Heterogeneous policy 3."+Environment.NewLine+"  4 - 00000004 - Use heterogeneous policy 4 - Heterogeneous policy 4."+Environment.NewLine+"Subgroup / Setting GUIDs:"+Environment.NewLine+"  54533251-82be-4824-96c1-47b60b740d00 / 7f2f5cfa-f10c-4823-b5e1-e93ae85f46b5",
                Print = (power) => power.HeteroPolicyInEffect.ToString(),   
            };
            formatters.Add(heteroPolicyInEffect);

            var heteroThreadSchedulingPolicy = new Formatter<IPowerConfiguration>()
            {
                Header = "HeteroPolicyThreadScheduling",
                Description = "Heterogeneous thread scheduling policy",
                Help = "Specify what thread scheduling policy to use on heterogeneous systems."+Environment.NewLine+"Subgroup:"+Environment.NewLine+"  Processor power management"+Environment.NewLine+"Possible values (index - hexadecimal or string value - friendly name - descr):"+Environment.NewLine+"  0 - 00000000 - All processors - Schedule to any available processor."+Environment.NewLine+"  1 - 00000001 - Performant processors - Schedule exclusively to more performant processors."+Environment.NewLine+"  2 - 00000002 - Prefer performant processors - Schedule to more performant processors when possible."+Environment.NewLine+"  3 - 00000003 - Efficient processors - Schedule exclusively to more efficient processors."+Environment.NewLine+"  4 - 00000004 - Prefer efficient processors - Schedule to more efficient processors when possible."+Environment.NewLine+"  5 - 00000005 - Automatic - Let the system choose an appropriate policy."+Environment.NewLine+"Subgroup / Setting GUIDs:"+Environment.NewLine+"  54533251-82be-4824-96c1-47b60b740d00 / 93b8b6dc-0698-4d1c-9ee4-0644e900c85d",
                Print = (power) => power.HeteroPolicyThreadScheduling.ToString(),
            };
            formatters.Add(heteroThreadSchedulingPolicy);

            var heteroThreadSchedulingPolicyShort = new Formatter<IPowerConfiguration>()
            {
                Header = "HeteroPolicyThreadSchedulingShort",
                Description = "Heterogeneous short running thread scheduling policy",
                Help = "Specify what thread scheduling policy to use for short running threads on heterogeneous systems."+Environment.NewLine+"Subgroup:"+Environment.NewLine+"  Processor power management"+Environment.NewLine+"Possible values (index - hexadecimal or string value - friendly name - descr):"+Environment.NewLine+"  0 - 00000000 - All processors - Schedule to any available processor."+Environment.NewLine+"  1 - 00000001 - Performant processors - Schedule exclusively to more performant processors."+Environment.NewLine+"  2 - 00000002 - Prefer performant processors - Schedule to more performant processors when possible."+Environment.NewLine+"  3 - 00000003 - Efficient processors - Schedule exclusively to more efficient processors."+Environment.NewLine+"  4 - 00000004 - Prefer efficient processors - Schedule to more efficient processors when possible."+Environment.NewLine+"  5 - 00000005 - Automatic - Let the system choose an appropriate policy."+Environment.NewLine+"Subgroup / Setting GUIDs:"+Environment.NewLine+"  54533251-82be-4824-96c1-47b60b740d00 / bae08b81-2d5e-4688-ad6a-13243356654b",
                Print = (power) => power.HeteroPolicyThreadSchedulingShort.ToString(),
            };
            formatters.Add(heteroThreadSchedulingPolicyShort);


            var boostMode = new Formatter<IPowerConfiguration>
            {
                Header = "BoostMode",
                Description = "Processor performance boost mode",
                Help = "Specify how processors select a target frequency when allowed to select above maximum frequency by current operating conditions."+Environment.NewLine+"Possible values (index - hexadecimal or string value - friendly name - descr):"+Environment.NewLine+"  0 - 00000000 - Disabled - Don't select target frequencies above maximum frequency."+Environment.NewLine+"  1 - 00000001 - Enabled - Select target frequencies above maximum frequency."+Environment.NewLine+"  2 - 00000002 - Aggressive - Always select the highest possible target frequency above nominal frequency."+Environment.NewLine+"  3 - 00000003 - Efficient Enabled - Select target frequencies above maximum frequency if hardware supports doing so efficiently."+Environment.NewLine+"  4 - 00000004 - Efficient Aggressive - Always select the highest possible target frequency above nominal frequency if hardware supports doing so efficiently."+Environment.NewLine+"  5 - 00000005 - Aggressive At Guaranteed - Always select the highest possible target frequency above guaranteed frequency."+Environment.NewLine+"  6 - 00000006 - Efficient Aggressive At Guaranteed - Always select the highest possible target frequency above guaranteed frequency if hardware supports doing so efficiently.",
                Print = (power) => power.BoostMode.ToString(),
            };
            formatters.Add(boostMode);

            var boostPercent = new Formatter<IPowerConfiguration>
            {
                Header = "BoostPolicy %",
                Description = "Processor performance boost policy",
                Help = "Specify how much processors may opportunistically increase frequency above maximum when allowed by current operating conditions."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 100 %"+Environment.NewLine+"Subgroup / Setting GUIDs:"+Environment.NewLine+"  54533251-82be-4824-96c1-47b60b740d00 / 45bcc044-d885-43e2-8605-ee0ec6e96b59",
                Print = (power) => power.BoostPolicyPercent.ToString(),
            };
            formatters.Add(boostPercent);

            var decreasePolicy = new Formatter<IPowerConfiguration>
            {
                Header = "DecreasePolicy",
                Description = "Processor performance decrease policy",
                Help = "Specify the algorithm used to select a new performance state when the ideal performance state is lower than the current performance state."+Environment.NewLine+"Possible values (index - hexadecimal or string value - friendly name - descr):"+Environment.NewLine+"  0 - 00000000 - Ideal - Select the ideal processor performance state."+Environment.NewLine+"  1 - 00000001 - Single - Select the processor performance state one closer to ideal than the current processor performance state."+Environment.NewLine+"  2 - 00000002 - Rocket - Select the lowest speed/power processor performance state."+Environment.NewLine+"Subgroup / Setting GUIDs:"+Environment.NewLine+"  54533251-82be-4824-96c1-47b60b740d00 / 40fbefc7-2e9d-4d25-a185-0cfd8574bac6",
                Print = (power) => power.DecreasePolicy.ToString(),
            };
            formatters.Add(decreasePolicy);

            var decreaseStabilizationInterval = new Formatter<IPowerConfiguration>
            {
                Header = "DecreaseStabilizationInterval",
                Description = "Short vs. long running thread threshold",
                Help = "Specifies the global threshold that designates which threads have a short versus a long runtime." + Environment.NewLine +
                       "Range, Units:"+Environment.NewLine+"  0 .. 100000 Microseconds",

                Print = (power) => power.DecreaseStabilizationInterval.ToString(),
            };
            formatters.Add(decreaseStabilizationInterval);

            var decreaseThreshold = new Formatter<IPowerConfiguration>
            {
                Header = "DecreaseThreshold %",
                Description = "Processor performance decrease threshold",
                Help = "Specify the lower busy threshold that must be met before decreasing the processor's performance state (in percentage)."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 100 %",
                Print = (power) => power.DecreaseThresholdPercent.ToString(),
            };
            formatters.Add(decreaseThreshold);

            var increasePolicy = new Formatter<IPowerConfiguration>
            {
                Header = "IncreasePolicy",
                Description = "Processor performance increase policy",
                Help = "Specify the algorithm used to select a new performance state when the ideal performance state is higher than the current performance state."+Environment.NewLine+"Possible values (index - hexadecimal or string value - friendly name - descr):"+Environment.NewLine+"  0 - 00000000 - Ideal - Select the ideal processor performance state."+Environment.NewLine+"  1 - 00000001 - Single - Select the processor performance state one closer to ideal than the current processor performance state."+Environment.NewLine+"  2 - 00000002 - Rocket - Select the highest speed/power processor performance state."+Environment.NewLine+"  3 - 00000003 - IdealAggressive - Select the ideal processor performance state optimized for responsiveness"+Environment.NewLine+"Subgroup / Setting GUIDs:"+Environment.NewLine+"  54533251-82be-4824-96c1-47b60b740d00 / 465e1f50-b610-473a-ab58-00d1077dc418",
                Print = (power) => power.IncreasePolicy.ToString(),
            };
            formatters.Add(increasePolicy);

            var increasePolicyPercent = new Formatter<IPowerConfiguration>
            {
                Header = "IncreaseStabilizationInterval",
                Description = "Processor performance increase time",
                Help = "Specify the minimum number of perf check intervals since the last performance state change before the performance state may be increased."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  1 .. 100 Time check intervals",
                Print = (power) => power.IncreaseStabilizationInterval.ToString(),
            };
            formatters.Add(increasePolicyPercent);

            var increaseThresholdPercent = new Formatter<IPowerConfiguration>
            {
                Header = "IncreaseThreshold%",
                Description = "Processor performance increase threshold",
                Help = "Specify the upper busy threshold that must be met before increasing the processor's performance state (in percentage)."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 100 %",
                Print = (power) => power.IncreaseThresholdPercent.ToString(),
            };
            formatters.Add(increaseThresholdPercent);

            var latencySensitivityPerformancePercent = new Formatter<IPowerConfiguration>
            {
                Header = "LatencySensitivityPerformancePercent",
                Description = "Latency sensitivity hint processor performance",
                Help = "Specify the processor performance in response to latency sensitivity hints."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 100 %",
                Print = (power) => power.LatencySensitivityPerformancePercent.ToString(),
            };
            formatters.Add(latencySensitivityPerformancePercent);

            var maxEfficiencyClass0Frequency = new Formatter<IPowerConfiguration>
            {
                Header = "MaxFrequency Class0 MHz",
                Description = "Maximum processor frequency for Efficiency Cores.",
                Help = "Description:"+Environment.NewLine+"  Specify the approximate maximum frequency of your Processor Power Efficiency Class 1 processor (in MHz)."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 4294967295 MHz",
                Print = (power) => power.MaxEfficiencyClass0Frequency.ToString(),
            };
            formatters.Add(maxEfficiencyClass0Frequency);

            var maxEfficiencyClass1Frequency = new Formatter<IPowerConfiguration>
            {
                Header = "MaxFrequency Class1 MHz",
                Description = "Maximum processor frequency for Processor Power Efficiency Class 1 (P-Cores).",
                Help = "Description:"+Environment.NewLine+"  Specify the approximate maximum frequency of your Processor Power Efficiency Class 1 processor (in MHz)."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 4294967295 MHz",
                Print = (power) => power.MaxEfficiencyClass1Frequency.ToString(),
            };
            formatters.Add(maxEfficiencyClass1Frequency);

            var maxThrottlingFrequency = new Formatter<IPowerConfiguration>
            {
                Header = "MaxThrottleFrequency %",
                Description = "Maximum processor state",
                Help = "Specify the maximum performance state of your processor (in percentage). "+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 100 %",
                Print = (power) => power.MaxThrottlingFrequencyPercent.ToString(),
            };
            formatters.Add(maxThrottlingFrequency);

            var minThrottlingFrequency = new Formatter<IPowerConfiguration>
            {
                Header = "MinThrottleFrequency %",
                Description = "Minimum processor state",
                Help = "Specify the minimum performance state of your processor (in percentage). "+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  0 .. 100 %",
                Print = (power) => power.MinThrottlingFrequencyPercent.ToString(),
            };
            formatters.Add(minThrottlingFrequency);

            var stabilizationInterval = new Formatter<IPowerConfiguration>
            {
                Header = "StabilizationInterval",
                Description = "Processor performance time check interval",
                Help = "Specify the amount that must expire before processor performance states and parked cores may be reevaluated (in milliseconds)."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  1 .. 5000 Milliseconds",
                Print = (power) => power.StabilizationInterval.ToString(),
            };
            formatters.Add(stabilizationInterval);

            var systemCoolingPolicy = new Formatter<IPowerConfiguration>
            {
                Header = "SystemCoolingPolicy",
                Description = "System cooling policy",
                Help = "Possible values (index - hexadecimal or string value - friendly name - descr):"+Environment.NewLine+"  0 - 00000001 - Passive - Slow the processor before increasing fan speed"+Environment.NewLine+"  1 - 00000000 - Active - Increase fan speed before slowing the processor",
                Print = (power) => power.SystemCoolingPolicy.ToString(),
            };
            formatters.Add(systemCoolingPolicy);


            var throttlePolicy = new Formatter<IPowerConfiguration>
            {
                Header = "ThrottlePolicy",
                Description = "Allow Throttle States",
                Help = "Allow processors to use throttle states in addition to performance states."+Environment.NewLine+"Possible values (index - hexadecimal or string value - friendly name - descr):"+Environment.NewLine+"  0 - 00000000 - Off - Off"+Environment.NewLine+"  1 - 00000001 - On - On"+Environment.NewLine+"  2 - 00000002 - Automatic - Automatically use throttle states when they are power efficient."+Environment.NewLine+"Subgroup / Setting GUIDs:"+Environment.NewLine+"  54533251-82be-4824-96c1-47b60b740d00 / 3b04d4fd-1cc7-4f23-ab1c-d1337819c4bb",
                Print = (power) => power.ThrottlePolicy.ToString(),
            };
            formatters.Add(throttlePolicy);

            var timeWindowSize = new Formatter<IPowerConfiguration>
            {
                Header = "TimeWindowSize",
                Description = "Processor performance history count",
                Help = "Specify the number of processor performance time check intervals to use when calculating the average utility."+Environment.NewLine+"Range, Units:"+Environment.NewLine+"  1 .. 128 Time check intervals",
                Print = (power) => power.TimeWindowSize.ToString(),
            };

            formatters.Add(timeWindowSize);

            return formatters;
        }

        internal void WriteToCSV(List<MatchData> matches)
        {
            OpenCSVWithHeader(Col_CSVOptions, Col_TestCase, Col_TestTimeinms, Col_SourceJsonFile, Col_Machine,
                "Profile Recorded At (s)",
                "BoostMode","Boost Policy %", 
                "DecreasePolicy", "DecreaseStabilizationInterval", "DecreaseThreshold %", "IncreasePolicy",
                "IncreaseStabilizationInterval", "IncreaseThreshold %", "LatencySensitivityPerformance %", "MaxEfficiencyClass0Frequency",
                "MaxEfficiencyClass1Frequency", "MaxThrottlingFrequency %", "MinThrottlingFrequency %",
                "StabilizationInterval", "SystemCoolingPolicy", "ThrottlePolicy", "TimeWindowSize",

                "Parking - ConcurrencyHeadroomThreshold %", "Parking - ConcurrencyThreshold %", "Parking - MaxEfficiencyClass1UnparkedProcessor %",
                "Parking - MaxUnparkedProcessor %", "Parking - MinEfficiencyClass1UnparkedProcessor %", "Parking - MinParkedDuration",
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

                WriteCSVLine(CSVOptions, match.TestName, match.TestDurationInMs, match.SourceFile, match.Machine, 
                    power.TimeSinceTraceStartS,
                    power.BoostMode, power.BoostPolicyPercent,
                    power.DecreasePolicy, power.DecreaseStabilizationInterval, power.DecreaseThresholdPercent, power.IncreasePolicy,
                    power.IncreaseStabilizationInterval, power.IncreaseThresholdPercent, power.LatencySensitivityPerformancePercent, power.MaxEfficiencyClass0Frequency,
                    power.MaxEfficiencyClass1Frequency, power.MaxThrottlingFrequencyPercent, power.MinThrottlingFrequencyPercent, 
                    power.StabilizationInterval, power.SystemCoolingPolicy, power.ThrottlePolicy, power.TimeWindowSize,

                    parking.ConcurrencyHeadroomThresholdPercent, parking.ConcurrencyThresholdPercent, parking.MaxEfficiencyClass1UnparkedProcessorPercent, 
                    parking.MaxUnparkedProcessorPercent, parking.MinEfficiencyClass1UnparkedProcessorPercent, parking.MinParkedDuration,
                    parking.MinUnparkedDuration, parking.MinUnparkedProcessorPercent, parking.OverUtilizationThresholdPercent,
                    parking.ParkingPerformanceState, parking.ParkingPolicy, parking.UnparkingPolicy, 
                    parking.UtilityDistributionEnabled, parking.UtilityDistributionThresholdPercent,

                    idle.DeepestIdleState, idle.DemoteThresholdPercent,idle.Enabled, idle.MinimumDurationBetweenChecks, idle.PromoteThresholdPercent,
                    idle.ScalingEnabled
                    ) ;
            }
        }

        public class MatchData
        {
            public string SourceFile { get; internal set; }
            public string TestName { get; set; }
            public DateTime PerformedAt { get; set; }
            public int DurationInMs { get; set; }
            public DateTimeOffset SessionStart { get; internal set; }
            public IPowerConfiguration PowerConfiguration { get; internal set; }
    public int TestDurationInMs { get; internal set; }
            public string Machine { get; internal set; }
        }
    }

}
