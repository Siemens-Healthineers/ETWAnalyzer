//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Commands
{
    /// <summary>
    /// Generic interface for command line parsing and command execution
    /// </summary>
    interface ICommand : ICommandExecutor, ICommandLineParser
    { 
    
    }
}
