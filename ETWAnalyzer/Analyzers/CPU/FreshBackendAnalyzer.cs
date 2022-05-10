//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers
{
    /// <summary>
    /// Checks if during a claim operation an LCO/ReserveAsync stacktag did consume > 100ms time
    /// </summary>
    class FreshBackendAnalyzer : AnalyzerBase
    {
        const string LCOReserverStacktag = "OneViewer\\LCO ReserveAsync";
        const string ViewingShellHost = "syngo.Viewing.Shell.Host.exe";
        const string LCMService = "syngo.Common.LCMService.exe";



        public override void AnalyzeTestRun(TestAnalysisResultCollection issues, TestRun run)
        {
            TestAnalysisResults = issues;

            List<ETWExtract> extracts = new();
            string lastBackend = null;
            List<KeyValuePair<DateTimeOffset, string>> newBackends = new();
            HashSet<ETWProcess> previousHosts = null;

            // run only for server ETL files which can have a viewingshell host process
            foreach (TestDataFile runDataFile in run.AllTestFilesSortedAscendingByTime.Where(x=> (x.Extract?.Processes?.Any( x=> x.ProcessName == LCMService)).GetValueOrDefault() ))
            {
                IETWExtract deser = runDataFile.Extract;

                var currentViewingShellHosts = deser.Processes.Where(x => x.ProcessName == ViewingShellHost).ToHashSet();
                if (Program.DebugOutput)
                {
                    Console.WriteLine($"Testcase: {runDataFile.ParentTest.Name} ViewingShellHosts: {currentViewingShellHosts.Count} {runDataFile.FileName}");
                }

                previousHosts = currentViewingShellHosts;

                string newStartedViewingShellHost = null;
                foreach (var newprocess in deser.Processes.Where(x => x.IsNew && x.ProcessName == ViewingShellHost))
                {
                    newStartedViewingShellHost = newprocess.ProcessWithID;
                    if (Program.DebugOutput)
                    {
                        Console.WriteLine($"\tStarted New Backend after {(newprocess.StartTime - deser.SessionStart).TotalSeconds:F0}s {newprocess.ProcessWithID}");
                    }

                    newBackends.Add(new KeyValuePair<DateTimeOffset, string>(newprocess.StartTime, newprocess.ProcessWithID));
                }

                foreach(var gone in deser.Processes.Where(x => x.HasEnded && x.ProcessName == ViewingShellHost))
                {
                    if (Program.DebugOutput)
                    {
                        Console.WriteLine($"\tHost ended: {gone.ProcessWithID}");
                    }
                }
                
                foreach (var backend in deser.SummaryStackTags.Stats.Where(x => x.Key.Name == ViewingShellHost))
                {
                    var reserve = backend.Value.Where(x => x.Stacktag == LCOReserverStacktag).FirstOrDefault();
                    if (reserve != null)
                    {
                        string backendStr = lastBackend == backend.Key.ToString() ? "same" : backend.Key.ToString();
                        lastBackend = backend.Key.ToString();

                        
                        if (Program.DebugOutput)
                        {
                            Console.WriteLine($"\tReserve CPU: {reserve.CPUInMs} ms");
                        }

                        if (reserve.CPUInMs > 100)
                        {
                            ETWProcess proc = runDataFile.FindProcessByKey(backend.Key);

                            List<string> additionalInfos = new()
                            {
                                $"Reserve CPU was {reserve.CPUInMs} ms of process {proc.GetProcessWithId(true)} {proc.CommandLineNoExe}"
                            };

                            issues.AddIssue(runDataFile, new Issue(this, "Fresh Backend Used", Classification.Performance, Severities.Warning, 
                                                                    VerboseOutput ? additionalInfos: null));
                            var kvp = newBackends.Where(x => x.Value == lastBackend).FirstOrDefault();
                            if( kvp.Key != default)
                            {
                                if (Program.DebugOutput)
                                {
                                    Console.WriteLine($"\tDid use previously started backend {(deser.SessionStart - kvp.Key).TotalMinutes:F1} minutes before");
                                }
                            }
                        }
                    }
                }


            }
        }

        public override void Print()
        {

        }
        public override void AnalyzeTestsByTime(TestAnalysisResultCollection issues, TestDataFile backend, TestDataFile frontend)
        {
        }

        public override void TakePersistentFlagsFrom(AnalyzeCommand analyzeCommand)
        {
            throw new NotImplementedException();
        }
    }
}
