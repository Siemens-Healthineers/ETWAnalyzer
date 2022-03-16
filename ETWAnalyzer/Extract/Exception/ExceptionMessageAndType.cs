//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

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
    public class ExceptionMessageAndType : IEquatable<ExceptionMessageAndType>
    {
        /// <summary>
        /// Raw exception message
        /// </summary>
        public string Message
        {
            get; set;
        }

        /// <summary>
        /// Exception type
        /// </summary>
        public string Type
        {
            get; set;
        }

        /// <summary>
        /// To save space for each event the process, time and thread are stored in separate arrays
        /// </summary>
        public List<ETWProcessIndex> Processes
        {
            get;
            set;
        } = new List<ETWProcessIndex>();

        /// <summary>
        /// Thread on which the exception did occur
        /// </summary>
        public List<int> Threads
        {
            get; set;
        } = new List<int>();

        /// <summary>
        /// Time on which the exception did occur
        /// </summary>
        public List<DateTimeOffset> Times
        {
            get; set;
        } = new List<DateTimeOffset>();


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Message.GetHashCode();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ExceptionMessageAndType other)
        {
            if (other == null)
            {
                return false;
            }

            return other.Message.Equals(Message, StringComparison.Ordinal);
        }

        internal void AddProcessTimeThread(ETWProcessIndex processIndex, int threadId, DateTimeOffset localTime)
        {
            Processes.Add(processIndex);
            Threads.Add(threadId);
            Times.Add(localTime);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ExceptionMessageAndType);
        }
    }
}
