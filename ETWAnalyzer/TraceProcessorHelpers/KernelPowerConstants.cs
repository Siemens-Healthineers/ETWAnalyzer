//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract.Power;
using System;
using System.Collections.Generic;

namespace ETWAnalyzer.TraceProcessorHelpers
{
    internal class KernelPowerConstants
    {
        public static readonly Guid Guid = new Guid("331c3b3a-2005-44c2-ac5e-77220c37d6b4");
        public const int PowerSettingsRundownEventId = 111;

        public static readonly Dictionary<Guid, BasePowerProfile> BasePowerProfiles = new Dictionary<Guid, BasePowerProfile>()
        {
            { new Guid("381b4222-f694-41f0-9685-ff5bb260df2e"), BasePowerProfile.Balanced },
            { new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"), BasePowerProfile.HighPerformance },
            { new Guid("a1841308-3541-4fab-bc81-f71556f20b4a"), BasePowerProfile.PowerSaver },
            { new Guid("e9a42b02-d5df-448d-aa00-03f14749eb61"), BasePowerProfile.UltimatePerformance },
        };


    }
}
