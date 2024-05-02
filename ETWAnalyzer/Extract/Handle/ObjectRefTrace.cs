using ETWAnalyzer.Extractors.Handle;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.Handle
{

    public class ObjectRefTrace
    {
        /// <summary>
        /// Process Index
        /// </summary>
        public ETWProcessIndex ProcessIndex { get; set; }

        /// <summary>
        /// Kernel object pointer. Value can be reused once object is closed.
        /// </summary>
        public long ObjectPtr { get; set; }

        /// <summary>
        /// Object creation event which originates from Object creation or handle creation event depending on which ETW providers were enabled during recording.
        /// </summary>
        public RefCountChangeEvent CreateEvent { get; set; }

        /// <summary>
        /// Object deletion event which originates from object ref DestroyObject or Handle close events
        /// </summary>
        public RefCountChangeEvent DestroyEvent { get; set; }

        /// <summary>
        /// Handle Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Handle lifetime or, if handle is created but not closed 3600s, or TimeSpan.MaxValue if handle was not created/closed during trace.
        /// </summary>
        [JsonIgnore]
        public TimeSpan Duration
        {
            get
            {
                return CreateEvent == null ? (DestroyEvent == null ? TimeSpan.MaxValue : TimeSpan.FromSeconds(3600)) : ((DestroyEvent == null) ? TimeSpan.FromSeconds(3600) : (DestroyEvent.Time - CreateEvent.Time));
            }
        }

        public ETWProcess GetProcess(IETWExtract extract) => extract.GetProcess(ProcessIndex);

        public List<HandleCreateEvent> HandleCreateEvents       { get; set; } = new();
        public List<HandleDuplicateEvent> HandleDuplicateEvents { get;set; } = new();
        public List<HandleCloseEvent> HandleCloseEvents         { get; set; } = new();

        public List<RefCountChangeEvent> RefChanges             { get; set; } = new();

        public List<FileMapEvent> FileMapEvents                 { get; set; } = new ();
        public List<FileUnmapEvent> FileUnmapEvents             { get; set; } = new();

        internal void RefreshCollectionsAfterDeserialize()
        {
            if( HandleCreateEvents == null )
            {
                HandleCreateEvents = new();
            }
            if( HandleDuplicateEvents == null)
            {
                HandleDuplicateEvents = new();
            }
            if( HandleCloseEvents == null)
            {
                HandleCloseEvents = new();
            }
            if( RefChanges == null)
            {
                RefChanges = new(); 
            }
            if( FileMapEvents == null)
            {
                FileMapEvents = new();
            }
            if(FileUnmapEvents == null)
            {
                FileUnmapEvents = new();    
            }
        }


        internal bool IsFileMap
        {
            get => FileMapEvents.Count > 0 ||  FileMapEvents.Count > 0;
        }

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
                        idx = ch.ProcessIndex;
                        continue;
                    } 
                    if (idx != ch.ProcessIndex)
                    {
                        return true;
                    }
                }

                foreach(var create in HandleCreateEvents)
                {
                    if(idx ==null)
                    {
                        idx = create.ProcessIndex;
                        continue;
                    }

                    if( idx != create.ProcessIndex)
                    {
                        return true;
                    }
                }

                foreach(var duplicate in  HandleDuplicateEvents)
                {
                    if( duplicate.ProcessIndex != duplicate.SourceProcessIdx)
                    {
                        return true;
                    }
                }

                return false;
            }
        }


        internal bool GetIsOverlapped()
        {
            int refCount = 0;
            foreach(var cr in HandleCreateEvents.Cast<StackEventBase>()
                             .Concat(HandleCloseEvents.Cast<StackEventBase>())
                             .Concat(HandleDuplicateEvents.Cast<StackEventBase>())
                             .OrderBy(x=>x.Time))
            {
                if( cr is HandleCreateEvent)
                {
                    refCount++;
                }else if( cr is HandleCloseEvent)
                {
                    refCount--;
                } else if ( cr is HandleDuplicateEvent)
                {
                    refCount++;
                }
                else
                {
                    throw new InvalidOperationException("Invalid event found.");
                }

                if( refCount > 1)
                {
                    return true;
                }

            }

            return false;
        }


        /// <summary>
        /// Handle is opened but not closed. First the 
        /// </summary>
        [JsonIgnore]
        public bool IsLeaked
        {
            get
            {
                if (HandleCreateEvents.Count > 0)
                {
                    // combine create/duplicate/close events into a time series 
                    var events = HandleCreateEvents.Cast<StackEventBase>()
                                                  .Concat(HandleCloseEvents.Cast<StackEventBase>())
                                                  .Concat(HandleDuplicateEvents.Cast<StackEventBase>())
                                                  .OrderBy(x => x.Time)
                                                  .ToList();

                    for(int i=0;i<events.Count; i++)
                    {
                        var ev = events[i];
                        if (ev is HandleCreateEvent create)
                        {
                            bool bClosed = false;
                            for (int k = i + 1; k < events.Count; k++)
                            {
                                if (events[k] is HandleCloseEvent close && close.HandleValue == create.HandleValue &&
                                                                    close.ProcessIndex == create.ProcessIndex)
                                {
                                    bClosed = true;
                                    break;
                                }
                            }

                            if (!bClosed)
                            {
                                return true;
                            }
                        }
                        else if( ev is HandleDuplicateEvent duplicate )
                        {
                            bool bClosed = false;
                            for(int k=i+1;k<events.Count;k++)
                            {
                                if (events[k] is HandleCloseEvent close && close.HandleValue == duplicate.HandleValue &&
                                                                           close.ProcessIndex == duplicate.ProcessIndex)
                                {
                                    bClosed = true;
                                    break;
                                } 
                            }

                            if( !bClosed )
                            {
                                return true;
                            }
                        }
                    }
                }
                else if( RefChanges?.Count > 0 )
                {
                    int finalRefCount = 0;
                    foreach(var  change in RefChanges) 
                    {
                        finalRefCount += change.RefCountChange;
                    }

                    if( finalRefCount > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void AddRefChange(DateTimeOffset time, int refCountChange, ETWProcessIndex process, int threadId, StackIdx idx)
        {
            RefChanges.Add(new RefCountChangeEvent(time, refCountChange, process, threadId, idx));
        }

        internal void AddHandlCreate(DateTimeOffset time, ulong handleValue, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
        {
            var created = new HandleCreateEvent(time, handleValue, processIdx, threadId, stackIdx);
            HandleCreateEvents.Add(created);

            if( CreateEvent == null)
            {
                CreateEvent = new RefCountChangeEvent(time, 1, processIdx, threadId, stackIdx);
            }
        }

        internal void AddHandleDuplicate(DateTimeOffset time, uint sourceHandle, uint targetHandle, ETWProcessIndex processIdx, ETWProcessIndex sourceProcessIndex, int threadId, StackIdx stackIndex)
        {
            var duplicate = new HandleDuplicateEvent(time, targetHandle, sourceHandle, processIdx, sourceProcessIndex, threadId, stackIndex);
            HandleDuplicateEvents.Add(duplicate);   
        }

        internal bool AddHandleClose(DateTimeOffset time, ulong handleValue, string handleName, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
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

            if(HandleCreateEvents.Count+HandleDuplicateEvents.Count == HandleCloseEvents.Count)
            {
                if (DestroyEvent == null)
                {
                    DestroyEvent = new RefCountChangeEvent(time, -1, processIdx, threadId, stackIdx);
                }
                // final release
                return true;
            }

            return false;
        }

        internal void AddFileMap(DateTimeOffset time, long viewBase, long viewSize, long fileObject, long byteOffset, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
        {
            FileMapEvents.Add(new FileMapEvent
            {
                Time = time,
                ViewBase = viewBase,
                ViewSize = viewSize,
                FileObject = fileObject,
                ByteOffset = byteOffset,
                ProcessIndex = processIdx,
                ThreadId = threadId,
                StackIdx = stackIdx,
            });
            CreateEvent = new RefCountChangeEvent()
            {
                Time = time,
                ProcessIndex = processIdx,
                ThreadId = threadId,
                StackIdx = stackIdx,
            };
        }

        internal void AddFileUnMap(DateTimeOffset time, long viewBase, long viewSize, long fileObject, long byteOffset, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
        {
            FileUnmapEvents.Add(new FileUnmapEvent
            {
                Time = time,
                ViewBase = viewBase,
                ViewSize = viewSize,
                FileObject = fileObject,
                ByteOffset = byteOffset,
                ProcessIndex = processIdx,
                ThreadId = threadId,
                StackIdx = stackIdx,
            });

            DestroyEvent = new RefCountChangeEvent()
            {
                Time = time,
                ProcessIndex = processIdx,
                ThreadId = threadId,
                StackIdx = stackIdx,
            };
        }

    }

    public class RefCountChangeEvent : StackEventBase
    {
        public int RefCountChange { get; set; }

        public RefCountChangeEvent(DateTimeOffset time, int refCountChange, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
            : base(time, processIdx, threadId, stackIdx)
        {
            RefCountChange = refCountChange;
        }

        public RefCountChangeEvent(DateTimeOffset time, int refCountChange, ETWProcessIndex process, int threadId)
            : this(time, refCountChange, process, threadId, StackIdx.None)
        { }

        public RefCountChangeEvent():this(default(DateTimeOffset), 0, ETWProcessIndex.Invalid, 0)
        { }
    }

    public class HandleCreateEvent : StackEventBase
    {
        public ulong HandleValue { get; set; }

        public HandleCreateEvent(DateTimeOffset time, ulong handleValue, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
            : base(time, processIdx, threadId, stackIdx)
        {
            HandleValue = handleValue;
        }

        public HandleCreateEvent() : this(default(DateTimeOffset), 0, ETWProcessIndex.Invalid, 0, StackIdx.None)
        { }

    }

    public class HandleDuplicateEvent : StackEventBase
    {
        public ulong HandleValue { get; set; }
        public ulong SourceHandleValue { get; set; }
        public ETWProcessIndex SourceProcessIdx { get; set; }

        public HandleDuplicateEvent(DateTimeOffset time, ulong handleValue, ulong sourceHandleValue, ETWProcessIndex processIdx, ETWProcessIndex sourceProcessIdx, int threadId, StackIdx stackIdx)
            : base(time, processIdx, threadId, stackIdx)
        {
            HandleValue = handleValue;
            SourceHandleValue = sourceHandleValue;
            SourceProcessIdx = sourceProcessIdx;
        }

        public HandleDuplicateEvent() : this(default(DateTimeOffset), 0,0, ETWProcessIndex.Invalid, ETWProcessIndex.Invalid, 0, StackIdx.None)
        { }
    }

    public class HandleCloseEvent : StackEventBase
    {
        public ulong HandleValue { get; set; }


        /// <summary>
        /// Needed for de/serialization 
        /// </summary>
        public HandleCloseEvent() : this(default(DateTimeOffset), 0, ETWProcessIndex.Invalid, 0, StackIdx.None)
        { }

        public HandleCloseEvent(DateTimeOffset time, ulong handleValue, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
            : base(time, processIdx, threadId, stackIdx)
        {
            HandleValue = handleValue;
        }
    }


}
