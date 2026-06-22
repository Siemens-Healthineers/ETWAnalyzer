//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.EventDump;
using System;
using Xunit;

namespace ETWAnalyzer_uTest.EventDump
{
    public class TraceLoggingProviderFilterTests
    {
        static readonly Guid Provider1Guid = new("11111111-1111-1111-1111-111111111111");
        static readonly Guid Provider2Guid = new("22222222-2222-2222-2222-222222222222");

        [Fact]
        public void NoEventFilter_Matches_All_Events_Of_Matching_Provider()
        {
            TraceLoggingProviderFilter filter = new("prov1");

            Assert.True(filter.IsMatchingProvider("prov1", Provider1Guid));
            Assert.False(filter.IsMatchingProvider("prov2", Provider2Guid));

            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "AnyEvent", 5));
        }

        [Fact]
        public void Can_Filter_By_Event_Name_Substring()
        {
            TraceLoggingProviderFilter filter = new("prov1:Start");

            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "AppStart", 1));
            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "StartUp", 2));
            Assert.False(filter.IsMatchingEvent("prov1", Provider1Guid, "Shutdown", 3));
        }

        [Fact]
        public void Can_Filter_By_Multiple_Event_Names()
        {
            TraceLoggingProviderFilter filter = new("prov1:evName1,evName2");

            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "evName1", 1));
            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "evName2", 2));
            Assert.False(filter.IsMatchingEvent("prov1", Provider1Guid, "evName3", 3));
        }

        [Fact]
        public void Can_Filter_By_Event_Id()
        {
            TraceLoggingProviderFilter filter = new("prov1:10,20");

            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "AnyName", 10));
            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "AnyName", 20));
            Assert.False(filter.IsMatchingEvent("prov1", Provider1Guid, "AnyName", 30));
        }

        [Fact]
        public void Can_Mix_Event_Name_And_Id()
        {
            TraceLoggingProviderFilter filter = new("prov1:Start,42");

            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "AppStart", 1));
            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "Other", 42));
            Assert.False(filter.IsMatchingEvent("prov1", Provider1Guid, "Other", 7));
        }

        [Fact]
        public void Exclamation_Excludes_Event_Name()
        {
            TraceLoggingProviderFilter filter = new("prov1:!Verbose");

            Assert.False(filter.IsMatchingEvent("prov1", Provider1Guid, "VerboseTrace", 1));
            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "Error", 2));
        }

        [Fact]
        public void Exclamation_Excludes_Event_Id()
        {
            TraceLoggingProviderFilter filter = new("prov1:!100");

            Assert.False(filter.IsMatchingEvent("prov1", Provider1Guid, "AnyName", 100));
            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "AnyName", 200));
        }

        [Fact]
        public void Exclusion_Wins_Over_Inclusion()
        {
            TraceLoggingProviderFilter filter = new("prov1:Start,!FalseStart");

            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "AppStart", 1));
            Assert.False(filter.IsMatchingEvent("prov1", Provider1Guid, "FalseStart", 2));
        }

        [Fact]
        public void Can_Filter_Multiple_Providers_With_Separate_Event_Lists()
        {
            TraceLoggingProviderFilter filter = new("prov1:evName1,evName2;prov2:10,20");

            Assert.True(filter.IsMatchingProvider("prov1", Provider1Guid));
            Assert.True(filter.IsMatchingProvider("prov2", Provider2Guid));

            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "evName1", 99));
            Assert.False(filter.IsMatchingEvent("prov1", Provider1Guid, "other", 99));

            Assert.True(filter.IsMatchingEvent("prov2", Provider2Guid, "whatever", 10));
            Assert.False(filter.IsMatchingEvent("prov2", Provider2Guid, "whatever", 99));
        }

        [Fact]
        public void Provider_Without_Event_List_Matches_All_Its_Events()
        {
            TraceLoggingProviderFilter filter = new("prov1:Start;prov2");

            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "AppStart", 1));
            Assert.False(filter.IsMatchingEvent("prov1", Provider1Guid, "Stop", 2));

            Assert.True(filter.IsMatchingEvent("prov2", Provider2Guid, "AnyEvent", 5));
        }

        [Fact]
        public void Provider_Wildcards_Are_Supported()
        {
            TraceLoggingProviderFilter filter = new("prov*:Start");

            Assert.True(filter.IsMatchingProvider("provABC", Provider1Guid));
            Assert.True(filter.IsMatchingEvent("provABC", Provider1Guid, "AppStart", 1));
            Assert.False(filter.IsMatchingEvent("provABC", Provider1Guid, "Stop", 2));
        }

        [Fact]
        public void Provider_Exclusion_Filter_Is_Supported()
        {
            TraceLoggingProviderFilter filter = new("!prov2");

            Assert.True(filter.IsMatchingProvider("prov1", Provider1Guid));
            Assert.False(filter.IsMatchingProvider("prov2", Provider2Guid));
        }

        [Fact]
        public void Can_Match_Provider_By_Guid()
        {
            TraceLoggingProviderFilter filter = new($"{Provider1Guid}:Start");

            Assert.True(filter.IsMatchingProvider("prov1", Provider1Guid));
            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "AppStart", 1));
            Assert.False(filter.IsMatchingEvent("prov1", Provider1Guid, "Stop", 2));
        }

        [Fact]
        public void Event_Filter_Is_Case_Insensitive()
        {
            TraceLoggingProviderFilter filter = new("prov1:start");

            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "AppSTART", 1));
        }

        [Fact]
        public void HasEventFilterForProvider_Is_False_When_No_Event_List_Supplied()
        {
            TraceLoggingProviderFilter filter = new("prov1");

            Assert.False(filter.HasEventFilterForProvider("prov1", Provider1Guid));
        }

        [Fact]
        public void HasEventFilterForProvider_Is_True_When_Event_List_Supplied()
        {
            TraceLoggingProviderFilter filter = new("prov1:Start,Stop");

            Assert.True(filter.HasEventFilterForProvider("prov1", Provider1Guid));
        }

        [Fact]
        public void HasEventFilterForProvider_Distinguishes_Providers()
        {
            TraceLoggingProviderFilter filter = new("prov1:Start;prov2");

            Assert.True(filter.HasEventFilterForProvider("prov1", Provider1Guid));
            Assert.False(filter.HasEventFilterForProvider("prov2", Provider2Guid));
        }

        [Fact]
        public void Star_Event_Token_Matches_All_Events_And_Prints_Individually()
        {
            TraceLoggingProviderFilter filter = new("prov1:*");

            Assert.True(filter.HasEventFilterForProvider("prov1", Provider1Guid));
            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "AppStart", 1));
            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "Shutdown", 2));
        }

        [Fact]
        public void Wildcard_Event_Name_Is_Supported()
        {
            TraceLoggingProviderFilter filter = new("prov1:Start*");

            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "StartUp", 1));
            Assert.False(filter.IsMatchingEvent("prov1", Provider1Guid, "AppStart", 2));
        }

        [Fact]
        public void QuestionMark_Wildcard_Event_Name_Is_Supported()
        {
            TraceLoggingProviderFilter filter = new("prov1:Ev?");

            Assert.True(filter.IsMatchingEvent("prov1", Provider1Guid, "Ev1", 1));
            Assert.False(filter.IsMatchingEvent("prov1", Provider1Guid, "Event", 2));
        }
    }
}
