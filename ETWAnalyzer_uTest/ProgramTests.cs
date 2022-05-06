//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
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

namespace ETWAnalyzer_uTest
{
    
    public class ProgramTests
    {
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
            ExtractSingleFile.SerializeResults(outFile, new ETWExtract());
            ExtractSerializer.DeserializeFile(outFile);
        }

        // File filtering Test-Methods
        [Fact]
        public void Can_Read_Folder_With_No_TestCaseName_And_No_StartAndStopDates_And_No_ComputerName() // T(0)-S(0)-C(0)
        {
            var runner = new AnalyzeCommand(new string[] { "-filedir", TestData.TestRunDirectory, "-outdir", "C:\\Temp" });
            List<TestDataFile> allMachedFiles = TestRun.ExistingSingleTestsIncludeComputerAndTestNameAndDateFilter(runner.TestCaseNames, runner.ComputerNames,runner.StartAndStopDates,TestRun.CreateFromDirectory(TestData.TestRunDirectory, SearchOption.TopDirectoryOnly, null)).ToTestDataFiles();
            
            Assert.Equal(2900, allMachedFiles.Count);
        }

        [Fact]
        public void Can_Read_Folder_With_ComputerName_And_No_StartAndStopDates_And_No_TestCaseName()// T(0)-S(0)-C(1)
        {
            var runner = new AnalyzeCommand(new string[] { "-filedir", TestData.TestRunDirectory, "-computer", "FO9DE01T0166PC", "-outdir", "C:\\Temp" });
            runner.Parse();

            List<TestDataFile> allMachedFilesPC = TestRun.ExistingSingleTestsIncludeComputerAndTestNameAndDateFilter(runner.TestCaseNames, runner.ComputerNames, runner.StartAndStopDates, TestRun.CreateFromDirectory(TestData.TestRunDirectory, SearchOption.TopDirectoryOnly, null)).ToTestDataFiles();

            runner = new AnalyzeCommand(new string[] { "-filedir", TestData.TestRunDirectory, "-computer", "DEFOR09T121SRV", "-outdir", "C:\\Temp" });
            runner.Parse();

            List<TestDataFile> allMachedFilesSRV = TestRun.ExistingSingleTestsIncludeComputerAndTestNameAndDateFilter(runner.TestCaseNames, runner.ComputerNames, runner.StartAndStopDates, TestRun.CreateFromDirectory(TestData.TestRunDirectory, SearchOption.TopDirectoryOnly, null)).ToTestDataFiles();

            Assert.Equal(2899,allMachedFilesPC.Count+allMachedFilesSRV.Count);
        }

        [Fact]
        public void Can_Read_Folder_With_StartAndStopDates_And_No_TestCaseName_And_No_Computer() // T(0)-S(1)-C(0)
        {
            var runner = new AnalyzeCommand(new string[] { "-filedir", TestData.TestRunDirectory, "-timerange", "12.09.2018 14.09.2018", "-outdir", "C:\\Temp" });
            runner.Parse();

            List<TestDataFile> allMachedFiles = TestRun.ExistingSingleTestsIncludeComputerAndTestNameAndDateFilter(runner.TestCaseNames, runner.ComputerNames, runner.StartAndStopDates, TestRun.CreateFromDirectory(TestData.TestRunDirectory, SearchOption.TopDirectoryOnly, null)).ToTestDataFiles();
            Assert.Equal(106, allMachedFiles.Count);
        }

        [Fact]
        public void Can_Read_Folder_With_StartAndStopDatesSecondFormat_And_No_TestCaseName_And_No_Computer() // T(0)-S(1)-C(0)
        {
            using var tmp = TempDir.Create();
            File.Copy(TestData.ClientEtlFile, Path.Combine(tmp.Name, Path.GetFileName(TestData.ClientEtlFile)));
            File.SetLastWriteTime(Path.Combine(tmp.Name, Path.GetFileName(TestData.ClientEtlFile)), DateTime.Now);
            var serverLocalTmp = Path.Combine(tmp.Name, Path.GetFileName(TestData.ServerEtlFile)).Replace("124447", "124448"); // make SpecificModify Date different or else it will end up in the same Test
            File.Copy(TestData.ServerEtlFile, serverLocalTmp);
            File.SetLastWriteTime(serverLocalTmp, DateTime.Now.Subtract(TimeSpan.FromDays(7.1)));

            var runner = new AnalyzeCommand(new string[] { "-filedir", tmp.Name, "-timerange", "7", "-outdir", "C:\\Temp" });
            runner.Parse();

            List<TestDataFile> allMachedFiles = TestRun.ExistingSingleTestsIncludeComputerAndTestNameAndDateFilter(runner.TestCaseNames, runner.ComputerNames, runner.StartAndStopDates, TestRun.CreateFromDirectory(tmp.Name, SearchOption.TopDirectoryOnly, null)).ToTestDataFiles();
            Assert.Single(allMachedFiles);
        }

        [Fact]
        public void Can_Read_Folder_With_StartAndStopDates_And_Computer_And_No_TestCaseName()// T(0)-S(1)-C(1)
        {
            var runner = new AnalyzeCommand(new string[] { "-filedir", TestData.TestRunDirectory, "-computer", "FO9DE01T0166PC", "-timerange", "12.09.2018 14.09.2018", "-outdir", "C:\\Temp" });
            runner.Parse();

            List <TestDataFile> allMachedFiles = TestRun.ExistingSingleTestsIncludeComputerAndTestNameAndDateFilter(runner.TestCaseNames, runner.ComputerNames, runner.StartAndStopDates, TestRun.CreateFromDirectory(TestData.TestRunDirectory, SearchOption.TopDirectoryOnly, null)).ToTestDataFiles();
            Assert.Equal(53, allMachedFiles.Count);
        }

        [Fact]
        public void Can_Read_Folder_With_TestCaseName_And_No_StartAndStopDates_And_No_Computer() // T(1)-S(0)-C(0)
        {
            var runner = new AnalyzeCommand(new string[] { "-filedir", TestData.TestRunDirectory, "-testcase", "CallupAdhocWarmReadingMR", "-outdir", "C:\\Temp" });
            runner.Parse();

            List<TestDataFile> allMachedFiles = TestRun.ExistingSingleTestsIncludeComputerAndTestNameAndDateFilter(runner.TestCaseNames, runner.ComputerNames, runner.StartAndStopDates ,TestRun.CreateFromDirectory(TestData.TestRunDirectory, SearchOption.TopDirectoryOnly, null)).ToTestDataFiles();
            Assert.Equal(246, allMachedFiles.Count);
        }

        [Fact]
        public void Can_Read_Folder_With_TestCaseName_And_Computer_And_No_StartAndStopDates()// T(1)-S(0)-C(1)
        {
            var runner = new AnalyzeCommand(new string[] { "-filedir", TestData.TestRunDirectory, "-testcase", "CallupAdhocWarmReadingMR", "-computer", "DEFOR09T121SRV", "-outdir", "C:\\Temp" });
            runner.Parse();

            List<TestDataFile> allMachedFiles = TestRun.ExistingSingleTestsIncludeComputerAndTestNameAndDateFilter(runner.TestCaseNames, runner.ComputerNames, runner.StartAndStopDates, TestRun.CreateFromDirectory(TestData.TestRunDirectory, SearchOption.TopDirectoryOnly, null)).ToTestDataFiles();
            Assert.Equal(123, allMachedFiles.Count);
        }

        [Fact]
        public void Can_Read_Folder_With_TestCaseName_And_StartAndStopDates_No_Computer() // T(1)-S(1)-C(0)
        {
            var runner = new AnalyzeCommand(new string[] { "-filedir", TestData.TestRunDirectory, "-testcase", "CallupAdhocWarmReadingMR", "-timerange", "21.09.2018 22.09.2018", "-outdir", "C:\\Temp" });
            runner.Parse();

            List<TestDataFile> allMachedFiles = TestRun.ExistingSingleTestsIncludeComputerAndTestNameAndDateFilter(runner.TestCaseNames, runner.ComputerNames, runner.StartAndStopDates, TestRun.CreateFromDirectory(TestData.TestRunDirectory, SearchOption.TopDirectoryOnly, null)).ToTestDataFiles();
            Assert.Equal(14, allMachedFiles.Count);
        }

        [Fact]
        public void Can_Read_Folder_With_TestCaseName_And_StartAndStopDates_And_Computer()// T(1)-S(1)-C(1)
        {
            var runner = new AnalyzeCommand(new string[] { "-filedir", TestData.TestRunDirectory, "-testcase", "CallupAdhocWarmReadingMR", "-timerange", "21.09.2018 22.09.2018", "-Computer", "FO9DE01T0166PC", "-outdir", "C:\\Temp" });
            runner.Parse();

            List<TestDataFile> allMachedFiles = TestRun.ExistingSingleTestsIncludeComputerAndTestNameAndDateFilter(runner.TestCaseNames, runner.ComputerNames, runner.StartAndStopDates ,TestRun.CreateFromDirectory(TestData.TestRunDirectory, SearchOption.TopDirectoryOnly, null)).ToTestDataFiles();
            Assert.Equal(7, allMachedFiles.Count);
        }



    }
}
