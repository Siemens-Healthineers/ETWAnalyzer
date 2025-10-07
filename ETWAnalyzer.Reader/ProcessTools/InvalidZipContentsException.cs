//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.ProcessTools
{
    /// <summary>
    /// Thrown when the Zip file did not contain right informaton
    /// </summary>
    [Serializable]
#pragma warning disable CA1032 // Implement standard exception constructors
    public class InvalidZipContentsException : Exception
#pragma warning restore CA1032 // Implement standard exception constructors
    {
        /// <summary>
        /// 
        /// </summary>
        public string ZipFile
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="zipFile"></param>
        public InvalidZipContentsException(string message, string zipFile) : base(message)
        {
            ZipFile = zipFile ?? throw new ArgumentNullException(nameof(zipFile));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public InvalidZipContentsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serializationInfo"></param>
        /// <param name="streamingContext"></param>
        protected InvalidZipContentsException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
        }
    }
}
