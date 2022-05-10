//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Windows.EventTracing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ETWAnalyzer.Extractors
{
    abstract class ExtractorBase
    {
        /// <summary>
        /// Set by specific extractor if we need to fetch symbols before extracting data
        /// </summary>
        public bool NeedsSymbols { get; set;  }

        /// <summary>
        /// Externally set to force reload of symbols in case some essential symbols are missing
        /// </summary>
        public Action ForceSymbolReloadWithRemoteSymbolSever
        {
            get; set;
        } = () => { };

        /// <summary>
        /// Exported process names in CSV have the format 
        /// process.exe (dddd)
        /// We split the string to pieces to parse pid and process name independently.
        /// </summary>
        public static char[] ProcessSplitChars = new char[] { '(', ')', ' ' };

        /// <summary>
        /// Before using the trace processor one needs to register the event parsers. Later during extraction processor.Process() is called which then executes all the 
        /// parsers where you then can fetch the results of your previously registered parsers (<see cref="RegisterParsers(ITraceProcessor)"/>).
        /// </summary>
        /// <param name="processor">Trace processor which will provide events which are of interest to you via the .UseeXXXXX methods.</param>
        public virtual void RegisterParsers(ITraceProcessor processor)
        {
        }

        /// <summary>
        /// Extract data from ETL file with the new TraceProcessor library. Over time we will switch over to this one instead of the other overload with TraceLog and WpaExporter
        /// /// </summary>
        /// <param name="processor">Traceprocessor instance</param>
        /// <param name="results">Results which will be serialized</param>
        /// <remarks>
        /// Traceprocessor links
        /// https://blogs.windows.com/windowsdeveloper/2019/05/09/announcing-traceprocessor-preview-0-1-0/
        /// https://blogs.windows.com/windowsdeveloper/2019/08/07/traceprocessor-0-2-0/
        /// https://blogs.windows.com/windowsdeveloper/2020/01/28/traceprocessor-0-3-0/
        /// Samples
        /// https://github.com/microsoft/eventtracing-processing
        /// https://randomascii.wordpress.com/2020/01/05/bulk-etw-trace-analysis-in-c/
        /// https://github.com/google/UIforETW/tree/master/TraceProcessors
        /// Stackoverflow Questions
        /// https://stackoverflow.com/questions/tagged/.net-traceprocessing
        /// </remarks>
        public virtual void Extract(ITraceProcessor processor, ETWExtract results)
        {
        }

        /// <summary>
        /// Get from a list of CSV files the one and only one file which matches the passed substring.
        /// </summary>
        /// <param name="wpaExportedCSVFiles">List of exported CSV files from ETL file.</param>
        /// <param name="filenameSubstring">csv file name substring. Only the file name part is matched not the directory part of the file name.</param>
        /// <returns>matching file or an InvalidDataExcption is thrown.</returns>
        public string GetCSVFile(string[] wpaExportedCSVFiles, string filenameSubstring)
        {
            var file = wpaExportedCSVFiles.SingleOrDefault(wpaFile => Path.GetFileName(wpaFile).Contains(filenameSubstring));
            if (file == null)
            {
                throw new InvalidDataException($"The required CSV file {filenameSubstring} was not found int exported CSV files by wpaExporter. There were {wpaExportedCSVFiles.Length} files exported.");
            }

            return file;
        }
    }
}
