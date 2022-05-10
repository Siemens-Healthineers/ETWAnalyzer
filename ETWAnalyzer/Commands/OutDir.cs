﻿//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Commands
{
    class OutDir
    {
        public string OutputDirectory
        {
            get; set;
        }

        /// <summary>
        /// Temporary extraction path 
        /// </summary>
        public string TempDirectory
        {
            get;set;
        }

        public bool IsDefault
        {
            get;set;
        }

        public void SetDefault(string outputDirectory)
        {
            OutputDirectory = outputDirectory;
            IsDefault = true;
        }
    }
}
