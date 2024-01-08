//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.CPU;
using ETWAnalyzer.Extract.CPU.Extended;
using ETWAnalyzer.Extract.FileIO;
using ETWAnalyzer.Extract.Modules;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
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

        public ExtractSerializer()
        {
        }


        internal Stream GetOutputStreamFor(string outputFile, string type, List<string> files)
        {
            string newFileName = GetFileNameFor(outputFile, type);
            files.Add(newFileName);
            Logger.Info($"Created output file name {newFileName} for output file {outputFile}");
            return File.Create(newFileName);
        }

        static internal string[] GetDerivedFileNameParts()
        {
            string[] all = new string[] { FileIOPostFix, ModulesPostFix, ExtendedCPUPostFix };
            return all.Select(x => "_Derived_" + x).ToArray();
        }

        internal string GetFileNameFor(string outputFile, string type)
        {
            string fileNameNoExt = Path.GetFileNameWithoutExtension(outputFile);
            string ext = Path.GetExtension(outputFile);
            string dir = Path.GetDirectoryName(outputFile);

            string extension = type == null ? ext : $"_Derived_{type}" + ext;
            string newfileName = Path.Combine(dir, fileNameNoExt + extension);
            return newfileName;
        }

        public List<string> Serialize(string outputFile, ETWExtract extract)
        {
            List<string> outputFiles = new();

            // overwrite any previous result in case we use a new extractor with changed functionality
            { 
                if (extract.FileIO != null)
                {
                    using var fileIOStream = GetOutputStreamFor(outputFile, FileIOPostFix, outputFiles);
                    Serialize<FileIOData>(fileIOStream, extract.FileIO);
                    extract.FileIO = null;
                }
                if (extract.Modules != null)
                {
                    using var moduleStream = GetOutputStreamFor(outputFile, ModulesPostFix, outputFiles);
                    Serialize<ModuleContainer>(moduleStream, extract.Modules);
                    extract.Modules = null;
                }

                // write extended CPU file only if we have data inside it
                if( extract?.CPU?.ExtendedCPUMetrics != null && ( extract.CPU.ExtendedCPUMetrics.CPUToFrequencyDurations.Count > 0  || extract.CPU.ExtendedCPUMetrics.MethodData.Count > 0) )
                {
                    using var frequencyStream = GetOutputStreamFor(outputFile, ExtendedCPUPostFix, outputFiles);
                    Serialize<CPUExtended>(frequencyStream, extract.CPU.ExtendedCPUMetrics);
                }

                if( extract?.CPU?.ExtendedCPUMetrics != null )
                {
                    // remove remanents from json which will never be read anyway because the explicit interface will read another file
                    extract.CPU.ExtendedCPUMetrics = null; 
                }

                // After all externalized data was removed serialize data to main extract file.

                using var mainfileStream = GetOutputStreamFor(outputFile, null, outputFiles);
                Serialize<ETWExtract>(mainfileStream, extract);

                outputFiles.Reverse(); // first file is the main file which is printed to console 
            }

            // Set the Modify DateTime of extract to ETW session start so we can later easily sort tests by file time
            DateTime fileTime = extract.SessionStart.LocalDateTime;
            if (fileTime.Year > 1 )
            {
                foreach (var file in outputFiles)
                {
                    File.SetLastWriteTime(file, fileTime);
                }
            }

            return outputFiles;
        }

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
                js.Formatting = Formatting.Indented;
                Serializer.Serialize(js, value);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Dispose();
                }
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
                using var fileStream = ETWExtract.OpenFileReadOnly(inFile);
                ETWExtract extract =  Deserialize<ETWExtract>(fileStream);
                extract.DeserializedFileName = inFile;
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
