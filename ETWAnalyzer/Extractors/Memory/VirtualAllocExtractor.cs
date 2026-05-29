using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Common;
using ETWAnalyzer.Extract.VirtualAlloc;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Processes;
using Microsoft.Windows.EventTracing.Streaming;
using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using PublicVirtualAllocFlags = ETWAnalyzer.Extract.VirtualAlloc.VirtualAllocFlags;

namespace ETWAnalyzer.Extractors.Memory
{
    /// <summary>
    /// Extract data from VirtualAlloc events which is the central OS API to request memory from OS in user mode.
    /// All other allocators (C/C++,.NET, Heaps) use underneath VirtalAlloc to allocate chunks of memory. 
    /// If a larger leak shows up it is not uncommon to find the root cause with the quite coarse grained VirtualAlloc events.
    /// </summary>
    internal class VirtualAllocExtractor : ExtractorBase, IUnparsedEventConsumer
    {
        Guid PageFaultV2Guid = new Guid("3d6fa8d3-fe05-11d0-9dda-00c04fd7ba7c");
        IPendingResult<IStackDataSource> myStackSource;
        IPendingResult<IProcessDataSource> myProcessesSource;
        IPendingResult<ISymbolDataSource> mySymbolDataSource;

        List<VirtualAllocOrFreeEvent> myVirtualAllocOrFreeEvents = new List<VirtualAllocOrFreeEvent>();

        /// <summary>
        /// When true, do not record allocation data for processes that have exited during the trace.
        /// </summary>
        public bool IgnoreExitedProcesses { get; set; }

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
            Logger.Info($"Total virtual alloc/free events: {myVirtualAllocOrFreeEvents.Count}");

            StackPrinter printer = new StackPrinter(StackFormat.DllAndMethod);

            // Key: (ProcessIdx, BaseAddress) -> pending commit event info
            // When a Decommit/Release matches, we remove from this dictionary
            var pendingCommits = new Dictionary<(ETWProcessIndex, ulong), PendingCommit>();

            // Per-process aggregate stats
            var statsMap = new Dictionary<ETWProcessIndex, VirtualAllocProcessStats>();

            foreach (var ev in myVirtualAllocOrFreeEvents)
            {
                if (ev.ProcessId == 0)
                {
                    continue;
                }

                DateTimeOffset evTime = ev.Timestamp.ConvertToTime();
                var processIdx = results.GetProcessIndexByPidAtTime(ev.ProcessId, evTime);
                if (processIdx == ETWProcessIndex.Invalid)
                {
                    processIdx = results.GetProcessIndexByPidAtTime(ev.ProcessId, evTime - TimeSpan.FromMilliseconds(2));
                    if (processIdx == ETWProcessIndex.Invalid)
                    {
                        continue;
                    }
                }

                var flags = (PublicVirtualAllocFlags)(uint)ev.Flags;
                long size = ev.AddressRange.Size.Bytes;
                ulong baseAddress = (ulong)ev.AddressRange.BaseAddress.Value;

                if (!statsMap.TryGetValue(processIdx, out var stats))
                {
                    stats = new VirtualAllocProcessStats { ProcessIdx = processIdx };
                    statsMap[processIdx] = stats;
                }

                bool isCommit = (flags & PublicVirtualAllocFlags.Commit) != 0;
                bool isFree = (flags & (PublicVirtualAllocFlags.Decommit | PublicVirtualAllocFlags.Release)) != 0;

                if (isCommit)
                {
                    stats.CommitCount++;
                    stats.CommittedSizeInBytes += size;
                    if (size > stats.MaxCommitSizeInBytes)
                    {
                        stats.MaxCommitSizeInBytes = size;
                    }

                    var key = (processIdx, baseAddress);
                    // Store pending commit with lazy stack resolution info
                    pendingCommits[key] = new PendingCommit
                    {
                        Event = ev,
                        ProcessIdx = processIdx,
                        Size = size,
                        BaseAddress = baseAddress,
                        Flags = flags,
                        TimeInSeconds = (float)(evTime - results.SessionStart).TotalSeconds,
                    };
                }

                if (isFree)
                {
                    stats.FreedCount++;
                    stats.FreedSizeInBytes += size;

                    var key = (processIdx, baseAddress);
                    pendingCommits.Remove(key);
                }
            }

            // Remove exited processes because they do not contribute to a leak by definition. This reduces the amount of Json data significantly.
            // The OS on process exit simply removes all memory pages. Hence for exited processes we see all not explicitly freed memory as leaked.
            if (IgnoreExitedProcesses)
            {
                var exitedKeys = new List<(ETWProcessIndex, ulong)>();
                foreach (var kvp in pendingCommits)
                {
                    if (results.GetProcess(kvp.Value.ProcessIdx).HasEnded)
                    {
                        exitedKeys.Add(kvp.Key);
                        statsMap.Remove(kvp.Value.ProcessIdx);
                    }
                }
                foreach (var key in exitedKeys)
                {
                    pendingCommits.Remove(key);
                }
            }

            // Remaining entries in pendingCommits are allocations that were never freed (potential leaks)
            foreach (var pending in pendingCommits.Values)
            {
                IStackSnapshot stack = pending.Event.Stack;
                string stackStr = "";
                if (stack != null)
                {
                    stackStr = printer.Print(stack);
                }

                StackIdx stackIndex = results.VirtualAllocData.Stacks.AddStack(stackStr);

                results.VirtualAllocData.VirtualAllocEvents.Add(new VirtualAllocEvent
                {
                    ProcessIdx = pending.ProcessIdx,
                    ThreadId = pending.Event.ThreadId,
                    BaseAddress = pending.BaseAddress,
                    Size = pending.Size,
                    Flags = pending.Flags,
                    TimeInSecondsSinceTraceStart = pending.TimeInSeconds,
                    StackIdx = stackIndex,
                });

                if (statsMap.TryGetValue(pending.ProcessIdx, out var stats))
                {
                    stats.NotReleasedCommitCount++;
                    stats.NotReleasedSizeInBytes += pending.Size;
                }
            }

            results.VirtualAllocData.PerProcessStats.AddRange(statsMap.Values.OrderByDescending(s => s.NotReleasedSizeInBytes));
        }

        /// <summary>
        /// Holds pending commit data until we know whether it was freed or not.
        /// </summary>
        class PendingCommit
        {
            public VirtualAllocOrFreeEvent Event;
            public ETWProcessIndex ProcessIdx;
            public long Size;
            public ulong BaseAddress;
            public PublicVirtualAllocFlags Flags;
            public float TimeInSeconds;
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

            public VirtualAllocOrFreeEvent(AddressRange addressRange, uint processId, VirtualAllocFlags flags,
                Timestamp timestamp, uint threadId, IPendingResult<IProcessDataSource> pendingProcessDataSource,
                IPendingResult<IStackDataSource> pendingStackDataSource)
            {
                this.pendingProcessDataSource = pendingProcessDataSource;
                this.pendingStackDataSource = pendingStackDataSource;
                ThreadId = threadId;

                AddressRange = addressRange;
                ProcessId = processId;
                Flags = flags;
                Timestamp = timestamp;
            }

            public AddressRange AddressRange { get; }
            public uint ProcessId { get; }
            public VirtualAllocFlags Flags { get; }
            public Timestamp Timestamp { get; }
            public uint ThreadId { get; }

            public IStackSnapshot Stack => pendingStackDataSource.Result.GetStack(Timestamp, ThreadId);

            public IProcess Process => pendingProcessDataSource.Result.GetProcess(Timestamp, ProcessId);
        }
    }
}
