//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.Analyzers
{
    /// <summary>
    /// Collection of Exception filter rules
    /// </summary>
    public class ExceptionFilters
    {
        /// <summary>
        ///  Filters
        /// </summary>
        public List<ExceptionFilterItem> Filters
        {
            get;
            set;
        } = new List<ExceptionFilterItem>();

        /// <summary>
        /// 
        /// </summary>
        public ExceptionFilters()
        {
        }

        /// <summary>
        /// Check if exception is important or not
        /// </summary>
        /// <param name="processWithId"></param>
        /// <param name="exceptionType"></param>
        /// <param name="message"></param>
        /// <param name="stackTrace"></param>
        /// <returns>true if exception is important, false otherwise</returns>
        public bool IsRelevantException(string processWithId, string exceptionType, string message, string stackTrace)
        {
            bool isOk = Filters.All(x => x.IsNewException(processWithId, exceptionType, message, stackTrace, 0));
            return isOk;
        }

        /// <summary>
        /// Check if exception is important or not
        /// </summary>
        /// <param name="processWithId"></param>
        /// <param name="exceptionType"></param>
        /// <param name="message"></param>
        /// <param name="stackTrace"></param>
        /// <param name="matchedWith"></param>
        /// <returns>true if exception is important, false otherwise</returns>
        public bool IsRelevantException(string processWithId, string exceptionType, string message, string stackTrace,out List<ExceptionFilterItem> matchedWith)
        {
            bool isNew = true;
            matchedWith = new List<ExceptionFilterItem>();
            foreach (var filteritem in Filters)
            {
                if(!filteritem.IsNewException(processWithId, exceptionType, message, stackTrace, 0))
                {
                    isNew = false;
                    matchedWith.Add(filteritem);
                }
            }
            return isNew;
        }

    }
}
