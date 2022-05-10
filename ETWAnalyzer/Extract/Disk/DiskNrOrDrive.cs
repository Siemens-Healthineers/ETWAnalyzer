//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.Disk
{
    /// <summary>
    /// Each file has normally a file path containing the drive letter except for a few 
    /// special files like MFT and such things
    /// In general we try to assign disk IO to specific drives if possible
    /// </summary>
    public enum DiskNrOrDrive
    {
        /// <summary>
        /// In case no disk drive can be determined we use the disk id as fallback
        /// </summary>
        Id0 = 0,
        /// <summary>
        /// 
        /// </summary>
        Id1 = 1,
        /// <summary>
        /// 
        /// </summary>
        Id2 = 2,
        /// <summary>
        /// 
        /// </summary>
        Id3 = 3,
        /// <summary>
        /// 
        /// </summary>
        Id4 = 4,
        /// <summary>
        /// 
        /// </summary>
        Id5 = 5,
        /// <summary>
        /// 
        /// </summary>
        Id6 = 6,
        /// <summary>
        /// 
        /// </summary>
        Id7 = 7,
        /// <summary>
        /// 
        /// </summary>
        Id8 = 8,
        /// <summary>
        /// 
        /// </summary>
        Id9 = 9,
        /// <summary>
        /// 
        /// </summary>
        Id10 = 10,

        /// <summary>
        /// Disk drive is extracted from full path name
        /// </summary>
        A = 50,
        /// <summary>
        /// 
        /// </summary>
        B,
        /// <summary>
        /// 
        /// </summary>
        C,
        /// <summary>
        /// 
        /// </summary>
        D,
        /// <summary>
        /// 
        /// </summary>
        E,
        /// <summary>
        /// 
        /// </summary>
        F,
        /// <summary>
        /// 
        /// </summary>
        G,
        /// <summary>
        /// 
        /// </summary>
        H,
        /// <summary>
        /// 
        /// </summary>
        I,
        /// <summary>
        /// 
        /// </summary>
        J,
        /// <summary>
        /// 
        /// </summary>
        K,
        /// <summary>
        /// 
        /// </summary>
        L,
        /// <summary>
        /// 
        /// </summary>
        M,
        /// <summary>
        /// 
        /// </summary>
        N,
        /// <summary>
        /// 
        /// </summary>
        O,
        /// <summary>
        /// 
        /// </summary>
        P,
        /// <summary>
        /// 
        /// </summary>
        Q,
        /// <summary>
        /// 
        /// </summary>
        X,
        /// <summary>
        /// 
        /// </summary>
        Y,
        /// <summary>
        /// 
        /// </summary>
        Z,
        /// <summary>
        /// 
        /// </summary>
        Unknown,
    }
}
