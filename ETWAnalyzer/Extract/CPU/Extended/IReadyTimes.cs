//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Newtonsoft.Json;
using System.Collections.Generic;

namespace ETWAnalyzer.Extract.CPU.Extended
{
    /// <summary>
    /// Ready details
    /// </summary>
    public interface IReadyTimes
    {
        /// <summary>
        /// Minimum Ready time in microseconds.
        /// </summary>
        float MinUs { get; }

        /// <summary>
        /// Maximum Ready time in microseconds.
        /// </summary>
        float MaxUs { get; }

        /// <summary>
        /// 5% Percentile Ready time in microseconds.
        /// </summary>
        float Percentile5 { get; }

        /// <summary>
        /// 25% Percentile Ready time in microseconds.
        /// </summary>
        float Percentile25 { get; }

        /// <summary>
        /// Median = 50% Percentile Ready time in microseconds.
        /// </summary>
        float Percentile50 { get; }

        /// <summary>
        /// 90% Percentile Ready time in microseconds.
        /// </summary>
        float Percentile90 { get; }

        /// <summary>
        /// 95% Percentile Ready time in microseconds.
        /// </summary>
        float Percentile95 { get; }

        /// <summary>
        /// 99% Percentile Ready time in microseconds.
        /// </summary>
        float Percentile99 { get; }

    }



    /// <summary>
    /// Ready time percentiles.
    /// This class is serialized to Json file
    /// </summary>
    public class ReadyTimes : IReadyTimes
    {
        /// <summary>
        /// Contains when filled Ready Min,Max,Percentiles 5,25,50,90,95,99
        /// </summary>
        public List<float> ReadyTimesUs { get; set; } = [];

        /// <summary>
        /// Add Ready time details for a given method.
        /// </summary>
        /// <param name="minS">Minimum Ready time in s</param>
        /// <param name="maxS">Maximum Ready time in s</param>
        /// <param name="percentile5">5% Percentile Ready Time in s</param>
        /// <param name="percentile25">25% Percentile Ready Time in s</param>
        /// <param name="percentile50">50% Percentile Ready Time in s</param>
        /// <param name="percentile90">90% Percentile Ready Time in s</param>
        /// <param name="percentile95">95% Percentile Ready Time in s</param>
        /// <param name="percentile99">99% Percentile Ready Time in s</param>
        public void AddReadyTimes(float minS, float maxS, float percentile5, float percentile25, float percentile50, float percentile90, float percentile95, float percentile99)
        {
            ReadyTimesUs.Add(minS);
            ReadyTimesUs.Add(maxS);
            ReadyTimesUs.Add(percentile5);
            ReadyTimesUs.Add(percentile25);
            ReadyTimesUs.Add(percentile50);
            ReadyTimesUs.Add(percentile90);
            ReadyTimesUs.Add(percentile95);
            ReadyTimesUs.Add(percentile99);
        }

        const float Million = 1000000.0f;

        /// <summary>
        /// Minimum Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public float MinUs { get => ReadyTimesUs[0] * Million; }

        /// <summary>
        /// Maximum Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public float MaxUs { get => ReadyTimesUs[1] * Million; }

        /// <summary>
        /// 5% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public float Percentile5 { get => ReadyTimesUs[2] * Million; }

        /// <summary>
        /// 25% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public float Percentile25 { get => ReadyTimesUs[3] * Million; }

        /// <summary>
        /// Median = 50% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public float Percentile50 { get => ReadyTimesUs[4] * Million; }

        /// <summary>
        /// 90% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public float Percentile90 { get => ReadyTimesUs[5] * Million; }

        /// <summary>
        /// 95% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public float Percentile95 { get => ReadyTimesUs[6] * Million; }

        /// <summary>
        /// 99% Percentile Ready time in microseconds.
        /// </summary>
        [JsonIgnore]
        public float Percentile99 { get => ReadyTimesUs[7] * Million; }
    }
}
