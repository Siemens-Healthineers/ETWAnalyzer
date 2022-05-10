//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Infrastructure
{
    /// <summary>
    /// Define a skip and take range with sane default iif nothing is specified.
    /// </summary>
    internal class SkipTakeRange
    {
        /// <summary>
        /// When none is specified 0
        /// </summary>
        public int SkipN { get;  }

        /// <summary>
        /// When none is specified int.MaxValue is used
        /// </summary>
        public int TakeN { get;  }

        /// <summary>
        /// Create a default range which skips nothing and takes everything.
        /// </summary>
        public SkipTakeRange() : this(null, null)
        { }


        /// <summary>
        /// Create a range
        /// </summary>
        /// <param name="takeN"></param>
        /// <param name="skipN"></param>
        public SkipTakeRange(int? takeN, int? skipN)
        {
            SkipN = skipN ?? 0;
            TakeN = takeN ?? int.MaxValue;
        }
    }
}
