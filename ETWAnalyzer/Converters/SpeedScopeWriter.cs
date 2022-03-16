using Microsoft.Diagnostics.Tracing.Stacks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ETWAnalyzer.Converters.StackSourceWriterHelper;

namespace ETWAnalyzer.Converters
{
    /// <summary>
    /// Copied from PerfView to enable custom speedscope extraction
    /// Taken from https://github.com/microsoft/perfview/blob/main/src/TraceEvent/Stacks/SpeedScopeStackSourceWriter.cs
    /// Added allThreadsMerged to view per process totals regardless on which thread the method did cause costs.
    /// </summary>
    static class SpeedScopeWriter
    {
        /// <summary>
        /// exports provided StackSource to a https://www.speedscope.app/ format 
        /// schema: https://www.speedscope.app/file-format-schema.json
        /// </summary>
        public static void WriteStackViewAsJson(StackSource source, string filePath, bool allThreadsMerged)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);

            using var writeStream = File.CreateText(filePath);
            Export(source, writeStream, Path.GetFileNameWithoutExtension(filePath), allThreadsMerged);
        }

        #region private
        private static void Export(StackSource source, TextWriter writer, string name, bool allThreadsMerged)
        {
            var samplesPerThread = GetSortedSamplesPerThread(source);

            var exportedFrameNameToExportedFrameId = new Dictionary<string, int>();
            var exportedFrameIdToFrameTuple = new Dictionary<int, FrameInfo>();
            var profileEventsPerThread = new Dictionary<string, IReadOnlyList<ProfileEvent>>();

            foreach (var pair in samplesPerThread)
            {
                var sortedProfileEvents = GetProfileEvents(source, pair.Value, exportedFrameNameToExportedFrameId, exportedFrameIdToFrameTuple);

                Debug.Assert(Validate(sortedProfileEvents), "The output should be always valid");

                profileEventsPerThread.Add(pair.Key.Name+ pair.Key.ProcessId, sortedProfileEvents);
            };

            // If requested merge all threads into one big list to get an aggregate metric for all methods from all threads
            // To make it work we need to shift the time information for each thread to ensure that the merged view does not get overlapping
            // events from concurrently running things. This breaks the Time Order View Of SpeedScope, but it is not so useful anyway.
            // Left Heavy and Sandwich are the views which we are most often interested most.
            if (allThreadsMerged)
            {
                List<ProfileEvent> all = new List<ProfileEvent>();
                double lastRealtiveMaxTime = 0.0d;
                foreach (var kvp in profileEventsPerThread)
                {
                    foreach (var ev in kvp.Value)
                    {
                        var tmp = new ProfileEvent(ev.Type, ev.FrameId, ev.RelativeTime + lastRealtiveMaxTime, ev.Depth);
                        all.Add(tmp);
                    }
#pragma warning disable CA1826 // Do not use Enumerable methods on indexable collections
                    lastRealtiveMaxTime += kvp.Value.LastOrDefault().RelativeTime + 0.05d;
#pragma warning restore CA1826 // Do not use Enumerable methods on indexable collections
                }
                profileEventsPerThread.Clear();
                HashSet<int> forbiddenFrames = exportedFrameNameToExportedFrameId.Where(x => x.Key.StartsWith("Thread (", StringComparison.Ordinal)).Select(x => x.Value).ToHashSet();
                profileEventsPerThread["All Threads"] = all.Where(x => !forbiddenFrames.Contains(x.FrameId)).OrderBy(x => x.RelativeTime).ToList();
            }

            var orderedFrameNames = exportedFrameNameToExportedFrameId.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToArray();

            WriteToFile(profileEventsPerThread, orderedFrameNames, writer, name);
        }

        /// <summary>
        /// writes pre-calculated data to SpeedScope format
        /// </summary>
        private static void WriteToFile(IReadOnlyDictionary<string, IReadOnlyList<ProfileEvent>> sortedProfileEventsPerThread,
            IReadOnlyList<string> orderedFrameNames, TextWriter writer, string name)
        {
            writer.Write("{");
            writer.Write($"\"exporter\": \"{GetExporterInfo()}\", ");
            writer.Write($"\"name\": \"{name}\", ");
            writer.Write("\"activeProfileIndex\": 0, ");
            writer.Write("\"$schema\": \"https://www.speedscope.app/file-format-schema.json\", ");

            writer.Write("\"shared\": { \"frames\": [ ");
            for (int i = 0; i < orderedFrameNames.Count; i++)
            {
                writer.Write($"{{ \"name\": \"{orderedFrameNames[i].Replace("\\", "\\\\").Replace("\"", "\\\"")}\" }}");

                if (i != orderedFrameNames.Count - 1)
                    writer.Write(", ");
            }
            writer.Write("] }, ");

            writer.Write("\"profiles\": [ ");

            bool isFirst = true;
            foreach (var perThread in sortedProfileEventsPerThread.OrderBy(pair => pair.Value.FirstOrDefault().RelativeTime))
            {
                if (!isFirst)
                    writer.Write(", ");
                else
                    isFirst = false;

                var sortedProfileEvents = perThread.Value;

                writer.Write("{ ");
                writer.Write("\"type\": \"evented\", ");
                writer.Write($"\"name\": \"{perThread.Key}\", ");
                writer.Write("\"unit\": \"milliseconds\", ");
                writer.Write($"\"startValue\": \"{sortedProfileEvents.FirstOrDefault().RelativeTime.ToString("R", CultureInfo.InvariantCulture)}\", ");
                writer.Write($"\"endValue\": \"{sortedProfileEvents.LastOrDefault().RelativeTime.ToString("R", CultureInfo.InvariantCulture)}\", ");
                writer.Write("\"events\": [ ");
                for (int i = 0; i < sortedProfileEvents.Count; i++)
                {
                    var frameEvent = sortedProfileEvents[i];

                    writer.Write($"{{ \"type\": \"{(frameEvent.Type == ProfileEventType.Open ? "O" : "C")}\", ");
                    writer.Write($"\"frame\": {frameEvent.FrameId}, ");
                    // "R" is crucial here!!! we can't loose precision because it can affect the sort order!!!!
                    writer.Write($"\"at\": {frameEvent.RelativeTime.ToString("R", CultureInfo.InvariantCulture)} }}");

                    if (i != sortedProfileEvents.Count - 1)
                        writer.Write(", ");
                }
                writer.Write("]");
                writer.Write("}");
            }

            writer.Write("] }");
        }
        #endregion private
    }

}
