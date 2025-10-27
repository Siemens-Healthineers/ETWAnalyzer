//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
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
        /// <param name="defaultUnit"></param>
        /// <param name="allownegativeMax">When true the max value can be negative.</param>
        /// <returns></returns>
        public static KeyValuePair<int, int> GetMinMax(this string minMaxStr, decimal defaultUnit=1.0m, bool allownegativeMax = false)
        {
            string[] minmax = minMaxStr.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

            string number1 = GetUnit(minmax[0], out decimal? unitMultiplier);
            decimal unit = (unitMultiplier ?? defaultUnit);
            int min = (int) (int.Parse(number1, CultureInfo.InvariantCulture)* unit);
            int max = int.MaxValue;
         
            // if -MinMax 0-100 or -100 was given we treat negative values as positive upper bounds.
            if (minMaxStr.StartsWith("-"))
            {
                max = allownegativeMax ? (-1) * min : min;
                min = 0;
            }

            if (minmax.Length == 2)
            {
                string number2 = GetUnit(minmax[1], out decimal? unitMultiplier2);
                decimal unit2 = (unitMultiplier2 ?? defaultUnit);
                max = (int) (int.Parse(number2, CultureInfo.InvariantCulture)* unit2);
            }

            return new KeyValuePair<int, int>(min, max);
        }

        static List<KeyValuePair<string, decimal>> units = new()
        {
                new KeyValuePair<string,decimal>( "Bytes", 1m ),
                new KeyValuePair<string,decimal>( "Byte", 1m ),
                new KeyValuePair<string,decimal>( "KB",   1000.0m),
                new KeyValuePair<string,decimal>( "KiB",  1024*1024m),
                new KeyValuePair<string,decimal>( "MB",   1_000_000m ),
                new KeyValuePair<string,decimal>( "MiB",  1024*1024m ),
                new KeyValuePair<string,decimal>( "GB",   1_000_000_000m ),
                new KeyValuePair<string,decimal>( "GiB",  1024*1024*1024m ),
                new KeyValuePair<string,decimal>( "TB",   1_000_000_000_000m ),
                new KeyValuePair<string,decimal>( "TiB",  1024*1024*1024*1024m ),
                new KeyValuePair<string,decimal>( "B",    1m ), // order is important
                new KeyValuePair<string,decimal>( "ms",   1/1000.0m ),
                new KeyValuePair<string,decimal>( "us",   1/1_000_000.0m ),
                new KeyValuePair<string,decimal>( "ns",   1/1_000_000_000.0m ),
                new KeyValuePair<string,decimal>( "second", 1.0m),
                new KeyValuePair<string,decimal>( "seconds", 1.0m),
                new KeyValuePair<string,decimal>( "s",    1m ),  // must be last of seconds or it will match .EndsWith("seconds") resulting in invalid numbers
        };


        static internal string GetUnit(string input, out decimal? multiplier)
        {
            string plainNumber = input;
            multiplier = null;
            foreach(var unit in units)
            {
                if( input.EndsWith(unit.Key, StringComparison.OrdinalIgnoreCase))
                {
                    multiplier = unit.Value;
                    plainNumber = input.Substring(0, input.IndexOf(unit.Key, StringComparison.OrdinalIgnoreCase));
                    break;
                }
            }

            return plainNumber;
        }


        /// <summary>
        /// Convert a decimal value which can have decimal.MaxValue to an integer. 
        /// The multiplication is done in decimal value range and after that the integer conversion is done.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="multiplier"></param>
        /// <returns></returns>
        public static int? MultiplyToInt(this decimal ?input, decimal multiplier)
        {
            if( input == null )
            {
                return null;
            }   

            return input.Value == decimal.MaxValue ? int.MaxValue : (int)(input.Value * multiplier);
        }

        /// <summary>
        /// Multiply a nullable decimal value which can have decimal.MaxValue.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="multiplier"></param>
        /// <returns></returns>
        public static decimal? Multiply(this decimal ?input, decimal multiplier)
        {
            if( input == null )
            {
                return null;
            }
            return input.Value == decimal.MaxValue ? decimal.MaxValue : (input.Value * multiplier);
        }

        /// <summary>
        /// Multiply a nullable decimal value which can have decimal.MaxValue.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="multiplier"></param>
        /// <returns></returns>
        public static double? Multiply(this double? input, decimal multiplier)
        {
            if (input == null)
            {
                return null;
            }
            return input.Value == double.MaxValue ? double.MaxValue : (double) ((decimal) input.Value * multiplier);
        }

        /// <summary>
        /// Get from a min-max string the decimal min and max values with unit default unit multiplier applied.
        /// If units are part of the input string they override the default unit.
        /// Supported units are Bytes, Byte, KB, KiB, MB, MiB, GB, GiB, TB, TiB, B, ms, us, ns, second, seconds, s.
        /// </summary>
        /// <param name="minMaxStr"></param>
        /// <param name="defaultUnit">default unit multiplier</param>
        /// <returns>Parsed and multiplied value.</returns>
        public static KeyValuePair<decimal,decimal> GetMinMaxDecimal(this string minMaxStr, decimal defaultUnit)
        {
            string[] minmax = minMaxStr.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

            string number1 = GetUnit(minmax[0], out decimal? multiplier);

            decimal min = decimal.Parse(number1, CultureInfo.InvariantCulture) * (multiplier ?? defaultUnit);

            decimal max = decimal.MaxValue;

            if (minmax.Length == 2)
            {
                string number2 = GetUnit(minmax[1], out decimal? multiplier2);
                max = decimal.Parse(number2, CultureInfo.InvariantCulture) * (multiplier2 ?? defaultUnit);
            }

            return new KeyValuePair<decimal, decimal>(min, max);
        }

        public static long ParseLongFromHex(this string hexValue)
        {
            if (hexValue.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase) ||
                hexValue.StartsWith("&H", StringComparison.CurrentCultureIgnoreCase))
            {
                hexValue = hexValue.Substring(2);
            }

            return long.Parse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        public static KeyValuePair<ulong,ulong> GetMinMaxULong(this string minMaxStr, decimal defaultUnit)
        {
            string[] minmax = minMaxStr.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

            string number1 = GetUnit(minmax[0], out decimal? multiplier);
            ulong min = (ulong) (ulong.Parse(number1, CultureInfo.InvariantCulture)*(multiplier ?? defaultUnit));
            ulong max = ulong.MaxValue;

            if (minmax.Length == 2)
            {
                string number2 = GetUnit(minmax[1], out decimal? multiplier2);
                max = (ulong) ( ulong.Parse(number2, CultureInfo.InvariantCulture) * (multiplier2 ?? defaultUnit));
            }

            return new KeyValuePair<ulong, ulong>(min, max);
        }

        public static Tuple<long, long> GetMinMaxLong(this string minStr, string maxStr, decimal defaultUnit)
        {
            string minNumber = GetUnit(minStr, out decimal? multiplierMin);
            long min = (long)((decimal)long.Parse(minNumber) * (multiplierMin ?? defaultUnit));

            long max = long.MaxValue;
            if (!String.IsNullOrEmpty(maxStr))
            {
                string maxNumberStr = GetUnit(maxStr, out decimal? multiplierMax);
                max = (long)((decimal)long.Parse(maxNumberStr) * (multiplierMax ?? defaultUnit));
            }

            return Tuple.Create(min, max);
        }

        /// <summary>
        /// Get from a minStr maxStr string the decimal min and max values with unit default unit multiplier applied.
        /// If units are part of the input string they override the default unit.
        /// Supported units are Bytes, Byte, KB, KiB, MB, MiB, GB, GiB, TB, TiB, B, ms, us, ns, second, seconds, s.
        /// </summary>
        /// <param name="minStr">Minimum value string.</param>
        /// <param name="maxStr">Optional maximum value string</param>
        /// <param name="defaultUnit">default unit multiplier</param>
        /// <returns>Parsed and multiplied value.</returns>
        public static Tuple<int, int> GetMinMaxInt(this string minStr, string maxStr, decimal defaultUnit)
        {
            string minNumber = GetUnit(minStr, out decimal? multiplierMin);
            int min = (int)((decimal)int.Parse(minNumber) * (multiplierMin ?? defaultUnit));

            int max = int.MaxValue;
            if (!String.IsNullOrEmpty(maxStr))
            {
                string maxNumberStr = GetUnit(maxStr, out decimal? multiplierMax);
                max = (int) ((decimal)int.Parse(maxNumberStr) * (multiplierMax ?? defaultUnit));
            }

            return Tuple.Create(min, max);
        }

        /// <summary>
        /// Get from a minStr maxStr string the decimal min and max values with unit default unit multiplier applied.
        /// If units are part of the input string they override the default unit.
        /// Supported units are Bytes, Byte, KB, KiB, MB, MiB, GB, GiB, TB, TiB, B, ms, us, ns, second, seconds, s.
        /// </summary>
        /// <param name="minStr">Minimum value string.</param>
        /// <param name="maxStr">Optional maximum value string</param>
        /// <param name="defaultUnit">default unit multiplier</param>
        /// <returns>Parsed and multiplied value.</returns>

        public static Tuple<decimal, decimal> GetMinMaxDecimal(this string minStr, string maxStr, decimal defaultUnit)
        {
            string minNumber = GetUnit(minStr, out decimal? multiplierMin);
            decimal min = decimal.Parse(minNumber) * (multiplierMin ?? defaultUnit);

            decimal max = decimal.MaxValue;
            if (!String.IsNullOrEmpty(maxStr))
            {
                string maxNumberStr = GetUnit(maxStr, out decimal? multiplierMax);
                max = decimal.Parse(maxNumberStr) * (multiplierMax ?? defaultUnit);
            }

            return Tuple.Create(min, max);
        }

        /// <summary>
        /// Get from a minStr maxStr string the decimal min and max values with unit default unit multiplier applied.
        /// If units are part of the input string they override the default unit.
        /// Supported units are Bytes, Byte, KB, KiB, MB, MiB, GB, GiB, TB, TiB, B, ms, us, ns, second, seconds, s.
        /// </summary>
        /// <param name="minStr">Minimum value string.</param>
        /// <param name="maxStr">Optional maximum value string</param>
        /// <param name="defaultUnit">default unit multiplier</param>
        /// <returns>Parsed and multiplied value.</returns>

        public static Tuple<double,double> GetMinMaxDouble(this string minStr, string maxStr, decimal defaultUnit)
        {
            string minNumber = GetUnit(minStr, out decimal? multiplierMin);
            double min = (double) ( (decimal)double.Parse(minNumber) * (multiplierMin ?? defaultUnit));

            double max = double.MaxValue;
            if (!String.IsNullOrEmpty(maxStr))
            {
                string maxNumberStr = GetUnit(maxStr, out decimal? multiplierMax);
                max = (double) ( (decimal) double.Parse(maxNumberStr) * (multiplierMax ?? defaultUnit));
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
