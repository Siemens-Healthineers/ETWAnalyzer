//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extractors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.Exceptions
{
    /// <summary>
    /// 
    /// </summary>
    public class ExceptionStackContainer
    {
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, HashSet<ExceptionMessageAndType>> Stack2Messages
        {
            get; set;
        } = new Dictionary<string, HashSet<ExceptionMessageAndType>>();

        internal void Add(IProcessExtract myProcessExtract, ExceptionRowData data)
        {
            if (!Stack2Messages.TryGetValue(data.Stack, out HashSet<ExceptionMessageAndType> messages))
            {
                messages = new HashSet<ExceptionMessageAndType>();
                Stack2Messages[data.Stack] = messages;
            }

            ExceptionMessageAndType tempMsg = new ExceptionMessageAndType
            {
                Message = data.ExceptionMessage,
                Type = data.ExceptionType,
            };


            if (!messages.TryGetValue(tempMsg, out ExceptionMessageAndType pooled))
            {
                messages.Add(tempMsg);
                pooled = tempMsg;
            }

            if (ETWExtract.IsValidProcessNameAndId(data.ProcessNameAndPid))
            {
                ETWProcessIndex processIndex = myProcessExtract.GetProcessIndex(data.ProcessNameAndPid);
                pooled.AddProcessTimeThread(processIndex, data.ThreadId, data.TimeInSec);
            }
        }
    }
}