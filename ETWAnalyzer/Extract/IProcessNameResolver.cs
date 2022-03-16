//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// 
    /// </summary>
    public interface IProcessNameResolver
    {
        /// <summary>
        /// Rename a process based on its command line
        /// </summary>
        /// <param name="exeName">Name of exe</param>
        /// <param name="cmdLine">Command line arguments to exe</param>
        /// <returns>Same as exeName or renamed process if one of the RenameRules did match</returns>
        string GetProcessName(string exeName, string cmdLine);
    }
}
