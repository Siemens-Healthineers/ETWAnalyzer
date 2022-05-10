//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Stack Tag duration data
    /// </summary>
    public interface IStackTagDuration
    {
        /// <summary>
        /// CPU consumption in ms
        /// </summary>
        long CPUInMs { get; set; }

        /// <summary>
        /// Time this stacktag did occur the last time after the First Occurence did happen for the duration of that trace in the current process
        /// </summary>
        TimeSpan FirstLastOccurenceDuration { get; set; }

        /// <summary>
        /// Time when this stacktag did occur the first time in this process
        /// This is calculated of the timestamps of CPU sampling and Context Switchin times
        /// </summary>
        DateTimeOffset FirstOccurence { get; set; }

        /// <summary>
        /// Stacktag Name
        /// </summary>
        string Stacktag { get; }

        /// <summary>
        /// Sum of CPU and WaitDurationInMs
        /// </summary>
        double TotalDuration { get; }

        /// <summary>
        /// Wait duration summed across all threads in a process
        /// This value might be very large. The data is only present if the input ETL data did record Context Switch information
        /// </summary>
        long WaitDurationInMs { get; set; }

        /// <summary>
        /// Get First occurrence time in seconds since trace start. Optionally with a time shifted offset which is subtracted.
        /// </summary>
        /// <param name="sessionStart">Session start time</param>
        /// <param name="timeShiftS">Optional Value which is subtracted. Units are seconds.</param>
        /// <returns>First occurrence time in s</returns>
        double GetFirstOccurrenceS(DateTimeOffset sessionStart, double timeShiftS);

        /// <summary>
        /// Get Last Occurrence time in seconds since trace start. Optionally with time shift offset which is subtracted.
        /// </summary>
        /// <param name="sessionStart">Session start time.</param>
        /// <param name="timeShiftS">Optional value which is subtracted. Units are seconds.</param>
        /// <returns>Last occurrence time in s</returns>
        double GetLastOccurrenceS(DateTimeOffset sessionStart, double timeShiftS);
    }
}