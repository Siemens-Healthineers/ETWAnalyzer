//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Commands;
using ETWAnalyzer.EventDump;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extractors.CPU;
using ETWAnalyzer.Helper;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer_uTest.TestInfrastructure;
using Microsoft.Windows.EventTracing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static ETWAnalyzer.Commands.DumpCommand;

namespace ETWAnalyzer_uTest.EventDump
{
    public class DumpCPUMethodTests
    {
        const string DummyMethod = "DummyMethod";

        private readonly ITestOutputHelper myWriter;

        public DumpCPUMethodTests(ITestOutputHelper myWriter)
        {
            this.myWriter = myWriter;
        }

        [Fact]
        public void ZeroPoint_ProcessEnd_Shifts_Time_Correctly()
        {
            using ITempOutput tempDir = TempDir.Create();
            DumpCPUMethod processEndDump = new()
            {
                ZeroTimeMode = ETWAnalyzer.Commands.DumpCommand.ZeroTimeModes.ProcessEnd,
                ZeroTimeProcessNameFilter = p => p == "cmd.exe(1234)",
                MethodFilter = new KeyValuePair<string, Func<string, bool>>("SomeMethod", p => true),
            };

            processEndDump.myPreloadedTests = new Lazy<SingleTest>[] { new Lazy<SingleTest>(Create(tempDir.Name)) };

            List<DumpCPUMethod.MatchData> matchData = processEndDump.ExecuteInternal();
            Assert.Single(matchData);
            var result = matchData[0];

            // Session starts at 10, process runs 11-12, when we subtract from processEnd-SessionStart we get 2h diff
            Assert.Equal(new DateTimeOffset(2000, 1, 1, 8, 0, 5, TimeSpan.Zero), result.FirstCallTime);
            Assert.Equal(new DateTimeOffset(2000, 1, 1, 8, 0, 10, TimeSpan.Zero), result.LastCallTime);
        }

        [Fact]
        public void ZeroPoint_ProcessStart_Shifts_Time_Correctly()
        {
            using ITempOutput tempDir = TempDir.Create();
            DumpCPUMethod processStart = new()
            {
                ZeroTimeMode = ETWAnalyzer.Commands.DumpCommand.ZeroTimeModes.ProcessStart,
                ZeroTimeProcessNameFilter = p => p == "cmd.exe(1234)",
                MethodFilter = new KeyValuePair<string, Func<string, bool>>("SomeMethod", p => true),
            };

            processStart.myPreloadedTests = new Lazy<SingleTest>[] { new Lazy<SingleTest>(Create(tempDir.Name)) };

            List<DumpCPUMethod.MatchData> matchData = processStart.ExecuteInternal();
            Assert.Single(matchData);
            var result = matchData[0];

            // Session starts at 10, process runs 11-12, when we subtract from Start-SessionStart we get 1h diff
            Assert.Equal(new DateTimeOffset(2000, 1, 1, 9, 0, 5, TimeSpan.Zero), result.FirstCallTime);
            Assert.Equal(new DateTimeOffset(2000, 1, 1, 9, 0, 10, TimeSpan.Zero), result.LastCallTime);
        }

        [Fact]
        public void ZeroPoint_MethodLast_Shifts_Time_Correctly()
        {
            using ITempOutput tempDir = TempDir.Create();
            DumpCPUMethod dumpLast = new()
            {
                ZeroTimeMode = ETWAnalyzer.Commands.DumpCommand.ZeroTimeModes.Last,
                ZeroTimeProcessNameFilter = p => p == "cmd.exe(1234)",
                MethodFilter = new KeyValuePair<string, Func<string, bool>>(DummyMethod, p => p == DummyMethod),
                ZeroTimeFilter = new KeyValuePair<string, Func<string, bool>>(DummyMethod, p => p == DummyMethod),
            };

            dumpLast.myPreloadedTests = new Lazy<SingleTest>[] { new Lazy<SingleTest>(Create(tempDir.Name)) };

            List<DumpCPUMethod.MatchData> matchData = dumpLast.ExecuteInternal();
            Assert.Single(matchData);
            var result = matchData[0];

            Assert.Equal(new DateTimeOffset(2000, 1, 1, 9, 59, 55, TimeSpan.Zero), result.FirstCallTime);
            Assert.Equal(new DateTimeOffset(2000, 1, 1, 10, 0, 0, TimeSpan.Zero), result.LastCallTime);
            Assert.Equal(5, result.FirstLastCallDurationS);
        }

        [Fact]
        public void ZeroPoint_MethodFirst_Shifts_Time_Correctly()
        {
            using ITempOutput tempDir = TempDir.Create();
            DumpCPUMethod dumpFirst = new()
            {
                ZeroTimeMode = ETWAnalyzer.Commands.DumpCommand.ZeroTimeModes.First,
                ZeroTimeProcessNameFilter = p => p == "cmd.exe(1234)",
                MethodFilter = new KeyValuePair<string, Func<string, bool>>(DummyMethod, p => p == DummyMethod),
                ZeroTimeFilter = new KeyValuePair<string, Func<string, bool>>(DummyMethod, p => p == DummyMethod),
            };

            dumpFirst.myPreloadedTests = new Lazy<SingleTest>[] { new Lazy<SingleTest>(Create(tempDir.Name)) };

            List<DumpCPUMethod.MatchData> matchData = dumpFirst.ExecuteInternal();
            Assert.Single(matchData);
            var result = matchData[0];

            Assert.Equal(new DateTimeOffset(2000, 1, 1, 10, 0, 0, TimeSpan.Zero), result.FirstCallTime);
            Assert.Equal(new DateTimeOffset(2000, 1, 1, 10, 0, 5, TimeSpan.Zero), result.LastCallTime);
            Assert.Equal(5, result.FirstLastCallDurationS);
        }

        [Fact]
        public void Unshifted_Methods_Have_Right_Time()
        {
            using ITempOutput tempDir = TempDir.Create();
            DumpCPUMethod dumpFirst = new()
            {
                ZeroTimeMode = ETWAnalyzer.Commands.DumpCommand.ZeroTimeModes.None,
                ZeroTimeProcessNameFilter = p => p == "cmd.exe(1234)",
                MethodFilter = new KeyValuePair<string, Func<string, bool>>(DummyMethod, p => p == DummyMethod),
            };

            dumpFirst.myPreloadedTests = new Lazy<SingleTest>[] { new Lazy<SingleTest>(Create(tempDir.Name)) };

            List<DumpCPUMethod.MatchData> matchData = dumpFirst.ExecuteInternal();
            Assert.Single(matchData);
            var result = matchData[0];

            Assert.Equal(new DateTimeOffset(2000, 1, 1, 10, 0, 5, TimeSpan.Zero), result.FirstCallTime);
            Assert.Equal(new DateTimeOffset(2000, 1, 1, 10, 0, 10, TimeSpan.Zero), result.LastCallTime);
            Assert.Equal(5, result.FirstLastCallDurationS);
        }


        /// <summary>
        /// Create delegate which captures context
        /// </summary>
        /// <param name="directory"></param>
        /// <returns>Func which Lazy expects</returns>
        Func<SingleTest> Create(string directory) => () => CreateSingleTest(directory);

        SingleTest CreateSingleTest(string directory)
        {
            string fileName = Path.Combine(directory, "test.json");
            File.WriteAllText(fileName, "Hi world"); // create file

            TestDataFile file = new("Test", fileName, new DateTime(2000, 1, 1), 5000, 100, "TestMachine", "None", true);
            DateTimeOffset TenClock = new(2000, 1, 1, 10, 0, 0, TimeSpan.Zero); // start at 10:00:00
            var extract = new ETWExtract()
            {
                SessionStart = TenClock,
            };

            ETWProcess newCmd = new()
            {
                ProcessID = 1234,
                ProcessName = "cmd.exe",
                CmdLine = "hi",
                HasEnded = true,
                IsNew = true,
                StartTime = TenClock + TimeSpan.FromHours(1), // Start at 11:00:00
                EndTime = TenClock + TimeSpan.FromHours(2), // Stop at  12:00:00
            };
            extract.Processes.Add(newCmd);
            ProcessKey newCmdKey = newCmd.ToProcessKey();

            List<string> MethodListForString = new()
            {
                DummyMethod,
            };

            extract.CPU = new CPUStats(null, new CPUPerProcessMethodList()
            {
                MethodStatsPerProcess = new List<MethodsByProcess>
                {
                    new MethodsByProcess(newCmdKey)
                    {
                        Costs =
                        {
                            new MethodCost((MethodIndex) 0, 100,500, 5.0m, 10.0m, 1, 0, 0) { MethodList = MethodListForString },
                        }
                    }
                }
            }, null);

            file.Extract = extract;

            SingleTest t = new(new TestDataFile[] { file });

            return t;
        }


        static readonly ETWProcess myCmdProcess = new()
        {
            ProcessID = 1234,
            ProcessName = "cmd.exe",
            CmdLine = "hi",
        };

        static readonly ProcessKey myCmdProcessKey = myCmdProcess.ToProcessKey();

        static readonly ETWProcess myCmdProcess2 = new()
        {
            ProcessID = 2222,
            ProcessName = "2222.exe",
            CmdLine = "hi",
        };
        static readonly ProcessKey myCmdProcessKey2 = myCmdProcess2.ToProcessKey();


        const string File1 = "File1.json";
        const string File2 = "File2.json";
        const string File3 = "File3.json";

        List<DumpCPUMethod.MatchData> CreateTestData()
        {

            DateTime time_500 = new DateTime(500, 1, 1);
            DateTime time_1000 = new DateTime(1000, 1, 1);
            DateTime time_1500 = new DateTime(1500, 1, 1);

            List<DumpCPUMethod.MatchData> data = new()
            {
                new DumpCPUMethod.MatchData
                {
                    CPUMs = 1,
                    WaitMs = 100,
                    ReadyMs = 50,
                    Method = "Wait100MsMethod_1msCPU",
                    Process = myCmdProcess,
                    ProcessKey = myCmdProcessKey,
                    SourceFile = File1,
                    PerformedAt = time_1500,
                },
                new DumpCPUMethod.MatchData
                {
                    CPUMs = 5,
                    WaitMs = 200,
                    ReadyMs = 100,
                    Method = "Wait200MsMethod_5msCPU",
                    Process = myCmdProcess,
                    ProcessKey = myCmdProcessKey,
                    SourceFile = File1,
                    PerformedAt = time_1500,
                },
                new DumpCPUMethod.MatchData
                {
                    CPUMs = 15000,
                    WaitMs = 900,
                    ReadyMs = 40,
                    Method = "Wait900MsMethod_15000msCPU",
                    Process = myCmdProcess2,
                    ProcessKey = myCmdProcessKey2,
                    SourceFile = File2,
                    PerformedAt = time_1500,
                },
                new DumpCPUMethod.MatchData
                {
                    CPUMs = 1000,
                    WaitMs = 5000,
                    ReadyMs = 100,
                    Method = "Wait5000MsMethod_1000msCPU",
                    Process = myCmdProcess2,
                    ProcessKey = myCmdProcessKey2,
                    SourceFile = File2,
                    PerformedAt = time_1500,
                },
                new DumpCPUMethod.MatchData
                {
                    CPUMs = 1,
                    WaitMs = 1,
                    ReadyMs = 1,
                    Method = "Wait1MsMethod_1msCPU",
                    Process = myCmdProcess,
                    ProcessKey = myCmdProcessKey,
                    SourceFile = File3,
                    PerformedAt = time_500,
                },
                new DumpCPUMethod.MatchData
                {
                    CPUMs = 1,
                    WaitMs = 1,
                    ReadyMs = 5000,
                    Method = "Wait1MsMethod_1msCPU",
                    Process = myCmdProcess2,
                    ProcessKey = myCmdProcessKey2,
                    SourceFile = File3,
                    PerformedAt = time_500,

                },
            };

            return data;
        }

        [Fact]
        public void File_TotalSortOrder_Default()
        {
            DumpCPUMethod dumper = new();

            List<DumpCPUMethod.MatchData> data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            Assert.Equal(File3, fileGroups[0].Key);
            Assert.Equal(File1, fileGroups[1].Key);
            Assert.Equal(File2, fileGroups[2].Key);
        }

        [Fact]
        public void File_TotalSortOrder_CPU()
        {

            DumpCPUMethod dumper = new();
            dumper.SortOrder = SortOrders.CPU;
            dumper.ShowTotal = TotalModes.Process;

            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            Assert.Equal(3, fileTotals.Count);
            Assert.Equal(File3, fileGroups[0].Key);
            Assert.Equal(File1, fileGroups[1].Key);
            Assert.Equal(File2, fileGroups[2].Key);

            Assert.Equal(6, fileTotals[File1].CPUMs);
            Assert.Equal(16000, fileTotals[File2].CPUMs);
            Assert.Equal(2, fileTotals[File3].CPUMs);

        }

        [Fact]
        public void File_TotalSortOrder_Wait()
        {
            DumpCPUMethod dumper = new();
            dumper.SortOrder = SortOrders.Wait;
            dumper.ShowTotal = TotalModes.Process;
            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            Assert.Equal(File3, fileGroups[0].Key);
            Assert.Equal(File1, fileGroups[1].Key);
            Assert.Equal(File2, fileGroups[2].Key);

            Assert.Equal(300, fileTotals[File1].WaitMs);
            Assert.Equal(5900, fileTotals[File2].WaitMs);
            Assert.Equal(2, fileTotals[File3].WaitMs);
        }

        [Fact]
        public void File_TotalSortOrder_Ready()
        {
            DumpCPUMethod dumper = new();
            dumper.SortOrder = SortOrders.Ready;
            dumper.ShowTotal = TotalModes.Process;
            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            Assert.Equal(File2, fileGroups[0].Key);
            Assert.Equal(File1, fileGroups[1].Key);
            Assert.Equal(File3, fileGroups[2].Key);

            Assert.Equal(150, fileTotals[File1].ReadyMs);
            Assert.Equal(140, fileTotals[File2].ReadyMs);
            Assert.Equal(5001, fileTotals[File3].ReadyMs);
        }

        [Fact]
        public void File_TotalSortOrder_TopN1_Default()
        {
            DumpCPUMethod dumper = new();
            dumper.SortOrder = SortOrders.Default;
            dumper.ShowTotal = TotalModes.Process;
            dumper.TopN = new ETWAnalyzer.Infrastructure.SkipTakeRange(1, null);

            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            Assert.Equal(File3, fileGroups[0].Key);
            Assert.Equal(File1, fileGroups[1].Key);
            Assert.Equal(File2, fileGroups[2].Key);

            Assert.Equal(6, fileTotals[File1].CPUMs);
            Assert.Equal(16000, fileTotals[File2].CPUMs);
            Assert.Equal(1, fileTotals[File3].CPUMs);
        }


        [Fact]
        public void File_TotalSortOrder_TopN1_CPU()
        {
            DumpCPUMethod dumper = new();
            dumper.SortOrder = SortOrders.CPU;
            dumper.ShowTotal = TotalModes.Process;
            dumper.TopN = new ETWAnalyzer.Infrastructure.SkipTakeRange(1, null);

            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            Assert.Equal(File3, fileGroups[0].Key);
            Assert.Equal(File1, fileGroups[1].Key);
            Assert.Equal(File2, fileGroups[2].Key);

            Assert.Equal(6, fileTotals[File1].CPUMs);
            Assert.Equal(16000, fileTotals[File2].CPUMs);
            Assert.Equal(1, fileTotals[File3].CPUMs);
        }

        [Fact]
        public void File_TotalSortOrder_TopN1_Ready()
        {
            DumpCPUMethod dumper = new();
            dumper.SortOrder = SortOrders.Ready;
            dumper.ShowTotal = TotalModes.Process;
            dumper.TopN = new ETWAnalyzer.Infrastructure.SkipTakeRange(1, null);

            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            Assert.Equal(File2, fileGroups[0].Key);
            Assert.Equal(File1, fileGroups[1].Key);
            Assert.Equal(File3, fileGroups[2].Key);

            Assert.Equal(150, fileTotals[File1].ReadyMs);
            Assert.Equal(140, fileTotals[File2].ReadyMs);
            Assert.Equal(5000, fileTotals[File3].ReadyMs);
        }


        [Fact]
        public void File_TotalSortOrder_TopN1_Wait()
        {

            DumpCPUMethod dumper = new();
            dumper.SortOrder = SortOrders.Wait;
            dumper.ShowTotal = TotalModes.Process;

            dumper.TopN = new ETWAnalyzer.Infrastructure.SkipTakeRange(1, null);

            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            Assert.Equal(File3, fileGroups[0].Key);
            Assert.Equal(File1, fileGroups[1].Key);
            Assert.Equal(File2, fileGroups[2].Key);

            Assert.Equal(300, fileTotals[File1].WaitMs);
            Assert.Equal(5900, fileTotals[File2].WaitMs);
            Assert.Equal(1, fileTotals[File3].WaitMs);
        }

        readonly KeyValuePair<string, MinMaxRange<int>>[] RangeValues = new KeyValuePair<string, MinMaxRange<int>>[]
        {
            new KeyValuePair<string, MinMaxRange<int>>("1", new MinMaxRange<int>(1, int.MaxValue)),
            new KeyValuePair<string, MinMaxRange<int>>("1ms", new MinMaxRange<int>(1, int.MaxValue)),
            new KeyValuePair<string, MinMaxRange<int>>("0.5s", new MinMaxRange<int>(500, int.MaxValue)),
            new KeyValuePair<string, MinMaxRange<int>>("1s-2s", new MinMaxRange<int>(1000, 2000)),
            new KeyValuePair<string, MinMaxRange<int>>("1ms-5000", new MinMaxRange<int>(1, 5000)),
            new KeyValuePair<string, MinMaxRange<int>>("500-1000", new MinMaxRange<int>(500, 1000)),
        };


        [Fact]
        public void CPUMs_Filter()
        {

            foreach (var input in RangeValues)
            {
                var args = new string[] { "-dump", "cpu", "-MinMaxCPUms",input.Key };
                DumpCommand dump = (DumpCommand)CommandFactory.CreateCommand(args);
                dump.Parse();
                dump.Run();
                DumpCPUMethod cpuDumper = (DumpCPUMethod)dump.myCurrentDumper;

                Assert.Equal(input.Value.Min, cpuDumper.MinMaxCPUMs.Min);
                Assert.Equal(input.Value.Max, cpuDumper.MinMaxCPUMs.Max);
            }
        }

        [Fact]
        public void MinMaxFirst_Filter()
        {
            // -MinMaxFirst    MinMaxRange<double> MinMaxFirstS
            // -MinMaxLast     MinMaxRange<double> MinMaxLastS 
            // -MinmaxDuration MinMaxRange<double> MinMaxDurationS
        }

        [Fact]
        public void MinMaxLast_Filter()
        {
            // -MinMaxFirst    MinMaxRange<double> MinMaxFirstS
            // -MinMaxLast     MinMaxRange<double> MinMaxLastS 
            // -MinmaxDuration MinMaxRange<double> MinMaxDurationS
        }


        [Fact]
        public void MinmaxDuration_Filter()
        {
            // -MinMaxFirst    MinMaxRange<double> MinMaxFirstS
            // -MinMaxLast     MinMaxRange<double> MinMaxLastS 
            // -MinmaxDuration MinMaxRange<double> MinMaxDurationS
        }

        [Fact]
        public void WaitMs_Filter()
        {

            foreach (var input in RangeValues)
            {
                var args = new string[] { "-dump", "cpu", "-MinMaxWaitms", input.Key };
                DumpCommand dump = (DumpCommand)CommandFactory.CreateCommand(args);
                dump.Parse();
                dump.Run();
                DumpCPUMethod cpuDumper = (DumpCPUMethod)dump.myCurrentDumper;

                Assert.Equal(input.Value.Min, cpuDumper.MinMaxWaitMs.Min);
                Assert.Equal(input.Value.Max, cpuDumper.MinMaxWaitMs.Max);
            }
        }


        [Fact]
        public void ReadyMs_Filter()
        {

            foreach (var input in RangeValues)
            {
                var args = new string[] { "-dump", "cpu", "-MinMaxReadyMS", input.Key };
                DumpCommand dump = (DumpCommand)CommandFactory.CreateCommand(args);
                dump.Parse();
                dump.Run();
                DumpCPUMethod cpuDumper = (DumpCPUMethod)dump.myCurrentDumper;

                Assert.Equal(input.Value.Min, cpuDumper.MinMaxReadyMs.Min);
                Assert.Equal(input.Value.Max, cpuDumper.MinMaxReadyMs.Max);
            }
        }


        [Fact]
        public void Total_File_Process_Calculation()
        {
           
            DumpCPUMethod dumper = new();
            dumper.ShowTotal = TotalModes.Process;

            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);

            Assert.Equal(300, fileTotals[File1].WaitMs);
            Assert.Equal(6, fileTotals[File1].CPUMs);
            Assert.Equal(150, fileTotals[File1].ReadyMs);

            Assert.Equal(5900, fileTotals[File2].WaitMs);
            Assert.Equal(16000, fileTotals[File2].CPUMs);
            Assert.Equal(140, fileTotals[File2].ReadyMs);

            Assert.Equal(2, fileTotals[File3].WaitMs);
            Assert.Equal(2, fileTotals[File3].CPUMs);
            Assert.Equal(5001, fileTotals[File3].ReadyMs);

            Assert.Equal(6, processTotals[File1][myCmdProcess].CPUMs);
            Assert.Equal(300, processTotals[File1][myCmdProcess].WaitMs);
            Assert.Equal(150, processTotals[File1][myCmdProcess].ReadyMs);

            Assert.Equal(16000, processTotals[File2][myCmdProcess2].CPUMs);
            Assert.Equal(5900, processTotals[File2][myCmdProcess2].WaitMs);
            Assert.Equal(140, processTotals[File2][myCmdProcess2].ReadyMs);

            Assert.Equal(1, processTotals[File3][myCmdProcess].CPUMs);
            Assert.Equal(1, processTotals[File3][myCmdProcess].WaitMs);
            Assert.Equal(1, processTotals[File3][myCmdProcess].ReadyMs);

            Assert.Equal(1, processTotals[File3][myCmdProcess2].CPUMs);
            Assert.Equal(1, processTotals[File3][myCmdProcess2].WaitMs);
            Assert.Equal(5000, processTotals[File3][myCmdProcess2].ReadyMs);
        }

        [Fact]
        public void Total_File_Process_Calculation_TopN1_Wait()
        {

            DumpCPUMethod dumper = new();
            dumper.SortOrder = SortOrders.Wait;
            dumper.ShowTotal = TotalModes.Process;

            dumper.TopN = new ETWAnalyzer.Infrastructure.SkipTakeRange(1, null);

            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);

            Assert.Equal(300, fileTotals[File1].WaitMs);
            Assert.Equal(5900, fileTotals[File2].WaitMs);
            Assert.Equal(1, fileTotals[File3].WaitMs);

            Assert.Equal(300, processTotals[File1][myCmdProcess].WaitMs);

            Assert.Equal(5900, processTotals[File2][myCmdProcess2].WaitMs);

            Assert.Equal(1, processTotals[File3][myCmdProcess].WaitMs);

        }

        [Fact]
        public void Total_File_Process_Calculation_TopN1_Ready()
        {

            DumpCPUMethod dumper = new();
            dumper.ShowTotal = TotalModes.Process;
            dumper.SortOrder = SortOrders.Ready;
            dumper.TopN = new ETWAnalyzer.Infrastructure.SkipTakeRange(1, null);

            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);

            Assert.Equal(150, fileTotals[File1].ReadyMs);
            Assert.Equal(140, fileTotals[File2].ReadyMs);
            Assert.Equal(5000, fileTotals[File3].ReadyMs);
        }

        List<TestDataFile> GetTestData()
        {

            List<TestDataFile> files = new();

            CPUPerProcessMethodList methodList = new();
            ProcessKey proc1 = new("test1.exe", 1024, DateTimeOffset.MinValue);
            ProcessKey proc2 = new("test2.exe", 2000, DateTimeOffset.MinValue);

            methodList.AddMethod(proc1, "Z", new CpuData(new Duration(100_000_000), new Duration(101_000_000), 5m, 6m, 10, 5), cutOffMs: 0);
            methodList.AddMethod(proc1, "Y", new CpuData(new Duration(90_000_000),  new Duration(91_000_000), 4m, 5m, 20, 4), cutOffMs: 0);
            methodList.AddMethod(proc1, "X", new CpuData(new Duration(80_000_000),  new Duration(81_000_000), 3m, 5m, 30, 3), cutOffMs: 0);
            methodList.AddMethod(proc1, "B", new CpuData(new Duration(20_000_000),  new Duration(21_000_000), 2m, 5m, 40, 2), cutOffMs: 0);
            methodList.AddMethod(proc1, "A", new CpuData(new Duration(9_000_000),   new Duration(11_000_000), 1m, 5m, 50, 1), cutOffMs: 0);  
            methodList.AddMethod(proc1, "A0", new CpuData(new Duration(5_000_000),  new Duration(5_000_000), 1m, 5m, 50, 1), cutOffMs: 0);

            methodList.AddMethod(proc2, "Z", new CpuData(new Duration(200_000_000), new Duration(101_000_000), 5m, 6m, 10, 5), cutOffMs: 0);
            methodList.AddMethod(proc2, "Y", new CpuData(new Duration(180_000_000), new Duration(91_000_000), 4m, 5m, 20, 4), cutOffMs: 0);
            methodList.AddMethod(proc2, "X", new CpuData(new Duration(160_000_000), new Duration(81_000_000), 3m, 5m, 30, 3), cutOffMs: 0);
            methodList.AddMethod(proc2, "B", new CpuData(new Duration(40_000_000),  new Duration(21_000_000), 2m, 5m, 40, 2), cutOffMs: 0);
            methodList.AddMethod(proc2, "A", new CpuData(new Duration(18_000_000),  new Duration(11_000_000), 1m, 5m, 50, 1), cutOffMs: 0);
            methodList.AddMethod(proc2, "A0", new CpuData(new Duration(10_000_000), new Duration(5_000_000), 1m, 5m, 50, 1), cutOffMs: 0);


            TestDataFile file = new("Test", "test.json", new DateTime(2000, 1, 1), 500, 1000, "TestMachine", null);
            file.Extract = new ETWExtract
            {
                Processes = new List<ETWProcess>
                {
                    new ETWProcess
                    {
                        ProcessName = "test1.exe",
                        ProcessID = 1024,
                    },
                    new ETWProcess
                    {
                        ProcessName = "test2.exe",
                        ProcessID = 2000,
                    }
                },
                CPU = new CPUStats(new Dictionary<ProcessKey, uint>
                    {
                        { proc1, 5000 },
                        { proc2, 6000 },
                    },
                    methodList,
                    null)
            };

            files.Add(file);

            return files;
        }


       [Fact]
        public void TotalMode_Not_SetMeans_It_Is_Off_CPU_Summary()
        {
            using ExceptionalPrinter redirect = new(myWriter,true);
            using CultureSwitcher invariant = new();

            DumpCPUMethod dumper = new();
            dumper.ShowTotal = null;

            List<DumpCPUMethod.MatchData> matches = new();

            List<TestDataFile> files = GetTestData();

            foreach (var file in files)
            {
                dumper.AddAndPrintTotalStats(matches, file);
            }

            redirect.Flush();
            var lines = redirect.GetSingleLines();
            Assert.Equal(4, lines.Count);
            Assert.Equal("\t      CPU ms Process Name        ",    lines[0]);
            Assert.Equal("1/1/2000 12:00:00 AM    ",               lines[1] );
            Assert.Equal("\t    5,000 ms test1.exe(1024)        ", lines[2]);
            Assert.Equal("\t    6,000 ms test2.exe(2000)        ", lines[3]);
        }


        [Fact]
        public void TotalMode_Total_CPU_Summary()
        {
            using ExceptionalPrinter redirect = new(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpCPUMethod dumper = new();
            // prevent parsing random json files in test directory
            dumper.FileOrDirectoryQueries = new List<string> { "dummy" };

            dumper.ShowTotal = TotalModes.Total;

            List<DumpCPUMethod.MatchData> matches = new();

            List<TestDataFile> files = GetTestData();

            foreach (var file in files)
            {
                dumper.AddAndPrintTotalStats(matches, file);
            }

            redirect.Flush();
            var lines = redirect.GetSingleLines();

            Assert.Equal(2, lines.Count);
            Assert.Equal("\t      CPU ms Process Name        ",     lines[0]);
            Assert.Equal("1/1/2000 12:00:00 AM    CPU 11,000 ms  ", lines[1]);
        }


        [Fact]
        public void TotalMode_Process_CPU_Summary()
        {
            using ExceptionalPrinter redirect = new(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpCPUMethod dumper = new();
            // prevent parsing random json files in test directory
            dumper.FileOrDirectoryQueries = new List<string> { "dummy" };
            dumper.ShowTotal = TotalModes.Process;

            List<DumpCPUMethod.MatchData> matches = new();

            List<TestDataFile> files = GetTestData();

            foreach (var file in files)
            {
                dumper.AddAndPrintTotalStats(matches, file);
            }

            redirect.Flush();
            var lines = redirect.GetSingleLines();

            Assert.Equal(4, lines.Count);
            Assert.Equal("\t      CPU ms Process Name        ",     lines[0]);
            Assert.Equal("1/1/2000 12:00:00 AM    CPU 11,000 ms  ", lines[1]);
            Assert.Equal("\t    5,000 ms test1.exe(1024)        ",  lines[2]);
            Assert.Equal("\t    6,000 ms test2.exe(2000)        ",  lines[3]);
        }

        [Fact]
        public void TotalMode_NotSet_None_CPU_MethodLevel()
        {
            using ExceptionalPrinter redirect = new(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpCPUMethod dumper = new();
            // prevent parsing random json files in test directory
            dumper.FileOrDirectoryQueries = new List<string> { "dummy" };

            dumper.ShowTotal = null;
            dumper.MethodFilter = new KeyValuePair<string, Func<string, bool>>("x", x => true);

            var matches = CreateTestData();
            

            dumper.PrintMatches(matches);

            redirect.Flush();
            var lines = redirect.GetSingleLines();


            Assert.Equal(14, lines.Count);
            Assert.Equal("         CPU ms       Wait msMethod",                    lines[0]);
            Assert.Equal("1/1/0500 12:00:00 AM   File3 ",                             lines[1]);
            Assert.Equal("   2222.exe(2222) hi",                                      lines[2]);
            Assert.Equal("           1 ms          1 ms Wait1MsMethod_1msCPU ",       lines[3]);
            Assert.Equal("   cmd.exe(1234) hi",                                       lines[4]);
            Assert.Equal("      15,000 ms        900 ms Wait900MsMethod_15000msCPU ", lines[13]);
            
        }

        [Fact]
        public void TotalMode_Process_CPU_MethodLevel()
        {
            using ExceptionalPrinter redirect = new(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpCPUMethod dumper = new();
            dumper.ShowTotal = TotalModes.Process;
            dumper.MethodFilter = new KeyValuePair<string, Func<string, bool>>("x", x => true);

            var matches = CreateTestData();


            dumper.PrintMatches(matches);

            redirect.Flush();
            var lines = redirect.GetSingleLines();


            Assert.Equal(8, lines.Count);
            Assert.Equal("1/1/0500 12:00:00 AM    CPU            2 ms Wait            2 ms Total            4 ms File3 ", lines[0]);
            Assert.Equal("   CPU            1 ms Wait:            1 ms Total:            2 ms 2222.exe(2222) hi",         lines[1]);
            Assert.Equal("   CPU            1 ms Wait:            1 ms Total:            2 ms cmd.exe(1234) hi",          lines[2]);
            Assert.Equal("Total 22,210 ms CPU 16,008 ms Wait 6,202 ms",                                                   lines[7]);
        }

        [Fact]
        public void TotalMode_Method_CPU_MethodLevel()
        {
            using CultureSwitcher invariant = new();
            using ExceptionalPrinter redirect = new(myWriter, true);

            DumpCPUMethod dumper = new();
            // prevent parsing random json files in test directory
            dumper.FileOrDirectoryQueries = new List<string> { "dummy" };

            dumper.ShowTotal = TotalModes.Method;
            dumper.MethodFilter = new KeyValuePair<string, Func<string, bool>>("x", x => true);

            var matches = CreateTestData();


            dumper.PrintMatches(matches);

            redirect.Flush();
            var lines = redirect.GetSingleLines();


            Assert.Equal(15, lines.Count);
            Assert.Equal("         CPU ms       Wait msMethod"                                                          ,lines[0]);
            Assert.Equal("1/1/0500 12:00:00 AM    CPU            2 ms Wait            2 ms Total            4 ms File3 ", lines[1]);
            Assert.Equal("   CPU            1 ms Wait:            1 ms Total:            2 ms 2222.exe(2222) hi"        , lines[2]);
            Assert.Equal("           1 ms          1 ms Wait1MsMethod_1msCPU "                                          , lines[3]);
            Assert.Equal("   CPU            1 ms Wait:            1 ms Total:            2 ms cmd.exe(1234) hi",          lines[4]);
            Assert.Equal("Total 22,210 ms CPU 16,008 ms Wait 6,202 ms"                                                  , lines[14]);
        }

        [Fact]
        public void TotalMode_Total_CPU_MethodLevel()
        {
            using ExceptionalPrinter redirect = new(myWriter, true);
            using CultureSwitcher invariant = new();

            DumpCPUMethod dumper = new();
            // prevent parsing random json files in test directory
            dumper.FileOrDirectoryQueries = new List<string> { "dummy" };
            dumper.ShowTotal = TotalModes.Total;
            dumper.MethodFilter = new KeyValuePair<string, Func<string, bool>>("x", x => true);

            var matches = CreateTestData();


            dumper.PrintMatches(matches);

            redirect.Flush();
            var lines = redirect.GetSingleLines();


            Assert.Equal(4, lines.Count);
            Assert.Equal("1/1/0500 12:00:00 AM    CPU            2 ms Wait            2 ms Total            4 ms File3 ", lines[0]);
            Assert.Equal("1/1/1500 12:00:00 AM    CPU            6 ms Wait          300 ms Total          306 ms File1 ", lines[1]);
            Assert.Equal("1/1/1500 12:00:00 AM    CPU       16,000 ms Wait        5,900 ms Total       21,900 ms File2 ", lines[2]);
            Assert.Equal("Total 22,210 ms CPU 16,008 ms Wait 6,202 ms", lines[3]);
        }

    }
}
