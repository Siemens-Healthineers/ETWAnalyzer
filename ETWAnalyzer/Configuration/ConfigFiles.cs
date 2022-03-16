//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Configuration
{
    /// <summary>
    /// Configuration files which are placed besides the ETWAnalyzer executable in a sub directory which are used by various analyzers/extractors
    /// </summary>
    static class ConfigFiles
    {
        /// <summary>
        /// Executable Directory
        /// </summary>
        public static readonly string ExeDirectory = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// Configuration directory residing besides the ETWAnalyzer executable
        /// </summary>
        const string ConfigDir = "Configuration";

        /// <summary>
        /// ETWAnalyzer full qualified path name
        /// </summary>
        public static string ETWAnalyzerExe
        {
            get
            {
                return Path.Combine(ExeDirectory, "ETWAnalyzer.exe");
            }
        }


        /// <summary>
        /// User defined stacktags which can be used e.g. to get of specific methods CPU and wait times trending data to find out
        /// when something has become faster or slower in a large collection of ETL files. 
        /// </summary>
        public static string SpecialStackTagFile
        {
            get
            {
                return Path.Combine(ExeDirectory, ConfigDir, "Special.stacktags");
            }
        }
        /// <summary>
        /// Normal default.stacktags file which is used to analyze issues
        /// </summary>
        public static string DefaultStackTagFile
        {
            get
            {
                return Path.Combine(ExeDirectory, ConfigDir, "default.stacktags");
            }
        }

        /// <summary>
        /// Stacktag file for GC and JIT events only
        /// </summary>
        public static string GCJitStacktagFile
        {
            get
            {
                return Path.Combine(ExeDirectory, ConfigDir, "GCAndJit.stacktags");
            }
        }

        /// <summary>
        /// Flat list of pdb file names which need to be present when symbols are loaded
        /// </summary>
        public static string RequiredPDBs
        {
            get
            {
                return Path.Combine(ExeDirectory, ConfigDir, "RequiredPdbs.txt");
            }
        }

        /// <summary>
        /// XML file process rename functionality
        /// </summary>
        public static string ProcessRenameRules
        {
            get
            {
                return Path.Combine(ExeDirectory, ConfigDir, "ProcessRenameRules.xml");
            }
        }

        /// <summary>
        /// XML file exception filtering rules
        /// </summary>
        public static string ExceptionFilteringRules
        {
            get
            {
                return Path.Combine(ExeDirectory, ConfigDir, "ExceptionFilters.xml");
            }
        }

        /// <summary>
        /// Json file with an enhanced list of https://docs.microsoft.com/en-us/windows-hardware/drivers/ifs/allocated-altitudes#400000---409999-fsfilter-top
        /// which contains the pretty much complete list of Antivirus and other MiniFilters
        /// </summary>
        public static string WellKnownDriverFiles
        {
            get
            {
                return Path.Combine(ExeDirectory, ConfigDir, "WellKnownDrivers.json");
            }
        }

        /// <summary>
        /// Json file contains a list of dlls and their module name or TFS source control path
        /// These files are used to determine the module build version of a given baseline
        /// </summary>
        public static string DllToBuildMapFile
        {
            get
            {
                return Path.Combine(ExeDirectory, ConfigDir, "DllToBuildMap.json");
            }
        }

        /// <summary>
        /// This xml file defines all Testcases / SingleTests which should occur in each TestRun
        /// </summary>
        public static string ExpectedTestsInTestRun
        {
            get{ return Testability_AlternateExpectedTestConfigFile ?? Path.Combine(ExeDirectory, ConfigDir, "TestRunConfiguration.xml"); }
        }

        static internal string Testability_AlternateExpectedTestConfigFile;

    }
}
