using ETWAnalyzer.Infrastructure;
using ETWAnalyzer_uTest.TestInfrastructure;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace ETWAnalyzer_uTest.Infrastructure
{
    public class MultilineFormatterTests
    {
        private ITestOutputHelper myWriter;
        public MultilineFormatterTests(ITestOutputHelper myWriter)
        {
            this.myWriter = myWriter;
        }

        List<string> Columns { get; set; } = new();

        private void ColumnPrinter(string str, ConsoleColor? color)
        {
            Columns.Add(str);
        }

        [Fact]
        public void CanFormat_ShortHeader()
        {
            MultiLineFormatter formatter = new(
            new ColumnDefinition()
            {
                Enabled = true,
                Title = "1234",
                DataWidth = 5,
            })
            {
                Printer = ColumnPrinter
            };
            Columns.Clear();

            formatter.PrintHeader();

            Assert.Single(Columns);
            Assert.Equal("1234  ", Columns[0]);

            Columns.Clear();
            formatter.Columns[0].Enabled = false;
            formatter.PrintHeader();
            Assert.Empty(Columns);
        }

        [Fact]
        public void CanFormat_EqualHeader()
        {
            MultiLineFormatter formatter = new(
            new ColumnDefinition()
            {
                Enabled = true,
                Title = "12345",
                DataWidth = 5,  // column width includes space
            })
            {
                Printer = ColumnPrinter
            };
            Columns.Clear();

            formatter.PrintHeader();
            Assert.Single(Columns);
            Assert.Equal("12345 ", Columns[0]);

            Columns.Clear();

            formatter.Columns[0].Enabled = false;
            formatter.PrintHeader();
            Assert.Empty(Columns);
        }

        [Fact]
        public void CanFormat_LargerHeader()
        {
            MultiLineFormatter formatter = new(
            new ColumnDefinition()
            {
                Enabled = true,
                Title = "123456",
                DataWidth = 5,
            })
            {
                Printer = ColumnPrinter
            };

            Columns.Clear();
            formatter.PrintHeader();

            Assert.Equal(2, Columns.Count);
            Assert.Equal("12345 ", Columns[0]);
            Assert.Equal("6     ", Columns[1]);
        }

        [Fact]
        public void Can_Format_LargeChainedHeader()
        {
            MultiLineFormatter formatter = new(
                new()
                {
                    Enabled = true,
                    Title = "123456",
                    DataWidth = 5,
                },
                new()
                {
                    Enabled = true,
                    Title = "321",
                    DataWidth = 5,
                },
                new()
                {
                Enabled = true,
                Title = "21",
                DataWidth = 5,
           })
            {
                Printer = ColumnPrinter
            };

            Columns.Clear();
            formatter.PrintHeader();

            Assert.Equal(6, Columns.Count);

            Assert.Equal("12345 ", Columns[0]);
            Assert.Equal("6     ", Columns[3]);
            Assert.Equal("321   ", Columns[1]);
            Assert.Equal("      ", Columns[4]);
            Assert.Equal("21    ", Columns[2]);
            Assert.Equal("      ", Columns[5]);
        }


        [Fact]
        public void Can_Format_LargeChainedHeader_WithDisabled()
        {
            MultiLineFormatter formatter = new(
                new()
                {
                    Enabled = false,
                    Title = "123456",
                    DataWidth = 5,
                },
                new()
                {
                    Enabled = true,
                    Title = "321",
                    DataWidth = 5,
                },
                new()
                {
                    Enabled = true,
                    Title = "21",
                    DataWidth = 5,
                })
            {
                Printer = ColumnPrinter
            };

            Columns.Clear();
            formatter.PrintHeader();

            Assert.Equal(2, Columns.Count);
            Assert.Equal("321   ", Columns[0]);
            Assert.Equal("21    ", Columns[1]);
        }


        [Fact]
        public void Can_FormatChained_Data()
        {
            using var testOutput = new ExceptionalPrinter(myWriter, true);

            MultiLineFormatter formatter = new(
                new()
                {
                    Enabled = true,
                    Title = "Header1",
                    DataWidth = 8,
                },
                new()
                {
                    Enabled = true,
                    Title = "Header2",
                    DataWidth = 9,
                },
                new()
                {
                    Enabled = true,
                    Title = "Header3",
                    DataWidth = 7,
                    Color = ConsoleColor.Red,
                }
             );

            formatter.PrintHeader();
            Console.WriteLine();

            string[] columnData = new string[] { "ColData1", "ColData2", "ColData3" };

            formatter.Print(false, columnData);

            formatter.Printer = ColumnPrinter;
            Columns.Clear();

            /*
 Header1 Header2  Header 
                  3      
 ColData ColData2 ColDat 
       1              a3 
             */

            formatter.PrintHeader();
            Assert.Equal(3, Columns.Count);

            Assert.Equal("Header1  ", Columns[0]);
            Assert.Equal("Header2   ", Columns[1]);
            Assert.Equal("Header3 ", Columns[2]);

            Columns.Clear();
            formatter.Print(false, columnData); ;
            Assert.Equal(6, Columns.Count);

            Assert.Equal("ColData1 ", Columns[0]);
            Assert.Equal("         ", Columns[3]);
            Assert.Equal(" ColData2 ", Columns[1]);
            Assert.Equal("          ", Columns[4]);
            Assert.Equal("ColData ", Columns[2]);
            Assert.Equal("      3 ", Columns[5]);


        }
    }
}
