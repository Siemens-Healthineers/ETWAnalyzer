//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest.Extract
{
    public class SymbolPathsTests
    {
        [Fact]
        public void CanGetRemoteServerFrom_EnvVar()
        {
            string old = Environment.GetEnvironmentVariable(SymbolPaths.NT_SYMBOLPATH);
            try
            {
                string remotePath = @"SRV*C:\Cache*SRV*c:\DebugSymbols*https://build-ACME.Cartoon.com/symbols/";

                Environment.SetEnvironmentVariable(SymbolPaths.NT_SYMBOLPATH, remotePath);
                string etlFile = @"C:\ETL\MyTest.etl";
                string symFolder = @"C:\localsyms";
                SymbolPaths sym = new SymbolPaths()
                {
                    RemoteSymbolServer = SymbolPaths.GetRemoteSymbolServerFromNTSymbolPath(),
                    SymbolFolder = symFolder
                };

                string combined = sym.GetCombinedSymbolPath(etlFile);
                Assert.Equal($"SRV*{symFolder};SRV*{etlFile}.NGENPDB;{remotePath}", combined);

            }
            finally
            {
                Environment.SetEnvironmentVariable(SymbolPaths.NT_SYMBOLPATH, old);
            }
        }

        [Fact]
        public void Can_Get_LocalPath_Without_Remote()
        {
            SymbolPaths sym = new SymbolPaths()
            {
                RemoteSymbolServer = null,
            };
            string etlFile = @"C:\ETL\MyTest.etl";
            string combined = sym.GetCombinedSymbolPath(etlFile);

            Assert.Equal($"SRV*;SRV*{etlFile}.NGENPDB;", combined);
        }

        [Fact]
        public void Can_Get_LocalPath_WithLocalSymFolder_Without_Remote()
        {
            string localFolder = @"C:\Symbols";

            SymbolPaths sym = new SymbolPaths()
            {
                RemoteSymbolServer = null,
                SymbolFolder = localFolder,
            };
            string etlFile = @"C:\ETL\MyTest.etl";
            string combined = sym.GetCombinedSymbolPath(etlFile);

            Assert.Equal($"SRV*{localFolder};SRV*{etlFile}.NGENPDB;", combined);
        }
    }
}
