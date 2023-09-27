using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETWAnalyzer.Extract;
using Xunit;

namespace ETWAnalyzer_uTest.Extract
{
    public class MemoryStatsTests
    {
        [Fact]
        public void MemStats_Is_Not_Null()
        {

            ETWExtract m1 = new ETWExtract();
            Assert.Null(m1.MemoryUsage);

        }

        const string File1 = "File1.json";

        const string FileName1 = "FileName1";

        static readonly ETWProcess myCmdProcess = new ETWProcess
        {
            ProcessID = 1234,
            ProcessName = "cmd.exe",
            CmdLine = "cmdHi",
        };

        static readonly ProcessKey myCmdProcessKey = myCmdProcess.ToProcessKey();

        static readonly ETWProcess myCmdProcess2 = new ETWProcess
        {
            ProcessID = 1111,
            ProcessName = "hello.exe",
            CmdLine = "helloHi",
        };

        static readonly ProcessKey myCmdProcessKey2 = myCmdProcess2.ToProcessKey();


        IETWExtract CreateTestData()
        {
            ETWExtract data = new ETWExtract();

            data.MemoryUsage = new MemoryStats(10000, 12000, 2000, 5000);

            data.MemoryUsage.WorkingSetsAtStart = new List<ProcessWorkingSet>
            {
                new ProcessWorkingSet() {WorkingSetInMiB = 1000, CommitInMiB = 900, WorkingsetPrivateInMiB = 600, SharedCommitSizeInMiB = 100, Process = myCmdProcess},
                new ProcessWorkingSet() {WorkingSetInMiB = 2000, CommitInMiB = 1800, WorkingsetPrivateInMiB = 400, SharedCommitSizeInMiB = 300, Process = myCmdProcess2},

            };

            data.MemoryUsage.WorkingSetsAtEnd = new List<ProcessWorkingSet>
            {
                new ProcessWorkingSet() {WorkingSetInMiB = 100, CommitInMiB = 90, WorkingsetPrivateInMiB = 60, SharedCommitSizeInMiB = 10, Process = myCmdProcess2},
                new ProcessWorkingSet() {WorkingSetInMiB = 200, CommitInMiB = 180, WorkingsetPrivateInMiB = 40, SharedCommitSizeInMiB = 30, Process = myCmdProcess},

            };

            return data;
        }

        [Fact]
        public void Can_Store_MemoryStats()
        {
            IETWExtract extract = CreateTestData();
            Assert.NotNull(extract.MemoryUsage);

            Assert.Equal(10000ul, extract.MemoryUsage.MachineCommitStartMiB);
            Assert.Equal(12000ul, extract.MemoryUsage.MachineCommitEndMiB);
            Assert.Equal(2000ul, extract.MemoryUsage.MachineActiveStartMiB);
            Assert.Equal(5000ul, extract.MemoryUsage.MachineActiveEndMiB);
            Assert.Equal(2000, extract.MemoryUsage.MachineCommitDiffMiB);
            Assert.Equal(3000, extract.MemoryUsage.MachineActiveDiffMiB);


            Assert.Equal(100ul, extract.MemoryUsage.WorkingSetsAtStart[0].SharedCommitSizeInMiB); 
            Assert.Equal(1000ul, extract.MemoryUsage.WorkingSetsAtStart[0].WorkingSetInMiB); 
            Assert.Equal(900ul, extract.MemoryUsage.WorkingSetsAtStart[0].CommitInMiB); 
            Assert.Equal(600ul, extract.MemoryUsage.WorkingSetsAtStart[0].WorkingsetPrivateInMiB);

            Assert.Equal(300ul, extract.MemoryUsage.WorkingSetsAtStart[1].SharedCommitSizeInMiB);  
            Assert.Equal(2000ul, extract.MemoryUsage.WorkingSetsAtStart[1].WorkingSetInMiB);
            Assert.Equal(1800ul, extract.MemoryUsage.WorkingSetsAtStart[1].CommitInMiB);
            Assert.Equal(400ul, extract.MemoryUsage.WorkingSetsAtStart[1].WorkingsetPrivateInMiB);


            Assert.Equal(10ul, extract.MemoryUsage.WorkingSetsAtEnd[0].SharedCommitSizeInMiB);  
            Assert.Equal(100ul, extract.MemoryUsage.WorkingSetsAtEnd[0].WorkingSetInMiB);
            Assert.Equal(90ul, extract.MemoryUsage.WorkingSetsAtEnd[0].CommitInMiB);
            Assert.Equal(60ul, extract.MemoryUsage.WorkingSetsAtEnd[0].WorkingsetPrivateInMiB);

            Assert.Equal(30ul, extract.MemoryUsage.WorkingSetsAtEnd[1].SharedCommitSizeInMiB);
            Assert.Equal(200ul, extract.MemoryUsage.WorkingSetsAtEnd[1].WorkingSetInMiB);
            Assert.Equal(180ul, extract.MemoryUsage.WorkingSetsAtEnd[1].CommitInMiB);
            Assert.Equal(40ul, extract.MemoryUsage.WorkingSetsAtEnd[1].WorkingsetPrivateInMiB);
        }


    }
}
