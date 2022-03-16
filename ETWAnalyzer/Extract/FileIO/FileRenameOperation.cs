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
    /// File Renames
    /// </summary>
    public class FileRenameOperation
    {
        /// <summary>
        /// Number of times the file was renamed
        /// </summary>
        public long Count { get; set; }
    }
}
