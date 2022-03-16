//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer
{
    class ExceptionDataCleaner
    {
        static string myCurrentYear = DateTime.Now.Year.ToString();

        static string[] RelevantStackIsBelowString = { "IL_Throw" };
        static string[] RelevantStackIsAboveString = { "System.Threading.Tasks.Task.Execute()" };

        const string CleanUpGuidRex = @"[\dabcdefABCDEF]{8}-([\dabcdefABCDEF]{4}-){3}[\dabcdefABCDEF]{8,14}";
        const string CleanUpDateRexBeforeYear = @"((((\d{1,2})|((Jan|Feb|Mar|May|June|July|Aug|Sept|Oct|Nov|Dec|Mon|Tue|Wed|Thu|Fri|Sat|Sun)[a-z,]{0,7}))[\s/]){1,3})";
        const string CleanUpDateRexAfterYear = @"((([\s:_T-]|[\./])\d{1,2}){1,6}([\d-:_\+]*)?\s?((PM)|(AM)|(GMT))?)";
        const string CleanUpFileNameRex = @"(?<=[a-zA-z]*\s?:\s?)'\s?[a-zA-Z]?:?([a-zA-z\\0-9_]*.?[a-z7]*)?\s?'";

        const string ReplacementOfTimeVariable = "  IsTimeStamp  ";
        const string ReplacementOfGuidVariable = "  IsGuid  ";
        const string ReplacementOfFileVariable = "  IsFileName  ";

        public static string CleanUpStack(string stack,char stackSeparator = '\n')
        {
            return GetFirstRowsAbove(GetLastRowsBelow(stack,stackSeparator),stackSeparator);
        }

        /// <summary>
        /// Selects n rows below the first exception 
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="stackSeparator"></param>
        /// <param name="nRows"></param>
        /// <returns></returns>
        internal static string GetLastRowsBelow(string stack,char stackSeparator='\n', int nRows = 4)
        {
            string[] splittedStack = stack.Split(stackSeparator);
            (int startIdx, int endIdx) = GetIndexesOfRelevantStackArea(splittedStack);

            if(startIdx != -1)
            {
                stack = "";
                for (int i = startIdx; i < nRows + startIdx && i < splittedStack.Length; i++)
                {
                    stack = String.Concat(stack, splittedStack[i], stackSeparator);
                }
            }
            return stack;
        }

        /// <summary>
        /// stack rows are relevant above the programm executes - selects n rows after the program executes
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="stackSeparator"></param>
        /// <param name="nRows"></param>
        /// <returns></returns>
        internal static string GetFirstRowsAbove(string stack,char stackSeparator='\n', int nRows = 500)
        {
            string[] splittedStack = stack.Split(stackSeparator);
            (int startIdx, int endIdx) = GetIndexesOfRelevantStackArea(splittedStack);

            if(endIdx != -1)
            {
                stack = "";
                for (int i = endIdx; i >= 0 && nRows > 0; i--)
                {
                    stack = String.Concat(splittedStack[i], stackSeparator, stack);
                    nRows--;
                }
            }
            return stack;
        }
        private static (int startIdxOfRelevantStack, int endIdxOfRelevantStack) GetIndexesOfRelevantStackArea(string[] splittedStack)
        {
            return (RelevantStackIsBelowString.Select(x => splittedStack.ToList().FindLastIndex(y => y.Contains(x))).Max(),
                    RelevantStackIsAboveString.Select(x => splittedStack.ToList().FindIndex(y => y.Contains(x))).Min());
        }

        /// <summary>
        /// Eliminates variables form message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string CleanUpMessage(string message)
            => CleanUpVariableDates(CleanUpGuid(CleanUpFileNames(message)));
        
        /// <summary>
        /// Eliminates variables from the message by replacing filenames with <see cref="ReplacementOfFileVariable"/>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static string CleanUpFileNames(string message)
            => new Regex(CleanUpFileNameRex).Replace(message, ReplacementOfFileVariable);

        /// <summary>
        /// Eliminates dates by replacing with <see cref="ReplacementOfTimeVariable"/>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static string CleanUpVariableDates(string message)
            => new Regex($"({CleanUpDateRexBeforeYear}{myCurrentYear}{CleanUpDateRexAfterYear})|({CleanUpDateRexBeforeYear}{myCurrentYear})|({myCurrentYear}{CleanUpDateRexAfterYear})").Replace(message, ReplacementOfTimeVariable);
        
        /// <summary>
        /// Eliminates guid by replacing with <see cref="ReplacementOfTimeVariable"/>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static string CleanUpGuid(string message) 
            => new Regex(CleanUpGuidRex).Replace(message, ReplacementOfGuidVariable);
    }
}
