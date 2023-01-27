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
            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.OpenCloseTime;
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
            decimal totalTime = dump.GetSortValue(groupedData);
            Assert.Equal(201, totalTime);

        }
    }
}
