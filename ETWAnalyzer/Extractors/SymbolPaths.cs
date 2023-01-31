//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Contains local Symbol folder which is used to create Symlinks from .NGENPDB folder and
    /// an option remote symbol server which can point to e.g. the Microsoft symbol server.
    /// </summary>
    public class SymbolPaths
    {
        internal const string NT_SYMBOLPATH = "_NT_SYMBOL_PATH";

        /// <summary>
        /// Symbols Cache folder which is used when pdbs are loaded. Into this folder are pdbs converted into a different format which 
        /// can be much faster loaded. 
        /// </summary>
        public string SymCacheFolder
        {
            get; set;
        } = "C:\\SymCache";

        /// <summary>
        /// When an ETL is unzipped we create a symlink from SymbolFolder\xxx => C:\temp\extract\xxx.etl.NGENPDB
        /// to shorten the path names
        /// </summary>
        public string SymbolFolder
        {
            get; set;
        } = "";

        /// <summary>
        /// Remote Symbol Server is read from App.Config and its value is set during startup here.
        /// </summary>
        public string RemoteSymbolServer
        {
            get; set;
        } = "";

        /// <summary>
        /// When we have created shortcuts delete them to not clutter up the symbol folder too much.
        /// </summary>
        List<string> myShortCuts = new();

        internal const string NgenPdbExtension = ".NGENPDB";
        internal const string EmbeddedPdbExtension = ".EmbeddedPdbs";

        /// <summary>
        /// Currently two directories besides the etl file are created
        /// </summary>
        static readonly string[] PdbExtensions = new string[] { NgenPdbExtension, EmbeddedPdbExtension };

        /// <summary>
        /// Despite being long path aware the underlying symcache.dll is still compiled with the 250 MAX_PATH limit.
        /// to work around we still need shortcuts, but these can only be created as Administrator, so this will work now all 
        /// the time if you are extracting data as Administrator
        /// </summary>
        internal void RemoveShortCuts()
        {
            foreach(string shortcut in myShortCuts)
            {
                if (Directory.Exists(shortcut))
                {
                    Directory.Delete(shortcut);
                }
            }

            myShortCuts.Clear();
        }

        /// <summary>
        /// Combine the local symbol folder which acts normally as symbol file cache from the remote symbol server
        /// and a local folder which is usually the NGENPDB folder from a shortcut directory link to keep symbol file names below the MAX_PATH limit.
        /// </summary>
        /// <param name="etlFile">Existing ETL file.</param>
        /// <returns>Combined symbol server folder for .NET Ngenpdb folder and EmbeddedPDBs folder.</returns>
        public string GetCombinedSymbolPath(string etlFile)
        {
            string shortSymPathInsertions = "";

            foreach (var extension in PdbExtensions)
            {
                string longPdbFolder = GetLongSymbolFolderForEtl(etlFile, extension);

                if (Directory.Exists(longPdbFolder) )
                {
                    try
                    {
                        CreateSymLinkToSymbolFolder(etlFile, extension);
                    }
                    catch (Exception)
                    {
                    }

                    string shortPdbFolder = GetShortSymbolFolderForEtl(etlFile, extension);
                    if (!Directory.Exists(shortPdbFolder))
                    {
                        Logger.Warn($"Symbolic link directory does not exist {shortPdbFolder} fallback to long path name.");
                        shortPdbFolder = longPdbFolder;
                    }
                    else
                    {
                        Logger.Info($"Short folder name found at: {shortPdbFolder} for file {etlFile}");
                    }

                    shortSymPathInsertions += $"SRV*{shortPdbFolder};";
                }
            }
           

            // We return first SRV*{SymbolFolder} because that folder is used as download folder by TraceEvent for remotely downloaded symbols
            // Otherwise we would download the pdbs to the NGenPDB folder of the ETL which is not what we want
            return $"SRV*{SymbolFolder};{shortSymPathInsertions}{RemoteSymbolServer}";
        }

        /// <summary>
        /// Create a folder name which is beneath the <see cref="SymbolFolder"/> with a directory name 
        /// of the ETL file like SymbolFolder\#_dddd where ddd is the hash code of the combined string of input etl and extension
        /// </summary>
        /// <param name="etlFile">Full path to input etl file</param>
        /// <param name="extension">Extension folder name</param>
        /// <returns>Short Symbol folder name for given ETL file</returns>
        public string GetShortSymbolFolderForEtl(string etlFile, string extension)
        {
            return Path.Combine(SymbolFolder, "#_"+ Math.Abs((etlFile+extension).GetHashCode()));
        }

        /// <summary>
        /// Get corresponding folder for native image pdbs besides the ETL file
        /// </summary>
        /// <param name="etlFile"></param>
        /// <param name="extension">File extension which is a pdb directory</param>
        /// <returns></returns>
        public string GetLongSymbolFolderForEtl(string etlFile, string extension)
        {
            if( string.IsNullOrEmpty(etlFile))
            {
                return "";
            }
            return Path.GetFullPath(etlFile) + extension;
        }

        /// <summary>
        /// Get first remote symbol server from NT_SYMBOL_PATH environment variable
        /// </summary>
        /// <returns></returns>
        public static string GetRemoteSymbolServerFromNTSymbolPath()
        {
            string env = Environment.GetEnvironmentVariable(NT_SYMBOLPATH);
            return env ?? "";
        }

        /// <summary>
        /// Create in symbol download folder a shortcut to the NGENPDB folder for etl file
        /// </summary>
        /// <param name="etlFile"></param>
        /// <param name="extension"></param>
        /// <exception cref="UnauthorizedAccessException"></exception>
        internal void CreateSymLinkToSymbolFolder(string etlFile, string extension)
        {
            
            // convention is that besides the xxx.etl a folder named xxx.ETL.NGENPDB exists which contains
            // the managed pdbs.
            string pdbFolder = etlFile + extension;

            if (!Directory.Exists(pdbFolder))
            {
                // ETL does not contain e.g. .NGENPDB folder 
                // Strange because we normally always have a NGenPDB folder in an ETL file
                Logger.Info($"ETL file does not have a {extension} folder for {etlFile}");
                return;
            }

            string shortcutFolder = GetShortSymbolFolderForEtl(etlFile, extension);

            if (Directory.Exists(shortcutFolder))
            {
                Logger.Info($"Link already exists. No need to create {shortcutFolder}");
                // Nothing to do. Link was already created.
                return;
            }

            // ensure symbol folder exists.
            Directory.CreateDirectory(Path.GetDirectoryName(shortcutFolder));

            Logger.Info($"Create Symbol link from {shortcutFolder} ==> {pdbFolder}");
            bool linkState = CreateSymbolicLink(shortcutFolder, pdbFolder, LinkFlags.TargetIsDirectory);
            
            if (linkState == false)
            {
                throw new UnauthorizedAccessException($"Could not create directory link {shortcutFolder} => {pdbFolder}.{Environment.NewLine}You have to run this application with Administrator rights.", new Win32Exception());
            }

            myShortCuts.Add(shortcutFolder);
        }

        enum LinkFlags
        {
            TargetIsFile = 0,
            TargetIsDirectory = 1,
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, LinkFlags dwFlags);

    }
}
