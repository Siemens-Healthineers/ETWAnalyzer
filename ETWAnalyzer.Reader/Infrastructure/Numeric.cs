//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        /// Calculate the percentile with interpolation between values. The returned value does not
        /// necessarily exist. It interpolates between two values based on the relative distance. 
        /// Excel uses this approach.
        /// </summary>
        /// <param name="values">Sequence of sorted values.</param>
        /// <param name="percentile">Percentile to calculate.</param>
        /// <returns>Median value which might be interpolated.</returns>
        public static float Percentile(this List<float> values, float percentile)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if( values.Count ==0 )
            {
                return 0;
            }

            int N = values.Count;
            float n = (N - 1) * percentile + 1;



            if (n == 1d)
            {
                return values[0];
            }
            else if (n == N)
            {
                return values[N - 1];
            }
            else
            {
                int k = (int)n;
                float d = n - k;
#if DEBUG

                Debug.Assert(values[k] >= values[k - 1], "Input array is not sorted!");
#endif
                return values[k - 1] + d * (values[k] - values[k - 1]);
            }
        }

        /// <summary>
        /// Calculate the percentile with interpolation between values. The returned value does not
        /// necessarily exist. It interpolates between two values based on the relative distance. 
        /// Excel uses this approach.
        /// </summary>
        /// <param name="values">Sequence of sorted values.</param>
        /// <param name="percentile">Percentile to calculate.</param>
        /// <returns>Median value which might be interpolated.</returns>
        public static decimal Percentile(this List<decimal> values, decimal percentile)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.Count == 0)
            {
                return 0;
            }

            int N = values.Count;
            decimal n = (N - 1) * percentile + 1;



            if (n == 1.0m)
            {
                return values[0];
            }
            else if (n == N)
            {
                return values[N - 1];
            }
            else
            {
                int k = (int)n;
                decimal d = n - k;
#if DEBUG

                Debug.Assert(values[k] >= values[k - 1], "Input array is not sorted!");
#endif
                return values[k - 1] + d * (values[k] - values[k - 1]);
            }
        }


        /// <summary>
        /// Calculate the percentile with interpolation between values. The returned value does not
        /// necessarily exist. It interpolates between two values based on the relative distance. 
        /// Excel uses this approach.
        /// </summary>
        /// <param name="values">Sequence of sorted values.</param>
        /// <param name="percentile">Percentile to calculate.</param>
        /// <returns>Median value which might be interpolated.</returns>
        public static long Percentile(this List<long> values, float percentile)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.Count == 0)
            {
                return 0;
            }

            int N = values.Count;
            float n = (N - 1) * percentile + 1;



            if (n == 1.0f)
            {
                return values[0];
            }
            else if (n == N)
            {
                return values[N - 1];
            }
            else
            {
                int k = (int)n;
                float d = n - k;
#if DEBUG

                Debug.Assert(values[k] >= values[k - 1], "Input array is not sorted!");
#endif
                return (long) (values[k - 1] + d * (values[k] - values[k - 1]));
            }
        }
    }
}
