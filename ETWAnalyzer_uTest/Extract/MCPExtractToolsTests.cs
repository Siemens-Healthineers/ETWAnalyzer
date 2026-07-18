//// SPDX-FileCopyrightText:  © 2026 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands.MCPServer.Tools;
using Xunit;

namespace ETWAnalyzer_uTest.Extract
{
    public class MCPExtractToolsTests
    {
        [Fact]
        public void Extract_Empty_Input_Returns_Error()
        {
            string result = EtwAnalyzerTools.Extract("");
            Assert.StartsWith("Error: No input file specified", result);
        }

        [Fact]
        public void Extract_Whitespace_Input_Returns_Error()
        {
            string result = EtwAnalyzerTools.Extract("   ");
            Assert.StartsWith("Error: No input file specified", result);
        }

        [Fact]
        public void ExtractTimeRange_Empty_Input_Returns_Error()
        {
            string result = EtwAnalyzerTools.ExtractTimeRange("", "1.0 2.0");
            Assert.StartsWith("Error: No input file specified", result);
        }

        [Fact]
        public void Extract_NonExisting_File_Does_Not_Throw()
        {
            // Must not throw. The extraction simply finds no files to extract.
            string result = EtwAnalyzerTools.Extract("C:\\this\\path\\does\\not\\exist_ffffffff.etl");
            Assert.NotNull(result);
        }

        [Fact]
        public void ExtractTimeRange_Invalid_Region_Returns_Error()
        {
            // Odd number of region values is rejected during -extractRegion parsing.
            string result = EtwAnalyzerTools.ExtractTimeRange("C:\\this\\path\\does\\not\\exist_ffffffff.etl", "1.0 2.0 3.0");
            Assert.Contains("Error", result);
        }
    }
}
