//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extractors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Configuration
{
    /// <summary>
    /// Contains list of dll names which map to a source control path to get the baselines
    /// </summary>
    public class DllToBuildMappings
    {
        /// <summary>
        /// List of dlls to which module branch they belong. For each module we declare a dll which is built inside this module.
        /// </summary>
        public List<MarkerFile> MarkerFiles { get; set; } = new List<MarkerFile>();

        Dictionary<string,MarkerFile> myMarkerFilesLookup;

        /// <summary>
        /// Get module name for a given file (no path) case insensitive.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>null if no module was found or a module version string.</returns>
        public string GetModulePath(string fileName)
        {
            if( myMarkerFilesLookup == null )
            {
                myMarkerFilesLookup = new Dictionary<string, MarkerFile>(StringComparer.OrdinalIgnoreCase);
                foreach(MarkerFile markerFile in MarkerFiles)
                {
                    myMarkerFilesLookup.Add(markerFile.DllName, markerFile);
                }
            }

            string versionVector = null;
            if(myMarkerFilesLookup.TryGetValue(fileName, out MarkerFile file) )
            {
                versionVector = file.VersionVector;
            }
            return versionVector;
        }

        /// <summary>
        /// Serialize data
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="instance"></param>
        public static void Serialize(Stream stream,  DllToBuildMappings instance)
        {
            ExtractSerializer.Serialize(stream, instance);
        }

        /// <summary>
        /// Deserialize data
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static DllToBuildMappings Deserialize(Stream stream)
        {
            return ExtractSerializer.Deserialize<DllToBuildMappings>(stream);
        }
    }

    /// <summary>
    /// A dll which is representative for a given build project/module. That enables mapping of file versions
    /// to software builds when providing a patch
    /// </summary>
    public class MarkerFile
    {
        /// <summary>
        /// Name of dll
        /// </summary>
        public string DllName { get; set; }

        /// <summary>
        /// Module name
        /// </summary>
        public string VersionVector { get; set; }
    }
}
