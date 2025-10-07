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
    /// Query friendly container to read process data
    /// </summary>
    public class FileIOContainer
    {
        /// <summary>
        /// Full Path of accessed file
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Process accessing this file
        /// </summary>
        public ETWProcess Process { get; }

        /// <summary>
        /// IO Statistics for this file for this process
        /// </summary>
        public FileIOStatistics Stats { get; }

        internal FileIOContainer(string fileName, ETWProcess process, FileIOStatistics stats)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Process = process ?? throw new ArgumentNullException(nameof(process));
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        /// <summary>
        /// Total IO Duration for given file 
        /// </summary>
        public long TotalIODurationus
        {
            get => Stats?.Open?.Durationus ?? 0 + Stats?.Close?.Durationus ?? 0 + Stats?.Read?.Durationus ?? 0 + Stats?.Write?.Durationus ?? 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{FileName} {Process?.ProcessWithID} {Stats?.ToString()}";
        }
    }
}
