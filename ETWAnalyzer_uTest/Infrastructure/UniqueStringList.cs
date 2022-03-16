//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest.Infrastructure
{
    public class UniqueStringListTests

    {
        [Fact]
        public void Can_Add_Null()
        {
            UniqueStringList list = new UniqueStringList();
            Assert.Equal(-2, list.GetIndexForString(null));

            Assert.Null(list.GetStringByIndex(-2));
        }

        [Fact]
        public void Adding_Same_String_ReturnsSameIndex()
        {
            UniqueStringList list = new UniqueStringList();
            int idx1 = list.GetIndexForString("A");
            int idx2 = list.GetIndexForString("A");

            Assert.Equal(idx1, idx2);
            Assert.Single(list.Strings);
        }
    }
}
