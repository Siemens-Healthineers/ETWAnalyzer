//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Helper;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using ETWAnalyzer.Extractors;
using ETWAnalyzer.Analyzers.Infrastructure;
using ETWAnalyzer.Commands;
using ETWAnalyzer_uTest.TestInfrastructure;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;
using ETWAnalyzer.Infrastructure;

namespace ETWAnalyzer_uTest
{
    
    public class ProgramTests
    {

        private ITestOutputHelper myWriter;

        public ProgramTests(ITestOutputHelper myWriter)
        {
            this.myWriter = myWriter;
        }

        [Fact]
        public void Throw_FileNotFoundException_On_Not_Existing_Input_File()
        {
            const string notExistingFile = "NotExistingFile";

            ExceptionAssert.Throws<DirectoryNotFoundException>(() =>
              Program.MainCore(new string[] {"-extract", "Disk", "-filedir", notExistingFile,  "-outdir", "C:\\Temp" })
            , notExistingFile);
        }


        [Fact]
        public void Throw_FormatException_On_InputFile_With_WrongExtension()
        {
            ExceptionAssert.Throws<FormatException>(() =>
                Program.MainCore(new string[] { "-extract", "Disk", "-filedir", TestData.DiscIOWPAProfile, "-outdir", "." })
            , TestData.DiscIOWPAProfile);
        }

        [Fact]
        public void Throw_ArgumentExceptions_On_Not_Supported_Processing_Action()
        {
            const string wrongAction = "i_am_not_supported_processing_action";

            ExceptionAssert.Throws<ArgumentException>(() =>
              Program.MainCore(new string[] { "-extract", wrongAction,"-filedir", TestData.ServerEtlFile,  "-outdir", "C:\\Temp" })
              , wrongAction);
        }

        [Fact]
        public void Throw_ArgumentExceptions_On_Not_Supported_Analyzing_Action()
        {
            const string wrongAction = "i_am_not_supported_processing_action";

            using var tmp = TempDir.Create();
            string existingJson = Path.Combine(tmp.Name, "test.json");
            File.WriteAllText(existingJson, "test");

            ExceptionAssert.Throws<ArgumentException>(() =>
              Program.MainCore(new string[] { "-analyze", wrongAction,"-filedir", existingJson,  "-outdir", "C:\\Temp" })
              , wrongAction);
        }

        [Fact]
        public void Throw_InvalidDataException_When_Processing_Action_Is_Missing()
        {
            ExceptionAssert.Throws<InvalidDataException>(() =>
                Program.MainCore(new string[] { "-extract","-filedir", TestData.ClientEtlFile,  "-outdir", "C:\\Temp" })
            , "-extract");
        }

        [Fact]
        public void Throw_InvalidDataException_When_Analyzing_Action_Is_Missing()
        {
            ExceptionAssert.Throws<InvalidDataException>(() =>
                Program.MainCore(new string[] { "-analyze","-filedir", TestData.ServerEtlFile,  "-outdir", "C:\\Temp" })
            , "-analyze");
        }

        [Fact]
        public void SymbolFolder_Can_Be_Passed_Via_Cmd_Line()
        {
            const string TestSymbolFolder = "asd;lfkjas;dlfj";

            var runner = new ExtractCommand(new string[] { "-filedir", TestData.ServerEtlFile, "-outdir", "C:\\Temp", "-symfolder", TestSymbolFolder });
            runner.Parse();

            Assert.Equal(TestSymbolFolder, runner.Symbols.SymbolFolder);
        }

        [Fact]
        public void Can_Serialize_ETWExtract()
        {
            using var tmp = TempDir.Create();
            string outFile = Path.Combine(tmp.Name, "out.json");

            ExtractSingleFile.SerializeResults(outFile, new ETWExtract());

            var fileInfo = new FileInfo(outFile);
            Assert.True(fileInfo.Exists, $"Output file {outFile} was not created");
            Assert.True(fileInfo.Length > 0, $"File {outFile} has no content");
        }

        [Fact]
        public void Can_Deserialize_ETWExtract()
        {
            using var tmp = TempDir.Create();
            string outFile = Path.Combine(tmp.Name, "out.json");
            var sessionStart = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero);
            ExtractSingleFile.SerializeResults(outFile, new ETWExtract()
            {
                SessionStart = sessionStart
            });

            var fileInfo = new FileInfo(outFile);
            // During serialization we set the last write time to the ETW session start time
            // that way we can simply sort all extracted file by write time to locate the relevant recording
            Assert.Equal(sessionStart, fileInfo.LastWriteTime);

            ExtractSerializer.DeserializeFile(outFile);
        }
 
    }
}
