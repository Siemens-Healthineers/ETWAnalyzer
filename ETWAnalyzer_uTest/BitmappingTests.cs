//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Helper;
using ETWAnalyzer.ScreenshotBitmapping;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ETWAnalyzer_uTest
{
    public class BitmappingTests
    {
        [Fact]
        public void Can_Read_Sample_Bitmapping()
        {
            SampleBitmapGenerator bitmapGenerator = new(TestData.ExpectedBitmappingOfTests);
            bitmapGenerator.ReadFromPrimaryFolder();
            Assert.True(bitmapGenerator.TestFolders.All(x => x.Screenshots.All(s => s.SumOfBlGrReComponentsPerPixel != 0)));
        }
        [Fact]
        public void Can_Serialize_SampleBitmappingConfig()
        {
            using var tmp = TempDir.Create();
            
                SampleBitmapGenerator bitmapGenerator = new(TestData.ExpectedBitmappingOfTests);
                bitmapGenerator.ReadFromPrimaryFolder();
                bitmapGenerator.SerializeAsExpectedScreenshotConfig(Path.Combine(tmp.Name,SampleBitmapGenerator.FileName));
                Assert.True(File.Exists(bitmapGenerator.ConfigFile));
                Assert.True(new FileInfo(bitmapGenerator.ConfigFile).Length > 1);
            
        }
        [Fact]
        public void Can_SerializeAndDeserialize_SampleBitmappingConfig()
        {
            using var tmp = TempDir.Create();
            
                SampleBitmapGenerator bitmapGenerator = new(TestData.ExpectedBitmappingOfTests);
                bitmapGenerator.ReadFromPrimaryFolder();
                bitmapGenerator.SerializeAsExpectedScreenshotConfig(Path.Combine(tmp.Name, SampleBitmapGenerator.FileName));
                var deserialized = bitmapGenerator.DeserializeExpectedScreenshotConfig(bitmapGenerator.ConfigFile);
                Assert.True(deserialized.TestFolders.Count > 1);
            
        }

    }
}
