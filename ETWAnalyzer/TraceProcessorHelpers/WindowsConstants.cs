using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.TraceProcessorHelpers
{
    internal class WindowsConstants
    {
        /// <summary>
        /// Pid of Idle process. In this process live the device drivers belonging to the System process. 
        /// </summary>
        public const int IdleProcessId = 0;

        public const int SystemProcessId = 4;
    }
}
