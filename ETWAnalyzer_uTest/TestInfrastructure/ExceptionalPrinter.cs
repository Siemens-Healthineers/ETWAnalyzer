using ETWAnalyzer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer_uTest.TestInfrastructure
{
    /// <summary>
    /// Cache ouptut messages and print them only in case if Dispose is called during an exception.
    /// The intention of this class is to omit testoutput in the good case, but add diagnostics messages
    /// when the test fails.
    /// </summary>
    class ExceptionalPrinter : IDisposable
    {
        public List<string> Messages { get; set; } = new List<string>();

        public void Add(string message)
        {
            Messages.Add(message);
        }

        /// <summary>
        /// Print messages only during exception unwind e.g. failed testcase.
        /// </summary>
        public void Dispose()
        {
            if (ExceptionHelper.InException)
            {
                foreach (var message in Messages)
                {
                    Console.WriteLine(message);
                }
            }

        }
    }
}
