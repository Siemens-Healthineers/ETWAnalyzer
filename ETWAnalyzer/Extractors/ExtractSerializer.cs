//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Common;
using ETWAnalyzer.Extract.CPU.Extended;
using ETWAnalyzer.Extract.FileIO;
using ETWAnalyzer.Extract.Handle;
using ETWAnalyzer.Extract.Modules;
using ETWAnalyzer.Extract.TraceLogging;
using ETWAnalyzer.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SevenZip;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ETWAnalyzer.Extractors
{

    /// <summary>
    /// Serializes the ETWExtract object into multiple json files or one compressed <see cref="TestRun.CompressedExtractExtension"/> file.
    /// Deserialization is also handled by this class which supplies utility methods to load other Derived files on access when needed.
    /// </summary>
    class ExtractSerializer
    {
        const string DerivedFilePart = "_Derived_";

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
        /// TraceLogging data is stored in external file with this postfix.
        /// </summary>
        public const string TraceLoggingPostFix = "TraceLogging";


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

        /// <summary>
        /// True when input file has file extension <see cref="TestRun.CompressedExtractExtension"/>, false otherwise
        /// </summary>
        bool Compressed
        {
            get; set;
        }

        /// <summary>
        /// Needed during compression to store a list of files with streams in 7z archive when compression is used.
        /// </summary>
        Dictionary<string, Stream> myCompressStreams = null;

        /// <summary>
        /// Full path to compressed or main input Json file.
        /// </summary>
        public string ExtractMainFileName
        {
            get;private set;
        }


        /// <summary>
        /// Get/Set how json indention is done. Default is Indented.
        /// </summary>
        internal static Formatting JsonFormatting
        {
            get; set;
        } = Formatting.None;

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
                    using var fileIOStream = GetOutputStreamFor(FileIOPostFix, outputFiles);
                    Serialize<FileIOData>(fileIOStream, extract.FileIO);
                    extract.FileIO = null;
                }
                if (extract.Modules != null)
                {
                    using var moduleStream = GetOutputStreamFor(ModulesPostFix, outputFiles);
                    Serialize<ModuleContainer>(moduleStream, extract.Modules);
                    extract.Modules = null;
                }

                // write extended CPU file only if we have data inside it or we have E-Core data
                if (extract?.CPU?.ExtendedCPUMetrics != null && (extract.CPU.ExtendedCPUMetrics.HasFrequencyData || extract.CPU.ExtendedCPUMetrics.MethodData.Count > 0))
                {
                    using var frequencyStream = GetOutputStreamFor(ExtendedCPUPostFix, outputFiles);
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

                    using var handleStream = GetOutputStreamFor(HandlePostFix, outputFiles);
                    Serialize<HandleObjectData>(handleStream, extract.HandleData);
                    extract.HandleData = null;

                    using var handleStackStream = GetOutputStreamFor(HandleStackPostFix, outputFiles);
                    Serialize<StackCollection>(handleStackStream, stacks);
                }

                if( extract?.TraceLogging != null && extract.TraceLogging.EventsByProvider.Count > 0 )
                {
                    using var traceLoggingStream = GetOutputStreamFor(TraceLoggingPostFix, outputFiles);
                    Serialize<TraceLoggingEventData>(traceLoggingStream, extract.TraceLogging);
                    extract.TraceLogging = null;  // do not serialize data twice into different files
                }

                // After all externalized data was removed serialize data to main extract file.
                using var mainfileStream = GetOutputStreamFor(null, outputFiles);
                Serialize<ETWExtract>(mainfileStream, extract);

                if ( Compressed)
                {
                    compressor.CompressStreamDictionary(myCompressStreams, ExtractMainFileName);  // write 7z file with our serialized Json data
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

        /// <summary>
        /// Open file read only.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        static internal FileStream OpenFileReadOnly(string filename)
        {
            return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        internal Stream GetOutputStreamFor(string type, List<string> files)
        {
            string newFileName = GetFileNameFor(type);
            Logger.Info($"Created output file name {newFileName} for output file {ExtractMainFileName} {(Compressed ? "in file " + ExtractMainFileName : "")}");

            Stream lret = null;
            if (Compressed)
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
            return all.Select(x => DerivedFilePart + x).ToArray();
        }

        internal string GetFileNameFor(string derivedName)
        {
            string fileNameNoExt = Path.GetFileNameWithoutExtension(ExtractMainFileName);
            string dir = Path.GetDirectoryName(ExtractMainFileName);

            string extension = derivedName == null ? TestRun.ExtractExtension : $"{DerivedFilePart}{derivedName}" + TestRun.ExtractExtension;
            string newfileName = Path.Combine(dir, fileNameNoExt + extension);
            return newfileName;
        }

        internal T Deserialize<T>(string derivedName) where T : class
        {
            string fileName = GetFileNameFor(derivedName);
            T lret = null;

            if (Compressed)
            {
                using (var extractor = new SevenZipExtractor(ExtractMainFileName))
                {
                    string fileNoPath = Path.GetFileName(fileName);
                    fileNoPath = MatchArchiveFileName(extractor.ArchiveFileNames, fileNoPath);
                    if (fileNoPath != null)
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

            if (lret is ETWExtract extract) // sub serializers need to get the input file name to deserialize derived files.
            {
                extract.DeserializedFileName = ExtractMainFileName;
            }


            return lret;
        }


        /// <summary>
        /// Support also renamed .json7z files without the need to match the compressed file names of the outer file. 
        /// </summary>
        /// <param name="archiveFileNames"></param>
        /// <param name="fileNoPath"></param>
        /// <returns>Matching file name inside 7z archive or null if none could be found.</returns>
        internal static string MatchArchiveFileName(ReadOnlyCollection<string> archiveFileNames, string fileNoPath)
        {
            string lret = null;
            if( archiveFileNames.Contains(fileNoPath) )
            {
                lret = fileNoPath;  // should be default if archive file was not renamed
            }
            else
            {
                string[] fileEndings = GetDerivedFileNameParts();

                // .json7z file might have been renamed. Cope with it
                if ( fileNoPath.Contains(DerivedFilePart))  // we need to look up a _Derived_Modules/FileIO.... file
                {
                    string end = fileEndings.Where(x =>  Path.GetFileNameWithoutExtension(fileNoPath).EndsWith(x)).FirstOrDefault();
                    if( end != null )
                    {
                        lret = archiveFileNames.Where(x => Path.GetFileNameWithoutExtension(x).EndsWith(end)).FirstOrDefault();
                    }
                }
                else  // we need to match any files which has not _Dervied_ in its name
                {
                    string derived = archiveFileNames.Where(x => x.Contains(DerivedFilePart)).FirstOrDefault();
                    if (derived != null) // use as root file any derived file because we also want to support additional files in the archive 
                    {
                        string fileNoExt = Path.GetFileNameWithoutExtension(derived);
                        lret = fileNoExt.Substring(0, fileNoExt.IndexOf(DerivedFilePart)) + Path.GetExtension(derived);
                    }
                    else // use the first (possibly only file)
                    {
                        lret = archiveFileNames.Where(x => !x.Contains(DerivedFilePart)).FirstOrDefault();
                    }
                }
            }
            return lret;
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

            if (stream is MemoryStream)  // compressor needs to read serialized data again later which is the reason why we need ReusableMemoryStream.
            {
                stream.Position = 0;
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
