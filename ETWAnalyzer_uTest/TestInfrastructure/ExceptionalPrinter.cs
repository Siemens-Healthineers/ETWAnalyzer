using ETWAnalyzer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer_uTest.TestInfrastructure
{
    class ExceptionalPrinter : IDisposable
    {
        public List<string> Messages { get; set; } = new List<string>();
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
