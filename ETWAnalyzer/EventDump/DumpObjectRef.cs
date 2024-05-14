//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Common;
using ETWAnalyzer.Extract.Handle;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ETWAnalyzer.EventDump
{
    /// <summary>
    /// Dump Object/VAMap/Handle tracing data.
    /// </summary>
    class DumpObjectRef : DumpFileDirBase<DumpObjectRef.MatchData>
    {
        internal class MatchData
        {
            public IObjectRefTrace ObjTrace { get; set; }

            public IStackCollection Stacks { get; set; } 
            public IETWExtract Extract { get; set; }
            public int MaxRefCount { get; internal set; }
            public TestDataFile File { get; set; }
            public int Id { get; internal set; }
            public string BaseLine { get; internal set; }
            public Dictionary<IHandleDuplicateEvent, ETWProcess> ClonedChildProcessMap { get; internal set; }
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
        public MinMaxRange<long> MinMaxId { get; internal set; }
        public bool NoCmdLine { get; internal set; }
        public DumpCommand.TotalModes? ShowTotal { get; internal set; }
        public bool Inherit { get; internal set; }

        Dictionary<StackIdx, bool> myStackFilterResult = new();
        

        /// <summary>
        /// Print stacks only once in CSV mode per stack source
        /// </summary>
        Dictionary<object,HashSet<StackIdx>> myPrinted = new();

        public override List<MatchData> ExecuteInternal()
        {
            HarmonizeFilterSettings();

            List<MatchData> lret = ReadFileData();
            if (IsCSVEnabled)
            {
                WriteCSVData(lret);
            }
            else
            {
                PrintMatches(lret);
            }

            return lret;
        }

        private void WriteCSVData(List<MatchData> lret)
        {
            OpenCSVWithHeader(Col_CSVOptions, Col_FileName, Col_Date, Col_TestCase, Col_TestTimeinms, Col_Baseline,
                              "Id", "Stack", "EventName", "Time", "Thread Id", "Handle Value", "Object Name", "Object Ptr",
                              "SourceProces", "SourceHandleValue",
                              "ViewBase", "ViewSize", "File Object", "File Offset",
                              "RefChange",
                              "Lifetime in s (9999 is not closed)", "MultiProcess", "MaxRefCount", "Overlapped (Opened multiple times)",
                              "Inherited by Process",
                              Col_Process, Col_ProcessName,
                              Col_StartTime, Col_CommandLine);



            foreach (var objectEvent in lret)
            {
                foreach (var create in objectEvent.ObjTrace.HandleCreateEvents)
                {
                    ETWProcess createProcess = objectEvent.Extract.GetProcess(create.ProcessIdx);

                    WriteCSVLine(CSVOptions, Path.GetFileNameWithoutExtension(objectEvent.File.FileName), objectEvent.File.PerformedAt, objectEvent.File.TestName, objectEvent.File.DurationInMs, objectEvent.BaseLine,
                        objectEvent.Id, GetStack(objectEvent.Stacks, create.StackIdx), create.GetType().Name, create.TimeNs, create.ThreadId, GetHandleValue(create.HandleValue), objectEvent.ObjTrace.Name, GetHandleValue((ulong)objectEvent.ObjTrace.ObjectPtr),
                        "", "",
                        "", "", "", "",
                        "",
                        objectEvent.ObjTrace.Duration.TotalSeconds, objectEvent.ObjTrace.IsMultiProcess, objectEvent.MaxRefCount, objectEvent.ObjTrace.IsOverlapped,
                        "",
                        GetProcessAndStartStopTags(createProcess, objectEvent.Extract), createProcess.GetProcessName(UsePrettyProcessName), 
                        createProcess.StartTime, NoCmdLine ? "" : createProcess.CommandLineNoExe);
                }

                // add inherited also to close handle events to allow to check if processes did close the duplicated handles again
                string inherited = String.Join(", ", objectEvent.ClonedChildProcessMap.Values
                                                                .ToHashSet()
                                                                .OrderBy(x => x.StartTime)
                                                                .Select(x => GetProcessAndStartStopTags(x, objectEvent.Extract)));

                foreach (var close in objectEvent.ObjTrace.HandleCloseEvents)
                {
                    ETWProcess closeProcess = objectEvent.Extract.GetProcess(close.ProcessIdx);

                    WriteCSVLine(CSVOptions, Path.GetFileNameWithoutExtension(objectEvent.File.FileName), objectEvent.File.PerformedAt, objectEvent.File.TestName, objectEvent.File.DurationInMs, objectEvent.BaseLine,
                        objectEvent.Id, GetStack(objectEvent.Stacks, close.StackIdx), close.GetType().Name, close.TimeNs, close.ThreadId, GetHandleValue(close.HandleValue), objectEvent.ObjTrace.Name, GetHandleValue((ulong)objectEvent.ObjTrace.ObjectPtr),
                        "", "",
                        "", "", "", "",
                        "",
                        objectEvent.ObjTrace.Duration.TotalSeconds, objectEvent.ObjTrace.IsMultiProcess, objectEvent.MaxRefCount, objectEvent.ObjTrace.IsOverlapped,
                        inherited,
                        GetProcessAndStartStopTags(closeProcess, objectEvent.Extract), closeProcess.GetProcessName(UsePrettyProcessName), 
                        closeProcess.StartTime, NoCmdLine ? "" : closeProcess.CommandLineNoExe);
                }

                foreach (var duplicate in objectEvent.ObjTrace.HandleDuplicateEvents)
                {
                    ETWProcess closeProcess = objectEvent.Extract.GetProcess(duplicate.ProcessIdx);

                    WriteCSVLine(CSVOptions, Path.GetFileNameWithoutExtension(objectEvent.File.FileName), objectEvent.File.PerformedAt, objectEvent.File.TestName, objectEvent.File.DurationInMs, objectEvent.BaseLine,
                    objectEvent.Id, GetStack(objectEvent.Stacks, duplicate.StackIdx), duplicate.GetType().Name, duplicate.TimeNs, duplicate.ThreadId, GetHandleValue(duplicate.HandleValue), objectEvent.ObjTrace.Name, GetHandleValue((ulong)objectEvent.ObjTrace.ObjectPtr),
                    GetProcessAndStartStopTags(duplicate.SourceProcessIdx, objectEvent.Extract), GetHandleValue(duplicate.SourceHandleValue),
                    "", "", "", "",
                    "",
                    objectEvent.ObjTrace.Duration.TotalSeconds, objectEvent.ObjTrace.IsMultiProcess, objectEvent.MaxRefCount, objectEvent.ObjTrace.IsOverlapped,
                    inherited,
                    GetProcessAndStartStopTags(closeProcess, objectEvent.Extract), closeProcess.GetProcessName(UsePrettyProcessName),
                    closeProcess.StartTime, NoCmdLine ? "" : closeProcess.CommandLineNoExe);
                }

                foreach (var map in objectEvent.ObjTrace.FileMapEvents)
                {
                    ETWProcess mapProcess = objectEvent.Extract.GetProcess(map.ProcessIdx);

                    WriteCSVLine(CSVOptions, Path.GetFileNameWithoutExtension(objectEvent.File.FileName), objectEvent.File.PerformedAt, objectEvent.File.TestName, objectEvent.File.DurationInMs, objectEvent.BaseLine,
                    objectEvent.Id, GetStack(objectEvent.Stacks, map.StackIdx), map.GetType().Name, map.TimeNs, map.ThreadId, "", "", GetHandleValue((ulong)objectEvent.ObjTrace.ObjectPtr),
                    "", "",
                    GetHandleValue((ulong)map.ViewBase), map.ViewSize, GetHandleValue((ulong)map.FileObject), map.ByteOffset,
                    "",
                    objectEvent.ObjTrace.Duration.TotalSeconds, objectEvent.ObjTrace.IsMultiProcess, objectEvent.MaxRefCount, objectEvent.ObjTrace.IsOverlapped,
                    "",
                    GetProcessAndStartStopTags(mapProcess, objectEvent.Extract), mapProcess.GetProcessName(UsePrettyProcessName), 
                    mapProcess.StartTime, NoCmdLine ? "" : mapProcess.CommandLineNoExe);
                }

                foreach (var unMap in objectEvent.ObjTrace.FileUnmapEvents)
                {
                    ETWProcess mapProcess = objectEvent.Extract.GetProcess(unMap.ProcessIdx);

                    WriteCSVLine(CSVOptions, Path.GetFileNameWithoutExtension(objectEvent.File.FileName), objectEvent.File.PerformedAt, objectEvent.File.TestName, objectEvent.File.DurationInMs, objectEvent.BaseLine,
                    objectEvent.Id, GetStack(objectEvent.Stacks, unMap.StackIdx), unMap.GetType().Name, unMap.TimeNs, unMap.ThreadId, "", "", GetHandleValue((ulong)objectEvent.ObjTrace.ObjectPtr),
                    "", "",
                    GetHandleValue((ulong)unMap.ViewBase), unMap.ViewSize, GetHandleValue((ulong)unMap.FileObject), unMap.ByteOffset,
                    "",
                    objectEvent.ObjTrace.Duration.TotalSeconds, objectEvent.ObjTrace.IsMultiProcess, objectEvent.MaxRefCount, objectEvent.ObjTrace.IsOverlapped,
                    "",
                    GetProcessAndStartStopTags(mapProcess, objectEvent.Extract), mapProcess.GetProcessName(UsePrettyProcessName),
                    mapProcess.StartTime, NoCmdLine ? "" : mapProcess.CommandLineNoExe);
                }

                foreach (var refChange in objectEvent.ObjTrace.RefChanges)
                {
                    ETWProcess refChangeProc = objectEvent.Extract.GetProcess(refChange.ProcessIdx);

                    WriteCSVLine(CSVOptions, Path.GetFileNameWithoutExtension(objectEvent.File.FileName), objectEvent.File.PerformedAt, objectEvent.File.TestName, objectEvent.File.DurationInMs, objectEvent.BaseLine,
                    objectEvent.Id, GetStack(objectEvent.Stacks, refChange.StackIdx), refChange.GetType().Name, refChange.TimeNs, refChange.ThreadId, "", "", GetHandleValue((ulong)objectEvent.ObjTrace.ObjectPtr),
                    "", "",
                    "", "", "", "",
                    refChange.RefCountChange,
                    objectEvent.ObjTrace.Duration.TotalSeconds, objectEvent.ObjTrace.IsMultiProcess, objectEvent.MaxRefCount, objectEvent.ObjTrace.IsOverlapped,
                    "",
                    GetProcessAndStartStopTags(refChangeProc, objectEvent.Extract), refChangeProc.GetProcessName(UsePrettyProcessName),
                    refChangeProc.StartTime, NoCmdLine ? "" : refChangeProc.CommandLineNoExe);

                }
            }
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

            int id = 0; // object id;

            foreach (var test in runData)
            {
                foreach (TestDataFile file in test.Value.Files)
                {
                    if (file?.Extract?.HandleData?.ObjectReferences?.Count == null)
                    {
                        ColorConsole.WriteError($"Warning: File {GetPrintFileName(file.FileName)} does not contain ObjectRef or Handle tracing data.");
                        continue;
                    }

                    // read stacks from extra file only if we need it
                    IStackCollection stacks = (
                                               ShowStack ||
                                               StackFilter.Key != null ||
                                               CreateStackFilter.Key != null ||
                                               DestroyStackFilter.Key != null
                                               ) ? file.Extract.HandleData.Stacks : null;

                    foreach (ObjectRefTrace handle in file.Extract.HandleData.ObjectReferences)
                    {
                        handle.RefreshCollectionsAfterDeserialize();

                        id++;

                        if(!MinMaxId.IsWithin(id))  // filter by object id
                        {
                            continue;
                        }

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

                        if( !MatchCreatingProcess(handle, file.Extract) )
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

                        Dictionary<IHandleDuplicateEvent, ETWProcess> clone2ChildProcMap = new();
                        foreach (var duplicate in handle.HandleDuplicateEvents)
                        {
                            ETWProcess cloned = GetClonedChildProcess(duplicate, file.Extract);
                            if (cloned != null)
                            {
                                clone2ChildProcMap[duplicate] = cloned;
                            }
                        }


                        if( Inherit && clone2ChildProcMap.Count == 0)
                        {
                            continue;
                        }

                        if (!MatchAnyProcess(handle, clone2ChildProcMap, file.Extract))
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

                        if(Overlapped && !handle.IsOverlapped)
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
                            ClonedChildProcessMap = clone2ChildProcMap,
                            Extract = file.Extract,
                            Stacks =  stacks,
                            MaxRefCount = maxRefCount,
                            File = file,
                            BaseLine = file.Extract.MainModuleVersion?.ToString(),
                            Id = id,
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

        bool MatchCreatingProcess(ObjectRefTrace trace, IProcessExtract resolver)
        {
            bool lret = false;
            ETWProcessIndex creator = ETWProcessIndex.Invalid;
            if (trace.CreateEvent != null)
            {
                creator = trace.CreateEvent.ProcessIdx;
                lret = ProcessNameFilter(GetProcessWithId(creator, resolver));
            }
            return lret;
        }


        /// <summary>
        /// Filter for object trace which have this process occurring in any event.
        /// </summary>
        /// <param name="trace"></param>
        /// <param name="clonedChildHandles">map of handle duplicate events which clone the current process handle into the new process.</param>
        /// <param name="resolver"></param>
        /// <returns>true if process was calling create/close/duplicate for given object.</returns>
        bool MatchAnyProcess(IObjectRefTrace trace, Dictionary<IHandleDuplicateEvent, ETWProcess> clonedChildHandles, IProcessExtract resolver)
        {
            bool lret = false;
            if (trace.CreateEvent != null)
            {
                if( RelatedProcessFilter.Value(GetProcessWithId(trace.CreateEvent.ProcessIdx, resolver)))
                {
                    lret = true;
                }
            }

            if (trace.DestroyEvent != null) // reference tracing
            {
                if (RelatedProcessFilter.Value(GetProcessWithId(trace.DestroyEvent.ProcessIdx, resolver)))
                {
                    lret = true;
                }
            }

            if( !lret )
            {
                lret = clonedChildHandles.Values.Any(x => RelatedProcessFilter.Value(x.GetProcessWithId(UsePrettyProcessName)));
            }

            if (!lret)
            {
                lret = trace.HandleCreateEvents.Any(x => RelatedProcessFilter.Value(GetProcessWithId(x.ProcessIdx, resolver)));
            }

            if (!lret)
            {
                lret = trace.HandleCloseEvents.Any(x => RelatedProcessFilter.Value(GetProcessWithId(x.ProcessIdx, resolver)));
            }

            if( !lret)
            {
                lret = trace.HandleDuplicateEvents.Any(x => RelatedProcessFilter.Value(GetProcessWithId(x.ProcessIdx, resolver)));
            }

            if (!lret)
            {
                lret = trace.HandleDuplicateEvents.Any(x => RelatedProcessFilter.Value(GetProcessWithId(x.SourceProcessIdx, resolver)));
            }

            if (!lret)
            {
                lret = trace.RefChanges.Any(x => RelatedProcessFilter.Value(GetProcessWithId(x.ProcessIdx, resolver)));
            }

            if( !lret )
            {
                lret = trace.FileMapEvents.Any(x => RelatedProcessFilter.Value(GetProcessWithId(x.ProcessIdx, resolver)));
            }

            if( !lret )
            {
                lret = trace.FileUnmapEvents.Any(x => RelatedProcessFilter.Value(GetProcessWithId(x.ProcessIdx, resolver)));
            }

            return lret;
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

        class Totals
        {
            public int ObjectCreateCount { get; internal set; }
            public int ObjectDestroyCount { get; internal set; }    
            public int CreateCount { get; internal set; }
            public int CloseCount { get; internal set; }
            public int DuplicateCount { get; internal set; }
            public int MapCount { get; internal set; }
            public int UnmapCount { get; internal set; }
            public int RefChangeCount { get; internal set; }
            public HashSet<ETWProcess> Processes { get; internal set; } = new();

            void AddProcess(IReadOnlyList<IStackEventBase> items, IETWExtract extract)
            {
                foreach(IStackEventBase item in items)
                {
                    Processes.Add(extract.GetProcess(item.ProcessIdx));
                }
            }

            void AddProcess(IReadOnlyList<IHandleDuplicateEvent> duplicates, IETWExtract extract)
            {
                foreach (IHandleDuplicateEvent duplicate in duplicates)
                {
                    Processes.Add(extract.GetProcess(duplicate.ProcessIdx));
                    Processes.Add(extract.GetProcess(duplicate.SourceProcessIdx));
                }
            }

            public void Add(IObjectRefTrace trace, IETWExtract extract)
            {
                if (trace.CreateEvent != null)
                {
                    ObjectCreateCount++;
                }

                if(trace.DestroyEvent != null)
                {
                    ObjectDestroyCount++;   
                }

                CreateCount += trace.HandleCreateEvents.Count;
                AddProcess(trace.HandleCreateEvents, extract);

                CloseCount += trace.HandleCloseEvents.Count;
                AddProcess(trace.HandleCloseEvents, extract);

                DuplicateCount += trace.HandleDuplicateEvents.Count;
                AddProcess(trace.HandleDuplicateEvents, extract);   

                MapCount += trace.FileMapEvents.Count;
                AddProcess(trace.FileMapEvents, extract);   

                UnmapCount += trace.FileUnmapEvents.Count;
                AddProcess(trace.FileUnmapEvents, extract);

                RefChangeCount += trace.RefChanges.Count;
                AddProcess(trace.RefChanges, extract);
            }

            public void PrintTotals(ConsoleColor color)
            {
                ColorConsole.WriteEmbeddedColorLine($"Totals: Processes: {Processes.Count} Objects Created/Destroyed: {ObjectCreateCount}/{ObjectDestroyCount} Diff: {ObjectCreateCount-ObjectDestroyCount}  Handles Created/Closed/Duplicated: {CreateCount}/{CloseCount}/{DuplicateCount} Diff: {CreateCount+DuplicateCount-CloseCount}, RefChanges: {RefChangeCount}, FileMap/Unmap: {MapCount}/{UnmapCount}", color);
            }
        }

        private void PrintMatches(List<MatchData> matches)
        {
            Totals fileTotal = new();
            Totals allfileTotal = new();

            string fileName = null;
            int fileCount = 0;

            foreach (var ev in matches.OrderBy(x=>x.File.PerformedAt).ThenBy(x=> (x.ObjTrace?.CreateEvent.TimeNs ?? 0)) )
            {
                fileTotal.Add(ev.ObjTrace, ev.Extract);
                allfileTotal.Add(ev.ObjTrace, ev.Extract);

                if (ev.File.FileName != fileName)
                {
                    if( ShowTotal != DumpCommand.TotalModes.None && fileName != null)
                    {
                        fileTotal.PrintTotals(ConsoleColor.Yellow);
                    }

                    PrintFileName(ev.File.FileName, null, ev.File.PerformedAt, ev.File.Extract.MainModuleVersion?.ToString());
                    fileCount++;
                    if (fileName != null) // do not loose first event when newing up totals in loop here.
                    {
                        fileTotal = new();
                    }

                    fileName = ev.File.FileName;
                }


                if ( ShowTotal == null || ShowTotal == DumpCommand.TotalModes.None )
                {
                    if (ev.ObjTrace.IsFileMap)
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

            if( matches.Count > 0 && ShowTotal != DumpCommand.TotalModes.None)
            {
                fileTotal.PrintTotals(ConsoleColor.Yellow);
            }

            if (fileCount > 1 && (ShowTotal == DumpCommand.TotalModes.Total || ShowTotal == null))
            {
                allfileTotal.PrintTotals(ConsoleColor.Red);
            }
        }


        /// <summary>
        /// Get child process name if a Handle duplicate is done to create a child process which inherits from the current process
        /// the handles.
        /// </summary>
        /// <param name="handleduplicate">Handle duplicate event to check.</param>
        /// <param name="extract">ETWExtract instance</param>
        /// <returns>null if no child process exist, or a child process instance.</returns>
        ETWProcess GetClonedChildProcess(IHandleDuplicateEvent handleduplicate, IETWExtract extract)
        {
            ETWProcess lret = null;
            if( handleduplicate.SourceHandleValue == handleduplicate.HandleValue &&
                handleduplicate.SourceProcessIdx == handleduplicate.ProcessIdx)
            {
                var parent = extract.GetProcess(handleduplicate.SourceProcessIdx);
                var child = extract.Processes.OrderBy(x => x.StartTime).Where(x => x.ParentPid == parent.ProcessID && 
                                                                              x.StartTime > parent.StartTime && 
                                                                              x.StartTime < parent.EndTime &&
                                                                              x.StartTime > handleduplicate.GetTime(extract.SessionStart)
                                                                              ).FirstOrDefault();
                if( child?.StartTime > handleduplicate.GetTime(extract.SessionStart))
                {
                    lret = child;
                }
                
            }

            return lret;
        }


        private void PrintObjectEvent(MatchData ev)
        {
            Console.WriteLine($"Id: {ev.Id} Object: 0x{ev.ObjTrace.ObjectPtr:X} {ev.ObjTrace.Name} Lifetime: {ev.ObjTrace.Duration.TotalSeconds:F6} s  " +
                              $"Create+Duplicate-Close: {ev.ObjTrace.HandleCreateEvents.Count}+{ev.ObjTrace.HandleDuplicateEvents.Count}-{ev.ObjTrace.HandleCloseEvents.Count} = {ev.ObjTrace.HandleCreateEvents.Count + ev.ObjTrace.HandleDuplicateEvents.Count - ev.ObjTrace.HandleCloseEvents.Count}");

            foreach (IHandleCreateEvent handleCreate in ev.ObjTrace.HandleCreateEvents)
            {
                PrintEventHeader(handleCreate, ev.Extract,    "[green]HandleCreate   [/green]", GetHandleStrAligned(handleCreate.HandleValue, ConsoleColor.Green));
                Console.WriteLine($"Stack: {GetStack(ev.Stacks, handleCreate.StackIdx)}");
            }

            foreach (IHandleDuplicateEvent handleduplicate in ev.ObjTrace.HandleDuplicateEvents)
            {
                PrintEventHeader(handleduplicate, ev.Extract, "[yellow]HandleDuplicate[/yellow]", GetHandleStrAligned(handleduplicate.HandleValue, ConsoleColor.Green));

                if (ev.ClonedChildProcessMap.TryGetValue(handleduplicate, out ETWProcess clone))
                {
                    ColorConsole.WriteEmbeddedColorLine($"Inherited by process [yellow]{GetProcessAndStartStopTags(clone, ev.Extract)}[/yellow] Stack: {GetStack(ev.Stacks, handleduplicate.StackIdx)}");
                }
                else
                {
                    ColorConsole.WriteEmbeddedColorLine($"SourceProcess: [magenta]{GetProcessAndStartStopTags(handleduplicate.SourceProcessIdx, ev.Extract)}[/magenta] SourceHandle: 0x{handleduplicate.SourceHandleValue:X} Stack: {GetStack(ev.Stacks, handleduplicate.StackIdx)}");
                }
            }


            if (!Leak)
            {
                foreach (IHandleCloseEvent handleClose in ev.ObjTrace.HandleCloseEvents)
                {
                    string inheritInfo = "";
                    if( ev.ClonedChildProcessMap.Count > 0)
                    {
                        var sourceProcessIdx = ev.ClonedChildProcessMap.Where(x => x.Key.HandleValue == handleClose.HandleValue).OrderBy(x => x.Key.TimeNs).FirstOrDefault().Key?.SourceProcessIdx;
                        if (sourceProcessIdx != null)
                        {
                            inheritInfo = $" Inherited from [yellow]{ev.Extract.GetProcess(sourceProcessIdx.Value).GetProcessWithId(UsePrettyProcessName)}[/yellow] ";
                        }
                    }
                    PrintEventHeader(handleClose, ev.Extract, $"[red]HandleClose    [/red]", GetHandleStrAligned(handleClose.HandleValue, ConsoleColor.Green));
                    ColorConsole.WriteEmbeddedColorLine($"{inheritInfo}Stack: {GetStack(ev.Stacks, handleClose.StackIdx)}");
                }
            }  

            if (ShowRef)
            {
                int currentRefCount = 0;
                foreach (IRefCountChangeEvent change in ev.ObjTrace.RefChanges)
                {
                    currentRefCount += change.RefCountChange;
                    if (change.RefCountChange > 0)
                    {
                        PrintEventHeader(change, ev.Extract, "[green]RefChange      [/green]");
                    }
                    else
                    {
                        PrintEventHeader(change, ev.Extract, "[red]RefChange      [/red]");
                    }
                    Console.WriteLine($"{change.RefCountChange} Stack: {GetStack(ev.Stacks, change.StackIdx)}");
                }
            }

            if (ev.ObjTrace.DestroyEvent != null && ev.ObjTrace.RefChanges.Count > 0)  // destroyevent is set only for ref traces uniquely otherwise we have the handle close event as final event
            {
                PrintEventHeader(ev.ObjTrace.DestroyEvent, ev.Extract, 
                                                                     "[red]Object Delete  [/red]");
                Console.WriteLine($"------------------------------- {GetStack(ev.Stacks, ev.ObjTrace.DestroyEvent.StackIdx)}");
            }
        }

        private void PrintMapEvent(MatchData ev)
        {
            Console.WriteLine($"Id: {ev.Id} MappingObject: 0x{ev.ObjTrace.ObjectPtr:X} Lifetime: {ev.ObjTrace.Duration.TotalSeconds:F6} s");
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
            ColorConsole.WriteEmbeddedColorLine($"\t{timeStr} [magenta]{GetProcessId(ev.ProcessIdx, resolver),5}[/magenta]/{ev.ThreadId,-5} {name} {beforeProc}[magenta]{GetProcessAndStartStopTags(ev.ProcessIdx, resolver,false)}[/magenta] ", null, true);
        }



        string GetStack(IStackCollection stacks, StackIdx stackIdx)
        {
            string lret = stackIdx.ToString();            
            if (ShowStack)
            {
                if (IsCSVEnabled) // add stack only once to CSV to save space per stack index
                {
                    if( !myPrinted.TryGetValue(stacks, out HashSet<StackIdx> set) )
                    {
                        set = new();
                        myPrinted.Add(stacks, set);
                    }

                    if (set.Add(stackIdx) == true)
                    {
                        lret = $"StackId: {lret} " + stacks.GetStack(stackIdx);
                    }
                }
                else
                {
                    lret = stacks.GetStack(stackIdx);
                }
            }

            return lret;
        }

        string GetProcessWithId(ETWProcessIndex procIdx, IProcessExtract resolver)
        {
            return resolver.GetProcess(procIdx).GetProcessWithId(UsePrettyProcessName);
        }

        string GetProcessAndStartStopTags(ETWProcessIndex processIdx, IETWExtract extract,bool bPrintId=true)
        {
            ETWProcess process = extract.GetProcess(processIdx);
            return GetProcessAndStartStopTags(process, extract, bPrintId);
        }

        string GetProcessAndStartStopTags(ETWProcess process, IETWExtract extract, bool bPrintId=true)
        {
            return $"{(bPrintId ? process.GetProcessWithId(UsePrettyProcessName) : process.GetProcessName(UsePrettyProcessName))}{base.GetProcessTags(process, extract.SessionStart)}";
        }

        string GetProcessName(ETWProcessIndex procIdx, IProcessExtract resolver)
        {
            return resolver.GetProcess(procIdx).GetProcessName(UsePrettyProcessName);
        }

        int GetProcessId(ETWProcessIndex procIdx, IProcessExtract resolver)
        {
            return resolver.GetProcess(procIdx).ProcessID;
        }

        string GetHandleValue(ulong value)
        {
            string str = "0x" + value.ToString("X");
            return str;
        }
        string GetHandleStrAligned(ulong value, ConsoleColor color)
        {
            string str = GetHandleValue(value);
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
