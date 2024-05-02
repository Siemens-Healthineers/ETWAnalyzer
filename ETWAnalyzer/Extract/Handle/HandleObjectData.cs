using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.Handle
{
    public class HandleObjectData
    {
        /// <summary>
        /// Contains Object Reference tracing data along with handle values if Handle Tracing was also enabled
        /// </summary>
        public List<ObjectRefTrace> ObjectReferences { get; set; } = new();

        /// <summary>
        /// Stacks are combined for Handle and Object Reference Tracing.
        /// </summary>
        public StackCollection Stacks { get; set; } = new();
    }
}
