//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Infrastructure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ETWAnalyzer.Extract.Modules
{
    /// <summary>
    /// Module Definition which contains file name, directory, File Version, Product Version, Product Name, Description and numeric FileVersion and 
    /// the list of processes which have this module loaded.
    /// </summary>
    public class ModuleDefinition : IEquatable<ModuleDefinition>
    {
        /// <summary>
        /// Contains indices to Strings array for 
        /// Directory, FileName, FileVersionStr, ProductVersionStr, Productname, FileVersion, Description, Processes
        /// The public setter is needed for Json.NET to de/serialize it.
        /// </summary>
        public string ModuleAndPid
        {
            get; set;
        }

        /// <summary>
        /// Used during extraction to prevent huge string reallocation costs to set <see cref="ModuleAndPid"/> only once.
        /// </summary>
        HashSet<ETWProcessIndex> myPidSet;

        /// <summary>
        /// Contains Index to unresolved pbd array which could not be loaded during extraction.
        /// </summary>
        public PdbIndex? PdbIdx { get; set; }

        /// <summary>
        /// ETWProcess Ids are prefixed in <see cref="ModuleAndPid"/> array with this character.
        /// </summary>
        const string PidPrefix = "#";

        /// <summary>
        /// Parsed integers from <see cref="ModuleAndPid"/> array for module description parts
        /// </summary>
        int[] myIndices;

        /// <summary>
        /// Module and pids are separated by spaces
        /// </summary>
        static readonly char[] IndexSeparators = new char[] { ' ' };

        const int MinIndices = 7;

        /// <summary>
        /// List of ETWProcessIndex values which indicate which processes have this module loaded.
        /// </summary>
        ETWProcessIndex[] ProcessIndicies
        {
            get; set;
        }

        /// <summary>
        /// Set during deserialization of ModuleDefinition list
        /// </summary>
        internal ModuleContainer Container { get; set; }


        ETWProcess[] myProcesses;

        /// <summary>
        /// Get Processes which have this module loaded.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyList<ETWProcess> Processes
        {
            get
            {
                if (myProcesses == null)
                {
                    GetIndices();
                    ETWProcess[] processes = new ETWProcess[ProcessIndicies.Length];
                    for (int i = 0; i < ProcessIndicies.Length; i++)
                    {
                        processes[i] = Container.Extract.GetProcess(ProcessIndicies[i]);
                    }
                    myProcesses = processes;
                }
                return myProcesses;
            }
        }

        /// <summary>
        /// Directory where <see cref="ModuleName"/> was loaded from 
        /// </summary>
        [JsonIgnore]
        public string ModulePath => Container.SharedStrings.GetStringByIndex(GetIndices()[0]);

        string myModuleName;
        /// <summary>
        /// File name without path of Module
        /// </summary>
        [JsonIgnore]
        public string ModuleName
        {
            get
            {
                if (myModuleName == null)
                {
                    myModuleName = Container.SharedStrings.GetStringByIndex(GetIndices()[1]);
                }
                return myModuleName;
            }
        }

        /// <summary>
        /// File Version which is a string in PE Header
        /// </summary>
        [JsonIgnore]
        public string FileVersionStr => Container.SharedStrings.GetStringByIndex(GetIndices()[2]);

        /// <summary>
        /// Product Version which is a string value in PE Header
        /// </summary>
        [JsonIgnore]
        public string ProductVersionStr => Container.SharedStrings.GetStringByIndex(GetIndices()[3]);

        /// <summary>
        /// ProductName of PE Header
        /// </summary>
        [JsonIgnore]
        public string ProductName => Container.SharedStrings.GetStringByIndex(GetIndices()[4]);

        /// <summary>
        /// File Version identifier
        /// </summary>
        [JsonIgnore]
        public Version Fileversion => new(Container.SharedStrings.GetStringByIndex(GetIndices()[5]) ?? "0.0.0.0");

        /// <summary>
        /// Description of module
        /// </summary>
        [JsonIgnore]
        public string Description => Container.SharedStrings.GetStringByIndex(GetIndices()[6]);



        int[] GetIndices()
        {
            if (myIndices == null)
            {
                string[] substrs = ModuleAndPid.Split(IndexSeparators);
                if (substrs.Length < MinIndices)
                {
                    throw new InvalidOperationException($"Expected at least {MinIndices} but got only {substrs.Length} from string >{ModuleAndPid}<");
                }

                List<int> indices = new();
                List<ETWProcessIndex> processIndicies = new();
                foreach (string str in substrs)
                {
                    if (str.StartsWith("#"))
                    {
                        string noPrefix = str.TrimStart('#');
                        processIndicies.Add((ETWProcessIndex)int.Parse(noPrefix));
                    }
                    else
                    {
                        indices.Add(int.Parse(str));
                    }
                }

                myIndices = indices.ToArray();
                ProcessIndicies = processIndicies.ToArray();
            }

            return myIndices;
        }

        /// <summary>
        /// Default ctor needed for deserialize
        /// </summary>
        public ModuleDefinition()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="container"></param>
        /// <param name="processIdx"></param>
        /// <param name="pdbIdx"></param>
        /// <param name="fullPath"></param>
        /// <param name="fileVersionStr"></param>
        /// <param name="productVersionStr"></param>
        /// <param name="productName"></param>
        /// <param name="fileVersion"></param>
        /// <param name="description"></param>
        public ModuleDefinition(ModuleContainer container, ETWProcessIndex processIdx, PdbIndex pdbIdx, string fullPath, string fileVersionStr, string productVersionStr, string productName, Version fileVersion, string description)
        {
            if (ModuleAndPid == null)
            {
                string dirName = Path.GetDirectoryName(fullPath);
                string fileName = Path.GetFileName(fullPath);
               
                UniqueStringList sharedStr = container.SharedStrings;

                int dirIdx = sharedStr.GetIndexForString(dirName);
                int fileIdx = sharedStr.GetIndexForString(fileName);
                int fileVersionStrIdx = sharedStr.GetIndexForString(fileVersionStr);
                int productVersionStrIdx = sharedStr.GetIndexForString(productVersionStr);
                int productNameIdx = sharedStr.GetIndexForString(productName);
                int fileVersionIdx = sharedStr.GetIndexForString(fileVersion?.ToString());
                int descriptionIdx = sharedStr.GetIndexForString(description);
                ModuleAndPid = $"{dirIdx} {fileIdx} {fileVersionStrIdx} {productVersionStrIdx} {productNameIdx} {fileVersionIdx} {descriptionIdx}";

                myModuleName = fileName;
            }

            PdbIdx = pdbIdx == Modules.PdbIndex.Invalid ? null : pdbIdx;
            AddPid(processIdx);
        }



        internal void AddPid(ETWProcessIndex processIdx)
        {
            if(myPidSet == null )
            {
                myPidSet = new HashSet<ETWProcessIndex>();
            }
            myPidSet.Add(processIdx);

            //ModuleAndPid += $" {PidPrefix}{processIdx}";

            // if these were set accidentally during debugging we reset them so later
            // we can view the always up to date values
            myIndices = null;
            myProcesses = null;
        }


        /// <summary>
        /// Make object ready for serialization
        /// </summary>
        internal void Freeze()
        {
            if ( myPidSet != null)
            {
                StringBuilder sb = new StringBuilder(ModuleAndPid);
                foreach (var pid in myPidSet.OrderBy(x=>x))
                {
                    sb.Append($" {PidPrefix}{pid}");
                }
                ModuleAndPid = sb.ToString();
            }

            myPidSet = null;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 17 * 31 + (this.ModuleName?.GetHashCode()).GetValueOrDefault();
            hash = hash * 31 + (int) this.PdbIdx.GetValueOrDefault();
            return hash;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ModuleDefinition other)
        {
            if (Object.ReferenceEquals(this.ModuleAndPid, other.ModuleAndPid))
            {
                return true;
            }

            if (ModuleAndPid != null && other.ModuleAndPid == null ||
                ModuleAndPid == null && other.ModuleAndPid == null)
            {
                return false;
            }

            // compare until Module indices end
            int end = ModuleAndPid.IndexOf('#');
            int end2 = other.ModuleAndPid.IndexOf('#');


            bool lret = true;

            if( end == -1 && end2 == -1)   // process during extraction is not yet set
            {
                lret = ModuleAndPid.Equals(other.ModuleAndPid, StringComparison.Ordinal);
                return lret;
            }

            if (end != end2)
            {
                lret = false;
            }

            if (lret)
            {
                // check if first part of string matches
                int i = 0;
                foreach (var c in ModuleAndPid)
                {
                    if (i == end)
                    {
                        break;
                    }
                    if (c != other.ModuleAndPid[i])
                    {
                        lret = false;
                    }
                    i++;
                }
            }

            return lret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{ModuleName} {ModulePath} {ProductName} {ProductVersionStr} {FileVersionStr} {Fileversion} {Description} PdbIndex: {PdbIdx}";
        }


    }
}
