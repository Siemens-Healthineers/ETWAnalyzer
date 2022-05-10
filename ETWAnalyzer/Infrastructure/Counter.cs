//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Infrastructure
{
    /// <summary>
    /// A keyed counter which can increment a value by key.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Counter<T>
    {
        /// <summary>
        /// Underlying backing store which contains the counts
        /// </summary>
        Dictionary<T, int> myCounter = new();

        /// <summary>
        /// Increment for given key counter by integer
        /// </summary>
        /// <param name="key"></param>
        public void Increment(T key)
        { 
            if( !myCounter.TryGetValue(key, out int value) )
            {
                myCounter[key] = value;
            }

            myCounter[key] = ++value;
        }

        /// <summary>
        /// Get count of given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int this[T key]
        {
            get => myCounter[key];
        }

        /// <summary>
        /// Get count of given key or 0
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Value of key</returns>
        public int GetValueOrDefault(T key)
        {
            myCounter.TryGetValue(key, out int lret);
            return lret;
        }

        /// <summary>
        /// Get all counts for all keys as KeyValuePairs
        /// The setter is only needed for serialization purposes
        /// </summary>
        public KeyValuePair<T, int>[] Counts
        {
            get => myCounter.ToArray();
            set
            {
                if( value != null)
                {
                    myCounter = new Dictionary<T, int>();
                    foreach (var kvp in value)
                    {
                        myCounter.Add(kvp.Key, kvp.Value);
                    }
                }
            }
        }

    }
}
