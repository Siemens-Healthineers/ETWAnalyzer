//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Processes;
using System;
using System.Collections.Generic;
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


        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            results.Modules = new Extract.Modules.ModuleContainer();

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

                        ETWProcessIndex idx = results.GetProcessIndexByPID(process.Id == IdlePid ? SystemPid : process.Id, createTime);
                        results.Modules.Add(results, idx, dll.Path, dll.FileVersion, dll.ProductVersion, dll.ProductName, dll.FileVersionNumber, dll.FileDescription);
                    }
                }
            }
        }
    }
}
