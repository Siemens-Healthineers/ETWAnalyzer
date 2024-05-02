using System;

namespace ETWAnalyzer.Extract.Handle
{
    public class FileMapEvent : StackEventBase
    {
        public long ViewBase { get; set; }
        public long FileObject { get; set; }
        public long MiscInfo { get; set; }
        public long ViewSize { get; set; }
        public long ByteOffset { get; set; }

        public FileMapEvent(DateTimeOffset time, long viewBase, long fileObject, long miscInfo, long viewSize, long byteOffset, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
            : base(time, processIdx, threadId, stackIdx)
        {
            ViewBase = viewBase;
            FileObject = fileObject;
            MiscInfo = miscInfo;
            ViewSize = viewSize;
            ByteOffset = byteOffset;
        }

        public FileMapEvent() : this(default(DateTimeOffset), 0, 0, 0, 0,0, ETWProcessIndex.Invalid, 0, StackIdx.None)
        { }
    }

    public class FileUnmapEvent : FileMapEvent
    {
        public FileUnmapEvent(DateTimeOffset time, long viewBase, long fileObject, long miscInfo, long viewSize, long byteOffset, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
        : base(time, viewBase, fileObject, miscInfo, viewSize, byteOffset, processIdx, threadId, stackIdx)
        {
        }

        public FileUnmapEvent() : this(default(DateTimeOffset), 0, 0, 0, 0, 0, ETWProcessIndex.Invalid, 0, StackIdx.None)
        { }
    }
}
