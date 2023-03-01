using ETWAnalyzer.EventDump;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static ETWAnalyzer.EventDump.DumpFile;

namespace ETWAnalyzer_uTest.EventDump
{
    public class DumpFileTests
    {
        [Fact]

        public void DumpFileCheck()
        {

            DumpFile dump = new();
            MatchData groupedData = new()
            {
                FileOpenTimeInus = 1,
                FileCloseTimeInus = 200,
                FileWriteTimeInus = 5,
                FileReadTimeInus = 6,
                FileReadSizeInBytes = 8,
                FileReadMaxPos = 32,
                FileWriteSizeInBytes = 64,
                FileWriteMaxFilePos = 10,
                FileOpenCount = 51,
                FileCloseCount = 65,
                FileWriteCount = 56,
                FileReadCount = 4,
                FileSetSecurityCount = 9,
                FileDeleteCount = 3,
                FileRenameCount = 3,
                FileOpenCloseTimeInus = 201,
            };

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.OpenCloseTime;
            decimal totalTime = dump.GetSortValue(groupedData);
            Assert.Equal(201, totalTime);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Length;
            decimal length = dump.GetSortValue(groupedData);
            Assert.Equal(20, length);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Size;
            decimal size = dump.GetSortValue(groupedData);
            Assert.Equal(212, size);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Time;
            decimal time = dump.GetSortValue(groupedData);
            Assert.Equal(212, time);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Count;
            decimal count = dump.GetSortValue(groupedData);
            Assert.Equal(212, size);

            MatchData groupedData01 = new()
            {
                FileOpenTimeInus = 1,
                FileCloseTimeInus = 200,
                FileWriteTimeInus = 5,
                FileReadTimeInus = 10,
                FileReadSizeInBytes = 20,
                FileReadMaxPos = 1000,
                FileWriteSizeInBytes = 100,
                FileWriteMaxFilePos = 500,
                FileOpenCount = 50,
                FileCloseCount = 300,
                FileWriteCount = 600,
                FileReadCount = 400,
                FileSetSecurityCount = 900,
                FileDeleteCount = 3000,
                FileRenameCount = 150,
                FileOpenCloseTimeInus = 201,
            };

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Length;
            decimal length01 = dump.GetSortValue(groupedData01);
            Assert.Equal(1000, length01);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Count;
            decimal count01 = dump.GetSortValue(groupedData01);
            Assert.Equal(2250, count01);

        }
    }
}
