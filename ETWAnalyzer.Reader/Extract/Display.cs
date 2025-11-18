//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Connected displays 
    /// </summary>
    public class Display
    {
        /// <summary>
        /// 
        /// </summary>
        public int HorizontalResolution { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int VerticalResolution { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public long RefreshRateHz { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsPrimaryDevice { get; set; }

        /// <summary>
        /// The returned value is in units of bits per pixel (bpp).
        /// </summary>
        public int ColorDepth { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// In Million Bytes (not Mega 1024)) 
        /// </summary>
        public long GraphicsCardMemorySizeMiB { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string GraphicsCardChipName { get; set; }
    }
}
