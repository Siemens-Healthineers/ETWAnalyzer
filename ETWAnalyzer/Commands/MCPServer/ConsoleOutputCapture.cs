//// SPDX-FileCopyrightText:  © 2026 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.IO;

namespace ETWAnalyzer.Commands.MCPServer
{
    /// <summary>
    /// Captures console output during command execution by temporarily redirecting Console.Out.
    /// </summary>
    internal sealed class ConsoleOutputCapture : IDisposable
    {
        private readonly TextWriter myOriginalOut;
        private readonly StringWriter myCapture;

        public ConsoleOutputCapture()
        {
            myOriginalOut = Console.Out;
            myCapture = new StringWriter();
            Console.SetOut(myCapture);
        }

        /// <summary>
        /// Get the captured output and restore the original console.
        /// </summary>
        public string GetOutput()
        {
            Console.SetOut(myOriginalOut);
            return myCapture.ToString();
        }

        public void Dispose()
        {
            Console.SetOut(myOriginalOut);
            myCapture.Dispose();
        }
    }
}
