//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using Dia2Lib;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Common;
using ETWAnalyzer.Extract.CPU;
using ETWAnalyzer.Extract.CPU.Extended;
using ETWAnalyzer.Extract.FileIO;
using ETWAnalyzer.Extract.Handle;
using ETWAnalyzer.Extract.Modules;
using ETWAnalyzer.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SevenZip;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extractors
{
    class ExtractSerializer
    {
        /// <summary>
        /// File IO data is stored in external file by default
        /// </summary>
        public const string FileIOPostFix = "FileIO";

        /// <summary>
        /// Module data is stored in external file with this postfix
        /// </summary>
        public const string ModulesPostFix = "Modules";

        /// <summary>
        /// Extended CPU metrics
        /// </summary>
        public const string ExtendedCPUPostFix = "CPUExtended";

        /// <summary>
        /// ObjectRef tracing data is stored in external file with this postfix.
        /// </summary>
        public const string HandlePostFix = "Handle";


        /// <summary>
        /// Stacks for ObjectRef tracing are stored in external file with this postfix.
        /// </summary>
        public const string HandleStackPostFix = "HandleStacks";

        /// <summary>
        /// Shared Json Serializer
        /// </summary>
        static volatile JsonSerializer mySerializer;

        static JsonSerializer Serializer
        {
            get
            {
                if (mySerializer == null)
                {
                    var serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });

                    // Needed to deserialize Version objects https://stackoverflow.com/questions/13170386/why-system-version-in-json-string-does-not-deserialize-correctly
                    serializer.Converters.Add(new VersionConverter());
                    mySerializer = serializer;
                }

                return mySerializer;
            }
        }

        public string ExtractMainFileName
        {
            get;private set;
        }

        /// <summary>
        /// Name of input/output file
        /// </summary>
        /// <param name="extractMainFileName">Input/output main json file, or compressed json <see cref="TestRun.CompressedExtractExtension"/> which contains all extracted json files in the archive.</param>
        public ExtractSerializer(string extractMainFileName)
        {
            ExtractMainFileName = extractMainFileName;
            SetSerializerModeOnExtension(ExtractMainFileName);
        }


        /// <summary>
        /// Open file read only.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        static internal FileStream OpenFileReadOnly(string filename)
        {
            return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        internal Stream GetOutputStreamFor(string outputFile, string type, List<string> files)
        {
            string newFileName = GetFileNameFor(outputFile, type);
            Logger.Info($"Created output file name {newFileName} for output file {outputFile} {(Compressed ? "in file " + ExtractMainFileName : "")}");

            Stream lret = null;
            if( Compressed )
            {
                lret = new ReusableMemoryStream();
                myCompressStreams.Add(Path.GetFileName(newFileName), lret);
            }
            else
            {
                lret = File.Create(newFileName);
                files.Add(newFileName);
            }

            return lret;
        }

        static internal string[] GetDerivedFileNameParts()
        {
            string[] all = new string[] { FileIOPostFix, ModulesPostFix, ExtendedCPUPostFix, HandlePostFix, HandleStackPostFix };
            return all.Select(x => "_Derived_" + x).ToArray();
        }

        internal string GetFileNameFor(string outputFile, string derivedName)
        {
            string fileNameNoExt = Path.GetFileNameWithoutExtension(outputFile);
            string dir = Path.GetDirectoryName(outputFile);

            string extension = derivedName == null ? TestRun.ExtractExtension : $"_Derived_{derivedName}" + TestRun.ExtractExtension;
            string newfileName = Path.Combine(dir, fileNameNoExt + extension);
            return newfileName;
        }

        internal T Deserialize<T>(string derivedName) where T:class
        {
            string fileName = GetFileNameFor(ExtractMainFileName, derivedName);
            T lret = null;

            if (Compressed)
            {
                using (var extractor = new SevenZipExtractor(ExtractMainFileName))
                {
                    string fileNoPath = Path.GetFileName(fileName);
                    if (extractor.ArchiveFileNames.Contains(fileNoPath))
                    {
                        var memoryStream = new MemoryStream();
                        extractor.ExtractFile(fileNoPath, memoryStream);
                        memoryStream.Position = 0;
                        lret = Deserialize<T>(memoryStream);
                    }
                }
            }
            else
            {
                if (File.Exists(fileName))
                {
                    using var fileStream = ExtractSerializer.OpenFileReadOnly(fileName);
                    lret = ExtractSerializer.Deserialize<T>(fileStream);
                }
            }

            if( lret is ETWExtract extract)
            {
                extract.DeserializedFileName = ExtractMainFileName;
            }


            return lret;
        }


        bool Compressed
        {
            get;set;
        }

        Dictionary<string, Stream> myCompressStreams = null;

        public List<string> Serialize(ETWExtract extract)
        {
            List<string> outputFiles = new();

            SevenZipCompressor compressor = null;
            

            if( Compressed)
            {
                compressor = new SevenZipCompressor();
                compressor.ArchiveFormat = OutArchiveFormat.SevenZip;
                myCompressStreams = new();

                outputFiles.Add(ExtractMainFileName);
            }

            // overwrite any previous result in case we use a new extractor with changed functionality
            {
                if (extract.FileIO != null)
                {
                    using var fileIOStream = GetOutputStreamFor(ExtractMainFileName, FileIOPostFix, outputFiles);
                    Serialize<FileIOData>(fileIOStream, extract.FileIO);
                    extract.FileIO = null;
                }
                if (extract.Modules != null)
                {
                    using var moduleStream = GetOutputStreamFor(ExtractMainFileName, ModulesPostFix, outputFiles);
                    Serialize<ModuleContainer>(moduleStream, extract.Modules);
                    extract.Modules = null;
                }

                // write extended CPU file only if we have data inside it or we have E-Core data
                if (extract?.CPU?.ExtendedCPUMetrics != null && (extract.CPU.ExtendedCPUMetrics.HasFrequencyData || extract.CPU.ExtendedCPUMetrics.MethodData.Count > 0))
                {
                    using var frequencyStream = GetOutputStreamFor(ExtractMainFileName, ExtendedCPUPostFix, outputFiles);
                    Serialize<CPUExtended>(frequencyStream, extract.CPU.ExtendedCPUMetrics);
                }

                if (extract?.CPU?.ExtendedCPUMetrics != null)
                {
                    // remove remanents from json which will never be read anyway because the explicit interface will read another file
                    extract.CPU.ExtendedCPUMetrics = null;
                }

                if (extract?.HandleData != null && extract.HandleData.ObjectReferences.Count > 0)
                {
                    StackCollection stacks = extract.HandleData.Stacks;
                    extract.HandleData.Stacks = null;

                    using var handleStream = GetOutputStreamFor(ExtractMainFileName, HandlePostFix, outputFiles);
                    Serialize<HandleObjectData>(handleStream, extract.HandleData);
                    extract.HandleData = null;

                    using var handleStackStream = GetOutputStreamFor(ExtractMainFileName, HandleStackPostFix, outputFiles);
                    Serialize<StackCollection>(handleStackStream, stacks);
                }


                // After all externalized data was removed serialize data to main extract file.
                using var mainfileStream = GetOutputStreamFor(ExtractMainFileName, null, outputFiles);
                Serialize<ETWExtract>(mainfileStream, extract);

                if ( Compressed)
                {
                    compressor.CompressStreamDictionary(myCompressStreams, ExtractMainFileName);
                }

                outputFiles.Reverse(); // first file is the main file which is printed to console 
            }

            // Set the Modify DateTime of extract to ETW session start so we can later easily sort tests by file time
            DateTime fileTime = extract.SessionStart.LocalDateTime;
            if (fileTime.Year > 1)
            {
                if (Compressed)
                {
                    File.SetLastWriteTime(ExtractMainFileName, fileTime);
                }
                else
                {
                    foreach (var file in outputFiles)
                    {
                        File.SetLastWriteTime(file, fileTime);
                    }
                }
            }

            return outputFiles;
        }

        private void SetSerializerModeOnExtension(string outputFile)
        {
            string extension = Path.GetExtension(outputFile).ToLowerInvariant();
            Compressed = extension switch
            {
                TestRun.CompressedExtractExtension => true,
                TestRun.ExtractExtension => false,
                _ => throw new NotSupportedException($"The extension {extension} is not supported.")
            };

            if( Compressed )
            {
                // SevenZipSharp uses 7zx64.dll which does not exist. Set dll on our own
                ConfigurationManager.AppSettings["7zLocation"] = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "7z.dll");
            }
        }


        /// <summary>
        /// Get/Set how json indention is done. Default is Indented.
        /// </summary>
        internal static Formatting JsonFormatting
        {
            get; set;
        } = Formatting.None;

        /// <summary>
        /// Serialize data to a stream. This method abstracts away the used serializer which 
        /// we can then use to test the used serializer specifics if we ever want to switch to a faster one.
        /// </summary>
        /// <typeparam name="T">Type to serialize</typeparam>
        /// <param name="stream">Input stream (it is left open)</param>
        /// <param name="value">object to serialize</param>
        internal static void Serialize<T>(Stream stream, T value)
        {
            // We need to keep the input stream open for unit tests
            // which is safe since on dispose it is flushed and we close the stream from SerializeResults already
            StreamWriter writer = null;
            try
            {
                writer = new StreamWriter(stream, new UTF8Encoding(false, true), 0xffff, true);
                using var js = new JsonTextWriter(writer);
                writer = null;
                js.Formatting = JsonFormatting;
                Serializer.Serialize(js, value);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Dispose();
                }
            }

            if (stream is MemoryStream)  // compressor needs to read serialized data again later
            {
                stream.Position = 0;
            }
        }

        /// <summary>
        /// Deserialize an input file for further analyze
        /// </summary>
        /// <param name="inFile"></param>
        internal static ETWExtract DeserializeFile(string inFile)
        {
            try
            {
                ExtractSerializer ser = new(inFile);
                ETWExtract extract = ser.Deserialize<ETWExtract>(null);
                return extract;
            }
            catch (Exception e)
            {
                throw new SerializationException($"Deserialize file: {inFile} failed.", e);
            }
        }

        /// <summary>
        /// Abstract away the used serializer to test serializer specifics if we want to change to a faster one.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inStream"></param>
        /// <returns>Deserialized object.</returns>
        internal static T Deserialize<T>(Stream inStream)
        {
            StreamReader stream = null;
            try
            {
                stream = new StreamReader(inStream);
                using var jsonReader = new JsonTextReader(stream);
                stream = null;

                var deserialized = Serializer.Deserialize<T>(jsonReader);
                return deserialized;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }
    }
}
