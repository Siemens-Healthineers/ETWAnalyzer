using ETWAnalyzer.Commands;
using ETWAnalyzer.Extractors;
using ETWAnalyzer.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest.Extract
{
    public class ExtractCommandTests
    {
        [Fact]
        public void Can_Generate_ChildCmdLine()
        {
            using (var tmp = TempDir.Create())
            {
                string spaceDir = Path.Combine(tmp.Name, "Space Dir");
                Directory.CreateDirectory(spaceDir);

                ExtractCommand cmd = new ExtractCommand(new string[] { "-extract", "all", "-filedir", spaceDir, ArgParser.UnzipOperationArg, "c:\\perftools\\ETLRewrite -injectOnly " + ExtractCommand.ETLFileDirVariable, "-outdir", spaceDir } );
                cmd.Parse();
                string cmdLine = cmd.GetCommandLineForSingleExtractFile("None.etl");
                string expected = $"-extract all -filedir \"None.etl\" -unzipoperation \"c:\\perftools\\ETLRewrite -injectOnly #EtlFileDir#\" -outdir \"{spaceDir}\"  -child";
                Assert.Equal(expected, cmdLine);
            }

        }

        [Fact]
        public void ExtractRegion_Parses_Multiple_Pairs()
        {
            using (var tmp = TempDir.Create())
            {
                ExtractCommand cmd = new ExtractCommand(new string[] { "-extract", "cpu", "-filedir", tmp.Name, ExtractCommand.ExtractRegionArg, "1.0", "2.0", "3.0", "4.5" });
                cmd.Parse();

                Assert.Equal(2, cmd.Regions.Count);
                Assert.Equal(1.0d, cmd.Regions[0].StartS);
                Assert.Equal(2.0d, cmd.Regions[0].EndS);
                Assert.Equal("Time_1.0-2.0", cmd.Regions[0].ToFileNamePart());
                Assert.Equal(3.0d, cmd.Regions[1].StartS);
                Assert.Equal(4.5d, cmd.Regions[1].EndS);
                Assert.Equal("Time_3.0-4.5", cmd.Regions[1].ToFileNamePart());
            }
        }

        [Fact]
        public void ExtractRegion_Duration_Prefix_Is_Relative_To_Start()
        {
            using (var tmp = TempDir.Create())
            {
                ExtractCommand cmd = new ExtractCommand(new string[] { "-extract", "cpu", "-filedir", tmp.Name, ExtractCommand.ExtractRegionArg, "1.0", "+2" });
                cmd.Parse();

                Assert.Single(cmd.Regions);
                Assert.Equal(1.0d, cmd.Regions[0].StartS);
                Assert.Equal(3.0d, cmd.Regions[0].EndS);
                Assert.Equal("Time_1.0-3.0", cmd.Regions[0].ToFileNamePart());
            }
        }

        [Fact]
        public void ExtractRegion_Mixed_Absolute_And_Duration()
        {
            using (var tmp = TempDir.Create())
            {
                ExtractCommand cmd = new ExtractCommand(new string[] { "-extract", "cpu", "-filedir", tmp.Name, ExtractCommand.ExtractRegionArg, "1.0", "2.0", "5.0", "+2.5" });
                cmd.Parse();

                Assert.Equal(2, cmd.Regions.Count);
                Assert.Equal(2.0d, cmd.Regions[0].EndS);
                Assert.Equal("Time_1.0-2.0", cmd.Regions[0].ToFileNamePart());
                Assert.Equal(5.0d, cmd.Regions[1].StartS);
                Assert.Equal(7.5d, cmd.Regions[1].EndS);
                Assert.Equal("Time_5.0-7.5", cmd.Regions[1].ToFileNamePart());
            }
        }

        [Fact]
        public void ExtractRegion_Odd_Number_Of_Values_Throws()
        {
            using (var tmp = TempDir.Create())
            {
                ExtractCommand cmd = new ExtractCommand(new string[] { "-extract", "cpu", "-filedir", tmp.Name, ExtractCommand.ExtractRegionArg, "1.0", "2.0", "3.0" });
                Assert.Throws<InvalidDataException>(() => cmd.Parse());
            }
        }

        [Fact]
        public void ExtractRegion_Start_After_End_Throws()
        {
            using (var tmp = TempDir.Create())
            {
                ExtractCommand cmd = new ExtractCommand(new string[] { "-extract", "cpu", "-filedir", tmp.Name, ExtractCommand.ExtractRegionArg, "5.0", "2.0" });
                Assert.Throws<InvalidDataException>(() => cmd.Parse());
            }
        }

        [Fact]
        public void No_ExtractRegion_Yields_Empty_Regions()
        {
            using (var tmp = TempDir.Create())
            {
                ExtractCommand cmd = new ExtractCommand(new string[] { "-extract", "cpu", "-filedir", tmp.Name });
                cmd.Parse();
                Assert.Empty(cmd.Regions);
            }
        }

        [Fact]
        public void ETWExtractTimeRange_IsWithin_Works()
        {
            var region = new ETWExtractTimeRange("1.0", "2.0");
            Assert.False(region.IsWithin(0.5m));
            Assert.True(region.IsWithin(1.0m));
            Assert.True(region.IsWithin(1.5m));
            Assert.True(region.IsWithin(2.0m));
            Assert.False(region.IsWithin(2.5m));
        }
    }
}

