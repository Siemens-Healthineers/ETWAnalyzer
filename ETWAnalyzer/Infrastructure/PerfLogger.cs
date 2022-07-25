using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Infrastructure
{
    internal class PerfLogger : IDisposable
    {
        Stopwatch myStopWatch;
        string myOperationName;

        public PerfLogger(string operationName)
        {
            myStopWatch = Stopwatch.StartNew();
            myOperationName = operationName;
            Logger.Info($"Start {operationName}");
        }

        public void Dispose()
        {
            Logger.Info($"End {myOperationName} in {myStopWatch.Elapsed.TotalSeconds:F1} s");
        }
    }
}
