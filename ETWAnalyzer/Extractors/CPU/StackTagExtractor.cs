//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Configuration;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Cpu;
using Microsoft.Windows.EventTracing.Processes;
using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extractors.CPU
{
    /// <summary>
    /// Extract from an ETL file all CPU sampling and Context Switch ETW data call stacks mapped stack tag names.
    /// That allows one to see e.g. the GC/JIT overhead of processes during the recording.
    /// Another use case is to stacktag specific methods in the SpecialStacktag file to get a trending of specific methods
    /// between ETL files. This enables use cases where one wants to find when a specific method did become more expensive or it did wait longer 
    /// to find the root cause of an regression issue by checking past ETW recording data.
    /// </summary>
    class StackTagExtractor : ExtractorBase
    {
        IPendingResult<IStackTagDataSource> myStackTags;
        IPendingResult<ICpuSampleDataSource> mySamplingData;
        IPendingResult<ICpuSchedulingDataSource> myCpuSchedlingData;
        readonly StackPrinter myStackPrinter = new();

        /// <summary>
        /// Stacktag file which is used for default stacktags
        /// </summary>
        public string DefaultStackTagFile
        {
            get;
            set;
        } = ConfigFiles.DefaultStackTagFile;

        /// <summary>
        /// GC/JIT is separately stacktagged to get a second view how much time in total is spent in GC/JIT
        /// without reducing CPU/Wait times from the default stacktags.
        /// </summary>
        public string SecondaryStackTagFile
        {
            get;
            set;
        } = ConfigFiles.GCJitStacktagFile;

        /// <summary>
        /// User supplied stacktag file which is written to an extra node in ETWExtract to perform e.g. trending of specific methods
        /// </summary>
        public string SpecialStacktagFile
        {
            get;
            set;
        } = ConfigFiles.SpecialStackTagFile;
        public int? Concurrency { get; internal set; }

        /// <summary>
        /// Ignore CPU sampling data. Used when CPU sampling data is corrupt
        /// </summary>
        public bool NoSampling { get; internal set; }


        /// <summary>
        /// Ignore Context Switch data. Can conserve memory during extraction at the cost of missing thread wait/ready data.
        /// </summary>
        public bool NoCSwitch {  get; internal set; }   

        /// <summary>
        /// True when it is not disabled and we have data
        /// </summary>
        bool CanUseCpuSamplingData { get => mySamplingData?.HasResult == true && !NoSampling; }

        /// <summary>
        /// True when it is not disabld and we have CSwitch data
        /// </summary>
        bool CanUseCPUCSwitchData {  get => myCpuSchedlingData?.HasResult == true && !NoCSwitch; }


        /// <summary>
        /// GC/JIT Stacktags are thrown away when smaller 10ms
        /// </summary>
        const double MinProcessGCJITTimeInMs = 10.0;

        const string DefaultStackTag = "Other";

        public StackTagExtractor()
        { }

        public override void RegisterParsers(ITraceProcessor processor)
        {
            NeedsSymbols = true;
            myStackTags = processor.UseStackTags();

            if (!NoSampling)
            {
                mySamplingData = processor.UseCpuSamplingData();
            }

            if (!NoCSwitch)
            {
                myCpuSchedlingData = processor.UseCpuSchedulingData();
            }
        }

        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            IStackTagMapper defaultStackTagMapper = myStackTags.Result.CreateMapper(DefaultStackTagFile);
            IStackTagMapper gcJitStackTagMapper = myStackTags.Result.CreateMapper(SecondaryStackTagFile);
            IStackTagMapper specialStackTagMapper = myStackTags.Result.CreateMapper(SpecialStacktagFile);

            // for each process we store the stack tag and its duration based on CPU sampling and Context Switch information
            var gcJitStackTags = new Dictionary<ProcessKey, Dictionary<string, StackTagDuration>>();
            var totalStackTagsRaw = new Dictionary<ProcessKey, Dictionary<string, StackTagDuration>>();
            var specialStackTagsRaw = new Dictionary<ProcessKey, Dictionary<string, StackTagDuration>>();

            using (new PerfLogger($"Extract from default stacktag file: {DefaultStackTagFile}"))
            {
                ExtractStackTagsFromProfilingAndContextSwitchEvents(gcJitStackTagMapper, gcJitStackTags, addDefaultStackTag: false);
            }

            using (new PerfLogger($"Extracting from secondary stacktag file: {SecondaryStackTagFile}"))
            {
                ExtractStackTagsFromProfilingAndContextSwitchEvents(defaultStackTagMapper, totalStackTagsRaw, addDefaultStackTag: true);
            }

            using (new PerfLogger($"Extracting from special stacktag file: {SpecialStacktagFile}"))
            {
                ExtractStackTagsFromProfilingAndContextSwitchEvents(specialStackTagMapper, specialStackTagsRaw, addDefaultStackTag: false);
            }

            ProcessStackTags totalStackTags = new()
            {
                UsedStackTagFiles = new string[] { DefaultStackTagFile, SecondaryStackTagFile }
            };

            static bool filterTooSmallTags(StackTagDuration tag) => tag.CPUInMsInternal > MinProcessGCJITTimeInMs;

            ConvertRawDataToProcessStackTags(gcJitStackTags, totalStackTags, filterTooSmallTags);
            ConvertRawDataToProcessStackTags(totalStackTagsRaw, totalStackTags, null);

            ProcessStackTags specialStackTags = new()
            {
                UsedStackTagFiles = new string[] { SpecialStacktagFile }
            };

            ConvertRawDataToProcessStackTags(specialStackTagsRaw, specialStackTags, null);

            results.SummaryStackTags = totalStackTags;
            results.SpecialStackTags = specialStackTags;

            // Try to release memory as early as possible
            myStackTags = null;
            mySamplingData = null;
            myCpuSchedlingData = null;
        }

        void ExtractStackTagsFromProfilingAndContextSwitchEvents(IStackTagMapper mapper, Dictionary<ProcessKey, Dictionary<string, StackTagDuration>> tags, bool addDefaultStackTag)
        {
            if (CanUseCpuSamplingData)
            {
                // Get Stack Tag information from CPU Sampling data
                foreach (ICpuSample sample in mySamplingData.Result.Samples)
                {
                    DateTimeOffset createTime = DateTimeOffset.MinValue;
                    if ((sample?.Process?.CreateTime).HasValue)
                    {
                        createTime = sample.Process.CreateTime.Value.DateTimeOffset;
                    }

                    string imageName = sample?.Process?.ImageName;
                    if (imageName == null)
                    {
                        continue;
                    }

                    var key = new ProcessKey(imageName, sample.Process.Id, createTime);


                    AddSampleDuration(mapper, tags, sample, key, addDefaultStackTag);
                }
            }


            // When we have context switch data recorded we can also calculate the thread wait time stacktags
            if( CanUseCPUCSwitchData )
            {
                foreach (ICpuThreadActivity slice in myCpuSchedlingData.Result.ThreadActivity)
                {
                    string waitTag = slice.SwitchIn.Stack?.GetStackTagPath(mapper);
                    if (waitTag == null)
                    {
                        continue;
                    }

                    waitTag = myStackPrinter.GetPrettyMethod(waitTag, default(IImage));

                    DateTimeOffset createTime = DateTimeOffset.MinValue;
                    IProcess process = slice?.SwitchIn?.Process;

                    if ((process?.CreateTime).HasValue)
                    {
                        createTime = process.CreateTime.Value.DateTimeOffset;
                    }

                    string imageName = process?.ImageName;
                    if (imageName == null)
                    {
                        continue;
                    }

                    var key = new ProcessKey(imageName, process.Id, createTime);

                    if (!tags.TryGetValue(key, out Dictionary<string, StackTagDuration> durationkvp))
                    {
                        durationkvp = new Dictionary<string, StackTagDuration>();
                        tags.Add(key, durationkvp);
                    }

                    if (!durationkvp.TryGetValue(waitTag, out StackTagDuration duration))
                    {
                        duration = new StackTagDuration(waitTag);
                        durationkvp.Add(waitTag, duration);
                    }

                    if (slice.WaitingDuration.HasValue)
                    {
                        duration.WaitDurationInMsInternal += (double)slice.WaitingDuration.Value.TotalMilliseconds;

                        DateTimeOffset sampleTime = slice.StartTime.DateTimeOffset;
                        duration.FirstOccurence = duration.FirstOccurence > sampleTime ? sampleTime : duration.FirstOccurence;
                        duration.FirstLastOccurenceDuration = sampleTime - duration.FirstOccurence;
                    }
                }
            }
        }


        /// <summary>
        /// Convert and merge raw stacktag data into an existing ProcessStackTags instance
        /// </summary>
        /// <param name="rawStackTags"></param>
        /// <param name="extractGcStackStackTags">Existing instance where data is filled into</param>
        /// <param name="filter">Do not include stacktags which do not pass the filter, e.g. do no cross a specific CPU threshold to prevent filling up the final json file.</param>
        private static void ConvertRawDataToProcessStackTags(Dictionary<ProcessKey, Dictionary<string, StackTagDuration>> rawStackTags, ProcessStackTags extractGcStackStackTags, Func<StackTagDuration, bool> filter = null)
        {
            if (filter == null)
            {
                filter = x => true;
            }

            foreach (var processKeyAndStackTagDict in rawStackTags)
            {
                // calculate ms back and order descending by stacktag duration
                List<StackTagDuration> sorted = processKeyAndStackTagDict.Value.Values.Where(filter).OrderByDescending(x => x.CPUInMsInternal).ToList();

                // Do not add empty values to Json file
                if (sorted.Count == 0)
                {
                    continue;
                }

                List<StackTagDuration> existing = extractGcStackStackTags.Stats.FirstOrDefault(x => x.Key == processKeyAndStackTagDict.Key).Value;
                if (existing != null)
                {
                    existing.AddRange(sorted);
                }
                else
                {
                    extractGcStackStackTags.Stats.Add(new KeyValuePair<ProcessKey, List<StackTagDuration>>(processKeyAndStackTagDict.Key, sorted));
                }
            }
        }

        private void AddSampleDuration(IStackTagMapper stacktagMapper, Dictionary<ProcessKey, Dictionary<string, StackTagDuration>> perProcessStackTags, ICpuSample sample, ProcessKey key, bool addDefaultTag)
        {
            if (!perProcessStackTags.TryGetValue(key, out Dictionary<string, StackTagDuration> stacktagDurations))
            {
                stacktagDurations = new Dictionary<string, StackTagDuration>();
                perProcessStackTags.Add(key, stacktagDurations);
            }

            string stackTag = sample.Stack?.GetStackTagPath(stacktagMapper) ?? DefaultStackTag;
            if (!addDefaultTag && stackTag == DefaultStackTag) // ignore unimportant processes such as conhost ... 
            {
                return;
            }

            stackTag = myStackPrinter.GetPrettyMethod(stackTag, default(IImage));

            if (!stacktagDurations.TryGetValue(stackTag, out StackTagDuration duration))
            {
                duration = new StackTagDuration(stackTag);
                stacktagDurations.Add(stackTag, duration);
            }

            DateTimeOffset sampleTime = sample.Timestamp.DateTimeOffset;

            duration.FirstOccurence = duration.FirstOccurence > sampleTime ? sampleTime : duration.FirstOccurence;
            duration.FirstLastOccurenceDuration = sampleTime - duration.FirstOccurence;
            duration.CPUInMsInternal += (double)sample.Weight.TotalMilliseconds;
        }
    }
}
