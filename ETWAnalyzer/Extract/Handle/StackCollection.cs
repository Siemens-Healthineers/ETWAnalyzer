using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.Handle
{
    /// <summary>
    /// Stack Index to StackCollection
    /// </summary>
    public enum StackIdx
    {
        /// <summary>
        /// Default is invalid
        /// </summary>
        None = -1,
    }


    /// <summary>
    /// Collection of stacks which are stored in an Index based list where <see cref="StackIdx"/> is used as lookup index.
    /// </summary>
    public class StackCollection
    {
        /// <summary>
        /// Used during create time to Map the Index to a gien stack
        /// </summary>
        [JsonIgnore]
        Dictionary<StackIdx, string> StackMap { get; } = new();

        /// <summary>
        /// Used during create time to map a stack string to an already existing index
        /// </summary>
        [JsonIgnore]
        Dictionary<string, StackIdx> Stack2Idx { get; } = new();

        /// <summary>
        /// This holds the complete list 
        /// </summary>
        public List<string> StackList { get; } = new();


        /// <summary>
        /// Add stack to collection
        /// </summary>
        /// <param name="stack">String version of stack</param>
        /// <returns>StackIdx which is the index the the <see cref="StackList"/> collection. The list does not contain duplicates if multiple times the same stack is added the same index is returned.</returns>
        public StackIdx AddStack(string stack)
        {
            StackIdx idx;
            if (!Stack2Idx.ContainsKey(stack))
            {
                StackList.Add(stack);
                idx = (StackIdx)StackList.Count-1;

                Stack2Idx.Add(stack, idx);
                StackMap[idx] = stack;
            }
            else
            {
                idx = Stack2Idx[stack];
            }

            return idx;
        }

        /// <summary>
        /// Get Stack trace for given index
        /// </summary>
        /// <param name="idx"></param>
        /// <returns>Stringified stack trace</returns>
        public string GetStack(StackIdx idx)
        {
            return StackList[(int)idx];
        }

        /// <summary>
        /// Used by Serializer to fill read only properties.
        /// </summary>
        /// <param name="stackList"></param>
        public StackCollection(List<string> stackList)
        {
            StackList = stackList;
        }

        /// <summary>
        /// Default ctor
        /// </summary>
        public StackCollection()
        { }
    }


    /// <summary>
    /// Base class for stack based events which contain a timestamp, process, thread and stack
    /// </summary>
    public class StackEventBase
    {
        /// <summary>
        /// Time
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// Stack Index
        /// </summary>
        public StackIdx StackIdx { get; set; }

        /// <summary>
        /// Process Index
        /// </summary>
        public ETWProcessIndex ProcessIndex { get; set; }

        /// <summary>
        /// Thread Id
        /// </summary>
        public int ThreadId { get; set; }

        /// <summary>
        /// Used by serializer to construct a valid instance
        /// </summary>
        /// <param name="time"></param>
        /// <param name="processIdx"></param>
        /// <param name="threadId"></param>
        /// <param name="stackIdx"></param>
        public StackEventBase(DateTimeOffset time, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
        {
            Time = time;
            StackIdx = stackIdx;
            ProcessIndex = processIdx;
            ThreadId = threadId;
            StackIdx = stackIdx;
        }
    }
}
