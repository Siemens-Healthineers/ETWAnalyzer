//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ETWAnalyzer.Extract.Common
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
    public interface IStackCollection
    {
        /// <summary>
        /// Get Stack trace for given index
        /// </summary>
        /// <param name="idx"></param>
        /// <returns>Stringified stack trace. When idx was StackIdx.None an empty string is returned.</returns>
        string GetStack(StackIdx idx);
    }


    /// <summary>
    /// Collection of stacks which are stored in an Index based list where <see cref="StackIdx"/> is used as lookup index.
    /// </summary>
    public class StackCollection : IStackCollection
    {
        /// <summary>
        /// Used during create time to Map the Index to a given stack
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
                idx = (StackIdx)StackList.Count - 1;

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
            return idx == StackIdx.None ? "" : StackList[(int)idx];
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
}
