//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;

namespace ETWAnalyzer.Extractors.Exceptions
{
    /// <summary>
    /// 
    /// </summary>
    public class ExceptionFilterItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string Stack { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ProcessName
        {
            get; set;
        }

        List<string> myProcessNames = new();

        private List<string> ProcessNames
        {
            get
            {
                if (!string.IsNullOrEmpty(ProcessName) && myProcessNames.Count == 0)
                {
                    myProcessNames = new List<string>(ProcessName.Split(SepChar, StringSplitOptions.RemoveEmptyEntries));
                }

                return myProcessNames;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsPositiveFilter
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string ExceptionType
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public List<string> ExceptionSubstrings
        {
            get; set;
        } = new List<string>();

        /// <summary>
        /// Substring of a call stack
        /// </summary>
        public string StackTracePart
        {
            get;
            set;
        }

        static readonly char[] SepChar = new char[] { ';' };

        List<string> myStackTraceParts = new();
        private List<string> StackTraceParts
        {
            get
            {
                if( !string.IsNullOrEmpty(StackTracePart) && myStackTraceParts.Count == 0)
                {
                    myStackTraceParts = new List<string>(StackTracePart.Split(SepChar, StringSplitOptions.RemoveEmptyEntries));
                }

                return myStackTraceParts;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public int ThreadId
        {
            get;
            set;
        }

        /// <summary>
        /// Used for DataBinding where a list of substrings is entered separated by ;
        /// </summary>
        [NonSerialized]
        string _Message;

        /// <summary>
        /// When a new filter is added it contains usually only one message substring
        /// If the List ExceptionSubstrings.Add is is used instead the UI becomes inconsistent because an empty string would be displayed.
        /// </summary>
        public string Message
        {
            get
            {
                return _Message;
            }
            set
            {
                _Message = value;
                ExceptionSubstrings.Clear();

                if( !string.IsNullOrEmpty(_Message) )
                {
                    ExceptionSubstrings.AddRange(_Message.Split(SepChar, StringSplitOptions.RemoveEmptyEntries));
                }
            }
        }

        /// <summary>
        /// Human understandable rationale why this exception, process, exception type is not important
        /// </summary>
        public string Reason
        {
            get;
            set;
        }

        /// <summary>
        /// Return true if no (null or empty) filter string on any property was set. Otherwise true. 
        /// </summary>
        public bool IsEmtpy
        {
            get
            {
                return string.IsNullOrEmpty(ProcessName) && string.IsNullOrEmpty(ExceptionType) && string.IsNullOrEmpty(Message) && string.IsNullOrEmpty(StackTracePart);
            }
        }


        /// <summary>
        /// Cache the result of string index calls 
        /// </summary>
        readonly Dictionary<string, bool> StackCompareCache = new();

        /// <summary>
        /// Cache the result of string index calls 
        /// </summary>
        readonly Dictionary<string, bool> SubstringCompareCache = new();

        /// <summary>
        /// Construct a new ExceptionFilterItem
        /// </summary>
        public ExceptionFilterItem()
        {
        }

         
        /// <summary>
        /// 
        /// </summary>
        /// <param name="exceptionProcess">Process with/without processid</param>
        /// <param name="exceptionType">Exact exception type</param>
        /// <param name="exceptionMessage">Message with/without substrings</param>
        /// <param name="exceptionStackTrace">Stacktrace with/without substrings</param>
        /// <param name="exceptionThreadId"></param>
        /// <returns></returns>
        public bool IsNewException(string exceptionProcess, string exceptionType, string exceptionMessage, string exceptionStackTrace, int exceptionThreadId)
        {
            if(exceptionProcess == null)
            {
                throw new ArgumentNullException(nameof(exceptionProcess));
            }
            // match is by default false since in case the filter is not set we have no reason to filter things awwy
            // the naming here is not really good. 
            bool msgMatch = false;
            bool typeMatch = false;
            bool processMatch = false;
            bool stackMatch = false;
            bool threadIdMatch = false;

            // truexxxMatch means that the filter really matches the entry
            bool trueMsgMatch = false;
            bool trueTypeMatch = false;
            bool trueStackMatch = false;
            bool trueProcessMatch = false;

            // StringComparison: 
            // string1.IndexOf(string2.StringComparison.OrdinalIgnoreCase)  => 0: Case insensitive without substring
            // string1.IndexOf(string2.StringComparison.Ordinal)            => 0: Case sensitive with/without substring

            if (exceptionMessage != null)
            {
                if (!SubstringCompareCache.TryGetValue(exceptionMessage, out msgMatch)) // Message of the arisen exception
                {
                    foreach (var filterSubstr in ExceptionSubstrings) // Elements of the splitted filter message by char ';'    e.g.: (Part1;Part2; ...)
                    {
                        if (exceptionMessage != null && exceptionMessage.IndexOf(filterSubstr, StringComparison.OrdinalIgnoreCase) != -1) 
                        {
                            // When arisen Exception Message is not null and filterSubstr is a part of the arisen Exceptionmessage (case insensitive)
                            msgMatch = true;
                            trueMsgMatch = true;
                            break;
                        }
                    }
                    SubstringCompareCache[exceptionMessage] = msgMatch;
                }
            }
            trueMsgMatch = msgMatch;

            if ( ExceptionSubstrings.Count == 0)
            {
                msgMatch = true;
            }

            // process names, exception type, message 
            StringComparison compareMode = IsPositiveFilter ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            if (string.IsNullOrEmpty(ExceptionType) )
            {
                typeMatch = true;
            }
            else 
            {
                
                if (!IsPositiveFilter && exceptionType == ExceptionType   ||      IsPositiveFilter && exceptionType != null && exceptionType.IndexOf(ExceptionType, compareMode) != -1)
                {
                    // ONLY matches when arisen Exception type is the exact same exception as the defined exception type in Filter
                    typeMatch = true;
                    trueTypeMatch = true;
                }
            }

            if (ThreadId == 0 || exceptionThreadId == 0)
            {
                threadIdMatch = IsPositiveFilter ? false : true;
            }
            else
            {
                threadIdMatch = ThreadId == exceptionThreadId;
            }

            // we use as filter e.g process(pid) 
            // process(4444) is then matching
            // If process is defined as filter then 
            // process(4444) is a substring of process
            if (string.IsNullOrEmpty(ProcessName))
            {
                processMatch = true;
            }
            else
            {
                for (int i = 0; i < ProcessNames.Count; i++) //  Processnames splitted by ';'    e.g.: (Process1;Process2;Process3.IsAlreadyIncluded)
                {
                    int startidx = exceptionProcess.IndexOf(ProcessNames[i], compareMode); // Compares arisen exception process with defined elements of ProcessNames in filter
                    if (startidx == 0)
                    { 
                        //When arisen Exception Processname exactly match the defined FilterProcessname
                        //or arisen Exception + Substring is a part of FilterProcessname


                        processMatch = true;
                        trueProcessMatch = true;
                        break;
                    }
                }
            }

            if ( StackTraceParts.Count == 0 ) //StaceTraceParts splitted by ';' e.g.: ( Stactrace1.Diagnostics;Stactrace2.System )
            {
                stackMatch = true;
            }
            else 
            {
                if (!StackCompareCache.TryGetValue(exceptionStackTrace ?? "", out stackMatch))
                {
                    for (int i = 0; i < StackTraceParts.Count; i++)
                    {
                        int startidx = exceptionStackTrace.IndexOf(StackTraceParts[i], compareMode);
                        if (startidx != -1)
                        {
                            // When arisen Exception stacktrace exactly matches the defined filter stacktrace
                            // or arisen Exception Stacktrace + substring matches Filter exception Stacktrace (case sensitve)
                            stackMatch = true;
                            trueStackMatch = true;
                            break;
                        }
                    }
                    StackCompareCache[exceptionStackTrace] = stackMatch;
                }
                trueStackMatch = stackMatch;
            }
            // Condition ? return1 by true : return2 by false;
            return IsPositiveFilter ? trueProcessMatch || trueTypeMatch || trueMsgMatch || trueStackMatch || threadIdMatch || IsEmtpy :
                                      !( processMatch && typeMatch && msgMatch && stackMatch && threadIdMatch );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[Postive: {IsPositiveFilter}] Process {ProcessName}, {ExceptionType}, {Message}, {StackTracePart}, {Reason}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal ExceptionFilterItem Clone()
        {
            return new ExceptionFilterItem()
            {
                ExceptionType = ExceptionType,
                Message = Message,
                ProcessName = ProcessName,
                Reason = Reason,
                StackTracePart = StackTracePart,
                IsPositiveFilter = IsPositiveFilter,
            };
        }
    }
}
