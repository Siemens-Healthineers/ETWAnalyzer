﻿//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extractors;
using Microsoft.Windows.EventTracing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ETWAnalyzer_uTest
{
    public class CPUPerProcessMethodListTests
    {
        [Fact]
        public void Can_Add_Methods_AndCutTooSmallMethods()
        {
            CPUPerProcessMethodList list = GetTestData();
            Assert.Equal(4, list.MethodNames.Count); // One method was cut off due to 10ms 

            Assert.Equal("Z", list.MethodNames[0]);

            list.SortMethodsByNameAndCPU();

            Assert.Equal("B", list.MethodNames[0]);
            Assert.Equal("X", list.MethodNames[1]);
            Assert.Equal("Y", list.MethodNames[2]);
            Assert.Equal("Z", list.MethodNames[3]);

            MethodsByProcess methods = list.MethodStatsPerProcess.First();

            Assert.Equal(20u,  methods.Costs[3].CPUMs);
            Assert.Equal("B", methods.Costs[3].Method);

            Assert.Equal(80u,  methods.Costs[2].CPUMs);
            Assert.Equal("X", methods.Costs[2].Method);

            Assert.Equal(90u,  methods.Costs[1].CPUMs);
            Assert.Equal("Y", methods.Costs[1].Method);

            Assert.Equal(100u, methods.Costs[0].CPUMs);
            Assert.Equal("Z", methods.Costs[0].Method);
        }

        CPUPerProcessMethodList GetTestData()
        {
            CPUPerProcessMethodList list = new CPUPerProcessMethodList();
            ProcessKey proc = new ProcessKey("test.exe", 1024, DateTimeOffset.MinValue);
            
            list.AddMethod(proc, "Z", new CpuData(new Duration(100_000_000), new Duration(101_000_000), 5m, 6m, 10, 5), cutOffMs:10);
            list.AddMethod(proc, "Y", new CpuData( new Duration(90_000_000), new Duration(91_000_000),  4m, 5m, 20, 4), cutOffMs:10);
            list.AddMethod(proc, "X", new CpuData( new Duration(80_000_000), new Duration(81_000_000),  3m, 5m, 30, 3), cutOffMs:10);
            list.AddMethod(proc, "B", new CpuData( new Duration(20_000_000), new Duration(21_000_000),  2m, 5m, 40, 2), cutOffMs:10);
            list.AddMethod(proc, "A", new CpuData( new Duration(10_000_000), new Duration(11_000_000),  1m, 5m, 50, 1), cutOffMs:10);

            return list;
        }

        [Fact]
        public void Can_Serialize_Deserialize_CPU_Stats()
        {
            ETWExtract extract = new ETWExtract();
            CPUPerProcessMethodList list = GetTestData();
            extract.CPU = new CPUStats(null, list);

            MemoryStream stream = new MemoryStream();
            ExtractSerializer.Serialize(stream, extract);
            stream.Position = 0;
            ETWExtract deserialized = ExtractSerializer.Deserialize<ETWExtract>(stream);

            CPUPerProcessMethodList cpu = deserialized.CPU.PerProcessMethodCostsInclusive;

            Assert.Equal(4, cpu.MethodNames.Count);
            Assert.Single(cpu.MethodStatsPerProcess);
            Dictionary<string, MethodCost> stats =  cpu.MethodStatsPerProcess[0].Costs.ToDictionary(x => x.Method);
            Assert.Equal(100u, stats["Z"].CPUMs);
            Assert.Equal(101u, stats["Z"].WaitMs);
            Assert.Equal(5.0f, stats["Z"].FirstOccurenceInSecond);
            Assert.Equal(6.0f, stats["Z"].LastOccurenceInSecond);
            Assert.Equal(10, stats["Z"].Threads);
            Assert.Equal(5, stats["Z"].DepthFromBottom);

            Assert.Equal(20u, stats["B"].CPUMs);
            Assert.Equal(21u, stats["B"].WaitMs);
        }
    }
}
