//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Processes;
using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        /// <param name="methodName">Method Name returned by library</param>
        /// <param name="frame">stackframe to check if it is a managed or unmanaged method</param>
        /// <returns>Pretty printed method. Once method has been prettified cached results are returned.</returns>
        public string GetPrettyMethod(string methodName, StackFrame frame)
        {
            return GetPrettyMethod(methodName, frame.Image);
        }

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

            for (int i=0;i<stack.Frames.Count;i++)
            {
                StackFrame frame = stack.Frames[i];
                if( !frame.HasValue )
                {
                    lret.AppendLine(UnknownMethod);
                }

                if( frame.Symbol != null)
                {
                    lret.AppendLine(GetPrettyMethod(frame.Symbol.FunctionName, frame));
                }
            }

            return lret.ToString();
        }
    }
}
