//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Extract;
using Xunit;
using Microsoft.Windows.EventTracing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ETWAnalyzer.Extractors;
using System.IO;
using System.Threading;
using ETWAnalyzer.Infrastructure;

namespace ETWAnalyzer_uTest.Extractors
{

    /// <summary>
    /// Ensure in xUnit Tests that we have a valid ETWExtract ready before any test runs this is done via IClassFixture
    /// https://stackoverflow.com/questions/46926852/xunit-constructor-runs-before-each-test
    /// </summary>
    public sealed class MachineDetailsFixture : IDisposable
    {
        internal static readonly object myLock = new();

        public ETWExtract Extract
        {
            get => myInteralExtract;
        }

        static ETWExtract myInteralExtract = null;

        public MachineDetailsFixture()
        {
            // parallel parsing of ETWs is not supported, or we will get sometimes
            // All tests which do TraceProcessor.Create/Process stuff will need this lock!
            //    System.InvalidOperationException : TraceAggregation has already been initalized.
            lock (DiskExtractorTestsFixture._Lock)
            {
                if (myInteralExtract == null)
                {
                    var tmp = new ETWExtract();
                    using ITraceProcessor processor = new TraceProcessorBuilder().WithSettings(new TraceProcessorSettings
                    {
                        AllowLostEvents = true,
                    }).Build(TestData.ClientEtlFile);
                    MachineDetailsExtractor extractor = new();
                    extractor.RegisterParsers(processor);
                    processor.Process();
                    extractor.Extract(processor, tmp);
                    myInteralExtract = tmp;
                }
            }
        }

        public void Dispose()
        {
        }
    }

    public class MachineDetailsExtractorTests : IClassFixture<MachineDetailsFixture>
    {
        /// <summary>
        /// Generated once by MachineDetailsFixture which is injected via ctor
        /// </summary>
        readonly ETWExtract myExtract;

        public MachineDetailsExtractorTests(MachineDetailsFixture fixture)
        {
            myExtract = fixture.Extract;  
        }


        [Fact]
        public void Can_Get_InputETLFileName()
        {
            Assert.Equal(TestData.ClientEtlFile, myExtract.SourceETLFileName);
        }

        [Fact]
        public void Can_Get_DomainName()
        {
            Assert.Equal("", myExtract.AdDomain);
            Assert.False(myExtract.IsDomainJoined);
        }

        [Fact]
        public void Can_GetModel()
        {
            Assert.Equal("MS-7846", myExtract.Model);
        }


        [Fact]
        public void Can_Get_Display_Devices()
        {
            Assert.Equal(7, myExtract.Displays.Count);
            var disp0 = myExtract.Displays[0];
            Assert.Equal(32, disp0.ColorDepth);
            Assert.Equal("AMD Radeon RX 5600 XT", disp0.DisplayName);
            Assert.Equal("AMD Radeon Graphics Processor (0x731F)", disp0.GraphicsCardChipName);
            Assert.Equal(4095, disp0.GraphicsCardMemorySizeMiB);
            Assert.Equal(2160, disp0.VerticalResolution);
            Assert.Equal(3840, disp0.HorizontalResolution);
            Assert.True(disp0.IsPrimaryDevice);
            Assert.Equal(59, disp0.RefreshRateHz);

            // not connected displays are returned with 0 x.y resolution and 0 bpp color depth
            Assert.Equal(0, myExtract.Displays[1].ColorDepth);
        }

        [Fact]
        public void Can_Get_OS_Name()
        {
            Assert.Equal("Windows 10 Pro", myExtract.OSName);
            Assert.Equal("Windows 8.1", myExtract.WinSatOSName);
        }

        [Fact]
        public void Can_Get_OS_Version()
        {
            Assert.Equal(new Version("10.0.19043"), myExtract.OSVersion);
        }

        [Fact]
        public void Can_Get_Trace_Start_Stop_Duration()
        {
            Assert.Equal(new DateTimeOffset(637725211144752345L, TimeSpan.FromHours(1)), myExtract.SessionStart);
            Assert.Equal(new DateTimeOffset(637725211529228305L, TimeSpan.FromHours(1)), myExtract.SessionEnd);
            Assert.Equal(new TimeSpan(384475960L), myExtract.SessionDuration);
        }


        [Fact]
        public void ETWProcess_HasEndedFlag_Is_Set_On_ExpectedProcesses()
        {
            // only a subset of the ended processes are tested
            var expectedEndedProcesses = new KeyValuePair<int, string>[]
            {
                new KeyValuePair<int,string>(25976, "net1.exe"),
                new KeyValuePair<int,string>(25728, "conhost.exe"),
                new KeyValuePair<int,string>(23520, "cmd.exe"),
            };

            foreach(KeyValuePair<int, string> expectedEnd in expectedEndedProcesses )
            {
                ETWProcess proc = myExtract.Processes.First(x => x.ProcessID == expectedEnd.Key && x.ProcessName == expectedEnd.Value);
                Assert.True(proc.HasEnded);
            }
        }

        [Fact]
        public void ETWProcess_IsNewFlag_Is_Set_On_NewProcessesOnly()
        {
            // only a subset of the ended processes are tested
            var exptectedNewProcesses = new KeyValuePair<int, string>[]
            {
                new KeyValuePair<int,string>(25976, "net1.exe"),
            };

            var expectedExisting = new KeyValuePair<int, string>[]
            {
                new KeyValuePair<int, string>(4, "System"),
            };

            foreach (KeyValuePair<int, string> expectedEnd in exptectedNewProcesses)
            {
                ETWProcess proc = myExtract.Processes.First(x => x.ProcessID == expectedEnd.Key && x.ProcessName == expectedEnd.Value);
                Assert.True(proc.IsNew);
            }

            foreach(KeyValuePair<int,string> oldProcess in expectedExisting)
            {
                ETWProcess proc = myExtract.Processes.First(x => x.ProcessID == oldProcess.Key && x.ProcessName == oldProcess.Value);
                Assert.False(proc.IsNew);
            }
        }

        [Fact]
        public void Can_Deserialize_Displays()
        {
            ETWExtract extract = new();

            extract.Displays.Add(new Display
            {
                ColorDepth = 32,
                DisplayName = "Alois",
                IsPrimaryDevice = true,
            });

            MemoryStream stream = new();
            ExtractSerializer.Serialize(stream, extract);
            stream.Position = 0;
            ETWExtract deserialized = ExtractSerializer.Deserialize<ETWExtract>(stream);

            Assert.Single(deserialized.Displays);
            Assert.Equal(32, deserialized.Displays[0].ColorDepth);
            Assert.Equal("Alois", deserialized.Displays[0].DisplayName);
            Assert.True(deserialized.Displays[0].IsPrimaryDevice);
        }
    }
}
