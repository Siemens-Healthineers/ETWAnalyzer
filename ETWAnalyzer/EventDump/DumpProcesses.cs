//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers.Infrastructure;
using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Metadata;
using Microsoft.Windows.EventTracing.Processes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static ETWAnalyzer.Commands.DumpCommand;
using static ETWAnalyzer.Extract.ETWProcess;

namespace ETWAnalyzer.EventDump
{
    class DumpProcesses : DumpFileEtlBase<DumpProcesses.MatchData>
    {
        public bool ShowFileOnLine { get; internal set; }
        public bool ShowAllProcesses { get; internal set; }

        public bool Crash { get; internal set; }
        public Func<string, bool> Parent { get; set; } = _ => true;
        public Func<string, bool> Session { get; set; } = _ => true;
        public DumpCommand.SortOrders SortOrder { get; internal set; }
        public bool Merge { get; internal set; }
        public bool NoCmdLine { get; internal set; }
        public bool ShowDetails { get; internal set; }
        public MinMaxRange<double> MinMaxDurationS { get; internal set; } = new MinMaxRange<double>();
        public bool ShowUser { get; set; }
        public MinMaxRange<double> MinMaxStart { get; internal set; } = new MinMaxRange<double>();

        const string WerFault = "WerFault.exe";

        public override List<MatchData> ExecuteInternal()
        {
            List<MatchData> lret = base.ExecuteInternal();

            lret = Crash ? GetCrashedProcesses(lret) : lret;

            if (Merge)
            {
                lret = CalculateProcessLifeTimesAndFilterRedundantCrossFileEvents(lret);
            }

            lret = lret.Where(x=> IsInRange(x.LifeTime) && x.IsMatch(NewProcessFilter)).ToList();

            if (IsCSVEnabled)
            {
                WriteToCSV(lret);
            }
            else
            {
                Print(lret);
            }

            return lret;
        }


        /// <summary>
        /// Used by context sensitive help
        /// </summary>
        static readonly internal SortOrders[] ValidSortOrders = new[]
        {
            SortOrders.Time,
            SortOrders.StopTime,
            SortOrders.Tree,
            SortOrders.Default,
        };


        string myCurrentSourceFile = null;

 
        /// <summary>
        /// Print output to console
        /// </summary>
        /// <param name="data"></param>
        private void Print(List<MatchData> data)
        {
            if( data.Count == 0) // nothing to print and Max would throw otherwise for max column calculation
            {
                return;
            }

            string currentSourceFile = null;

            if (SortOrder == DumpCommand.SortOrders.Time)
            {
                // sort by time or by alphabet if no time info is there
                var nostartEnd = data.Where(x => x.StartTime == null && x.EndTime == null).OrderBy(x=>x.ProcessName);
                var endedbutnotStarted = data.Where(x => x.EndTime != null && x.StartTime == null).OrderBy(x => x.EndTime);
                var started = data.Where(x => x.StartTime != null).OrderBy(x => x.StartTime);
                data = nostartEnd.ToList();
                data.AddRange(endedbutnotStarted);
                data.AddRange(started);
            }
            else if( SortOrder == DumpCommand.SortOrders.StopTime)
            {
                // sort by time or by alphabet if no time info is there
                var nostartEnd = data.Where(x => x.StartTime == null && x.EndTime == null).OrderBy(x => x.ProcessName);
                var ended = data.Where(x => x.EndTime != null).OrderBy(x => x.EndTime);
                var startedbutnotEnded = data.Where(x => x.StartTime != null && x.EndTime != null).OrderBy(x => x.StartTime);
                data = nostartEnd.ToList();
                data.AddRange(startedbutnotEnded);
                data.AddRange(ended);
            }
            else if( SortOrder == DumpCommand.SortOrders.Tree) // Process tree visualization is different
            {
                data = MatchData.ConvertToTree(data);

                foreach (var root in data)
                {
                    PrintProcessTree(root, 0);
                }

                return;
            }

            int userWidth = data.Max(x => x.User.Length);
            int lifeTimeWidth = data.Max(x => x.LifeTimeString.Length);
            int startTimeWidth = data.Max(x => x.StartTime.HasValue ? GetDateTimeString(x.StartTime.Value, x.SessionStart, TimeFormatOption).Length : 0);
            int stopTimeWidth = data.Max(x => x.EndTime.HasValue ? GetDateTimeString(x.EndTime.Value, x.SessionStart, TimeFormatOption).Length : 0);
            int returnCodeWidth = data.Max(x => (x.ReturnCodeString?.Length).GetValueOrDefault());


            foreach (var m in data)
            {
                if( currentSourceFile != m.SourceFile && !ShowFileOnLine)
                {
                    PrintFileName(m.SourceFile, null, m.PerformedAt.DateTime, m.BaseLine);
                    currentSourceFile = m.SourceFile;
                }

                string startTime = "";
                if (m.StartTime != null)
                {
                    startTime = GetDateTimeString(m.StartTime.Value, m.SessionStart, TimeFormatOption);
                }

                string stopTime = "";
                if (m.EndTime != null)
                {
                    stopTime = GetDateTimeString(m.EndTime.Value, m.SessionStart, TimeFormatOption);
                }

                string user = "";
                if (ShowUser)
                {
                    user = m.User.WithWidth(-1 * userWidth) + " ";
                }


                string fileName = "";
                if( ShowFileOnLine )
                {
                    fileName = Path.GetFileNameWithoutExtension(m.SourceFile);
                }
                string cmdLine = m.ProcessName;
                if (!NoCmdLine)
                {
                    cmdLine = String.IsNullOrEmpty(m.CmdLine) ? m.ProcessName : m.CmdLine;
                }

                string sessionId = "";
                if (ShowDetails)
                {
                    sessionId = ShowDetails ? $" Session: { m.SessionId, 2}" : "";
                }

                string str = $"PID: [yellow]{m.ProcessId,-6}[/yellow] " +
                             $"Start: [green]{startTime.WithWidth(startTimeWidth)}[/green] " +
                             $"Stop: [darkcyan]{stopTime.WithWidth(stopTimeWidth)} Duration: {m.LifeTimeString.WithWidth(lifeTimeWidth)}[/darkcyan] " +
                             $"RCode: {m.ReturnCodeString.WithWidth(returnCodeWidth)} Parent: {m.ParentProcessId, 5} " +
                             $"[yellow]{sessionId}[/yellow] " +
                             $"[yellow]{user}[/yellow]{cmdLine} {fileName}";
                ColorConsole.WriteEmbeddedColorLine(str);
            }
        }

        /// <summary>
        /// Print a process tree starting at indention level
        /// </summary>
        /// <param name="root">Starting process</param>
        /// <param name="level">indention level</param>
        void PrintProcessTree(MatchData root, int level)
        {
            string indention = new string(' ', 2 * level); // 2 spaces per level

            int userWidth = root.Childs.Count == 0 ? 0 : root.Childs.Max(x => x.User.Length);
            int lifeTimeWidth = root.Childs.Count == 0 ? 0 : root.Childs.Max(x => x.LifeTimeString.Length);
            int startTimeWidth = root.Childs.Count == 0 ? 0 : root.Childs.Max(x => x.StartTime.HasValue ? GetDateTimeString(x.StartTime.Value, x.SessionStart, TimeFormatOption).Length : 0);
            int stopTimeWidth = root.Childs.Count == 0 ? 0 : root.Childs.Max(x => x.EndTime.HasValue ? GetDateTimeString(x.EndTime.Value, x.SessionStart, TimeFormatOption).Length : 0);
            int returnCodeWidth = root.Childs.Count == 0 ? 0 : root.Childs.Max(x => (x.ReturnCodeString?.Length).GetValueOrDefault());

            if (myCurrentSourceFile != root.SourceFile && !ShowFileOnLine)
            {
                PrintFileName(root.SourceFile, null, root.PerformedAt.DateTime, root.BaseLine);
                myCurrentSourceFile = root.SourceFile;
            }

            PrintTreeLine(root, userWidth, level, indention, returnCodeWidth);

            foreach (var child in root.Childs)
            {
                PrintProcessTree(child, level + 1);
            }
        }

        /// <summary>
        /// Print a process tree Item
        /// </summary>
        /// <param name="data"></param>
        /// <param name="userWidth"></param>
        /// <param name="level"></param>
        /// <param name="indention"></param>
        /// <param name="returnCodeWidth"></param>
        void PrintTreeLine(MatchData data, int userWidth, int level, string indention, int returnCodeWidth)
        {
            string startTime = "";
            if (data.StartTime != null)
            {
                startTime = GetDateTimeString(data.StartTime.Value, data.SessionStart, TimeFormatOption);
            }

            string stopTime = "";
            if (data.EndTime != null)
            {
                stopTime = GetDateTimeString(data.EndTime.Value, data.SessionStart, TimeFormatOption);
            }

            string user = "";
            if (ShowUser)
            {
                user = data.User.WithWidth(-1 * userWidth) + " ";
            }


            string fileName = "";
            if (ShowFileOnLine)
            {
                fileName = Path.GetFileNameWithoutExtension(data.SourceFile);
            }
            string cmdLine = data.ProcessName;
            if (!NoCmdLine)
            {
                cmdLine = String.IsNullOrEmpty(data.CmdLine) ? data.ProcessName : data.CmdLine;
            }

            string sessionId = "";
            if (ShowDetails)
            {
                sessionId = ShowDetails ? $" Session: {data.SessionId,2}" : "";
            }

            string treeMarker = level > 0 ? "|- " : "";
            string str = $"{indention}{treeMarker}[yellow]{data.ProcessId,-6}[/yellow]" +
                         $" {data.StartStopTags} " +
                         (data.ReturnCode == null ? "" : $"RCode: {data.ReturnCodeString.WithWidth(returnCodeWidth)}") +
                         $"[yellow]{sessionId}[/yellow] " +
                         $"[yellow]{user}[/yellow]{cmdLine} {fileName}";
            ColorConsole.WriteEmbeddedColorLine(str);
        }


        internal List<MatchData> CalculateProcessLifeTimesAndFilterRedundantCrossFileEvents(List<MatchData> data)
        {
            // Calculate process lifetime and update Start/End and Lifetime in all related events so we can later 
            // filter away redundant events from other ETL files to get only one Process/Start/Stop event with full lifetime info
            foreach (var perProcess in data.GroupBy(x => x.ProcessWithPid))
            {
                // ordering by start and end time combined ensures that start/end times are following each other since
                // a process with the same pid cannot be started in between and inside a group we deal only with one process cmd line combination
                var processEvents = perProcess.Where(x=> x.StartTime!=null || x.EndTime!=null).OrderBy(x => x.StartTime ?? x.EndTime).ToArray();
                for (int i = 0; i < processEvents.Length; i++)
                {
                    MatchData current = processEvents[i];
                    if (current.StartTime != null)
                    {
                        if (current.EndTime != null)
                        {
                            current.LifeTime = current.EndTime - current.StartTime;
                        }
                        else
                        {
                            // The next event after a start must be its stop event or nothing 
                            for (int j = i + 1;  j < i+2 && j < processEvents.Length; j++)
                            {
                                if (processEvents[j].EndTime != null)
                                {
                                    processEvents[j].LifeTime = processEvents[j].EndTime - current.StartTime;
                                    current.EndTime = processEvents[j].EndTime;
                                    current.LifeTime = processEvents[j].LifeTime;
                                }

                                if (processEvents[j].StartTime == null )
                                {
                                    processEvents[j].StartTime = current.StartTime;
                                }
                            }
                        }
                    }
                }
            }

            ILookup<string, MatchData> processpidCmdLineGroups = data.ToLookup(x => x.ProcessWithPid + x.CmdLine);

            // Update IsNewprocess flag across files 
            // this might be wrong if the pid is reused and the process name and cmdline match but most of the time it should be good enough
            foreach (var group in processpidCmdLineGroups)
            {
                foreach (MatchData match in group)
                {
                    if (group.Any(x => x.IsNewProcess))
                    {
                        match.IsNewProcess = true;
                    }
                    if( group.Any(x=> x.HasEnded))
                    {
                        match.HasEnded = true;
                    }
                }
            }

            HashSet<MatchData> unique = new();

            // when multiple ETL files were processed we can skip the ones where same process occurs again just to see it is still running. 
            // That is not interesting. We are interested in the events when the process did start or stop
            List<MatchData> withoutRedundantMatches = new();

            foreach(var group in processpidCmdLineGroups)
            {
                foreach (MatchData m in group)
                {
                    // When we have for same pid events with start/stop we omit trace files where we have no start/stop data
                    if (m.StartTime == null && m.EndTime == null && group.Any(x => (x.StartTime != null || x.EndTime != null)) )
                    {
                        continue;
                    }

                    //// ensure no duplicates 
                    if (!ShowAllProcesses && unique.Add(m) == false)
                    {
                        continue;
                    }
                   
                    withoutRedundantMatches.Add(m);
                }
            }

            return withoutRedundantMatches;
        }

        bool IsInRange(TimeSpan ?lifeTime)
        {
            int livingS = (int) (lifeTime.GetValueOrDefault().TotalSeconds);
            bool lret = MinMaxDurationS.IsWithin(livingS);
            return lret;
        }


        /// <summary>
        /// Convert Json file to a list of filterd matches.
        /// </summary>
        /// <param name="json">Input Json file</param>
        /// <returns>List of matches which have passed the Process/Parent/Session filters.</returns>
        protected override List<MatchData> DumpJson(TestDataFile json)
        {
            List<MatchData> lret = new();

            if( json?.Extract?.Processes == null || json.Extract.Processes.Count == 0)
            {
                ColorConsole.WriteError($"No process data present in file {json.FileName}");
                return lret;
            }

            IETWExtract extract = json.Extract;
            
            // when -Parent filter is active we add also the parent processes to output
            // since a parent can have many childs we need to ensure to not print parent processes more than once
            HashSet<ETWProcess> alreadyPrinted = new();

            // order process starts by process name and group them by exe
            foreach (var processGroup in extract.Processes.GroupBy(x => x.GetProcessName(UsePrettyProcessName)).OrderBy(x => x.Key))
            {
                HashSet<ETWProcess> foundParentProcesses = new();

                HashSet<ETWProcess> processes = processGroup.Where(ProcessFilter).Where(x => ParentFilter(x, extract.Processes, foundParentProcesses)).Where(SessionIdFilter).ToHashSet();

                // Add parents to list and remove already printed ones
                processes.UnionWith(foundParentProcesses);
                processes.ExceptWith(alreadyPrinted);

                // update alradyPrinted list with current and parents 
                alreadyPrinted.UnionWith(foundParentProcesses);
                alreadyPrinted.UnionWith(processes);

                // Now we can sort 
                List<ETWProcess> sortedProcesses = processes.OrderBy(x => x.StartTime).ThenBy(x => x.ProcessID).ToList();

                // then order by start time and if not present by process id
                foreach (var process in sortedProcesses)
                {
                    string cmdLine = String.IsNullOrEmpty(process.CmdLine) ? process.GetProcessName(UsePrettyProcessName) : process.GetProcessName(UsePrettyProcessName) + " " + process.CommandLineNoExe;

                    double zeroS = GetZeroTimeInS(json.Extract);
                    if( !MinMaxStart.IsWithin( (process.StartTime-json.Extract.SessionStart).TotalSeconds-zeroS) )
                    {
                        continue; // process start time is outside of range
                    }

                    lret.Add(new MatchData
                    {
                        CmdLine = String.Intern(cmdLine),
                        ProcessWithPid = String.Intern(process.GetProcessWithId(UsePrettyProcessName)),
                        ProcessName = String.Intern(process.GetProcessName(UsePrettyProcessName)),
                        StartStopTags = GetProcessTags(process, extract.SessionStart),
                        ProcessId = process.ProcessID,
                        IsNewProcess = process.IsNew,
                        HasEnded = process.HasEnded,
                        PerformedAt = json.PerformedAt,
                        TestCase = json.TestName,
                        ReturnCode = process.ReturnCode,
                        ParentProcessId = process.ParentPid,
                        SessionId = process.SessionId,
                        User = process.Identity ?? "",
                        SourceFile = json.FileName,
                        StartTime = process.StartTime == DateTimeOffset.MinValue ? (DateTimeOffset?)null : process.StartTime.AddSeconds((-1.0d) * zeroS),
                        EndTime = (process.EndTime == DateTimeOffset.MaxValue || process.EndTime == DateTimeOffset.MinValue) ? (DateTimeOffset?)null : process.EndTime.AddSeconds((-1.0d) * zeroS),
                        LifeTime = (process.StartTime != DateTimeOffset.MinValue && process.EndTime != DateTimeOffset.MaxValue) ? (process.EndTime - process.StartTime) : null,
                        SessionStart = extract.SessionStart,
                        BaseLine = extract.MainModuleVersion?.ToString(),
                        ZeroTimeS = zeroS,
                    });
                }
            }

            return lret;
        }


        /// <summary>
        /// Dump an ETL file directly
        /// </summary>
        /// <param name="etlFile">Input etl file.</param>
        /// <returns>List of filtered processes.</returns>
        protected override List<MatchData> DumpETL(string etlFile)
        {
            List<MatchData> lret = new();
            using ITraceProcessor processor = TraceProcessor.Create(etlFile, new TraceProcessorSettings
            {
                AllowLostEvents = true,
                ToolkitPath = ETWAnalyzer.TraceProcessorHelpers.Extensions.GetToolkitPath()
            });

            IPendingResult<IProcessDataSource> processes = processor.UseProcesses();
            ITraceMetadata meta = processor.UseMetadata();
            processor.Process();

            ETWExtract extract = new();
            extract.SourceETLFileName = etlFile;
            extract.SessionStart = meta?.StartTime ?? DateTimeOffset.MinValue;
            TestDataFile testDataFile = new(Path.GetFileName(etlFile), etlFile, meta.StartTime.DateTime, 0, 0, "MachineNameNotKnown", null);
            testDataFile.Extract = new ETWExtract
            {
                Processes = processes.Result.Processes.Select(process => new ETWProcess 
                { 
                    CmdLine = String.IsNullOrEmpty(process.CommandLine) ? process.ImageName : process.CommandLine,
                    ProcessID = process.Id,
                    ProcessName = String.Intern(process.ImageName),
                    ParentPid = process.ParentId,
                    IsNew = process.CreateTime.HasValue,
                    HasEnded = process.ExitTime.HasValue,
                    ReturnCode = process.ExitCode,
#pragma warning disable CA1416
                    Identity = process.User.Value ?? "",
#pragma warning restore CA1416
                    SessionId = process.SessionId,
                    StartTime = process.CreateTime.HasValue ? process.CreateTime.Value.DateTimeOffset : DateTimeOffset.MinValue,
                    EndTime = process.ExitTime.HasValue ? process.ExitTime.Value.DateTimeOffset : DateTimeOffset.MaxValue,
                }).ToList(),
            };


            lret = DumpJson(testDataFile);

            return lret;
        }

        /// <summary>
        /// Get processes ReturnCode which corresponds to a NTStatus code. This can indicate exceptions and other abnormal program terminations.
        /// Additionally check if WerFault was called with -p ddd. All processes which have this id are then also printed out along with WerFault.exe invocations.
        /// </summary>
        /// <param name="data">Process List</param>
        /// <returns>Filtered list of crashed processes and WerFault invocations.</returns>
        List<MatchData> GetCrashedProcesses(List<MatchData> data)
        {
            List<MatchData> lret = data.Where(x => ETWProcess.IsPossibleCrash(x.ReturnCode)).ToList();
            int lastPid = 0;
            foreach(MatchData werDumpCall in data.Where(x => x.ProcessName == WerFault && x.CmdLine.Contains(" -p ")).OrderBy( x=>x.StartTime ) )
            {
                int start = werDumpCall.CmdLine.IndexOf(" -p ");
                if (start > -1)
                {
                    int stop = werDumpCall.CmdLine.IndexOf(' ', start + 4);
                    if (stop > -1)
                    {
                        string pid = werDumpCall.CmdLine.Substring(start + 4, stop - start - 4);
                        int crashPid = int.Parse(pid);
                        List<MatchData> crashList = data.Where(x => x.ProcessId == crashPid).ToList();
                        foreach (var crash in crashList)
                        {
                            if (!lret.Contains(crash)) // do not add process multiple times if it has caused several WerFault invocations
                            {
                                lret.Add(crash);
                            }
                        }

                        if (lastPid != crashPid) // only show the first WerDump call. Normally WerFault is called two times. 
                        {
                            lret.Add(werDumpCall);
                        }
                        lastPid = crashPid;
                    }
                }
            }


            lret = lret.OrderBy(x=>x.PerformedAt).ThenBy(x=>x.EndTime).ThenBy(x=>x.StartTime).ToList();

            return lret;
        }

        private void WriteToCSV(List<MatchData> rowData)
        {
            OpenCSVWithHeader(Col_CSVOptions, Col_TestCase, "TestDate", Col_ProcessName, "ProcessName(pid)", "Parent ProcessId", "Session Id", "Return Code", "NewProcess", "Start Time", "End Time", "LifeTime in minutes", "User", Col_CommandLine, Col_Baseline, "SourceFile");
            foreach (var data in rowData)
            {
                WriteCSVLine(CSVOptions, data.TestCase, data.PerformedAt, data.ProcessName, data.ProcessWithPid, data.ParentProcessId, data.SessionId, ETWProcess.GetReturnString(data.ReturnCode, out bool bCrash), Convert.ToInt32(data.IsNewProcess),
                            GetDateTimeString(data.StartTime, data.SessionStart, TimeFormatOption), GetDateTimeString(data.EndTime, data.SessionStart, TimeFormatOption), GetDurationInMinutes(data.LifeTime), data.User, 
                            data.CmdLine, data.BaseLine, data.SourceFile);
            }
        }


        string GetDurationInMinutes(TimeSpan? nullable)
        {
            return nullable.HasValue ? $"{nullable.Value.TotalMinutes:F0}" : "";
        }

        bool ProcessFilter(ETWProcess process)
        {
            bool lret =
            (process.ProcessName != null) &&
            (ProcessNameFilter(process.GetProcessName(UsePrettyProcessName)) ||        // filter by process name like cmd.exe and with pid like cmd.exe(100)
             ProcessNameFilter(process.GetProcessWithId(UsePrettyProcessName))
            ) &&
            CommandLineFilter(process.CmdLine) &&
            process.IsMatch(NewProcessFilter)   // If new process filter is set check flags
            ;

            return lret;
        }

        /// <summary>
        /// Check if current process has a parent process which matches the Parent Filter. 
        /// </summary>
        /// <param name="process">child process to check</param>
        /// <param name="all">Full list of processes</param>
        /// <param name="parents">If parent process was found it is added to list of known parents.</param>
        /// <returns>true if process passes <see cref="Parent"/> filter, false otherwise.</returns>
        internal bool ParentFilter(ETWProcess process, IReadOnlyList<ETWProcess> all, HashSet<ETWProcess> parents)
        {
            ETWProcess parent = all.FirstOrDefault(x => process.ParentPid == x.ProcessID && process.StartTime >= x.StartTime && process.EndTime <= x.EndTime);

            bool lret = Parent(parent?.GetProcessName(UsePrettyProcessName)) ||   // filter by process name like cmd.exe and with pid like cmd.exe(100)
                        Parent(parent?.GetProcessWithId(UsePrettyProcessName));

            if( lret && parent != null)
            {
                parents.Add(parent);
            }

            return lret;
        }

        internal bool SessionIdFilter(ETWProcess process)
        {
            bool lret =
                (Session(process.SessionId.ToString()));
            return lret;
        }

        public class MatchData : IEquatable<MatchData>
        {
            /// <summary>
            /// Process Start time
            /// </summary>
            public DateTimeOffset? StartTime { get; set; }

            /// <summary>
            /// Process Exit time
            /// </summary>
            public DateTimeOffset? EndTime { get; set; }

            /// <summary>
            /// Process duration
            /// </summary>
            public TimeSpan? LifeTime { get; set; }

            /// <summary>
            /// Parent Process, only filled after a call to <see cref="ConvertToTree(List{MatchData})"/>
            /// </summary>
            public MatchData Parent { get; set; }

            /// <summary>
            /// Child processes only filled after a call to  <see cref="ConvertToTree(List{MatchData})"/>
            /// </summary>
            public List<MatchData> Childs { get; set; } = new();

            public ETWProcess Process { get; set; }

            /// <summary>
            /// Pretty print of process lifetime. t ?lt; 60s => total seconds, t>60s &lt; 10minutes mm:ss, t>10min mm no seconds
            /// </summary>
            public string LifeTimeString
            {
                get
                {
                    string lifeTime = "";
                    if (LifeTime != null)
                    {
                        if (LifeTime.Value.TotalSeconds < 60)
                        {
                            lifeTime = $"{LifeTime.Value.TotalSeconds:F0} s";
                        }
                        else if (LifeTime.Value.TotalMinutes < 10)
                        {
                            lifeTime = $"{((int)LifeTime.Value.TotalMinutes):00}:{LifeTime.Value.Seconds:00} min";
                        }
                        else
                        {
                            lifeTime = $"{LifeTime.Value.TotalMinutes:F0} min";
                        }
                    }

                    return lifeTime;
                }
            }
			
            /// <summary>
            /// Process return code
            /// </summary>
            public int? ReturnCode;


            /// <summary>
            /// Return code as string or exception code if it is matching the well known ones. 
            /// </summary>
            public string ReturnCodeString
            {
                get
                {
                    return ETWProcess.GetReturnString(ReturnCode, out bool _);
                }
            }


            /// <summary>
            /// Input Json file
            /// </summary>
            public string SourceFile;

            /// <summary>
            /// Test case file name
            /// </summary>
            public string TestCase;

            /// <summary>
            /// Test time
            /// </summary>
            public DateTimeOffset PerformedAt;

            /// <summary>
            /// Is new process when true which was started during session.
            /// </summary>
            public bool IsNewProcess;

            /// <summary>
            /// Full process name
            /// </summary>
            public string ProcessWithPid;

            /// <summary>
            /// Parent process id
            /// </summary>
            public int ParentProcessId { get; set; }

            /// <summary>
            /// Windows Session Id
            /// </summary>
            public int SessionId { get; set; }

            /// <summary>
            /// Process Id
            /// </summary>
            internal int ProcessId { get; set; }

            /// <summary>
            /// User SID or translated SID when extraction was done on generating machine or it was a well known SID.
            /// </summary>
            internal string User { get; set; }



            private string myCmdLine;

            /// <summary>
            /// Process Command Line
            /// </summary>
            public string CmdLine 
            { 
                get => myCmdLine; 
                set => myCmdLine = String.Intern(value ?? ""); // speed up string comparisons 
            }


            private string processName;

            /// <summary>
            /// Process Name,
            /// </summary>
            public string ProcessName 
            {
                get => processName; 
                set => processName = String.Intern(value ?? ""); 
            }

            /// <summary>
            /// Process did exit during ETW recording
            /// </summary>
            public bool HasEnded { get; internal set; }


            /// <summary>
            /// ETW Trace session start time
            /// </summary>
            public DateTimeOffset SessionStart { get; internal set; }

            /// <summary>
            /// -zt Time shift 
            /// </summary>
            public double ZeroTimeS { get; internal set; }

            /// <summary>
            /// Software baseline
            /// </summary>
            public string BaseLine { get; internal set; }

            public static List<MatchData> ConvertToTree(List<MatchData> data)
            {
                // Create a dictionary to quickly look up nodes by ProcessId
                HashSet<MatchData> visited = new();


                foreach(var process in data)
                {
                    foreach(var any in data)
                    {
                        if( any.IsParent(process))
                        {
                            any.Childs.Add(process);
                            process.Parent = any;
                            break;
                        }
                    }
                }

                // sort by file ( SessionStart) then by session and then by name
                List<MatchData> roots = data.Where(x=>x.Parent == null).OrderBy(x => x.SessionStart).ThenBy(x => x.SessionId).ThenBy(x => x.ProcessName).ToList();

                return roots;
            }



            string myProcessKey;

            /// <summary>
            /// Unique process key which consists of process name, id and start time
            /// </summary>
            public string ProcessKey
            {
                get
                {
                    if( myProcessKey== null)
                    {
                        myProcessKey = ProcessName + ProcessId + StartTime?.Ticks;
                    }
                    return myProcessKey;
                }
            }

            /// <summary>
            /// Process start/stop tags +- or actual start/stop time in various formats controlled with -processfmt switch
            /// </summary>
            public string StartStopTags { get; internal set; }

            /// <summary>
            /// Checks if other process can be a child of current MatchData instance.
            /// </summary>
            /// <param name="possibleChild">Child to check agains current instance.</param>
            /// <returns>true if this instance is a parent of possibleChild process, false otherwise.</returns>
            bool IsParent(MatchData possibleChild)
            {
                if (Object.ReferenceEquals(this, possibleChild))
                {
                    return false;
                }

                return this.ProcessId == possibleChild.ParentProcessId &&
                       this.SourceFile == possibleChild.SourceFile &&
                       this.SessionId == possibleChild.SessionId &&
                       IsWithinLifeTime(possibleChild.StartTime ?? DateTimeOffset.MinValue, possibleChild.EndTime ?? DateTime.MaxValue);
            }


            bool IsWithinLifeTime(DateTimeOffset childStart, DateTimeOffset childEnd)
            {
                DateTimeOffset start = StartTime ?? DateTimeOffset.MinValue;
                DateTimeOffset end = EndTime ?? DateTimeOffset.MaxValue;
                return childStart >= start && childStart < end;
            }

           public bool IsMatch(ProcessStates? filter)
            {
                bool lret = filter switch
                {
                    null => true,
                    ProcessStates.None =>         !HasEnded && !IsNewProcess,
                    ProcessStates.Started =>                    IsNewProcess,
                    ProcessStates.Stopped =>       HasEnded,
                    ProcessStates.OnlyStopped =>   HasEnded && !IsNewProcess,
                    ProcessStates.OnlyStarted =>  !HasEnded &&  IsNewProcess,
                    _ => false,
                };

                return lret;
            }

            public override int GetHashCode()
            {
                // To combine hash codes from different fields see
                // https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
                int hash = 17 * 31 + StartTime.GetValueOrDefault().GetHashCode();
                hash = hash * 31 + EndTime.GetValueOrDefault().GetHashCode();
                hash = hash * 31 + LifeTime.GetValueOrDefault().GetHashCode();
                hash = hash * 31 + (CmdLine ?? "").GetHashCode();
                hash = hash * 31 + ProcessWithPid.GetHashCode();
                hash = hash * 31 + ParentProcessId;
                hash = hash * 31 + SessionId;
                return hash;
            }

            public bool Equals(MatchData other)
            {
                return other.StartTime == StartTime &&
                       other.EndTime == EndTime &&
                       other.LifeTime == LifeTime &&
                       other.ReturnCode == ReturnCode &&
                       other.CmdLine == CmdLine &&
                       other.IsNewProcess == IsNewProcess &&
                       other.ProcessWithPid == ProcessWithPid &&
                       other.ParentProcessId == ParentProcessId &&
                       other.SessionId == SessionId;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as MatchData);
            }

            public override string ToString()
            {
                return $"{ProcessName}({ProcessId}) Start: {StartTime ?? DateTime.MaxValue} End: {EndTime ?? DateTime.MaxValue} CmdLine: {CmdLine} TestCase: {TestCase}";
            }
        }
    }
}
