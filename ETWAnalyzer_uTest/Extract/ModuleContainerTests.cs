//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Modules;
using ETWAnalyzer.Extractors;
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
    public class ModuleContainerTests
    {
        [Fact]
        public void Can_Serialize_Deserialize_Data()
        {
            ETWExtract extract = new();
            extract.Processes = new List<ETWProcess>
            {
               new ETWProcess { ProcessName = "Test.exe", ProcessID = 100 },
               new ETWProcess { ProcessName = "Test2.exe", ProcessID = 200 }
            };

            ModuleContainer container = new();
            container.Add(extract, (ETWProcessIndex) 0, "C:\\Windows\\1.dll", "File Version of 1.dll", "Product Version of 1.dll", "Product Name of 1.dll", new Version(1, 0, 100, 1), "Description of 1.dll");
            container.Add(extract, (ETWProcessIndex) 1, "C:\\Windows\\1.dll", "File Version of 1.dll", "Product Version of 1.dll", "Product Name of 1.dll", new Version(1, 0, 100, 1), "Description of 1.dll");
            container.Add(extract, (ETWProcessIndex) 1, "C:\\Windows\\2.dll", "File Version of 2.dll", "Product Version of 2.dll", "Product Name of 2.dll", new Version(1, 0, 100, 1), "Description of 2.dll");

            extract.Modules = container;

            var stream = new MemoryStream();
            ExtractSerializer.Serialize<ETWExtract>(stream, extract);

            stream.Position = 0;

            using var expprinter = new ExceptionalPrinter();

            string str = Encoding.UTF8.GetString(stream.ToArray());
            expprinter.Messages.Add($"Serialized: {str}");
            ETWExtract deser = ExtractSerializer.Deserialize<ETWExtract>(stream);

            ModuleContainer dcontainer = deser.Modules;
            Verify(dcontainer);
        }

        private void Verify(ModuleContainer dcontainer)
        {
            Assert.Equal(2, dcontainer.Modules.Count);
            ModuleDefinition m0 = dcontainer.Modules[0];
            ModuleDefinition m1 = dcontainer.Modules[1];

            
            Assert.Equal("2.dll", m1.ModuleName);

            Assert.Equal("1.dll", m0.ModuleName);
            Assert.Equal("C:\\Windows", m0.ModulePath);
            Assert.Equal("File Version of 1.dll", m0.FileVersionStr);
            Assert.Equal("Product Version of 1.dll", m0.ProductVersionStr);
            Assert.Equal("Product Name of 1.dll", m0.ProductName);
            Assert.Equal(new Version(1, 0, 100, 1), m0.Fileversion);
            Assert.Equal("Description of 1.dll", m0.Description);

            IReadOnlyList<ETWProcess> processes = m0.Processes;

            Assert.Equal(2, processes.Count);
            Assert.Equal("Test.exe", processes[0].ProcessName);


            IReadOnlyList<ETWProcess> processes1 = m1.Processes;

            Assert.Single(processes1);
            Assert.Equal("Test2.exe", processes1[0].ProcessName);
        }
    }
}
