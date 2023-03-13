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
    /// Concrete command which is executed via command line input arguments
    /// </summary>
    interface ICommandExecutor
    {
        /// <summary>
        /// Execute command. If an error happens there we will not print the command line help again.
        /// </summary>
        void Run();

        /// <summary>
        /// Return code which should be returned by executable when command is finished.
        /// </summary>
        public int? ReturnCode { get; }


        /// <summary>
        /// Get Help for current Command
        /// </summary>
        public string Help
        {
            get;
        }
    }
}
