using ETWAnalyzer.JsonSerializing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extensions
{
    internal static class ObjectCopyExtensions
    {
        public static T GetDeepCopy<T>(this T objSource)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                JsonCreationBase<T> json = new JsonCreationBase<T>();
                json.SerializeToJson(objSource, stream);
                stream.Position = 0;
                T lret =  JsonCreationBase<T>.DeserializeJson(stream);
                return lret;
            }
        }
    }
}
