//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer_uTest.TestInfrastructure
{
    public class TestContext
    {
        public static bool IsInAzurePipeline()
        {
            return Environment.GetEnvironmentVariable("TF_BUILD") != null;
        }
    }
}
