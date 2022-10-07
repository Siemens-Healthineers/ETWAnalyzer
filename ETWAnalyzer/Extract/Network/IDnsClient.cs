//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using System.Collections.Generic;

namespace ETWAnalyzer.Extract.Network
{
    /// <summary>
    /// Contains DNS Events which originate from the 
    /// </summary>
    public interface IDnsClient
    {
        /// <summary>
        /// DNS Events
        /// </summary>
        IReadOnlyList<IDnsEvent> Events { get; }
    }
}