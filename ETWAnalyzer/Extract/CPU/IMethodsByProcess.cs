//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System.Collections.Generic;

namespace ETWAnalyzer.Extract
{

    /// <summary>
    /// Contains the read only part of IMethodsByProcess to make querying easier
    /// </summary>
    public interface IMethodsByProcess
    {
        /// <summary>
        /// Get costs for each methods. Costs mean here CPU consumption, wait time, first/last call time based on CPU sampling data (this is NOT method call count!) and other stats
        /// </summary>
        IReadOnlyList<MethodCost> Costs { get; }

        /// <summary>
        /// Process
        /// </summary>
        ProcessKey Process { get; }
    }
}