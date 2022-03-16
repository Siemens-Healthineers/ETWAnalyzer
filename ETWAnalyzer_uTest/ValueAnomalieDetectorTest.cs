using ETWAnalyzer.Analyzers.Exception.Duration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ETWAnalyzer_uTest
{
    public class ValueAnomalieDetectorTest
    {
        [Fact]
        public void Can_Determine_Median_Of_Uneven_Elementcount()
        {
            (double idx, double median) = ValueAnomalieDetector<object>.GetCenterIdxAndMedianOf(new List<double>() { 3, 2, 5, 4, 1 });
            Assert.Equal(3, median);
            Assert.Equal(2, idx);
        }
        [Fact]
        public void Can_Determine_Median_Of_Even_Elementcount()
        {
            (double idx, double median) = ValueAnomalieDetector<object>.GetCenterIdxAndMedianOf(new List<double>() { 3, 2, 4, 1 });
            Assert.Equal(2.5, median);
            Assert.Equal(1.5, idx);
        }
        [Fact]
        public void Can_Determine_Anomalie_In_Uneven_Elementcount()
        {
            Dictionary<string, double> unsortedValues = new()
            { { "Key1", 3.9 }, { "Key3", 9 }, { "Key4", 10 }, { "Key2", 8 }, { "Key5", 11 }, { "Key7", 16.1 }, { "Key6", 12 } };

            ValueAnomalieDetector<string> vd = new(unsortedValues);
            Assert.Equal(10, vd.Median);
            Assert.Equal(8.5, vd.Median_25);
            Assert.Equal(11.5, vd.Median_75);
            Assert.Equal(11.5 - 8.5, vd.QuartilDistance);
            Assert.Equal(4, vd.LowerWhisker);
            Assert.Equal(16, vd.UpperWhishker);
            Assert.Equal(2, vd.SourceIdentificationWithDetectedAnomalieValues.Count);
            Assert.True(vd.SourceIdentificationWithDetectedAnomalieValues.ContainsKey("Key1"));
            Assert.True(vd.SourceIdentificationWithDetectedAnomalieValues.ContainsKey("Key7"));
        }
        [Fact]
        public void Can_Determine_Anomalie_In_Uneven_Elementcount_Changing_Factor()
        {
            Dictionary<string, double> unsortedValues = new()
            { { "Key1", 3.9 }, { "Key3", 9 }, { "Key4", 10 }, { "Key2", 8 }, { "Key5", 11 }, { "Key7", 16.1 }, { "Key6", 12 } };

            ValueAnomalieDetector<string> vd = new(unsortedValues);
            vd.SetNewAnomalieThreshold(2);

            Assert.Equal(10, vd.Median);
            Assert.Equal(8.5, vd.Median_25);
            Assert.Equal(11.5, vd.Median_75);
            Assert.Equal(11.5 - 8.5, vd.QuartilDistance);
            Assert.Equal(2.5, vd.LowerWhisker);
            Assert.Equal(17.5, vd.UpperWhishker);
            Assert.Empty(vd.SourceIdentificationWithDetectedAnomalieValues);
        }

        [Fact]
        public void Can_Determine_Anomalie_In_Even_Elementcount()
        {
            Dictionary<string, double> unsortedValues = new()
            { { "Key1", 2.4 }, { "Key2", 8 }, { "Key4", 10 }, { "Key3", 9 }, { "Key5", 11 }, { "Key7", 13 },{ "Key6", 12 }, { "Key8", 18.6 } };

            ValueAnomalieDetector<string> vd = new(unsortedValues);
            Assert.Equal(10.5, vd.Median);
            Assert.Equal(8.5, vd.Median_25);
            Assert.Equal(12.5, vd.Median_75);
            Assert.Equal(12.5 - 8.5, vd.QuartilDistance);
            Assert.Equal(2.5, vd.LowerWhisker);
            Assert.Equal(18.5, vd.UpperWhishker);
            Assert.Equal(2, vd.SourceIdentificationWithDetectedAnomalieValues.Count);
            Assert.True(vd.SourceIdentificationWithDetectedAnomalieValues.ContainsKey("Key1"));
            Assert.True(vd.SourceIdentificationWithDetectedAnomalieValues.ContainsKey("Key8"));
        }
        [Fact]
        public void Can_Determine_Multiple_Lower_And_Upper_Anomalie_In_Even_Elementcount()
        {
            Dictionary<string, double> unsortedValues = new()
            {
                { "Key1", -4 }, { "Key2", -3.5 }, { "Key3", -3 }, { "Key4", 7 }, { "Key5", 8 }, { "Key6", 9 }, { "Key7", 10 },
                { "Key8", 11 }, { "Key9", 12 }, { "Key10", 13 }, { "Key11", 14 }, { "Key12", 24 }, { "Key13", 25 }, { "Key14", 26 }
            };

            ValueAnomalieDetector<string> vd = new(unsortedValues);
            Assert.Equal(10.5, vd.Median);
            Assert.Equal(7, vd.Median_25);
            Assert.Equal(14, vd.Median_75);
            Assert.Equal(7, vd.QuartilDistance);
            Assert.Equal(-3.5, vd.LowerWhisker);
            Assert.Equal(24.5, vd.UpperWhishker);

            Assert.Equal(3, vd.SourceIdentificationWithDetectedAnomalieValues.Count);
            Assert.True(vd.SourceIdentificationWithDetectedAnomalieValues.ContainsKey("Key1"));
            Assert.True(vd.SourceIdentificationWithDetectedAnomalieValues.ContainsKey("Key13"));
            Assert.True(vd.SourceIdentificationWithDetectedAnomalieValues.ContainsKey("Key14"));

            Assert.Single(vd.SourceIdentificationWithDetectedLowerAnomalieValues);
            Assert.True(vd.SourceIdentificationWithDetectedLowerAnomalieValues.ContainsKey("Key1"));

            Assert.Equal(2, vd.SourceIdentificationWithDetectedHighAnomalieValues.Count);
            Assert.True(vd.SourceIdentificationWithDetectedHighAnomalieValues.ContainsKey("Key13"));
            Assert.True(vd.SourceIdentificationWithDetectedHighAnomalieValues.ContainsKey("Key14"));
        }

    }
}
