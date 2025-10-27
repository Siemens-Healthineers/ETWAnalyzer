//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

namespace ETWAnalyzer.TraceProcessorHelpers
{

    /// <summary>
    /// Unit Conversion factors 
    /// </summary>
    internal class Units
    {
        /// <summary>
        /// No conversion is needed because input data and filter unit are the same
        /// </summary>
        public const decimal SameUnit = 1.0m;

        /// <summary>
        /// Byte to MiB conversion factor
        /// </summary>
        public const decimal MiBUnit = 1 / (1024m * 1024m);

        /// <summary>
        /// second to ms conversion factor
        /// </summary>
        public const decimal MSUnit = 1 / 1000m;

        /// <summary>
        /// second to us conversion factor
        /// </summary>
        public const decimal UsUnit = 1 / 1_000_000m;
    }
}
