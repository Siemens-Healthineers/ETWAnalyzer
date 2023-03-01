//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Commands;
using ETWAnalyzer.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ETWAnalyzer_uTest.Extract
{
    public class ConvertCommandTests
    {
        [Fact]
        public void Can_Convert_MultipleFiles()
        {
            var inFiles = new List<string>();
            using ITempOutput itemp_Dir = TempDir.Create();
            var files = new List<string>();
            var cmd = new ConvertCommand(new string[] { "-convert", "-fd",  $"{itemp_Dir.Name}\\*filex*etl", "-pid", "-1" });
            cmd.myConvertCallback = (fileName => 
            {
                files.Add(fileName);
            });
            string file1 = Path.Combine(itemp_Dir.Name, "filex1.etl");
            string file2 = Path.Combine(itemp_Dir.Name, "filex2.etl");
            string file3 = Path.Combine(itemp_Dir.Name, "dummy.etl");
            File.WriteAllText(file1, "Hello File1!");
            File.WriteAllText(file2, "Hello File2!");
            File.WriteAllText(file3, "Hello File3!");
            cmd.Parse();
            cmd.Run();
            Assert.Equal(2, files.Count);
            Assert.Contains(file1, files);
            Assert.Contains(file2, files);
        }


        [Fact]
        public void Can_Convert_Single_File()

        {
            var inFiles = new List<string>();
            using ITempOutput itemp_Dir = TempDir.Create();
            var files = new List<string>();
            var cmd = new ConvertCommand(new string[]
            {
                "-convert",
                "-fd",
                $"{itemp_Dir.Name}\\*filex*.etl",
                "-pid",
                "-1"
            });
            cmd.myConvertCallback = (fileName =>
            {
                files.Add(fileName);
            });
            string file1 = Path.Combine(itemp_Dir.Name, "filex1.etl");
            File.WriteAllText(file1, "Hello File1!");
            cmd.Parse();
            cmd.Run();
            Assert.Equal(file1, files[0]);
        }


        [Fact]
        public void No_File()
        {
            var inFiles = new List<string>();
            using ITempOutput itemp_Dir = TempDir.Create();
            var files = new List<string>();
            var cmd = new ConvertCommand(new string[]
            {
                "-convert",
                "-fd",
                $"{itemp_Dir.Name}\\*filex*.etl",
                "-pid",
                "-1"
            });
            cmd.myConvertCallback = (fileName =>
            {
                files.Add(fileName);
            });
            Assert.Throws<NotSupportedException>(() =>
            {
                cmd.Parse();
                cmd.Run();
            }
            );
            cmd.Run();
            Assert.Empty(files);
        }
    }
}
