//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Helper;
using System;
using System.IO;
using Xunit;

namespace ETWAnalyzer_uTest
{

    public class DeleterTests
    {
        [Fact]
        public void Can_Delete_Directory_Long_Path()
        {
            using var tmp = TempDir.Create();
            string tooLongDir = Path.Combine(tmp.Name, @"This is a very long directory name\Which will finally trigger the max path limitation\OnWindowsssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssss\NowThatWasReallyLong");
            string tooLongDirPrefixedWithLongPath = Deleter.LongPathPrefix + tooLongDir;

            // Since .NET 4.6.2 .NET supports long path names with the prefix \\?\ in the file name which allows
            // files up to 32 K length
            try
            {
                Directory.CreateDirectory(tooLongDirPrefixedWithLongPath);
            }
            catch (PathTooLongException)
            {
                Assert.True(false, $"Change file C:\\Program Files(x86)\\Microsoft Visual Studio\\2019\\Enterprise\\Common7\\IDE\\Extensions\\TestPlatform\\testhost.exe.config " +
                                    "and testhost.x86.exe.config and add as FIRST AppContextSwitchOverride " + Environment.NewLine + "<AppContextSwitchOverrides value=\"Switch.System.IO.UseLegacyPathHandling = false; Switch.System.IO.BlockLongPaths = false\" />" + Environment.NewLine +
                                    " to enable long path handling. The test host process was compiled against .NET 4.0 as target runtime which has disabled long path support by default!");
            }

            Deleter.DeleteDirectory(tooLongDir);
        }

        const string TestDirectory = @"C:\temp\1\2\3\4";

        [Fact]
        public void NotPrefixed_Dir_Gets_Right_Folder_Depth()
        {
            Assert.Equal(5, Deleter.FolderDepth(TestDirectory));
        }

        [Fact]
        public void SimplePath_Works()
        {
            Assert.Equal(0, Deleter.FolderDepth("C:"));
        }

        [Fact]
        public void CurrentDirectory_Returns_Zero()
        {
            Assert.Equal(0, Deleter.FolderDepth(@"."));
        }


        [Fact]
        public void C_Colon_Returns_Zero()
        {
            Assert.Equal(0, Deleter.FolderDepth(@"C:"));
        }

        [Fact]
        public void C_Colon_Backslash_Returns_Zero()
        {
            Assert.Equal(0, Deleter.FolderDepth(@"C:\"));
        }


        [Fact]
        public void Prefixed_Dir_Gets_Right_Folder_Depth()
        {
            string dir = Deleter.LongPathPrefix + TestDirectory;
            Assert.Equal(5, Deleter.FolderDepth(dir));
        }


        [Fact]
        public void Delete_With_Null_Dir_Throws_Exception()
        {
            ExceptionAssert.Throws<ArgumentException>(() => Deleter.DeleteDirectory(null));
        }

        [Fact]
        public void Throw_Exception_Folder_Depth_Short()
        {
            string dir = @"C:\temp\CertainlyNotUsedDirectoryForNow";
            Directory.CreateDirectory(dir);

            ExceptionAssert.Throws<ArgumentException>(() => Deleter.DeleteDirectory(dir));

            Directory.Delete(dir);
        }

        [Fact]
        public void Delete_Not_Existing_Dir()
        {
            string dirPath = "C:\\Users\\z003rpcr\\ich_existiere_nicht";

            ExceptionAssert.Throws<DirectoryNotFoundException>(() => Deleter.DeleteDirectory(dirPath));
        }


        [Fact]
        public void Delete_Associated_Pngs()
        {
            using var tmp = TempDir.Create();
            string etlFileName = "CallupClaimColdReadingCT_14402msFO9DE01T0157PC.20200822-180009.etl";
            string png1 = "CallupClaimColdReadingCT_14402msFO9DE01T0157PC.20200822-180009.7z1.png";
            string png2 = "CallupClaimColdReadingCT_14402msFO9DE01T0157PC.20200822-180009.7z2.png";
            string otherPng = "CallupClaimColdReadingCT_14414msFO9DE01T0157PC.20200813-030000.7z1.png";

            string[] fileNames = new string[] { etlFileName, png1, png2, otherPng };

            foreach (var fileName in fileNames)
            {
                File.WriteAllText(Path.Combine(tmp.Name, fileName), "");
            }

            string etlFullPath = Path.Combine(tmp.Name, etlFileName);

            Deleter.DeleteTempFilesAfterExtracting(tmp.Name, etlFullPath, true);

            Assert.True(File.Exists(Path.Combine(tmp.Name, otherPng)));
            Assert.False(File.Exists(Path.Combine(tmp.Name, etlFileName)));
            Assert.False(File.Exists(Path.Combine(tmp.Name, png1)));
            Assert.False(File.Exists(Path.Combine(tmp.Name, png2)));
        }

        [Fact]
        public void Can_Delete_Directory_With_Content_And_SubDirectories_TempDir()
        {
            Program.DebugOutput = false;

            using var tmp = TempDir.Create();
            string etwRoot = Path.Combine(tmp.Name, "ETWAnalyzer");
            string deepDir = Path.Combine(etwRoot, "SubDirLevel1", "SubDirLevel2");
            Directory.CreateDirectory(deepDir);
            File.Create(Path.Combine(deepDir, "Files1.txt")).Dispose();
            File.Create(Path.Combine(deepDir, "..\\Files2.txt")).Dispose();

            Deleter.DeleteDirectory(etwRoot);

            Assert.False(Directory.Exists(etwRoot), $"Deleting failed. Directory {etwRoot} still exists.");
        }
    }
}
