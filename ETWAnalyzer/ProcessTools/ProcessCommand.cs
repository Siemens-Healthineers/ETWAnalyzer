//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Helper
{
    /// <summary>
    /// Wrap an external process which is started hidden while its output is captured.
    /// </summary>
    class ProcessCommand
    {
        readonly string myExecutable;
        readonly string myArgs;

        Process myProcess;

        /// <summary>
        /// Create a process class but to not start it yet. It is started with the Execute method.
        /// </summary>
        /// <param name="exe">Executable to start</param>
        /// <param name="args">Command line arguments</param>
        public ProcessCommand(string exe, string args)
        {
            myExecutable = exe;
            myArgs = args;
        }

        /// <summary>
        /// Kills the process which is currently running
        /// </summary>
        public void Kill()
        {
            if (myProcess != null && !myProcess.HasExited)
            {
                myProcess.Kill();
            }
        }

        /// <summary>
        /// Start process with given arguments
        /// </summary>
        /// <returns>ExecResult which contains the stdout,stderr, return code and the process instance</returns>
        /// <exception cref="InvalidOperationException">When process could not be started.</exception>
        public ExecResult Execute()
        {
            return Execute(ProcessPriorityClass.Normal);
        }

        /// <summary>
        /// Start process with given arguments
        /// </summary>
        /// <param name="priority">Process Priority</param>
        /// <returns>ExecResult which contains the stdout,stderr, return code and the process instance</returns>
        /// <exception cref="InvalidOperationException">When process could not be started.</exception>
        public ExecResult Execute(ProcessPriorityClass priority)
        { 
            var startInfo = new ProcessStartInfo(myExecutable, myArgs)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            try
            {
                CtrlCHandler.Instance.Register(CtlrCPressed);
                myProcess = Process.Start(startInfo);
                myProcess.PriorityClass = priority;
                Logger.Info($"Start Process: {startInfo.FileName} {startInfo.Arguments}");
                // Read stderror from another thread because otherwise we would run into deadlock situations
                // where the writing process blocks because we do not read the data. The internal buffer
                // is ca. 2048 bytes before the pipe blocks which is the reason why usually the issue goes unnoticed
                // until the application hangs.

                // Using ReadAsync does NOT fix the issue because the internal buffer is 4096 bytes where always 4096 bytes are tried to read
                // which is way over the internal blocking size ... 
                var stdErrTask = Task.Run(() =>
                   {
                       string line = null;
                       StringBuilder sb = new StringBuilder();
                       while ((line = myProcess.StandardError.ReadLine()) != null)
                       {
                           sb.AppendLine(line);
                       }
                       return sb.ToString();
                   });
                string stdout = myProcess.StandardOutput.ReadToEnd();
                myProcess.WaitForExit();

                return new ExecResult(myProcess, stdout, stdErrTask.Result);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"ProcessCommand failed with command line: {startInfo.FileName} {startInfo.Arguments}", ex);
            }
        }

        private void CtlrCPressed()
        {
            if (Program.DebugOutput)
            {
                Console.WriteLine($"Kill child process {myExecutable} {myArgs}");
            }
            Kill();
        }
    }
}
