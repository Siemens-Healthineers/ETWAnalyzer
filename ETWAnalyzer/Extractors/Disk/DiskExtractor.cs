//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Disk;
using ETWAnalyzer.Infrastructure;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Disk;
using Microsoft.Windows.EventTracing.Processes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ETWAnalyzer.Extractors
{
    class DiskExtractor : ExtractorBase
    {
        IPendingResult<IDiskActivityDataSource> myDiskIO;

        public DiskExtractor()
        {
        }

        public override void RegisterParsers(ITraceProcessor processor)
        {
            myDiskIO = processor.UseDiskIOData();
        }

        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            using var logger = new PerfLogger("Extract Disk");
            if ( !myDiskIO.HasResult )
            {
                Console.WriteLine("Warning: No DiskIO activity was recorded");
                return;
            }

            DiskIOData data = new DiskIOData();

            foreach (IDiskActivity diskActivity in myDiskIO.Result.Activity)
            {
                data.Add(diskActivity);
            }

            results.Disk = data;
        }
    }
}
