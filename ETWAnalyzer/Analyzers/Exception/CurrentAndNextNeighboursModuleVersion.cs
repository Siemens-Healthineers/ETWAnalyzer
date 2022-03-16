using ETWAnalyzer.Extensions;
using ETWAnalyzer.Extract;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers.Exception
{
    [Serializable]
    internal class CurrentAndNextNeighboursModuleVersion:IEquatable<CurrentAndNextNeighboursModuleVersion>
    {
        [JsonIgnore]
        public bool IsVersionFromFirstRunInTimeSeries
            => PreviousModuleVersion == null && CurrentModuleVersion != null;
        [JsonIgnore]
        public bool IsVersionFromLastRunInTimeSeries
            => FollowingModuleVersion == null && CurrentModuleVersion != null;

        [JsonIgnore]
        public bool CurrentEqualsPrevious => CurrentModuleVersion.Equals(PreviousModuleVersion);

        [JsonIgnore]
        public bool CurrentEqualsFollowing => CurrentModuleVersion.Equals(FollowingModuleVersion);

        public ModuleVersion CurrentModuleVersion { get; private set; }
        public ModuleVersion PreviousModuleVersion { get; private set; }
        public ModuleVersion FollowingModuleVersion { get; private set; }
        [JsonConstructor]
        public CurrentAndNextNeighboursModuleVersion(ModuleVersion currentModuleVersion, ModuleVersion previousModuleVersion, ModuleVersion followingModuleVersion)
        {
            CurrentModuleVersion = currentModuleVersion;
            PreviousModuleVersion = previousModuleVersion;
            FollowingModuleVersion = followingModuleVersion;
        }
        
        public CurrentAndNextNeighboursModuleVersion(TestDataFile source)
        {
            SetCurrentAndNextModulVersionFrom(source);
        }
        private void SetCurrentAndNextModulVersionFrom(TestDataFile testdatafile)
        {
            TestRun currRun = testdatafile.ParentTest.Parent;
            List<TestRun> runs = currRun.Parent.Runs.ToList();

            // Cannot compare the currRun with the source runs in the TestRunData because currRun is a reduced copy
            int currIdx = runs.FindIndex(x => x.TestRunStart.Equals(currRun.TestRunStart));

            PreviousModuleVersion = currIdx > 0 ? runs[currIdx-1].GetMainModuleVersion() : null;
            CurrentModuleVersion = runs[currIdx].GetMainModuleVersion();
            FollowingModuleVersion = currIdx < (runs.Count - 1) ? runs[currIdx + 1].GetMainModuleVersion() : null;
        }

        public bool Equals(CurrentAndNextNeighboursModuleVersion other)
        {
            return  CurrentModuleVersion.Equals(other.CurrentModuleVersion) &&
                    PreviousModuleVersion.Equals(other.PreviousModuleVersion) &&
                    FollowingModuleVersion.Equals(other.FollowingModuleVersion);
        }
    }
}
