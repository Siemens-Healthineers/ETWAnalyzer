//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Disk;
using Xunit;
using Microsoft.Windows.EventTracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETWAnalyzer.Extractors;
using System.Threading;

namespace ETWAnalyzer_uTest
{

    /// <summary>
    /// Ensure in xUnit Tests that we have a valid ETWExtract ready before any test runs this is done via IClassFixture
    /// https://stackoverflow.com/questions/46926852/xunit-constructor-runs-before-each-test
    /// </summary>
    public sealed class DiskExtractorTestsFixture : IDisposable
    {
        public ETWExtract Extract
        {
            get => myInteralExtract;
        }

        static ETWExtract myInteralExtract = null;

        internal static object _Lock = new();

        public DiskExtractorTestsFixture()
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
                    DiskExtractor diskExtractor = new();
                    extractor.RegisterParsers(processor);
                    diskExtractor.RegisterParsers(processor);
                    processor.Process();
                    extractor.Extract(processor, tmp);
                    diskExtractor.Extract(processor, tmp);
                    myInteralExtract = tmp; // publish in a atomic way to prevent seeing Null Objects.
                }
            }
        }

        public void Dispose()
        {
        }
    }

    public class DiskExtractorTests : IClassFixture<DiskExtractorTestsFixture>
    {
        /// <summary>
        /// Generated once by DiskExtractorTestsFixture which is injected via ctor
        /// </summary>
        readonly ETWExtract myExtract;

        public DiskExtractorTests(DiskExtractorTestsFixture fixture)
        {
            myExtract = fixture.Extract;
        }

        [Fact]
        public void Can_Get_Correct_DiskMetrics_From_ETL()
        {
            Assert.NotNull(myExtract.Disk);
            // Compared to WPA we have some deviations here because we sum up microseconds and not nanoseconds which add up as
            // small sub ms error in the total here but the general relation stays the same
            Assert.Equal(103425uL, myExtract.Disk.TotalDiskFlushTimeInus);
            Assert.Equal(643237uL, myExtract.Disk.TotalDiskReadTimeInus);
            Assert.Equal(1133445uL, myExtract.Disk.TotalDiskServiceTimeInus);
            Assert.Equal(386783uL, myExtract.Disk.TotalDiskWriteTimeTimeInus);

            //Assert.Equal()
            Dictionary<DiskIOTypes, DiskActivity> cDriveSingleFile = myExtract.Disk.DriveToPath[DiskNrOrDrive.D].FilePathToDiskEvents[@"D:\Source\git\WMIWatcher\bin\Release\netcoreapp3.1\win-x64\coreclr.dll"];
            Assert.Single(cDriveSingleFile);
            Assert.True(cDriveSingleFile.ContainsKey(DiskIOTypes.Read));
            DiskActivity diskActivity = cDriveSingleFile[DiskIOTypes.Read];
            Assert.Equal(DiskIOPriorities.Normal, diskActivity.Priorities);
            Assert.Single(diskActivity.Processes);
            Assert.Equal(new KeyValuePair<int,DateTimeOffset>(5616, DateTimeOffset.MinValue), diskActivity.Processes.First()) ;
            Assert.Equal(6, diskActivity.ThreadIDs.Count);
            Assert.Equal(7104, diskActivity.ThreadIDs.First());
        }

        [Fact]
        public void Get_Correct_Number_Of_DetectedDrives()
        {
            /*
                Count = 7
                    [0]: D
                    [1]: C
                    [2]: Id0
                    [3]: Unknown
                    [4]: E
                    [5]: Id1
                    [6]: F
            */
            var keys = myExtract.Disk.DriveToPath.Keys;

            Assert.Equal(5, keys.Count);
            Assert.Contains(DiskNrOrDrive.D, keys);
            Assert.Contains(DiskNrOrDrive.C, keys);
            Assert.Contains(DiskNrOrDrive.Id0, keys);
            Assert.Contains(DiskNrOrDrive.Unknown, keys);
            Assert.Contains(DiskNrOrDrive.Id1, keys);
        }
    }
}
