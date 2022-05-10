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


    public class DumpFileDirBaseTests 
    {

        class Dummy : DumpFileDirBase<string>
        {
            public override List<string> ExecuteInternal()
            {
                return new List<string> { };
            }

            public bool IsMatching(TestDataFile file, ProcessKey index)
            {
                return IsMatchingProcessAndCmdLine(file, index);
            }
        }

        [Fact]
        public void CanFilter_Process()
        {
            Dummy dummy = new Dummy();
            TestDataFile file = new TestDataFile("Test", "test.etl", new DateTime(2000, 1, 1), 5000, 100, "TestMachine", "None", true);
            var extract = new ETWExtract();
            extract.Processes.Add(new ETWProcess
            {
                ProcessID = 1234,
                ProcessName = "cmd.exe",
                CmdLine = "hi"
            });

            extract.Processes.Add(new ETWProcess
            {
                ProcessID = 1235,
                ProcessName = "cmd.exe",
                CmdLine = "this is a test",
            });

            extract.Processes.Add(new ETWProcess
            {
                ProcessID = 500,
                ProcessName = "cmd.exe",
                CmdLine = null,
            });

            file.Extract = extract;
            ProcessKey p0 = extract.Processes[0].ToProcessKey();
            ProcessKey p1 = extract.Processes[1].ToProcessKey();
            ProcessKey p2 = extract.Processes[2].ToProcessKey();

            Assert.True(dummy.IsMatching(file, p0));
            Assert.True(dummy.IsMatching(file, p1));
            Assert.True(dummy.IsMatching(file, p2));

            dummy.ProcessNameFilter = Matcher.CreateMatcher("a.exe");
            Assert.False(dummy.IsMatching(file, p0));
            Assert.False(dummy.IsMatching(file, p1));
            Assert.False(dummy.IsMatching(file, p2));

            dummy.ProcessNameFilter = Matcher.CreateMatcher("1234", MatchingMode.CaseInsensitive, pidFilterFormat: true);
            Assert.True(dummy.IsMatching(file, p0));
            Assert.False(dummy.IsMatching(file, p1));
            Assert.False(dummy.IsMatching(file, p2));

            dummy.ProcessNameFilter = Matcher.CreateMatcher("cmd.exe;!1234", MatchingMode.CaseInsensitive, pidFilterFormat: true);
            Assert.False(dummy.IsMatching(file, p0));
            Assert.True(dummy.IsMatching(file, p1));
            Assert.True(dummy.IsMatching(file, p2));


        }
    }
}
