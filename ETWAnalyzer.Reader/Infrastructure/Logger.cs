//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ETWAnalyzer
{
    /// <summary>
    /// Cross process logging logger
    /// </summary>
    class Logger : IDisposable
    {
        const string ApplicationName = "ETWAnalyzer";

        public static readonly Logger Instance = new Logger(ApplicationName + "_Trace.log");

        readonly long myMaxFileSizeInBytes;
        readonly int myMaxGenerationsToKeep;
        readonly string myLoggingDirectory;
        readonly string myBaseFileName;
        readonly int myPid = Process.GetCurrentProcess().Id;

        FileStream myFile;
        private string myInitialFileName;
        StreamWriter myLog;
        Mutex myGuard;

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint GetFinalPathNameByHandle(SafeFileHandle hFile, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpszFilePath, uint cchFilePath, FinalPathFlags dwFlags);
        [Flags]
        public enum FinalPathFlags : uint
        {
            FILE_NAME_NORMALIZED = 0x0,
            VOLUME_NAME_GUID = 0x1,
            VOLUME_NAME_NT = 0x2,
            VOLUME_NAME_NONE = 0x4,
            FILE_NAME_OPENED = 0x8
        }

        internal Action<string> Delete = File.Delete;

        /// <summary>
        /// Create logger with fixed file name
        /// </summary>
        public Logger(string baseFileName, long maxFileSizeInMB=30, int maxGenerationsToKeep=4)
        {
            myBaseFileName = baseFileName;
            myMaxFileSizeInBytes = maxFileSizeInMB * 1024 * 1024L;
            myMaxGenerationsToKeep = maxGenerationsToKeep;
            myLoggingDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            myGuard = new Mutex(false, "LoggerMutex");
            ReopenFile();
        }


        /// <summary>
        /// Write an info message
        /// </summary>
        /// <param name="message"></param>
        public static void Info(string message)
        {
            Instance.Write("Info:" + message);
        }

        /// <summary>
        /// Write a warning message
        /// </summary>
        /// <param name="message"></param>
        public static void Warn(string message)
        {
            Instance.Write("Warn: " + message);
        }

        /// <summary>
        /// Write an error message
        /// </summary>
        /// <param name="message"></param>
        public static void Error(string message)
        {
            Instance.Write("Error: " + message);
        }


        string GetPrefix()
        {
            string time = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
            return $"{time} {myPid} # ";
        }

        internal void Write(string message)
        {

            try
            {
                try
                {
                    myGuard.WaitOne();
                }
                catch (AbandonedMutexException) // some process did exit and did leave the mutex orphaned. We still got ownership of it
                {
                }

                myFile.Seek(0, SeekOrigin.End);
                if (myFile.Position > myMaxFileSizeInBytes)
                {
                    string currentFileName = GetCurrentFileName(myFile.SafeFileHandle);
                    if (myInitialFileName != currentFileName)
                    {
                        // When file was already renamed we just need to close the current file and reopen the "old" base file name which is not small again
                        //Console.WriteLine($"Log file rename detected! Renamed from {myInitialFileName} to {currentFileName}");
                        ReopenFile();
                    }
                    else
                    {
                        RollOver();
                    }
                }
                myLog.WriteLine(GetPrefix()+message);
                myLog.Flush();
            }
            finally
            {
                myGuard.ReleaseMutex();
            }


        }

        static string GetCurrentFileName(SafeFileHandle handle)
        {
            StringBuilder sb = new StringBuilder(32000);
            if (GetFinalPathNameByHandle(handle, sb, 32000, FinalPathFlags.FILE_NAME_NORMALIZED) == 0)
            {
                return "";
            }
            else
                return sb.ToString();
        }

        /// <summary>
        /// Currently used log folder. In case of access problems the log files are located in the ProgramDataFolder
        /// </summary>
        public string LogFolder
        {
            get;
            private set;
        }

        private void ReopenFile()
        {
            LogFolder = myLoggingDirectory;
            string logFile = Path.Combine(myLoggingDirectory, myBaseFileName);
            if (myFile != null)
            {
                myFile.Dispose();
                myFile = null;
            }

            for (int i = 0; i < 2; i++)
            {
                try
                {
                    // we write to this file but other processes can read and write to it.
                    // To support renaming an open file we need to add FileShare.Delete to be able to move the file while it is still open
                    myFile = new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                    myInitialFileName = GetCurrentFileName(myFile.SafeFileHandle);
                    break;
                }
                catch(UnauthorizedAccessException)
                {
                    // In case location is accessible use ProgramData folder 
                    string programmDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ApplicationName);
                    LogFolder = programmDataFolder;
                    if ( !Directory.Exists(programmDataFolder))
                    {
                        Directory.CreateDirectory(programmDataFolder);
                    }
                    logFile = Path.Combine(programmDataFolder, myBaseFileName);
                }
                catch (IOException)
                {
                    logFile = Path.Combine(Path.GetDirectoryName(logFile), $"{Path.GetFileNameWithoutExtension(myBaseFileName)}_{DateTime.Now.ToString("hh_MM_ss.fff", CultureInfo.InvariantCulture)}{Path.GetExtension(myBaseFileName)}");
                }
            }

            myLog = new StreamWriter(myFile);

        }

        internal void RollOver()
        {
            string fileName = myFile.Name;
            myLog.Close();
            myLog = null;
            string dir = Path.GetDirectoryName(fileName);
            string ext = Path.GetExtension(fileName);
            string basename = Path.GetFileNameWithoutExtension(fileName);
            string newFileName = Path.Combine(dir, $"{basename}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff", CultureInfo.InvariantCulture)}{ext}");
            File.Move(fileName, newFileName);
            string[] files = Directory.GetFiles(myLoggingDirectory, $"{Path.GetFileNameWithoutExtension(myBaseFileName)}*{Path.GetExtension(myBaseFileName)}");
            string[] sortedByAgeDescending = files.Select(f => new FileInfo(f)).OrderByDescending(x => x.LastWriteTime).Select(x => x.FullName).ToArray();

            for (int i = myMaxGenerationsToKeep; i < sortedByAgeDescending.Length; i++)
            {
                try
                {
                    Delete(sortedByAgeDescending[i]);
                }
                catch (Exception)
                {

                }
            }

            ReopenFile();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            myFile?.Dispose();
            myFile = null;

            myGuard?.Dispose();
            myGuard = null;

            myLog?.Dispose();
            myLog = null;
        }
    }
}
