//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.ProcessTools
{
    class ValueConverter
    {
        public static bool GetBool(string str)
        {
            if( int.TryParse(str, out int value) )
            {
                if( value == 0 || value == 1)
                {
                    return value == 1;
                }
                throw new InvalidDataException($"Expected a bool value but got: {str}");
            }

            if( bool.TryParse(str, out bool bValue))
            {
                return bValue;
            }

            throw new InvalidDataException($"Expected a bool value but got: {str}");
        }
    }
}
