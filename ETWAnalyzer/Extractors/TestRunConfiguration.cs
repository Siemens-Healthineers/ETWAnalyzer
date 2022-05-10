//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ETWAnalyzer.Extract;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Testnames and expected risen number of each test deserialized from TestRunConfiguration.xml
    /// </summary>
    public class TestRunConfiguration
    {
        readonly ExpectedTestRun myExpectedRun;

        /// <summary>
        /// 
        /// </summary>
        public ExpectedTestRun ExpectedRun { get => myExpectedRun; }


        internal TestRunConfiguration()
        {
            XmlSerializer serializer = new(typeof(ExpectedTestRun));

            using var inFile = File.OpenRead(ConfigFiles.ExpectedTestsInTestRun);
            myExpectedRun = (ExpectedTestRun)serializer.Deserialize(inFile);

        }       
        
    }
    /// <summary>
    /// Contains a list of SingleTests/TestCases 
    /// </summary>
    public class ExpectedTestRun
    {
        ExpectedTestRun(){ }       
        
        /// <summary>
        /// 
        /// </summary>
        public List<TestCase> TestCases { get; set; } = new List<TestCase>();
    }

    /// <summary>
    /// Each TestCase/ SingleTests with Name and number of occurring
    /// </summary>
    public class TestCase
    {

        /// <summary>
        /// 
        /// </summary>
        public TestCase()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="testCaseName"></param>
        /// <param name="iterationCount"></param>
        public TestCase(string testCaseName,int iterationCount)
        {
            TestCaseName = testCaseName;
            IterationCount = iterationCount;
        }

        /// <summary>
        /// 
        /// </summary>
        public string TestCaseName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int IterationCount { get; set; }
       
    }
    
}
