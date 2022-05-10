//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

namespace ETWAnalyzer.Extract.Exceptions
{

    /// <summary>
    /// Contains data about all recorded .NET exceptions
    /// </summary>
    public interface IExceptionStats
    {
        /// <summary>
        /// Exception count
        /// </summary>
        int Count { get; }

        /// <summary>
        /// All exceptions with time stamps and stacks as flat list.
        /// </summary>
        ExceptionEventForQuery[] Exceptions { get; }
    }
}