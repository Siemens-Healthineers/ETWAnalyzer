using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Infrastructure
{
    /// <summary>
    /// Do not close memory stream on dispose, so we can reuse it
    /// </summary>
    internal class ReusableMemoryStream : MemoryStream
    {
        protected override void Dispose(bool disposing)
        {
            
        }
    }
}
