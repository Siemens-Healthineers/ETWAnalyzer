//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Runtime.InteropServices;

namespace ETWAnalyzer
{
    /// <summary>
    /// Helper methods to find e.g. if you are in an exception unwind scenario.
    /// </summary>
    public static class ExceptionHelper
    {
        /// <summary>
        /// Check if we are in a exception unwind scenario or not.
        /// </summary>
        public static bool InException
        {
            get
            {
                return Marshal.GetExceptionPointers() == IntPtr.Zero &&
                       Marshal.GetExceptionCode() == 0 ? false : true;
            }
        }
    }
}
