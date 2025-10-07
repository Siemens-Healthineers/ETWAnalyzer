//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Extractors
{
    /// <summary>
    /// Categories from an event to extract from *.csv file
    /// </summary>
    class ExceptionRowData
    {
        public string ProcessNameAndPid { get; set; }
        public string ExceptionType { get; set; }
        public string ExceptionMessage { get; set; }
        public uint ThreadId { get; set; }
        public DateTimeOffset TimeInSec { get; set; }
        public string Stack { get; set; }
    }

}
