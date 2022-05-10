//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers
{

    /// <summary>
    /// Clasifies a potential issue into several sub sub groups
    /// </summary>
    public enum Classification
    {
        /// <summary>
        /// Data file of frontend or backend is missing. By default we expect two ETL files
        /// </summary>
        MissingETWData,

        /// <summary>
        /// Machine setup or other things like Windows Update or NGen is running which can influence the test
        /// </summary>
        EnvironmentProblem,

        /// <summary>
        /// Functional issues in our software like exceptions in the UI or not working UI at all
        /// </summary>
        Functional,

        /// <summary>
        /// Performance related issue was identified
        /// </summary>
        Performance
    }
}
