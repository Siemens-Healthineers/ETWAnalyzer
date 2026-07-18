//// SPDX-FileCopyrightText:  © 2026 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using System.Globalization;
using System.IO;

namespace ETWAnalyzer.Extractors
{
    /// <summary>
    /// Describes a trace relative time region (seconds since trace/session start) which is used by the
    /// -extractRegion command line switch to extract CPU/Disk/File/TCP data only for a specific time window.
    /// </summary>
    public class ETWExtractTimeRange
    {
        /// <summary>
        /// Prefix used to enter the end value as a duration relative to the start time (e.g. -extractRegion 1.0 +2).
        /// </summary>
        public const string DurationPrefix = "+";

        /// <summary>
        /// Region start in seconds relative to the trace/session start.
        /// </summary>
        public double StartS { get; }

        /// <summary>
        /// Region end in seconds relative to the trace/session start.
        /// </summary>
        public double EndS { get; }

        /// <summary>
        /// Original entered start text. Used to build the extract file name suffix so 1.0 stays 1.0.
        /// </summary>
        public string StartText { get; }

        /// <summary>
        /// End text used to build the extract file name suffix. When the end was entered as a duration (+x) this holds the resolved end time.
        /// </summary>
        public string EndText { get; }

        /// <summary>
        /// Create a time region from the entered start/end strings.
        /// </summary>
        /// <param name="startText">Start time in seconds since trace start.</param>
        /// <param name="endText">End time in seconds since trace start, or a duration relative to the start when prefixed with '+' (e.g. +2).</param>
        /// <exception cref="InvalidDataException">When start/end could not be parsed or start &gt; end.</exception>
        public ETWExtractTimeRange(string startText, string endText)
        {
            StartText = startText;
            StartS = ArgParser.ParseDouble(startText);

            if (endText != null && endText.StartsWith(DurationPrefix, System.StringComparison.Ordinal))
            {
                // End is entered as a duration relative to the start time e.g. -extractRegion 1.0 +2 => region 1.0 - 3.0
                double duration = ArgParser.ParseDouble(endText.Substring(DurationPrefix.Length));
                EndS = StartS + duration;
                EndText = FormatSeconds(EndS);
            }
            else
            {
                EndS = ArgParser.ParseDouble(endText);
                EndText = endText;
            }

            if (EndS < StartS)
            {
                throw new InvalidDataException($"-extractRegion start time {startText} must not be larger than end time {endText}.");
            }
        }

        /// <summary>
        /// Format a trace relative time in seconds with a stable invariant representation which always keeps at least one decimal.
        /// </summary>
        /// <param name="seconds">Seconds value.</param>
        /// <returns>Formatted value e.g. 3.0</returns>
        static string FormatSeconds(double seconds)
        {
            return seconds.ToString("0.0###", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Check if a trace relative time in seconds is within this region.
        /// </summary>
        /// <param name="traceRelativeSeconds">Seconds since trace start.</param>
        /// <returns>True when the value is within [StartS, EndS].</returns>
        public bool IsWithin(decimal traceRelativeSeconds)
        {
            double s = (double)traceRelativeSeconds;
            return s >= StartS && s <= EndS;
        }

        /// <summary>
        /// File name suffix part for this region e.g. Time_1.0-2.0
        /// </summary>
        /// <returns>File name suffix.</returns>
        public string ToFileNamePart()
        {
            return $"Time_{StartText}-{EndText}";
        }
    }
}

