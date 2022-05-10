//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Container for ETWMark events which are extracted when present
    /// </summary>
    public class ETWMark
    {
        /// <summary>
        /// 
        /// </summary>
        public DateTimeOffset Time { get; }

        /// <summary>
        /// Message
        /// </summary>
        public string MarkMessage { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="markMessage"></param>
        public ETWMark(DateTimeOffset time, string markMessage)
        {
            Time = time;
            MarkMessage = markMessage;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Time} {MarkMessage}";
        }
    }

}
