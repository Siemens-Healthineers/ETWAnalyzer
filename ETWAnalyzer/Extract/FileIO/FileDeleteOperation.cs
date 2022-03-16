//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.FileIO
{
    /// <summary>
    /// File Deletion operations
    /// </summary>
    public class FileDeleteOperation
    {
        /// <summary>
        /// Number of times the file was deleted
        /// </summary>
        public long Count { get; set; }
    }
}
