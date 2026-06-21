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
        /// When set to true the <see cref="WithDigitGrouping(IFormattable)"/> and <see cref="WidthFormat(string, object, int)"/> methods
        /// omit the N0 digit grouping (thousands separator) and use the natural number format instead.
        /// Enabled by the -NoDigitSep dump command flag.
        /// </summary>
        public static bool NoDigitGrouping { get; set; }

        /// <summary>
        /// Format a numeric value with N0 (digit grouping, e.g. 1,234,567) or with the natural format without digit grouping (e.g. 1234567)
        /// when <see cref="NoDigitGrouping"/> is set.
        /// </summary>
        /// <param name="value">Numeric value to format.</param>
        /// <returns>Formatted string.</returns>
        public static string WithDigitGrouping(this IFormattable value)
        {
            if (value == null)
            {
                return "";
            }
            return value.ToString(NoDigitGrouping ? null : "N0", null);
        }

        /// <summary>
        /// Format a string with the format expression and then adds spaces before, after the string until the desired width for tabular output is reached.
        /// </summary>
        /// <param name="fmt"></param>
        /// <param name="arg"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static string WidthFormat(this string fmt, object arg, int width)
        {
            if (NoDigitGrouping && fmt == "N0")
            {
                fmt = "";
            }
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
