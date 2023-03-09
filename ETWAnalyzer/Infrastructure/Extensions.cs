//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Infrastructure
{
    internal static class Extensions
    {
        public static T ETWMinBy<T,V>(this IEnumerable<T> input, Func<T,V> selector) where V : IComparable<V>
        {
            T lret = default(T);    
            V min = default(V);
            bool bFirst = true;
            foreach(var item in input)
            {
                V other = selector(item);
                if( bFirst)
                {
                    lret = item;
                    min = other;
                    bFirst = false;
                }

                if ( min.CompareTo(other) > 0 )
                {
                    min = other;
                    lret = item;
                }
            }

            return lret;
        }

        public static T ETWMaxBy<T, V>(this IEnumerable<T> input, Func<T, V> selector) where V : IComparable<V>
        {
            T lret = default(T);
            V max = default(V);
            bool bFirst = true;
            foreach (var item in input)
            {
                V other = selector(item);
                if (bFirst)
                {
                    lret = item;
                    max = other;
                    bFirst = false;
                }

                if (max.CompareTo(other) < 0)
                {
                    max = other;
                    lret = item;
                }
            }

            return lret;
        }

        /// <summary>
        /// Cut from a string a substring, where the start and length can be out of bounds and prepend it with ... to show that it is cutted.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="start">Characters to skip</param>
        /// <param name="length">Characters to take. If negative the string is cut from the end length characters.</param>
        /// <returns>Cuted string or original string if no cutting was applied.</returns>
        public static string CutMinMax(this string str, int start, int length)
        {
            if( str == null )
            {
                return null;
            }

            string lret = str;

            if (length < 0 ) // cut last n chars from string
            {
                int startIdx = str.Length + length;
                if (startIdx > 0)
                {
                    lret = "..." + str.Substring(startIdx);
                }
            }
            else
            {
                int startIdx = Math.Max(0,start);
                if( startIdx >= str.Length )
                {
                    startIdx = str.Length - 1;
                    if( startIdx < 0 )
                    {
                        startIdx = 0;
                    }
                }
                int maxLen = lret.Length - startIdx;
                int possibleLen = Math.Min(maxLen, length);
                lret = lret.Substring(startIdx, possibleLen);
                if (startIdx > 0)
                {
                    lret = "..." + lret;
                }

            }

            return lret;
        }

        /// <summary>
        /// Parse a min max string of the from dd, xx-yy
        /// </summary>
        /// <param name="minMaxStr"></param>
        /// <param name="allownegativeMax">When true the max value can be negative.</param>
        /// <returns></returns>
        public static KeyValuePair<int, int> GetMinMax(this string minMaxStr, bool allownegativeMax = false)
        {
            string[] minmax = minMaxStr.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

            int min = int.Parse(minmax[0], CultureInfo.InvariantCulture);
            int max = int.MaxValue;

            // if -MinMax 0-100 or -100 was given we treat negative values as positive upper bounds.
            if (minMaxStr.StartsWith("-"))
            {
                max = allownegativeMax ? (-1) * min : min;
                min = 0;
            }

            if (minmax.Length == 2)
            {
                max = int.Parse(minmax[1], CultureInfo.InvariantCulture);
            }

            return new KeyValuePair<int, int>(min, max);
        }

        public static KeyValuePair<ulong,ulong> GetMinMaxULong(this string minMaxStr)
        {
            string[] minmax = minMaxStr.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

            ulong min = ulong.Parse(minmax[0], CultureInfo.InvariantCulture);
            ulong max = ulong.MaxValue;

            if (minmax.Length == 2)
            {
                max = ulong.Parse(minmax[1], CultureInfo.InvariantCulture);
            }

            return new KeyValuePair<ulong, ulong>(min, max);
        }

        public static Tuple<double,double> GetMinMaxDouble(this string minStr, string maxStr)
        {
            double min = double.Parse(minStr);
            double max = double.MaxValue;
            if (!String.IsNullOrEmpty(maxStr))
            {
                max = double.Parse(maxStr);
            }

            return Tuple.Create(min, max);
        }

        public static Tuple<int,int> GetRange(this string topN, string skip)
        {
            int topnNr = int.Parse(topN, CultureInfo.InvariantCulture);
            int skipNr = 0;
            if( !String.IsNullOrEmpty(skip) )
            {
                skipNr = int.Parse(skip, CultureInfo.InvariantCulture);
            }

            return Tuple.Create(topnNr, skipNr);
        }
    }
}
