//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers;
using ETWAnalyzer.JsonSerializing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ETWAnalyzer.ScreenshotBitmapping
{
    /// <summary>
    /// This Folder structure must be stored before the NewExceptionAnalyzer runs.
    /// After creating a NewException instance the folders can be deleted, because the screenshot data is stored to the SampleBitmapping.json
    /// Generates colorvalues in SampleBitmapping.json for screenshotsamples
    /// Folderstructure:
    /// PrimaryFolder: SampleBitmapping
    ///     ->TestFolder1 (=TestName)
    ///         ->SampleScreenshotA1.png
    ///         ->SampleScreenshotA2.png
    ///         ->...
    ///     ->TestFolder2
    ///         ->SampleScreenshotB1.png
    ///         ->...
    ///         
    /// Analyzes all colors of the Screenshots and serializes the Folderstructure with average color values 
    /// </summary>
    public class SampleBitmapGenerator:JsonCreationBase<SampleBitmapGenerator>
    {
        /// <summary>
        /// 
        /// </summary>
        public const string FileName ="SampleBitmapping.json";

        /// <summary>
        /// 
        /// </summary>
        public string ConfigFile { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string PrimaryFolder { get; set; }

        /// <summary>
        ///
        /// </summary>
        public List<TestWithScreenshots> TestFolders { get; set; } = new List<TestWithScreenshots>();
        /// <summary>
        /// Json Constructor
        /// </summary>
        public SampleBitmapGenerator()
        { }
        /// <summary>
        /// The primary folder is necessary to generate the configuration
        /// </summary>
        /// <param name="primaryFolder">top folder</param>
        public SampleBitmapGenerator(string primaryFolder)
        {
            PrimaryFolder = primaryFolder;
        }
        /// <summary>
        /// Reads all Folders below the PrimaryFolder - sets them as TestNames with Screenshots
        /// Prints all found Tests with screenshots
        /// </summary>
        public void ReadFromPrimaryFolder()
        {
            string[] subfolders = Directory.GetDirectories(PrimaryFolder);
            foreach (var testfolder in subfolders)
            {
                TestFolders.Add(new TestWithScreenshots(testfolder));
            }
            Print();
        }
        /// <summary>
        /// Serializes the sample screenshot config if it does not exist
        /// </summary>
        /// <param name="filePath">serialize to this path - Default: ConfigFiles.SampleBittmapping</param>
        public void SerializeAsExpectedScreenshotConfig(string filePath)
        {
            if(File.Exists(filePath))
            { 
                Console.WriteLine($"{filePath} already exists.");
            }
            else
            {
                using FileStream fs = File.Create(ConfigFile = filePath);
                SerializeToJson(this, fs);
            }
        }
        /// <summary>
        /// Deserializes the expected screenshot color values
        /// </summary>
        /// <param name="filePath">should be stored in the Configuration</param>
        /// <returns>An deserialized SampleBitmapGenerator instance</returns>
       public SampleBitmapGenerator DeserializeExpectedScreenshotConfig(string filePath)
        {
            if (File.Exists(filePath))
            {
                using FileStream fs = File.OpenRead(ConfigFile = filePath);
                return DeserializeJson(fs);
            }
            else
            {
                throw new FileNotFoundException($"{filePath} not exists.");
            }
        }
        /// <summary>
        /// Prints the found folder structure
        /// </summary>
        void Print()
        {
            Console.WriteLine($"PrimaryFolder: {PrimaryFolder}\n");
            foreach (var test in TestFolders)
            {
                test.Print();
            }
        }
        /// <summary>
        /// Returns the the instance which matched to testname
        /// </summary>
        /// <param name="testname">Search for TestWithScreenshot intance for this test</param>
        /// <returns>TestWithScreenshots instance matching to testname </returns>
        public TestWithScreenshots GetExpectedColors(string testname)
        {
            return TestFolders.Find(x => x.TestName == testname);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class TestWithScreenshots
    {
        /// <summary>
        /// Test which Screenshots belong to
        /// </summary>
        public string TestName { get; set; }
        /// <summary>
        /// All Screenshot of the test with relevant average color values
        /// </summary>
        public List<Screenshot> Screenshots { get; set; } = new List<Screenshot>();
        /// <summary>
        /// Json Constructor
        /// </summary>
        public TestWithScreenshots()
        { }
        /// <summary>
        /// Sets the TestName
        /// Generates Screenshotcolorvalues for the .png below the testfoler
        /// </summary>
        /// <param name="testFolder">screenshots should exist below this folder</param>
        public TestWithScreenshots(string testFolder)
        {
            TestName = Path.GetFileName(testFolder);
            GenerateVaildScreenshotdata(Directory.GetFiles(testFolder));
        }
        /// <summary>
        /// Checks if screenshots are valid and generates Screenshot instances
        /// </summary>
        /// <param name="paths"></param>
        private void GenerateVaildScreenshotdata(string[] paths)
        {
            int maxThread = (int)(Environment.ProcessorCount * (75 / 100.0f));
            Parallel.ForEach(paths, new ParallelOptions { MaxDegreeOfParallelism = maxThread }, path =>
                {
                    if (Path.GetExtension(path) != ".png")
                    {
                        throw new InvalidDataException("Screenshot (Extension: .png) expected.");
                    }
                    Screenshots.Add(new Screenshot(path));
                });
        }
        /// <summary>
        /// Prints the folderstructure below the Testfolder
        /// </summary>
        public void Print()
        {
            Console.WriteLine($"For Test: {TestName} ");
            foreach (var shot in Screenshots)
            {
                Console.WriteLine(shot);
            }
            Console.WriteLine("found.\n");
        }
    }




}


