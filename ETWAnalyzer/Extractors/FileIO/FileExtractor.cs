//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers.Infrastructure;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.FileIO;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Disk;
using Microsoft.Windows.EventTracing.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extractors.FileIO
{
    class FileExtractor : ExtractorBase
    {
        IPendingResult<IFileActivityDataSource> myFileIO;

        public FileExtractor()
        {
        }

        public override void RegisterParsers(ITraceProcessor processor)
        {
            myFileIO = processor.UseFileIOData();
        }


        bool HasProcess(IFileActivity activty)
        {
            return activty.IssuingProcess != null;
        }

        long GetDurationInus(TraceTimestamp start, TraceTimestamp stop)
        {
            return Math.Max(0, (long)Math.Round((stop - start).TotalMicroseconds, MidpointRounding.AwayFromZero));
        }

        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            using var logger = new PerfLogger("Extract File");

            if (!myFileIO.HasResult)
            {
                string msg = "Warning: No FileIO activity was recorded. Skipping FileIO Activity extraction.";
                Console.WriteLine(msg);
                Logger.Warn(msg);
                return;
            }

            FileIOData data = new();

            foreach (IWriteFileActivity fileActivity in myFileIO.Result.WriteFileActivity.Where(HasProcess))
            {
                var fileIO = new FileIOStatistics
                {
                    Write =
                        new FileOffsetOperation
                        {
                            MaxFilePosition = fileActivity.Offset.Value + fileActivity.ActualSize.Bytes,
                            Count = 1,
                            AccessedBytes = fileActivity.RequestedSize.Bytes,
                            Durationus = GetDurationInus(fileActivity.StartTime, fileActivity.StopTime),
                        }
                };
                data.Add(results, fileActivity.IssuingProcess.Id, fileActivity.IssuingProcess.CreateTime.ConvertToTime(), fileActivity.Path, fileIO);
            }


            foreach(IReadFileActivity readFileActivity in myFileIO.Result.ReadFileActivity.Where(HasProcess))
            {
                var fileIO = new FileIOStatistics
                {
                    Read =
                        new FileOffsetOperation
                        {
                            MaxFilePosition = readFileActivity.Offset.Value + readFileActivity.ActualSize.Bytes,
                            Count = 1,
                            AccessedBytes = readFileActivity.RequestedSize.Bytes,
                            Durationus = GetDurationInus(readFileActivity.StartTime, readFileActivity.StopTime)
                        }
                };

                data.Add(results, readFileActivity.IssuingProcess.Id, readFileActivity.IssuingProcess.CreateTime.ConvertToTime(), readFileActivity.Path, fileIO);

            }

            foreach (ICreateFileObjectActivity open in myFileIO.Result.CreateFileObjectActivity.Where(HasProcess))
            {
                var fileOpen = new FileIOStatistics
                {
                    Open = 
                        new FileOpenOperation
                        {
                            Durationus = GetDurationInus(open.StartTime, open.StopTime),
                            Count = 1,
                        }
                };

                fileOpen.Open.AddUniqueNotSucceededNtStatus(open.ErrorCode);
                data.Add(results, open.IssuingProcess.Id, open.IssuingProcess.CreateTime.ConvertToTime(), open.Path, fileOpen);
            }


            foreach(ICloseFileActivity close in myFileIO.Result.CloseFileActivity.Where(HasProcess))
            {
                var fileIO = new FileIOStatistics
                {
                    Close =
                    new FileCloseOperation
                    {
                        Durationus = GetDurationInus(close.StartTime, close.StopTime),
                        Count = 1
                    }
                };

                data.Add(results, close.IssuingProcess.Id, close.IssuingProcess.CreateTime.ConvertToTime(), close.Path, fileIO);
            }

            // Cleanup is usually called togehter during Close or file deletion which is usually the more expensive operation
            // Include that in Close duration as well
            foreach(ICleanupFileActivity cleanup in myFileIO.Result.CleanupFileActivity.Where(HasProcess))
            {
                var fileIO = new FileIOStatistics
                {
                    Close =
                    new FileCloseOperation
                    {
                        Durationus = GetDurationInus(cleanup.StartTime, cleanup.StopTime),
                        Cleanups = 1
                    }
                };

                data.Add(results, cleanup.IssuingProcess.Id, cleanup.IssuingProcess.CreateTime.ConvertToTime(), cleanup.Path, fileIO);
            }

            foreach(ISetFileSecurityActivity acl in myFileIO.Result.SetFileSecurityActivity.Where(HasProcess))
            {
                var fileSetSec = new FileIOStatistics
                {
                    SetSecurity = new FileSetSecurityOperation()
                };

                fileSetSec.SetSecurity.AddSecurityEvent(acl.StartTime.DateTimeOffset, acl.ErrorCode);

                data.Add(results, acl.IssuingProcess.Id, acl.IssuingProcess.CreateTime.ConvertToTime(), acl.Path, fileSetSec);
            }

            
            foreach(IDeleteOnCloseFileActivity del in myFileIO.Result.DeleteOnCloseFileActivity.Where(HasProcess))
            {
                var fileDelete = new FileIOStatistics
                {
                    Delete = new FileDeleteOperation()
                };

                fileDelete.Delete.Count = 1;
                data.Add(results, del.IssuingProcess.Id, del.IssuingProcess.CreateTime.ConvertToTime(), del.Path, fileDelete);
            }

            foreach(var ren in myFileIO.Result.RenameFileActivity.Where(HasProcess))
            {
                var fileRename = new FileIOStatistics
                {
                    Rename = new FileRenameOperation()
                };

                fileRename.Rename.Count = 1;
                data.Add(results, ren.IssuingProcess.Id, ren.IssuingProcess.CreateTime.ConvertToTime(), ren.Path, fileRename);
            }


            // do not create a file if no FileIO data was present in trace
            if (data.FileName2PerProcessMapping.Count > 0)
            {
                results.FileIO = data;
            }
        }
    }
}
