//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.Extract.Modules
{
    /// <summary>
    /// Contains information about loaded modules from all processes.
    /// </summary>
    public class ModuleContainer : IModuleContainer
    {
        /// <summary>
        /// Set during serialization
        /// </summary>
        internal IETWExtract Extract
        {
            get;set;
        }

        /// <summary>
        /// List of loaded modules which contain also the processes which have them loaded. 
        /// The setter is only needed for De/Serializaton of data!
        /// </summary>
        public List<ModuleDefinition> Modules {  get; set; } = new();

        /// <summary>
        /// List of all not resolved pdbs during extraction. This data is needed later
        /// to resolve missing method names
        /// </summary>
        public List<PdbIdentifier> UnresolvedPdbs { get; set; } = new();

        /// <summary>
        /// List of all not resolved pdbs during extraction. This data is needed later
        /// to resolve missing method names
        /// </summary>
        IReadOnlyList<IPdbIdentifier> IModuleContainer.UnresolvedPdbs { get => UnresolvedPdbs; }

        /// <summary>
        /// Common strings like file names, directories, file descriptions ... go here into this global list so we can reference in other instances the strings by index 
        /// to save space in the generate json file. This sacrifices readability for a smaller Json file.
        /// </summary>
        public UniqueStringList SharedStrings
        {
            get; set;
        } = new UniqueStringList();

        /// <summary>
        /// List of loaded modules which contain also the processes which have them loaded. 
        /// </summary>
        IReadOnlyList<ModuleDefinition> IModuleContainer.Modules => Modules;

        Dictionary<ModuleDefinition, ModuleDefinition> myAddedModules = new();

        /// <summary>
        /// Cache 
        /// </summary>
        ILookup<string, ModuleDefinition> myModuleLookup;

        /// <summary>
        /// System process has this const name (and pid 4 but we do not want to rely on that)
        /// </summary>
        const string SystemProcessName = "System";

        /// <summary>
        /// Resolved modules for a process
        /// </summary>
        Dictionary<Tuple<ETWProcess,string>, ModuleDefinition> myModuleCache = new();

        /// <summary>
        /// Add a module to the container
        /// </summary>
        /// <param name="extract"></param>
        /// <param name="processIdx"></param>
        /// <param name="pdbIdx"></param>
        /// <param name="fullPath"></param>
        /// <param name="fileVersionStr"></param>
        /// <param name="productVersionStr"></param>
        /// <param name="productName"></param>
        /// <param name="fileVersion"></param>
        /// <param name="description"></param>
        public void Add(ETWExtract extract, ETWProcessIndex processIdx, PdbIndex pdbIdx, string fullPath, string fileVersionStr, string productVersionStr, string productName, Version fileVersion, string description)
        {
            ModuleDefinition mod = new ModuleDefinition(this, processIdx, pdbIdx, fullPath, fileVersionStr, productVersionStr, productName, fileVersion, description);

            if( myAddedModules.TryGetValue(mod, out ModuleDefinition existing) )
            {
                existing.AddPid(processIdx); // module is already present, just add the new pid to it
            }
            else
            {
                myAddedModules[mod] = mod; // store in dictionary
                Modules.Add(mod);
            }
        }


        /// <summary>
        /// Find Module Definition for a loaded module in a process.
        /// </summary>
        /// <param name="moduleName">Module name</param>
        /// <param name="process">Process in which the module is loaded</param>
        /// <returns>ModuleDefinition when module could be located or null when it could not be found.</returns>
        public ModuleDefinition FindModule(string moduleName, ETWProcess process)
        {
            ModuleDefinition lret = null;

            var key = Tuple.Create(process, moduleName);
            if (!myModuleCache.TryGetValue(key, out lret))
            {

                if (myModuleLookup == null)
                {
                    myModuleLookup = Modules.ToLookup(x => x.ModuleName);
                }

                IEnumerable<ModuleDefinition> candidates = myModuleLookup[moduleName];
                foreach (ModuleDefinition candidate in candidates)
                {
                    var matchingProcess = candidate.Processes.FirstOrDefault(x => x.Equals(process) || x.ProcessName == SystemProcessName);
                    if (matchingProcess != null)
                    {
                        lret = candidate;
                        break;
                    }
                }

                myModuleCache[key] = lret;
            }

            return lret;
        }

        /// <summary>
        /// Make ModuleDefinition objects ready for serialization
        /// </summary>
        internal void Freeze()
        {
            foreach(ModuleDefinition mod in Modules)
            {
                mod.Freeze();
            }
        }
    }
}
