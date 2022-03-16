//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Processes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ETWAnalyzer.Extract.ETWProcess;

namespace ETWAnalyzer.TraceProcessorHelpers
{
    static class Extensions
    {
        public static string GetToolkitPath()
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string toolkitPath = Path.Combine(folderPath, "Microsoft", "Windows.EventTracing.Processing", "1.2.1", "x64");
            return toolkitPath;
        }
        static public DateTimeOffset ConvertToTime(this TraceTimestamp ?time)
        {
            return time == null ? DateTimeOffset.MinValue : time.Value.DateTimeOffset;
        }

        static public bool IsMatch(this IProcess process, ProcessStates? state)
        {
            if( state == null )
            {
                return true;
            }

            if(process.CreateTime == null && process.ExitTime == null && state == ProcessStates.None)
            {
                return true;
            }

            if( process.CreateTime.HasValue && state == ProcessStates.Started)
            {
                return true;
            }
            if( process.ExitTime.HasValue && state == ProcessStates.Stopped)
            {
                return true;
            }

            return false;
        }
    }
}
