//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static ETWAnalyzer.Extract.TestDataFile;

namespace ETWAnalyzer_uTest
{
    
    public class TestDataFileTests
    {
        const string ClientName = "FO9DE01T0166PC";

        [Fact]
        public void Can_Load_Empty_7z_File()
        {
            TestDataFile file = new TestDataFile(TestData.TestRunSample_Client);

            Assert.Equal(ClientName, file.MachineName);
            Assert.True(file.IsValidTest);
            Assert.Equal(636728634209271265, file.PerformedAt.ToUniversalTime().Ticks);

            Assert.Empty(file.Screenshots);
            Assert.Equal("CallupAdhocColdReadingCR", file.TestName);
            Assert.Equal(11341, file.DurationInMs);
            Assert.NotNull(file.FileName);
        }

        [Fact]
        public void Can_Parse_FlatFileName()
        {
            // profiling uses            TestCase_dddmsMachine.Date-Time.7z
            // simplified profiling uses TestCase_dddmsMachine-Date-Time.7z
            // SSTUaPMapWorkitemFromRTC2_2645msDEFOR09T130SRV.20200725-125302
            var file = new TestDataFile(new ETLFileInfo( "TestCase_500msMachine.20200725-235959.7z", 0, default));
            Assert.Equal("Machine", file.MachineName);
            Assert.Equal(500, file.DurationInMs);
            Assert.Equal("TestCase", file.TestName);
            Assert.Equal("20200725-235959", file.SpecificModifyDate);

            var file2 = new TestDataFile(new ETLFileInfo("TestCase_500msMachine-20200725-235959.7z", 0, default));
            Assert.Equal("Machine", file2.MachineName);
            Assert.Equal(500, file2.DurationInMs);
            Assert.Equal("TestCase", file2.TestName);


            var file3 = new TestDataFile(new ETLFileInfo("TestCase_500msMachine-With_SomeOtherData.7z", 0, default));
            Assert.Equal("Machine-With_SomeOtherData", file3.MachineName);
            Assert.Equal(500, file3.DurationInMs);
            Assert.Equal("TestCase", file3.TestName);
            Assert.Null(file3.SpecificModifyDate);
        }


        [Fact]
        public void Can_Extract_SpecificTime_From_SimplifiedProfiling_Format()
        {
            var file = new TestDataFile(new ETLFileInfo("LoadPrepCR_2229msNFRQR2004-GURU-20200910-210005.7z", 0, default));
            Assert.Equal("20200910-210005", file.SpecificModifyDate);
        }

    }
}
