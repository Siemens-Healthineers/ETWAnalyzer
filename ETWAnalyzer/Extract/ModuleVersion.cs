//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Version identifier to identify a module build by its TFS name and used version.
    /// The Module is determined by the mapping of modules specific dll names (see Configuration\DllToBuildMap.json) 
    /// for the marker files. 
    /// </summary>
    public class ModuleVersion : IEquatable<ModuleVersion>
    {
        /// <summary>
        /// Module version of the form Major.Minor.yymm.ddbb where Major is the main version and 
        /// Minor is the patch version. yy year, mm month, dd day, bb build number starting with 1.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Rough TFS path to module
        /// </summary>
        public string Module { get; set; }

        /// <summary>
        /// File which was used to retrieve the module version with full file path 
        /// </summary>
        public string ModuleFile { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Version } {Module}";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>

        public override bool Equals(object obj)
        {
            return Equals(obj as ModuleVersion);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="versionA"></param>
        /// <param name="versionB"></param>
        /// <returns></returns>
        public static bool AreEqual(ModuleVersion versionA, ModuleVersion versionB)
        {
            if (versionA is null)
            {
                throw new ArgumentNullException(nameof(versionA));
            }

            if (versionB is null)
            {
                throw new ArgumentNullException(nameof(versionB));
            }

            return versionA.Module == versionB.Module &&
                    versionA.ModuleFile == versionB.ModuleFile &&
                    versionA.Version == versionB.Version;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ModuleVersion other)
        {
            if(other is null)
            {
                return false;
            }

            return Version == other.Version &&
                   Module == other.Module &&
                   ModuleFile == other.ModuleFile;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
