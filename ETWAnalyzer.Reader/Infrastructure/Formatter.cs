using System;

namespace ETWAnalyzer.Infrastructure
{
    /// <summary>
    /// Simple wrapper around column formatters
    /// </summary>
    /// <typeparam name="T">Type to print</typeparam>
    class Formatter<T>
    {
        /// <summary>
        /// Header string
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Contains powercfg shortcut name and setting Guid
        /// </summary>
        public string Identifier { get; set; } 

        /// <summary>
        /// Description of property
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Help string
        /// </summary>
        public string Help { get; set; }

        /// <summary>
        /// Print method with alignment
        /// </summary>
        public Func<T, string> Print { get; set; }


        private string myPreviousValue = null;

        /// <summary>
        /// Get stringified value but do not repeat same values. 
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Value from Print delegate or ... if value is a duplicate of the previous value.</returns>
        public string PrintNoDup(T data)
        {
            string lret = Print(data);
            if( myPreviousValue != lret)
            {
                myPreviousValue = lret;
            }
            else
            {
                lret = "...";
            }

            return lret;
        }

        public string GetHelpIndented(int level)
        {
            return GetIndented(Help, level);
        }

        public string GetIndented(string str, int level)
        {
            if (str != null && !String.IsNullOrEmpty(str))
            {
                string indent = new string('\t', level);

                str = indent + str.Replace(Environment.NewLine, Environment.NewLine + new string('\t', level));
            }
            return str;
        }

    }
}
