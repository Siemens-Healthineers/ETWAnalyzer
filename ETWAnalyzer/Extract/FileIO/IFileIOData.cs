//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;

namespace ETWAnalyzer.Extract.FileIO
{
    /// <summary>
    /// Query friendly reading interface which is normally different from the way how the data is stored on disk.
    /// </summary>
    public interface IFileIOData
    {
        /// <summary>
        /// Get File IO Data as flat list where for each file the process and its metrics are stored.
        /// </summary>
        /// <param name="processMapper">Usually the IETWExtract instance to map the process indices's to the actual process instance.</param>
        IReadOnlyList<FileIOContainer> GetFileNameProcessStats(IProcessExtract processMapper);
    }
}