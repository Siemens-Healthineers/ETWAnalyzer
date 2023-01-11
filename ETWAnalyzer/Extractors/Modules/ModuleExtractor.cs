//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Modules;
using ETWAnalyzer.Infrastructure;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Processes;
using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extractors.Modules
{
    class ModuleExtractor : ExtractorBase
    {
        /// <summary>
        /// Pid of Idle process. This gets the device drivers belonging to the System process. 
        /// </summary>
        const int IdlePid = 0;

        /// <summary>
        /// System process has always pid 4
        /// </summary>
        const int SystemPid = 4;

        IPendingResult<IProcessDataSource> myProcesses;

        public ModuleExtractor()
        {
        }

        public override void RegisterParsers(ITraceProcessor processor)
        {
            myProcesses = processor.UseProcesses();
        }

        PdbIndex GetPdbIndex(ETWExtract results, PdbIdentifier pdb)
        {
            if( pdb == null )
            {
                return PdbIndex.Invalid;
            }

            // UnresolvedPdbs is sorted with default comparer only then this magic will work
            int idx = results.Modules.UnresolvedPdbs.BinarySearch(pdb);
            idx =  idx < 0 ? (int) PdbIndex.Invalid : idx;  // on not found will return negative values with the next matching proximity index
            return (PdbIndex) idx;
        }

        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            using var logger = new PerfLogger("Extract Module");

            // merge with potentially existing pdb data
            if (results.Modules == null) 
            {
                results.Modules = new Extract.Modules.ModuleContainer();
            }

            foreach (var process in myProcesses.Result.Processes)
            {
                DateTimeOffset createTime = DateTimeOffset.MinValue;

                if( process.CreateTime != null)
                {
                    createTime = process.CreateTime.Value.DateTimeOffset;
                }

                if (process.Images != null)
                {
                    foreach (var dll in process.Images)
                    {
                        if (dll.FileName == null || dll.FileVersion == null)
                        {
                            continue;
                        }

                        ETWProcessIndex processIdx = results.GetProcessIndexByPID(process.Id == IdlePid ? SystemPid : process.Id, createTime);
                        PdbIndex pdbIdx = PdbIndex.Invalid;
                        if (dll.Pdb != null)
                        {
                            PdbIdentifier pdb = new PdbIdentifier(Path.GetFileName(dll.Pdb.Path), dll.Pdb.Id, dll.Pdb.Age);
                            pdbIdx = GetPdbIndex(results, pdb);
                        }
                        results.Modules.Add(results, processIdx, pdbIdx, dll.Path, dll.FileVersion, dll.ProductVersion, dll.ProductName, dll.FileVersionNumber, dll.FileDescription);
                    }
                }
            }
        }
    }
}
