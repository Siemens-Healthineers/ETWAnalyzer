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
            Assert.Equal(new DateTimeOffset(2000, 1, 1, 8, 0,  5, TimeSpan.Zero), result.FirstCallTime);
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
                MethodFilter = new KeyValuePair<string, Func<string, bool>>(DummyMethod,  p => p == DummyMethod),
                ZeroTimeFilter = new KeyValuePair<string, Func<string,bool>>(DummyMethod, p => p == DummyMethod),
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

        [Fact]
        public void FileSorting_AndTotalCalculation()
        {
            ETWProcess cmdProcess = new ETWProcess
            {
                ProcessID = 1234,
                ProcessName = "cmd.exe",
                CmdLine = "hi",
            };
            ProcessKey cmdProcessKey = cmdProcess.ToProcessKey();

            ETWProcess cmdProcess2 = new ETWProcess
            {
                ProcessID = 2222,
                ProcessName = "2222.exe",
                CmdLine = "hi",
            };
            ProcessKey cmdProcssKey2 = cmdProcess2.ToProcessKey();

            List<DumpCPUMethod.MatchData> data = new List<DumpCPUMethod.MatchData>
            {
                new DumpCPUMethod.MatchData
                {
                    CPUMs = 1,
                    WaitMs = 100,
                    ReadyMs = 50,
                    Method = "Wait100MsMethod_1msCPU",
                    Process = cmdProcess,
                    ProcessKey = cmdProcessKey,
                    SourceFile = "File1.json"
                },
                new DumpCPUMethod.MatchData
                {
                    CPUMs = 5,
                    WaitMs = 200,
                    ReadyMs = 100,
                    Method = "Wait200MsMethod_5msCPU",
                    Process = cmdProcess,
                    ProcessKey = cmdProcessKey,
                    SourceFile = "File1.json"
                },
                new DumpCPUMethod.MatchData
                {
                    CPUMs = 15000,
                    WaitMs = 900,
                    ReadyMs = 40,
                    Method = "Wait900MsMethod_15000msCPU",
                    Process = cmdProcess2,
                    ProcessKey = cmdProcssKey2,
                    SourceFile = "File2.json"
                },
                new DumpCPUMethod.MatchData
                {
                    CPUMs = 1000,
                    WaitMs = 5000,
                    ReadyMs = 100,
                    Method = "Wait500MsMethod_1000msCPU",
                    Process = cmdProcess2,
                    ProcessKey = cmdProcssKey2,
                    SourceFile = "File2.json"
                }
            };

            DumpCPUMethod dumper = new();

            var (fileTotals, processTotals) = dumper.GetFileAndProcessTotals(data);
            // Todo
            // Assert if file and process totals are correct
            Assert.Equal(1, fileTotals["File1.json"].WaitMs);
            Assert.Equal(1, fileTotals["File1.json"].CPUMs);
            Assert.Equal(1, fileTotals["File1.json"].ReadyMs);

            Func<IGrouping<string, DumpCPUMethod.MatchData>, decimal> sorter = dumper.CreateFileSorter(fileTotals);

            List<IGrouping<string, DumpCPUMethod.MatchData>> fileGroups = data.GroupBy(x => x.SourceFile).OrderBy(sorter).ToList();
            // Todo
            // Assert if files are sorted by current sort order (CPU)

            // Todo: Add tests for sort order wait and ready times
            dumper.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Wait;
            dumper.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Ready;



        }
    }
}
