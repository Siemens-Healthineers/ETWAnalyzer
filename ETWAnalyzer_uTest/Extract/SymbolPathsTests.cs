//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Helper;
using ETWAnalyzer_uTest.TestInfrastructure;
using System;
using System.Collections.Generic;
using System.IO;
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
            // this test need Admin access or the create directory link privilege 
            if (!TestContext.IsAdministrator())
            {
                return;
            }

            string old = Environment.GetEnvironmentVariable(SymbolPaths.NT_SYMBOLPATH);
            try
            {
                using var tmp = TempDir.Create();
                string localFolder = tmp.Name;

                string remotePath = @"SRV*C:\Cache*SRV*c:\DebugSymbols*https://build-ACME.Cartoon.com/symbols/";

                Environment.SetEnvironmentVariable(SymbolPaths.NT_SYMBOLPATH, remotePath);
                string etlFile = Path.Combine(localFolder,"MyTest.etl");

                SymbolPaths sym = new SymbolPaths()
                {
                    RemoteSymbolServer = SymbolPaths.GetRemoteSymbolServerFromNTSymbolPath(),
                    SymbolFolder = localFolder
                };

                string embeddedFolder = sym.GetLongSymbolFolderForEtl(etlFile, SymbolPaths.EmbeddedPdbExtension);
                Directory.CreateDirectory(embeddedFolder);

                string combined = sym.GetCombinedSymbolPath(etlFile);
                Assert.Equal($"SRV*{localFolder};SRV*{sym.GetShortSymbolFolderForEtl(etlFile, SymbolPaths.EmbeddedPdbExtension)};{remotePath}", combined);
            }
            finally
            {
                Environment.SetEnvironmentVariable(SymbolPaths.NT_SYMBOLPATH, old);
            }
        }

        [Fact]
        public void Can_Get_LocalPath_Without_Remote()
        {
            using var tmp = TempDir.Create();
            string localFolder = tmp.Name;

            SymbolPaths sym = new SymbolPaths()
            {
                RemoteSymbolServer = null,
            };
            string etlFile = @"C:\ETL\MyTest.etl";
            string ngenFolder = sym.GetLongSymbolFolderForEtl(etlFile, SymbolPaths.NgenPdbExtension);
            Directory.CreateDirectory(ngenFolder);

            string combined = sym.GetCombinedSymbolPath(etlFile);

            Assert.Equal($"SRV*;SRV*{etlFile}.NGENPDB;", combined);
        }

        [Fact]
        public void Can_Get_LocalPath_WithLocalSymFolder_Without_Remote()
        {
            // this test need Admin access or the create directory link privilege 
            if ( !TestContext.IsAdministrator())
            {
                return;
            }

            using var tmp = TempDir.Create();
            string localFolder = tmp.Name;

            SymbolPaths sym = new SymbolPaths()
            {
                RemoteSymbolServer = null,
                SymbolFolder = localFolder,
            };
            string etlFile = Path.Combine(localFolder,"MyTest.etl");
            string ngenFolder = sym.GetLongSymbolFolderForEtl(etlFile, SymbolPaths.NgenPdbExtension);
            Directory.CreateDirectory(ngenFolder);

            string combined = sym.GetCombinedSymbolPath(etlFile);

            Assert.Equal($"SRV*{localFolder};SRV*{sym.GetShortSymbolFolderForEtl(etlFile,SymbolPaths.NgenPdbExtension)};", combined);
        }

        [Fact]
        public void Can_Get_ShortPath_Embedded_And_NgenPDBs()
        {
            // this test need Admin access or the create directory link privilege 
            if (!TestContext.IsAdministrator())
            {
                return;
            }

            using var tmp = TempDir.Create();
            string localFolder = tmp.Name;

            SymbolPaths sym = new SymbolPaths()
            {
                RemoteSymbolServer = null,
                SymbolFolder = localFolder,
            };
            string etlFile = Path.Combine(localFolder, "MyTest.etl");
            string ngenFolder = sym.GetLongSymbolFolderForEtl(etlFile, SymbolPaths.NgenPdbExtension);
            Directory.CreateDirectory(ngenFolder);

            string embeddedPdbFolder = sym.GetLongSymbolFolderForEtl(etlFile, SymbolPaths.EmbeddedPdbExtension);
            Directory.CreateDirectory(embeddedPdbFolder);

            string combined = sym.GetCombinedSymbolPath(etlFile);

            Assert.Equal($"SRV*{localFolder};SRV*{sym.GetShortSymbolFolderForEtl(etlFile, SymbolPaths.NgenPdbExtension)};SRV*{sym.GetShortSymbolFolderForEtl(etlFile, SymbolPaths.EmbeddedPdbExtension)};", combined);
        }

        [Fact]
        public void Link_Creation_Does_Not_Fail_If_Link_Already_Exists()
        {
            // this test need Admin access or the create directory link privilege 
            if (!TestContext.IsAdministrator())
            {
                return;
            }

            using var tmp = TempDir.Create();
            string localFolder = tmp.Name;

            SymbolPaths sym = new SymbolPaths()
            {
                RemoteSymbolServer = null,
                SymbolFolder = localFolder,
            };

            string etlFileName = Path.Combine(localFolder, "test.etl");
            string ngenpdbDirName = etlFileName + SymbolPaths.NgenPdbExtension;
            Directory.CreateDirectory(ngenpdbDirName);
            string longName = sym.GetLongSymbolFolderForEtl(etlFileName, SymbolPaths.NgenPdbExtension);
            sym.CreateSymLinkToSymbolFolder(etlFileName, SymbolPaths.NgenPdbExtension);
            string linkDir = Directory.GetDirectories(localFolder).Where(x => Path.GetFileName(x).StartsWith("#")).FirstOrDefault();
            sym.CreateSymLinkToSymbolFolder(etlFileName, SymbolPaths.NgenPdbExtension);
            Assert.True( Directory.Exists(linkDir));
        }

        [Fact]
        public void Link_Is_Created_And_Removed_In_SymbolFolder()
        {
            // this test need Admin access or the create directory link privilege 
            if (!TestContext.IsAdministrator())
            {
                return;
            }

            using var tmp = TempDir.Create();
            string localFolder = tmp.Name;

            SymbolPaths sym = new SymbolPaths()
            {
                RemoteSymbolServer = null,
                SymbolFolder = localFolder,
            };

            string etlFileName = Path.Combine(localFolder, "test.etl");
            string ngenpdbDirName = etlFileName + SymbolPaths.NgenPdbExtension;
            Directory.CreateDirectory(ngenpdbDirName);
            string longName = sym.GetLongSymbolFolderForEtl(etlFileName, SymbolPaths.NgenPdbExtension);

            sym.CreateSymLinkToSymbolFolder(etlFileName, SymbolPaths.NgenPdbExtension);

            string linkDir = Directory.GetDirectories(localFolder).Where(x => Path.GetFileName(x).StartsWith("#")).FirstOrDefault();
            Assert.True(Directory.Exists(linkDir));

            sym.RemoveShortCuts();
            Assert.False(Directory.Exists(linkDir));
        }
    }
}
