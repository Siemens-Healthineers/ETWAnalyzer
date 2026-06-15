//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using Xunit;

namespace ETWAnalyzer_uTest.Commands
{
    /// <summary>
    /// Verifies command line parsing of the optional -MissingPdb filter argument of the Dump Version command.
    /// </summary>
    public class DumpCommandMissingPdbTests
    {
        /// <summary>
        /// When -MissingPdb is specified without a following filter the filter defaults to *
        /// which matches all missing pdbs.
        /// </summary>
        [Fact]
        public void MissingPdb_Without_Filter_Defaults_To_Star()
        {
            DumpCommand cmd = new DumpCommand(new string[] { "Version", "-missingpdb" });
            cmd.Parse();

            Assert.Equal("*", cmd.MissingPdbFilter.Key);
            // Default * filter must match any pdb name.
            Assert.True(cmd.MissingPdbFilter.Value("ntdll.pdb"));
        }

        /// <summary>
        /// When -MissingPdb is followed by a filter the filter string is used as is.
        /// </summary>
        [Fact]
        public void MissingPdb_With_Filter_Uses_Specified_Filter()
        {
            DumpCommand cmd = new DumpCommand(new string[] { "Version", "-missingpdb", "ntdll*" });
            cmd.Parse();

            Assert.Equal("ntdll*", cmd.MissingPdbFilter.Key);
            Assert.True(cmd.MissingPdbFilter.Value("ntdll.pdb"));
            Assert.False(cmd.MissingPdbFilter.Value("kernel32.pdb"));
        }

        /// <summary>
        /// When -MissingPdb is followed by another switch argument the filter defaults to *
        /// and the following switch is still parsed.
        /// </summary>
        [Fact]
        public void MissingPdb_Followed_By_Switch_Defaults_To_Star()
        {
            DumpCommand cmd = new DumpCommand(new string[] { "Version", "-missingpdb", "-clip" });
            cmd.Parse();

            Assert.Equal("*", cmd.MissingPdbFilter.Key);
            Assert.True(cmd.MissingPdbFilter.Value("anything.pdb"));
        }
    }
}
