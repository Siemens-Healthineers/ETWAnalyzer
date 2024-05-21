//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.Common;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Windows.EventTracing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.Extract.Handle
{
    /// <summary>
    /// Contains all events from Object Provider and VAMAP to correlate 
    /// Handle and file mapping events.
    /// </summary>
    public class ObjectRefTrace : IObjectRefTrace
    {
        /// <summary>
        /// Kernel object pointer. Value can be reused once object is closed.
        /// </summary>
        public long ObjectPtr { get; set; }

        /// <summary>
        /// Object creation event which originates from Object creation or handle creation event depending on which ETW providers were enabled during recording.
        /// </summary>
        public RefCountChangeEvent CreateEvent { get; set; }

        IRefCountChangeEvent IObjectRefTrace.FirstCreateEvent => FirstCreateEvent;


        /// <summary>
        /// Returns CreateEvent or, if no Create Event is existing, the first Handle Create or file map event
        /// </summary>

        [JsonIgnore]
        public RefCountChangeEvent FirstCreateEvent
        {
            get
            {
                RefCountChangeEvent first = CreateEvent;
                if (first == null)
                {
                    if (IsFileMap && FileMapEvents.Count > 0)
                    {
                        var firstMap = FileMapEvents.OrderBy(x => x.TimeNs).First();
                        first = new RefCountChangeEvent(default(TraceTimestamp), 1, firstMap.ProcessIdx, firstMap.ThreadId, firstMap.StackIdx)
                        {
                            TimeNs = firstMap.TimeNs 
                        };
                    }
                    else if (HandleCreateEvents.Count > 0)
                    {
                        var firstHandleCreate = HandleCreateEvents.OrderBy(x => x.TimeNs).First();
                        first = new RefCountChangeEvent(default(TraceTimestamp), 1, firstHandleCreate.ProcessIdx, firstHandleCreate.ThreadId, firstHandleCreate.StackIdx)
                        { 
                            TimeNs = firstHandleCreate.TimeNs 
                        };
                    }

                    CreateEvent = first;    // cache it for later use
                }

                return first;
            }
        }

        /// <summary>
        /// Object deletion event which originates from object ref DestroyObject or Handle close events
        /// </summary>
        public RefCountChangeEvent DestroyEvent { get; set; }

        IRefCountChangeEvent IObjectRefTrace.LastDestroyEvent => LastDestroyEvent;

        /// <summary>
        /// Object destroy event from Object Reference Tracing, otherwise the last handle close or file unmap event is returned.
        /// </summary>

        [JsonIgnore]

        public RefCountChangeEvent LastDestroyEvent
        {
            get
            {
                RefCountChangeEvent last = DestroyEvent;
                if (last == null)
                {
                    if (IsFileMap && FileUnmapEvents.Count > 0)
                    {
                        var lastUnmap = FileUnmapEvents.OrderBy(x => x.TimeNs).Last();
                        last = new RefCountChangeEvent(default(TraceTimestamp), -1, lastUnmap.ProcessIdx, lastUnmap.ThreadId, lastUnmap.StackIdx)
                        { 
                            TimeNs = lastUnmap.TimeNs 
                        };   
                    } else if( HandleCloseEvents.Count > 0 && ( HandleCreateEvents.Count - (HandleCloseEvents.Count+HandleDuplicateEvents.Count) == 0  ))
                    {
                        var lastClose = HandleCloseEvents.Last();
                        last = new RefCountChangeEvent(default(TraceTimestamp), -1, lastClose.ProcessIdx, lastClose.ThreadId, lastClose.StackIdx)
                        {
                            TimeNs = lastClose.TimeNs,
                        };
                    }

                    DestroyEvent = last;    
                }

                return last;
            }
        }

        /// <summary>
        /// Handle Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type id which is mapped to event type name of <see cref="IHandleObjectData.ObjectTypeMap"/>
        /// </summary>
        public UInt16 ObjectType { get; set; }

        /// <summary>
        /// Get Object Type string
        /// </summary>
        /// <param name="extract">Extracted data</param>
        /// <returns>Stringified type string.</returns>
        public string GetObjectType(IETWExtract extract)
        {
            string lret = "";
            if (ObjectType == HandleObjectData.FileMapTypeId)
            {
                lret = "FileMapping";
            }
            else
            {
                if (!extract.HandleData.ObjectTypeMap.TryGetValue(ObjectType, out lret))
                {
                    lret = "UnknownType";
                }
            }

            return lret;
        }

        /// <summary>
        /// Magic value for events where are not closed or we do not have the create time at hand.
        /// </summary>
        public static TimeSpan LeakTime { get; } = TimeSpan.FromSeconds(9999);

        /// <summary>
        /// Handle lifetime or <see cref="LeakTime"/> duration to indicate a potential leak.
        /// </summary>
        [JsonIgnore]
        public TimeSpan Duration
        {
            get
            {
                return FirstCreateEvent == null ? LeakTime : ((LastDestroyEvent == null) ? LeakTime : TimeSpan.FromTicks((LastDestroyEvent.TimeNs - FirstCreateEvent.TimeNs) / 100));
            }
        }

        /// <summary>
        /// Contains all handle create events if Handle tracing was enabled.
        /// </summary>
        public List<HandleCreateEvent> HandleCreateEvents { get; set; } = new();

        /// <summary>
        /// Contains all handle duplicate events if Handle tracing was enabled.
        /// </summary>
        public List<HandleDuplicateEvent> HandleDuplicateEvents { get; set; } = new();

        /// <summary>
        /// Contains all handle close events if Handle tracing was enabled.
        /// </summary>
        public List<HandleCloseEvent> HandleCloseEvents { get; set; } = new();

        /// <summary>
        /// Contains all object reference change events if ObjectRef tracing was enabled.
        /// </summary>
        public List<RefCountChangeEvent> RefChanges { get; set; } = new();

        /// <summary>
        /// Contains all file mapping events if VAMAP provider was enabled.
        /// </summary>
        public List<FileMapEvent> FileMapEvents { get; set; } = new();

        /// <summary>
        /// Contains all file unmapping events if VAMAP provider was enabled.
        /// </summary>
        public List<FileUnmapEvent> FileUnmapEvents { get; set; } = new();


        /// <summary>
        /// returns true when no object events are stored in object. False otherwise. 
        /// </summary>
        internal bool IsEmpty
        {
            get
            {
                return (CreateEvent == null && DestroyEvent == null &&
                         HandleCreateEvents.Count == 0 &&
                         HandleDuplicateEvents.Count == 0 &&
                         HandleCloseEvents.Count == 0 &&
                         RefChanges.Count == 0 &&
                         FileMapEvents.Count == 0 &&
                         FileUnmapEvents.Count == 0) ? true : false;
            }
        }

        /// <summary>
        /// To save space we set all empty collections to null before serialize. But after deserialize we 
        /// set them again here. 
        /// </summary>
        internal void RefreshCollectionsAfterDeserialize()
        {
            if (HandleCreateEvents == null)
            {
                HandleCreateEvents = new();
            }
            if (HandleDuplicateEvents == null)
            {
                HandleDuplicateEvents = new();
            }
            if (HandleCloseEvents == null)
            {
                HandleCloseEvents = new();
            }
            if (RefChanges == null)
            {
                RefChanges = new();
            }
            if (FileMapEvents == null)
            {
                FileMapEvents = new();
            }
            if (FileUnmapEvents == null)
            {
                FileUnmapEvents = new();
            }
        }

        /// <summary>
        /// True if object contains file map/unmap events. If it is false it can only contain object provider events.
        /// </summary>
        [JsonIgnore]
        public bool IsFileMap
        {
            get => FileMapEvents.Count > 0 || FileMapEvents.Count > 0;
        }

        /// <summary>
        /// True if object was created/duplicated from multiple processes. If it is false it can still be inherited by 
        /// child processes.
        /// </summary>
        [JsonIgnore]
        public bool IsMultiProcess
        {
            get
            {
                ETWProcessIndex? idx = null;
                foreach (var ch in RefChanges)
                {
                    if (idx == null)
                    {
                        idx = ch.ProcessIdx;
                        continue;
                    }
                    if (idx != ch.ProcessIdx)
                    {
                        return true;
                    }
                }

                foreach (var create in HandleCreateEvents)
                {
                    if (idx == null)
                    {
                        idx = create.ProcessIdx;
                        continue;
                    }

                    if (idx != create.ProcessIdx)
                    {
                        return true;
                    }
                }

                foreach (var duplicate in HandleDuplicateEvents)
                {
                    if (duplicate.ProcessIdx != duplicate.SourceProcessIdx)
                    {
                        return true;
                    }
                }

                foreach (var map in FileMapEvents)
                {
                    if ((idx == null))
                    {
                        idx = map.ProcessIdx;
                        continue;
                    }

                    if (map.ProcessIdx != idx)
                    {
                        return true;
                    }
                }

                foreach (var unmap in FileUnmapEvents)
                {
                    if ((idx == null))
                    {
                        idx = unmap.ProcessIdx; ;
                        continue;
                    }

                    if (unmap.ProcessIdx != idx)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        IReadOnlyList<IHandleCloseEvent> IObjectRefTrace.HandleCloseEvents => HandleCloseEvents;

        IReadOnlyList<IHandleCreateEvent> IObjectRefTrace.HandleCreateEvents => HandleCreateEvents;

        IReadOnlyList<IHandleDuplicateEvent> IObjectRefTrace.HandleDuplicateEvents => HandleDuplicateEvents;

        IReadOnlyList<IRefCountChangeEvent> IObjectRefTrace.RefChanges => RefChanges;

        IReadOnlyList<IFileMapEvent> IObjectRefTrace.FileMapEvents => FileMapEvents;

        IReadOnlyList<IFileMapEvent> IObjectRefTrace.FileUnmapEvents => FileUnmapEvents;

        IRefCountChangeEvent IObjectRefTrace.CreateEvent => CreateEvent;

        IRefCountChangeEvent IObjectRefTrace.DestroyEvent => DestroyEvent;

        bool? myIsOverlapped;

        /// <summary>
        /// When the same object is referenced multiple times by e.g. subsequent Create or DuplicateHandle events we know that
        /// two different handles can have an effect to the same object. 
        /// </summary>
        [JsonIgnore]
        public bool IsOverlapped
        {
            get
            {
                if( myIsOverlapped == null)
                {
                    myIsOverlapped = GetIsOverlapped();
                }
                return myIsOverlapped.Value;
            }
        }


        /// <summary>
        /// When the same object is referenced multiple times by e.g. subsequent Create or DuplicateHandle events we know that
        /// two different handles can have an effect to the same object. 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal bool GetIsOverlapped()
        {
            int refCount = 0;
            List<StackEventBase> allEvents = new List<StackEventBase> (HandleCreateEvents);
            allEvents.AddRange (HandleDuplicateEvents);
            allEvents.AddRange (HandleCloseEvents);
            allEvents.Sort( (x,y) => x.TimeNs.CompareTo(y.TimeNs));

            foreach (var cr in allEvents)
            {
                if (cr is HandleCreateEvent)
                {
                    refCount++;
                }
                else if (cr is HandleCloseEvent)
                {
                    refCount--;
                }
                else if (cr is HandleDuplicateEvent)
                {
                    refCount++;
                }
                else
                {
                    throw new InvalidOperationException("Invalid event found.");
                }

                if (refCount > 1)
                {
                    return true;
                }

            }

            return false;
        }


        /// <summary>
        /// Handle is opened but not closed. First the 
        /// </summary>
        /// <returns>True when leak was detected. False otherwise.</returns>
        internal void CheckLeakAndRemoveNonLeakingEvents()
        {
            if (HandleCreateEvents.Count + HandleDuplicateEvents.Count > 0)
            {
                // combine create/duplicate/close events into a time series 
                var events = HandleCreateEvents.Cast<StackEventBase>()
                                                .Concat(HandleCloseEvents.Cast<StackEventBase>())
                                                .Concat(HandleDuplicateEvents.Cast<StackEventBase>())
                                                .Concat(HandleCloseEvents).Cast<StackEventBase>()
                                                .OrderBy(x => x.TimeNs)
                                                .ToList();

                List<HandleCreateEvent> leakedCreate = new();
                HashSet<HandleCloseEvent> closed = new();
                List<HandleDuplicateEvent> leakedDuplicates = new();

                for (int i = 0; i < events.Count; i++)
                {
                    var ev = events[i];
                    if (ev is HandleCreateEvent create)
                    {
                        bool bClosed = false;
                        for (int k = i + 1; k < events.Count; k++)
                        {
                            if (events[k] is HandleCloseEvent close && close.HandleValue == create.HandleValue &&
                                                                       close.ProcessIdx == create.ProcessIdx &&
                                                                       !closed.Contains(close))
                            {
                                closed.Add(close);
                                bClosed = true;
                                break;
                            }
                        }

                        if (!bClosed)
                        {
                            leakedCreate.Add(create);
                        }
                    }
                    else if (ev is HandleDuplicateEvent duplicate)
                    {
                        bool bClosed = false;
                        for (int k = i + 1; k < events.Count; k++)
                        {
                            if (events[k] is HandleCloseEvent close && close.HandleValue == duplicate.HandleValue &&
                                                                       close.ProcessIdx == duplicate.ProcessIdx &&
                                                                       !closed.Contains(close))
                            {
                                closed.Add(close);
                                bClosed = true;
                                break;
                            }
                        }

                        if (!bClosed)
                        {
                            leakedDuplicates.Add(duplicate);
                        }
                    }
                }

                HandleCreateEvents = leakedCreate;
                HandleCloseEvents = new(); // by definition we are missing closed
                HandleDuplicateEvents = leakedDuplicates;
            }
            else if (RefChanges?.Count > 0)
            {
                int finalRefCount = 0;
                foreach (var change in RefChanges)
                {
                    finalRefCount += change.RefCountChange;
                }
            }
        }

        internal void AddRefChange(TraceTimestamp time, int refCountChange, ETWProcessIndex process, int threadId, StackIdx idx)
        {
            RefChanges.Add(new RefCountChangeEvent(time, refCountChange, process, threadId, idx));
        }

        internal void AddHandlCreate(TraceTimestamp time, ulong handleValue, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
        {
            var created = new HandleCreateEvent(time, handleValue, processIdx, threadId, stackIdx);
            HandleCreateEvents.Add(created);
        }

        internal void AddHandleDuplicate(TraceTimestamp time, uint sourceHandle, uint targetHandle, ETWProcessIndex processIdx, ETWProcessIndex sourceProcessIndex, int threadId, StackIdx stackIndex)
        {
            var duplicate = new HandleDuplicateEvent(time, targetHandle, sourceHandle, processIdx, sourceProcessIndex, threadId, stackIndex);
            HandleDuplicateEvents.Add(duplicate);
        }

        internal bool AddHandleClose(TraceTimestamp time, ulong handleValue, string handleName, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
        {
            if (Name != null && Name != handleName)
            {
                Name += " -- Other Name -- " + handleName; // should never happen but if it does we know we deal with corrupt data.
            }
            else
            {
                Name = handleName;
            }
            var closed = new HandleCloseEvent(time, handleValue, processIdx, threadId, stackIdx);

            HandleCloseEvents.Add(closed);

            if (HandleCreateEvents.Count + HandleDuplicateEvents.Count == HandleCloseEvents.Count)
            {
                // final release
                return true;
            }

            return false;
        }

        internal void AddFileMap(TraceTimestamp time, long viewBase, long viewSize, long fileObject, long byteOffset, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
        {
            FileMapEvents.Add(new FileMapEvent
            {
                TimeNs = time.Nanoseconds,
                ViewBase = viewBase,
                ViewSize = viewSize,
                FileObject = fileObject,
                ByteOffset = byteOffset,
                ProcessIdx = processIdx,
                ThreadId = threadId,
                StackIdx = stackIdx,
            });
        }

        internal void AddFileUnMap(TraceTimestamp time, long viewBase, long viewSize, long fileObject, long byteOffset, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
        {
            FileUnmapEvents.Add(new FileUnmapEvent
            {
                TimeNs = time.Nanoseconds,
                ViewBase = viewBase,
                ViewSize = viewSize,
                FileObject = fileObject,
                ByteOffset = byteOffset,
                ProcessIdx = processIdx,
                ThreadId = threadId,
                StackIdx = stackIdx,
            });
        }

    }
}
