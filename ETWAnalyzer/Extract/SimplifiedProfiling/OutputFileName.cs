///// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAU.Toolkit.Diagnostics.Profiling.Simplified
{
    /// <summary>
    /// class for generating the profiling result file name and support for parsing it
    /// Result files are named like: 
    /// %testCaseName%_%testduration%ms_%hostname%_(SRV|SLT|SINGLE)_TestStatus-(Unknown|Passed|Failed)_YYYYMMDD-HHMMSSzzz.7z
    /// e.g. P01Open_953ms_DEFOR09T142SRV_SRV_TestStatus-Passed_20201130-195317+1:00.7z
    /// </summary>
    public class OutputFileName
    {
        internal static readonly string MarkerForUpdatingTheDurationLater = "_0ms";
        /// <summary>
        /// the constructed filename or the input when parsed
        /// </summary>
        public string FileName { private set; get; }

        
        /// <summary/>
        public string TestCaseName { private set; get; }
        
        /// <summary/>
        public int TestDurationinMS { private set; get; }

        /// <summary/>
        public string MachineWhereResultsAreGeneratedOn { private set; get; }
        /// <summary/>
        public GeneratedAt GeneratedAt { private set; get; }
        
        /// <summary/>
        public TestStatus TestStatus { private set; get; }
        
        /// <summary/>
        public DateTime ProfilingStoppedTime { private set; get; }

        private OutputFileName()
        {

        }
        internal OutputFileName(ProfilingStopArgs profilingStopArgs)
        {
            TestCaseName = profilingStopArgs.TestCaseName;
            MachineWhereResultsAreGeneratedOn = Environment.MachineName;
            GeneratedAt = profilingStopArgs.GeneratedAt;
            TestStatus = profilingStopArgs.TestStatus;
            ProfilingStoppedTime = profilingStopArgs.StopTime;
            FileName = $"{TestCaseName}{MarkerForUpdatingTheDurationLater}_{MachineWhereResultsAreGeneratedOn}_{Enum.GetName(typeof(GeneratedAt), GeneratedAt)}" +
                $"_TestStatus-{Enum.GetName(typeof(TestStatus), TestStatus)}_{ProfilingStoppedTime.ToString(@"yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.etl";
        }

        /// <summary>
        /// decomposes the given file name to the seperate pieces
        /// </summary>
        /// <param name="filename">complete file name with out path</param>
        /// <returns>a new instance of <see cref="OutputFileName"/>, if the format is matching or null, if parsing was not sucessfull</returns>
        public static OutputFileName ParseFromFileName(string filename)
        {
            OutputFileName outputFileName = new OutputFileName
            {
                FileName = filename
            };
            bool success = true;

            try
            {
                var noext = Path.GetFileNameWithoutExtension(filename);
                var fragments = noext.Split(new string[] { "_" }, 6, StringSplitOptions.RemoveEmptyEntries);
                if (fragments.Length != 6)
                {
                    success = false;
                    goto exit;
                }
                outputFileName.TestCaseName = fragments[0];

                success = int.TryParse(fragments[1].Replace("ms", ""), out int ms);
                if( !success )
                {
                    goto exit;
                }
                outputFileName.TestDurationinMS = ms;

                outputFileName.MachineWhereResultsAreGeneratedOn = fragments[2];
                
                success = Enum.TryParse<GeneratedAt>(fragments[3], out GeneratedAt genAt);
                if (!success)
                {
                    goto exit;
                }

                outputFileName.GeneratedAt = genAt;
                
                success = Enum.TryParse<TestStatus>(fragments[4].Replace("TestStatus-", ""), out TestStatus testStatus);
                if (!success)
                {
                    goto exit;
                }

                outputFileName.TestStatus = testStatus;

                success = DateTime.TryParseExact(fragments[5], @"yyyyMMdd-HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime stopTime);
                if (!success)
                {
                    goto exit;
                }

                outputFileName.ProfilingStoppedTime = stopTime;
                return outputFileName;
            }
            catch(Exception)
            {
                //   Console.WriteLine($"could not parse the filename due to the following exception {e}");
                success = false;
            }

exit:
            return success ? outputFileName : null;
        }






    }
}
