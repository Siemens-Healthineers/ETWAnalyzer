using ETWAnalyzer.EventDump;
using Microsoft.Windows.EventTracing.Disk;
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
        DumpFile dump = new();
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

        [Fact]

        public void DumpFile_Count()
        {
            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Count;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Invalid;
            // You need to set -FileOperation to sort by a specific row. Possible values are Enum.GetNames(typeof(FileIOoper...=
            var values = Enum.GetNames<FileOperation>().Where(x => x!= "Invalid").ToArray();
            string valuesList = String.Join(", ", values);
            var ex = Assert.Throws<ArgumentException>( () => dump.GetSortValue(groupedData01));
            Assert.Equal("You need to set -FileOperationValue to sort by a specific row. Possible values are Close, Open, Read, Write, SetSecurity, Delete, Rename.", ex.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Count;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Delete;
            decimal countDelete = dump.GetSortValue(groupedData01);
            Assert.Equal(3000, countDelete);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Count;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Close;
            decimal countClose = dump.GetSortValue(groupedData01);
            Assert.Equal(300, countClose);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Count;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Open;
            decimal countOpen = dump.GetSortValue(groupedData01);
            Assert.Equal(50, countOpen);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Count;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Read;
            decimal countRead = dump.GetSortValue(groupedData01);
            Assert.Equal(400, countRead);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Count;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Write;
            decimal countWrite = dump.GetSortValue(groupedData01);
            Assert.Equal(600, countWrite);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Count;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.SetSecurity;
            decimal countSetSecurity = dump.GetSortValue(groupedData01);
            Assert.Equal(900, countSetSecurity);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Count;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Rename;
            decimal countRename = dump.GetSortValue(groupedData01);
            Assert.Equal(150, countRename);

        }

        [Fact]
        public void DumpFile_Size()
        {
            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Size;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Invalid;
            // You need to set -FileOperation to sort by a specific row. Possible values are Enum.GetNames(typeof(FileIOoper...=
            var values = Enum.GetNames<FileOperation>().Where(x => x != "Invalid").ToArray();
            String.Join(",", values);
            var exInvlaid = Assert.Throws<ArgumentException>( () => dump.GetSortValue(groupedData01));
            Assert.Equal($"You need to set -FileOperationValue to sort by a specific row. Possible values are Read, Write.", exInvlaid.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Size;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Delete;
            var exDelete = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal($"You need to set -FileOperationValue to sort by a specific row. Possible values are Read, Write.", exDelete.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Size;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Close;
            var exClose = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal($"You need to set -FileOperationValue to sort by a specific row. Possible values are Read, Write.", exClose.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Size;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Open;
            var exOpen = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal($"You need to set -FileOperationValue to sort by a specific row. Possible values are Read, Write.", exOpen.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Size;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Read;
            decimal sizeRead = dump.GetSortValue(groupedData01);
            Assert.Equal(20, sizeRead);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Size;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Write;
            decimal sizeWrite = dump.GetSortValue(groupedData01);
            Assert.Equal(100, sizeWrite);

            //to be checked in dumpfile
            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Size;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.SetSecurity;
            var exSecurity = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal($"You need to set -FileOperationValue to sort by a specific row. Possible values are Read, Write.", exSecurity.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Size;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Rename;
            var exRename = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal($"You need to set -FileOperationValue to sort by a specific row. Possible values are Read, Write.", exRename.Message);
        }

        [Fact]
        public void DumpFile_Time()
        {
            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Time;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Invalid;
            // You need to set -FileOperation to sort by a specific row. Possible values are Enum.GetNames(typeof(FileIOoper...=
            var values = Enum.GetNames<FileOperation>().Where(x => x != "Invalid").ToArray();
            String.Join(",", values);
            var exInvalid = Assert.Throws<ArgumentException>( () => dump.GetSortValue(groupedData01));
            Assert.Equal("You need to set -FileOperationValue to sort by a specific row. Possible values are Read, Write, Open, Close.", exInvalid.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Time;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Delete;
            var exDelete = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal("You need to set -FileOperationValue to sort by a specific row. Possible values are Read, Write, Open, Close.", exDelete.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Time;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Close;
            decimal timeClose = dump.GetSortValue(groupedData01);
            Assert.Equal(200, timeClose);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Time;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Open;
            decimal timeOpen = dump.GetSortValue(groupedData01);
            Assert.Equal(1, timeOpen);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Time;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Read;
            decimal timeRead = dump.GetSortValue(groupedData01);
            Assert.Equal(10, timeRead);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Time;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Write;
            decimal timeWrite = dump.GetSortValue(groupedData01);
            Assert.Equal(5, timeWrite);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Time;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.SetSecurity;
            var exSecurity = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal("You need to set -FileOperationValue to sort by a specific row. Possible values are Read, Write, Open, Close.", exSecurity.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Time;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Rename;
            var exRename = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal("You need to set -FileOperationValue to sort by a specific row. Possible values are Read, Write, Open, Close.", exRename.Message);
        }

        [Fact]
        public void DumpFile_Length()
        {
            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Length;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Read;
            decimal lengthRead = dump.GetSortValue(groupedData01);
            Assert.Equal(1000, lengthRead);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Length;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Write;
            decimal lengthWrite = dump.GetSortValue(groupedData01);
            Assert.Equal(500, lengthWrite);

            // Exception tests
            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Length;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.SetSecurity;
            var exSecurity = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal("You need to set -FileOperationValue to sort by a specific row. Possible values are Read, Write.", exSecurity.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Length;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Close;
            var exClose = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal("You need to set -FileOperationValue to sort by a specific row. Possible values are Read, Write.", exClose.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Length;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Open;
            var exOpen = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal("You need to set -FileOperationValue to sort by a specific row. Possible values are Read, Write.", exOpen.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Length;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Delete;
            var exDelete = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal("You need to set -FileOperationValue to sort by a specific row. Possible values are Read, Write.", exDelete.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Length;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Rename;
            var exRename = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal("You need to set -FileOperationValue to sort by a specific row. Possible values are Read, Write.", exRename.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.Length;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Invalid;
            var exInvalid = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal("You need to set -FileOperationValue to sort by a specific row. Possible values are Read, Write.", exInvalid.Message);
        }

        [Fact]
        public void DumpFile_Check()
        {
            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.TotalSize;
            decimal totalSize = dump.GetSortValue(groupedData01);
            Assert.Equal(120, totalSize);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.OpenCloseTime;
            decimal openCloseTime = dump.GetSortValue(groupedData01);
            Assert.Equal(201, openCloseTime);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.TotalTime;
            decimal totalFileTime = dump.GetSortValue(groupedData01);
            Assert.Equal(216, totalFileTime);

            //tests with exceptions for SortOrders
            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.ReadSize;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Open;
            var exRead = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal("The -FileOperationValue is not valid. Allowed value must be Read only.", exRead.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.WriteSize;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Open;
            var exWrite = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal("The -FileOperationValue is not valid. Allowed value must be Write only.", exWrite.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.TotalSize;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Open;
            var exTotalSize = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal("The -FileOperationValue is not valid. Allowed value must be Invalid only.", exTotalSize.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.TotalTime;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Open;
            var exTotalTime = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal("The -FileOperationValue is not valid. Allowed value must be Invalid only.", exTotalTime.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.ReadTime;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Open;
            var exReadTime = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal("The -FileOperationValue is not valid. Allowed value must be Read only.", exReadTime.Message);

            dump.SortOrder = ETWAnalyzer.Commands.DumpCommand.SortOrders.WriteTime;
            dump.FileOperationValue = ETWAnalyzer.Extract.FileIO.FileIOStatistics.FileOperation.Open;
            var exWriteTime = Assert.Throws<ArgumentException>(() => dump.GetSortValue(groupedData01));
            Assert.Equal("The -FileOperationValue is not valid. Allowed value must be Write only.", exWriteTime.Message);
        }

    }
}
