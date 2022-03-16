//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETWAnalyzer.Extract;
using System.IO;

namespace ETWAnalyzer.EventDump
{
    /// <summary>
    /// Base class for hybrid commands which can cope with an ETL file or a Json file or directory
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class DumpFileEtlBase<T> : DumpFileDirBase<T>
    {
        public override List<T> ExecuteInternal()
        {
            List<T> lret = new();

            string ext = Path.GetExtension(FileOrDirectory);
            string etlFile = FileOrDirectory;
            if (ext == TestRun.SevenZExtension)
            {
                etlFile = ExtractEtlInplace(etlFile);
            }
            if (Path.GetExtension(etlFile) == TestRun.ETLExtension)
            {
                lret.AddRange(DumpETL(etlFile));
            }
            else  // assume Json extract files
            {
                Lazy<SingleTest>[] tests = base.GetTestRuns(true, SingleTestCaseFilter, TestFileFilter);
                WarnIfNoTestRunsFound(tests);
                foreach (Lazy<SingleTest> test in tests)
                {
                    foreach (TestDataFile file in test.Value.Files)
                    {
                        lret.AddRange(DumpJson(file));
                    }
                }
            }

            return lret;
        }

        protected abstract List<T> DumpJson(TestDataFile file);
        protected abstract List<T> DumpETL(string etlFileName);

        private string ExtractEtlInplace(string etlFile)
        {
            Console.WriteLine($"Uncompressing compressed ETL in place at {etlFile}");
            var zipExtract = new EtlZipCommand();
            string decompressedFile = zipExtract.Unzip(etlFile, null, new SymbolPaths { SymbolFolder = Settings.Default.SymbolDownloadFolder });
            return decompressedFile;
        }


    }
}