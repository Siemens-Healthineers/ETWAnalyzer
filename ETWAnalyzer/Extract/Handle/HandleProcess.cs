//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

namespace ETWAnalyzer.Extract.Handle
{
    /// <summary>
    /// Existing handle which belongs to a process
    /// </summary>
    public interface IHandleProcess
    {
        /// <summary>
        /// Handle value
        /// </summary>
        uint Handle { get; }

        /// <summary>
        /// Process Index
        /// </summary>
        ETWProcessIndex Process { get; }
    }

    /// <summary>
    /// Wrapper around existing handle list
    /// </summary>
    public class HandleProcess : IHandleProcess
    {
        /// <summary>
        /// Handle value
        /// </summary>
        public uint Handle { get; set; }
        /// <summary>
        /// Process Index
        /// </summary>
        public ETWProcessIndex Process { get; set; }
    }
}
