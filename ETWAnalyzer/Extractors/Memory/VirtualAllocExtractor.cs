using ETWAnalyzer.Extract;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Processes;
using Microsoft.Windows.EventTracing.Streaming;
using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TraceReloggerLib;

namespace ETWAnalyzer.Extractors.Memory
{
    internal class VirtualAllocExtractor : ExtractorBase, IUnparsedEventConsumer
    {
        Guid PageFaultV2Guid = new Guid("3d6fa8d3-fe05-11d0-9dda-00c04fd7ba7c");
        IPendingResult<IStackDataSource> myStackSource;
        IPendingResult<IProcessDataSource> myProcessesSource;
        IPendingResult<ISymbolDataSource> mySymbolDataSource;

        List<VirtualAllocOrFreeEvent> myVirtualAllocOrFreeEvents = new List<VirtualAllocOrFreeEvent>();

        public VirtualAllocExtractor()
        {
        }

        public override void RegisterParsers(ITraceProcessor processor)
        {
            NeedsSymbols = true;

            myStackSource = processor.UseStacks();
            myProcessesSource = processor.UseProcesses();
            mySymbolDataSource = processor.UseSymbols();
            processor.UseStreaming().UseUnparsedEvents(this, new Guid[] { PageFaultV2Guid });
        }

        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            using var logger = new PerfLogger("Extract VirtualAlloc");
            Console.WriteLine($"Total virtual alloc/free events: {myVirtualAllocOrFreeEvents.Count}");
        }

        public void Process(TraceEvent eventContext)
        {
                ClassicEvent classicEvent = eventContext.AsClassicEvent;

                if (classicEvent.Version < 2)
                {
                    return;
                }

                int eventId = classicEvent.Id;

                const int virtualAllocEventId = 98;
                const int virtualFreeEventId = 99;

                if (eventId != virtualAllocEventId && eventId != virtualFreeEventId)
                {
                    return;
                }

                VirtualAlloc64EventData eventData;

                if (classicEvent.Is32Bit)
                {
                    if (classicEvent.Data.Length != Marshal.SizeOf<VirtualAlloc32EventData>())
                    {
                        throw new InvalidTraceDataException("Invalid virtual alloc/free event.");
                    }

                    VirtualAlloc32EventData thunk = MemoryMarshal.Read<VirtualAlloc32EventData>(classicEvent.Data);

                    eventData.Base = thunk.Base;
                    eventData.Size = thunk.Size;
                    eventData.ProcessId = thunk.ProcessId;
                    eventData.Flags = thunk.Flags;
                }
                else
                {
                    if (classicEvent.Data.Length != Marshal.SizeOf<VirtualAlloc64EventData>())
                    {
                        throw new InvalidTraceDataException("Invalid virtual alloc/free event.");
                    }

                    eventData = MemoryMarshal.Read<VirtualAlloc64EventData>(classicEvent.Data);
                }

                AddressRange addressRange = new AddressRange(new Address(eventData.Base),
                    unchecked((long)eventData.Size));
                uint processId = eventData.ProcessId;
                VirtualAllocFlags flags = eventData.Flags;
                Timestamp timestamp = classicEvent.Timestamp;
                uint threadId = classicEvent.ThreadId.Value;
                myVirtualAllocOrFreeEvents.Add(new VirtualAllocOrFreeEvent(addressRange, processId, flags, timestamp,
                    threadId, myProcessesSource, myStackSource));
        }

        public void ProcessFailure(FailureInfo failureInfo)
        {
            failureInfo.ThrowAndLogParseFailure();
        }

        struct VirtualAlloc64EventData
        {
            public ulong Base;
            public ulong Size;
            public uint ProcessId;
            public VirtualAllocFlags Flags;
        }

        struct VirtualAlloc32EventData
        {
        #pragma warning disable CS0649
            public uint Base;
            public uint Size;
            public uint ProcessId;
            public VirtualAllocFlags Flags;
        #pragma warning restore CS0649
        }

        // See:
        //   https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualalloc
        // and:
        //   https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualfree
        [Flags]
        enum VirtualAllocFlags : uint
        {
            None = 0,
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            Physical = 0x400000,
            ResetUndo = 0x1000000,
            LargePages = 0x20000000
        }

        class VirtualAllocOrFreeEvent
        {
            readonly IPendingResult<IProcessDataSource> pendingProcessDataSource;
            readonly IPendingResult<IStackDataSource> pendingStackDataSource;
            readonly uint threadId;

            public VirtualAllocOrFreeEvent(AddressRange addressRange, uint processId, VirtualAllocFlags flags,
                Timestamp timestamp, uint threadId, IPendingResult<IProcessDataSource> pendingProcessDataSource,
                IPendingResult<IStackDataSource> pendingStackDataSource)
            {
                this.pendingProcessDataSource = pendingProcessDataSource;
                this.pendingStackDataSource = pendingStackDataSource;
                this.threadId = threadId;

                AddressRange = addressRange;
                ProcessId = processId;
                Flags = flags;
                Timestamp = timestamp;
            }

            public AddressRange AddressRange { get; }
            public uint ProcessId { get; }
            public VirtualAllocFlags Flags { get; }
            public Timestamp Timestamp { get; }

            public IStackSnapshot Stack => pendingStackDataSource.Result.GetStack(Timestamp, threadId);

            public IProcess Process => pendingProcessDataSource.Result.GetProcess(Timestamp, ProcessId);
        }
    }
}
