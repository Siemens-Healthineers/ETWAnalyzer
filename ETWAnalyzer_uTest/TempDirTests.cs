//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Helper;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer_uTest
{
    
    public class TempDirTests
    {
        [Fact]
        public void Ensure_Directory_IsKept_When_Test_Fails()
        {
            string dirName = null;
            try
            {
                using var tmp = TempDir.Create();
                dirName = tmp.Name;
                Assert.True(Directory.Exists(dirName), $"Directory {dirName} was not created.");
                throw new Exception("Test Exception");
            }
            catch (Exception)
            {
                Assert.True(Directory.Exists(dirName), $"Directory {dirName} was not deleted");
            }
        }

        [Fact]
        public void TempDir_Is_Deleted_After_Dispose_And_Contained_SubDirs()
        {
            string textFileName = null;

            string expectedTargetDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), @"ETWAnalyzer\" + nameof(TempDir_Is_Deleted_After_Dispose_And_Contained_SubDirs));

            // If it already exists remove it. Due to previous test which checks if directory stays when it is left with an exception
            if (Directory.Exists(expectedTargetDir))
            {
                Directory.Delete(expectedTargetDir, true);
            }

            using (var tmp = TempDir.Create())
            {
                Assert.Equal(Path.GetFullPath(expectedTargetDir), tmp.Name);
                Assert.True(Directory.Exists(expectedTargetDir));
                string subDir = Path.Combine(tmp.Name, "Subdir1", "SubDir2");
                Directory.CreateDirectory(subDir);
                textFileName = Path.Combine(subDir, "TestFile.txt");
                File.WriteAllText(textFileName, "This is a test file");
            }

            // Check if file is deleted recursively after disposing TempDir when no exception happens
            Assert.False(File.Exists(textFileName), $"File {textFileName} should have been deleted after TempDir was disposed.");
        }
    }
}
