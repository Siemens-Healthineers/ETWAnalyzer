//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.Common;
using ETWAnalyzer.Infrastructure;
using System;
using System.Collections.Generic;

namespace ETWAnalyzer.Extract.Handle
{
    /// <summary>
    /// Handle Tracing data.
    /// </summary>
    public class HandleObjectData : IHandleObjectData
    {
        /// <summary>
        /// Object Type id to type name map
        /// </summary>
        public Dictionary<UInt16, string> ObjectTypeMap { get; set; } = new();

        /// <summary>
        /// Contains Object and Handle ETW tracing data
        /// </summary>
        public List<ObjectRefTrace> ObjectReferences { get; set; } = new();

        /// <summary>
        /// Stacks are combined for Handle and Object Reference Tracing.
        /// </summary>
        public StackCollection Stacks { get; set; } = new();

        /// <summary>
        /// Needed to deserialize dependant stack collection file
        /// </summary>
        internal string DeserializedFileName { get; set; }

        IStackCollection IHandleObjectData.Stacks => myStackReader.Value;

        readonly Lazy<StackCollection> myStackReader;

        IReadOnlyList<IObjectRefTrace> IHandleObjectData.ObjectReferences => ObjectReferences;

        IReadOnlyDictionary<ushort, string> IHandleObjectData.ObjectTypeMap
        {
            get
            {
                if (ObjectTypeMap == null)  // legacy data might not have it set.
                {
                    ObjectTypeMap = new();
                }

                return ObjectTypeMap;
            }
        }
                

        /// <summary>
        /// Synthetic Type Id for file mapping events
        /// </summary>
        public const int FileMapTypeId = 1000;

        /// <summary>
        /// 
        /// </summary>
        public HandleObjectData()
        {
            myStackReader = new Lazy<StackCollection>(ReadHandleStacksFromExternalFile);
        }

        

        StackCollection ReadHandleStacksFromExternalFile()
        {
            StackCollection lret = Stacks;
            if (DeserializedFileName != null)
            {
                ExtractSerializer ser = new(DeserializedFileName);
                lret = ser.Deserialize<StackCollection>(ExtractSerializer.HandleStackPostFix);
            }

            return lret;
        }
    }
}
