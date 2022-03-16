//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest.Extract
{
    public class ETWProcessTests
    {
        [Fact]
        public void Can_Detect_CrashedProcess()
        {
            unchecked
            {
                Assert.True(ETWProcess.IsPossibleCrash((int)NtStatus.CLR_Exception));
            }
            Assert.False(ETWProcess.IsPossibleCrash(null));
            Assert.False(ETWProcess.IsPossibleCrash(0));
            Assert.False(ETWProcess.IsPossibleCrash(-1));
        }

        [Fact]
        public void ProcessStates_Match_Returns_CorrectResults()
        {
            var startStopped = new ETWProcess
            {
                IsNew = true,
                HasEnded = true,
                ProcessName = "cmd.exe",
                ProcessID = 100
            };

            Assert.False(startStopped.IsMatch(ETWProcess.ProcessStates.OnlyStopped));
            Assert.False(startStopped.IsMatch(ETWProcess.ProcessStates.OnlyStarted));
            Assert.True(startStopped.IsMatch(ETWProcess.ProcessStates.Started));
            Assert.True(startStopped.IsMatch(ETWProcess.ProcessStates.Stopped));
            Assert.False(startStopped.IsMatch(ETWProcess.ProcessStates.None));

            var startedOnly = new ETWProcess
            {
                IsNew = true,
                HasEnded = false,
                ProcessName = "cmd.exe",
                ProcessID = 200
            };

            Assert.False(startedOnly.IsMatch(ETWProcess.ProcessStates.OnlyStopped));
            Assert.True(startedOnly.IsMatch(ETWProcess.ProcessStates.OnlyStarted));
            Assert.True(startedOnly.IsMatch(ETWProcess.ProcessStates.Started));
            Assert.False(startedOnly.IsMatch(ETWProcess.ProcessStates.Stopped));
            Assert.False(startedOnly.IsMatch(ETWProcess.ProcessStates.None));

            var endedOnly = new ETWProcess
            {
                IsNew = false,
                HasEnded = true,
                ProcessName = "cmd.exe",
                ProcessID = 300
            };

            Assert.True(endedOnly.IsMatch(ETWProcess.ProcessStates.OnlyStopped));
            Assert.False(endedOnly.IsMatch(ETWProcess.ProcessStates.OnlyStarted));
            Assert.False(endedOnly.IsMatch(ETWProcess.ProcessStates.Started));
            Assert.True(endedOnly.IsMatch(ETWProcess.ProcessStates.Stopped));
            Assert.False(endedOnly.IsMatch(ETWProcess.ProcessStates.None));

            var immortal = new ETWProcess
            {
                IsNew = false,
                HasEnded = false,
                ProcessName = "cmd.exe",
                ProcessID = 400
            };

            Assert.False(immortal.IsMatch(ETWProcess.ProcessStates.OnlyStopped));
            Assert.False(immortal.IsMatch(ETWProcess.ProcessStates.OnlyStarted));
            Assert.False(immortal.IsMatch(ETWProcess.ProcessStates.Started));
            Assert.False(immortal.IsMatch(ETWProcess.ProcessStates.Stopped));
            Assert.True(immortal.IsMatch(ETWProcess.ProcessStates.None));


        }
    }
}
