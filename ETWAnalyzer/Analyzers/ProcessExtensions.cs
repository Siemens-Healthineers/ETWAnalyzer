//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers
{
    /// <summary>
    /// Well known processes. This is an enum for now but this should become more flexible and factored out to configuration which a little DSL
    /// to support other processes easily without needing to change ETWAnalyzer
    /// </summary>
    public enum KnownProcess
    {
        /// <summary>
        /// Frontend prcoess
        /// </summary>
        Frontend,

        /// <summary>
        /// Backend Process
        /// </summary>
        Backend,

        /// <summary>
        /// New started backend process
        /// </summary>
        BackendNew,
        /// <summary>
        /// Workflow Server
        /// </summary>
        WorkflowServer,

        /// <summary>
        /// SDM
        /// </summary>
        SDMServer,

        /// <summary>
        /// SQL Server
        /// </summary>
        SQLServer,
    }


    static class ProcessExtensions
    {
        public const string LCMServiceExe = "syngo.Common.LCMService.exe";
        const string ViewingShellHostExe = "syngo.Viewing.Shell.Host.exe";

        /// <summary>
        /// This ETL file was loaded by ProfilingDataManager which did rename processes already
        /// </summary>
        const string ViewingShellHostRenamed = "syngo_VShell.MMReading__.exe";
        const string ContainerExe = "syngo.Common.Container.exe";

        /// <summary>
        /// This ETL was loaded by ProfilingDataManager which did rename processes already
        /// </summary>
        const string ContainerVortalRenamed = "Vortal.syngo__________.exe";

        static readonly string[] FrontendCmdLines = new string[] { "-VortalUI", "-SW.VSS", "-SW.Falcon", "-SW.VOR" };
        const string MMReadingBackendCmdLine = "MM Reading";
        const string FalconBackendCmdLine = "/type \"Falcon\"";

        /// <summary>
        /// Find a single process using a best fit search if multiple processes are possible
        /// </summary>
        /// <param name="process">Process type to search for</param>
        /// <param name="file">Extracted ETL file Json</param>
        /// <returns>When found the best fit process or a KeyNotFoundException exception is thrown.</returns>
        /// <param name="throwOnError"></param>
        public static ETWProcess FindProcess(KnownProcess process, TestDataFile file, bool throwOnError = true)
        {
            return process switch
            {
                KnownProcess.Frontend => FindFrontend(file, throwOnError),
                KnownProcess.Backend => FindBackend(file, false, throwOnError),
                KnownProcess.BackendNew => FindBackend(file, true, throwOnError),
                KnownProcess.SDMServer => FindSingleProcessByName(file, "syngo.Services.sDM.Server.exe", throwOnError),
                KnownProcess.WorkflowServer => FindSingleProcessByName(file, "syngo.Services.Workflow.Server.exe", throwOnError),
                KnownProcess.SQLServer => FindSingleProcessByName(file, "sqlserver.exe", throwOnError),
                _ => throw new NotSupportedException($"Process type {process} is not known"),
            };
        }


        static bool IsBackend(ETWProcess x) => (x.ProcessName == ViewingShellHostExe ||
                                                 x.ProcessName == ViewingShellHostRenamed) &&
                                               (x.CmdLine.Contains(MMReadingBackendCmdLine) ||
                                                 x.CmdLine.Contains(FalconBackendCmdLine));


        /// <summary>
        /// Try to locate a OneViewer backend which does something and which is not prestarted
        /// </summary>
        /// <param name="file">ETW file</param>
        /// <param name="isNew">if true a new started Backend process is returned if present. Otherwise the backend with most CPU which is not prestarted is selected.</param>
        /// <param name="throwOnError"></param>
        /// <returns></returns>
        static ETWProcess FindBackend(TestDataFile file, bool isNew, bool throwOnError = true)
        {
            List<ETWProcess> candidates = new List<ETWProcess>();
            foreach (var beCandidate in file.Extract.Processes.Where(IsBackend))
            {
                if (isNew && beCandidate.IsNew)
                {
                    return beCandidate;
                }

                candidates.Add(beCandidate);
            }

            // CPU is sorted from high to low the Vortal with highest CPU is taken
            foreach (var kvp in file.Extract.CPU.PerProcessCPUConsumptionInMs.OrderByDescending(x => x.Value))
            {
                foreach (var candidate in candidates)
                {
                    if (kvp.Key == candidate.ToProcessKey().ToString())
                    {
                        // fresh backend due to empty reuse pool
                        if (candidate.IsNew)
                        {
                            if ((candidate.StartTime - file.Extract.SessionStart) < TimeSpan.FromSeconds(10))
                            {
                                return candidate;
                            }

                            // otherwise it is a fresh started backend to refill the reuse pool
                            continue;
                        }


                        return candidate;
                    }
                }
            }

            if (throwOnError)
            {
                throw new KeyNotFoundException($"No OneViewer process found in data file {file.FileName}");
            }

            return null;
        }

        /// <summary>
        /// Find single process by name. This is only useful if there is only one process of that type running.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="exeName"></param>
        /// <exception cref="InvalidOperationException">When more than one process is found</exception>
        /// <param name="throwOnError"></param>
        /// <returns>Single process</returns>
        public static ETWProcess FindSingleProcessByName(TestDataFile file, string exeName, bool throwOnError = true)
        {
            ETWProcess process = file.Extract.Processes.Where(x => exeName.Equals(x.ProcessName, StringComparison.OrdinalIgnoreCase)).SingleOrDefault();
            if (throwOnError && process == null)
            {
                throw new InvalidOperationException($"Process {exeName} could not be found in {file.JsonExtractFileWhenPresent}");
            }

            return process;
        }

        /// <summary>
        /// Find a process by its process key from a TestDataFile
        /// </summary>
        /// <param name="file"></param>
        /// <param name="key">ProcessKey</param>
        /// <returns>Found process or null if it was not found. If multiple processes would match the first occurrence is returned.</returns>
        public static ETWProcess FindProcessByKey(this TestDataFile file, ProcessKey key)
        {
            if( key == null )
            {
                return null;
            }
            return file.Extract.Processes.Where(x => x.ProcessName != null && (x.ProcessID == key.Pid && key.Name == x.ProcessName) ).FirstOrDefault();
        }

        /// <summary>
        /// Check if given process is would be returned by any of the KnownProcess find enum values
        /// </summary>
        /// <param name="file"></param>
        /// <param name="process"></param>
        /// <returns>true if process is known, false otherwise</returns>
        public static bool IsKnownProcess(TestDataFile file, ProcessKey process)
        {
            foreach (KnownProcess wellKnown in Enum.GetValues(typeof(KnownProcess)))
            {
                try
                {
                    ETWProcess proc = FindProcess(wellKnown, file, false);
                    if (proc != null && proc.ToProcessKey().EqualNameAndPid(process))
                    {
                        return true;
                    }
                }
                catch (KeyNotFoundException)
                { }
            }

            return false;

        }

        static ETWProcess FindFrontend(TestDataFile file, bool throwOnError = true)
        {
            List<ETWProcess> candidates = new List<ETWProcess>();
            foreach (var vortalCandidate in file.Extract.Processes.Where(x => (x.ProcessName == ContainerExe || x.ProcessName == ContainerVortalRenamed) && FrontendCmdLines.Any(substr => x.CmdLine.Contains(substr))))
            {
                candidates.Add(vortalCandidate);
            }

            // CPU is sorted from high to low the Vortal with highest CPU is taken
            foreach (var kvp in file.Extract.CPU.PerProcessCPUConsumptionInMs.OrderByDescending(x => x.Value))
            {
                foreach (var candidate in candidates)
                {
                    if (kvp.Key == candidate.ToProcessKey().ToString())
                    {
                        return candidate;
                    }
                }
            }

            if (throwOnError)
            {
                throw new KeyNotFoundException($"No Vortal Process found in data file {file.FileName}.");
            }

            return null;
        }

    }
}
