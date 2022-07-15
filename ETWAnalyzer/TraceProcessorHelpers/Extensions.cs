//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Processes;
using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ETWAnalyzer.Extract.ETWProcess;

namespace ETWAnalyzer.TraceProcessorHelpers
{
    static class Extensions
    {
        static public DateTimeOffset ConvertToTime(this TraceTimestamp ?time)
        {
            return time == null ? DateTimeOffset.MinValue : time.Value.DateTimeOffset;
        }

        public static bool IsMatch(this IProcess process, ProcessStates? state)
        {
            if( state == null )
            {
                return true;
            }

            if(process.CreateTime == null && process.ExitTime == null && state == ProcessStates.None)
            {
                return true;
            }

            if( process.CreateTime.HasValue && state == ProcessStates.Started)
            {
                return true;
            }
            if( process.ExitTime.HasValue && state == ProcessStates.Stopped)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if address is in address range which is normally a module address range. For some reason the Limit address can be larger than the 
        /// base address. So we need to check for both possibilities. 
        /// </summary>
        /// <param name="range">Address range</param>
        /// <param name="address">address to check.</param>
        /// <returns></returns>
        public static bool IsInRange(this AddressRange range, Address address) =>
            ( (range.BaseAddress < range.LimitAddress) && (address > range.BaseAddress && address < range.LimitAddress)) ||
            ( (range.BaseAddress > range.LimitAddress) && (address < range.BaseAddress && address > range.LimitAddress));

        /// <summary>
        /// Resolve symbol of an address of a process.
        /// </summary>
        /// <param name="process">Process to resolve.</param>
        /// <param name="address">Address inside this process.</param>
        /// <returns>IStackSymnbol if lookup was successful, or null if symbol could not be resolved.</returns>
        public static IStackSymbol GetSymbolForAddress(this IProcess process, Address address)
        {
            if (process == null || process.Images == null)
            {
                return null;
            }

            IStackSymbol lret = null;
            foreach (var image in process.Images)
            {
                AddressRange range = image.AddressRange;
                if (range.IsInRange(address))
                {
                    lret = image.GetSymbol(address);
                    break;
                }
            }

            return lret;
        }
    }
}
