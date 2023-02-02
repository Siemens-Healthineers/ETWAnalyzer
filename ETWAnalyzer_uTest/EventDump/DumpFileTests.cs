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
                FileOpenTimeInus = 12,
                FileCloseTimeInus = 200,
                FileWriteTimeInus = 5,
                FileReadTimeInus = 6,
                FileReadSizeInBytes = 8,
                FileReadMaxPos = 1024,
                FileWriteSizeInBytes = 64,
                FileWriteMaxFilePos = 256,
                FileOpenCount = 51,
                FileCloseCount = 65,
                FileWriteCount = 56,
                FileReadCount = 4,
                FileSetSecurityCount = 9,
                FileDeleteCount = 3000,
                FileRenameCount = 201,
                FileOpenCloseTimeInus = 201,
            };

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Length;
            decimal length01 = dump.GetSortValue(groupedData01);
            Assert.Equal(512, length01);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Size;
            decimal size01 = dump.GetSortValue(groupedData01);
            Assert.Equal(223, size01);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Time;
            decimal time01 = dump.GetSortValue(groupedData01);
            Assert.Equal(223, time01);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Count;
            decimal count01 = dump.GetSortValue(groupedData01);
            Assert.Equal(185, count01);

        }
    }
}
