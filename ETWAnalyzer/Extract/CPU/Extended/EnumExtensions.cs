//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

namespace ETWAnalyzer.Extract.CPU.Extended
{
    /// <summary>
    /// Helper methods to convert a combined ProcessMethodIdx to/from ETWProcessIndex and MethodIndex.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Get MethodIndex from ProcessMethodIdx.
        /// </summary>
        /// <param name="processMethodIdx"></param>
        /// <returns></returns>
        public static MethodIndex MethodIndex(this ProcessMethodIdx processMethodIdx)
        {
            return (MethodIndex)((long)processMethodIdx & 0xFFFFF);
        }


        /// <summary>
        /// Get ETWProcessIndex from ProcessMethodIndex.
        /// </summary>
        /// <param name="processMethodIdx"></param>
        /// <returns></returns>
        public static ETWProcessIndex ProcessIndex(this ProcessMethodIdx processMethodIdx)
        {
            return (ETWProcessIndex) ((long)processMethodIdx >> 20);
        }

        /// <summary>
        /// Create a combined index from ETWProcessIndex and MethodIndex.
        /// </summary>
        /// <param name="process">Process Index can consume up to 44 bits.</param>
        /// <param name="method">We support up to 1 million methods (20 bits).</param>
        /// <returns></returns>
        public static ProcessMethodIdx Create(this ETWProcessIndex process, MethodIndex method)
        {
           return (ProcessMethodIdx)(((long)process << 20) | (long) method);
        }
    }
}
