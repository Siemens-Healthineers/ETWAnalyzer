//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.EventDump;
using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
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
            DumpCPUMethod processEndDump = new DumpCPUMethod()
            {
                ZeroTimeMode = ETWAnalyzer.Commands.DumpCommand.ZeroTimeModes.ProcessEnd,
                ZeroTimeProcessNameFilter = p => p == "cmd.exe(1234)",
                MethodFilter = new KeyValuePair<string, Func<string, bool>>("SomeMethod", p => true),
            };

            processEndDump.myPreloadedTests = new Lazy<SingleTest>[] { new Lazy<SingleTest>(CreateSingleTest) };

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
            DumpCPUMethod processStart = new DumpCPUMethod()
            {
                ZeroTimeMode = ETWAnalyzer.Commands.DumpCommand.ZeroTimeModes.ProcessStart,
                ZeroTimeProcessNameFilter = p => p == "cmd.exe(1234)",
                MethodFilter = new KeyValuePair<string, Func<string, bool>>("SomeMethod", p => true),
            };

            processStart.myPreloadedTests = new Lazy<SingleTest>[] { new Lazy<SingleTest>(CreateSingleTest) };

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
            DumpCPUMethod dumpLast = new DumpCPUMethod()
            {
                ZeroTimeMode = ETWAnalyzer.Commands.DumpCommand.ZeroTimeModes.Last,
                ZeroTimeProcessNameFilter = p => p == "cmd.exe(1234)",
                MethodFilter = new KeyValuePair<string, Func<string, bool>>(DummyMethod,  p => p == DummyMethod),
                ZeroTimeFilter = new KeyValuePair<string, Func<string,bool>>(DummyMethod, p => p == DummyMethod),
            };

            dumpLast.myPreloadedTests = new Lazy<SingleTest>[] { new Lazy<SingleTest>(CreateSingleTest) };

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
            DumpCPUMethod dumpFirst = new DumpCPUMethod()
            {
                ZeroTimeMode = ETWAnalyzer.Commands.DumpCommand.ZeroTimeModes.First,
                ZeroTimeProcessNameFilter = p => p == "cmd.exe(1234)",
                MethodFilter = new KeyValuePair<string, Func<string, bool>>(DummyMethod, p => p == DummyMethod),
                ZeroTimeFilter = new KeyValuePair<string, Func<string, bool>>(DummyMethod, p => p == DummyMethod),
            };

            dumpFirst.myPreloadedTests = new Lazy<SingleTest>[] { new Lazy<SingleTest>(CreateSingleTest) };

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
            DumpCPUMethod dumpFirst = new DumpCPUMethod()
            {
                ZeroTimeMode = ETWAnalyzer.Commands.DumpCommand.ZeroTimeModes.None,
                ZeroTimeProcessNameFilter = p => p == "cmd.exe(1234)",
                MethodFilter = new KeyValuePair<string, Func<string, bool>>(DummyMethod, p => p == DummyMethod),
            };

            dumpFirst.myPreloadedTests = new Lazy<SingleTest>[] { new Lazy<SingleTest>(CreateSingleTest) };

            List<DumpCPUMethod.MatchData> matchData = dumpFirst.ExecuteInternal();
            Assert.Single(matchData);
            var result = matchData[0];

            Assert.Equal(new DateTimeOffset(2000, 1, 1, 10, 0, 5, TimeSpan.Zero), result.FirstCallTime);
            Assert.Equal(new DateTimeOffset(2000, 1, 1, 10, 0, 10, TimeSpan.Zero), result.LastCallTime);
            Assert.Equal(5, result.FirstLastCallDurationS);
        }



        SingleTest CreateSingleTest()
        {
            TestDataFile file = new TestDataFile("Test", "test.etl", new DateTime(2000, 1, 1), 5000, 100, "TestMachine", "None", true);
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
                            new MethodCost((MethodIndex) 0, 100,500, 5.0m, 10.0m, 1, 0) { MethodList = MethodListForString },
                        }
                    }
                }
            });

            file.Extract = extract;

            SingleTest t = new SingleTest(new TestDataFile[] { file });

            return t;
        }
    }
}
