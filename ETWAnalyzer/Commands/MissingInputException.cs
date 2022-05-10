//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Commands
{
    /// <summary>
    /// Thrown when we have no input file or Directory
    /// </summary>
    class MissingInputException : ApplicationException
    {
        public MissingInputException()
        {
        }

        public MissingInputException(string message) : base(message)
        {
        }

        public MissingInputException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MissingInputException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
