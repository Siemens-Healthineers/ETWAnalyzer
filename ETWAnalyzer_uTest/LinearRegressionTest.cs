//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers.Exception;
using ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer;
using ETWAnalyzer.Analyzers.Infrastructure;
using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest
{
    public class LinearRegressionTest
    {
        CurrentAndNextNeighboursModuleVersion vTemp = new CurrentAndNextNeighboursModuleVersion(new ModuleVersion(), new ModuleVersion(), new ModuleVersion());
        (TestDataFile, TestDataFile, TestDataFile, TestDataFile, TestDataFile, TestDataFile, TestDataFile, TestDataFile, TestDataFile) GenerateTests()
        {
            var t0 = new TestDataFile("CallupAdhocColdReadingCR", "Test0.json", new DateTime(2000, 1, 1), 1000, 10, "Machine", null) { Extract = new ETWExtract() { MainModuleVersion = new ModuleVersion() } };
            var t1 = new TestDataFile("CallupAdhocColdReadingCR", "Test1.json", new DateTime(2000, 1, 2), 1000, 10, "Machine", null) { Extract = new ETWExtract() { MainModuleVersion = new ModuleVersion() } };
            var t2 = new TestDataFile("CallupAdhocColdReadingCR", "Test2.json", new DateTime(2000, 1, 3), 1000, 10, "Machine", null) { Extract = new ETWExtract() { MainModuleVersion = new ModuleVersion() } };
            var t3 = new TestDataFile("CallupAdhocColdReadingCR", "Test3.json", new DateTime(2000, 1, 4), 1000, 10, "Machine", null) { Extract = new ETWExtract() { MainModuleVersion = new ModuleVersion() } };
            var t4 = new TestDataFile("CallupAdhocColdReadingCR", "Test4.json", new DateTime(2000, 1, 5), 1000, 10, "Machine", null) { Extract = new ETWExtract() { MainModuleVersion = new ModuleVersion() } };
            var t5 = new TestDataFile("CallupAdhocColdReadingCR", "Test5.json", new DateTime(2000, 1, 6), 1000, 10, "Machine", null) { Extract = new ETWExtract() { MainModuleVersion = new ModuleVersion() } };
            var t6 = new TestDataFile("CallupAdhocColdReadingCR", "Test6.json", new DateTime(2000, 1, 7), 1000, 10, "Machine", null) { Extract = new ETWExtract() { MainModuleVersion = new ModuleVersion() } };
            var t7 = new TestDataFile("CallupAdhocColdReadingCR", "Test7.json", new DateTime(2000, 1, 8), 1000, 10, "Machine", null) { Extract = new ETWExtract() { MainModuleVersion = new ModuleVersion() } };
            var t8 = new TestDataFile("CallupAdhocColdReadingCR", "Test8.json", new DateTime(2000, 1, 9), 1000, 10, "Machine", null) { Extract = new ETWExtract() { MainModuleVersion = new ModuleVersion() } };
            return (t0, t1, t2, t3, t4, t5, t6, t7, t8);
        }


        [Fact]
        public void Slope_And_Intercept_Is_Zero()
        {
            List<Point> function = new()
            {
                new Point(0, 0),
                new Point(1, 0),
                new Point(2, 0),
                new Point(3, 0),
                new Point(4, 0),
                new Point(5, 0),
                new Point(6, 0),
                new Point(7, 0),
                new Point(8, 0),
            };
            LinearRegression linearRegression = new(function);
            Assert.Equal(0, linearRegression.SlopeOfTheLine);
            Assert.Equal(0, linearRegression.YAxisIntercept);
            Assert.Equal("y = + 0", linearRegression.LinearEquation);

        }
        [Fact]
        public void Slope_Is_Zero_Intercept_Is_One()
        {
            List<Point> function = new()
            {
                new Point(0, 1),
                new Point(1, 1),
                new Point(2, 1),
                new Point(3, 1),
                new Point(4, 1),
                new Point(5, 1),
                new Point(6, 1),
                new Point(7, 1),
                new Point(8, 1),
            };
            LinearRegression linearRegression = new(function);
            Assert.Equal(0, linearRegression.SlopeOfTheLine);
            Assert.Equal(1, linearRegression.YAxisIntercept);
            Assert.Equal("y = + 1",linearRegression.LinearEquation);
        }
        [Fact]
        public void Slope_Is_Zero_Intercep_Is_One()
        {
            List<Point> function = new()
            {
                new Point(0, 1),
                new Point(1, 1),
                new Point(2, 1),
                new Point(3, 1),
                new Point(4, 1),
                new Point(5, 1),
                new Point(6, 1),
                new Point(7, 1),
                new Point(8, 1),
            };
            LinearRegression linearRegression = new(function);
            Assert.Equal("y = + 1", linearRegression.LinearEquation);
        }
        [Fact]
        public void Slope_Of_Alternating_Function()
        {
            List<Point> function = new()
            {
                new Point(0, 1),
                new Point(1, 0),
                new Point(2, 1),
                new Point(3, 0),
                new Point(4, 1),
                new Point(5, 0),
                new Point(6, 1),
                new Point(7, 0),
                new Point(8, 1),
            };
            LinearRegression linearRegression = new(function);
            Assert.Equal("y = + 0.56", linearRegression.LinearEquation);
        }
        [Fact]
        public void Pos_Slope_Pos_Intercept()
        {
            List<Point> function = new()
            {
                new Point(0, 0),
                new Point(1, 1),
                new Point(2, 0),
                new Point(3, 0),
                new Point(4, 0),
                new Point(5, 0),
                new Point(6, 0),
                new Point(7, 1),
                new Point(8, 1),
            };
            LinearRegression linearRegression = new(function);
            Assert.Equal("y = 0.07*runidx + 0.07", linearRegression.LinearEquation);
        }
        [Fact]
        public void Pos_Slope_Neg_Intercept()
        {
            List<Point> function = new()
            {
                new Point(0, 0),
                new Point(1, 0),
                new Point(2, 0),
                new Point(3, 0),
                new Point(4, 0),
                new Point(5, 0),
                new Point(6, 0),
                new Point(7, 1),
                new Point(8, 0),
            };
            LinearRegression linearRegression = new(function);
            Assert.Equal("y = 0.05*runidx - 0.09", linearRegression.LinearEquation);
        }
        [Fact]
        public void Neg_Slope_Neg_Intercept()
        {
            List<Point> function = new()
            {
                new Point(0, 0),
                new Point(1, -1),
                new Point(2, 0),
                new Point(3, 0),
                new Point(4, 0),
                new Point(5, 0),
                new Point(6, 0),
                new Point(7, -1),
                new Point(8, -1),
            };
            LinearRegression linearRegression = new(function);
            Assert.Equal("y = -0.07*runidx - 0.07", linearRegression.LinearEquation);
        }
        [Fact]
        public void Neg_Slope_Pos_Intercept()
        {
            List<Point> function = new()
            {
                new Point(0, 0),
                new Point(1, 1),
                new Point(2, 0),
                new Point(3, 0),
                new Point(4, 0),
                new Point(5, 0),
                new Point(6, 0),
                new Point(7, 0),
                new Point(8, 0),
            };
            LinearRegression linearRegression = new(function);
            Assert.Equal("y = -0.05*runidx + 0.31", linearRegression.LinearEquation);
        }

        private ExceptionSourceFileWithNextNeighboursModuleVersion[] GenerateSourcesFor(params TestDataFile[] testDataFiles)
        {
            return testDataFiles.ToList().Select(x => GenerateSource(x)).ToArray();
        }
        private ExceptionSourceFileWithNextNeighboursModuleVersion GenerateSource(TestDataFile generateFor)
        {
            return new ExceptionSourceFileWithNextNeighboursModuleVersion(generateFor, new CurrentAndNextNeighboursModuleVersion(new ModuleVersion(), new ModuleVersion(), new ModuleVersion()), ExceptionCluster.UndefineAble);
        }
        private ExceptionSourceFileWithNextNeighboursModuleVersion GenerateDiffSourceAsCluster(TestDataFile generateFor,ExceptionCluster generateAsCluster)
        {
            return new ExceptionSourceFileWithNextNeighboursModuleVersion(generateFor, new CurrentAndNextNeighboursModuleVersion(new ModuleVersion(), new ModuleVersion(), new ModuleVersion()), generateAsCluster);
        }


        [Fact]
        public void From_TimeSeriesData_Pos_Slope_Pos_Intercept()
        {
            var (t0, t1, t2, t3, t4, t5, t6, t7, t8) = GenerateTests();

            TestRun[] runs = TestRun.CreateForSpecifiedFiles(new List<TestDataFile>() { t0, t1, t2, t3, t4, t5, t6, t7, t8 });

            List<Point> function = TimeSeriesToMathematicalFunctionAdapter.GenerateTimeAndValueDiscretFunction(GenerateSourcesFor(t1,t7,t8), runs.ToList());
            Assert.Equal("y = 0.07*runidx + 0.07", new LinearRegression(function).LinearEquation);
        }

        [Fact]
        public void From_TimeSeriesDifferentiatedData_Pos_Slope_Pos_Intercept()
        {
            var (t0, t1, t2, t3, t4, t5, t6, t7, t8) = GenerateTests();

            TestRun[] runs = TestRun.CreateForSpecifiedFiles(new List<TestDataFile>() { t0, t1, t2, t3, t4, t5, t6, t7, t8 });

            ExceptionSourceFileWithNextNeighboursModuleVersion[] diffData =  {  GenerateDiffSourceAsCluster(t1, ExceptionCluster.OutlierException),
                                                                                GenerateDiffSourceAsCluster(t7,ExceptionCluster.StartingException) };

            List<Point> function = TimeSeriesToMathematicalFunctionAdapter.GenerateTimeAndValueDiscretFunctionFromDifferentiatedExceptionData(diffData, runs.ToList());
            Assert.Equal("y = 0.07*runidx + 0.07", new LinearRegression(function).LinearEquation);
        }
        [Fact]
        public void From_TimeSeriesData_Pos_Slope_Neg_Intercept()
        {
            var (t0, t1, t2, t3, t4, t5, t6, t7, t8) = GenerateTests();

            TestRun[] runs = TestRun.CreateForSpecifiedFiles(new List<TestDataFile>() { t0, t1, t2, t3, t4, t5, t6, t7, t8 });

            List<Point> function = TimeSeriesToMathematicalFunctionAdapter.GenerateTimeAndValueDiscretFunction(GenerateSourcesFor(t7), runs.ToList());
            Assert.Equal("y = 0.05*runidx - 0.09", new LinearRegression(function).LinearEquation);
        }
        [Fact]
        public void From_TimeSeriesDifferentiatedData_Pos_Slope_Neg_Intercept()
        {
            var (t0, t1, t2, t3, t4, t5, t6, t7, t8) = GenerateTests();

            TestRun[] runs = TestRun.CreateForSpecifiedFiles(new List<TestDataFile>() { t0, t1, t2, t3, t4, t5, t6, t7, t8 });

            ExceptionSourceFileWithNextNeighboursModuleVersion[] diffData = { GenerateDiffSourceAsCluster(t7, ExceptionCluster.OutlierException) };

            List<Point> function = TimeSeriesToMathematicalFunctionAdapter.GenerateTimeAndValueDiscretFunctionFromDifferentiatedExceptionData(diffData, runs.ToList());
            Assert.Equal("y = 0.05*runidx - 0.09", new LinearRegression(function).LinearEquation);
        }

        [Fact]
        public void From_TimeSeriesData_Neg_Slope_Pos_Intercept()
        {
            var (t0, t1, t2, t3, t4, t5, t6, t7, t8) = GenerateTests();
            TestRun[] runs = TestRun.CreateForSpecifiedFiles(new List<TestDataFile>() { t0, t1, t2, t3, t4, t5, t6, t7, t8 });

            List<Point> function = TimeSeriesToMathematicalFunctionAdapter.GenerateTimeAndValueDiscretFunction(GenerateSourcesFor(t1), runs.ToList());
            Assert.Equal("y = -0.05*runidx + 0.31", new LinearRegression(function).LinearEquation);
        }

        [Fact]
        public void From_TimeSeriesDifferentiatedData_Neg_Slope_Pos_Intercept()
        {
            var (t0, t1, t2, t3, t4, t5, t6, t7, t8) = GenerateTests();
            TestRun[] runs = TestRun.CreateForSpecifiedFiles(new List<TestDataFile>() { t0, t1, t2, t3, t4, t5, t6, t7, t8 });

            ExceptionSourceFileWithNextNeighboursModuleVersion[] diffData = { GenerateDiffSourceAsCluster(t1, ExceptionCluster.OutlierException) };

            List<Point> function = TimeSeriesToMathematicalFunctionAdapter.GenerateTimeAndValueDiscretFunctionFromDifferentiatedExceptionData(diffData, runs.ToList());
            Assert.Equal("y = -0.05*runidx + 0.31", new LinearRegression(function).LinearEquation);
        }

        [Fact]
        public void DiffData_Equals_SourceData_Exception_Active_At_The_Start()
        {
            var (t0, t1, t2, t3, t4, t5, t6, t7, t8) = GenerateTests();
            TestRun[] runs = TestRun.CreateForSpecifiedFiles(new List<TestDataFile>() { t0, t1, t2, t3, t4, t5, t6, t7, t8 });

            ExceptionSourceFileWithNextNeighboursModuleVersion[] sourceData = GenerateSourcesFor(t0, t1);
            ExceptionSourceFileWithNextNeighboursModuleVersion[] diffData =     { GenerateDiffSourceAsCluster(t1, ExceptionCluster.EndingException) };            
            
            List<Point> functionOfSources = TimeSeriesToMathematicalFunctionAdapter.GenerateTimeAndValueDiscretFunction(sourceData, runs.ToList());
            List<Point> functionOfDiffs = TimeSeriesToMathematicalFunctionAdapter.GenerateTimeAndValueDiscretFunctionFromDifferentiatedExceptionData(diffData, runs.ToList());


            Assert.Equal(new LinearRegression(functionOfSources).LinearEquation, new LinearRegression(functionOfDiffs).LinearEquation);
        }

        [Fact]
        public void DiffData_Equals_SourceData_Exception_Active_At_The_End()
        {
            var (t0, t1, t2, t3, t4, t5, t6, t7, t8) = GenerateTests();
            TestRun[] runs = TestRun.CreateForSpecifiedFiles(new List<TestDataFile>() { t0, t1, t2, t3, t4, t5, t6, t7, t8 });

            ExceptionSourceFileWithNextNeighboursModuleVersion[] sourceData = GenerateSourcesFor(t7, t8);
            ExceptionSourceFileWithNextNeighboursModuleVersion[] diffData = { GenerateDiffSourceAsCluster(t7, ExceptionCluster.StartingException) };

            List<Point> functionOfSources = TimeSeriesToMathematicalFunctionAdapter.GenerateTimeAndValueDiscretFunction(sourceData, runs.ToList());
            List<Point> functionOfDiffs = TimeSeriesToMathematicalFunctionAdapter.GenerateTimeAndValueDiscretFunctionFromDifferentiatedExceptionData(diffData, runs.ToList());

            Assert.Equal(new LinearRegression(functionOfSources).LinearEquation, new LinearRegression(functionOfDiffs).LinearEquation);
        }

        [Fact]
        public void DiffData_Equals_SourceData_Exception_Active_At_The_Start_Middle_End()
        {
            var (t0, t1, t2, t3, t4, t5, t6, t7, t8) = GenerateTests();
            TestRun[] runs = TestRun.CreateForSpecifiedFiles(new List<TestDataFile>() { t0, t1, t2, t3, t4, t5, t6, t7, t8 });

            ExceptionSourceFileWithNextNeighboursModuleVersion[] sourceData = GenerateSourcesFor(t0, t1, t3, t4, t5, t8);
            ExceptionSourceFileWithNextNeighboursModuleVersion[] diffData = { GenerateDiffSourceAsCluster(t1, ExceptionCluster.EndingException),
                                                                              GenerateDiffSourceAsCluster(t3, ExceptionCluster.StartingException),
                                                                              GenerateDiffSourceAsCluster(t5,ExceptionCluster.EndingException),
                                                                              GenerateDiffSourceAsCluster(t8,ExceptionCluster.UndefineAble)         };

            List<Point> functionOfSources = TimeSeriesToMathematicalFunctionAdapter.GenerateTimeAndValueDiscretFunction(sourceData, runs.ToList());
            List<Point> functionOfDiffs = TimeSeriesToMathematicalFunctionAdapter.GenerateTimeAndValueDiscretFunctionFromDifferentiatedExceptionData(diffData, runs.ToList());

            Assert.Equal(new LinearRegression(functionOfSources).LinearEquation, new LinearRegression(functionOfDiffs).LinearEquation);
        }



    }
}
