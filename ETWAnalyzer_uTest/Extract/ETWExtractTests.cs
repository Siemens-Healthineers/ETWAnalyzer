//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using Xunit;
using ETWAnalyzer;
using System.IO;
using System.Collections.Generic;
using ETWAnalyzer.Extract.Exceptions;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extractors;

namespace ETWAnalyzer_uTest
{
    
    public class ETWExtractTests
    {
        [Fact]
        public void ProcessList_Is_Not_Null()
        {
            ETWExtract c1 = new ETWExtract();
            Assert.NotNull(c1.Processes);
        }


        void AddProcesses(ETWExtract data)
        {
            data.Processes.AddRange( new ETWProcess[]
                {
                    new ETWProcess() { ProcessID = 1, ProcessName = "Tester.exe", CmdLine = "erster", StartTime = new DateTime(2018, 7, 13), EndTime = new DateTime(2018, 8, 12), HasEnded = true },
                    new ETWProcess() { ProcessID = 20, ProcessName = "FirstPerfProblem.exe", CmdLine = "zweiter", StartTime = new DateTime(2018, 10, 3, 6, 59, 38), EndTime = new DateTime(2018, 10, 3, 6, 59, 37), HasEnded = false },
                    new ETWProcess() { ProcessID = 300, ProcessName = "ETWController.exe", CmdLine = "dritter", StartTime = new DateTime(2010, 1, 30, 13, 20, 10, 993), EndTime = new DateTime(2010, 1, 30, 13, 20, 10, 995) }
                });
        }

        [Fact]
        public void Get_Process_Index_Of_Not_Existing_Process_Throws()
        {
            ETWExtract c1 = new ETWExtract();
            ExceptionAssert.Throws<KeyNotFoundException>(() => c1.GetProcessIndex("xxxx"), "xxxx");
        }

        [Fact]
        public void Can_Store_Exception_Stats()
        {
            ETWExtract c1 = new ETWExtract();
            var start = DateTime.Now;
            c1.SessionStart = start;
            AddProcesses(c1);
            Assert.Null(c1.Exceptions);
            c1.Exceptions = new ExceptionStats();
            var now = DateTime.Now;
            var rowData = new ExceptionRowData
            {
                ExceptionMessage = "ExMessage",
                ProcessNameAndPid = "Tester.exe (1)",
                Stack = "ntdll!RtlUserThreadStart",
                ExceptionType = "NotAGodExceptionType",
                ThreadId = 100,
                TimeInSec = new DateTimeOffset(now)
            };

            c1.Exceptions.Add(c1, rowData);
            rowData.ProcessNameAndPid = "FirstPerfProblem.exe (20)";
            c1.Exceptions.Add(c1, rowData);
            rowData.TimeInSec = now;
            c1.Exceptions.Add(c1, rowData);

            Assert.Equal(2, c1.Exceptions.Count);

            var exceptions = c1.Exceptions.Exceptions;

            Assert.Equal(new DateTimeOffset(now), exceptions[2].Time);
            Assert.Equal("Tester.exe", exceptions[0].Process.ProcessName);
            Assert.Equal("FirstPerfProblem.exe", exceptions[1].Process.ProcessName);

        }

    }
}
