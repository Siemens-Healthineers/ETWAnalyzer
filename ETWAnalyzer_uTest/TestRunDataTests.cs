//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest
{
    public class TestRunDataTests
    {
        [Fact]
        public void Can_Create_TestRunData_From_Single_Json_File()
        {
            using var tmp = TempDir.Create();
            string jsonFile = Path.Combine(tmp.Name, "Test.json");
            CreateFiles(jsonFile);
            TestRunData runData = new TestRunData(tmp.Name);
            Assert.Single(runData.AllFiles);

            TestRunData runDataSingleJsonFile = new TestRunData(jsonFile);
            Assert.Single(runDataSingleJsonFile.AllFiles);

            TestDataFile file = runData.AllFiles[0];

            Assert.Equal("Test", file.TestName);  // Test name should not contain file extension if it could no be parsed
            Assert.NotNull(file.JsonExtractFileWhenPresent);
            Assert.Null(file.EtlFileNameIfPresent);
            Assert.Equal(jsonFile, file.FileName);
            Assert.NotNull(file.ParentTest);
            Assert.NotNull(file.ParentTest.Parent);
            Assert.NotNull(file.ParentTest.Parent.Parent);
        }

        [Fact]
        public void Can_Create_TestRunData_From_Single_Json_File_NoExtension()
        {
            using var tmp = TempDir.Create();
            string jsonFile = Path.Combine(tmp.Name, "Test.json");
            CreateFiles(jsonFile);
            

            TestRunData runDataSingleJsonFile = new TestRunData(Path.Combine(tmp.Name, "Test"));
            Assert.Single(runDataSingleJsonFile.AllFiles);
        }

        [Fact]
        public void Can_Navigate_FromTestRun_To_Derived_Files_In_Output_Folder()
        {
            using var tmp = TempDir.Create();
            string outFolder = Path.Combine(tmp.Name, "OutputFolder");
            Directory.CreateDirectory(outFolder);
            string outExtractFolder = Path.Combine(outFolder, Program.ExtractFolder);
            Directory.CreateDirectory(outExtractFolder);

            string sevenZFile = Path.Combine(tmp.Name, "Test1.7z");
            string extractedToTempFile = Path.Combine(outFolder, "Test1.etl");
            string extractJsonFile = Path.Combine(outFolder, "Test1.json");
            CreateFiles(sevenZFile, extractedToTempFile, extractJsonFile);

            TestRunData runData = new TestRunData(tmp.Name, SearchOption.TopDirectoryOnly, outFolder);

            Assert.Single(runData.AllFiles);
            TestDataFile file = runData.AllFiles[0];

            Assert.Equal(sevenZFile, file.FileName);
            Assert.Equal(extractedToTempFile, file.EtlFileNameIfPresent);
            Assert.Equal(extractJsonFile, file.JsonExtractFileWhenPresent);
            Assert.NotNull(file.ParentTest);
            Assert.NotNull(file.ParentTest.Parent);
            Assert.NotNull(file.ParentTest.Parent.Parent);
        }

        [Fact]
        public void Json_And_ZipFiles_From_Other_Folder_Are_Merged()
        {
            using var tmp = TempDir.Create();

            string otherFolder = Path.Combine(tmp.Name, "OtherFolder");
            Directory.CreateDirectory(otherFolder);

            const string testfileNameNoExt = "LoadPrepCR_2229msNFRQR2004-GURU-20200910-210005";
            const string clientTestfileNameNoExt = "LoadPrepCR_2229msRTC-20200910-210005";

            string clientSevenZFile = Path.Combine(tmp.Name, clientTestfileNameNoExt + TestRun.SevenZExtension);
            string clientextractJson = Path.Combine(tmp.Name, clientTestfileNameNoExt + TestRun.ExtractExtension);
            CreateFiles(clientSevenZFile, clientextractJson);

            string sevenZFile = Path.Combine(tmp.Name, testfileNameNoExt+TestRun.SevenZExtension);
            string extractJsonFile = Path.Combine(otherFolder, testfileNameNoExt + TestRun.ExtractExtension);
            CreateFiles(sevenZFile, extractJsonFile);

            var runData = new TestRunData(tmp.Name, SearchOption.AllDirectories);


            Assert.Equal(2, runData.AllFiles.Count);

            TestDataFile guru = runData.AllFiles.Where(x => x.MachineName == "NFRQR2004-GURU").Single();
            TestDataFile rtc =  runData.AllFiles.Where(x => x.MachineName == "RTC").Single();

            Assert.Equal(sevenZFile, guru.FileName);
            Assert.Equal(extractJsonFile, guru.JsonExtractFileWhenPresent);
            Assert.Equal(clientSevenZFile, rtc.FileName);
            Assert.Equal(clientextractJson, rtc.JsonExtractFileWhenPresent);
        }

        [Fact]
        public void Input_Etl_And_Extract_SideBySide_Result_In_Single_TestDataFile_LeadToSingleTestDataFile()
        {
            using var tmp = TempDir.Create();
            const string testFileNameNoExt = "LoadPrepCR";

            string etlFile = Path.Combine(tmp.Name, testFileNameNoExt + TestRun.ETLExtension);
            string extracted = Path.Combine(tmp.Name, testFileNameNoExt + TestRun.ExtractExtension);

            CreateFiles(etlFile, extracted);

            var runData = new TestRunData(tmp.Name, SearchOption.AllDirectories);

            Assert.Single(runData.AllFiles);
            Assert.Contains(TestRun.ETLExtension, runData.AllFiles[0].FileName);
        }

        /// <summary>
        /// When etl and extract are side by side do not create duplicate TestRunDataFiles which leads to duplicate output during queries which have recursive search enabled
        /// </summary>
        [Fact]
        public void Input_Etl_And_Extract_Result_In_SubFolder_LeadToSingleTestDataFile()
        {
            using var tmp = TempDir.Create();
            string dir = Path.Combine(tmp.Name, "SubDir");
            const string testFileNameNoExt = "LoadPrepCR";

            string etlFile = Path.Combine(dir, testFileNameNoExt + TestRun.ETLExtension);
            string extractedSubFolder = Path.Combine(dir, Program.ExtractFolder, testFileNameNoExt + TestRun.ExtractExtension);

            CreateFiles(etlFile, extractedSubFolder);

            var runData = new TestRunData(tmp.Name, SearchOption.AllDirectories);

            Assert.Single(runData.AllFiles);
            Assert.Contains(TestRun.ETLExtension, runData.AllFiles[0].FileName);
        }


        [Fact]
        public void Input_SevenZ_And_Extract_Result_In_SubFolder_LeadToSingleTestDataFile()
        {
            using var tmp = TempDir.Create();
            string dir = Path.Combine(tmp.Name, "SubDir");
            const string testFileNameNoExt = "LoadPrepCR";

            string zipFile = Path.Combine(dir, testFileNameNoExt + TestRun.SevenZExtension);
            string extractedSubFolder = Path.Combine(dir, Program.ExtractFolder, testFileNameNoExt + TestRun.ExtractExtension);

            CreateFiles(zipFile, extractedSubFolder);

            var runData = new TestRunData(tmp.Name, SearchOption.AllDirectories);

            Assert.Single(runData.AllFiles);
            Assert.Contains(TestRun.SevenZExtension, runData.AllFiles[0].FileName);
        }

        void CreateFiles(params string[] paths)
        {
            foreach (var path in paths)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, "test");
            }
        }
    }
}
