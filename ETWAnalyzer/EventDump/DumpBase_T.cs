using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.EventDump
{
    /// <summary>
    /// Typed base class which caches its output so we have a chance to later add specific unit tests for important aspects of the dump commands
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract class DumpBase<T> : DumpBase
    {
        public string ETLFile { get; set; }
        public bool UsePrettyProcessName { get; set; }

        protected string Col_CSVOptions = "CSVOptions";

        public override void Execute()
        {
            ExecuteInternal();
        }

        /// <summary>
        /// Execute command and return cached output for unit testing
        /// </summary>
        /// <returns></returns>
        public abstract List<T> ExecuteInternal();
    }
}
