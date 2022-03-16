//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ETWAnalyzer.JsonSerializing
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonCreationBase<T>
    {
        /// <summary>
        /// Deserialize JsonIndex-File for comparing
        /// </summary>
        /// <param name="inStream">Open stream to th JsonIndex-File</param>
        /// <returns>TestDataFile - Objects</returns>
        internal HashSet<T> DeserializeJsonToHashset(Stream inStream)
        {
            StreamReader stream = null;
            try
            {
                stream = new StreamReader(inStream);
                using var jsonReader = new JsonTextReader(stream);
                stream = null;
                var deserialized = JsonSerializer.Create().Deserialize<HashSet<T>>(jsonReader);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inStream"></param>
        /// <returns></returns>
        static internal T DeserializeJson(Stream inStream)
        {
            StreamReader stream = null;
            try
            {
                stream = new StreamReader(inStream);
                using var jsonReader = new JsonTextReader(stream);
                stream = null;
                var deserialized = JsonSerializer.Create().Deserialize<T>(jsonReader);
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
        /// <summary>
        /// Serialize the updated Hashset which contains all extracted Files
        /// </summary>
        /// <param name="toSerialize"></param>
        /// <param name="stream">Stream to JsonFiles - Folder</param>
        internal void SerializeToJson(List<T> toSerialize, Stream stream)
        {
            StreamWriter writer = null;
            try
            {
                writer = new StreamWriter(stream, new UTF8Encoding(false, true), 0xffff, true);
                using var js = new JsonTextWriter(writer);
                writer = null;

                js.Formatting = Formatting.Indented;
                JsonSerializer.Create().Serialize(js, toSerialize);
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
        /// 
        /// </summary>
        /// <param name="toSerialize"></param>
        /// <param name="stream"></param>
        internal void SerializeToJson(T toSerialize, Stream stream)
        {
            StreamWriter writer = null;
            try
            {
                writer = new StreamWriter(stream, new UTF8Encoding(false, true), 0xffff, true);
                using var js = new JsonTextWriter(writer);
                writer = null;

                js.Formatting = Formatting.Indented;
                JsonSerializer.Create().Serialize(js, toSerialize);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Dispose();
                }
            }
        }
    }
}
