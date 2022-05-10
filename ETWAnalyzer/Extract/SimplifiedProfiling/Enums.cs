//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

namespace TAU.Toolkit.Diagnostics.Profiling.Simplified
{

    /// <summary>
    /// enum to determine where the results were generated
    /// </summary>
    public enum GeneratedAt
    {
        /// <summary/>
        Invalid = 0,
        /// <summary>
        /// generated on the client
        /// </summary>
        CLT =1,
        /// <summary>
        /// generated on the server
        /// </summary>
        SRV = 2,
        /// <summary>
        /// result generated on a single machine containing Server and Client profiling results
        /// </summary>
        SINGLE = 3
    }
    /// <summary>
    ///
    /// </summary>
    public enum TestStatus
    {
        
        /// <summary/>
        Unknown = 0,
        /// <summary/>
        Passed = 1,
        /// <summary/>
        Failed = 2

    }

    internal enum RunningState
    {
        NoInitialized = 0,
        Active = 1,
        Stopped = 2,
        Finalized =3
    }
}
