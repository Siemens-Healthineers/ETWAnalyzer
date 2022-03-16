//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Helper
{
    /// <summary>
    /// 
    /// </summary>
    public class ExecResult
    {
        /// <summary>
        /// Exited process from which the output was read
        /// </summary>
        public Process ExitedProcess { get; }

        /// <summary>
        /// Exit code of command
        /// </summary>
        public int ReturnCode { get => ExitedProcess.ExitCode; }

        /// <summary>
        /// Standard output written by called process
        /// </summary>
        public string StandardOutput { get; }

        /// <summary>
        /// Standard error messages written by called process
        /// </summary>
        public string StandardErrorOutput { get; }

        /// <summary>
        /// Combined standard output followed by standard error output
        /// </summary>
        public string AllOutput
        {
            get => StandardOutput + (String.IsNullOrEmpty(StandardOutput) ? "" : Environment.NewLine) + StandardErrorOutput;
        }

        /// <summary>
        /// Default is true except if someone calls SetFailed() which sets it to false.
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// Create executable result
        /// </summary>
        /// <param name="exitedProcess"></param>
        /// <param name="standardOutput"></param>
        /// <param name="standardErrorOutput"></param>
        public ExecResult(Process exitedProcess, string standardOutput, string standardErrorOutput)
        {
            ExitedProcess = exitedProcess;
            StandardOutput = standardOutput;
            StandardErrorOutput = standardErrorOutput;
            Succeeded = true;
        }

        internal void SetFailed()
        {
            Succeeded = false;
        }
    }
}
