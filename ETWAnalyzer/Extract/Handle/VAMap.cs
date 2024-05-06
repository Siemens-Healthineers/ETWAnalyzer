using System;
using ETWAnalyzer.Extract.Common;
using Microsoft.Windows.EventTracing;

namespace ETWAnalyzer.Extract.Handle
{
    /// <summary>
    /// File map event produced by MiMapViewOfSection which is usually called by MapViewOfFile.
    /// </summary>
    public interface IFileMapEvent : IStackEventBase
    {
        /// <summary>
        /// Offset of mapped file
        /// </summary>
        long ByteOffset { get;  }

        /// <summary>
        /// Kernel pointer of open file object.
        /// </summary>
        long FileObject { get; }

        /// <summary>
        /// Various flags.
        /// </summary>
        long MiscInfo { get; }

        /// <summary>
        /// Returned pointer to which address the file was mapping in the target process.
        /// </summary>
        long ViewBase { get; }

        /// <summary>
        /// Mapped size of file in target process.
        /// </summary>
        long ViewSize { get; }
    }

    /// <summary>
    /// File map event produced by MiMapViewOfSection which is usually called by MapViewOfFile.
    /// </summary>
    public class FileMapEvent : StackEventBase, IFileMapEvent
    {

        /// <summary>
        /// Returned pointer to which address the file was mapping in the target process.
        /// </summary>

        public long ViewBase { get; set; }

        /// <summary>
        /// Kernel pointer of open file object.
        /// </summary>

        public long FileObject { get; set; }

        /// <summary>
        /// Various flags.
        /// </summary>

        public long MiscInfo { get; set; }

        /// <summary>
        /// Mapped size of file in target process.
        /// </summary>

        public long ViewSize { get; set; }

        /// <summary>
        /// Offset of mapped file
        /// </summary>

        public long ByteOffset { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="viewBase"></param>
        /// <param name="fileObject"></param>
        /// <param name="miscInfo"></param>
        /// <param name="viewSize"></param>
        /// <param name="byteOffset"></param>
        /// <param name="processIdx"></param>
        /// <param name="threadId"></param>
        /// <param name="stackIdx"></param>
        public FileMapEvent(TraceTimestamp time, long viewBase, long fileObject, long miscInfo, long viewSize, long byteOffset, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
            : base(time, processIdx, threadId, stackIdx)
        {
            ViewBase = viewBase;
            FileObject = fileObject;
            MiscInfo = miscInfo;
            ViewSize = viewSize;
            ByteOffset = byteOffset;
        }

        /// <summary>
        /// Empty file map object needed for deserialization.
        /// </summary>
        public FileMapEvent() : this(default(TraceTimestamp), 0, 0, 0, 0, 0, ETWProcessIndex.Invalid, 0, StackIdx.None)
        { }
    }


    /// <summary>
    /// File unmap event produced by MiUnmapViewOfSection which is called by UnmapViewOfFile
    /// </summary>
    public class FileUnmapEvent : FileMapEvent
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="viewBase"></param>
        /// <param name="fileObject"></param>
        /// <param name="miscInfo"></param>
        /// <param name="viewSize"></param>
        /// <param name="byteOffset"></param>
        /// <param name="processIdx"></param>
        /// <param name="threadId"></param>
        /// <param name="stackIdx"></param>
        public FileUnmapEvent(TraceTimestamp time, long viewBase, long fileObject, long miscInfo, long viewSize, long byteOffset, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
        : base(time, viewBase, fileObject, miscInfo, viewSize, byteOffset, processIdx, threadId, stackIdx)
        {
        }


        /// <summary>
        /// Empty file unmap event needed for deserialization.
        /// </summary>
        public FileUnmapEvent() : this(default(TraceTimestamp), 0, 0, 0, 0, 0, ETWProcessIndex.Invalid, 0, StackIdx.None)
        { }
    }
}
