//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Microsoft.Diagnostics.Tracing.StackSources;
using Microsoft.Performance.SDK.Runtime;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Processes;
using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ETWAnalyzer.TraceProcessorHelpers
{
    /// <summary>
    /// Supported stack trace formatting rules
    /// </summary>
    enum StackFormat
    {
        MethodsOnly,
        DllAndMethod
    }

    /// <summary>
    /// Stacktrace printer which pretty prints managed methods without the extra 0x0 and [COLD] post/prefixes
    /// </summary>
    class StackPrinter
    {
        readonly StackFormat myStackFormat = StackFormat.MethodsOnly;

        /// <summary>
        /// Default string when method name could not be resolved, either we did not have symbols for it or 
        /// the JIT events were not present due to ETW buffer overflow
        /// </summary>
        public const string UnknownMethod = "<UnknownMethod>";

        /// <summary>
        /// Cache formatted string manipulations
        /// </summary>
        readonly ConcurrentDictionary<string, string> UglyVsPrettyMethodNames = new();

        public StackPrinter()
        {
        }


        public StackPrinter(StackFormat stackFormat)
        {
            myStackFormat = stackFormat;
        }

        /// <summary>
        /// Remove [Cold] and 0x0 and other things from method names which can confuse readers
        /// </summary>
        /// <param name="methodName">Method Name returned by library</param>
        /// <param name="image">Image name from where the dll name can be retrieved if we have no loaded symbol</param>
        /// <returns>Pretty printed method. Once method has been prettified cached results are returned.</returns>
        public string GetPrettyMethod(string methodName, IImage image)
        {
            if( String.IsNullOrEmpty(methodName) )
            {
                // If we have no symbols for an image we use a method name the dll name. This way we get for xxxx.dll as method name the total CPU costs of that unknown module
                // this comes in handy if e.g. AV scanners which never provide symbols we can attribute the CPU costs to the right module
                methodName = image?.FileName ?? UnknownMethod; 
            }

            string prettyName = null;

            if (!UglyVsPrettyMethodNames.TryGetValue(methodName, out prettyName))
            {
                prettyName = methodName
                                .Replace(" 0x0", "")               // Managed methods contain some IL offset after the function name. That is superfluous
                                .Replace("[COLD] ", "");           // NGenned managed methods contain [COLD] if the method is located in some cold code path e.g. during exception throwing
                if (image?.Pdb == null)
                {
                    prettyName = prettyName.Replace("::", ".");   // Managed JITed methods have :: while NGenned methods have . between class and method name. Be consistent and use . for everything, even C++
                }
                UglyVsPrettyMethodNames[methodName] = prettyName;
            }

            string imageName = image?.FileName;

            return myStackFormat switch
            {
                StackFormat.DllAndMethod => imageName != null ? $"{imageName}!{prettyName}" : $"<UnknownModule>!{prettyName}",
                StackFormat.MethodsOnly => prettyName,
                _ => throw new InvalidOperationException($"Stackformat {myStackFormat} is not supported. Please implement support for it."),
            };
        }


        /// <summary>
        /// Remove [Cold] and 0x0 and other things from method names which can confuse readers
        /// </summary>
        /// <param name="frame">Current StackFrame from which method and image name is deduced.</param>
        /// <returns>Pretty printed method. Once method has been prettified cached results are returned.</returns>
        public (string,bool) GetPrettyMethod(Microsoft.Windows.EventTracing.Symbols.StackFrame frame)
        {
            string functionName = GetFunctionName(frame);
            return (GetPrettyMethod(functionName, frame.Image), functionName != "");
        }

        /// <summary>
        /// Remove [Cold] and 0x0 and other things from method names which can confuse readers
        /// </summary>
        /// <param name="symbol">symbol to check if it is a managed or unmanaged method from which the function name is resolved.</param>
        /// <returns>Pretty printed method. Once method has been prettified cached results are returned.</returns>
        public string GetPrettyMethod(IStackSymbol symbol)
        {
            return GetPrettyMethod(GetFunctionName(symbol, symbol?.Image), symbol?.Image);
        }

        static ConcurrentSet<string> myLoggedProblemSymbols = new();


        private string GetFunctionName(IStackSymbol symbol, IImage image)
        {
            string functionName = symbol?.FunctionName ?? ""; 
            return functionName;
        }

        /// <summary>
        /// Get function name from stack frame with exception handling and logging where pdb resolution errors are only logged once per image name.
        /// </summary>
        /// <param name="frame"></param>
        /// <returns>Resolved method name or empty string.</returns>
        string GetFunctionName(Microsoft.Windows.EventTracing.Symbols.StackFrame frame)
        {
            string functionName = "";   
            try
            {
                if( myLoggedProblemSymbols.Contains(frame.Image?.FileName ?? "UnknownImage"))  // this can become a major perf hit if debugging ETWAnalyzer 
                {
                    // previously failed symbol load
                    return functionName;
                }
                functionName = GetFunctionName(frame.Symbol, frame.Image); // can fail for some pdbs https://github.com/microsoft/eventtracing-processing-samples/issues/12
            }
            catch (NotImplementedException ex)
            {
                if (myLoggedProblemSymbols.Add(frame.Image?.FileName ?? "UnknownImage"))
                {
                    Logger.Warn($"Symbol load did throw an exception for image {frame.Image?.FileName}. Exception: {ex}");
                }
            }

            return functionName;
        }

        Dictionary<KeyValuePair<uint, List<Address>>, string> myFrameAddressMap = new(new StackComparer());

        class StackComparer : IEqualityComparer<KeyValuePair<uint, List<Address>>>
        {
            public StackComparer() { }
            public bool Equals(KeyValuePair<uint, List<Address>> x, KeyValuePair<uint, List<Address>> y)
            {
                if (x.Key != y.Key)
                {
                    return false;
                }

                if( x.Value.Count != y.Value.Count)
                {
                    return false;
                }

                for(int i=0;i<x.Value.Count;i++)
                {
                    if (x.Value[i]!= y.Value[i]) 
                    { 
                        return false; 
                    }
                }

                return true;
            }

            public int GetHashCode(KeyValuePair<uint, List<Address>> obj)
            {
                int hash = 17*31 + (int) obj.Key;
                for(int i=0;i<obj.Value.Count;i++)
                {
                    hash = hash * 31 + (obj.Value[i]).GetHashCode();
                }
                return hash;
            }
        }

        List<Address> myStackList = new List<Address>();

        /// <summary>
        /// Get a pretty printed stack string out of of an <see cref="IStackSnapshot"/> instance
        /// </summary>
        /// <param name="stack">Stack snapshot</param>
        /// <returns>Pretty printed stack frame string</returns>
        /// <exception cref="ArgumentNullException">stack is null</exception>
        public string Print(IStackSnapshot stack)
        {
            if( stack == null)
            {
                throw new ArgumentNullException(nameof(stack));
            }

            StringBuilder lret = new();
            lock(myStackList) 
            {
                myStackList.Clear();
                for (int i = 0; i < stack.Frames.Count; i++)
                {
                    myStackList.Add(stack.Frames[i].Address);
                }
                var key = new KeyValuePair<uint, List<Address>>(stack.ProcessId, myStackList);
                if (myFrameAddressMap.TryGetValue(key, out string stackTrace) )
                {
                    return stackTrace;
                }

                for (int i=0;i<stack.Frames.Count;i++)
                {
                    var frame = stack.Frames[i];
                    if( !frame.HasValue )
                    {
                        lret.AppendLine(UnknownMethod);
                    }

                    var frameKey = new KeyValuePair<uint, Address>(stack.ProcessId, frame.RelativeVirtualAddress);

                    (string methodName, bool bSuccess) = GetPrettyMethod(frame);
                    if( bSuccess)
                    {
                        lret.AppendLine(methodName);
                    }
                    else
                    {
                        string method = frame.Image?.FileName ?? UnknownMethod;
                        method = AddRva(method, frame.RelativeVirtualAddress);
                        lret.AppendLine(method);
                    }

                }

                string retStr = lret.ToString();
                myFrameAddressMap[new KeyValuePair<uint, List<Address>>(key.Key, new List<Address>(myStackList))] = retStr;

                return lret.ToString();
            }
        }

        /// <summary>
        /// Add to method name if it was not resolved the RVA address. This is needed to later resolve the method
        /// name when matching symbols could be loaded.
        /// </summary>
        /// <param name="method">Method of the form xxxx.dll!method where method is xxx.dll if the symbol lookup did fail.</param>
        /// <param name="rva">Image Relative Virtual Address</param>
        /// <returns>For unresolved methods the image + Image Relative Virtual Address.</returns>
        internal static string AddRva(string method, Address rva)
        {
            string lret = method;

            // do not try to resolve invalid RVAs (like 0)
            if (rva.Value > 0 && (method.EndsWith(".exe", StringComparison.Ordinal) || method.EndsWith(".dll", StringComparison.Ordinal) || method.EndsWith(".sys", StringComparison.Ordinal)))
            {
                // Method could not be resolved. Use RVA
                lret = method + "+0x" + rva.Value.ToString("X", CultureInfo.InvariantCulture);
            }

            return lret;
        }
    }
}
