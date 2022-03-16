//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.Exceptions
{
    /// <summary>
    /// This class is used to perform in memory queries on deserialized Json payload for easy querying the contained data.
    /// </summary>
    public class ExceptionEventForQuery
    {
        /// <summary>
        /// 
        /// </summary>
        public ETWProcess Process { get; }

        /// <summary>
        /// 
        /// </summary>
        public DateTimeOffset Time { get; }

        /// <summary>
        /// 
        /// </summary>
        public string Stack { get; }

        /// <summary>
        /// 
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Exception Type
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        /// <param name="process"></param>
        /// <param name="time"></param>
        /// <param name="stack"></param>
        public ExceptionEventForQuery(string message, string type, ETWProcess process, DateTimeOffset time, string stack)
        {
            Message = message;
            Type = type;
            Process = process;
            Time = time;
            Stack = stack;
        }
    }
}
