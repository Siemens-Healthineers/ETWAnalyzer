//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.FileIO;
using ETWAnalyzer.Extract.Modules;
using ETWAnalyzer.ProcessTools;
using Microsoft.Diagnostics.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.LoadSymbol
{
    /// <summary>
    /// Try to load symbols from all still unresolved pdbs which are stored in the xxx_Derived_Modules.json file.
    /// After lookup duplicate methods are merged because different offsets like kernel32.dll+0x100 and kernel32.dll+0x110 
    /// can point to different parts of the same method.
    /// The unresolved pdb list is updated. Unresolved methods are left unchanged.
    /// You can repeat that as often as you want.
    /// </summary>
    internal class SymbolLoader : IDisposable
    {
        /// <summary>
        /// TraceEvent symbol loader which loads pdbs from symbol server and stores them in a local folder
        /// </summary>
        SymbolReader myReader;

        /// <summary>
        /// Keep list of unresolvable pdbs which are not tried to resolve from symbol server again
        /// </summary>
        HashSet<IPdbIdentifier> myCouldNotLoadFromSymbolServerPdbs = new();

        // some methods are prefixed with ? chars. Remove them
        static char[] SymbolStartChars = new char[] { '?' };

        /// <summary>
        /// Key is pdb path name and corresponding wrapped DiaSymReader
        /// </summary>
        Dictionary<string, SymbolModule> myLoadedPdbs = new ();

        /// <summary>
        /// Create a new symbol loader which can resolve method names from extracted data
        /// </summary>
        /// <param name="reader">TraceEvent symbol reader instance</param>
        /// <exception cref="ArgumentNullException">if reader is null</exception>
        public SymbolLoader(SymbolReader reader)
        {
            if( reader == null )
            {
                throw new ArgumentNullException(nameof(reader));
            }

            myReader = reader;
        }


        /// <summary>
        /// Try to lookup cached pdb
        /// </summary>
        /// <param name="pdbId"></param>
        /// <returns></returns>
        SymbolModule GetPdb(IPdbIdentifier pdbId)
        {
            SymbolModule lret = null;

            if( myCouldNotLoadFromSymbolServerPdbs.Contains(pdbId) )  // do not try to load pdb from symbol server again
            {
                return lret;
            }

            // will download pdb from symbol server if configured correctly
            string localPdbPath = myReader.FindSymbolFilePath(pdbId.Name, pdbId.Id, pdbId.Age);
            if (localPdbPath != null)
            {
                if (!myLoadedPdbs.TryGetValue(localPdbPath, out SymbolModule module))
                {
                    module = new SymbolModule(myReader, localPdbPath);
                    myLoadedPdbs[localPdbPath] = module;
                }
                
                lret = module;
            }
            else
            {
                myCouldNotLoadFromSymbolServerPdbs.Add(pdbId);
            }

            return lret;
        }

        /// <summary>
        /// Try to load symbols from all still unresolved pdbs which are stored in the xxx_Derived_Modules.json file.
        /// After lookup duplicate methods are merged because different offsets like kernel32.dll+0x100 and kernel32.dll+0x110 
        /// can point to different parts of the same method.
        /// </summary>
        /// <param name="extract">Input extract data which is transformed and can later be saved as a resolved Json file/s</param>
        public void LoadSymbols(IETWExtract extract)
        {
            ETWExtract baseExtract = (ETWExtract)extract;

            if (extract?.Modules?.UnresolvedPdbs?.Count == null || extract.Modules.UnresolvedPdbs.Count == 0)
            {
                Console.WriteLine("No unresolved modules found. Cannot resolve.");
                return;
            }

            IReadOnlyList<IPdbIdentifier> unresolved = extract.Modules.UnresolvedPdbs;
            ETWProcess kernelProcess = extract.Processes.Where(x => x.ProcessName == "System").First();

            HashSet<string> noDebugInfo = new();
            HashSet<IPdbIdentifier> newUnresolved = new();
            HashSet<string> resolvedMethods = new();

            foreach (var proc in extract.CPU.PerProcessMethodCostsInclusive.MethodStatsPerProcess)
            {
                foreach (var cost in proc.Costs)
                {
                    if (cost.IsUnresolved)
                    {
                        ETWProcess process = extract.TryGetProcessByPID(proc.Process.Pid, proc.Process.StartTime);
                        IPdbIdentifier ipdb = cost.TryGetPdb(extract, process, cost.Module);
                        if (ipdb == null) // can be a kernel module
                        {
                            ipdb = cost.TryGetPdb(extract, kernelProcess, cost.Module);
                        }

                        if (ipdb != null)
                        {
                            SymbolModule pdb = GetPdb(ipdb);
                            if (pdb != null)
                            {
                                string method = pdb.FindNameForRva(cost.Rva);
                                if (method != null)
                                {
                                    method = method.TrimStart(SymbolStartChars);
                                    // Console.WriteLine($"Resolved {cost.Method} -> {method}");
                                    string resolvedMethod = $"{cost.Module}!{method}";
                                    resolvedMethods.Add(resolvedMethod);
                                    cost.Method = resolvedMethod;
                                }
                            }
                            else
                            {
                                newUnresolved.Add(ipdb);
                            }
                        }
                        else
                        {
                            noDebugInfo.Add($"{cost.Module} in {process?.ProcessNamePretty}");
                        }
                    }
                }
            }

            foreach (var noPdb in noDebugInfo)
            {
                ColorConsole.WriteLine($"Binary was built without pdb or image id events are missing: {noPdb}", ConsoleColor.DarkGreen);
            }

            ColorConsole.WriteLine($"Resolved {unresolved.Count - newUnresolved.Count} pdbs. Still missing: {newUnresolved.Count}");

            UpdateUnresolvedPdbList(extract, baseExtract, newUnresolved);
            SortMethodsAndRemoveDuplicates(baseExtract, resolvedMethods);
            baseExtract.FileIO = (FileIOData)extract.FileIO;
        }

        private static void UpdateUnresolvedPdbList(IETWExtract extract, ETWExtract baseExtract, HashSet<IPdbIdentifier> newUnresolved)
        {
            baseExtract.Modules = (ModuleContainer)extract.Modules;
            List<PdbIdentifier> newList =  newUnresolved.Cast<PdbIdentifier>().ToList();
            newList.Sort();

            Dictionary<int, int> old2Newidx = GetMappingTable(baseExtract.Modules.UnresolvedPdbs, newList);
            baseExtract.Modules.UnresolvedPdbs = newList;
            foreach(var module in baseExtract.Modules.Modules)
            {
                if( module.PdbIdx == null)
                {
                    continue;
                }

                if (old2Newidx.ContainsKey((int)module.PdbIdx))
                {
                    module.PdbIdx = (PdbIndex)old2Newidx[(int)module.PdbIdx];
                }
                else
                {
                    module.PdbIdx = null;
                }
            }
        }


        /// <summary>
        /// Generate an index mapping table from an old list to a new list where the new list does not need to contain
        /// all old values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="old"></param>
        /// <param name="newList"></param>
        /// <returns></returns>
        static Dictionary<int, int> GetMappingTable<T>(List<T> old, List<T> newList)
        {
            // build mapping between old and new MethodIdx which are now invalid in the MethodCosts
            Dictionary<T, int> newIndicies = new();
            Dictionary<int, int> oldNewIndex = new();
            for (int i = 0; i < newList.Count; i++)
            {
                newIndicies[newList[i]] = i;
            }

            for(int j=0; j< old.Count;j++)
            {
                T oldKey = old[j];
                if (newIndicies.ContainsKey(oldKey))
                {
                    oldNewIndex[j] = newIndicies[oldKey];
                }
                else
                {
                    // skip new entry because it is no longer part of the new list 
                }
            }

            return oldNewIndex;
        }

        void SortMethodsAndRemoveDuplicates(ETWExtract extract, HashSet<string> resolvedMethods)
        {
            // create new sorted method list without duplicates
            List<string> uniqueMethods = extract.CPU.PerProcessMethodCostsInclusive.MethodNames.ToHashSet().ToList();
            uniqueMethods.Sort();

            // Map same values from old index to updated index
            Dictionary<int, int> oldNewIndex = GetMappingTable(extract.CPU.PerProcessMethodCostsInclusive.MethodNames, uniqueMethods);

            // set global list
            extract.CPU.PerProcessMethodCostsInclusive.MethodNames = uniqueMethods;

            // merge duplicate methods which were previously resolved. 
            foreach(var perProcess in extract.CPU.PerProcessMethodCostsInclusive.MethodStatsPerProcess)
            {
                Dictionary<string, MethodCost> previous = new Dictionary<string, MethodCost>();
                List<MethodCost> summedCosts = new(); // without duplicate resolved method names
                for(int i=0;i<perProcess.Costs.Count;i++)
                {
                    var cost = perProcess.Costs[i];
                    cost.MethodList = uniqueMethods;
                    cost.MethodIdx = (MethodIndex) oldNewIndex[(int)cost.MethodIdx];  // update index

                    string currentMethod = cost.MethodList[(int)cost.MethodIdx];

                    if (resolvedMethods.Contains(currentMethod) && previous.TryGetValue(currentMethod, out MethodCost existing) )
                    {
                        if(Program.DebugOutput)
                        {
                            Console.WriteLine($"Merge {currentMethod}. Add CPU {cost.CPUMs}/{existing.CPUMs} Wait: {cost.WaitMs}/{existing.WaitMs} Ready: {cost.ReadyMs}/{existing.ReadyMs}");
                        }    

                        // Merge resolved method which can contain several samples
                        existing.FirstOccurenceInSecond = Math.Min(existing.FirstOccurenceInSecond, cost.FirstOccurenceInSecond);
                        existing.LastOccurenceInSecond = Math.Max(existing.LastOccurenceInSecond, cost.LastOccurenceInSecond);
                        existing.DepthFromBottom = Math.Max(existing.DepthFromBottom, cost.DepthFromBottom);
                        existing.ReadyMs += cost.ReadyMs;
                        existing.CPUMs += cost.CPUMs;
                        existing.WaitMs += cost.WaitMs;
                        existing.Threads += cost.Threads;
                    }
                    else
                    {
                        previous[currentMethod] = cost;
                        summedCosts.Add(cost);
                    }
                }

                perProcess.Costs = summedCosts; // replace previous costs 
            }

            Console.WriteLine($"Resolved {resolvedMethods.Count} methods.");

        }

        /// <summary>
        /// Unload all symbols readers and related COM objects
        /// </summary>
        public void Dispose()
        {
            myReader.Dispose();
            myReader = null;
            foreach(var kvp in myLoadedPdbs)
            {
                kvp.Value.Dispose();
            }
            myLoadedPdbs.Clear();
        }
    }
}
