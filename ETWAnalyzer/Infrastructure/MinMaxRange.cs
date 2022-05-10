//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Infrastructure
{
    /// <summary>
    /// Define an inclusive Min-Max Range. If min or max is not defined the range is unlimitted.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class MinMaxRange<T> where T : struct, IComparable<T>
    {
        public T? Min { get; }
        public T? Max { get; }

        /// <summary>
        /// Create an unbound range.
        /// </summary>
        public MinMaxRange():this(null,null)
        {
        }

        /// <summary>
        /// Create a range 
        /// </summary>
        /// <param name="min">Lower bound, or null if no lower bound is needed.</param>
        /// <param name="max">Upper bound, or null if now upper bound is needed.</param>
        public MinMaxRange(T? min, T? max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Check if value is in inclusive range (value is element of [Min;Max])
        /// </summary>
        /// <param name="value">Input value</param>
        /// <returns>True if value is within Min &lt;= value &lt;= Max. False otherwise.</returns>
        public bool IsWithin(T value)
        {
            if ( (Min != null && value.CompareTo(Min.Value) < 0)  ||
                 (Max != null && value.CompareTo(Max.Value) > 0) )
            {
                return false;
            }

            return true;
        }
    }
}
