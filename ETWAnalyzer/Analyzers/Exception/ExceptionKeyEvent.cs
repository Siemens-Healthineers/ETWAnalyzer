//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer
{
    /// <summary>
    /// Relevant Exception Information
    /// Compareable relevant Properties: FlatStack, Message and Type - Properties
    /// </summary>
    public class ExceptionKeyEvent : IEquatable<ExceptionKeyEvent>
    {
        /// <summary>
        /// Clearly identification by calculating the hashcode of the exception defining attributes
        /// </summary>
        public int ID => myID = myID == default ? string.Concat(ProcessNamePretty, FlatStack, FlatMessage, Type).GetHashCode():myID;
        private int myID = default;
        /// <summary>
        /// exception outgoing processname
        /// </summary>
        public string ProcessNamePretty { get; }


        /// <summary>
        /// Localization of the exception - Function cascade uncleaned
        /// </summary>
        [JsonProperty]
        public string Stack { get; }

        /// <summary>
        /// Data Cleaning Property - Localization of the exception - excluded variable substrings
        /// </summary>
        [JsonIgnore]
        public string FlatStack => myFlatStack ??= ExceptionDataCleaner.CleanUpStack(Stack);
        private string myFlatStack;

        /// <summary>
        /// Exception Message
        /// </summary>
        [JsonProperty]
        public string Message { get; }

        /// <summary>
        /// Data Cleaning Property - Exception message - excluded variable substrings
        /// </summary>
        [JsonIgnore]
        public string FlatMessage => myFlatMessage ??= ExceptionDataCleaner.CleanUpMessage(Message);
        private string myFlatMessage;

        /// <summary>
        /// Exception Type
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Occurrence Time
        /// </summary>
        public DateTimeOffset Time { get; }
        /// <summary>
        /// Number of duplicate exceptions
        /// </summary>
        [JsonIgnore]
        public ulong Occurrence { get; set; } = 1;
        /// <summary>
        /// Deep Copy - Constructor
        /// </summary>
        /// <param name="copyThis"></param>
        public ExceptionKeyEvent(ExceptionKeyEvent copyThis) : this(copyThis.ProcessNamePretty, copyThis.Stack, copyThis.Message, copyThis.Type, copyThis.Time) {}

        /// <summary>
        /// Generates an instance from by using the original uncleand properties
        /// </summary>
        /// <param name="processNamePretty">exception outgoing process</param>
        /// <param name="stack">exception outgoing functioncascade</param>
        /// <param name="message">exception message</param>
        /// <param name="type">exception type / specific class</param>
        /// <param name="time">appearence time</param>
        [JsonConstructor]
        public ExceptionKeyEvent(string processNamePretty, string stack, string message, string type, DateTimeOffset time)
        {
            Stack = stack;
            Message = message;
            Type = type;
            Time = time;
            ProcessNamePretty = processNamePretty;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ExceptionKeyEvent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ExceptionKeyEvent other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return FlatStack == other.FlatStack &&
                   FlatMessage == other.FlatMessage &&
                   Type == other.Type &&
                   ProcessNamePretty == other.ProcessNamePretty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hashCode = -1935525472;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FlatStack);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FlatMessage);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Type);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ProcessNamePretty);
            return hashCode;
        }

    }
}
