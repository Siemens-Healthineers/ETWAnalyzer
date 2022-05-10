//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Threading;
using System.IO;
using System.Linq;

namespace ETWAnalyzer
{
    /// <summary>
    /// Can delete directories which contain files which are over the MAX_PATH limit of 250 characters. Additionally it 
    /// contains a safeguard to never delete directories which a directory depth less than 6 to prevent accidental deletion 
    /// of the complete hard disk due to wrong or missing user input.
    /// </summary>
    class Deleter
    {

        internal static void Debug(string fmt, params object[] args)
        {
            if (Program.DebugOutput)
            {
                Console.WriteLine(fmt, args);
            }
        }

        /// <summary>
        /// The \\?\ file name prefix allows up to 32K long file names since Windows 7.
        /// .NET supports the "old" \\?\ prefix for long path since .NET 4.6.2. See https://blogs.msdn.microsoft.com/jeremykuhne/2016/07/30/net-4-6-2-and-long-paths-on-windows-10/
        /// With Windows 10 Anniversary Update Long path support was added as opt in switch which is disabled by default.
        /// See https://docs.microsoft.com/fr-ca/windows/desktop/FileIO/naming-a-file#maximum-path-length-limitation
        /// Unless that is enabled on all machines by default we will need to use this prefix and build for .NET >= 4.6.2
        /// </summary>
        internal const string LongPathPrefix = @"\\?\";

        /// <summary>
        /// Deletes all temp Files after creating a json
        /// </summary>
        /// <param name="folder">folder where the Temp files are</param>
        /// <param name="etlFile">Name of extracted etl file.</param>
        /// <param name="deleteETLFile">specifies the etl source must be deleted. If false only related files like etlx, .log files are deleted</param>
        internal static void DeleteTempFilesAfterExtracting(string folder, string etlFile, bool deleteETLFile)
        {
            const int dirDepth = 1;

            string pathWithoutExtension = folder + "\\" +  Path.GetFileNameWithoutExtension(etlFile);

            folder = LongPathPrefix + folder;

            if (!Directory.Exists(folder))
            {
                throw new DirectoryNotFoundException($"Directory '{folder}' which should be deleted does not exist!");
            }
            if (FolderDepth(folder) < dirDepth)
            {
                throw new ArgumentException($"Directory name {folder} is too short. Required folder depths was {FolderDepth(folder)} but it needs to be < {dirDepth}!");
            }

            Debug("Temp files in Directory {0} are deleted", folder);

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if(deleteETLFile)// when file was extracted
                    {
                        if( File.Exists(etlFile))
                        {
                            File.Delete(etlFile);
                        }

                        // besides the xxx.etl file related directories can exist which we also need to remove
                        string[] relatedDirs = new string[]
                        {
                            ".NGENPDB",
                            ".EmbeddedPdbs",
                            ".Screenshots",
                            ".PayLoad",
                        };

                        foreach(var relatedDir in relatedDirs)
                        {
                            string fullDir = LongPathPrefix + etlFile + relatedDir;
                            if( Directory.Exists(fullDir))
                            {
                                Directory.Delete(fullDir, true);
                            }
                        }

                        string shortcutDir = Path.Combine("C:\\Symbols", Path.GetFileNameWithoutExtension(etlFile));   // remove symbolic link which is created during extraction
                        if( Directory.Exists(shortcutDir))
                        {
                            Directory.Delete(shortcutDir);
                        }


                        string screenshotBaseFileName = Path.GetFileNameWithoutExtension(etlFile);
                        string[] screenshots = Directory.GetFiles(Path.GetDirectoryName(etlFile), "*.png")
                                                       .Where(x =>
                                                                     Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(x))   // Remove of file the png and then the 7z1/7z2 file extension
                                                                     .Equals(screenshotBaseFileName, StringComparison.InvariantCultureIgnoreCase)).ToArray();
                        foreach(var screenshotFile in screenshots)
                        {
                            File.Delete(screenshotFile);
                        }
                    }

                    string[] relatedExternalFiles = new string[]
                    {
                        etlFile +".log", // log file from tracing
                        etlFile + "x",  // .etlx file from TraceEvent library
                    };

                    foreach (var deleteCandidate in relatedExternalFiles)
                    {
                        if( File.Exists(deleteCandidate))
                        {
                            File.Delete(deleteCandidate);
                        }
                    }
                    
                    break;
                }
                catch (IOException e)
                {
                    if (i == 2)
                    {
                        throw new IOException($"Deleting directory {folder} failed: " + e.Message);
                    }
                    else
                    {
                        Thread.Sleep(200);
                    }
                }
            }
        }


        /// <summary>
        /// Delete recursively temp directory
        /// </summary>
        static public void DeleteDirectory(string dir)
        {
            const int dirDepth = 6;

            if (String.IsNullOrEmpty(dir))
            {
                throw new ArgumentException("Directory to delete is null or empty!");
            }

            dir = LongPathPrefix + dir;

            if (!Directory.Exists(dir))
            {
                throw new DirectoryNotFoundException("Directory to delete does not exist!");
            }
            if (FolderDepth(dir) <= dirDepth)
            {
                throw new ArgumentException($"Directory name {dir} is too short!");
            }

            Debug("Directory {0} is about to be deleted", dir);

            const int RetryCount = 3;

            for (int i = 0; i < RetryCount; i++)
            {
                try
                {
                    Directory.Delete(dir, true);
                    
                    Debug($"Directory {dir} was deleted");
                    break;
                }
                catch (Exception e)
                {
                    if (i == 2)
                    {
                        throw new IOException($"Deleting directory {dir} failed: " + e.Message);
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            }
        }

        /// <summary>
        /// Get folder depth.
        /// C:\ => 0 
        /// C:  => 0 
        /// C:\temp => 1
        /// C:\temp\SubDir1 => 2 
        /// \\?\C:\temp\SubDir1 => 2 ...
        /// </summary>
        /// <param name="path">Folder path name</param>
        /// <returns>integer starting at 0 for folder depth</returns>
        internal static int FolderDepth(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                return -1;
            }

            int startidx = path.IndexOf(LongPathPrefix, StringComparison.Ordinal); // check if string contains \\?\ prefix
            if (startidx == 0) // did start with then skip prefix
            {
                startidx += LongPathPrefix.Length;
            }

            if ( startidx == -1 ) // if not found start at 1 because the for loop looks always at i-1 
            {
                startidx = 1;
            }

            int depth = 0;
            for(int i=startidx;i< path.Length-1;i++)
            {
                // look at i-1,i,i+1 to not count \\\\\ each \ as depth level for e.g. UNC path names.
                if (path[i] == '\\' && path[i-1] != '\\' && path[i+1] != '\\' ) 
                {
                    depth++;
                }
            }

            return depth;
        }

    }
}
