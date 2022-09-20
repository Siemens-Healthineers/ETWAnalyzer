//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Commands;
using ETWAnalyzer.Extractors;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TAU.Toolkit.Diagnostics.Profiling.Simplified;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// This represents a single ETL or test run file which is zipped or a JSON file(extracted ETL).
    /// The file name encodes the test time, name, duration and on which computer it was performed.
    /// The test time is encoded in the file modify date.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class TestDataFile : IEquatable<TestDataFile>
    {
        /// <summary>
        /// Two tests with the same name are treated as equal when they were created around the same time
        /// </summary>
        public static readonly TimeSpan SimilarCreationTimeRange = TimeSpan.FromMinutes(30);

        static readonly string[] ArchiveExtensions = new string[] { ".7z", ".zip", };

        const string EtlExtension = ".etl";

        //[JsonIgnore]
        /// <summary>
        /// When created from a directory each test belongs to a SingleTest which belongs to a TestRun which is part of a TestRunData
        /// which defines some context information which we can need later to look up defendant data files like the Json file
        /// </summary>
        public SingleTest ParentTest
        {
            get;
            internal set;
        }



        /// <summary>
        /// indicates the test status
        /// </summary>
        public TestStatus TestStatus
        {
            get; private set;
        }

        /// <summary>
        /// indicates where the data was generated
        /// </summary>
        public GeneratedAt GeneratedAt
        {
            get; private set;
        }

        /// <summary>
        /// Test case name
        /// </summary>
        [JsonProperty]
        public string TestName
        {
            get; private set;
        }

        /// <summary>
        /// Test duration in milliseconds
        /// </summary>
        [JsonProperty]
        public int DurationInMs
        {
            get; private set;
        }

        /// <summary>
        /// Name of the computer in which it was performed
        /// </summary>
        [JsonProperty]
        public string MachineName
        {
            get; private set;
        } = "";

        /// <summary>
        /// Test time
        /// </summary>
        [JsonProperty]
        public DateTime PerformedAt
        {
            get; private set;
        }

        /// <summary>
        /// Input file name of the etl, zip or json file
        /// </summary>
        [JsonProperty]
        public string FileName
        {
            get; private set;
        }

        /// <summary>
        /// Size of Etl ,zip o json File
        /// </summary>
        [JsonProperty]
        public long SizeInMB
        {
            get; private set;
        }

        /// <summary>
        /// True if etl file is the result from an automated regression test where the file name is of the form 
        /// TestCase_ddmsMachine.zip/.7z/.etl
        /// e.g. CallupAdhocColdReading_9204msDEERLN1T061SRV.7z
        /// </summary>
        [JsonProperty]
        public bool IsValidTest
        {
            get; private set;
        }

        /// <summary>
        /// The source modify date encoded in FileName e.g. 20200717-124447 for file name
        /// CallupAdhocWarmReadingCT_3117msFO9DE01T0162PC.20200717-124447.7z
        /// </summary>
        [JsonProperty]
        public string SpecificModifyDate
        {
            get;private set;
        }

        /// <summary>
        /// Create Test case from ETL or Json file
        /// </summary>
        /// <param name="etlFileOrJsonFile"></param>
        public TestDataFile(string etlFileOrJsonFile) : this(new ETLFileInfo(new FileInfo(etlFileOrJsonFile)))
        {
        }
        /// <summary>
        /// Default Constructor for testing
        /// </summary>
        public TestDataFile() { }

        /// <summary>
        /// 
        /// </summary>
        public ETLFileInfo ETLfileinfo{ get; }
            
        /// <summary>
        /// Encapsulates file name, size and modify date to aid unit testing
        /// </summary>
        public class ETLFileInfo
        {
            /// <summary>
            /// 
            /// </summary>
            public DateTime LastWriteTime { get; }

            /// <summary>
            /// 
            /// </summary>
            public long SizeInMB { get; }

            /// <summary>
            /// 
            /// </summary>
            public string FullName { get; }

            /// <summary>
            /// Used by unit tests
            /// </summary>
            /// <param name="savedFileName"></param>
            /// <param name="sizeInMB"></param>
            /// <param name="lastWrite"></param>
            public ETLFileInfo(string savedFileName, long sizeInMB, DateTime lastWrite)
            {
                LastWriteTime = lastWrite;
                SizeInMB = sizeInMB;
                FullName = savedFileName;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="existingFile"></param>
            public ETLFileInfo(FileInfo existingFile)
            {
                if (existingFile is null)
                {
                    throw new ArgumentNullException(nameof(existingFile));
                }

                try
                {
                    LastWriteTime = existingFile.LastWriteTime;
                    SizeInMB = existingFile.Length / (1024 * 1024);
                    FullName = existingFile.FullName;
                }
                catch(FileNotFoundException ex)
                {
                    string regFile =
                    @"Windows Registry Editor Version 5.00" + Environment.NewLine +
                    @"[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\FileSystem]" + Environment.NewLine +
                    "\"LongPathsEnabled\" = dword:00000001" + Environment.NewLine +
                    @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem]" + Environment.NewLine +
                    "\"LongPathsEnabled\" = dword:00000001" + Environment.NewLine;
                    string regLocation = Environment.ExpandEnvironmentVariables("%temp%\\EnableLongPath.reg");
                    try
                    {
                        File.WriteAllText(regLocation, regFile);
                    }
                    catch(IOException)
                    { }

                    throw new PathTooLongException($"Please enable long path support on your windows machine. Import the reg file located at {regLocation}", ex);
                }
            }
        }
        
        /// <summary>
        /// Create test case from an etl or json file
        /// </summary>
        /// <param name="etlFileOrJsonFile"></param>
        public TestDataFile(ETLFileInfo etlFileOrJsonFile)
        {
            if (etlFileOrJsonFile is null)
            {
                throw new ArgumentNullException(nameof(etlFileOrJsonFile));
            }

            PerformedAt = etlFileOrJsonFile.LastWriteTime;
            SizeInMB = etlFileOrJsonFile.SizeInMB;
            FileName = etlFileOrJsonFile.FullName;

            string ext = Path.GetExtension(FileName).ToLowerInvariant();
            if (ext == EtlExtension)
            {
                // The etl file can be rewritten. Try to determine modify date from archive file if present.
                foreach (var aExt in ArchiveExtensions)
                {
                    string archiveFile = Path.ChangeExtension(FileName, aExt);

                    // Use as write time the archive file because this is usually not modified 
                    if (File.Exists(archiveFile))
                    {
                        this.PerformedAt = new FileInfo(archiveFile).LastWriteTime;
                        
                    }
                }
            }

            ParseTestCaseNameTimeAndComputerNameFromFileName(FileName);

            myExtract = new Lazy<IETWExtract>(() => JsonExtractFileWhenPresent == null ? null : ExtractSerializer.DeserializeFile(JsonExtractFileWhenPresent));
        }

        /// <summary>
        /// When from given zip file the ETL is already extracted to output folder or current folder where the input file is located 
        /// the path of the extracted ETL is returned
        /// </summary>
        public string EtlFileNameIfPresent
        {
            get
            {
                string ext = Path.GetExtension(FileName);
                if (ext.ToLowerInvariant() == ArgParser.EtlExtension)
                {
                    return FileName;
                }
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(FileName);
                OutDir directory = ParentTest?.Parent?.Parent?.OutputDirectory;
                string dir = directory?.TempDirectory ?? directory.OutputDirectory;
                if( dir == null)
                {
                    return null;
                }
                string etlFileName = Path.Combine(dir, fileNameWithoutExtension + TestRun.ETLExtension);
                return File.Exists(etlFileName) ? etlFileName : null;
            }
        }

        /// <summary>
        /// Full path to extracted json file
        /// </summary>
        string myJsonExtractFile;

        /// <summary>
        /// Cache result of Locate operation
        /// </summary>
        bool myLocateHasHappened;

        /// <summary>
        /// Return previously set Json file. If not set explicitly the Json file is tried to locate in the Extract folder besides the ETL file.
        /// The state is cached on first access. If you want to get the current state set it to null to force reevaluation.
        /// </summary>
        public string JsonExtractFileWhenPresent
        {
            get
            {
                if (myJsonExtractFile == null && !myLocateHasHappened)
                {
                    myJsonExtractFile = TryToLocateJsonFile();
                    myLocateHasHappened = true;
                }
                return myJsonExtractFile;
            }
            set
            {
                myJsonExtractFile = value;
                // reset locate flag
                if (value == null )
                {
                    myLocateHasHappened = false;
                }
            }
        }

        private string TryToLocateJsonFile()
        {
            string ext = Path.GetExtension(FileName).ToLowerInvariant();
            string jsonFile = null;
            if (ext == TestRun.ExtractExtension)
            {
                jsonFile = FileName;
            }
            else
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(FileName);
                OutDir directory = ParentTest?.Parent?.Parent?.OutputDirectory;
                if (directory?.OutputDirectory != null)
                {
                    jsonFile = Path.Combine(directory.OutputDirectory, directory.IsDefault ? Program.ExtractFolder : "", fileNameWithoutExtension + TestRun.ExtractExtension);
                }
                if( !File.Exists(jsonFile)) // search below 7z file in Extract Folder
                {
                    jsonFile = Path.Combine(Path.GetDirectoryName(FileName), Program.ExtractFolder, fileNameWithoutExtension + TestRun.ExtractExtension);
                }
                if( !File.Exists(jsonFile)) // search side by side of etl/zip file
                {
                    jsonFile = Path.Combine(Path.GetDirectoryName(FileName), fileNameWithoutExtension + TestRun.ExtractExtension);
                }
            }
            return File.Exists(jsonFile) ? jsonFile : null;
        }

        Lazy<IETWExtract> myExtract;

        /// <summary>
        /// Get Deserialized Extract when present. If it is present it is deserialized and put here so you can query the full object graph from there
        /// </summary>
        public IETWExtract Extract
        {
            get
            {
                return myExtract?.Value;
            }
            set
            {
                myExtract = new Lazy<IETWExtract>(() => value);
            }
        }
        
        /// <summary>
        /// Allow synthetic creation of tests from other sources than the real file. 
        /// This can be useful if the tests are stored in some index file to speed up loading the test information instead of reading
        /// the test data form each file form the file system.
        /// </summary>
        /// <param name="testName"></param>
        /// <param name="durationInMS"></param>
        /// <param name="fullFileName"></param>
        /// <param name="machineName"></param>
        /// <param name="performedAt"></param>
        /// <param name="sizeInMB"></param>
        /// <param name="specificModifyDate"></param>
        /// <param name="isVaildTest"></param>
        /// <param name="generatedAt"></param>
        /// <param name="testStatus"></param>
        [JsonConstructor]
        public TestDataFile(string testName, string fullFileName, DateTime performedAt, int durationInMS, int sizeInMB, string machineName,string specificModifyDate, bool isVaildTest = true, GeneratedAt generatedAt = GeneratedAt.Invalid,TestStatus testStatus = TestStatus.Unknown)
        {
            TestName = testName;
            IsValidTest = isVaildTest;
            FileName = fullFileName;
            PerformedAt = performedAt;
            DurationInMs = durationInMS;
            SizeInMB = sizeInMB;
            SpecificModifyDate = specificModifyDate;
            MachineName = machineName;
            GeneratedAt = generatedAt;
            TestStatus = testStatus;
        }

        /// <summary>
        /// Parse test duration and machine name from file name if it originates from an automated performance test.
        /// e.g. CallupAdhocColdReading_9204msDEERLN1T061SRV.7z or 
        ///      CallupAdhocWarmReadingCT_3117msFO9DE01T0162PC.20200717-124447.7z
        /// </summary>
        /// <param name="fullFileName">File name to parse.</param>
        void ParseTestCaseNameTimeAndComputerNameFromFileName(string fullFileName)
        {

            var outputFileName = TAU.Toolkit.Diagnostics.Profiling.Simplified.OutputFileName.ParseFromFileName(fullFileName);
            if (outputFileName != null)
            {
                IsValidTest = true;
                TestName = outputFileName.TestCaseName;
                PerformedAt = outputFileName.ProfilingStoppedTime;
                DurationInMs = outputFileName.TestDurationinMS;
                MachineName = outputFileName.MachineWhereResultsAreGeneratedOn;
                GeneratedAt = outputFileName.GeneratedAt;
                TestStatus = outputFileName.TestStatus;
            }
            else
            {

                string[] parts = Path.GetFileName(fullFileName).Split(new char[] { '_' });
                if (parts.Length > 1)
                {
                    parts = new string[] { parts[0], String.Join("_", parts.Skip(1)) };
                }
                if (parts.Length > 1)
                {
                    // Take all digits and parse them as ms
                    string digits = new(parts[1].TakeWhile(Char.IsDigit).ToArray());
                    if (digits.Length > 0 && int.TryParse(digits, out int durationInMS) && parts[1].IndexOf("ms", digits.Length, 2, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        int sIdx = parts[1].IndexOf('s'); // get first char after dddddms
                        int len = parts[1].Length - sIdx - 1;
                        if (parts[1].IndexOf('.') > -1) // until .
                        {
                            len = parts[1].IndexOf('.') - sIdx - 1;
                        }

                        if (sIdx > -1 && len > 0) // if we have an s after the digits and before the . a length > 0 interpret this as machine name
                        {
                            IsValidTest = true;
                            TestName = parts[0];
                            DurationInMs = durationInMS;
                            MachineName = parts[1].Substring(sIdx + 1, len);
                            int matches = 0;
                            for (int i = MachineName.Length - 1; i >= 0; i--)
                            {
                                // we have two formats 
                                // profiling uses            TestCase_dddmsMachine.Date-Time.7z
                                // simplified profiling uses TestCase_dddmsMachine-Date-Time.7z
                                // SSTUaPMapWorkitemFromRTC2_2645msDEFOR09T130SRV.20200725-125302
                                // 
                                if (MachineName[i] == '-' || MachineName[i] == '.')
                                {
                                    matches++;
                                    if (matches == 2)
                                    {
                                        MachineName = MachineName.Substring(0, i);
                                        break;
                                    }
                                }
                                else
                                {
                                    // if we have sth else than digits at the end then we need to skip splitting because it is sth else
                                    if (!Char.IsDigit(MachineName[i]))
                                    {
                                        break;
                                    }
                                }
                            }

                            string lastPart = Path.GetFileNameWithoutExtension(parts[1]);

                            int lastIdx = lastPart.Length;

                            // move two split chars back
                            for (int i = 0; i < 2 && lastIdx != -1; i++)
                            {
                                lastIdx = lastPart.LastIndexOfAny(new char[] { '.', '-' }, lastIdx - 1);
                            }

                            if (lastIdx > 0)
                            {
                                // e.g. CallupAdhocWarmReadingCT_3117msFO9DE01T0162PC.20200717-124447.7z 
                                SpecificModifyDate = lastPart.Substring(lastIdx + 1);
                            }
                        }
                    }
                }

                if (!IsValidTest)
                {
                    TestName = Path.GetFileNameWithoutExtension(fullFileName);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsEqualOrTestFromOtherMachineAroundSameTime(TestDataFile other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (Object.ReferenceEquals(other, this))
            {
                return true;
            }

            if (other.DurationInMs == this.DurationInMs &&
                this.HasSimilarCreationDate(other.PerformedAt))
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        string[] myScreenshots;
        /// <summary>
        /// Get screenshot files of current test or the report.html file if present
        /// </summary>
        public string[] Screenshots
        {
            get
            {
                if (myScreenshots == null)
                {
                    var screenshots = GetRelatedFiles(false)
                                                .Where(x => x.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                                            x.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                                                .ToList();

                    // this works only with extracted etl files 
                    string reportFile = Path.Combine(FileName + ".Screenshots", "Report.html");
                    if (File.Exists(reportFile))
                    {
                        screenshots.Add(reportFile);
                    }
                    myScreenshots = new string[2];
                    myScreenshots = screenshots.ToArray();
                    return screenshots.ToArray();
                }
                else { return myScreenshots; }
                }
            set { myScreenshots = value; }

        }

        /// <summary>
        /// Get for a given testcase the current file and screenshots. 
        /// If bOtherMachine is true the test file and screenshots of another computer are also returned. 
        /// <param name="bOtherMachine">If true the related files of other machines within the test time +- 30 minutes are also returned. </param>
        /// </summary>
        public string[] GetRelatedFiles(bool bOtherMachine)
        {
            string fileMask = Path.GetFileName(FileName);
            if (!String.IsNullOrEmpty(MachineName) && bOtherMachine)
            {
                fileMask = fileMask.Replace(MachineName, "*");
            }
            fileMask += "*";

            return Directory.GetFiles(Path.GetDirectoryName(FileName), fileMask)
                                .Select(x => new FileInfo(x))
                                .Where(HasSimilarCreationDate)
                                .Select(x => x.FullName)
                                .ToArray();
        }


        bool HasSimilarCreationDate(FileInfo other)
        {
            return HasSimilarCreationDate(other.LastWriteTime);
        }

        bool HasSimilarCreationDate(DateTime other)
        {
            return other >= this.PerformedAt - SimilarCreationTimeRange &&
                    other <= this.PerformedAt + SimilarCreationTimeRange;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{TestName} {PerformedAt.ToShortDateString()} {DurationInMs}ms {MachineName}";
        }

        /// <summary>
        /// Compare TestName, Duration, MachineName, PerformedAt, FileName, SizeInMB and IsValidTest flags
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>true if all properties match false otherwise</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as TestDataFile);
        }

        /// <summary>
        /// Compare TestName, Duration, MachineName, PerformedAt, FileName, SizeInMB and IsValidTest flags
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if all properties match false otherwise</returns>
        public bool Equals(TestDataFile other)
        {
            return other != null &&
                   TestName == other.TestName &&
                   DurationInMs == other.DurationInMs &&
                   MachineName == other.MachineName &&
                   PerformedAt == other.PerformedAt &&
                   FileName == other.FileName &&
                   SizeInMB == other.SizeInMB &&
                   IsValidTest == other.IsValidTest;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hashCode = -1787730904;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TestName);
            hashCode = hashCode * -1521134295 + DurationInMs.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(MachineName);
            hashCode = hashCode * -1521134295 + PerformedAt.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FileName);
            hashCode = hashCode * -1521134295 + SizeInMB.GetHashCode();
            hashCode = hashCode * -1521134295 + IsValidTest.GetHashCode();
            return hashCode;
        }

        string _UniqueKeyName;

        /// <summary>
        /// Get unique key name for this instance. The computer name is omitted since for a graph we usually want to search
        /// later for both or several data points which were recorded from different computers
        /// </summary>
        public string UniqueKeyName
        {
            get
            {
                if (_UniqueKeyName == null)
                {
                    _UniqueKeyName = $"{TestName} {PerformedAt.ToString(CultureInfo.InvariantCulture)} {DurationInMs}ms";
                }

                return _UniqueKeyName;
            }
        }
    }
}
