//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer_uTest
{
    /// <summary>
    /// Tests are built by the pattern 
    /// Arrange
    /// Act
    /// Assert
    /// See https://docs.microsoft.com/en-us/visualstudio/test/unit-test-basics?view=vs-2017
    /// https://medium.com/@pjbgf/title-testing-code-ocd-and-the-aaa-pattern-df453975ab80
    /// </summary>
    
    public class SingleTestTests
    {
        [Fact]
        public void Add_One_TestDataFile_Shows_Up_In_TestsArray()
        {
            TestDataFile[] files = new TestDataFile[]
            {
              new TestDataFile("", TestData.ClientEtlFile, new DateTime(2018, 2, 18), 11341, 200, "FO9DE01T0166PC",null),
            };

            SingleTest testcase = new SingleTest(files);
            Assert.Single(testcase.Files);
            Assert.Same(files[0], testcase.Files[0]);

            Assert.NotNull(files[0]);
            Assert.NotNull(files[0].FileName);
            Assert.NotEqual(default, files[0].PerformedAt);
            Assert.NotEqual(0L, files[0].SizeInMB);
            Assert.NotNull(files[0].MachineName);
        }

        [Fact]
        public void Add_Two_TestDataFiles_With_Different_Time_Stamps_Throws_Exception()
        {
            TestDataFile[] file = new TestDataFile[2];
            file[0] = new TestDataFile("", TestData.ClientEtlFile, new DateTime(2018, 4, 15), 11341, 200, "FO9DE01T0166PC",null);
            file[1] = new TestDataFile("", TestData.ClientEtlFile, new DateTime(2018, 2, 18), 11341, 200, "FO9DE01T0166PC",null);
                        
            ExceptionAssert.Throws<ArgumentException>(() => new SingleTest(file), "have not same generation date");

        }

        [Fact]
        public void Add_Two_TestDataFiles_With_Same_Time_Stamps_Succeeds()
        {
            var testTime = new DateTime(2018, 2, 18);

            TestDataFile[] file = new TestDataFile[2];
            file[0] = new TestDataFile("", TestData.ClientEtlFile, testTime, 11341, 200, "FO9DE01T0166PC",null);

            file[1] = new TestDataFile("", TestData.ClientEtlFile, testTime, 11341, 200, "FO9DE01T0166PC",null);
            var testcase = new SingleTest(file);

            Assert.Equal(2, testcase.Files.Count);

            Assert.Equal(testTime, testcase.PerformedAt);
        }

        [Fact]
        public void IEnumerableCtor_Add_Two_TestDataFiles_With_Same_Time_Stamps_Succeeds()
        {
            var testTime = new DateTime(2018, 2, 18);

            TestDataFile[] file = new TestDataFile[2];
            file[0] = new TestDataFile("", TestData.ClientEtlFile, testTime, 11341, 200, "FO9DE01T0166PC",null);

            file[1] = new TestDataFile("", TestData.ClientEtlFile, testTime, 11341, 200, "FO9DE01T0166PC",null);
            var testcase = new SingleTest((IEnumerable<TestDataFile>)file);

            Assert.Equal(2, testcase.Files.Count);

            Assert.Equal(testTime, testcase.PerformedAt);
        }


    }
}
