//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Extract;
using Xunit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETWAnalyzer.Extractors;

namespace ETWAnalyzer_uTest
{

    
    public class ExtractSingleFileTests
    {
        /// <summary>
        /// We rely in <see cref="ETWExtract"/>, <see cref="ExceptionStats"/> on private fields which must not show up in the serialized
        /// output to keep the output file size small.
        /// </summary>
        [Fact]
        public void Serializer_Does_Not_Serialize_Private_Or_Internal_Fields()
        {
            SerializerTestClass test = new SerializerTestClass();
            test.SetPrivate("Private String", "Internal String");
            test.Str1 = "String 1";
            test.PublicNonSerializedProperty = "public";
            test.EncapsulatedProperty = "Enc";

            var memory = new MemoryStream();
            ExtractSerializer.Serialize<SerializerTestClass>(memory, test);
            memory.Position = 0;
            SerializerTestClass deserialized = ExtractSerializer.Deserialize<SerializerTestClass>(memory);

            Assert.Null( deserialized.myInternalString);
            Assert.Null(deserialized.GetPrivateString());
            Assert.Null(deserialized.PublicNonSerializedProperty);
        }

        /// <summary>
        /// We rely in <see cref="ExceptionStats"/> that the serializer sets the data via the public property. That allows us
        /// during property set to set at runtime fields in the properties to connect the enclosing class with the inner ones
        /// to translate process ids and time.
        /// </summary>
        [Fact]
        public void Serializer_Calls_Set_Property_On_Deserialize()
        {
            SerializerTestClass test = new SerializerTestClass()
            {
                EncapsulatedProperty = "someNonNullValue"
            };
            var memory = new MemoryStream();
            ExtractSerializer.Serialize<SerializerTestClass>(memory, test);
            memory.Position = 0;
            SerializerTestClass deserialized = ExtractSerializer.Deserialize<SerializerTestClass>(memory);

            Assert.True(deserialized.GetSideEffect());
        }
    }

    /// <summary>
    /// Check how serializer writes data.
    /// </summary>
    public class SerializerTestClass
    {
        public string Str1 { get; set; }
        private string myPrivateString;
        internal string myInternalString;

        string myEncapsulatedProperty;
        bool mySideEffect;

        public bool GetSideEffect()
        {
            return mySideEffect;
        }


        /// <summary>
        /// Used to test if serializer simply writes all private data or if it uses the property get/set methods
        /// to set the data. 
        /// </summary>
        public string EncapsulatedProperty
        {
            get
            {
                return myEncapsulatedProperty;
            }
            set
            {
                mySideEffect = true;
                myEncapsulatedProperty = value;
            }
        }

        [JsonIgnore]
        public string PublicNonSerializedProperty
        {
            get;set;
        }

        public void SetPrivate(string privateString, string internalString)
        {
            myPrivateString = privateString;
            myInternalString = internalString;
        }

        internal object GetPrivateString()
        {
            return myPrivateString;
        }
    }
}
