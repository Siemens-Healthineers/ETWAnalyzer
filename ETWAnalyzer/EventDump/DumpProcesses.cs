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
using System.Text;
using System.Threading.Tasks;
using static ETWAnalyzer.Extract.ETWProcess;

namespace ETWAnalyzer.EventDump
{
    class DumpProcesses : DumpFileEtlBase<DumpProcesses.MatchData>
    {
        public bool ShowFileOnLine { get; internal set; }
        public bool ShowAllProcesses { get; internal set; }

        public bool Crash { get; internal set; }
        public DumpCommand.SortOrders SortOrder { get; internal set; }
        public bool Merge { get; internal set; }
        public bool NoCmdLine { get; internal set; }
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


                string str = $"PID: [yellow]{m.ProcessId,-6}[/yellow] Start: [green]{startTime.WithWidth(startTimeWidth)}[/green] Stop: [darkcyan]{stopTime.WithWidth(stopTimeWidth)} Duration: {m.LifeTimeString.WithWidth(lifeTimeWidth)}[/darkcyan] RCode: {m.ReturnCodeString.WithWidth(returnCodeWidth)} Parent: {m.ParentProcessId,5} [yellow]{user}[/yellow]{cmdLine} {fileName}";
                ColorConsole.WriteEmbeddedColorLine(str);
            }
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

        protected override List<MatchData> DumpJson(TestDataFile json)
        {
            List<MatchData> lret = new();

            if( json?.Extract?.Processes == null || json.Extract.Processes.Count == 0)
            {
                ColorConsole.WriteError($"No process data present in file {json.FileName}");
                return lret;
            }

            IETWExtract extract = json.Extract;

            // order process starts by process name and group them by exe
            foreach (var processGroup in extract.Processes.GroupBy(x => x.GetProcessName(UsePrettyProcessName)).OrderBy(x => x.Key))
            {
                // then order by start time and if not present by process id
                foreach (var process in processGroup.OrderBy(x => x.StartTime).ThenBy(x => x.ProcessID).Where(ProcessFilter))
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
                        ProcessId = process.ProcessID,
                        IsNewProcess = process.IsNew,
                        HasEnded = process.HasEnded,
                        PerformedAt = json.PerformedAt,
                        TestCase = json.TestName,
                        ReturnCode = process.ReturnCode,
                        ParentProcessId = process.ParentPid,
                        User = process.Identity ?? "",
                        SourceFile = json.FileName,
                        StartTime = process.StartTime == DateTimeOffset.MinValue ? (DateTimeOffset?)null : process.StartTime.AddSeconds((-1.0d)*zeroS),
                        EndTime = (process.EndTime == DateTimeOffset.MaxValue || process.EndTime == DateTimeOffset.MinValue ) ? (DateTimeOffset?)null : process.EndTime.AddSeconds((-1.0d)*zeroS),
                        LifeTime = (process.StartTime != DateTimeOffset.MinValue && process.EndTime != DateTimeOffset.MaxValue) ? (process.EndTime - process.StartTime) : null,
                        SessionStart = extract.SessionStart,
                        BaseLine = extract.MainModuleVersion?.ToString(),
                        ZeroTimeS = zeroS,
                    });
                }
            }

            return lret;
        }

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

            // order process starts by process name and group them by exe
            foreach (var processGroup in processes.Result.Processes.GroupBy(x => x.ImageName).OrderBy(x => x.Key))
            {
                // then order by start time and if not present by process id
                foreach (var process in processGroup.OrderBy(x => x.CreateTime).ThenBy(x => x.Id).Where(ProcessFilter))
                {
                    string cmdLine = String.IsNullOrEmpty(process.CommandLine) ? process.ImageName : process.CommandLine;
                    string ret = process.ExitCode.HasValue ? process.ExitCode.Value.ToString(CultureInfo.InvariantCulture) : "";
                    var data = new MatchData
                    {
                        CmdLine = String.Intern(cmdLine),
                        ProcessWithPid = String.Intern($"{process.ImageName}({process.Id})"),
                        ProcessName = String.Intern(process.ImageName),
                        ProcessId = process.Id,
                        IsNewProcess = process.CreateTime.HasValue,
                        HasEnded = process.ExitTime.HasValue,
                        PerformedAt = meta.StartTime,
                        TestCase = Path.GetFileName(etlFile),
                        ReturnCode = process.ExitCode,
#pragma warning disable CA1416
                        User = process.User.Value ?? "",
#pragma warning restore CA1416
                        ParentProcessId = process.ParentId,
                        SourceFile = etlFile,
                        StartTime = process.CreateTime.HasValue ? process.CreateTime.Value.DateTimeOffset : (DateTimeOffset?)null,
                        EndTime = process.ExitTime.HasValue ? process.ExitTime.Value.DateTimeOffset : (DateTimeOffset?)null,
                        LifeTime = (process.CreateTime != null && process.ExitTime != null) ? (process.ExitTime.Value.DateTimeOffset - process.CreateTime.Value.DateTimeOffset) : null,
                        SessionStart = meta?.StartTime ?? DateTimeOffset.MinValue,
                    };

                    if (MinMaxStart.IsWithin((data.StartTime.GetValueOrDefault() - data.SessionStart).TotalSeconds)) // we do not support zerotime filtering for etl files
                    {
                        lret.Add(data);
                    }

                }
            }

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


            return lret;
        }

        private void WriteToCSV(List<MatchData> rowData)
        {
            OpenCSVWithHeader(Col_CSVOptions, Col_TestCase, "TestDate", Col_ProcessName, "ProcessName(pid)", "Parent ProcessId", "Return Code", "NewProcess", "Start Time", "End Time", "LifeTime in minutes", "User", Col_CommandLine, Col_Baseline, "SourceFile");
            foreach (var data in rowData)
            {
                WriteCSVLine(CSVOptions, data.TestCase, data.PerformedAt, data.ProcessName, data.ProcessWithPid, data.ParentProcessId, ETWProcess.GetReturnString(data.ReturnCode, out bool bCrash), Convert.ToInt32(data.IsNewProcess),
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

        bool ProcessFilter(IProcess process)
        {
            bool isNew = process.CreateTime.HasValue;

            bool lret =
             (process.ImageName != null) && // etl can have partial events with empty image names
             (process.ImageName != "conhost.exe") &&
             (ProcessNameFilter(process.ImageName) ||   // filter by process name like cmd.exe and with pid like cmd.exe(100)
              ProcessNameFilter($"{process.ImageName}({process.Id})")
             ) &&
             CommandLineFilter(process.CommandLine) &&
             process.IsMatch(NewProcessFilter)   // If new process filter is set check flags
            ;

            return lret;
        }


        public class MatchData : IEquatable<MatchData>
        {
            public DateTimeOffset? StartTime;
            public DateTimeOffset? EndTime;
            public TimeSpan? LifeTime;
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
			
            public int? ReturnCode;

            public string ReturnCodeString
            {
                get
                {
                    return ETWProcess.GetReturnString(ReturnCode, out bool _);
                }
            }

            private string cmdLine;
            public string SourceFile;
            public string TestCase;
            public DateTimeOffset PerformedAt;
            public bool IsNewProcess;
            private string processName;
            public string ProcessWithPid;
            public int ParentProcessId;
            internal int ProcessId;
            internal string User;



            public string CmdLine 
            { 
                get => cmdLine; 
                set => cmdLine = String.Intern(value ?? ""); // speed up string comparisons 
            }
            public string ProcessName 
            {
                get => processName; 
                set => processName = String.Intern(value ?? ""); 
            }
            public bool HasEnded { get; internal set; }
            public DateTimeOffset SessionStart { get; internal set; }
            public double ZeroTimeS { get; internal set; }
            public string BaseLine { get; internal set; }

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
                       other.ParentProcessId == ParentProcessId;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as MatchData);
            }

            public override string ToString()
            {
                return $"{ProcessWithPid} Start: {StartTime ?? DateTime.MaxValue} End: {EndTime ?? DateTime.MaxValue} CmdLine: {CmdLine} TestCase: {TestCase}";
            }
        }
    }
}
