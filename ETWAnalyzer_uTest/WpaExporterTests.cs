//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using System;
using Xunit;

namespace ETWAnalyzer_uTest
{
    
    public class WpaExporterTests
    {
        [Fact]
        public void ThrowArgumentException_When_InputFile_Is_Null_Or_Empty()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => new WpaExportCommand("", TestData.CPUWPAProfile, TestData.ExecutableDirectory, null, null),
                "etlFile");
        }

        [Fact]
        public void ThrowArgumentException_When_WpaProfileFile_Is_Null_Or_Empty()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => new WpaExportCommand(TestData.ServerEtlFile, "", TestData.ExecutableDirectory, null, null),
                "wpaProfile");
        }

        [Fact]
        public void ThrowArgumentException_When_OutputDirectory_Is_Null_Or_Empty()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => new WpaExportCommand(TestData.ServerEtlFile, TestData.CPUWPAProfile, "", null, null),
                "outputFolder");
        }

    }
}
