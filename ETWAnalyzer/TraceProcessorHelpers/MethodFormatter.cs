//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Infrastructure;
using System.Collections.Generic;

namespace ETWAnalyzer.TraceProcessorHelpers
{
    internal class MethodFormatter
    {
        public bool NoDll { get; }
        public bool NoArgs { get; }
        public int MethodCutStart { get; }
        public int MethodCutLength { get; }


        /// <summary>
        /// Cache for splitted method name with no dll name
        /// </summary>
        readonly Dictionary<string, string> myMethodNamesNoDllCache = new();

        /// <summary>
        /// Cache for splitted method name with no arguments
        /// </summary>
        readonly Dictionary<string, string> myMethodNamesNoArgCache = new();

        public MethodFormatter():this(noDll:true, noArgs:true, methodCutStart:0, int.MaxValue)
        {
        }

        public MethodFormatter(bool noDll, bool noArgs, int methodCutStart, int methodCutLength)
        {
            NoDll = noDll;
            NoArgs = noArgs;
            MethodCutStart = methodCutStart;
            MethodCutLength = methodCutLength;
        }

        /// <summary>
        /// Remove from method name the dll name and or method arguments to produce easy to read output
        /// </summary>
        /// <param name="method">Input method</param>
        /// <param name="noCut">When true do not trim method name.</param>
        /// <returns></returns>
        public string Format(string method, bool noCut = false)
        {
            string lret = method;
            if (NoDll)
            {
                if (!myMethodNamesNoDllCache.TryGetValue(method, out string noDll))
                {
                    lret = method.Substring(method.IndexOf('!') + 1);
                    myMethodNamesNoDllCache[method] = lret;
                }
                else
                {
                    lret = noDll;
                }
            }

            if (NoArgs)
            {
                if (!myMethodNamesNoArgCache.TryGetValue(method, out string noArgs))
                {
                    int idx = lret.IndexOf('(');
                    if (idx != -1)
                    {
                        lret = lret.Substring(0, idx);
                    }
                    myMethodNamesNoArgCache[method] = lret;
                }
                else
                {
                    lret = noArgs;
                }
            }


            return noCut ? lret : lret.CutMinMax(this.MethodCutStart, this.MethodCutLength);
        }
    }
}
