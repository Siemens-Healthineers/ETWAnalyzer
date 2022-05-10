//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Infrastructure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public List<ModuleDefinition> Modules
        {
            get;
            set;
        } = new List<ModuleDefinition>();

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

        /// <summary>
        /// Add a module to the container
        /// </summary>
        /// <param name="extract"></param>
        /// <param name="processIdx"></param>
        /// <param name="fullPath"></param>
        /// <param name="fileVersionStr"></param>
        /// <param name="productVersionStr"></param>
        /// <param name="productName"></param>
        /// <param name="fileVersion"></param>
        /// <param name="description"></param>
        public void Add(ETWExtract extract, ETWProcessIndex processIdx, string fullPath, string fileVersionStr, string productVersionStr, string productName, Version fileVersion, string description)
        {
            ModuleDefinition mod = new ModuleDefinition(this, processIdx, fullPath, fileVersionStr, productVersionStr, productName, fileVersion, description);
            bool bFound = false;
            foreach (var module in Modules)
            {
                if (mod.Equals(module))
                {
                    module.AddPid(processIdx);
                    bFound = true;
                    break;
                }
            }

            if (!bFound)
            {
                Modules.Add(mod);
            }
        }
    }
}
