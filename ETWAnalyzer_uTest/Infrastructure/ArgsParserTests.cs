using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest.Infrastructure
{

    class DummyParser : ArgParser
    {
        public DummyParser(string[] args) : base(args)
        { }

        public override string Help => throw new NotImplementedException();

        public override void Parse()
        {
            throw new NotImplementedException();
        }

        public override void Run()
        {
            throw new NotImplementedException();
        }
    }

    public class ArgsParserTests 
    {
        [Fact]
        public void MyFirstTest()
        {
            Assert.True(ArgParser.IsNumberChar('1'));
        }


        [Fact]
        public void DecimalSeparatorIsValid()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.True(ArgParser.IsNumberChar('.'));
        }

        [Fact]
        public void ThousandSeparatorIsValid()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.True(ArgParser.IsNumberChar(','));
        }


        [Fact]
        public void NegativeNumberWorks()
        {
            DummyParser parser = new DummyParser(new string[] { "-200" } );
            Assert.False(parser.PeekNextArgumentSwitch(out string arg));
        }

        [Fact]
        public void SingleMinusIsNotAnArgument()
        {
            DummyParser parser = new DummyParser(new string[] { "-" });
            Assert.False(parser.PeekNextArgumentSwitch(out string arg));
        }

        [Fact]
        public void MinusAndNotNumberIsArgument()
        {
            DummyParser parser = new DummyParser(new string[] { "-a" });
            Assert.True(parser.PeekNextArgumentSwitch(out string arg));
        }

        [Fact]
        public void Can_Parse_Double_In_German_Locale()
        {
            double value = ArgParser.ParseDouble("1.001");
            Assert.Equal(1.001d, value);

            CultureInfo currentculture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                value = ArgParser.ParseDouble("1.001");
                Assert.Equal(1.001d, value);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = currentculture;
            }
        }
    }
}
