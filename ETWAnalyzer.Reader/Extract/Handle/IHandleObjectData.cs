//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using ETWAnalyzer.Extract.Common;

namespace ETWAnalyzer.Extract.Handle
{
    /// <summary>
    /// This contains no aggregates but all ObjectRef and VAMAP traces.
    /// This includes Handle Create/Destroy/Duplicate          (Handle Traces)
    ///                      Handle Increase/Decrease RefCount (ObjectRef Traces)
    ///                      File Map/Unmap events             (VAMAP traces)
    /// </summary>
    public interface IHandleObjectData
    {
        /// <summary>
        /// Object Type id to type name map
        /// </summary>
        public IReadOnlyDictionary<UInt16, string> ObjectTypeMap {  get; }

        /// <summary>
        /// Contains Object and Handle ETW tracing data
        /// </summary>

        IReadOnlyList<IObjectRefTrace> ObjectReferences { get; }

        /// <summary>
        /// Stacks used by <see cref="ObjectReferences"/> which are referenced by StackIdx values
        /// </summary>
        IStackCollection Stacks { get; }

    }
}