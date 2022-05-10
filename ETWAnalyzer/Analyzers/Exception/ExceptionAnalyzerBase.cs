//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using ETWAnalyzer.Extensions;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers.Exception
{
    abstract class ExceptionAnalyzerBase : AnalyzerBase
    {
        public bool IsPrintingFullMsgFlag { get; private set; }
        public bool IsPrintingFullStackFlag { get; private set; }
        public bool IsPrintingFlatStackFlag { get; private set; }
        public bool IsPrintingFlatMsgFlag { get; private set; }
        public string OutdirFlag { get; set; }

        public override void TakePersistentFlagsFrom(AnalyzeCommand analyzeCommand)
        {
            base.TakePersistentFlagsFrom(analyzeCommand);

            OutdirFlag = analyzeCommand.OutDir;

            IsPrintingFlatMsgFlag = analyzeCommand.IsPrintingFlatMsg;
            IsPrintingFullMsgFlag = analyzeCommand.IsPrintingFullMsg;
            IsPrintingFlatStackFlag = analyzeCommand.IsPrintingFlatStack;
            IsPrintingFullStackFlag = analyzeCommand.IsPrintingFullStack;
        }

        /// <summary>
        /// Relevant processes
        /// </summary>
        protected readonly static string[] RelevantProcessNames =
        {
                "Vortal",//UI
                "TAU.UniversalTestFramework.Runner.exe",//frondend
                "ViewingShell_MMReading",   //backend
                "syngo.Services.Workflow.Server.exe",//backend
                "syngo.Services.sDM.Server.exe",// backend
        };

        /// <summary>
        /// Irrelevant Processes
        /// </summary>
        readonly static string[] myIrrelevantProcessNames =
        {
                "MR QMS",
                "w3wp.exe",
                "syngo.Services.sDM.Diagnostics.exe",
                "syngo.Services.Workflow.Admin.exe",
                "syngo.Common.Starter.exe",
                "MR QMS Job",
                "syngo.Services.Workflow.MonitorView.exe",
                "BrowsingServiceManager",
                "ImageAccess",
                "Reporting Web Services",
                "TAU.Remoting.Hoster.exe",
                "syngo.LogViewer.exe",
                "ViewingShell_HLM",
                "syngo.Services.sDM.DeploymentFunctions.exe",
                "syngo.Services.Workflow.Client.exe",
                "Agent.Listener.exe",
                "syngo.Common.Container.exe",
                "SQLPS.exe",
                "CSM",
        };
        protected TestRun GenerateReducedDeepCopyOfSourceRun(TestRun run)
        {
            List<SingleTest> tempSingleTests = new();
            ModuleVersion moduleVersion = run.GetMainModuleVersion();
            foreach (var test in run.Tests)
            {
                foreach (var singleTest in test.Value)
                {
                    List<TestDataFile> tempTestDataFiles = new();

                    foreach (var file in singleTest.Files)
                    {
                        var deepCopy = file.JsonExtractFileWhenPresent != null ? new TestDataFile(file.FileName) : file;

                        var reducedDeepCopy = new TestDataFile(deepCopy.TestName, deepCopy.FileName, deepCopy.PerformedAt, deepCopy.DurationInMs, (int)deepCopy.SizeInMB, deepCopy.MachineName, deepCopy.SpecificModifyDate, deepCopy.IsValidTest)
                        {
                            Extract = new ETWExtract()
                            {
                                Exceptions = new ExceptionStats(deepCopy.Extract.Exceptions.Exceptions.ToList()),
                                MainModuleVersion = moduleVersion.GetDeepCopy()
                            }
                        };
                        tempTestDataFiles.Add(reducedDeepCopy);
                    }
                    tempSingleTests.Add(new SingleTest(tempTestDataFiles.ToArray(),run));
                }
            }
            return new TestRun(tempSingleTests, run.Parent, false);
        }
    }
}
