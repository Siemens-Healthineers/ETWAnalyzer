using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Infrastructure
{
    class ColumnFormatter<T>
    {
        public Func<T, string> Formatter;
        string myHeader;
        public string Header
        {
            get => myHeader;
            set => myHeader = value;
        }

        public ConsoleColor? Color;

        public ColumnFormatter()
        {
            Header = "";
            Formatter = x => "";
            Color = null;
        }
    }
}
