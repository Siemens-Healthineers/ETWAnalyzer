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
            public TestDataFile File { get; set; }
        }

        /// <summary>
        /// Unit testing only. ReadFileData will return this list instead of real data
        /// </summary>
        internal List<MatchData> myUTestData = null;

        public KeyValuePair<string, Func<string, bool>> StackFilter { get; internal set; }
        public MinMaxRange<double> MinMaxDurationS { get; internal set; } = new MinMaxRange<double>();
        public KeyValuePair<string, Func<string, bool>> DestroyStackFilter { get; internal set; }
        public KeyValuePair<string, Func<string, bool>> ObjectNameFilter { get; internal set; }
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
        public MinMaxRange<long> MinMaxMapSize { get; internal set; }

        public override List<MatchData> ExecuteInternal()
        {
            HarmonizeFilterSettings();

            List<MatchData> lret = ReadFileData();
            if (IsCSVEnabled)
            {
                OpenCSVWithHeader(Col_CSVOptions, Col_FileName, Col_Date, Col_TestCase, Col_TestTimeinms, Col_Baseline, Col_Process, Col_ProcessName,
                                  Col_StartTime, Col_CommandLine);



                foreach (var objectEvent in lret)
                {
                    //WriteCSVLine(CSVOptions, Path.GetFileNameWithoutExtension(objectEvent.File.FileName), objectEvent.File.PerformedAt, objectEvent.File.TestName, objectEvent.File.DurationInMs, 
                    //    objectEvent.Process.ProcessWithID, objectEvent.Process.ProcessNamePretty,
                    //    objectEvent.Dns.Query, objectEvent.Dns.QueryStatus, objectEvent.Dns.TimedOut, GetDateTimeString(objectEvent.Dns.Start, objectEvent.SessionStart, TimeFormatOption, false), objectEvent.Dns.Duration.TotalSeconds,
                    //    objectEvent.Dns.Adapters, objectEvent.Dns.ServerList, objectEvent.Dns.Result,
                    //    NoCmdLine ? "" : objectEvent.Process.CommandLineNoExe);
                }
            }
            else
            {
                PrintMatches(lret);
            }

            return lret;
        }

        private void HarmonizeFilterSettings()
        {
            if (HandleFilter.Key != null || ObjectNameFilter.Key != null) // disable map events when handle specific filters are enabled
            {
                Map = 0;
            }

            // turn off handle events when a file mapping filter is enabled.
            if (PtrInMap != null || !MinMaxMapSize.IsDefault || ViewBaseFilter.Key != null)
            {
                Map = 1;
            }
        }

        string GetProcessWithId(ETWProcessIndex procIdx, IProcessExtract resolver)
        {
            return resolver.GetProcess(procIdx).GetProcessWithId(UsePrettyProcessName);
        }

        bool MatchCreatingOrUsingProcess(ObjectRefTrace trace, IProcessExtract resolver)
        {
            bool lret = false;
            ETWProcessIndex creator = ETWProcessIndex.Invalid;
            if (trace.CreateEvent != null)
            {
                creator = trace.CreateEvent.ProcessIndex;
                lret = ProcessNameFilter(GetProcessWithId(creator, resolver));
            }

            if( !lret && trace.DestroyEvent != null )
            {
                string destroyName = GetProcessWithId(trace.DestroyEvent.ProcessIndex, resolver);
                lret = ProcessNameFilter(destroyName);
            }

            if (!lret)
            {
                lret = trace.HandleCreateEvents.Any(x => ProcessNameFilter(GetProcessWithId(x.ProcessIndex, resolver)));
            }

            if(!lret)
            {
                lret = trace.HandleCloseEvents.Any(x => ProcessNameFilter(GetProcessWithId(x.ProcessIndex, resolver)));
            }

            if (!lret)
            {
                lret = trace.HandleDuplicateEvents.Any(x => ProcessNameFilter(GetProcessWithId(x.ProcessIndex, resolver)));
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

            foreach( var ev in trace?.HandleCreateEvents )
            {
                if( ev.ProcessIndex != creator )
                {
                    // filter by process name with pid like cmd.exe(100)
                    if (!RelatedProcessFilter.Value( GetProcessWithId(ev.ProcessIndex, resolver)) )
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
                    if (!RelatedProcessFilter.Value( GetProcessWithId(ev.ProcessIndex, resolver)) )
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
                    if (!RelatedProcessFilter.Value( GetProcessWithId(ev.ProcessIndex, resolver)) )
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
                    if (!RelatedProcessFilter.Value( GetProcessWithId(ev.ProcessIndex, resolver)) )
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        bool IsHandleMatch(ulong handleValue)
        {
            return HandleFilter.Value("0x" + handleValue.ToString("X"));
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

                        // print either object or file mapping events. Default is to print both
                        if ( (Map == 0 && handle.IsFileMap) ||
                             (Map == 1 && !handle.IsFileMap))
                        {
                            continue;
                        }

                        if (MultiProcess && !handle.IsMultiProcess) // this needs to go before the leak check which removes all non leaking processes
                        {
                            continue;
                        }

                        if (!ObjectNameFilter.Value(handle.Name))
                        {
                            continue;
                        }

                        if (!ObjectFilter.Value("0x" + handle.ObjectPtr.ToString("X")))
                        {
                            continue;
                        }

                        if (Leak)
                        {
                            if (!handle.CheckLeakAndRemoveNonLeakingEvents())
                            {
                                continue;
                            }
                        }

                        if( !MatchCreatingOrUsingProcess(handle, file.Extract) )
                        {
                            continue;
                        }

                        if( handle.IsFileMap && handle.FileMapEvents.Count > 0)
                        {
                            if (PtrInMap != null)
                            {
                                long start = handle.FileMapEvents[0].ViewBase;
                                long end = start + handle.FileMapEvents[0].ViewSize;
                                if (!(start <= PtrInMap && PtrInMap <= end))
                                {
                                    continue;
                                }
                            }

                            if (!ViewBaseFilter.Value("0x" + handle.FileMapEvents[0].ViewBase.ToString("X")))
                            {
                                continue;
                            }

                            if (!MinMaxMapSize.IsWithin(handle.FileMapEvents[0].ViewSize))
                            {
                                continue;
                            }
                        }

                        if(!MatchRelatedProcess(handle, file.Extract))
                        {
                            continue;
                        }

                        if( !handle.IsFileMap )
                        {
                            handle.HandleDuplicateEvents = handle.HandleDuplicateEvents.Where(x => IsHandleMatch(x.HandleValue)).ToList();
                            handle.HandleCreateEvents = handle.HandleCreateEvents.Where(x => IsHandleMatch(x.HandleValue)).ToList();
                            handle.HandleCloseEvents = handle.HandleCloseEvents.Where(x => IsHandleMatch(x.HandleValue)).ToList();

                            if( handle.HandleDuplicateEvents.Count + handle.HandleCloseEvents.Count + handle.HandleCreateEvents.Count  == 0)
                            {
                                continue;
                            }
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
                            File = file,
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
            string fileName = null;
            foreach (var ev in matches)
            {
                if (ev.File.FileName != fileName)
                {
                    PrintFileName(ev.File.FileName, null, ev.File.PerformedAt, ev.File.Extract.MainModuleVersion?.ToString());
                    fileName = ev.File.FileName;    
                }

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
                Console.WriteLine($"\t{FormatTime(handleCreate.Time)} Handle Created 0x{handleCreate.HandleValue:X} {GetProcessWithId(handleCreate.ProcessIndex, ev.ProcessExtract)}/{handleCreate.ThreadId} Stack: {GetStack(ev.Stacks, handleCreate.StackIdx)}");
            }

            foreach (HandleDuplicateEvent handleduplicate in ev.ObjTrace.HandleDuplicateEvents)
            {
                Console.WriteLine($"\t{FormatTime(handleduplicate.Time)} Handle Duplicate 0x{handleduplicate.HandleValue:X} {GetProcessWithId(handleduplicate.ProcessIndex, ev.ProcessExtract)}/{handleduplicate.ThreadId} SourceProcess: {GetProcessWithId(handleduplicate.SourceProcessIdx, ev.ProcessExtract)} SourceHandle: 0x{handleduplicate.SourceHandleValue:X} Stack: {GetStack(ev.Stacks, handleduplicate.StackIdx)}");
            }


            if (!Leak)
            {
                foreach (HandleCloseEvent handleClose in ev.ObjTrace.HandleCloseEvents)
                {
                    Console.WriteLine($"\t{FormatTime(handleClose.Time)} Handle Closed 0x{handleClose.HandleValue:X} {GetProcessWithId(handleClose.ProcessIndex, ev.ProcessExtract)}/{handleClose.ThreadId} Stack: {GetStack(ev.Stacks, handleClose.StackIdx)}");
                }
            }

            if (ShowRef)
            {
                int currentRefCount = 0;
                foreach (RefCountChangeEvent change in ev.ObjTrace.RefChanges)
                {
                    currentRefCount += change.RefCountChange;
                    Console.WriteLine($"\t{FormatTime(change.Time)} {currentRefCount} change: {change.RefCountChange} {GetProcessWithId(change.ProcessIndex, ev.ProcessExtract)}/{change.ThreadId} Stack: {GetStack(ev.Stacks, change.StackIdx)}");
                }
            }

            if (ev.ObjTrace.DestroyEvent != null)
            {
                Console.WriteLine($"\t{FormatTime(ev.ObjTrace.DestroyEvent.Time)} Object Deleted {GetProcessWithId(ev.ObjTrace.DestroyEvent.ProcessIndex, ev.ProcessExtract)}/{ev.ObjTrace.DestroyEvent.ThreadId} ------------------------------- {GetStack(ev.Stacks, ev.ObjTrace.DestroyEvent.StackIdx)}");
            }
        }

        private void PrintMapEvent(MatchData ev)
        {
            Console.WriteLine($"MappingObject: 0x{ev.ObjTrace.ObjectPtr:X} Lifetime: {ev.ObjTrace.Duration.TotalSeconds:F6} s");
            foreach (var fileMapEvent in ev.ObjTrace.FileMapEvents)
            {
                Console.WriteLine($"\t{FormatTime(fileMapEvent.Time)} Map to 0x{fileMapEvent.ViewBase:X}-0x{fileMapEvent.ViewBase + fileMapEvent.ViewSize:X} Size: {fileMapEvent.ViewSize} bytes, {GetProcessWithId(fileMapEvent.ProcessIndex, ev.ProcessExtract)}/{fileMapEvent.ThreadId} Offset: {fileMapEvent.ByteOffset} Stack: {GetStack(ev.Stacks, fileMapEvent.StackIdx)}");
            }

            foreach (var fileUnmapEvent in ev.ObjTrace.FileUnmapEvents)
            {
                Console.WriteLine($"\t{FormatTime(fileUnmapEvent.Time)} Unmap of 0x{fileUnmapEvent.ViewBase:X} {GetProcessWithId(fileUnmapEvent.ProcessIndex, ev.ProcessExtract)}/{fileUnmapEvent.ThreadId} Stack: {GetStack(ev.Stacks, fileUnmapEvent.StackIdx)}");
            }
        }

        string FormatTime(DateTimeOffset time)
        {
            return time.ToString("HH:mm:ss.ffffff");
        }
    }
}
