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
        /// Contains Object and Handle ETW tracing data
        /// </summary>

        IReadOnlyList<IObjectRefTrace> ObjectReferences { get; }

        /// <summary>
        /// Stacks used by <see cref="ObjectReferences"/> which are referenced by StackIdx values
        /// </summary>
        IStackCollection Stacks { get; }

    }
}