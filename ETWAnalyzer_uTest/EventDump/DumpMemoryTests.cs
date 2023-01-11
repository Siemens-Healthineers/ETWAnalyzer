using ETWAnalyzer.EventDump;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Modules;
using ETWAnalyzer.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ETWAnalyzer_uTest.EventDump
{
    public class DumpMemoryTests
    {
        [Fact]
        public void Can_Dump_To_CSV()
        {
            using var tmpDir = TempDir.Create();
            string csvFile = Path.Combine(tmpDir.Name, "test.csv");
            {
                using DumpMemory dump = new DumpMemory();
                dump.CSVFile = csvFile;
                ETWExtract extract = new();
                extract.Processes = new List<ETWProcess>
                {
                   new ETWProcess { ProcessName = "Test.exe", ProcessID = 100 },
                };
                ModuleContainer container = new();
                container.Add(extract, (ETWProcessIndex)0, "C:\\Windows\\1.dll", "File Version of 1.dll", "Product Version of 1.dll", "Product Name of 1.dll", new Version(1, 0, 100, 1), "Description of 1.dll");
                extract.Modules = container;
                dump.WriteToCSV(new List<DumpMemory.Match>
                {
                    new DumpMemory.Match
                    {
                        CommitedMiB = 100,
                        PerformedAt = new DateTime(2000,1,1),
                        Process = "test.exe(1)",
                        ProcessName = "test.exe",
                        TestCase = "MemoryLeakTest",
                        WorkingSetMiB = 500,
                        Module = extract.Modules.Modules[0],
                    }
                }) ;
            }

            string[] lines = File.ReadAllLines(csvFile);
            Assert.Equal(3, lines.Length);
            Assert.Contains("0001-01-01 00:00:00.000;test.exe(1);test.exe;100;0;500;;;MemoryLeakTest;0;;;1.0.100.1;File Version of 1.dll;Product Version of 1.dll;Product Name of 1.dll;Description of 1.dll;C:\\Windows", lines[2]);
        }
    }
}
