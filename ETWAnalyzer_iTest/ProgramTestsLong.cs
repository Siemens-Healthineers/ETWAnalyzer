//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Exceptions;
using ETWAnalyzer.Extractors;
using ETWAnalyzer.Helper;
using ETWAnalyzer_iTest;
using ETWAnalyzer_uTest;
using ETWAnalyzer_uTest.TestInfrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static ETWAnalyzer.Extract.CPUPerProcessMethodList;

namespace ETWAnalyzer_iTest
{
    /// <summary>
    /// Here end up the long running tests which need some time to execute
    /// </summary>
    public class ProgramTestsLong
    {
        string GetExtractFile(ITempOutput outDir, string inputFile)
        {
            string outFile = Path.Combine(outDir.Name, Path.GetFileNameWithoutExtension(inputFile) + ".json");
            return outFile;
        }
        [Fact]
        public void Can_Extract_FullDetail_From_Zip()
        {
            Program.DebugOutput = false;
            using var tmp = TempDir.Create();
            string pathName = Path.Combine(tmp.Name, Path.GetFileName(TestData.ServerZipFile));
            File.Copy(TestData.ServerZipFile, pathName);

            Program.MainCore(new string[] { "-extract", "Disk", "CPU", "Memory","-filedir", pathName,  "-outdir", tmp.Name });

            string outFile = GetExtractFile(tmp, TestData.ServerZipFile);
            var fileInfo = new FileInfo(outFile);

            // Check Folder
            DirectoryInfo directoryInfo = new(tmp.Name);
            Assert.Equal(2, directoryInfo.GetFiles().Length);

            // Check JsonFiles
            string[] extractJsonFiles =  Directory.GetFiles(Path.GetDirectoryName(outFile), "*"+TestRun.ExtractExtension);
            Assert.Single(extractJsonFiles);
            Assert.Contains(outFile, extractJsonFiles);
        }
        [Fact]
        public void Can_Extract_FullDetail_From_Zip_With_Optional_Args()
        {
            
            Program.DebugOutput = false;
            using var tmp = TempDir.Create();
            string pathName = Path.Combine(tmp.Name, Path.GetFileName(TestData.ServerZipFile));
            File.Copy(TestData.ServerZipFile, pathName);

            Program.MainCore(new string[] { "-extract", "Disk", "CPU", "Memory", "-filedir", pathName, "-outdir", tmp.Name ,"-symServer", "syngo","-keepTemp", "-NoOverwrite","-pthreads","80", "-child","-recursive" });

            string outFile = GetExtractFile(tmp, TestData.ServerZipFile);
            var fileInfo = new FileInfo(outFile);

            // Check Folder
            DirectoryInfo directoryInfo = new(tmp.Name);
            Assert.Equal(3,directoryInfo.GetFiles().Length);
            Assert.Empty(directoryInfo.GetDirectories());

            // Check JsonFiles
            string[] extractJsonFiles = Directory.GetFiles(Path.GetDirectoryName(outFile), "*" + TestRun.ExtractExtension);
            Assert.Single(extractJsonFiles);
            Assert.Contains(outFile, extractJsonFiles);
        }

    

        [Fact]
        public void Can_Extract_FullDetail_From_ETL()
        {
            Program.DebugOutput = false;
            using var tmp = TempDir.Create();
            string pathName = Path.Combine(tmp.Name, Path.GetFileName(TestData.ServerEtlFile));
            Program.DebugOutput = false;
            Program.MainCore(new string[] { "-extract", "Disk", "CPU", "Memory", "Exception", "Stacktag","-filedir", TestData.ServerEtlFile,   "-outdir", tmp.Name });

            string outFile = GetExtractFile(tmp, TestData.ServerEtlFile);
            var fileInfo = new FileInfo(outFile);

            // Check Folder
            DirectoryInfo directoryInfo = new(tmp.Name);
            Assert.Single(directoryInfo.GetFiles());

            // Check JsonFiles
            directoryInfo = new DirectoryInfo(Path.GetDirectoryName(outFile));
            string[] files = directoryInfo.GetFiles("*.json", SearchOption.AllDirectories).Select(x=>x.FullName).ToArray();

            Assert.Single(files);
            Assert.Contains(Path.Combine(tmp.Name, Path.GetFileNameWithoutExtension(TestData.ServerEtlFile)) + TestRun.ExtractExtension, files);

            Assert.True(fileInfo.Exists, $"Output file {outFile} was not created");
            Assert.True(fileInfo.Length > 0, $"File {outFile} has no content");
        }

        [Fact]
        public void Can_Extract_FullDetail_From_ETLFolder()
        {
            Program.DebugOutput = false;
            using var tmp = TempDir.Create();
            string pathName = Path.Combine(tmp.Name, Path.GetFileName(TestData.ServerEtlFile));
            File.Copy(TestData.ServerEtlFile, pathName);
            pathName = Path.Combine(tmp.Name, Path.GetFileName(TestData.ClientEtlFile));
            File.Copy(TestData.ClientEtlFile, pathName);

            Program.MainCore(new string[] { "-extract", "Exception","-filedir", tmp.Name,  "-outdir", tmp.Name });

            string outFile1 = GetExtractFile(tmp, TestData.ServerEtlFile);
            var fileInfo1 = new FileInfo(outFile1);

            string outFile2 = GetExtractFile(tmp, TestData.ClientEtlFile);
            var fileInfo2 = new FileInfo(outFile2);

            // Check Folder
            DirectoryInfo directoryInfo = new(tmp.Name);
            Assert.Equal(4, directoryInfo.GetFiles().Length);

            // Check JsonFiles
            directoryInfo = new DirectoryInfo(Path.GetDirectoryName(outFile1));
            Assert.Equal(2, directoryInfo.GetFiles("*.json").Length);

            Assert.True(fileInfo1.Exists, $"Output file {outFile1} was not created");
            Assert.True(fileInfo1.Length > 0, $"File {outFile1} has no content");

            Assert.True(fileInfo2.Exists, $"Output file {outFile2} was not created");
            Assert.True(fileInfo2.Length > 0, $"File {outFile2} has no content");
        }

        [Fact]
        public void Can_Extract_From_ETLFolder_Check_Extract()
        {
            Program.DebugOutput = false;
            using var tmp = TempDir.Create();
            string pathName = Path.Combine(tmp.Name, Path.GetFileName(TestData.ServerEtlFile));
            File.Copy(TestData.ServerEtlFile, pathName);

            Program.MainCore(new string[] { "-extract", "Disk", "CPU", "Memory", "Exception","-filedir", tmp.Name,  "-outdir", tmp.Name });

            string etractServerJson = GetExtractFile(tmp, TestData.ServerEtlFile);
            var fileInfo1 = new FileInfo(etractServerJson);

            string extract1 = Path.Combine(tmp.Name, TestData.ServerEtlFileNameNoPath);

            Assert.True(File.Exists(extract1), $"Extracted ETL file should have been kept at {extract1}");

            Assert.True(fileInfo1.Exists, $"Output file {etractServerJson} was not created");
            Assert.True(fileInfo1.Length > 0, $"File {etractServerJson} has no content");

            using var extractStream = new FileStream(etractServerJson, FileMode.Open);
            var extractedServer = ExtractSerializer.Deserialize<ETWExtract>(extractStream);

            CheckServerExtract(extractedServer, etractServerJson);
        }

        DateTimeOffset GetTimeUpToSeconds(DateTimeOffset fullprecisionTime)
        {
            return new DateTimeOffset(fullprecisionTime.Year, fullprecisionTime.Month, fullprecisionTime.Day, fullprecisionTime.Hour, fullprecisionTime.Minute, fullprecisionTime.Second, fullprecisionTime.Offset);
        }

        private void CheckServerExtract(ETWExtract extractedServer, string extractJsonFileName)
        {
            string fileName = Path.GetFileName(extractedServer.SourceETLFileName);
            Assert.Equal(TestData.ServerEtlFileNameNoPath, fileName);
            Assert.Equal("10.0.19043", extractedServer.OSVersion.ToString());

            Assert.Equal(new DateTimeOffset(637736071555000000L, TimeSpan.FromMinutes(60.0d)), extractedServer.BootTime);

            // Check Marks count, string and time
            Assert.Empty(extractedServer.ETWMarks);

            // Check total disk IO data
            Assert.Equal(5, extractedServer.Disk.DriveToPath.Count);
            Assert.Equal(103425uL, extractedServer.Disk.TotalDiskFlushTimeInus);
            Assert.Equal(643237uL, extractedServer.Disk.TotalDiskReadTimeInus);
            Assert.Equal(1133445uL, extractedServer.Disk.TotalDiskServiceTimeInus);
            Assert.Equal(386783uL, extractedServer.Disk.TotalDiskWriteTimeTimeInus);

            // Check CPU summary data
            Assert.Equal(121, extractedServer.CPU.PerProcessCPUConsumptionInMs.Count);
            Assert.Equal(3430u, extractedServer.CPU.PerProcessCPUConsumptionInMs.Where( x=> x.Key.Name == "SerializerTests.exe" && x.Key.Pid == 22416).FirstOrDefault().Value );

            // Check CPU Method Level Data
            //
            CPUPerProcessMethodList list = extractedServer.CPU.PerProcessMethodCostsInclusive;

            if (!TestContext.IsInGithubPipeline())
            {
                Assert.True(list.MethodNames.Count > 1000); // depending on local symbol cache state method count might fluctuate quite a lot!
            }
            MethodsByProcess methods = list.MethodStatsPerProcess.Where(x => x.Process.Pid == 22416).First();

            // Checked with WPA from CPU Sampling and Context Switch Data
            MethodCost cost = methods.Costs.Where(x => x.Method == "SerializerTests.dll!SerializerTests.Program.Combined").First();
            Assert.Equal(2899u, cost.CPUMs);
            Assert.Equal(5u, cost.ReadyMs);
            Assert.Equal(1227u, cost.WaitMs);
            Assert.Equal(1, cost.Threads);
            Assert.Equal(3.7385f, cost.FirstOccurenceInSecond);
            Assert.Equal(7.9859f, cost.LastOccurenceInSecond);

            if (!TestContext.IsInGithubPipeline())
            {
                var firstMethod = methods.Costs.Where(x => x.Method == "kernel32.dll!BaseThreadInitThunk").First();
                Assert.Equal("kernel32.dll!BaseThreadInitThunk", firstMethod.Method);
                Assert.Equal(42, firstMethod.DepthFromBottom);
                Assert.Equal(3285u, firstMethod.CPUMs);
                Assert.Equal(4514u, firstMethod.WaitMs);
                Assert.Equal(8u, firstMethod.ReadyMs);
                Assert.Equal(3.4746f, firstMethod.FirstOccurenceInSecond);
                Assert.Equal(8.0306f, firstMethod.LastOccurenceInSecond);
                Assert.Equal(11, firstMethod.Threads);
            }

            // Check Exception Data
            Assert.Equal(1, extractedServer.Exceptions.Count);

            // Check # of exception with no Stacktrace
            Assert.True(extractedServer.Exceptions.Stacks.Stack2Messages.ContainsKey(ExceptionExtractor.NoStackString));
            HashSet<ExceptionMessageAndType> msgAndType = extractedServer.Exceptions.Stacks.Stack2Messages[ExceptionExtractor.NoStackString];

            // Get most often occuring stack less exception
            ExceptionMessageAndType singleException = msgAndType.OrderByDescending(x=>x.Times.Count).First();

            // check type, message and count
            Assert.Equal("Could not load file or assembly 'notExistingToTriggerGACPrefetch, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'. The system cannot find the file specified.", singleException.Message);
            Assert.Equal("System.IO.FileNotFoundException", singleException.Type);
            Assert.Single(singleException.Times);
            Assert.Single(singleException.Processes);

            // Check Memory Data
            Assert.Equal(17120, extractedServer.MemorySizeMB);
            Assert.Equal(14019uL, extractedServer.MemoryUsage.MachineCommitStartMiB);
            Assert.Equal(14001uL, extractedServer.MemoryUsage.MachineCommitEndMiB);
           
            Assert.Equal(-18, extractedServer.MemoryUsage.MachineCommitDiffMiB);

            Assert.Equal(9661uL, extractedServer.MemoryUsage.MachineActiveStartMiB);
            Assert.Equal(9531uL, extractedServer.MemoryUsage.MachineActiveEndMiB);
            
            Assert.Equal(257, extractedServer.MemoryUsage.WorkingSetsAtStart.Count);

            // processes are sorted by Commit descending
            ProcessWorkingSet highestStart = extractedServer.MemoryUsage.WorkingSetsAtStart[0];
            Assert.Equal(401uL, highestStart.WorkingSetInMiB);
            Assert.Equal(529uL, highestStart.CommitInMiB);
            Assert.Equal(327uL, highestStart.WorkingsetPrivateInMiB);
            Assert.Equal(2uL, highestStart.SharedCommitSizeInMiB);

            Assert.Equal(259, extractedServer.MemoryUsage.WorkingSetsAtEnd.Count);
            // processes are sorted by Commit descending
            ProcessWorkingSet highestEnd = extractedServer.MemoryUsage.WorkingSetsAtEnd[0];
            Assert.Equal(407uL, highestEnd.WorkingSetInMiB);
            Assert.Equal(529uL, highestEnd.CommitInMiB);
            Assert.Equal(332uL, highestEnd.WorkingsetPrivateInMiB);
            Assert.Equal(2uL, highestEnd.SharedCommitSizeInMiB);
        }

        [Fact]
        public void Can_Extract_FullDetail_From_SevenZipFolder()
        {
            Program.DebugOutput = false;
            using var tmp = TempDir.Create();
            string pathName = Path.Combine(tmp.Name, Path.GetFileName(TestData.ServerZipFile));
            File.Copy(TestData.ServerZipFile, pathName);
            pathName = Path.Combine(tmp.Name, Path.GetFileName(TestData.ClientZipFile));
            File.Copy(TestData.ClientZipFile, pathName);

            Program.MainCore(new string[] { "-extract", "Disk", "CPU", "Memory","-filedir", tmp.Name,  "-outdir", tmp.Name });

            string outFile1 = GetExtractFile(tmp, TestData.ServerZipFile);
            var fileInfo1 = new FileInfo(outFile1);

            string outFile2 = GetExtractFile(tmp, TestData.ClientZipFile);
            var fileInfo2 = new FileInfo(outFile2);

            // Check Folder
            DirectoryInfo directoryInfo = new(tmp.Name);
            Assert.Equal(4, directoryInfo.GetFiles().Length);

            // Check JsonFiles
            string[] jsonExtracts = Directory.GetFiles(Path.GetDirectoryName(outFile1), "*"+TestRun.ExtractExtension);
            Assert.Equal(2, jsonExtracts.Length);

            Assert.Contains(outFile1, jsonExtracts);
            Assert.Contains(outFile2, jsonExtracts);
        }

        [Fact]
        public void Can_Extract_FullDetail_From_SevenZipFolder_KeepingTemp()
        {
            Program.DebugOutput = false;
            using var tmp = TempDir.Create();
            string pathName = Path.Combine(tmp.Name, Path.GetFileName(TestData.ServerZipFile));
            File.Copy(TestData.ServerZipFile, pathName);
            pathName = Path.Combine(tmp.Name, Path.GetFileName(TestData.ClientZipFile));
            File.Copy(TestData.ClientZipFile, pathName);

            Program.MainCore(new string[] { "-extract", "Disk", "CPU", "Memory","-filedir", tmp.Name,  "-outdir", tmp.Name, "-keepTemp" });

            string outFile1 = GetExtractFile(tmp, TestData.ServerZipFile);
            var fileInfo1 = new FileInfo(outFile1);

            string outFile2 = GetExtractFile(tmp, TestData.ClientZipFile);
            var fileInfo2 = new FileInfo(outFile2);

            Assert.True(fileInfo1.Exists, $"Output file {outFile1} was not created");
            Assert.True(fileInfo1.Length > 0, $"File {outFile1} has no content");

            Assert.True(fileInfo2.Exists, $"Output file {outFile2} was not created");
            Assert.True(fileInfo2.Length > 0, $"File {outFile2} has no content");
        }
    }
}
