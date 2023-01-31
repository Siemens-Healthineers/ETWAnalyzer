//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT
///
using ETWAnalyzer.Extract.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest.Extract
{
    public class PdbIdentifierTests
    {
        [Fact]
        public void Can_Deserialize_PdbName_WithSpaces()
        {
            const string pdbName = "Name With Spaces.pdb";
            Guid pdbGuid = new Guid("39e2c995-82fe-4437-9c23-50af818eae5e");

            PdbIdentifier pdb = new PdbIdentifier(pdbName, pdbGuid, 15);
            Assert.Equal(15, pdb.Age);
            Assert.Equal(pdbGuid, pdb.Id);
            Assert.Equal(pdbName, pdb.Name);
        }
    }
}
