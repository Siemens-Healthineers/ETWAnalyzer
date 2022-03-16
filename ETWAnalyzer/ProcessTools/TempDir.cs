//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using ETWAnalyzer;
using System.Linq;
using System.Reflection;

namespace ETWAnalyzer.Helper
{
    /// <summary>
    /// Base interface to create scratch folders which are delete upon dispose and all its contents.
    /// </summary>
    public interface ITempOutput : IDisposable
    {
        /// <summary>
        /// Get full path to scratch folder which is deleted when the test is exited without an exception.
        /// </summary>
        string Name
        {
            get;
        }
    }


    class OutputDirectory
    {
        public string fullPathName;
        public OutputDirectory(string tempPathDir)
        {
            FullPathName = tempPathDir;
        }

        public string FullPathName
        {
            get
            {
                return fullPathName;
            }

            set
            {
                Environment.SetEnvironmentVariable("TEMP",
                    Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\AppData\Local\Temp"),
                    EnvironmentVariableTarget.Process);
                fullPathName = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), value);
            }
        }

        public bool InException { get; set; }

        public void Create()
        {
            try
            {
                Directory.CreateDirectory(FullPathName);
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.Message);
            }
        }

        public void Delete()
        {
            try
            {
                Directory.Delete(@"\\?\" + FullPathName, true);
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.Message);
            }
        }
    }

    /// <summary>
    /// Implementation of ITempDir
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
    public class TempDir : ITempOutput
    {
        /// <summary>
        /// Cleanup old temp folders only after 100 tests to keep file based 
        /// tests fast
        /// </summary>
        static int DisposeCount;

        /// <summary>
        /// Get full path to scratch folder which is deleted when the test is exited without an exception.
        /// </summary>
        public string Name
        {
            get { return OutDir.FullPathName; }
        }

        OutputDirectory OutDir { get; set; }


        /// <summary>
        /// Create a sub directory in the temp folder which is unique and empty
        /// </summary>
        /// <returns></returns>
        public static ITempOutput Create(string subDirInTempDir)
        {
            return new TempDir(subDirInTempDir);
        }

        /// <summary>
        /// Copy a source directory to a destination directory recursively.
        /// </summary>
        /// <param name="sourcePath">Source directory path from which all files are copied</param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public static ITempOutput CopyDir(string sourcePath, string destinationPath)
        {
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));
            }
            foreach (string newPath in Directory.GetFiles(sourcePath, ".", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);
            }

            return new TempDir() { OutDir = new OutputDirectory(destinationPath) };
        }
        private TempDir()
        {
        }

        /// <summary>
        /// Create a temp directory named after your test in the %temp%\uTest\xxx directory
        /// which is deleted and all sub directories when the ITempDir object is disposed.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ITempOutput Create()
        {
            var stack = new StackTrace(1);
            var sf = stack.GetFrame(0);
            return new TempDir(sf.GetMethod().Name);
        }

        /// <summary>
        /// Needed to enable long path handling in testhost process during unit tests to prevent failing tests which need Long Path suport
        /// </summary>
        public static void EnableLongPathHandling()
        {
            // Enable long path support even when hosting executable has not enabled it via app.config 
            var type = Type.GetType("System.AppContextSwitches");
            if (type != null) // might be not present in .NET Core
            {
                var useLegacyPathHandlingSwitch = type.GetField("_useLegacyPathHandling", BindingFlags.NonPublic | BindingFlags.Static);
                var blockLongPaths = type.GetField("_blockLongPaths", BindingFlags.NonPublic | BindingFlags.Static);
                useLegacyPathHandlingSwitch.SetValue(null, -1);
                blockLongPaths.SetValue(null, -1);
            }
        }

        /// <summary>
        /// Create a scratch directory below the output directory.
        /// </summary>
        /// <param name="dirName">Name of scratch subfolder. It is stored below TAUFolder\uTests\dirName</param>
        public TempDir(string dirName)
        {
            EnableLongPathHandling();

            if (String.IsNullOrEmpty(dirName))
            {
                throw new ArgumentException(nameof(dirName));
            }
            OutDir = new OutputDirectory(Path.Combine("ETWAnalyzer", dirName));
            if (Directory.Exists(OutDir.FullPathName)) // add a unique number to ensure that test directories are isolated
            {
                OutDir = new OutputDirectory(Path.Combine("ETWAnalyzer", dirName + "_" + DateTime.Now.Ticks));
            }
            OutDir.Create();
        }





        /// <summary>
        /// Delete directory on dispose if no exception is currently thrown.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
        public void Dispose()
        {
            // Delete scratch folder only if the test is not exited with an exception. Otherwise we keep the data there.
            bool inException = ExceptionHelper.InException; // true if method is left with an exception

            if (!inException)
            {
                if (Name.Length < 10)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Directory name {0} seems to be invalid. Do not delete recursively your hard disc.", Name));
                }
                OutDir.Delete();
            }

            DisposeCount++;
            // only cleanup after each 100 created temp directories or if tempdir is left over due to an exception
            // in that case trigger cleanup logic every time.
            if (DisposeCount % 50 == 0 || inException)  
            {
                GarbageCollectOldTestFolders();
            }
        }

        /// <summary>
        /// Remove old temp folders from tests which are older than 1h
        /// </summary>
        private void GarbageCollectOldTestFolders()
        {
            string rootDir = Path.GetDirectoryName(OutDir.FullPathName);
            string[] dataFromTestsOlder1h = Directory.GetDirectories(rootDir).Select(x => new FileInfo(x))
                                                                             .Where(writeTime => DateTime.Now - writeTime.LastWriteTime > TimeSpan.FromHours(1))
                                                                             .Select(x => x.FullName)
                                                                             .ToArray();
            foreach (var old in dataFromTestsOlder1h)
            {
                try
                {
                    // delete directories which contain long path names
                    Deleter.DeleteDirectory(old);
                }
                catch (Exception)
                {
                    Console.WriteLine("Could not delete temp test folder folder {old}");
                }
            }
        }
    }
}
