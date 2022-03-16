using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers.Exception.Duration
{
    class ValueAnomalieDetector<TKey>
    {
        private double myFactorMulipliedWithQuartilDistanceToDetectAnomalieThreshold;

        private Dictionary<TKey, double> mySourceIdentificationWithDetectedAnomalieValues;
        public Dictionary<TKey, double> SourceIdentificationWithDetectedAnomalieValues 
            =>mySourceIdentificationWithDetectedAnomalieValues ??= SourceIdentificationKeyWithValues.Where(x => IsExceedingThreshold(x.Value)).ToDictionary(k => k.Key, v => v.Value);

        private Dictionary<TKey, double> mySourceIdentificationWithDetectedLowerAnomalieValues;
        public Dictionary<TKey, double> SourceIdentificationWithDetectedLowerAnomalieValues
            => mySourceIdentificationWithDetectedLowerAnomalieValues ??= SourceIdentificationKeyWithValues.Where(x => IsExceedingLowerThreshold(x.Value)).ToDictionary(k => k.Key, v => v.Value);

        private Dictionary<TKey, double> mySourceIdentificationWithDetectedUpperAnomalieValues;
        public Dictionary<TKey, double> SourceIdentificationWithDetectedHighAnomalieValues
            => mySourceIdentificationWithDetectedUpperAnomalieValues ??= SourceIdentificationKeyWithValues.Where(x => IsExceedingUpperThreshold(x.Value)).ToDictionary(k => k.Key, v => v.Value);
        private bool IsExceedingThreshold(double currValue) 
            => IsExceedingLowerThreshold(currValue) || IsExceedingUpperThreshold(currValue);
        private bool IsExceedingUpperThreshold(double currValue)
            => currValue > UpperWhishker;
        private bool IsExceedingLowerThreshold(double currValue)
            => currValue < LowerWhisker;

        public Dictionary<TKey, double> SourceIdentificationKeyWithValues { get; private set; }
        public Dictionary<TKey, double> OrderedSourceIdentificationKeyWithValues { get; private set; }

        public double Median { get; private set; }
        public double Median_25 { get; private set; }
        public double Median_75 { get; private set; }
        public double QuartilDistance { get; private set; }
        public double LowerWhisker  => Median_25 - myFactorMulipliedWithQuartilDistanceToDetectAnomalieThreshold * QuartilDistance;
        public double UpperWhishker => Median_75 + myFactorMulipliedWithQuartilDistanceToDetectAnomalieThreshold * QuartilDistance;
        public bool IsKeyOfLowValueAnomalie(TKey key)
            =>SourceIdentificationWithDetectedLowerAnomalieValues.TryGetValue(key, out var lowerValue);
        public bool IsKeyOfHighValueAnomalie(TKey key)
            =>SourceIdentificationWithDetectedHighAnomalieValues.TryGetValue(key, out var highValue);

        public ValueAnomalieDetector(Dictionary<TKey, double> sourceIdentificationKeyWithValues, double factorMulipliedWithQuartilDistanceToDetectAnomalieThreshold = 1.5)
        {
            CheckIfFactorIsValid(factorMulipliedWithQuartilDistanceToDetectAnomalieThreshold);

            myFactorMulipliedWithQuartilDistanceToDetectAnomalieThreshold = factorMulipliedWithQuartilDistanceToDetectAnomalieThreshold;
            SourceIdentificationKeyWithValues = sourceIdentificationKeyWithValues;
            OrderedSourceIdentificationKeyWithValues = SourceIdentificationKeyWithValues.OrderBy(x => x.Value).ToDictionary(k => k.Key, v => v.Value);

            List<double> orderedSources = OrderedSourceIdentificationKeyWithValues.Values.ToList();
            (double centerIdx, Median)  = GetCenterIdxAndMedianOf(orderedSources);

            int startIdx = 0;
            int countOfElements = GetIdxOfFirstElementInCenter(centerIdx) + 1;
            (double idx_25, Median_25) = GetCenterIdxAndMedianOf(orderedSources.GetRange(startIdx,countOfElements));

            startIdx = GetIdxOfSecondElementInCenter(centerIdx);
            countOfElements = orderedSources.Count - startIdx;
            (double idx_75, Median_75) = GetCenterIdxAndMedianOf(orderedSources.GetRange(startIdx, countOfElements));
            QuartilDistance = Median_75 - Median_25;
        }

        private int GetIdxOfFirstElementInCenter(double centerIdx)
            => (int)Math.Floor(centerIdx);

        private int GetIdxOfSecondElementInCenter(double centerIdx) 
            => (int)Math.Ceiling(centerIdx);

        /// <summary>
        /// Assoziates the center index with the median of the given list
        /// count of list elements is even  : index is in the center between two elements
        /// count of list elements is uneven: index is exactly in the center of the elements
        /// </summary>
        /// <param name="values">find centerIdx and median in this unsorted list</param>
        /// <returns>
        /// count of list elements is even:     centerIdx is x.5 because the two center elements have the index x and x+1 (example: list = {1,2} returns (centerIdx = 0.5, median = 1.5))
        /// count of list elements is uneven:   centerIdx is x because the value is exactly at the index x
        /// </returns>
        internal static (double centerIdx,double medianValue) GetCenterIdxAndMedianOf(List<double> values)
        {
            values = values.OrderBy(x => x).ToList();
            int zeroIfEven = values.Count % 2;
            double idxCenter = (values.Count-1) / 2.0 ;
            double valueCenter = values[(int)idxCenter];

            return (idxCenter, zeroIfEven == 0 ? (values[(int)idxCenter + 1] + valueCenter) / 2 : valueCenter);
        }

        public void SetNewAnomalieThreshold(double factorMulipliedWithQuartilDistanceToDetectAnomalieThreshold)
        {
            CheckIfFactorIsValid(factorMulipliedWithQuartilDistanceToDetectAnomalieThreshold);
            myFactorMulipliedWithQuartilDistanceToDetectAnomalieThreshold = factorMulipliedWithQuartilDistanceToDetectAnomalieThreshold;
        }

        private void CheckIfFactorIsValid(double factor)
        {
            if (factor <= 0) throw new ArgumentException($"The factor {factor} must be creater than 0 ");
        }
    }
}
