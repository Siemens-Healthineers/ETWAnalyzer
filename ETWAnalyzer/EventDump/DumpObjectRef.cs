using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Handle;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.EventDump
{
    class DumpObjectRef : DumpFileDirBase<DumpObjectRef.MatchData>
    {
        internal class MatchData
        {
            public ObjectRefTrace ObjTrace { get; set; }

            public StackCollection Stacks { get; set; } 
            public IProcessExtract ProcessExtract { get; set; }
            public int MaxRefCount { get; internal set; }
        }

        /// <summary>
        /// Unit testing only. ReadFileData will return this list instead of real data
        /// </summary>
        internal List<MatchData> myUTestData = null;

        public KeyValuePair<string, Func<string, bool>> StackFilter { get; internal set; }
        public MinMaxRange<double> MinMaxDurationS { get; internal set; } = new MinMaxRange<double>();
        public KeyValuePair<string, Func<string, bool>> DestroyStackFilter { get; internal set; }
        public KeyValuePair<string, Func<string, bool>> HandleNameFilter { get; internal set; }
        public bool ShowStack { get; internal set; }
        public bool ShowRef { get; internal set; }
        public bool Leak { get; internal set; }

        /// <summary>
        /// Get handles which are opened from multiple processes.
        /// </summary>
        public bool MultiProcess { get; internal set; }

        /// <summary>
        /// Get only handles which have overlapping lifetime e.g. if duplicated or already existing handles are opened again.
        /// </summary>
        public bool Overlapped { get; internal set; }
        public KeyValuePair<string, Func<string, bool>> ObjectFilter { get; internal set; }
        public KeyValuePair<string, Func<string, bool>> HandleFilter { get; internal set; }
        public KeyValuePair<string, Func<string, bool>> RelatedProcessFilter { get; internal set; }
        public int? Map { get; internal set; }
        public KeyValuePair<string, Func<string, bool>> ViewBaseFilter { get; internal set; }
        public long? PtrInMap { get; internal set; }

        public override List<MatchData> ExecuteInternal()
        {
            List<MatchData> lret = ReadFileData();
            if (IsCSVEnabled)
            {
                //OpenCSVWithHeader(Col_CSVOptions, "Directory", Col_FileName, Col_Date, Col_TestCase, Col_TestTimeinms, Col_Baseline, Col_Process, Col_ProcessName,
                //                  "DNS Query", "Query StatusCode", "TimedOut", Col_StartTime, "Duration in s", "Queried Network Adapters",
                //                  "Server List", "DNS Result", Col_CommandLine);

                //foreach (var dnsEvent in lret)
                //{
                //    WriteCSVLine(CSVOptions, Path.GetDirectoryName(dnsEvent.File.FileName),
                //        Path.GetFileNameWithoutExtension(dnsEvent.File.FileName), dnsEvent.File.PerformedAt, dnsEvent.File.TestName, dnsEvent.File.DurationInMs, dnsEvent.Baseline,
                //        dnsEvent.Process.ProcessWithID, dnsEvent.Process.ProcessNamePretty,
                //        dnsEvent.Dns.Query, dnsEvent.Dns.QueryStatus, dnsEvent.Dns.TimedOut, GetDateTimeString(dnsEvent.Dns.Start, dnsEvent.SessionStart, TimeFormatOption, false), dnsEvent.Dns.Duration.TotalSeconds,
                //        dnsEvent.Dns.Adapters, dnsEvent.Dns.ServerList, dnsEvent.Dns.Result,
                //        NoCmdLine ? "" : dnsEvent.Process.CommandLineNoExe);
                //}
            }
            else
            {
                PrintMatches(lret);
            }

            return lret;
        }

        bool MatchRelatedProcess(ObjectRefTrace trace, IProcessExtract resolver)
        {
            ETWProcessIndex creator = ETWProcessIndex.Invalid;
            if( trace.CreateEvent != null )
            {
                creator = trace.CreateEvent.ProcessIndex;
            }

            foreach(var ev in trace?.HandleCreateEvents)
            {
                if( ev.ProcessIndex != creator )
                {
                    // filter by process name with pid like cmd.exe(100)
                    if (!RelatedProcessFilter.Value(resolver.GetProcess(ev.ProcessIndex).GetProcessWithId(UsePrettyProcessName)))
                    {
                        return false;
                    }
                }
            }

            foreach (var ev in trace?.HandleCloseEvents)
            {
                if (ev.ProcessIndex != creator)
                {
                    // filter by process name with pid like cmd.exe(100)
                    if (!RelatedProcessFilter.Value(resolver.GetProcess(ev.ProcessIndex).GetProcessWithId(UsePrettyProcessName)))
                    {
                        return false;
                    }
                }
            }

            foreach (var ev in trace?.HandleDuplicateEvents)
            {
                if (ev.ProcessIndex != creator)
                {
                    // filter by process name with pid like cmd.exe(100)
                    if (!RelatedProcessFilter.Value(resolver.GetProcess(ev.ProcessIndex).GetProcessWithId(UsePrettyProcessName)))
                    {
                        return false;
                    }
                }
            }

            foreach (var ev in trace.RefChanges)
            {
                if (ev.ProcessIndex != creator)
                {
                    // filter by process name with pid like cmd.exe(100)
                    if (!RelatedProcessFilter.Value(resolver.GetProcess(ev.ProcessIndex).GetProcessWithId(UsePrettyProcessName)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private List<MatchData> ReadFileData()
        {
            if (myUTestData != null)
            {
                return myUTestData;
            }

            var lret = new List<MatchData>();

            Lazy<SingleTest>[] runData = GetTestRuns(true, SingleTestCaseFilter, TestFileFilter);
            WarnIfNoTestRunsFound(runData);
            foreach (var test in runData)
            {
                foreach (TestDataFile file in test.Value.Files)
                {
                    if (file?.Extract?.HandleData?.ObjectReferences?.Count == null)
                    {
                        ColorConsole.WriteError($"Warning: File {GetPrintFileName(file.FileName)} does not contain ObjectRef or Handle tracing data.");
                        continue;
                    }

                    var stacks = file?.Extract?.HandleData?.Stacks;

                    foreach (ObjectRefTrace handle in file.Extract.HandleData.ObjectReferences)
                    {
                        handle.RefreshCollectionsAfterDeserialize();

                        if( handle.CreateEvent != null )
                        {
                            if (!IsMatchingProcessAndCmdLine(file, file.Extract.GetProcess(handle.CreateEvent.ProcessIndex).ToProcessKey()))
                            {
                              continue;
                            } 
                        }

                        if( PtrInMap != null && handle.IsFileMap && handle.FileMapEvents.Count > 0)
                        {
                            long start = handle.FileMapEvents[0].ViewBase;
                            long end = start + handle.FileMapEvents[0].ViewSize;
                            if ( !(start <= PtrInMap && PtrInMap <= end) )
                            {
                                continue;
                            }
                        }

                        if(!MatchRelatedProcess(handle, file.Extract))
                        {
                            continue;
                        }

                        if ( Leak && !handle.IsLeaked)
                        {
                            continue;
                        }

                        if( !HandleNameFilter.Value(handle.Name) )
                        {
                            continue;
                        }

                        if( !ObjectFilter.Value("0x"+handle.ObjectPtr.ToString("X")))
                        {
                            continue;
                        }

                        if (handle.IsFileMap && handle.FileMapEvents.Count > 0)
                        {
                            if (!ViewBaseFilter.Value("0x" + handle.FileMapEvents[0].ViewBase.ToString("X")))
                            {
                                continue;
                            }
                        }

                        if( !handle.IsFileMap &&
                            !handle.HandleCreateEvents.Any( h =>  HandleFilter.Value("0x"+h.HandleValue.ToString("X")) ) &&
                            !handle.HandleDuplicateEvents.Any(h => HandleFilter.Value("0x" + h.HandleValue.ToString("X"))))
                        {
                            continue;
                        }

                        if( MultiProcess && !handle.IsMultiProcess )
                        {
                            continue;
                        }

                        if (StackFilter.Key != null && handle.CreateEvent != null)
                        {
                            if (!StackFilter.Value(stacks?.GetStack(handle.CreateEvent.StackIdx)))
                            {
                                continue;
                            }
                        }

                        if (DestroyStackFilter.Key != null && handle.DestroyEvent != null)
                        {
                            if (!DestroyStackFilter.Value(stacks?.GetStack(handle.DestroyEvent.StackIdx)))
                            {
                                continue;
                            }
                        }

                        if (!MinMaxDurationS.IsWithin(handle.Duration.TotalSeconds))
                        {
                            continue;
                        }

                        if(Overlapped && !handle.GetIsOverlapped())
                        {
                            continue;
                        }

                        int maxRefCount = 0;
                        int currentRefCount = 0;
                        foreach (var change in handle.RefChanges)
                        {
                            currentRefCount += change.RefCountChange;
                            maxRefCount = Math.Max(maxRefCount, currentRefCount);
                        }

                        lret.Add(new MatchData
                        {
                            ObjTrace = handle,
                            ProcessExtract = file.Extract,
                            Stacks = file.Extract.HandleData.Stacks,
                            MaxRefCount = maxRefCount,
                        });
                    }
                }
            }

            return lret;
        }

        string GetStack(StackCollection stacks, StackIdx stackIdx)
        {
            if( ShowStack )
            {
                return stacks.GetStack(stackIdx);
            }
            else
            {
                return stackIdx.ToString(); 
            }
        }

        private void PrintMatches(List<MatchData> matches)
        {
            foreach (var ev in matches)
            {
                if( ev.ObjTrace.IsFileMap)
                {
                    if (Map == null || Map == 1)
                    {
                        PrintMapEvent(ev);
                    }
                }
                else
                {
                    if (Map == null || Map == 0)
                    {
                        PrintObjectEvent(ev);
                    }
                }
            }
        }

        private void PrintObjectEvent(MatchData ev)
        {
            Console.WriteLine($"Object: 0x{ev.ObjTrace.ObjectPtr:X} {ev.ObjTrace.Name} Lifetime: {ev.ObjTrace.Duration.TotalSeconds:F6} s  " +
                              $"Create+Duplicate-Close: {ev.ObjTrace.HandleCreateEvents.Count}+{ev.ObjTrace.HandleDuplicateEvents.Count}-{ev.ObjTrace.HandleCloseEvents.Count} = {ev.ObjTrace.HandleCreateEvents.Count + ev.ObjTrace.HandleDuplicateEvents.Count - ev.ObjTrace.HandleCloseEvents.Count}");

            foreach (HandleCreateEvent handleCreate in ev.ObjTrace.HandleCreateEvents)
            {
                Console.WriteLine($"\t{FormatTime(handleCreate.Time)} Handle Created 0x{handleCreate.HandleValue:X} {ev.ProcessExtract.GetProcess(handleCreate.ProcessIndex).GetProcessWithId(true)}/{handleCreate.ThreadId} Stack: {GetStack(ev.Stacks, handleCreate.StackIdx)}");
            }

            foreach (HandleDuplicateEvent handleduplicate in ev.ObjTrace.HandleDuplicateEvents)
            {
                Console.WriteLine($"\t{FormatTime(handleduplicate.Time)} Handle Duplicate 0x{handleduplicate.HandleValue:X} {ev.ProcessExtract.GetProcess(handleduplicate.ProcessIndex).GetProcessWithId(true)}/{handleduplicate.ThreadId} SourceProcess: {ev.ProcessExtract.GetProcess(handleduplicate.SourceProcessIdx).GetProcessWithId(true)} SourceHandle: 0x{handleduplicate.SourceHandleValue:X} Stack: {GetStack(ev.Stacks, handleduplicate.StackIdx)}");
            }


            if (!Leak)
            {
                foreach (HandleCloseEvent handleClose in ev.ObjTrace.HandleCloseEvents)
                {
                    Console.WriteLine($"\t{FormatTime(handleClose.Time)} Handle Closed 0x{handleClose.HandleValue:X} {ev.ProcessExtract.GetProcess(handleClose.ProcessIndex).GetProcessWithId(true)}/{handleClose.ThreadId} Stack: {GetStack(ev.Stacks, handleClose.StackIdx)}");
                }
            }

            if (ShowRef)
            {
                int currentRefCount = 0;
                foreach (RefCountChangeEvent change in ev.ObjTrace.RefChanges)
                {
                    currentRefCount += change.RefCountChange;
                    Console.WriteLine($"\t{FormatTime(change.Time)} {currentRefCount} change: {change.RefCountChange} {ev.ProcessExtract.GetProcess(change.ProcessIndex).GetProcessWithId(true)}/{change.ThreadId} Stack: {GetStack(ev.Stacks, change.StackIdx)}");
                }
            }

            if (ev.ObjTrace.DestroyEvent != null)
            {
                Console.WriteLine($"\t{FormatTime(ev.ObjTrace.DestroyEvent.Time)} Object Deleted {ev.ProcessExtract.GetProcess(ev.ObjTrace.DestroyEvent.ProcessIndex).GetProcessWithId(true)}/{ev.ObjTrace.DestroyEvent.ThreadId} ------------------------------- {GetStack(ev.Stacks, ev.ObjTrace.DestroyEvent.StackIdx)}");
            }
        }

        private void PrintMapEvent(MatchData ev)
        {
            Console.WriteLine($"MappingObject: 0x{ev.ObjTrace.ObjectPtr:X} Lifetime: {ev.ObjTrace.Duration.TotalSeconds:F6} s");
            foreach (var fileMapEvent in ev.ObjTrace.FileMapEvents)
            {
                Console.WriteLine($"\t{FormatTime(fileMapEvent.Time)} Map to 0x{fileMapEvent.ViewBase:X}-0x{fileMapEvent.ViewBase + fileMapEvent.ViewSize:X} Size: {fileMapEvent.ViewSize} bytes, {ev.ProcessExtract.GetProcess(fileMapEvent.ProcessIndex).GetProcessWithId(true)}/{fileMapEvent.ThreadId} Offset: {fileMapEvent.ByteOffset} Stack: {GetStack(ev.Stacks, fileMapEvent.StackIdx)}");
            }

            foreach (var fileUnmapEvent in ev.ObjTrace.FileUnmapEvents)
            {
                Console.WriteLine($"\t{FormatTime(fileUnmapEvent.Time)} Unmap of 0x{fileUnmapEvent.ViewBase:X} {ev.ProcessExtract.GetProcess(fileUnmapEvent.ProcessIndex).GetProcessWithId(true)}/{fileUnmapEvent.ThreadId} Stack: {GetStack(ev.Stacks, fileUnmapEvent.StackIdx)}");
            }
        }

        string FormatTime(DateTimeOffset time)
        {
            return time.ToString("HH:mm:ss.ffffff");
        }
    }
}
