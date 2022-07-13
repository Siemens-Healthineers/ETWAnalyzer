using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.PMC
{
    /// <summary>
    /// Method Call data recorded by LBR data
    /// </summary>
    public class MethodCall : IMethodCall
    {
        /// <summary>
        /// Calling method
        /// </summary>
        public string Caller { get; private set; }

        /// <summary>
        /// Method Name
        /// </summary>
        public string MethodName { get; private set; }

        /// <summary>
        /// Sampled Call Count (actual call count is much higher)
        /// </summary>
        public long Count { get; private set; }

        /// <summary>
        /// Process in which these calls did occur
        /// </summary>
        public ETWProcess Process { get; private set; }

        /// <summary>
        /// Construct a Method call object
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="methodName"></param>
        /// <param name="callCount"></param>
        /// <param name="process"></param>
        public MethodCall(string caller, string methodName, long callCount, ETWProcess process)
        {
            Caller = caller;
            MethodName = methodName;
            Count = callCount;
            Process = process;
        }
    }
}
