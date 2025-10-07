//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.FileIO
{
    /// <summary>
    /// Contains duration, and file close counts
    /// </summary>
    public class FileCloseOperation
    {
        /// <summary>
        /// IO Duration summed across all threads in microseconds rounded to nearest value away from 0.
        /// This includes als the IO duration from File Cleanup events. The cleanup count is tracked in the <see cref="Cleanups"/> counter
        /// </summary>
        public long Durationus { get; set; }

        /// <summary>
        /// Number of times the file was closed
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        /// Number of file system Cleanup calls
        /// </summary>
        public long Cleanups { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Count: {Count} Duration: {Durationus/1000:N0} ms";
        }
    }
}
