//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Helper;
using ETWAnalyzer_uTest.TestInfrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace ETWAnalyzer_uTest
{
    /// <summary>
    /// The TestData class encapsulates access to test files.
    /// The files are located under the TestData directory of the test assembly where all files have set as build 
    /// option "Copy If Newer" to copy them to the binary output directory when test dll is compiled.
    /// The directory layout in the bin folder is therefore
    /// ..\bin\
    ///          ETWAnalyzer_uTest.dll
    ///          TestData\
    ///                      .... Test Files some of them compressed.
    /// The TestData class will never operate on the checked in test files but only on copied filed during the compile
    /// Some files are compressed which are uncompressed on first access to speed up the tests. Subsequent test runs
    /// will operate then on uncompressed file which reduces the version control history size. 
    /// </summary>
    public static class TestData
    {
        private static string executableDirectory;
        public static string ExecutableDirectory { 
            get
            {
                //needed to be done again as it is null if we execute tests using the VisualStudio Test Explorer in some scenarios
                //(e.g. Microsoft Visual Studio Enterprise 2022 (64-bit) - CurrentVersion 17.7.4)
                if (executableDirectory == null)
                    SetExecutableDirectory();
                return executableDirectory;

            }
            set => executableDirectory = value;
        }

        static TestData()
        {
            SetExecutableDirectory();
        }

        private static void SetExecutableDirectory()
        {
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);
            executableDirectory = dirPath;
        }

        /// <summary>
        /// Get subdirectory below the current dll named TestData
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        static string GetPath(string fileName)
        {
            return Path.Combine(ExecutableDirectory, "TestData", fileName);
        }

        public static string ExceptionFilteringRules
        {
            get
            {
                return GetPath("ExceptionFilters.xml");
            }
        }

        /// <summary>
        /// Compressed ETL file for zip tests to keep tests fast
        /// </summary>
        public static string EmptyETL
        {
            get
            {
                return GetPath(nameof(EmptyETL) + ".7z");
            }
        }

        /// <summary>
        /// Compressed 7z file which contains a text file which violates the assumptions of
        /// ZipExtractor to find an ETL file with the same name after decompressing it.
        /// </summary>
        public static string ZipWithTextFile
        {
            get
            {
                return GetPath(nameof(ZipWithTextFile) + ".7z");
            }
        }

        /// <summary>
        /// Compressed 7z file which contains an empty etl and the 7zip log file which should be excluded during extraction.
        /// </summary>
        public static string ZipWith7zLogFile
        {
            get
            {
                return GetPath(nameof(ZipWith7zLogFile) + ".7z");
            }
        }

        /// <summary>
        /// Compressed zip file which contains one valid empty etl file and a second file
        /// This is used to check if the skip functionality works as expected.
        /// </summary>
        public static string ZipWithTwoFiles
        {
            get => GetPath(nameof(ZipWithTwoFiles) + ".zip");
        }

        public static string ServerZipFileNameNoPath
        {
            get => "CallupAdhocWarmReadingCT_3117msDEFOR09T121SRV.20200717-124447.7z";
        }

        public static string ServerEtlFileNameNoPath
        {
            get => Path.GetFileNameWithoutExtension(ServerZipFileNameNoPath) + ".etl";
        }


        public static string ServerZipFile
        {
            get => GetPath(ServerZipFileNameNoPath);
        }

        public static string ServerJsonFileName
        {
            get => GetPath(ServerJsonFileNameNoPath);
        }

        public static string ClientJsonFileName
        {
            get => GetPath(ClientJsonFileNameNoPath);
        }

        public static string ServerJsonFileNameNoPath
        {
            get => Path.GetFileNameWithoutExtension(ServerZipFileNameNoPath) + ".json";
        }

        public static string ClientJsonFileNameNoPath
        {
            get => Path.GetFileNameWithoutExtension(ClientZipFileNameNoPath) + ".json";
        }

        static readonly object _Lock = new object();

        /// <summary>
        /// Server ETL file from one test run
        /// </summary>
        public static string ServerEtlFile
        {
            get
            {
                lock (_Lock) // prevent concurrent unzip of test data which can cause problems in some tests
                {
                    string samplePath = GetPath("SampleData");
                    string unzipedFile = Path.Combine(samplePath, Path.GetFileNameWithoutExtension(ServerZipFile) + ".etl");
                    if (!File.Exists(unzipedFile))
                    {
                        // To speed up the tests store only the compressed version and unzip it only once
                        // the EtlZipCommand will skip decompression if the resulting ETL file exist already in place
                        EtlZipCommand cmd = new EtlZipCommand();
                        unzipedFile = cmd.Unzip(ServerZipFile, null, new ETWAnalyzer.Extract.SymbolPaths { SymbolFolder = "C:\\Symbols" });
                    }
                    return unzipedFile;
                }
            }
        }

        public static string ClientZipFileNameNoPath
        {
            get => "CallupAdhocWarmReadingCT_3117msFO9DE01T0162PC.20200717-124447.7z";
        }

        public static string ClientEtlFileNameNoPath
        {
            get => Path.GetFileNameWithoutExtension(ClientZipFileNameNoPath) + ".etl";
        }

        public static string ClientZipFile
        {
            get => GetPath(ClientZipFileNameNoPath);
        }

        public static string TestRunConfigurationXml
        {
            get => GetPath("TestRunConfiguration.xml");
        }

        /// <summary>
        /// Client ETL file from one test run
        /// </summary>
        public static string ClientEtlFile
        {
            get
            {
                lock (_Lock)
                {
                    string samplePath = GetPath("SampleData");
                    string unzipedFile = Path.Combine(samplePath, Path.GetFileNameWithoutExtension(ClientZipFile) + ".etl");

                    if (!File.Exists(unzipedFile))
                    {
                        // To speed up the tests store only the compressed version and unzip it only once
                        // the EtlZipCommand will skip decompression if the resulting ETL file exist already in place
                        EtlZipCommand cmd = new EtlZipCommand();
                        unzipedFile = cmd.Unzip(ClientZipFile, null, new ETWAnalyzer.Extract.SymbolPaths { SymbolFolder = "C:\\Symbols" });
                    }
                    return unzipedFile;
                }
            }
        }


        public static string DiscIOWPAProfile
        {
            get => GetPath("DiscIO.wpaProfile");
        }

        public static string DiscIOWpaProfileCsvFileName
        {
            get => "Disk_Usage_Utilization_by_Disk.csv";
        }

        /// <summary>
        /// CSV file which contains numbers in german number separator format
        /// </summary>
        public static string DiscIOWpaProfile_DE
        {
            get => GetPath("Disk_Usage_Utilization_by_Disk_DE.csv");
        }

        /// <summary>
        /// CSV file contains only header but no data
        /// </summary>
        public static string DiscIOWpaProfiler_Empty
        {
            get => GetPath("Disk_Usage_Utilization_by_Disk_Empty.csv");
        }

        /// <summary>
        /// Return full path to sample CSV file
        /// </summary>
        public static string DiscIOCSVFile
        {
            get => GetPath(DiscIOWpaProfileCsvFileName);
        }

        public static string CPUWPAProfile
        {
            get => GetPath("CPUWeight.wpaProfile");
        }

        public static string CPUWpaProfileCSVFileName
        {
            get => "CPU_Usage_(Sampled)_Utilization_By_CPU.csv";
        }

        /// <summary>
        /// Get directory containing uncompressed test run files which are all 0 bytes in size for testing the testrun logic.
        /// </summary>
        public static DataOutput<string> TestRunDirectory
        {
            get => UnzipTestContainerFileOnlyOnce.Value;
        }

        /// <summary>
        /// Copies an folder of empty .7z files and converts them to .json
        /// </summary>
        /// <returns>folder with empty .json files</returns>
        public static string TestRunDirectoryJson(ITempOutput temp)
        {
            Directory.CreateDirectory(Path.Combine(temp.Name, Program.ExtractFolder));
            DataOutput<string> value = UnzipTestContainerFileOnlyOnce.Value;
            DirectoryInfo directoryInfo = new DirectoryInfo(value.Data);
            FileInfo[] fileinfo = directoryInfo.GetFiles();
            HashSet<string> compressedEtl = new HashSet<string>();
            List<TestDataFile> filesToExtract = new List<TestDataFile>();


            Parallel.ForEach(fileinfo, new ParallelOptions
            {
                MaxDegreeOfParallelism = 4,
            }, (file) =>
            {
                string emptyJsonFile = Path.Combine(temp.Name, Program.ExtractFolder, file.Name.Substring(0, file.Name.Length - 3) + ".json");
                using (var createdFile = new FileStream(emptyJsonFile, FileMode.Create))
                {
                }
                (new FileInfo(emptyJsonFile)).LastWriteTime = file.LastWriteTime;

                lock (compressedEtl)
                {
                    compressedEtl.Add(file.FullName);
                    filesToExtract.Add(new TestDataFile(file.FullName));
                }
            });

            return Path.Combine(temp.Name, Program.ExtractFolder);
        }

        public static DataOutput<string> TestRunSample_Server
        {
            get
            {
                DataOutput<string> output = TestRunDirectory;
                return new DataOutput<string>(Path.Combine(output.Data, TestSampleFileServer), output.Output);
            }
        }

        public static DataOutput<string> TestRunSample_Client
        {
            get
            {
                DataOutput<string> output = TestRunDirectory;
                return new DataOutput<string>(Path.Combine(output.Data, TestSampleFileClient), output.Output);
            }
        }

        /// <summary>
        /// Lazy is a special construct which executes the passed method only once. That way we
        /// can ensure that the access to the path is fast without uncompressing the 7z file during every property get call.
        /// </summary>
        static readonly Lazy<DataOutput<string>> UnzipTestContainerFileOnlyOnce = new Lazy<DataOutput<string>>(UnzipFiles("SampleData", TestSampleFileServer));

        const string TestSampleFileClient = "CallupAdhocColdReadingCR_11341msFO9DE01T0166PC.7z";
        const string TestSampleFileServer = "CallupAdhocColdReadingCR_11341msDEFOR09T121SRV.7z";

        static DataOutput<string> UnzipFiles(string pathName, string sampleTestFile)
        {
            string samplePath = GetPath(pathName);
            string extractedFile = Path.Combine(samplePath, sampleTestFile);
            string outputStr = null;
            // check if file is already uncompressed 
            if (!File.Exists(extractedFile))
            {
                // No then uncompress it now
                ProcessCommand cmd = new ProcessCommand("7z.exe", $"x {Path.Combine(samplePath, pathName + ".7z")} -o{samplePath}");
                ExecResult output = cmd.Execute();
                outputStr = $"7z output: {output.AllOutput}";
            }


            return new DataOutput<string>(samplePath, outputStr);
        }

        public static string GetSampleDataV2SpecificDate
        {
            get => UnzipTestContainerFileOnlyOnceV2.Value.Data;
        }

        static readonly Lazy<DataOutput<string>> UnzipTestContainerFileOnlyOnceV2 = new Lazy<DataOutput<string>>(UnzipFiles("SampleDataV2", TestSampleFileServerV2));
        const string TestSampleFileServerV2 = "CallupAdhocColdReadingCR_12701msDEFOR09T121SRV.20190923-164416.7z";

        public static string GetSampleDataJson
        {
            get => Path.Combine(UnzipTestContainerFileOnlyOnceJson.Value, Program.ExtractFolder);
        }

        static readonly Lazy<string> UnzipTestContainerFileOnlyOnceJson = new Lazy<string>(UnzipFilesJson);

        static string UnzipFilesJson()
        {
            string samplePath = GetPath("SampleDataJson");
            string extractedFile = Path.Combine(samplePath, Program.ExtractFolder, "CallupAdhocColdReadingCT_25498msDEFOR09T130SRV.20191107-111219.json");
            // check if file is already uncompressed 
            if (!File.Exists(extractedFile))
            {
                // No then uncompress it now
                ProcessCommand cmd = new ProcessCommand("7z.exe", $"x {Path.Combine(samplePath, Program.ExtractFolder, "SampleDataJsonFiles.sample")} -o{Path.Combine(samplePath, Program.ExtractFolder)}");
                ExecResult output = cmd.Execute();
                Console.WriteLine($"7z output: {output.AllOutput}");
            }
            return samplePath;
        }

        /// <summary>
        /// This folder contains all expected snapshots
        /// </summary>
        public static string ExpectedBitmappingOfTests
        {
            get { return GetPath("SampleBitmapping"); }
        }
    }
}
