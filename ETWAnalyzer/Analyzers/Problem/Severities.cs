//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers
{
    /// <summary>
    /// 
    /// </summary>
    public enum Severities
    {
        /// <summary>
        /// Info only
        /// </summary>
        Info,

        /// <summary>
        /// Warning
        /// </summary>
        Warning,

        /// <summary>
        /// Fatal. Needs attention
        /// </summary>
        Fatal
    }
}
