using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETWAnalyzer.Extract.Common;
using ETWAnalyzer.Extractors;

namespace ETWAnalyzer.Extract.Handle
{
    /// <summary>
    /// Handle Tracing data.
    /// </summary>
    public class HandleObjectData : IHandleObjectData
    {
        /// <summary>
        /// Contains Object and Handle ETW tracing data
        /// </summary>
        public List<ObjectRefTrace> ObjectReferences { get; set; } = new();

        /// <summary>
        /// Stacks are combined for Handle and Object Reference Tracing.
        /// </summary>
        public StackCollection Stacks { get; set; } = new();

        /// <summary>
        /// Needed to deserialize dependant stack collection file
        /// </summary>
        internal string DeserializedFileName { get; set; }

        IStackCollection IHandleObjectData.Stacks => myStackReader.Value;

        readonly Lazy<StackCollection> myStackReader;

        IReadOnlyList<IObjectRefTrace> IHandleObjectData.ObjectReferences => ObjectReferences;

        /// <summary>
        /// 
        /// </summary>
        public HandleObjectData()
        {
            myStackReader = new Lazy<StackCollection>(ReadHandleStacksFromExternalFile);
        }

        

        StackCollection ReadHandleStacksFromExternalFile()
        {
            StackCollection lret = Stacks;
            if (DeserializedFileName != null)
            {
                ExtractSerializer ser = new();
                string file = ser.GetFileNameFor(DeserializedFileName, ExtractSerializer.HandleStackPostFix);
                if (File.Exists(file))
                {
                    using var fileStream = ExtractSerializer.OpenFileReadOnly(file);
                    lret = ExtractSerializer.Deserialize<StackCollection>(fileStream);
                }
            }

            return lret;
        }
    }
}
