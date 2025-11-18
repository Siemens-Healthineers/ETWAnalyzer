//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Newtonsoft.Json;
using System;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Stacktag name comes from a WPA .stacktags file
    /// Duration is calculated by CPU sampling events and 
    /// WaitDurationInMS is calculated by Context Switch events
    /// If the trace does not contain context switch event the wait durations are still zero
    /// </summary>
    public class StackTagDuration : IStackTagDuration
    {
        /// <summary>
        /// Stack tag name for 
        /// </summary>
        public string Stacktag { get; }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public double CPUInMsInternal { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public double WaitDurationInMsInternal { get; internal set; }

        /// <summary>
        /// For storage we store in Json only full ms sampling values to save space
        /// </summary>
        public long CPUInMs
        {
            get => (long)Math.Ceiling(CPUInMsInternal);  // Round to next full ms value
            set => CPUInMsInternal = value;
        }

        /// <summary>
        /// For storage we store in Json only full ms from Context Switch tracing data for this stacktag
        /// </summary>
        public long WaitDurationInMs
        {
            get => (long)Math.Ceiling(WaitDurationInMsInternal); // Round to next full ms value
            set => WaitDurationInMsInternal = value;
        }

        /// <summary>
        /// Sum of CPUInMs and WaitDurationInMs
        /// </summary>
        [JsonIgnore]
        public double TotalDuration { get => CPUInMsInternal + WaitDurationInMsInternal; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stackTag"></param>
        public StackTagDuration(string stackTag)
        {
            Stacktag = stackTag;
        }

        /// <summary>
        /// Time when this stacktag did occur the first time in this process
        /// This is calculated of the timestamps of CPU sampling and Context Switchin times
        /// </summary>
        public DateTimeOffset FirstOccurence { get; set; } = DateTimeOffset.MaxValue;


        /// <summary>
        /// Get First occurrence time in seconds since trace start. Optionally with a time shifted offset which is subtracted.
        /// </summary>
        /// <param name="sessionStart">Session start time</param>
        /// <param name="timeShiftS">Optional Value which is subtracted. Units are seconds.</param>
        /// <returns>First occurrence time in s</returns>
        public double GetFirstOccurrenceS(DateTimeOffset sessionStart, double timeShiftS)
        {
            return (FirstOccurence - sessionStart).TotalSeconds - timeShiftS;
        }


        /// <summary>
        /// Time this stacktag did occur the last time after the First Occurence did happen for the duration of that trace in the current process
        /// </summary>
        public TimeSpan FirstLastOccurenceDuration { get; set; }

        /// <summary>
        /// Get Last Occurrence time in seconds since trace start. Optionally with time shift offset which is subtracted.
        /// </summary>
        /// <param name="sessionStart">Session start time.</param>
        /// <param name="timeShiftS">Optional value which is subtracted. Units are seconds.</param>
        /// <returns>Last occurrence time in s</returns>
        public double GetLastOccurrenceS(DateTimeOffset sessionStart, double timeShiftS)
        {
            return ((FirstOccurence + FirstLastOccurenceDuration) - sessionStart).TotalSeconds - timeShiftS;
        }

    }
}
