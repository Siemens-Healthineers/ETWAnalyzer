using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Common;
using ETWAnalyzer.Extract.Handle;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.EventDump
{
    class DumpObjectRef : DumpFileDirBase<DumpObjectRef.MatchData>
    {
        internal class MatchData
        {
            public ObjectRefTrace ObjTrace { get; set; }

            public IStackCollection Stacks { get; set; } 
            public IETWExtract Extract { get; set; }
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
        public KeyValuePair<string, Func<string, bool>> CreateStackFilter { get; internal set; }

        Dictionary<StackIdx, bool> myStackFilterResult = new();


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

                    IStackCollection stacks = file.Extract.HandleData.Stacks;

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
                            handle.CheckLeakAndRemoveNonLeakingEvents();
                        }
                        
                        if( StackFilter.Key != null)
                        {
                            ThrowAwayAllEventsWithNotMatchingStacks(stacks, handle);
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


                        if (CreateStackFilter.Key != null)
                        {
                            if (handle.CreateEvent == null || !CreateStackFilter.Value(stacks?.GetStack(handle.CreateEvent.StackIdx)))
                            {
                                continue;
                            }
                        }

                        if (DestroyStackFilter.Key != null)
                        {
                            if(handle.DestroyEvent == null || !DestroyStackFilter.Value(stacks?.GetStack(handle.DestroyEvent.StackIdx)))
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

                        if (handle.IsEmpty)
                        {
                            continue;
                        }

                        lret.Add(new MatchData
                        {
                            ObjTrace = handle,
                            Extract = file.Extract,
                            Stacks = file.Extract.HandleData.Stacks,
                            MaxRefCount = maxRefCount,
                            File = file,
                        });
                    }
                }
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

        bool MatchCreatingOrUsingProcess(ObjectRefTrace trace, IProcessExtract resolver)
        {
            bool lret = false;
            ETWProcessIndex creator = ETWProcessIndex.Invalid;
            if (trace.CreateEvent != null)
            {
                creator = trace.CreateEvent.ProcessIdx;
                lret = ProcessNameFilter(GetProcessWithId(creator, resolver));
            }

            if (!lret && trace.DestroyEvent != null)
            {
                string destroyName = GetProcessWithId(trace.DestroyEvent.ProcessIdx, resolver);
                lret = ProcessNameFilter(destroyName);
            }

            if (!lret)
            {
                lret = trace.HandleCreateEvents.Any(x => ProcessNameFilter(GetProcessWithId(x.ProcessIdx, resolver)));
            }

            if (!lret)
            {
                lret = trace.HandleCloseEvents.Any(x => ProcessNameFilter(GetProcessWithId(x.ProcessIdx, resolver)));
            }

            if (!lret)
            {
                lret = trace.HandleDuplicateEvents.Any(x => ProcessNameFilter(GetProcessWithId(x.ProcessIdx, resolver)));
            }

            return lret;
        }

        bool MatchRelatedProcess(ObjectRefTrace trace, IProcessExtract resolver)
        {
            ETWProcessIndex creator = ETWProcessIndex.Invalid;
            if (trace.CreateEvent != null)
            {
                creator = trace.CreateEvent.ProcessIdx;
            }

            foreach (var ev in trace?.HandleCreateEvents)
            {
                if (ev.ProcessIdx != creator)
                {
                    // filter by process name with pid like cmd.exe(100)
                    if (!RelatedProcessFilter.Value(GetProcessWithId(ev.ProcessIdx, resolver)))
                    {
                        return false;
                    }
                }
            }

            foreach (var ev in trace?.HandleCloseEvents)
            {
                if (ev.ProcessIdx != creator)
                {
                    // filter by process name with pid like cmd.exe(100)
                    if (!RelatedProcessFilter.Value(GetProcessWithId(ev.ProcessIdx, resolver)))
                    {
                        return false;
                    }
                }
            }

            foreach (var ev in trace?.HandleDuplicateEvents)
            {
                if (ev.ProcessIdx != creator)
                {
                    // filter by process name with pid like cmd.exe(100)
                    if (!RelatedProcessFilter.Value(GetProcessWithId(ev.ProcessIdx, resolver)))
                    {
                        return false;
                    }
                }
            }

            foreach (var ev in trace.RefChanges)
            {
                if (ev.ProcessIdx != creator)
                {
                    // filter by process name with pid like cmd.exe(100)
                    if (!RelatedProcessFilter.Value(GetProcessWithId(ev.ProcessIdx, resolver)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void ThrowAwayAllEventsWithNotMatchingStacks(IStackCollection stacks, ObjectRefTrace handle)
        {
            if(handle.CreateEvent != null &&  !CachingStackFilter(stacks, handle.CreateEvent.StackIdx) )
            {
                handle.CreateEvent = null;
            }
            if(handle.DestroyEvent != null && !CachingStackFilter(stacks, handle.DestroyEvent.StackIdx) )
            {
                handle.DestroyEvent = null;
            }
            handle.HandleCreateEvents =  handle.HandleCreateEvents.Where(x => CachingStackFilter(stacks, x.StackIdx)).ToList();
            handle.HandleCloseEvents  =  handle.HandleCloseEvents.Where(x => CachingStackFilter(stacks, x.StackIdx)).ToList();
            handle.HandleDuplicateEvents = handle.HandleDuplicateEvents.Where(x => CachingStackFilter(stacks, x.StackIdx)).ToList();
            handle.RefChanges = handle.RefChanges.Where(x => CachingStackFilter(stacks, x.StackIdx)).ToList();
            handle.FileMapEvents = handle.FileMapEvents.Where(x => CachingStackFilter(stacks, x.StackIdx)).ToList();
            handle.FileUnmapEvents = handle.FileUnmapEvents.Where(x => CachingStackFilter(stacks, x.StackIdx)).ToList();
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

            foreach (IHandleCreateEvent handleCreate in ev.ObjTrace.HandleCreateEvents)
            {
                PrintEventHeader(handleCreate, ev.Extract,    "[green]HandleCreate   [/green]", GetHandleStr(handleCreate.HandleValue, ConsoleColor.Green));
                Console.WriteLine($"Stack: {GetStack(ev.Stacks, handleCreate.StackIdx)}");
            }

            foreach (IHandleDuplicateEvent handleduplicate in ev.ObjTrace.HandleDuplicateEvents)
            {
                PrintEventHeader(handleduplicate, ev.Extract, "[yellow]HandleDuplicate[/yellow]", GetHandleStr(handleduplicate.HandleValue, ConsoleColor.Green));
                Console.WriteLine($"SourceProcess: {GetProcessWithId(handleduplicate.SourceProcessIdx, ev.Extract)} SourceHandle: 0x{handleduplicate.SourceHandleValue:X} Stack: {GetStack(ev.Stacks, handleduplicate.StackIdx)}");
            }


            if (!Leak)
            {
                foreach (IHandleCloseEvent handleClose in ev.ObjTrace.HandleCloseEvents)
                {
                    PrintEventHeader(handleClose, ev.Extract, "[red]HandleClose    [/red]", GetHandleStr(handleClose.HandleValue, ConsoleColor.Green));
                    Console.WriteLine($"Stack: {GetStack(ev.Stacks, handleClose.StackIdx)}");
                }
            }  

            if (ShowRef)
            {
                int currentRefCount = 0;
                foreach (IRefCountChangeEvent change in ev.ObjTrace.RefChanges)
                {
                    currentRefCount += change.RefCountChange;
                    PrintEventHeader(change, ev.Extract,      "RefChange      ");
                    Console.WriteLine($"{change.RefCountChange} Stack: {GetStack(ev.Stacks, change.StackIdx)}");
                }
            }

            if (ev.ObjTrace.DestroyEvent != null)
            {
                PrintEventHeader(ev.ObjTrace.DestroyEvent, ev.Extract, 
                                                                     "[red]Object Delete  [/red]");
                Console.WriteLine($"------------------------------- {GetStack(ev.Stacks, ev.ObjTrace.DestroyEvent.StackIdx)}");
            }
        }

        private void PrintMapEvent(MatchData ev)
        {
            Console.WriteLine($"MappingObject: 0x{ev.ObjTrace.ObjectPtr:X} Lifetime: {ev.ObjTrace.Duration.TotalSeconds:F6} s");
            foreach (IFileMapEvent fileMapEvent in ev.ObjTrace.FileMapEvents)
            {
                PrintEventHeader(fileMapEvent, ev.Extract, "Map");
                Console.WriteLine($"0x{fileMapEvent.ViewBase:X}-0x{fileMapEvent.ViewBase + fileMapEvent.ViewSize:X} Size: {fileMapEvent.ViewSize} bytes, Offset: {fileMapEvent.ByteOffset} Stack: {GetStack(ev.Stacks, fileMapEvent.StackIdx)}");
            }

            foreach (IFileMapEvent fileUnmapEvent in ev.ObjTrace.FileUnmapEvents)
            {
                PrintEventHeader(fileUnmapEvent, ev.Extract, "Unmap");
                Console.WriteLine($"0x{fileUnmapEvent.ViewBase:X} Stack: {GetStack(ev.Stacks, fileUnmapEvent.StackIdx)}");
            }
        }

        void PrintEventHeader(IStackEventBase ev, IETWExtract resolver, string name, string beforeProc = null)
        {
            string timeStr = base.GetTimeString(ev.GetTime(resolver.SessionStart), resolver.SessionStart, this.TimeFormatOption, 6);
            ColorConsole.WriteEmbeddedColorLine($"\t{timeStr} {GetProcessId(ev.ProcessIdx, resolver),5}/{ev.ThreadId,-5} {name} {beforeProc}{GetProcessName(ev.ProcessIdx, resolver)} ", null, true);
        }

        string GetStack(IStackCollection stacks, StackIdx stackIdx)
        {
            if (ShowStack)
            {
                return stacks.GetStack(stackIdx);
            }
            else
            {
                return stackIdx.ToString();
            }
        }

        string GetProcessWithId(ETWProcessIndex procIdx, IProcessExtract resolver)
        {
            return resolver.GetProcess(procIdx).GetProcessWithId(UsePrettyProcessName);
        }

        string GetProcessName(ETWProcessIndex procIdx, IProcessExtract resolver)
        {
            return resolver.GetProcess(procIdx).GetProcessName(UsePrettyProcessName);
        }

        int GetProcessId(ETWProcessIndex procIdx, IProcessExtract resolver)
        {
            return resolver.GetProcess(procIdx).ProcessID;
        }

        string GetHandleStr(ulong value, ConsoleColor color)
        {
            string str = "0x" + value.ToString("X");
            str = $"{str,-8}";
            return $"[{color}]{str}[/{color}]";
        }

        bool IsHandleMatch(ulong handleValue)
        {
            return HandleFilter.Value("0x" + handleValue.ToString("X"));
        }

        bool CachingStackFilter(IStackCollection stacks, StackIdx idx)
        {
            if (!myStackFilterResult.TryGetValue(idx, out var lret))
            {
                lret = StackFilter.Value(stacks?.GetStack(idx));
                myStackFilterResult[idx] = lret;
            }
            return myStackFilterResult[idx];
        }
    }
}
