//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Infrastructure
{
    static class EnumerableSorterExtensions
    {
        /// <summary>
        /// Sort an input sequence descending select the first n values of it
        /// </summary>
        /// <typeparam name="TDataType">Enumerable type</typeparam>
        /// <typeparam name="V">Sort key type</typeparam>
        /// <param name="data">Input sequence</param>
        /// <param name="keyselector">Keyselector by which sub property the sort is performed</param>
        /// <param name="topN">Return last N top elements</param>
        /// <returns>Array last topN results of sorted sequence</returns>
        public static TDataType[] SortDescendingGetFirstTopN<TDataType, V>(this IEnumerable<TDataType> data, Func<TDataType, V> keyselector, int topN)
        {
            return data.OrderByDescending(keyselector).Take(topN).ToArray();
        }

        /// <summary>
        /// Sort an input sequence ascending and then select the last topN values of it
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="data"></param>
        /// <param name="keyselector"></param>
        /// <param name="sortStatePreparer">Some sorting algos might need to accumulate some state which can be set here before the sort is performed.</param>
        /// <param name="topN"></param>
        /// <returns>Array with first topN results of sorted sequence</returns>
        public static TData[] SortAscendingGetTopNLast<TData, V>(this IEnumerable<TData> data, Func<TData, V> keyselector, Action<IEnumerable<TData>> sortStatePreparer, SkipTakeRange topN)
        {
            sortStatePreparer?.Invoke(data);
            var sorted = data.OrderBy(keyselector).ToArray();
            List<TData> lret = new List<TData>();
            int skipCount = sorted.Length - topN.TakeN >= 0 ? sorted.Length - topN.TakeN : 0;
            skipCount -= topN.SkipN;
            skipCount = Math.Max(0, skipCount);
            for (int i=skipCount; i<sorted.Length && lret.Count<topN.TakeN;i++)
            {
                lret.Add(sorted[i]);
            }
            return lret.ToArray();
        }
    }
}
