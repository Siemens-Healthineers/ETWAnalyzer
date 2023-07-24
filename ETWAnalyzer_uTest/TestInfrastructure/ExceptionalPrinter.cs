using ETWAnalyzer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

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

        static char[] NewLineChars = Environment.NewLine.ToCharArray();

        ITestOutputHelper myWriter;

        /// <summary>
        /// Redirected Stdout
        /// </summary>
        StringWriter myStringWriter;

        public ExceptionalPrinter(ITestOutputHelper writer):this(writer, false)
        {
            myWriter = writer;
        }

        /// <summary>
        /// Redirect stdout 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="redirectStdout"></param>
        public ExceptionalPrinter(ITestOutputHelper writer, bool redirectStdout)
        {
            myWriter = writer;
            if (redirectStdout)
            {
                myStringWriter = new StringWriter();
                Console.SetOut(myStringWriter);
            }
        }




        /// <summary>
        /// Get all strings as single line strings. Multi line output is splitted at the newling character
        /// </summary>
        /// <returns>List of single lines</returns>
        public IReadOnlyList<string> GetSingleLines()
        {
            
            List<string> lret = new();
            foreach(var msg in Messages)
            {
                lret.AddRange(msg.Split(NewLineChars, StringSplitOptions.RemoveEmptyEntries));
            }

            return lret;
        }

        /// <summary>
        /// Flush pending data to list
        /// </summary>
        public void Flush()
        {
            if (myStringWriter != null)
            {
                Add(myStringWriter.ToString());
                myStringWriter = new();
                Console.SetOut(myStringWriter);
            }
        }

        public void Add(string message)
        {
            Messages.Add(message);
        }

        /// <summary>
        /// Print messages only during exception unwind e.g. failed testcase.
        /// </summary>
        public void Dispose()
        {
            Flush();

            if (ExceptionHelper.InException)
            {
                foreach (var message in Messages)
                {
                    myWriter.WriteLine(message);
                }
            }

        }
    }
}
