//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.Exceptions;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ETWAnalyzer_uTest
{
    
    public class ExceptionMessageAndTypeTests
    {
        [Fact]
        public void Can_Use_In_HashSet()
        {
            HashSet<ExceptionMessageAndType> set = new HashSet<ExceptionMessageAndType>();

            for(int i=0;i<10;i++)
            {
                const int Same = 1;
                ExceptionMessageAndType cont = new ExceptionMessageAndType
                {
                    Message = Same.ToString()
                };

                bool bAdded = set.Add(cont);
                if( i > 0 )
                {
                    Assert.False(bAdded, "Object has same content. If it is added more than once then the GetHashCode or EqualityComparer does not work"); 
                }
            }
        }
    }
}
