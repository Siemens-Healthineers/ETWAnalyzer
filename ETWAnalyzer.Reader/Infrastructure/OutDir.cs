//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;

namespace ETWAnalyzer.Infrastructure
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
            OutputDirectory = TestRun.GetDirectorySave(outputDirectory); // Do not store file query like c:\temp\*.etl 
            IsDefault = true;
        }
    }
}
