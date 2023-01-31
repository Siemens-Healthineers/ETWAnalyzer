using ETWAnalyzer.EventDump;
using ETWAnalyzer.Extract;
using ETWAnalyzer_uTest.TestInfrastructure;
using Microsoft.Windows.EventTracing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ETWAnalyzer_uTest.EventDump
{
    public class DumpExceptionTests
    {
        const string Exe1 = "Exe1.exe";
        const string Exe1Arg = "-noarg";
        const int Exe1Pid = 1000;

        const string Baseline = "v1.0";
        const string ExceptionMessage1 = "First";
        const string ExceptionType1 = "InvalidOperationExeption";
        const string Stacktrace1 = "no stack";
        const string TestCase1 = "ExceptionTest";
        const string Process = "Process";


        static char[] myNewLineSplitChars = Environment.NewLine.ToArray();

        private ITestOutputHelper myWriter;
        public DumpExceptionTests(ITestOutputHelper myWriter)
        {
            this.myWriter = myWriter;
        }

        [Fact]
        public void Print_Default()
        {
            using var testOutput = new ExceptionalPrinter(myWriter);

            DumpExceptions printer = new DumpExceptions();

            List<DumpExceptions.MatchData> exceptions = CreateTestData();

            StringWriter writer = new StringWriter();
            Console.SetOut(writer);

            printer.PrintMatches(exceptions);

            testOutput.Add(writer.ToString());

            string[] lines = writer.ToString().Split(myNewLineSplitChars, StringSplitOptions.RemoveEmptyEntries);
            var lines1 = lines[0];

            Assert.Equal(4, lines.Length);
            Assert.Contains(Baseline, lines[0]);
            Assert.Contains(Exe1, lines[1]);
            Assert.Contains(ExceptionType1, lines[2]);
            Assert.Contains(ExceptionMessage1, lines[3]);
        }


        [Fact]
        public void Print_Stacks()
        {
            using var testOutput = new ExceptionalPrinter(myWriter);

            DumpExceptions printer = new DumpExceptions();
            printer.ShowStack = true;
            
            List<DumpExceptions.MatchData> exceptions = CreateTestData();

            StringWriter writer = new StringWriter();
            Console.SetOut(writer);

            printer.PrintMatches(exceptions);

            testOutput.Add(writer.ToString());

            string[] lines = writer.ToString().Split(myNewLineSplitChars, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(5, lines.Length);
            Assert.Contains(Baseline, lines[0]);
            Assert.Contains(Exe1, lines[0]);
            Assert.Contains(Exe1, lines[1]);
            Assert.Contains(ExceptionType1, lines[2]);
            Assert.Contains(ExceptionMessage1, lines[3]);
            Assert.Contains(Stacktrace1, lines[4]);
        }

        [Fact]
        public void Print_ShowTime()
        {
            using var testOutput = new ExceptionalPrinter(myWriter);

            DumpExceptions printer = new DumpExceptions();
            printer.ShowTime = true;

            List<DumpExceptions.MatchData> exceptions = CreateTestData();

            StringWriter writer = new StringWriter();
            Console.SetOut(writer);

            printer.PrintMatches(exceptions);

            testOutput.Add(writer.ToString());

            string[] lines = writer.ToString().Split(myNewLineSplitChars, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(4, lines.Length);
            Assert.Contains(Baseline, lines[0]);
            Assert.Contains(Exe1, lines[1]);
            Assert.Contains(ExceptionType1, lines[2]);
            Assert.Contains(ExceptionMessage1, lines[3]);
        }

        [Fact]
        public void Print_ByTime()
        {
            using var testOutput = new ExceptionalPrinter(myWriter);

            DumpExceptions printer = new DumpExceptions();

            printer.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Time;

            List<DumpExceptions.MatchData> exceptions = CreateTestData();
            //DumpExceptions.MatchData other = exceptions[0].Clone();

            ETWProcess otherprocess = new ETWProcess
            {
                ProcessName = "Other.exe",
                ProcessID = 2000,
            };

            //other.Process = otherprocess;

            //exceptions.Add(other);

            StringWriter writer = new StringWriter();
            Console.SetOut(writer);

            printer.PrintMatches(exceptions);

            testOutput.Add(writer.ToString());

            string[] lines = writer.ToString().Split(myNewLineSplitChars, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(3, lines.Length);
            Assert.Contains(Baseline, lines[0]);
            Assert.Contains(Process, lines[1]);
            Assert.Contains(ExceptionMessage1, lines[2]);
        }


        List<DumpExceptions.MatchData> CreateTestData(int n=1)
        {
            ETWProcess process1 = new ETWProcess
            {
                CmdLine = Exe1Arg,
                ProcessID = Exe1Pid,
                ProcessName = Exe1,
            };

            List<DumpExceptions.MatchData> lret = new List<DumpExceptions.MatchData>()
            {
                new DumpExceptions.MatchData
                {
                    Message = ExceptionMessage1,
                    BaseLine = Baseline,
                    SessionStart = new DateTimeOffset(2000,1,1,1,1,1, TimeSpan.Zero),
                    SourceFile = "C:\\Json1.json",
                    PerformedAt = new DateTime(2000,1,1),
                    Type = ExceptionType1,
                    Stack = Stacktrace1,
                    TestCase = TestCase1,
                    TimeStamp = new DateTimeOffset(2000, 1, 1, 1, 1, 5, TimeSpan.Zero),
                    Process = process1,
        }
            };

            while( lret.Count < n)
            {
                var copy = lret[lret.Count-1].Clone();
                copy.TimeStamp = copy.TimeStamp.AddSeconds(1.0);
                lret.Add(copy);
            }

            return lret;
        }
    }
}
