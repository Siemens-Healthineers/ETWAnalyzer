//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Helper;
using Xunit;
using System;
using System.IO;
using System.Linq;
using ETWAnalyzer.ProcessTools;
using ETWAnalyzer.Reader.ProcessTools;

namespace ETWAnalyzer_uTest
{
    
    public class ETLZipCommandTests
    {
        [Fact]
        public void Can_Extract_Zip()
        {
            using var tmp = TempDir.Create();
            var zipExtractor = new EtlZipCommand();
            string etlPath = zipExtractor.Unzip(TestData.EmptyETL, tmp.Name, new SymbolPaths { SymbolFolder = tmp.Name });
            string unzippedFile = Path.Combine(tmp.Name, nameof(TestData.EmptyETL) + ".etl");
            Assert.True(File.Exists(unzippedFile), $"Unzipped file {unzippedFile} was not found!");
        }

        [Fact]
        public void Logfile_Is_Not_Extracted()
        {
            using var tmp = TempDir.Create();
            var zipExtractor = new EtlZipCommand();
            string etlPath = zipExtractor.Unzip(TestData.ZipWith7zLogFile, tmp.Name, new SymbolPaths { SymbolFolder = tmp.Name });
            var files = Directory.GetFiles(tmp.Name);
            var logs = files.Where(file => file.Contains(EtlZipCommand.SharedLogFile));
            Assert.True(!logs.Any(), $"7z logfiles should not be extracted, but the following 7z log files are existing {string.Join(";",logs)}");
        }



        [Fact]
        public void Do_Not_Unzip_Again_If_Unzipped_File_Already_Exists()
        {
            using var tmp = TempDir.Create();
            var zipExtractor = new EtlZipCommand();
            string etlPath = zipExtractor.Unzip(TestData.ZipWithTwoFiles, tmp.Name, new SymbolPaths { SymbolFolder = tmp.Name });
            string expectedFile = Path.Combine(tmp.Name, nameof(TestData.ZipWithTwoFiles) + ".txt");
            Assert.True(File.Exists(expectedFile), $"This zip file should contain a text file at {expectedFile}");
            File.Delete(expectedFile);
            zipExtractor.Unzip(TestData.ZipWithTwoFiles, tmp.Name, new SymbolPaths { SymbolFolder = tmp.Name });
            Assert.True(!File.Exists(expectedFile)); // After second unzip operation the etl file is used as marker to skip the zip operation
        }

        [Fact]
        public void Overwrite_Files_If_OverwriteFlag_Is_Passed()
        {
            using ITempOutput tmp = TempDir.Create();
            var zipExtractor = new EtlZipCommand(forceOverwrite: true);

            string etlPath = zipExtractor.Unzip(TestData.ZipWithTwoFiles, tmp.Name, new SymbolPaths { SymbolFolder = tmp.Name });
            string expectedFile = Path.Combine(tmp.Name, nameof(TestData.ZipWithTwoFiles) + ".txt");
            Assert.True(File.Exists(expectedFile), $"This zip file should contain a text file at {expectedFile}");
            File.Delete(expectedFile);
            zipExtractor.Unzip(TestData.ZipWithTwoFiles, tmp.Name, new SymbolPaths { SymbolFolder = tmp.Name });
            Assert.True(File.Exists(expectedFile)); // After second unzip operation file must exist again due to overwrite flag
        }

        [Fact]
        public void Fail_When_Zip_Contains_No_ETL()
        {
            using var tmp = TempDir.Create();
            var zipExtractor = new EtlZipCommand();
            ExceptionAssert.Throws<InvalidZipContentsException>(() => zipExtractor.Unzip(TestData.ZipWithTextFile, tmp.Name, new SymbolPaths { SymbolFolder = tmp.Name }));
            string unzippedFile = Path.Combine(tmp.Name, nameof(TestData.ZipWithTextFile) + ".txt");
            Assert.True(File.Exists(unzippedFile), $"Unzipped file {unzippedFile} was not found!");
        }

        [Fact]
        public void Can_Parse_ErrorCount()
        {
            string[] lines1 = new string[] 
            {
                "7 - Zip[64] 9.20  Copyright(c) 1999 - 2010 Igor Pavlov  2010 - 11 - 18",
                "",
                "Processing archive: _3_56_40.18LongTrace.zip",
                "",
                "Error: Can not open output file 7ZipLog.txt",
                "",
                "Sub items Errors: 1"
            };

            string[] lines20Errors = new string[]
            {
                "7 - Zip[64] 9.20  Copyright(c) 1999 - 2010 Igor Pavlov  2010 - 11 - 18",
                "Processing archive: _3_56_40.18LongTrace.zip",
                "Error: can not open output file 7ZipLog.txt",
                "Sub items Errors: 20"
            };

            string[] lines_DifferentFile = new string []
            {
                "7 - Zip[64] 9.20  Copyright(c) 1999 - 2010 Igor Pavlov  2010 - 11 - 18",
                "Processing archive: _3_56_40.18LongTrace.zip",
                "Error: can not open output file 7ZipLog_OTHERFILE.txt",
                "Sub items Errors: 1"
            };
            string[] lines_CannotDelete = new string[]
            {
                "7 - Zip[64] 9.20  Copyright(c) 1999 - 2010 Igor Pavlov  2010 - 11 - 18",
                "Processing archive: _3_56_40.18LongTrace.zip",
                "ERROR: Can not delete output file 7ZipLog.txt",
                "Sub items Errors: 1"
            };

            string[] lines_CannotDeleteDifferentFile = new string[]
            {
                "7 - Zip[64] 9.20  Copyright(c) 1999 - 2010 Igor Pavlov  2010 - 11 - 18",
                "Processing archive: _3_56_40.18LongTrace.zip",
                "ERROR: Can not delete output file 7ZipLogOTHERFILE.txt",
                "Sub items Errors: 1"
            };

            Assert.True(EtlZipCommand.HasSingleFileError(lines1));
            Assert.False(EtlZipCommand.HasSingleFileError(lines20Errors));
            Assert.False(EtlZipCommand.HasSingleFileError(lines_DifferentFile));
            Assert.False(EtlZipCommand.HasSingleFileError(lines_DifferentFile));
            Assert.True(EtlZipCommand.HasSingleFileError(lines_CannotDelete));
            Assert.False(EtlZipCommand.HasSingleFileError(lines_CannotDeleteDifferentFile));
        }

        [Fact]
        public void Ignoring_FileSharingViolation_in_SharedLogFile_during_Unzip()
    
        {
            string[] lines1 = new string[]
            {
                "7-Zip 19.00 (x86) : Copyright (c) 1999-2018 Igor Pavlov : 2019-02-21",
                "Scanning the drive for archives:",
                @"1 file, 65547127 bytes (63 MiB)",
                @"Extracting archive: D:\tmp_input\P01Open_1750ms_IBDI1VIARELP027_SRV_TestStatus-Passed_20210224-082343.7z",
                "--",
                @"Path = D:\tmp_input\P01Open_1750ms_IBDI1VIARELP027_SRV_TestStatus-Passed_20210224-082343.7z",
                "Type = 7z",
                "Physical Size = 65547127",
                "Headers Size = 89073",
                "Method = LZMA2:24 LZMA:384k",
                "Solid = +",
                "Blocks = 2",
                "Sub items Errors: 1",
                "Archives with Errors: 1",
                "Sub items Errors: 1",
                @"ERROR: Can not open output file : The process cannot access the file because it is being used by another process. : D:\_local_temp\etw_analyser\temp2\7ZipLog.txt"
            };
            Assert.True(EtlZipCommand.HasSingleFileError(lines1));
        }



        private static void Unzip_And_Check_Link(ITempOutput tmp)
        {
            var extractor = new EtlZipCommand();
            string symDir = Path.Combine(tmp.Name, "Symbols");

            extractor.Unzip(TestData.ZipWithTwoFiles, tmp.Name, new SymbolPaths { SymbolFolder = symDir });

            string symlinkDir = Path.Combine(symDir, Path.GetFileNameWithoutExtension(TestData.ZipWithTwoFiles));
            Assert.True(Directory.Exists(symlinkDir), $"Symbolic link directory should exist {symlinkDir}");
        }



        [Fact]
        public void ThrowArgumentEx_When_Path_Is_Null()
        {
            ExceptionAssert.Throws<ArgumentException>(() => new EtlZipCommand().Unzip(null, null, null), "pathinputFile");
        }

        [Fact]
        public void ThrowArgumentEx_When_Path_Is_Empty()
        {
            ExceptionAssert.Throws<ArgumentException>(() => new EtlZipCommand().Unzip("", null, null), "pathinputFile");
        }

        [Fact]
        public void ThrowIOException_On_NotExistingFile()
        {
            ExceptionAssert.Throws<InvalidDataException>(() => new EtlZipCommand().Unzip("NotExistingFile.7z", null, new SymbolPaths()));
        }

        [Fact]
        public void ThrowArgumentException_On_Null_SymbolFolder()
        {
            ExceptionAssert.Throws<ArgumentException>(() => new EtlZipCommand().Unzip("NotExistingFile.7z", null, null), "symbols");
        }
    }
}
