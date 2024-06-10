using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Infrastructure
{
    internal class ReusableMemoryStream : MemoryStream
    {
        protected override void Dispose(bool disposing)
        {
            
        }
    }
}
