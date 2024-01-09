//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Exceptions;
using ETWAnalyzer.Extract.Modules;
using ETWAnalyzer.Extractors;
using ETWAnalyzer.Helper;
using ETWAnalyzer_iTest;
using ETWAnalyzer_uTest;
using ETWAnalyzer_uTest.TestInfrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static ETWAnalyzer.Extract.CPUPerProcessMethodList;

namespace ETWAnalyzer_iTest
{
    /// <summary>
    /// Here end up the long running tests which need some time to execute
    /// </summary>
    public class ProgramTestsLong
    {

        private ITestOutputHelper myWriter;

        public ProgramTestsLong(ITestOutputHelper myWriter)
        {
            this.myWriter = myWriter;
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
            Assert.Equal(4, directoryInfo.GetFiles().Length);

            // Check JsonFiles
            string[] extractJsonFiles =  Directory.GetFiles(Path.GetDirectoryName(outFile), "*"+TestRun.ExtractExtension);
            Assert.Equal(3, extractJsonFiles.Length);
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
            Assert.Equal(5,directoryInfo.GetFiles().Length);
            Assert.Empty(directoryInfo.GetDirectories());

            // Check JsonFiles
            string[] extractJsonFiles = Directory.GetFiles(Path.GetDirectoryName(outFile), "*" + TestRun.ExtractExtension);
            Assert.Equal(3, extractJsonFiles.Length);
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
            Assert.Equal(3, directoryInfo.GetFiles().Length);

            // Check JsonFiles
            directoryInfo = new DirectoryInfo(Path.GetDirectoryName(outFile));
            string[] files = directoryInfo.GetFiles("*.json", SearchOption.AllDirectories).Select(x=>x.FullName).ToArray();

            Assert.Equal(3, files.Length);
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
            Assert.Equal(6, directoryInfo.GetFiles().Length);

            // Check JsonFiles
            directoryInfo = new DirectoryInfo(Path.GetDirectoryName(outFile1));
            Assert.Equal(4, directoryInfo.GetFiles("*.json").Length);

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


            string[] extractArgs = new string[] { "-extract", "Disk", "CPU", "Memory", "Exception", "-filedir", tmp.Name, "-outdir", tmp.Name };
                        
            if (!TestContext.IsInGithubPipeline()) // when we are executed with symbol server access then use it to resolve method names
            {
                extractArgs = extractArgs.Concat(new string[] { "-symserver", "MS" }).ToArray();
            }

            Program.MainCore(extractArgs);

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


        bool IsSymbolServerReachable()
        {
            string msSymbolServer = "https://msdl.microsoft.com/download/symbols";

            try
            {
                var client = new HttpClient();
                var response = client.GetAsync(msSymbolServer).Result;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    //  it's at least in some way responsive
                    //  but may be internally broken
                    //  as you could find out if you called one of the methods for real
                    Debug.Write(string.Format("{0} Available", msSymbolServer));
                }
                else
                {
                    //  well, at least it returned...
                    Debug.Write(string.Format("{0} Returned, but with status: {1}",
                        msSymbolServer, response.StatusCode));
                }

                myWriter.WriteLine("Symbol server is reachable");
                return true;
            }
            catch (Exception ex)
            {
                //  not available at all, for some reason
                Debug.Write(string.Format("{0} unavailable: {1}", msSymbolServer, ex.Message));
                myWriter.WriteLine("Symbol server is not reachable. Test runs only partially.");
                return false;
            }

        }

        /// <summary>
        /// To speed up symbol loading from symbol server we resolve only symbols for dwmredir
        /// </summary>
        /// <param name="extract"></param>
        ETWExtract RemoveUnresolvedSymbolsExceptForDwmRedir(IETWExtract extract)
        {
            var mod = extract.Modules; // touch it to force reading file
            ETWExtract ex = (ETWExtract)extract;
            ex.Modules = (ModuleContainer) mod; // set to actual property

            var dwmredir = new PdbIdentifier("dwmredir.pdb", new Guid("f5d79b57-f07c-dd7c-cda3-f706954dd179"), 1);

            int pdbIdx = ex.Modules.UnresolvedPdbs.IndexOf(dwmredir);
            ex.Modules.UnresolvedPdbs = new List<PdbIdentifier> {  dwmredir };

            foreach(var module in ex.Modules.Modules)
            {
                module.PdbIdx = module.PdbIdx == (PdbIndex) pdbIdx ? 0 : null;
            }

            return ex;
        }

        [Fact]
        public void Extract_Without_Symbols_And_Then_Resolve_Symbols()
        {
            Program.DebugOutput = false;
            using var tmp = TempDir.Create();
            string pathName = Path.Combine(tmp.Name, Path.GetFileName(TestData.ServerEtlFile));
            File.Copy(TestData.ServerEtlFile, pathName);

            Program.MainCore(new string[] { "-extract", "CPU", "-filedir", tmp.Name, "-outdir", tmp.Name, 
                "-NoOverwrite",
                "-symFolder", Path.Combine(tmp.Name, "symfolder"),
                "-symcache",  Path.Combine(tmp.Name, "symcache") });

            string unresolvedJson = GetExtractFile(tmp, TestData.ServerEtlFile);

            // resolve with cached symbols from empty cache. Should change nothing
            string outDir = Path.Combine(tmp.Name, "Extract1");
            Directory.CreateDirectory(outDir);
            Program.MainCore(new string[] { "-LoadSymbol", "-fd", unresolvedJson,
                                            "-outdir", outDir,
                                            "-NoOverwrite",
                                            "-symFolder", Path.Combine(tmp.Name, "symfolder"),
                                            "-symcache",  Path.Combine(tmp.Name, "symcache") });

            string resolvedJson = GetExtractFile(outDir, unresolvedJson);

            TestDataFile extract1 = new TestDataFile(unresolvedJson);
            TestDataFile extract2 = new TestDataFile(resolvedJson);

            // When no symbol server was present an no cached symbols we get so many 
            // unresolved pdbs
            var unresolvedPdbs = extract1.Extract.Modules.UnresolvedPdbs.Count;
            Assert.Equal(1856, unresolvedPdbs);
            Assert.Equal(3691, extract1.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames.Count);

            // We have 12 unresolved symbols
            Assert.Contains("dwmredir.dll!dwmredir.dll+0x20BB", extract1.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
            Assert.Contains("dwmredir.dll!dwmredir.dll+0x2702", extract1.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
            Assert.Contains("dwmredir.dll!dwmredir.dll+0x2DC1", extract1.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
            Assert.Contains("dwmredir.dll!dwmredir.dll+0x2E70", extract1.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
            Assert.Contains("dwmredir.dll!dwmredir.dll+0x3FF2", extract1.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
            Assert.Contains("dwmredir.dll!dwmredir.dll+0x586F", extract1.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
            Assert.Contains("dwmredir.dll!dwmredir.dll+0x5975", extract1.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
            Assert.Contains("dwmredir.dll!dwmredir.dll+0x5A12", extract1.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
            Assert.Contains("dwmredir.dll!dwmredir.dll+0x92F9", extract1.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
            Assert.Contains("dwmredir.dll!dwmredir.dll+0xAC77", extract1.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
            Assert.Contains("dwmredir.dll!dwmredir.dll+0xADB3", extract1.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
            Assert.Contains("dwmredir.dll!dwmredir.dll+0xBB69", extract1.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);

            uint totalddwmRedirCPUMs = 0;
            foreach (var perProcess in extract1.Extract.CPU.PerProcessMethodCostsInclusive.MethodStatsPerProcess)
            {
                foreach (var cost in perProcess.Costs.Where(x=>x.Module== "dwmredir.dll"))
                {
                    totalddwmRedirCPUMs += cost.CPUMs;
                }

            }
            Assert.Equal(284, (int) totalddwmRedirCPUMs);


            if (IsSymbolServerReachable())
            {
                // Create a changed file to 
                ExtractSerializer ser = new ExtractSerializer();
                ser.Serialize(resolvedJson, RemoveUnresolvedSymbolsExceptForDwmRedir(extract2.Extract));


                // resolve a second time with more symbols which should lead some more resolved files
                // this also checks if our pdb index rewrite was correct or if we did make some errors
                string outDir2 = Path.Combine(tmp.Name, "Extract2");
                Directory.CreateDirectory(outDir2);
                Program.MainCore(new string[] { "-LoadSymbol", "-fd", resolvedJson,
                                            "-outdir", outDir2,
                                            "-NoOverwrite",
                                            "-symserver", $"SRV*{Path.Combine(tmp.Name, "MSSymbols")}*https://msdl.microsoft.com/download/symbols"
                                          });

                string resolvedJson2 = GetExtractFile(outDir2, unresolvedJson);


                TestDataFile extract3 = new TestDataFile(resolvedJson2);

                var pdb2 = extract2.Extract.Modules.UnresolvedPdbs.Count;
                var unresolvedAfterMSSymbols = extract3.Extract.Modules.UnresolvedPdbs.Count;

                Assert.Equal(0, unresolvedAfterMSSymbols);
                foreach (var module in extract3.Extract.Modules.Modules)
                {
                    // when everything was resolved we must not have any indicies left
                    Assert.True(module.PdbIdx == null);
                }

                // we must have 4 methods less because we have resolved the, but all other methods must remain
                Assert.Equal(3687, extract3.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames.Count);

                // which boil down to 8 resolved methods 
                Assert.Contains("dwmredir.dll!CPortBase::PortThread", extract3.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
                Assert.Contains("dwmredir.dll!CPortBase::PortThreadInternal", extract3.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
                Assert.Contains("dwmredir.dll!CSessionPort::ProcessCommand", extract3.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
                Assert.Contains("dwmredir.dll!CSessionPort::WaitForMultipleObjects", extract3.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
                Assert.Contains("dwmredir.dll!CWindowManager::AsyncFlush", extract3.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
                Assert.Contains("dwmredir.dll!CWindowManager::CaptureSurfaceBits", extract3.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
                Assert.Contains("dwmredir.dll!CWindowManager::ProcessKernelOnlySyncLpc", extract3.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);
                Assert.Contains("dwmredir.dll!CWindowManager::ProcessSyncLpc", extract3.Extract.CPU.PerProcessMethodCostsInclusive.MethodNames);

                foreach (var perprocess in extract3.Extract.CPU.PerProcessMethodCostsInclusive.MethodStatsPerProcess)
                {
                    foreach (var cost in perprocess.Costs)
                    {
                        Assert.NotNull(cost.Method);
                    }
                }

                uint resolvedtotalddwmRedirCPUMs = 0;
                foreach (var perProcess in extract3.Extract.CPU.PerProcessMethodCostsInclusive.MethodStatsPerProcess)
                {
                    foreach (var cost in perProcess.Costs.Where(x => x.Module == "dwmredir.dll"))
                    {
                        resolvedtotalddwmRedirCPUMs += cost.CPUMs;
                    }

                }
                Assert.Equal(284, (int)resolvedtotalddwmRedirCPUMs);
            }
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
            Assert.Equal(7, directoryInfo.GetFiles().Length);

            // Check JsonFiles
            string[] jsonExtracts = Directory.GetFiles(Path.GetDirectoryName(outFile1), "*"+TestRun.ExtractExtension);
            Assert.Equal(5, jsonExtracts.Length);

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

        string GetExtractFile(ITempOutput outDir, string inputFile)
        {
            return GetExtractFile(outDir.Name, inputFile);
        }

        string GetExtractFile(string outDir, string inputFile)
        {
            string outFile = Path.Combine(outDir, Path.GetFileNameWithoutExtension(inputFile) + ".json");
            return outFile;
        }

    }
}
