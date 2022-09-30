//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Analyzers.Infrastructure;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.FileIO;
using ETWAnalyzer.Extractors;
using ETWAnalyzer.Extractors.FileIO;
using ETWAnalyzer_uTest.TestInfrastructure;
using Microsoft.Windows.EventTracing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest
{
    /// <summary>
    /// Ensure in xUnit Tests that we have a valid ETWExtract ready before any test runs this is done via IClassFixture
    /// https://stackoverflow.com/questions/46926852/xunit-constructor-runs-before-each-test
    /// </summary>
    public sealed class FileExtractorTestsFixture : IDisposable
    {
        public ETWExtract Extract
        {
            get => myInteralExtract;
        }

        static ETWExtract myInteralExtract = null;

        internal static object _Lock = new();

        public FileExtractorTestsFixture()
        {
            // since xUnit calls this in parallel use locks to guard against parallel execution which ruins 
            // the tests randomly where e.g. TraceEvent is just installing its dlls once which is then also running concurrently ... 
            lock (_Lock)
            {
                if (myInteralExtract == null)
                {
                    var tmp = new ETWExtract();
                    using ITraceProcessor processor = TraceProcessor.Create(TestData.ServerEtlFile, new TraceProcessorSettings
                    {
                        AllowLostEvents = true,
                    });

                    MachineDetailsExtractor extractor = new();
                    FileExtractor fileExtractor = new();
                    extractor.RegisterParsers(processor);
                    fileExtractor.RegisterParsers(processor);
                    processor.Process();
                    extractor.Extract(processor, tmp);
                    fileExtractor.Extract(processor, tmp);
                    myInteralExtract = tmp; // publish in a atomic way to prevent seeing Null Objects.
                }
            }
        }

        public void Dispose()
        {
        }
    }

    public class FileExtractorTests : IClassFixture<FileExtractorTestsFixture>
    {
        /// <summary>
        /// Generated once by DiskExtractorTestsFixture which is injected via ctor
        /// </summary>
        readonly ETWExtract myExtract;

        public FileExtractorTests(FileExtractorTestsFixture fixture)
        {
            myExtract = fixture.Extract;
        }

        [Fact]
        public void Can_Merge_File_SetSecurity_Calls()
        {
            const string File1 = @"C:\temp\SomeDir\File1.txt";
            FileIOData data = new();
            ETWExtract extract = new();
            extract.Processes.Add(new ETWProcess { ProcessID = 1, ProcessName = "Dummy.exe" });
            DateTimeOffset year2k = new(2000, 1, 1, 1, 1, 1, TimeSpan.Zero);

            FileIOStatistics stat = new()
            {
                SetSecurity = new FileSetSecurityOperation()
            };
            stat.SetSecurity.AddSecurityEvent(year2k, 1);

            FileIOStatistics stat2 = new()
            {
                SetSecurity = new FileSetSecurityOperation()
            };
            stat2.SetSecurity.AddSecurityEvent(year2k, 2);

            data.Add(extract, 1, DateTimeOffset.MinValue, File1, stat);
            data.Add(extract, 1, DateTimeOffset.MinValue, File1, stat2);

            IReadOnlyList<FileIOContainer> list = data.GetFileNameProcessStats(extract);
            Assert.Single(list);
            FileIOContainer first = list[0];

            Assert.Equal(File1, first.FileName);
            Assert.Equal("Dummy.exe", first.Process.ProcessName);

            FileIOStatistics stats = first.Stats;

            Assert.Equal(2, stats.SetSecurity.NtStatus.Count);
            Assert.Equal(1, stats.SetSecurity.NtStatus[0]);
            Assert.Equal(2, stats.SetSecurity.NtStatus[1]);
            Assert.Equal(2, stats.SetSecurity.Times.Count);
            Assert.Equal(year2k, stats.SetSecurity.Times[0]);
            Assert.Equal(year2k, stats.SetSecurity.Times[1]);
        }

        [Fact]
        public void Can_Merge_File_Open_Calls()
        {
            const string File1 = @"C:\temp\SomeDir\File1.txt";
            FileIOData data = new();
            ETWExtract extract = new();
            extract.Processes.Add(new ETWProcess { ProcessID = 1, ProcessName = "Dummy.exe" });

            FileIOStatistics stat = new()
            {
                Open = new FileOpenOperation
                {
                    Count = 1,
                    Durationus = 100,
                }
            };

            stat.Open.AddUniqueNotSucceededNtStatus(5);

            FileIOStatistics stat2 = new()
            {
                Open = new FileOpenOperation
                {
                    Count = 1,
                    Durationus = 100,
                }
            };
            stat2.Open.AddUniqueNotSucceededNtStatus(5);

            data.Add(extract, 1, DateTimeOffset.MinValue, File1, stat);
            data.Add(extract, 1, DateTimeOffset.MinValue, File1, stat2);

            IReadOnlyList<FileIOContainer> list = data.GetFileNameProcessStats(extract);
            Assert.Single(list);
            FileIOContainer first = list[0];

            // Not set properties must be null to ensure that not set values are not part of the serialized json, or the file
            // size will explode!
            Assert.Null(first.Stats.Read);
            Assert.Null(first.Stats.Write);
            Assert.Null(first.Stats.SetSecurity);
            Assert.Null(first.Stats.Close);

            Assert.NotNull(first.Stats.Open);

            Assert.Equal(200, first.Stats.Open.Durationus);
            Assert.Equal(2, first.Stats.Open.Count);

            // only unique values are stored
            Assert.Single(first.Stats.Open.NtStatus);
            Assert.Equal(5, first.Stats.Open.NtStatus[0]);
        }

        [Fact]
        public void Can_Merge_File_Close_Calls()
        {
            const string File1 = @"C:\temp\SomeDir\File1.txt";
            FileIOData data = new();
            ETWExtract extract = new();
            extract.Processes.Add(new ETWProcess { ProcessID = 1, ProcessName = "Dummy.exe" });

            FileIOStatistics stat = new()
            {
                Close = new FileCloseOperation
                {
                    Count = 1,
                    Cleanups = 2,
                    Durationus = 100,                    
                }
            };
            FileIOStatistics stat2 = new()
            {
                Close = new FileCloseOperation
                {
                    Count = 1,
                    Cleanups = 2,
                    Durationus = 100,
                }
            };

            data.Add(extract, 1, DateTimeOffset.MinValue, File1, stat);
            data.Add(extract, 1, DateTimeOffset.MinValue, File1, stat2);

            IReadOnlyList<FileIOContainer> list = data.GetFileNameProcessStats(extract);
            Assert.Single(list);
            var stats = list[0].Stats;
            Assert.Equal(2, stats.Close.Count);
            Assert.Equal(4, stats.Close.Cleanups);
            Assert.Equal(200, stats.Close.Durationus);
        }

        [Fact]
        public void Can_Merge_File_Write_Calls()
        {
            const string File1 = @"C:\temp\SomeDir\File1.txt";
            FileIOData data = new();
            ETWExtract extract = new();
            extract.Processes.Add(new ETWProcess { ProcessID = 1, ProcessName = "Dummy.exe" });

            FileIOStatistics stat = new()
            {
                Write = new FileOffsetOperation
                {
                    Count = 1,
                    Durationus = 100,
                    AccessedBytes = 5000,
                    MaxFilePosition = 1000
                }
            };
            FileIOStatistics stat2 = new()
            {
                Write = new FileOffsetOperation
                {
                    Count = 1,
                    Durationus = 100,
                    AccessedBytes = 5000,
                    MaxFilePosition = 500
                }
            };

            data.Add(extract, 1, DateTimeOffset.MinValue, File1, stat);
            data.Add(extract, 1, DateTimeOffset.MinValue, File1, stat2);

            IReadOnlyList<FileIOContainer> list = data.GetFileNameProcessStats(extract);
            Assert.Single(list);
            var stats = list[0].Stats;

            Assert.Equal(2, stats.Write.Count);
            Assert.Equal(200, stats.Write.Durationus);
            Assert.Equal(10_000, stats.Write.AccessedBytes);
            Assert.Equal(1000, stats.Write.MaxFilePosition);
        }

        [Fact]
        public void Can_Merge_File_Read_Calls()
        {
            const string File1 = @"C:\temp\SomeDir\File1.txt";
            FileIOData data = new();
            ETWExtract extract = new();
            extract.Processes.Add(new ETWProcess { ProcessID = 1, ProcessName = "Dummy.exe" });

            FileIOStatistics stat = new()
            {
                Read = new FileOffsetOperation
                {
                    Count = 1,
                    Durationus = 100,
                    AccessedBytes = 5000,
                    MaxFilePosition = 1000
                }
            };
            FileIOStatistics stat2 = new()
            {
                Read = new FileOffsetOperation
                {
                    Count = 1,
                    Durationus = 100,
                    AccessedBytes = 5000,
                    MaxFilePosition = 500
                }
            };

            data.Add(extract, 1, DateTimeOffset.MinValue, File1, stat);
            data.Add(extract, 1, DateTimeOffset.MinValue, File1, stat2);

            IReadOnlyList<FileIOContainer> list = data.GetFileNameProcessStats(extract);
            Assert.Single(list);
            var stats = list[0].Stats;

            Assert.Equal(2, stats.Read.Count);
            Assert.Equal(200, stats.Read.Durationus);
            Assert.Equal(10_000, stats.Read.AccessedBytes);
            Assert.Equal(1000, stats.Read.MaxFilePosition);
        }

        [Fact]
        public void Can_Serialize_Deserialize_FileMetrics()
        {
            const string File1 = @"C:\temp\SomeDir\File1.txt";
            const string File2 = @"C:\Windows\system32\cmd.exe";

            FileIOStatistics stat1 = new();

            stat1.Add( new FileIOStatistics
            {
                Read = new FileOffsetOperation
                {
                    AccessedBytes = 1024 * 1024,
                    Durationus = 100,
                    MaxFilePosition = 500,
                    Count = 5
                },
            });

            stat1.Add(new FileIOStatistics
            {
                Read = new FileOffsetOperation
                {
                    AccessedBytes = 1024 * 1024,
                    Durationus = 100,
                    MaxFilePosition = 500,
                    Count = 5
                },
            });

            Assert.Null(stat1.Write);

            FileIOStatistics stat2 = new();
            stat2.Add(new FileIOStatistics
            {
                Write = new FileOffsetOperation
                {
                    AccessedBytes = 500000,
                    Durationus = 50,
                    MaxFilePosition = 1000,
                    Count = 1
                },
            });

            stat2.Add(new FileIOStatistics
            {
                Write = new FileOffsetOperation
                {
                    AccessedBytes = 500000,
                    Durationus = 50,
                    MaxFilePosition = 1000,
                    Count = 2
                },
            });

            Assert.Null(stat2.Read);
            Assert.Equal(3, stat2.Write.Count);
            Assert.Equal(1000, stat2.Write.MaxFilePosition);
            Assert.Equal(100, stat2.Write.Durationus);
            Assert.Equal(1_000_000, stat2.Write.AccessedBytes);

            ETWExtract extract = new();
            extract.Processes.Add(new ETWProcess
            {
                ProcessName = "Test1.exe",
                ProcessID = 100
            });
            extract.FileIO = new FileIOData();
            extract.FileIO.Add(extract, 100, DateTimeOffset.MinValue, File1,  stat1);
            extract.FileIO.Add(extract, 100, DateTimeOffset.MinValue, File2,  stat2);

            MemoryStream stream = new();
            ExtractSerializer.Serialize(stream, extract);
            stream.Position = 0;
            string serialized = Encoding.UTF8.GetString(stream.ToArray());

            using var expprinter = new ExceptionalPrinter();


            IETWExtract deserialized = ExtractSerializer.Deserialize<ETWExtract>(stream);
            expprinter.Messages.Add($"Serialized Data: {serialized}");

            IReadOnlyList<FileIOContainer> flatList = deserialized.FileIO.GetFileNameProcessStats(deserialized).OrderBy(x => x.FileName).ToList();

            Assert.Equal(2, flatList.Count);
            FileIOContainer first = flatList[0];
            FileIOContainer second = flatList[1];

            Assert.Equal(File1, first.FileName);
            Assert.Equal(File2, second.FileName);

            Assert.Equal("Test1.exe", first.Process.ProcessName);
            Assert.Equal("Test1.exe", second.Process.ProcessName);

            Assert.Equal(stat1.Read.Count, first.Stats.Read.Count);
            Assert.Equal(stat1.Read.Durationus, first.Stats.Read.Durationus);
            Assert.Equal(stat1.Read.AccessedBytes, first.Stats.Read.AccessedBytes);
            Assert.Equal(stat1.Read.MaxFilePosition, first.Stats.Read.MaxFilePosition);
            Assert.Null(first.Stats.Write);


            Assert.Null(second.Stats.Read);

            Assert.Equal(stat2.Write.Count, second.Stats.Write.Count);
            Assert.Equal(stat2.Write.AccessedBytes, second.Stats.Write.AccessedBytes);
            Assert.Equal(stat2.Write.Durationus, second.Stats.Write.Durationus);
            Assert.Equal(stat2.Write.MaxFilePosition, second.Stats.Write.MaxFilePosition);
            





        }

        [Fact]
        public void Metric_Numbers_Match_With_WPA()
        {
            IReadOnlyList<FileIOContainer> container = myExtract.FileIO.GetFileNameProcessStats(myExtract);

            Func<FileIOContainer, bool> CreateFilterFor(int pid) => (FileIOContainer io) => io.Process.ProcessID == pid;

            Func<FileIOContainer, bool> filterCmd = CreateFilterFor(22416);

            var filtered = container.Where(filterCmd).ToList();

            long? cmdReadCount = filtered.Sum(x => x.Stats?.Read?.Count);
            Assert.Equal(105, cmdReadCount);
            long? cmdReadBytes = filtered.Sum(x => x.Stats?.Read?.AccessedBytes);
            Assert.Equal(67_192_114, cmdReadBytes);

            long? cmdCreateCount = filtered.Sum(x => x.Stats?.Open?.Count);
            Assert.Equal(449, cmdCreateCount);

            long? cmdCloseCount = filtered.Sum(x => x.Stats?.Close?.Count);
            Assert.Equal(281, cmdCloseCount);

            long? cmdWriteCount = filtered.Sum(x => x.Stats?.Write?.Count);
            Assert.Equal(189, cmdWriteCount);

            long? cmdWriteSize = filtered.Sum(x => x.Stats?.Write?.AccessedBytes);
            Assert.Equal(114790706, cmdWriteSize);

            long? cmdWriteDurationus = filtered.Sum(x => x.Stats?.Write?.Durationus);
            Assert.Equal(545053, cmdWriteDurationus);


            // Due to rounding errors we get differences if we sum durations. 
            // We verify IO time by selecting processes with only one read/write event and use that as exact match

            //Line #, Process, Event Type, Duration (µs), Size (B), File Path, Count
            //73, svchost.exe(2232), Read, 5,958.200, 8,192, C:\Windows\System32\schedsvc.dll, 1


            var svchost = container.Where(CreateFilterFor(15916)).ToList();
            long? svcHostReadDurationus = svchost.Sum(x => x.Stats?.Read?.Durationus);

            Assert.Equal(391, svcHostReadDurationus);
        }
    }
}
