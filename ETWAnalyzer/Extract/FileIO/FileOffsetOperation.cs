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
    /// Contains metrics about how many bytes were read/written and how far the file was read corresponds to
    /// the file size if the file was read until the end.
    /// </summary>
    public class FileOffsetOperation
    {
        /// <summary>
        /// Maximum Offset+ r/w Size which corresponds to the file size if the file was read/written until the end
        /// </summary>
        public long MaxFilePosition { get; set; }

        /// <summary>
        /// Read/Written Bytes summed across all threads. The size is the requested buffer size by the Read/WriteFile Api call. 
        /// This is the reason why AccessedBytes can be much larger than the Maximum file position.
        /// </summary>
        public long AccessedBytes { get; set; }

        /// <summary>
        /// Number of operations performed
        /// </summary>
        public long Count;

        /// <summary>
        /// IO Duration summed across all threads in microseconds rounded to nearest value away from 0.
        /// This time includes also queuing time if async (overlapped/IO Completion Port) file APIs were used.
        /// </summary>
        public long Durationus { get; set; }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"MaxFilePos: {MaxFilePosition:N0} bytes Accessed: {AccessedBytes:N0} bytes Count: {Count} Duration: {Durationus / 1000:N0} ms";
        }
    }
}
