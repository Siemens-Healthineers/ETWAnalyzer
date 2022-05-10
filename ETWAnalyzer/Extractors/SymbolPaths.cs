//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// When an ETL is unzipped we create a symlink from C:\temp\extract\xxx.etl.NGENPDB => SymbolFolder\xxx.ETL 
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
        /// Combine the local symbol folder which acts normally as symbol file cache from the remote symbol server
        /// and a local folder which is usually the NGENPDB folder
        /// </summary>
        /// <param name="etlFile">Existing ETL file.</param>
        /// <returns>Combined symbol server folder.</returns>
        public string GetCombinedSymbolPath(string etlFile)
        {
            string folderName = GetShortSymbolFolderForEtl(etlFile);

            // if link was not created for some reason fallback to long folder name besides ETL file
            if( !Directory.Exists(folderName) )
            {
                Logger.Warn($"Symbolic link directory does not exist {folderName} fallback to long path name.");
                folderName = GetLongSymbolFolderForEtl(etlFile);
            }

            // We return first SRV*{SymbolFolder} because that folder is used as download folder by TraceEvent for remotely downloaded symbols
            // Otherwise we would download the pdbs to the NGenPDB folder of the ETL which is not what we want
            return $"SRV*{SymbolFolder};SRV*{folderName};{RemoteSymbolServer}";
        }

        /// <summary>
        /// Create a folder name which is beneath the <see cref="SymbolFolder"/> with a directory name 
        /// of the ETL file like SymbolFolder\etlFileName without etl or other extensions.
        /// </summary>
        /// <param name="etlFile"></param>
        /// <returns>Short Symbol folder name for given ETL file</returns>
        public string GetShortSymbolFolderForEtl(string etlFile)
        {
            return Path.Combine(SymbolFolder, Path.GetFileNameWithoutExtension(etlFile));
        }

        /// <summary>
        /// Get corresponding folder for native image pdbs besides the ETL file
        /// </summary>
        /// <param name="etlFile"></param>
        /// <returns></returns>
        public string GetLongSymbolFolderForEtl(string etlFile)
        {
            return Path.GetFullPath(etlFile) + ".NGENPDB";
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
    }
}
