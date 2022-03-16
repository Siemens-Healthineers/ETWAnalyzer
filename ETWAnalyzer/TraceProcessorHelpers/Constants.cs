//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.TraceProcessorHelpers
{
    /// <summary>
    /// Common .NET Runtime ETW Constants
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Registered ETW Provider name for .NET Runtime 
        /// </summary>
        public const string DotNetRuntimeProviderName = "Microsoft-Windows-DotNETRuntime";

        /// <summary>
        /// Guid of .NET Runtime ETW provider
        /// </summary>
        public static readonly Guid DotNetRuntimeGuid = new("e13c0d23-ccbc-4e12-931b-d9cc2eee27e4");

        /// <summary>
        /// Event id for Exception event 
        /// </summary>
        public const int ExceptionEventId = 80;

        /// <summary>
        /// Event Id for Clr Stack walk event
        /// </summary>
        public const int ClrStackWalkEventId = 82;
    }
}
