//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Commands
{
    /// <summary>
    /// Parses a previously supplied command line.
    /// </summary>
    interface ICommandLineParser
    {
        /// <summary>
        /// Parse supplied command line and throw an exception when not all needed or unkonw arguments were passed. 
        /// If during parse an error happens the help will be printed before the actual error.
        /// </summary>
        void Parse();
    }
}
