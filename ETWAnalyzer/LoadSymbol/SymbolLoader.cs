//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.CPU.Extended;
using ETWAnalyzer.Extract.FileIO;
using ETWAnalyzer.Extract.Modules;
using ETWAnalyzer.ProcessTools;
using Microsoft.Diagnostics.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

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

                    try
                    {
                        module = new SymbolModule(myReader, localPdbPath);
                        myLoadedPdbs[localPdbPath] = module;
                    }
                    catch (COMException comEx)
                    {
                        myCouldNotLoadFromSymbolServerPdbs.Add(pdbId);
                        string warnMsg = $"Warning: Could not resolve pdb {localPdbPath}. COMException code: 0x{comEx.HResult:X}";
                        if ( (uint) comEx.HResult == 0x806D000C)
                        {
                            warnMsg = $"Warning pdb {localPdbPath} uses a no longer supported pdb format.";
                        }
                        Console.WriteLine(warnMsg);
                        Logger.Warn(warnMsg);
                    }
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

            Dictionary<PdbIndex, PdbIndex> old2Newidx = GetMappingTable<PdbIdentifier,PdbIndex>(baseExtract.Modules.UnresolvedPdbs, newList);
            baseExtract.Modules.UnresolvedPdbs = newList;
            foreach(var module in baseExtract.Modules.Modules)
            {
                if( module.PdbIdx == null)
                {
                    continue;
                }

                if (old2Newidx.ContainsKey(module.PdbIdx.GetValueOrDefault()))
                {
                    module.PdbIdx = old2Newidx[module.PdbIdx.GetValueOrDefault()];
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
        /// <typeparam name="T">List Type</typeparam>
        /// <typeparam name="U">Enum type</typeparam>
        /// <param name="old"></param>
        /// <param name="newList"></param>
        /// <returns></returns>
        static Dictionary<U, U> GetMappingTable<T,U>(List<T> old, List<T> newList) where U : System.Enum
        {
            // build mapping between old and new MethodIdx which are now invalid in the MethodCosts
            Dictionary<T, U> newIndicies = new();
            Dictionary<U, U> oldNewIndex = new();
            for (int i = 0; i < newList.Count; i++)
            {
                newIndicies[newList[i]] = (U)(object)i;
            }

            for(int j=0; j< old.Count;j++)
            {
                T oldKey = old[j];
                if (newIndicies.ContainsKey(oldKey))
                {
                    oldNewIndex[(U)(object)j] = newIndicies[oldKey];
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
            Dictionary<MethodIndex, MethodIndex> oldNewIndex = GetMappingTable<string,MethodIndex>(extract.CPU.PerProcessMethodCostsInclusive.MethodNames, uniqueMethods);

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
                    cost.MethodIdx = oldNewIndex[cost.MethodIdx];  // update index

                    string currentMethod = cost.MethodList[(int)cost.MethodIdx];

                    if (resolvedMethods.Contains(currentMethod) && previous.TryGetValue(currentMethod, out MethodCost existing) )
                    {
#if DEBUG
                        if(Program.DebugOutput)
                        {
                            Console.WriteLine($"Merge {currentMethod}. Add CPU {cost.CPUMs}/{existing.CPUMs} Wait: {cost.WaitMs}/{existing.WaitMs} Ready: {cost.ReadyMs}/{existing.ReadyMs}");
                        }    
#endif

                        // Merge resolved method which can contain several samples
                        existing.FirstOccurenceInSecond = Math.Min(existing.FirstOccurenceInSecond, cost.FirstOccurenceInSecond);  // This is exact
                        existing.LastOccurenceInSecond = Math.Max(existing.LastOccurenceInSecond, cost.LastOccurenceInSecond);     // This is exact
                        existing.DepthFromBottom = Math.Max(existing.DepthFromBottom, cost.DepthFromBottom);                       // Should work reasonably well
                        existing.ReadyMs += cost.ReadyMs;  // Sum across all threads. May resolve much higher Ready times because we sum overlapping time ranges with this which is prevented during normal lookup.
                        existing.CPUMs += cost.CPUMs;      // This is exact
                        existing.WaitMs += cost.WaitMs;    // Sum across all threads. May resolve much higher thread times because we sum overlapping time ranges with this which is prevented during normal lookup.
                        existing.Threads += cost.Threads;  // May overestimate thread count if multiple entries per method are present we can get multiples of the actual thread count.
                    }
                    else
                    {
                        previous[currentMethod] = cost;
                        summedCosts.Add(cost);
                    }
                }

                perProcess.Costs = summedCosts; // replace previous costs 
            }

            // force loading of serialized Json via IETWExtract interface and set it to public property in ETWExtract to serialize changes back to Json file
            IETWExtract iExtract = (IETWExtract)extract;
            if (iExtract?.CPU?.ExtendedCPUMetrics?.MethodIndexToCPUMethodData != null)
            {
                MergeCPUMethodData(extract, oldNewIndex, iExtract);
            }

            Console.WriteLine($"Resolved {resolvedMethods.Count} methods.");

        }

        /// <summary>
        /// When symbols are resolved we have method+RVA1 method+RVA2 which either needs to be combined, or left in isolation.
        /// To make things easy we merge on a best effort basis the extended CPU data.
        /// CPU can be summed, for First/Last min/max are taken. Other things like Ready Time is left untouched because there is no clear
        /// right way to merge the percentiles.
        /// </summary>
        /// <param name="extract"></param>
        /// <param name="oldNewIndex"></param>
        /// <param name="iExtract"></param>
        private static void MergeCPUMethodData(ETWExtract extract, Dictionary<MethodIndex, MethodIndex> oldNewIndex, IETWExtract iExtract)
        {
            extract.CPU.ExtendedCPUMetrics = (CPUExtended)iExtract.CPU.ExtendedCPUMetrics;
            List<CPUMethodData> methodData = extract.CPU.ExtendedCPUMetrics.MethodData;

            // Update method index after method list has been sorted by method name again
            foreach (var method in methodData)
            {
                method.Index = method.Index.ProcessIndex().Create(oldNewIndex[method.Index.MethodIndex()]);
            }

            // Merge methodData 
            HashSet<int> skip = new(); // already merged entries which can be skipped
            for (int i = 0; i < methodData.Count; i++)
            {
                if (skip.Contains(i))
                {
                    continue;
                }

                CPUMethodData methodI = methodData[i];

                for (int k = i + 1; k < methodData.Count; k++)
                {
                    var methodK = methodData[k];
                    if (skip.Contains(k))
                    {
                        continue;
                    }

                    if (methodI.Index == methodK.Index)
                    {
                        skip.Add(k);

                        if (methodI.CPUConsumption != null && methodK.CPUConsumption != null)
                        {
                            foreach (var iMerge in methodI.CPUConsumption)
                            {
                                var found = methodK.CPUConsumption.FirstOrDefault(x => x.EfficiencyClass == iMerge.EfficiencyClass);
                                if (found != null)
                                {
#if DEBUG
                                    Console.WriteLine($"Merging index {methodI.Index}");
#endif
                                    iMerge.CPUMs += found.CPUMs;
                                    iMerge.EnabledCPUsAvg = Math.Max(iMerge.EnabledCPUsAvg, found.EnabledCPUsAvg);
                                    iMerge.FirstS = Math.Min(iMerge.FirstS, found.FirstS);
                                    iMerge.LastS = Math.Max(iMerge.LastS, found.LastS);
                                    iMerge.UsedCores = Math.Max(iMerge.UsedCores, found.UsedCores);
                                }
                            }
                        }
                    }
                }
            }

            List<CPUMethodData> merged = new();
            for (int i = 0; i < methodData.Count; i++)
            {
                if (skip.Contains(i))
                {
                    continue;
                }
                merged.Add(methodData[i]);
            }

            // update array 
            extract.CPU.ExtendedCPUMetrics.MethodData = merged;
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
