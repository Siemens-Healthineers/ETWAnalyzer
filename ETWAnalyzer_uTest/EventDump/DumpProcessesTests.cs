//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.EventDump;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest.EventDump
{
    public class DumpProcessesTests
    {
        [Fact]
        public void Can_Dump_OnlyStarted_Processes()
        {
            DumpProcesses processStart = new DumpProcesses()
            {
                NewProcessFilter = ETWProcess.ProcessStates.OnlyStarted
            };

            processStart.myPreloadedTests = new Lazy<SingleTest>[] { new Lazy<SingleTest>(CreateSingleTest) };
            List<DumpProcesses.MatchData> matching = processStart.ExecuteInternal();

            Assert.Single(matching);
            Assert.Equal("cmdStartOnly.exe", matching[0].ProcessName);
        }

        [Fact]
        public void Can_Dump_OnlyStopped_Processes()
        {
            DumpProcesses processStart = new DumpProcesses()
            {
                NewProcessFilter = ETWProcess.ProcessStates.OnlyStopped
            };

            processStart.myPreloadedTests = new Lazy<SingleTest>[] { new Lazy<SingleTest>(CreateSingleTest) };
            List<DumpProcesses.MatchData> matching = processStart.ExecuteInternal();

            Assert.Single(matching);
            Assert.Equal("cmdEndOnly.exe", matching[0].ProcessName);
        }

        [Fact]
        public void Can_Dump_Started_Processes()
        {
            DumpProcesses processStart = new DumpProcesses()
            {
                NewProcessFilter = ETWProcess.ProcessStates.Started
            };

            processStart.myPreloadedTests = new Lazy<SingleTest>[] { new Lazy<SingleTest>(CreateSingleTest) };
            List<DumpProcesses.MatchData> matching = processStart.ExecuteInternal();

            Assert.Equal(2, matching.Count);
            Assert.Equal("cmdStartStop.exe", matching[1].ProcessName);
            Assert.Equal("cmdStartOnly.exe", matching[0].ProcessName);
        }


        [Fact]
        public void ZeroPoint_ProcessStart_Shifts_Time_Correctly()
        {
            DumpProcesses processStart = new DumpProcesses()
            {
                ZeroTimeMode = ETWAnalyzer.Commands.DumpCommand.ZeroTimeModes.ProcessStart,
                ZeroTimeProcessNameFilter = Matcher.CreateMatcher("cmdStartOnly", MatchingMode.CaseInsensitive, true),
                NewProcessFilter = ETWProcess.ProcessStates.Started,

            };

            processStart.myPreloadedTests = new Lazy<SingleTest>[] { new Lazy<SingleTest>(CreateSingleTest) };
            List<DumpProcesses.MatchData> matching = processStart.ExecuteInternal();
            
            foreach(var m in matching)
            {
                Assert.Equal(15.0d, m.ZeroTimeS);
            }

            var match = matching.Where(x => x.ProcessName == "cmdStartStop.exe").Single();
            Assert.Equal(TenClock.AddSeconds(10.0d).AddSeconds(-15.0d), match.StartTime);
            Assert.Equal(TenClock.AddSeconds(20.0d).AddSeconds(-15.0d), match.EndTime);
        }


        DateTimeOffset TenClock = new DateTimeOffset(2000, 1, 1, 10, 0, 0, TimeSpan.Zero); // start at 10:00:00

        SingleTest CreateSingleTest()
        {
            TestDataFile file = new TestDataFile("Test", "test.etl", new DateTime(2000, 1, 1), 5000, 100, "TestMachine", "None", true);
            
            var extract = new ETWExtract()
            {
                SessionStart = TenClock,
            };

            ETWProcess startStop = new ETWProcess
            {
                ProcessID = 1234,
                ProcessName = "cmdStartStop.exe",
                CmdLine = "hi",
                HasEnded = true,
                IsNew = true,
                StartTime = TenClock + TimeSpan.FromSeconds(10), // Start at 10:00:10
                EndTime = TenClock + TimeSpan.FromSeconds(20),   // Stop at  10:00:20
            };
            extract.Processes.Add(startStop);

            ETWProcess startOnly = new ETWProcess
            {
                ProcessID = 12345,
                ProcessName = "cmdStartOnly.exe",
                HasEnded = false,
                IsNew = true,
                StartTime = TenClock + TimeSpan.FromSeconds(15)  // Start at 10:00:15
            };
            extract.Processes.Add(startOnly);

            ETWProcess endOnly = new ETWProcess
            {
                ProcessID = 123456,
                ProcessName = "cmdEndOnly.exe",
                HasEnded = true,
                IsNew = false,
                EndTime = TenClock + TimeSpan.FromSeconds(5),   // End at 10:00:05
            };
            extract.Processes.Add(endOnly);

            ETWProcess immortal = new ETWProcess
            {
                ProcessID = 4,
                ProcessName = "cmdImmortal.exe",
                HasEnded = false,
                IsNew = false,
            };
            extract.Processes.Add(immortal);

            file.Extract = extract;
            SingleTest t = new SingleTest(new TestDataFile[] { file });

            return t;
        }
    }
}
