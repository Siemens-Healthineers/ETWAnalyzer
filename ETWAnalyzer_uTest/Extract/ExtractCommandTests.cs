using ETWAnalyzer.Commands;
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
    }
}
