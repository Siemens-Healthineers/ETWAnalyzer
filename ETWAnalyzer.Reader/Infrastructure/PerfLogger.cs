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
        long myMemoryAtStartMB;

        public PerfLogger(string operationName)
        {
            using var proc = Process.GetCurrentProcess();

            myStopWatch = Stopwatch.StartNew();
            myOperationName = operationName;

            myMemoryAtStartMB = proc.PrivateMemorySize64 / (1024 * 1024L);
            Logger.Info($"Start {operationName} Memory: {myMemoryAtStartMB} MB");
        }

        public void Dispose()
        {
            using var proc = Process.GetCurrentProcess();
            long memoryNowMB = proc.PrivateMemorySize64 / (1024 * 1024L);
            Logger.Info($"End {myOperationName} in {myStopWatch.Elapsed.TotalSeconds:F1} s Memory: {memoryNowMB} MB, Diff: {memoryNowMB-myMemoryAtStartMB} MB");
        }
    }
}
