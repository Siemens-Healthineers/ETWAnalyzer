using ETWAnalyzer.Extractors;
using Microsoft.Windows.EventTracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest.Extractors
{
    public class ExtractSerializerTests
    {
        [Fact]
        public void Can_Match_ExpectedFiles_FromArchive()
        {
            string rootFileName = "Test.json";
            ExtractSerializer ser = new(rootFileName);
            string extended = ser.GetFileNameFor(ExtractSerializer.ExtendedCPUPostFix);

            List<string> zipFileNames = new List<string> { rootFileName, extended };

            Assert.Equal(rootFileName, ExtractSerializer.MatchArchiveFileName(zipFileNames.AsReadOnly(), rootFileName));
            Assert.Equal(extended, ExtractSerializer.MatchArchiveFileName(zipFileNames.AsReadOnly(), extended));
        }


        [Fact]
        public void Can_Match_RenamedArchive_FromArchive()
        {
            string rootFileName = "Test.json";
            ExtractSerializer ser = new(rootFileName);
            string extended = ser.GetFileNameFor(ExtractSerializer.ExtendedCPUPostFix);

            List<string> zipFileNames = new List<string> { rootFileName, extended };

            string renamedRoot = rootFileName.Replace("Test", "NewName");
            string renamedExtended = extended.Replace("Test", "NewName");


            Assert.Equal(rootFileName, ExtractSerializer.MatchArchiveFileName(zipFileNames.AsReadOnly(), renamedRoot));
            Assert.Equal(extended, ExtractSerializer.MatchArchiveFileName(zipFileNames.AsReadOnly(), renamedExtended));
        }


        [Fact]
        public void Can_Match_RenamedArchive_WithAdditionalFile()
        {
            string rootFileName = "Test.json";
            string additionalFile = "Other.json";
            ExtractSerializer ser = new(rootFileName);
            string extended = ser.GetFileNameFor(ExtractSerializer.ExtendedCPUPostFix);

            List<string> zipFileNames = new List<string> { additionalFile, rootFileName, extended };

            string renamedRoot = rootFileName.Replace("Test", "NewName");
            string renamedExtended = extended.Replace("Test", "NewName");


            Assert.Equal(rootFileName, ExtractSerializer.MatchArchiveFileName(zipFileNames.AsReadOnly(), renamedRoot));
            Assert.Equal(extended, ExtractSerializer.MatchArchiveFileName(zipFileNames.AsReadOnly(), renamedExtended));

        }


    }
}
