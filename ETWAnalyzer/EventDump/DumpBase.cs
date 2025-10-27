//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Modules;
using ETWAnalyzer.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.EventDump
{
    /// <summary>
    /// Base class for all Dump commands to support common a execution method
    /// </summary>
    abstract class DumpBase : IDisposable
    {
        /// <summary>
        /// Explicitly enabled/disabled columns
        /// </summary>
        internal Dictionary<string, bool> ColumnConfiguration { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        protected bool GetOverrideFlag(string column, bool defaultFlag)
        {
            // if only disable rules are active we leave the enabled defaults 
            // otherwise just the enabled/disabled columns are enabled
            bool onlyDisableRules = ColumnConfiguration.Values.All(x => x == false);

            if(ColumnConfiguration.TryGetValue(column, out bool overrideFlag))
            {
                return overrideFlag;
            }

            // all other columns are disabled if explicit enable columns are configured.
            return onlyDisableRules ? defaultFlag : false;
        }



        /// <summary>
        /// Specifies how time is formatted
        /// </summary>
        internal enum TimeFormats
        {
            /// <summary>
            /// Time is printed as local time on the system where it was recorded. This is usually the time customers report when something did happen.
            /// </summary>
            Local = 0,

            /// <summary>
            /// Same as <see cref="Local"/> where the date part is omitted
            /// </summary>
            LocalTime,

            /// <summary>
            /// Format time as UTC with date
            /// </summary>
            UTC,

            /// <summary>
            /// Format time as UTC (no date part)
            /// </summary>
            UTCTime,

            /// <summary>
            /// Format time as local time on the current system locale settings
            /// </summary>
            Here,

            /// <summary>
            /// Format time as local time (no date part)
            /// </summary>
            HereTime,

            /// <summary>
            /// Seconds since trace start
            /// </summary>
            s,

            /// <summary>
            /// Seconds since trace start
            /// </summary>
            second,

            /// <summary>
            /// No format skip time formatting
            /// </summary>
            None,
        }

        /// <summary>
        /// Controls how time/date is formatted in output
        /// </summary>
        internal TimeFormats TimeFormatOption
        {
            get;set;
        }

        /// <summary>
        /// Overriden Time Precision supplied at command line
        /// </summary>
        internal int? TimePrecision
        {
            get; set;
        }

        /// <summary>
        /// Default precision for this command which can be different, depending on what granularity time makes sense
        /// </summary>
        internal int DefaultTimePrecision
        {
            get; set;
        } = 3;

        /// <summary>
        /// Get effective time precision used for formatting strings
        /// </summary>
        internal int OverridenOrDefaultTimePrecision
        {
            get => TimePrecision == null ? DefaultTimePrecision : TimePrecision.Value;
        }

        internal TimeFormats? ProcessFormatOption
        {
            get;set;
        }

        /// <summary>
        /// DateTime Format string used by various methods.
        /// </summary>
        internal const string DateTimeFormat0 = "yyyy-MM-dd HH:mm:ss";
        internal const string DateTimeFormat1 = "yyyy-MM-dd HH:mm:ss.f";
        internal const string DateTimeFormat2 = "yyyy-MM-dd HH:mm:ss.ff";
        internal const string DateTimeFormat3 = "yyyy-MM-dd HH:mm:ss.fff";
        internal const string DateTimeFormat4 = "yyyy-MM-dd HH:mm:ss.ffff";
        internal const string DateTimeFormat5 = "yyyy-MM-dd HH:mm:ss.fffff";
        internal const string DateTimeFormat6 = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static readonly string[] DateTimeFormatStrings = new string[]
        {
            DateTimeFormat0, DateTimeFormat1, DateTimeFormat2, DateTimeFormat3, DateTimeFormat4, DateTimeFormat5, DateTimeFormat6
        };

        /// <summary>
        /// Time format string
        /// </summary>
        internal const string TimeFormat0 = "HH:mm:ss";
        internal const string TimeFormat1 = "HH:mm:ss.f";
        internal const string TimeFormat2 = "HH:mm:ss.ff";
        internal const string TimeFormat3 = "HH:mm:ss.fff";
        internal const string TimeFormat4 = "HH:mm:ss.ffff";
        internal const string TimeFormat5 = "HH:mm:ss.fffff";
        internal const string TimeFormat6 = "HH:mm:ss.ffffff";

        internal static readonly string[] TimeFormatStrings = new string[]
        {
            TimeFormat0, TimeFormat1, TimeFormat2, TimeFormat3, TimeFormat4, TimeFormat5, TimeFormat6
        };


        /// <summary>
        /// Default column width with which seconds are formatted. This needs to be at least 8, otherwise the header description will not fit
        /// </summary>
        protected const int SecondsColWidth = 8;

        /// <summary>
        /// </summary>
        protected const int TimeFormatColWidth = 12;

        /// <summary>
        /// Related to <see cref="DateTimeFormatStrings"/> strings with default precison 3
        /// </summary>
        protected const int DateTimeColWidth = 23;

        /// <summary>
        /// Used by interactive mode to preload data and unit tests
        /// </summary>
        internal Lazy<SingleTest>[] myPreloadedTests = null;

        public abstract void Execute();

        /// <summary>
        /// Common method to format time into the same format across all commands
        /// </summary>
        /// <param name="time">Local time</param>
        /// <param name="sessionStart">ETW Trace session start time</param>
        /// <param name="fmt">Controls how time is formatted.</param>
        /// <param name="alignWidth">if true the output width is adjusted to some common width depending on the used time format</param>
        /// <returns>Formatted time string locale independent</returns>
        protected internal string GetDateTimeString(DateTimeOffset? time, DateTimeOffset sessionStart, TimeFormats fmt, bool alignWidth=false)
        {
            if (time == null)
            {
                return "".WithWidth(alignWidth ? GetWidth(fmt) : 0);
            }

            if (fmt == TimeFormats.LocalTime || fmt == TimeFormats.HereTime || fmt == TimeFormats.UTCTime)
            {
                return GetTimeString(time.Value, sessionStart, fmt).WithWidth(alignWidth ? GetWidth(fmt) : 0);
            }
            else
            {
                return GetDateTimeString(ConvertTime(time.Value, sessionStart, fmt)).WithWidth(alignWidth ? GetWidth(fmt) : 0);
            }
        }

        int WidthDiffTo3 { get => OverridenOrDefaultTimePrecision - 3; }

        protected int GetWidth(TimeFormats format)
        {
            return format switch
            {
                TimeFormats.s => SecondsColWidth + WidthDiffTo3,
                TimeFormats.second => SecondsColWidth + WidthDiffTo3,

                TimeFormats.HereTime => TimeFormatColWidth+ WidthDiffTo3,
                TimeFormats.LocalTime => TimeFormatColWidth + WidthDiffTo3,
                TimeFormats.UTCTime => TimeFormatColWidth + WidthDiffTo3,
                
                TimeFormats.Here => DateTimeColWidth + WidthDiffTo3,
                TimeFormats.Local => DateTimeColWidth + WidthDiffTo3,
                TimeFormats.UTC => DateTimeColWidth + WidthDiffTo3,
                _ => 0 // should not happen
            };
        }

        protected virtual string GetAbbreviatedName(TimeFormats format)
        {
            return format switch
            {
                TimeFormats.s => "s",
                TimeFormats.second => "s",

                TimeFormats.HereTime => "Here",
                TimeFormats.LocalTime => "Local",
                TimeFormats.UTCTime => "UTC",

                TimeFormats.Here => "Here",
                TimeFormats.Local => "Local",
                TimeFormats.UTC => "UTC",
                _ => throw new NotSupportedException($"No abbreviated name for {format} defined.")
            };
        }

        /// <summary>
        /// Common method to format a DateTime to a local time, or time in seconds since trace start
        /// </summary>
        /// <param name="time">Local time</param>
        /// <param name="sessionStart">Trace sessions start time</param>
        /// <param name="fmt">Controls how time is formatted.</param>
        /// <returns>Formatted time locale independent.</returns>
        protected string GetTimeString(DateTimeOffset ?time, DateTimeOffset sessionStart, TimeFormats fmt)
        {
            string lret = "";

            if (time != null)
            {
                Tuple<double?,DateTime?>  newTime = ConvertTime(time.Value, sessionStart, fmt);
                if (newTime.Item1.HasValue) // interpret as timespan 
                {
                    lret = FormatAsSeconds(newTime.Item1.Value, OverridenOrDefaultTimePrecision);
                }
                else
                {
                    string preciseFormat = TimeFormatStrings[OverridenOrDefaultTimePrecision];
                    lret = newTime.Item2.Value.ToString(preciseFormat, CultureInfo.InvariantCulture);
                }
            }

            return lret;
        }

        private static string FormatAsSeconds(double timeInS, int precision)
        {
            string seconds = timeInS.ToString($"F{precision}", CultureInfo.InvariantCulture);
            return seconds;
        }

        /// <summary>
        /// Get time as DateTime or seconds since trace start.
        /// </summary>
        /// <param name="time">Input time</param>
        /// <param name="sessionStart">ETW Session Start time which is needed to calculate the time since session start if output format is s</param>
        /// <param name="fmt">Time output format</param>
        /// <returns>Tuple which contains seconds since trace start or DateTime in desired time zone.</returns>
        /// <exception cref="NotImplementedException"></exception>
        Tuple<double?,DateTime?> ConvertTime(DateTimeOffset time, DateTimeOffset sessionStart, TimeFormats fmt)
        {
            return fmt switch
            {
                TimeFormats.Local =>     Tuple.Create<double?, DateTime?>(null, time.DateTime), // return time as local time on the system where it was recorded. This is the time customers usually report
                TimeFormats.LocalTime => Tuple.Create<double?, DateTime?>(null, time.DateTime), 
                TimeFormats.UTC =>       Tuple.Create<double?, DateTime?>(null, time.UtcDateTime),  // return time as UTC time 
                TimeFormats.UTCTime =>   Tuple.Create<double?, DateTime?>(null, time.UtcDateTime),
                TimeFormats.Here =>      Tuple.Create<double?, DateTime?>(null, time.LocalDateTime), // current timezone where the analysis takes place
                TimeFormats.HereTime =>  Tuple.Create<double?, DateTime?>(null, time.LocalDateTime), 
                TimeFormats.s =>         Tuple.Create<double?, DateTime?>(GetTimeDiff(time, sessionStart), null),
                TimeFormats.second =>    Tuple.Create<double?, DateTime?>(GetTimeDiff(time, sessionStart), null),
                _ => throw new NotImplementedException($"TimeFormat {fmt} not implemented."),
            };
        }

        /// <summary>
        /// Get time difference with correct sign.
        /// </summary>
        /// <param name="current">Current time</param>
        /// <param name="start">Base time which is subtracted</param>
        /// <returns>Time difference as double. Start can be greater than current. In that case the difference is negative.</returns>
        double GetTimeDiff(DateTimeOffset current, DateTimeOffset start)
        {
            double lret = 0.0d;
            if( current > start)
            {
                lret = (current - start).TotalSeconds;
            }
            else
            {
                lret = (start - current).TotalSeconds * -1.0d;
            }

            return lret;
        }

        /// <summary>
        /// Print version string from module definition
        /// </summary>
        /// <param name="def"></param>
        /// <param name="addDirectory">Print also directory of module</param>
        /// <returns>Module definition string.</returns>
        protected string GetModuleString(ModuleDefinition def, bool addDirectory=false)
        {
            string versionStr = def.FileVersionStr;
            string version = def.Fileversion?.ToString();

            string lret = null;
            if (!String.IsNullOrEmpty(version))
            {
                if (version == versionStr)
                {
                    lret = versionStr;
                }
                else
                {
                    lret = $"FileVersion: {version?.Trim()}, VersionString: {versionStr?.Trim()},";
                }
            }

            if (!String.IsNullOrEmpty(def.ProductVersionStr))
            {
                if (def.ProductVersionStr != version &&
                    def.ProductVersionStr != versionStr)
                {
                    if( !String.IsNullOrEmpty(lret) )
                    {
                        lret += " ";
                    }
                    lret += $"ProductVersion: {def.ProductVersionStr.Trim()},";
                }
            }

            if (!String.IsNullOrEmpty(def.ProductName))
            {
                lret += $" ProductName: {def.ProductName.Trim()},";
            }
            if (!String.IsNullOrEmpty(def.Description?.Trim()))
            {
                lret += $" Description: {def.Description.Trim()},";
            }
            if( addDirectory)
            {
                lret += $" Directory: {def.ModulePath}";
            }

            lret = lret.TrimEnd();
            lret = lret.TrimEnd(',');
            lret = lret.Replace('[', '('); // Do not mess with colors in ColorConsole output

            return lret;
        }


        /// <summary>
        /// Return empty string or default value representation of a nullable value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        protected string GetNullableString<T>(Nullable<T> value) where T : struct
        {
            return value == null ? "" : value.ToString();
        }

        protected string GetDateTimeString(Tuple<double?,DateTime?> time)
        {
            string lret = "";
            if( time.Item1.HasValue) // interpret as timespan
            {
                lret = FormatAsSeconds(time.Item1.Value, OverridenOrDefaultTimePrecision);
            }
            else
            {
                lret = time.Item2.Value.ToString(DateTimeFormatStrings[OverridenOrDefaultTimePrecision], CultureInfo.InvariantCulture);
            }
            
            return lret;
        }

        protected string GetProcessTags(ETWProcess process, DateTimeOffset sessionStart)
        {
            if(process == null)
            {
                return "";
            }

            if(ProcessFormatOption == null)
            {
                return process.StartStopTags;
            }
            else
            {
                
                string lret = "";
                if (ProcessFormatOption != TimeFormats.None)
                {
                    if (process.IsNew)
                    {
                        lret += " +" + GetDateTimeString(process.StartTime, sessionStart, ProcessFormatOption.Value);
                    }
                    if (process.HasEnded)
                    {
                        lret += " - " + GetDateTimeString(process.EndTime, sessionStart, ProcessFormatOption.Value);
                    }
                    if (process.IsNew && process.HasEnded) // print process duration as timespan
                    {
                        lret += $" {FormatTimeSpan(process.EndTime - process.StartTime, OverridenOrDefaultTimePrecision)}";
                    }
                }
                return lret;
            }

        }

        /// <summary>
        /// Common method to format a TimeSpan into a string with variable precision
        /// </summary>
        /// <param name="span"></param>
        /// <param name="precision"></param>
        /// <returns>string representation</returns>
        protected string FormatTimeSpan(TimeSpan span, int precision)
        {
            if (precision == 0)
            {
                return $"{span.TotalSeconds.ToString("F0", CultureInfo.InvariantCulture)} s";
            }
            else
            { 
                string digits = new string('f', precision);
                return span.ToString($@"d\.hh\:mm\:ss\.{digits}");
            }
        }


        /// <summary>
        /// Get maximum string length for column width calculation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">items to be formatted</param>
        /// <param name="getter">String extractor</param>
        /// <returns>Maximum string length returned by getter for all items.</returns>
        protected static int GetMaxLength<T>(IList<T> collection, Func<T,string> getter)
        {
            return collection.Max(x => (getter(x)?.Length).GetValueOrDefault());
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {

        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
