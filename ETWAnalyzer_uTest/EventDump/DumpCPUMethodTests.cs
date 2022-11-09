//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.EventDump;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest.EventDump
{
    public class DumpCPUMethodTests
    {
        const string DummyMethod = "DummyMethod";

        [Fact]
        public void ZeroPoint_ProcessEnd_Shifts_Time_Correctly()
        {
            using ITempOutput tempDir = TempDir.Create();
            DumpCPUMethod processEndDump = new DumpCPUMethod()
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
            DumpCPUMethod processStart = new DumpCPUMethod()
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
            DumpCPUMethod dumpLast = new DumpCPUMethod()
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
            DumpCPUMethod dumpFirst = new DumpCPUMethod()
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
            DumpCPUMethod dumpFirst = new DumpCPUMethod()
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

            TestDataFile file = new TestDataFile("Test", fileName, new DateTime(2000, 1, 1), 5000, 100, "TestMachine", "None", true);
            DateTimeOffset TenClock = new DateTimeOffset(2000, 1, 1, 10, 0, 0, TimeSpan.Zero); // start at 10:00:00
            var extract = new ETWExtract()
            {
                SessionStart = TenClock,
            };

            ETWProcess newCmd = new ETWProcess
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

            List<string> MethodListForString = new List<string>
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

            SingleTest t = new SingleTest(new TestDataFile[] { file });

            return t;
        }


        static readonly  ETWProcess myCmdProcess = new ETWProcess
        {
            ProcessID = 1234,
            ProcessName = "cmd.exe",
            CmdLine = "hi",
        };

        static readonly ProcessKey myCmdProcessKey = myCmdProcess.ToProcessKey();

        static readonly ETWProcess myCmdProcess2 = new ETWProcess
        {
            ProcessID = 2222,
            ProcessName = "2222.exe",
            CmdLine = "hi",
        };
        static readonly  ProcessKey myCmdProcssKey2 = myCmdProcess2.ToProcessKey();


        const string File1 = "File1.json";
        const string File2 = "File2.json";

        List<DumpCPUMethod.MatchData> CreateTestData()
        {
          

            List<DumpCPUMethod.MatchData> data = new List<DumpCPUMethod.MatchData>
            {
                new DumpCPUMethod.MatchData
                {
                    CPUMs = 1,
                    WaitMs = 100,
                    ReadyMs = 50,
                    Method = "Wait100MsMethod_1msCPU",
                    Process = myCmdProcess,
                    ProcessKey = myCmdProcessKey,
                    SourceFile = File1
                },
                new DumpCPUMethod.MatchData
                {
                    CPUMs = 5,
                    WaitMs = 200,
                    ReadyMs = 100,
                    Method = "Wait200MsMethod_5msCPU",
                    Process = myCmdProcess,
                    ProcessKey = myCmdProcessKey,
                    SourceFile = File1
                },
                new DumpCPUMethod.MatchData
                {
                    CPUMs = 15000,
                    WaitMs = 900,
                    ReadyMs = 40,
                    Method = "Wait900MsMethod_15000msCPU",
                    Process = myCmdProcess2,
                    ProcessKey = myCmdProcssKey2,
                    SourceFile = File2
                },
                new DumpCPUMethod.MatchData
                {
                    CPUMs = 1000,
                    WaitMs = 5000,
                    ReadyMs = 100,
                    Method = "Wait500MsMethod_1000msCPU",
                    Process = myCmdProcess2,
                    ProcessKey = myCmdProcssKey2,
                    SourceFile = File2

                },
            };

            return data;
        }

        [Fact]
        public void File_TotalSortOrder_Default()
        {
            DumpCPUMethod dumper = new();

            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            Assert.Equal(File1, fileGroups[0].Key);
            Assert.Equal(File2, fileGroups[1].Key);
        }

        [Fact]
        public void File_TotalSortOrder_CPU()
        {

            DumpCPUMethod dumper = new();
            dumper.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.CPU;
            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            Assert.Equal(File1, fileGroups[0].Key);
            Assert.Equal(File2, fileGroups[1].Key);

        }

        [Fact]
        public void File_TotalSortOrder_Wait()
        {
            DumpCPUMethod dumper = new();
            dumper.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Wait;
            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            Assert.Equal(File1, fileGroups[0].Key);
            Assert.Equal(File2, fileGroups[1].Key);
        }

        [Fact]
        public void File_TotalSortOrder_Ready()
        {
            DumpCPUMethod dumper = new();
            dumper.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Ready;
            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            Assert.Equal(File1, fileGroups[0].Key);
            Assert.Equal(File2, fileGroups[1].Key);
        }

        [Fact]
        public void File_TotalSortOrder_TopN1_Default()
        {
            DumpCPUMethod dumper = new();
            dumper.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Default;
            dumper.TopN = new ETWAnalyzer.Infrastructure.SkipTakeRange(1, null);

            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            Assert.Equal(File1, fileGroups[0].Key);
            Assert.Equal(File2, fileGroups[1].Key);
        }


        [Fact]
        public void File_TotalSortOrder_TopN1_CPU()
        {
            DumpCPUMethod dumper = new();
            dumper.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.CPU;
            dumper.TopN = new ETWAnalyzer.Infrastructure.SkipTakeRange(1, null);

            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            Assert.Equal(File1, fileGroups[0].Key);
            Assert.Equal(File2, fileGroups[1].Key);
        }

        [Fact]
        public void File_TotalSortOrder_TopN1_Ready()
        {
            DumpCPUMethod dumper = new();
            dumper.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Ready;
            dumper.TopN = new ETWAnalyzer.Infrastructure.SkipTakeRange(1, null);

            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            Assert.Equal(File1, fileGroups[0].Key);
            Assert.Equal(File2, fileGroups[1].Key);
        }


        [Fact]
        public void File_TotalSortOrder_TopN1_Wait()
        {

            DumpCPUMethod dumper = new();
            dumper.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Wait;
            dumper.TopN = new ETWAnalyzer.Infrastructure.SkipTakeRange(1, null);

            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            Assert.Equal(File1, fileGroups[0].Key);
            Assert.Equal(File2, fileGroups[1].Key);
        }

        [Fact]
        public void Total_File_Process_Calculation()
        {
           
            DumpCPUMethod dumper = new();

            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            // Todo
            // Assert if file and process totals are correct
            Assert.Equal(300, fileTotals[File1].WaitMs);
            Assert.Equal(6, fileTotals[File1].CPUMs);
            Assert.Equal(150, fileTotals[File1].ReadyMs);

            Assert.Equal(5900, fileTotals[File2].WaitMs);
            Assert.Equal(16000, fileTotals[File2].CPUMs);
            Assert.Equal(140, fileTotals[File2].ReadyMs);


        }

        [Fact]
        public void Total_File_Process_Calculation_TopN1_Wait()
        {

            DumpCPUMethod dumper = new();
            dumper.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Wait;
            dumper.TopN = new ETWAnalyzer.Infrastructure.SkipTakeRange(1, null);

            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            // Todo
            // Assert if file and process totals are correct
            Assert.Equal(300, fileTotals[File1].WaitMs);
            Assert.Equal(6, fileTotals[File1].CPUMs);
            Assert.Equal(150, fileTotals[File1].ReadyMs);

            Assert.Equal(5900, fileTotals[File2].WaitMs);
            Assert.Equal(16000, fileTotals[File2].CPUMs);
            Assert.Equal(140, fileTotals[File2].ReadyMs);
        }

        [Fact]
        public void Total_File_Process_Calculation_TopN1_Ready()
        {

            DumpCPUMethod dumper = new();
            dumper.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Ready;
            dumper.TopN = new ETWAnalyzer.Infrastructure.SkipTakeRange(1, null);

            var data = CreateTestData();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            // Todo
            // Assert if file and process totals are correct
            Assert.Equal(300, fileTotals[File1].WaitMs);
            Assert.Equal(6, fileTotals[File1].CPUMs);
            Assert.Equal(150, fileTotals[File1].ReadyMs);

            Assert.Equal(5900, fileTotals[File2].WaitMs);
            Assert.Equal(16000, fileTotals[File2].CPUMs);
            Assert.Equal(140, fileTotals[File2].ReadyMs);
        }
    }
}
