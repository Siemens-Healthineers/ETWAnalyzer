using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Common;
using ETWAnalyzer.Extract.Handle;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Diagnostics.Tracing.AutomatedAnalysis;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Events;
using Microsoft.Windows.EventTracing.Processes;
using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ETWAnalyzer.Extractors.Handle
{

    /// <summary>
    /// Object Reference Extractor
    /// </summary>
    internal class ObjectRefExtractor : ExtractorBase
    {
        /// <summary>
        /// Guid for Object Tracing which contains the events for Handle and ObjectRef tracing
        /// </summary>
        Guid ObTraceGuid = new Guid("89497f50-effe-4440-8cf2-ce6b1cdcaca7");

        /// <summary>
        /// Guid for VAMAP calls
        /// </summary>
        Guid MapTraceGuid = new Guid("90cbdc39-4a3e-11d1-84f4-0000f80464e3");

        IPendingResult<IStackDataSource> myStackSource;
        IPendingResult<IProcessDataSource> myProcessesSource;
        IPendingResult<ISymbolDataSource> mySymbolDataSource;

        /// <summary>
        /// Create Handle event which contains Handle value
        /// </summary>
        const int CreateHandle = 0x20;

        /// <summary>
        /// Close Handle event
        /// </summary>
        const int CloseHandle = 0x21;

        /// <summary>
        /// Duplicate Handle event
        /// </summary>
        const int DuplicateHandle = 0x22;

        /// <summary>
        /// Enumerates all Handle types which is not a fixed list across Windows editions
        /// </summary>
        const int TypeDCEnd = 0x25;

        /// <summary>
        /// At trace start end all already open handles are dumped via this event
        /// </summary>
        const int HandleDCEnd = 0x27;
        
        /// <summary>
        /// Kernel object manager create object event
        /// </summary>
        const int CreateObject = 0x30;

        /// <summary>
        /// Kernel object manager delete object
        /// </summary>
        const int DeleteObject = 0x31;

        /// <summary>
        /// Kernel object manager object refcount increase
        /// </summary>
        const int IncreaseObjectRefCount = 0x32;

        /// <summary>
        /// Kernel object manager object refcount decrease
        /// </summary>
        const int DecreaseObjectRefCount = 0x33;

        /// <summary>
        /// File Mapping Provider
        /// </summary>
        const int MapViewOfFile = 0x25;

        /// <summary>
        ///  File Mapping Provider
        /// </summary>
        const int UnmapViewOfFile = 0x26;

        /// <summary>
        /// All handle related events 
        /// </summary>
        List<ObjectTraceBase> myObjectTraceEvents = new();

        public ObjectRefExtractor()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="processor"></param>
        /// <exception cref="InvalidTraceDataException"></exception>
        public override unsafe void RegisterParsers(ITraceProcessor processor)
        {
            NeedsSymbols = true;

            myStackSource = processor.UseStacks();
            myProcessesSource = processor.UseProcesses();
            mySymbolDataSource = processor.UseSymbols();

            TraceEventCallback handleKernelMemoryEvent = (EventContext eventContext) =>
            {
                ClassicEvent classicEvent = eventContext.Event.AsClassicEvent;

                int eventId = classicEvent.Id;
                int processId = 4;
                if (classicEvent.ProcessId != null) // assign unknown processes to System 
                {
                    processId = unchecked((int)classicEvent.ProcessId.Value);
                }
                TraceTimestamp timestamp = classicEvent.Timestamp;
                int threadId = classicEvent.ThreadId.GetValueOrDefault();

                if( classicEvent.ProviderId == ObTraceGuid)
                {
                    ParseObjectProviderEvent(classicEvent, eventId, processId, timestamp, threadId);
                }
                else if( classicEvent.ProviderId == MapTraceGuid)
                {
                    ParseVAMapEvent(classicEvent, eventId, processId, timestamp, threadId);
                }

            };
           
            processor.Use(new Guid[] { ObTraceGuid, MapTraceGuid }, handleKernelMemoryEvent);

        }

        private unsafe void ParseVAMapEvent(ClassicEvent classicEvent, int eventId, int processId, TraceTimestamp timestamp, int threadId)
        {
            switch (eventId)
            {
                case MapViewOfFile:
                    if (classicEvent.Data.Length != Marshal.SizeOf<MapFileETW>())
                    {
                        throw new InvalidTraceDataException("Invalid MapFileETW event.");
                    }
                    MapFileETW mapView;
                    mapView = MemoryMarshal.Read<MapFileETW>(classicEvent.Data);
                    myObjectTraceEvents.Add(new MapFileEvent
                    {
                        TimeStamp = timestamp,
                        ProcessId = processId,
                        ThreadId = threadId,
                        ViewBase = mapView.ViewBase,
                        FileObject = mapView.FileKey,
                        MiscInfo = mapView.MiscInfo,
                        ViewSize = mapView.ViewSize,
                        ObjectPtr = mapView.ViewBase | (long)processId << 42,  // use tuple of process Id and ViewBase. Most Intel processors do not support more than 40 address bits so we can use this as unique key
                    });
                    break;
                case UnmapViewOfFile:
                    if (classicEvent.Data.Length != Marshal.SizeOf<MapFileETW>())
                    {
                        throw new InvalidTraceDataException("Invalid Un/MapFileETW event.");
                    }
                    MapFileETW unmapView;
                    unmapView = MemoryMarshal.Read<MapFileETW>(classicEvent.Data);
                    myObjectTraceEvents.Add(new UnMapFileEvent
                    {
                        TimeStamp = timestamp,
                        ProcessId = processId,
                        ThreadId = threadId,
                        ViewBase = unmapView.ViewBase,
                        FileObject = unmapView.FileKey,
                        MiscInfo = unmapView.MiscInfo,
                        ViewSize = unmapView.ViewSize,
                        ObjectPtr = unmapView.ViewBase | (long) processId << 42, // use tuple of process Id and ViewBase. Most Intel processors do not support more than 40 address bits so we can use this as unique key
                    });
                    break;
                default:
                    break;
            }
        }

        private unsafe void ParseObjectProviderEvent(ClassicEvent classicEvent, int eventId, int processId, TraceTimestamp timestamp, int threadId)
        {
            switch (eventId)
            {
                case CreateHandle:
                    if (classicEvent.Data.Length != Marshal.SizeOf<CreateHandleETW>())
                    {
                        throw new InvalidTraceDataException("Invalid CreateHandle event.");
                    }
                    CreateHandleETW createHandle;
                    createHandle = MemoryMarshal.Read<CreateHandleETW>(classicEvent.Data);
                    myObjectTraceEvents.Add(new CreateHandleEvent
                    {
                        TimeStamp = timestamp,
                        ProcessId = processId,
                        ThreadId = threadId,
                        ObjectPtr = createHandle.ObjectPtr,
                        ObjectType = createHandle.ObjectType,
                        HandleValue = createHandle.Handle,
                    });
                    break;
                case CloseHandle:
                    if (classicEvent.Data.Length < Marshal.SizeOf<CloseHandleETW>())
                    {
                        throw new InvalidTraceDataException("Invalid CloseHandleETW event.");
                    }

                    string eventName = null;
                    if (classicEvent.Data.Length > 16)
                    {
                        var strBytes = classicEvent.Data.Slice(14);
                        if (strBytes.Length > 2) // includes \0 
                        {
                            strBytes = strBytes.Slice(0, strBytes.Length - 2);
                        }
                        eventName = Encoding.Unicode.GetString(strBytes.ToArray());
                    }

                    CloseHandleETW closeHandle;
                    closeHandle = MemoryMarshal.Read<CloseHandleETW>(classicEvent.Data);
                    myObjectTraceEvents.Add(new CloseHandleEvent
                    {
                        TimeStamp = timestamp,
                        ProcessId = processId,
                        ThreadId = threadId,
                        ObjectPtr = closeHandle.ObjectPtr,
                        ObjectType = closeHandle.ObjectType,
                        HandleValue = closeHandle.Handle,
                        Name = eventName,
                    });
                    break;
                case DuplicateHandle:
                    if (classicEvent.Data.Length != Marshal.SizeOf<DuplicateHandleETW>())
                    {
                        throw new InvalidTraceDataException("Invalid DuplicateHandleETW event.");
                    }
                    DuplicateHandleETW duplicateHandle = MemoryMarshal.Read<DuplicateHandleETW>(classicEvent.Data);
                    myObjectTraceEvents.Add(new DuplicateObjectEvent
                    {
                        TimeStamp = timestamp,
                        ProcessId = processId,
                        ThreadId = threadId,

                        ObjectPtr = duplicateHandle.ObjectPtr,
                        ObjectType = duplicateHandle.ObjectType,
                        SourceHandle = duplicateHandle.SourceHandle,
                        SourceProcessId = duplicateHandle.SourceProcessId,
                        TargetHandle = duplicateHandle.TargetHandle,
                        TargetHandleId = duplicateHandle.TargetHandleId,
                    });
                    break;
                case HandleDCEnd:
                    // this event is not delivered although it is present according to TraceEvent
                    HandleDCEndETW handleDCEnd;
                    handleDCEnd = MemoryMarshal.Read<HandleDCEndETW>(classicEvent.Data);

                    break;
                case TypeDCEnd:
                    // this event is not delivered although it is present according to TraceEvent
                    break;
                case CreateObject:
                    CreateObjectETW creatObject;
                    if (classicEvent.Data.Length != Marshal.SizeOf<CreateObjectETW>())
                    {
                        throw new InvalidTraceDataException("Invalid CreateObject event.");
                    }
                    creatObject = MemoryMarshal.Read<CreateObjectETW>(classicEvent.Data);
                    myObjectTraceEvents.Add(new CreateObjectEvent
                    {
                        TimeStamp = timestamp,
                        ProcessId = processId,
                        ThreadId = threadId,
                        ObjectPtr = creatObject.ObjectPtr,
                        ObjectType = creatObject.ObjectType,
                    });
                    break;
                case DeleteObject:
                    DeleteObjectETW deleteEvent;
                    if (classicEvent.Data.Length != Marshal.SizeOf<DeleteObjectETW>())
                    {
                        throw new InvalidTraceDataException("Invalid DeleteObject event.");
                    }
                    deleteEvent = MemoryMarshal.Read<DeleteObjectETW>(classicEvent.Data);
                    myObjectTraceEvents.Add(new DeleteObjectEvent
                    {
                        TimeStamp = timestamp,
                        ProcessId = processId,
                        ThreadId = threadId,
                        ObjectPtr = deleteEvent.ObjectPtr,
                        ObjectType = deleteEvent.ObjectType,
                    });
                    break;
                case IncreaseObjectRefCount:
                    IncreaseObjectRefCountETW incRefCount;
                    if (classicEvent.Data.Length != Marshal.SizeOf<IncreaseObjectRefCountETW>())
                    {
                        throw new InvalidTraceDataException("Invalid IncreaseObjectRefCount event.");
                    }
                    incRefCount = MemoryMarshal.Read<IncreaseObjectRefCountETW>(classicEvent.Data);

                    myObjectTraceEvents.Add(new IncreaseRefCount
                    {
                        TimeStamp = timestamp,
                        ProcessId = processId,
                        ThreadId = threadId,
                        ObjectPtr = incRefCount.ObjectPtr,
                        Tag = incRefCount.Tag,
                        RefCount = incRefCount.RefCount,
                    });
                    break;
                case DecreaseObjectRefCount:
                    DecreaseObjectRefCountETW decRefCount;
                    if (classicEvent.Data.Length != Marshal.SizeOf<DecreaseObjectRefCountETW>())
                    {
                        throw new InvalidTraceDataException("Invalid DecreaseObjectRefCount event.");
                    }
                    decRefCount = MemoryMarshal.Read<DecreaseObjectRefCountETW>(classicEvent.Data);
                    myObjectTraceEvents.Add(new DecreaseRefCount
                    {
                        TimeStamp = timestamp,
                        ProcessId = processId,
                        ThreadId = threadId,
                        ObjectPtr = decRefCount.ObjectPtr,
                        Tag = decRefCount.Tag,
                        RefCount = (-1) * decRefCount.RefCount,
                    });
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Key is ObjectPtr which is the kernel object pointer 
        /// </summary>
        Dictionary<long, ObjectRefTrace> OpenHandles { get; set; } = new();    

        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            using var logger = new PerfLogger("Extract ObjectRef");
            StackPrinter printer = new StackPrinter();

            var objectEvents = myObjectTraceEvents.OrderBy(x => x.TimeStamp).ToList();

            for(int i=0;i<objectEvents.Count;i++)
            {
                var ev = objectEvents[i];

                if (ev.ProcessId == 0)
                {
                    continue;
                }

                var processIdx = results.GetProcessIndexByPidAtTime(ev.ProcessId, ev.TimeStamp.DateTimeOffset);
                if (processIdx == ETWProcessIndex.Invalid)
                {
                    // some events are logged only after process exit while cleaning up the handle table. Since we do not know how long the process did run we might subtract too much so we 
                    // do a few reasonable times here. 
                    processIdx = results.GetProcessIndexByPidAtTime(ev.ProcessId, ev.TimeStamp.DateTimeOffset-TimeSpan.FromMilliseconds(2));
                    if (processIdx == ETWProcessIndex.Invalid)
                    {
                        processIdx = results.GetProcessIndexByPidAtTime(WindowsConstants.SystemProcessId, ev.TimeStamp.DateTimeOffset);  //otherwise assign it to System 
                        if (processIdx == ETWProcessIndex.Invalid)
                        {     
                            if (processIdx == ETWProcessIndex.Invalid)
                            {
                                continue;
                            }
                        }
                    }
                }

                IStackSnapshot stack = ev.GetStack(myStackSource);
                string stackStr = "";
                if(stack != null)
                {
                    stackStr = printer.Print(stack);
                }

                StackIdx stackIndex = results.HandleData.Stacks.AddStack(stackStr);

                switch( ev )
                {
                    case CreateObjectEvent create:
                        var trace = new ObjectRefTrace()
                        {
                            ObjectPtr = create.ObjectPtr,
                            CreateEvent = new RefCountChangeEvent(create.TimeStamp, 1, processIdx, create.ThreadId, stackIndex),
                            ProcessIdx = processIdx,
                        };

                        if (OpenHandles.ContainsKey(trace.ObjectPtr))  // should not happen but the same object pointer can have a create without a delete event. Perhaps some events have gone missing
                        {
                            results.HandleData.ObjectReferences.Add(trace);
                        }
                        else
                        {
                            OpenHandles[create.ObjectPtr] = trace;
                        }
                        break;
                    case CreateHandleEvent createHandle:
                        if(OpenHandles.TryGetValue(createHandle.ObjectPtr, out ObjectRefTrace hTrace) )
                        {
                            hTrace.AddHandlCreate(createHandle.TimeStamp, createHandle.HandleValue, processIdx, createHandle.ThreadId,  stackIndex);
                        }
                        else // ObjectRef Traces are missing
                        {
                            trace = new ObjectRefTrace()
                            {
                                ObjectPtr = createHandle.ObjectPtr,
                                CreateEvent = new RefCountChangeEvent(createHandle.TimeStamp, 1, processIdx, createHandle.ThreadId, stackIndex),
                                ProcessIdx = processIdx,
                            };
                            trace.AddHandlCreate(createHandle.TimeStamp, createHandle.HandleValue, processIdx, createHandle.ThreadId, stackIndex);
                            OpenHandles.Add(trace.ObjectPtr, trace);
                        }
                        break;
                    case DuplicateObjectEvent duplicateObjectEvent:
                        var sourceProcessIndex = results.GetProcessIndexByPidAtTime(duplicateObjectEvent.SourceProcessId, ev.TimeStamp.DateTimeOffset);
                        if (processIdx == ETWProcessIndex.Invalid)
                        {
                            continue;
                        }

                        if (!OpenHandles.TryGetValue(duplicateObjectEvent.ObjectPtr, out hTrace))
                        {
                            hTrace = new ObjectRefTrace()
                            {
                                ObjectPtr = duplicateObjectEvent.ObjectPtr,
                                CreateEvent = new RefCountChangeEvent(duplicateObjectEvent.TimeStamp, 1, processIdx, duplicateObjectEvent.ThreadId, stackIndex),
                                ProcessIdx = processIdx,
                            };
                            OpenHandles.Add(hTrace.ObjectPtr, hTrace);
                        }
                        hTrace.AddHandleDuplicate(duplicateObjectEvent.TimeStamp, duplicateObjectEvent.SourceHandle, duplicateObjectEvent.TargetHandle, processIdx, sourceProcessIndex, duplicateObjectEvent.ThreadId, stackIndex);
                        break;
                    case CloseHandleEvent closeHandle:
                        if (OpenHandles.TryGetValue(closeHandle.ObjectPtr, out hTrace)) // handle is still known deleteobject is missing or was not enabled
                        {
                            if (hTrace.AddHandleClose(closeHandle.TimeStamp, closeHandle.HandleValue, closeHandle.Name, processIdx, closeHandle.ThreadId, stackIndex))
                            {
                                // final close reached
                                results.HandleData.ObjectReferences.Add(hTrace);
                                OpenHandles.Remove(closeHandle.ObjectPtr);
                            }
                        }
                        else
                        {
                            // already closed by DeleteObject event and added to list 
                            const int BackwardWalkCount = 50;
                            for(int k=results.HandleData.ObjectReferences.Count-1; k>0 && k > results.HandleData.ObjectReferences.Count- BackwardWalkCount; k--)
                            {
                                var objectTrace = results.HandleData.ObjectReferences[k];
                                if (objectTrace.ObjectPtr == closeHandle.ObjectPtr)
                                {
                                    objectTrace.AddHandleClose(closeHandle.TimeStamp, closeHandle.HandleValue, closeHandle.Name, processIdx, closeHandle.ThreadId, stackIndex);
                                    break;
                                }
                            }
                        }
                        break;
                    case DeleteObjectEvent del:
                        if (OpenHandles.TryGetValue(del.ObjectPtr, out trace))
                        {
                            trace.DestroyEvent = new RefCountChangeEvent(del.TimeStamp, -1, processIdx, del.ThreadId, stackIndex);
                            results.HandleData.ObjectReferences.Add(trace);
                            OpenHandles.Remove(del.ObjectPtr);  
                        }
                        break;
                    case IncreaseRefCount inc:
                        if (OpenHandles.TryGetValue(inc.ObjectPtr, out trace))
                        {
                            trace.AddRefChange(inc.TimeStamp, (int) inc.RefCount, processIdx, inc.ThreadId, stackIndex);
                        }
                        break;
                    case DecreaseRefCount dec:
                        if (OpenHandles.TryGetValue(dec.ObjectPtr, out trace))
                        {
                            trace.AddRefChange(dec.TimeStamp, (int)dec.RefCount, processIdx, dec.ThreadId, stackIndex);
                        }
                        break;
                    case UnMapFileEvent unmapFile:
                        if (OpenHandles.TryGetValue(unmapFile.ObjectPtr, out trace))
                        {
                            trace.AddFileUnMap(unmapFile.TimeStamp, unmapFile.ViewBase, unmapFile.ViewSize, unmapFile.FileObject, unmapFile.ByteOffset, processIdx, unmapFile.ThreadId, stackIndex);
                            results.HandleData.ObjectReferences.Add(trace);
                            OpenHandles.Remove(unmapFile.ObjectPtr);
                        }
                        break;
                    case MapFileEvent mapFile:
                        if (!OpenHandles.TryGetValue(mapFile.ObjectPtr, out trace))
                        {
                            trace = new()
                            {
                                ProcessIdx = processIdx,
                                ObjectPtr = mapFile.ObjectPtr,
                            };
                            OpenHandles.Add(mapFile.ObjectPtr, trace);
                        }
                        trace.AddFileMap(mapFile.TimeStamp, mapFile.ViewBase, mapFile.ViewSize, mapFile.FileObject, mapFile.ByteOffset, processIdx, mapFile.ThreadId, stackIndex);
                        break;

                    default:
                    break;
                };
            }

            // still open handles are added at the end
            results.HandleData.ObjectReferences.AddRange(OpenHandles.Values);

            // clean up resulting Json 
            foreach(var clean in results.HandleData.ObjectReferences) 
            {
                if (clean.FileMapEvents.Count == 0)
                {
                    clean.FileMapEvents = null;
                }
                if( clean.FileUnmapEvents.Count == 0)
                {
                    clean.FileUnmapEvents = null;
                }
                if (clean.RefChanges.Count == 0 )
                {
                    clean.RefChanges = null;
                }
                if( clean.HandleCloseEvents.Count ==0 )
                {
                    clean.HandleCloseEvents = null;
                }
                if( clean.HandleDuplicateEvents.Count ==0 )
                {
                    clean.HandleDuplicateEvents = null;
                }
                if( clean.HandleCreateEvents.Count == 0 )
                {
                    clean.HandleCreateEvents = null;
                }
            }
        }
    }
}
