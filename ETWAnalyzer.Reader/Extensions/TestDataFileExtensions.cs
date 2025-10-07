//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using System.Linq;

namespace ETWAnalyzer.Reader.Extensions
{
    static class TestDataFileExtensions
    {
        /// <summary>
        /// Find a process by its process key from a TestDataFile. You should cache the results since it involves an array lookup!
        /// </summary>
        /// <param name="file"></param>
        /// <param name="key">ProcessKey</param>
        /// <returns>Found process or null if it was not found.</returns>
        public static ETWProcess FindProcessByKey(this TestDataFile file, ProcessKey key)
        {
            return key.FindProcessByKey(file.Extract);
        }


        /// <summary>
        /// Find process by key in extracted data. You should cache the results since it involves an array lookup!
        /// </summary>
        /// <param name="extract"></param>
        /// <param name="key"></param>
        /// <returns>Process or null if process was not found.</returns>
        public static ETWProcess FindProcessByKey(this ProcessKey key, IETWExtract extract)
        {
            if (key == null)
            {
                return null;
            }

            return extract.Processes.Where(x => x.ProcessName != null && (x.ProcessID == key.Pid && key.Name == x.ProcessName) && (x.StartTime == key.StartTime)).FirstOrDefault();
        }
    }
}
