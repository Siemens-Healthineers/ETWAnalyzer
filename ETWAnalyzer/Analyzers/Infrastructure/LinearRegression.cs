//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace ETWAnalyzer.Analyzers.Infrastructure
{
    class LinearRegression
    {
        List<Point> Function { get; }

        public string LinearEquation => "y = " + (SlopeOfTheLine == 0 ? "" : 
            ((FormattableString)$"{Math.Round(SlopeOfTheLine,2)}*runidx ").ToString(CultureInfo.InvariantCulture)) + 
            ((FormattableString)$"{( YAxisIntercept>=0 ? "+ " : "- " )}{Math.Round(Math.Abs(YAxisIntercept),2)}").ToString(CultureInfo.InvariantCulture);
        public double ArithmeticMeanOfXValues => Function.Average(x => x.X);
        public double ArithmeticMeanOfYValues => Function.Average(y => y.Y);
        public double SlopeOfTheLine 
        { 
            get
            {
                if(mySlopeOfTheLine == null)
                {
                    mySlopeOfTheLine = CalculateSlopeOfTheLine();
                }
                return (double)mySlopeOfTheLine;
            }
        }

        private double? mySlopeOfTheLine;

        public double YAxisIntercept 
        { 
            get 
            {
                if(myYAxisIntercept == null)
                {
                    myYAxisIntercept = ArithmeticMeanOfYValues - SlopeOfTheLine * ArithmeticMeanOfXValues;
                }
                return (double)myYAxisIntercept;
            }
        }
        private double? myYAxisIntercept;


        public LinearRegression(List<Point> function)
        {
            Function = function;
            mySlopeOfTheLine = null;
            myYAxisIntercept = null;
        }

        private double CalculateSlopeOfTheLine()
        {
            double numerator = 0;
            double denominator = 0;

            foreach (var point in Function)
            {
                double deltaToXMean = point.X - ArithmeticMeanOfXValues;
                double deltaToYMean = point.Y - ArithmeticMeanOfYValues;

                numerator += (deltaToXMean * deltaToYMean);
                denominator += (deltaToXMean*deltaToXMean);
            }
            double m = numerator / denominator;

            return Double.IsNaN(m) ? 0 : m;

        }


    }
}
