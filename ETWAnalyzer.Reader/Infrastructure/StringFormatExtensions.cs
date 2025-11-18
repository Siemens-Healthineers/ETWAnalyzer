//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Infrastructure
{
    internal static class StringFormatExtensions
    {
        /// <summary>
        /// New Line characters used for triming.
        /// </summary>
        internal static char[] NewLineChars = Environment.NewLine.ToCharArray();   

        /// <summary>
        /// Format a string with the format expression and then adds spaces before, after the string until the desired width for tabular output is reached.
        /// </summary>
        /// <param name="fmt"></param>
        /// <param name="arg"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static string WidthFormat(this string fmt, object arg, int width)
        {
            string str = string.Format("{0:"+fmt+"}", arg);
            return str.WithWidth(width);
        }

        /// <summary>
        /// Add spaces before, after the string until the desired width for tabular output is reached.
        /// </summary>
        /// <param name="str">String to potentially widen</param>
        /// <param name="width">width. If positive spaces before the string are added, otherwise afterwards.</param>
        /// <returns>String with at least the input width</returns>
        public static string WithWidth(this string str, int width)
        {
            if (width == 0)
            {
                return str;
            }
            else
            {
                string widthFmt = "{0," + width + "}";
                return String.Format(widthFmt, str);
            }
        }
    }
}
