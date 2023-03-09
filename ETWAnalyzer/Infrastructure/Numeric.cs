//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Infrastructure
{
    internal static class Numeric
    {
        /// <summary>
        /// Calculate median of an array of doubles. The input array is sorted ascending!
        /// </summary>
        /// <param name="xs">input list which is sorted after this method was called!</param>
        /// <returns>Median value</returns>
        public static double Median(this List<double> xs)
        {
            if (xs.Count > 0)
            {
                xs.Sort();
                double mid = (xs.Count - 1) / 2.0;
                return (xs[(int)(mid)] + xs[(int)(mid + 0.5)]) / 2;
            }
            else
            {
                return 0.0d;
            }
        }
    }
}
