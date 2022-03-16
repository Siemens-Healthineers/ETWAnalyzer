//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;

namespace ETWAnalyzer.Infrastructure
{
    /// <summary>
    /// Contains a unique list of strings which can be added and looked up by string
    /// </summary>
    public class UniqueStringList
    {
        /// <summary>
        /// Contains stored strings. This list is not meant to be modified or accessed directly.
        /// Use GetStringByIndex to retrieve strings by index or you will run into issues with null strings!
        /// </summary>
        public List<string> Strings
        {
            get;
            set;
        } = new List<string>();

        /// <summary>
        /// For fast lookup we use a dictionary. This is ok for extraction where we consume GB of data.
        /// When the object is serialized this is empty and the overhead is basically the list.
        /// </summary>
        Dictionary<string, int> myIndicies;

        /// <summary>
        /// Default ctr
        /// </summary>
        public UniqueStringList()
        { }

        /// <summary>
        /// Uses a different comparer. By default we keep case sensitivity
        /// </summary>
        /// <param name="comparer"></param>
        public UniqueStringList(StringComparer comparer = null)
        {
            myIndicies = new Dictionary<string, int>(comparer ?? StringComparer.Ordinal);
        }

        /// <summary>
        /// Get String out of index
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public string GetStringByIndex(int idx)
        {
            if( idx == -2)
            {
                return null;
            }

            return Strings[idx];
        }

        /// <summary>
        /// Add string to list if not already done and return index to added or already existing string.
        /// Input string can be null.
        /// </summary>
        /// <param name="str">String to add to list if not yet part of the list</param>
        /// <returns>Index to string in list</returns>
        public int GetIndexForString(string str)
        {
            if( myIndicies == null )
            {
                myIndicies = new Dictionary<string, int>();
            }

            if( str == null )
            {
                return -2;
            }

            if( !myIndicies.TryGetValue(str, out int idx) )
            {
                Strings.Add(str);
                idx = Strings.Count - 1;
                myIndicies.Add(str, idx);
            }

            return idx;
        }
    }
}
