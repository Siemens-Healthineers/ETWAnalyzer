//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ETWAnalyzer.Extractors
{
    /// <summary>
    /// Categories as members from an event
    /// </summary>
    public class ExceptionEvent
    {
        /// <summary>
        /// 
        /// </summary>
        public string ExceptionType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// 
        public string ExceptionMessage { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int ThreadId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTimeOffset TimeInSec { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Stack { get; set; }
    }


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
