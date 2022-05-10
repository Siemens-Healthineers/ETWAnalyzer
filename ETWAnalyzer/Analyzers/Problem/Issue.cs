//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers
{
    /// <summary>
    /// Issue 
    /// </summary>
    class Issue
    {
        /// <summary>
        /// Analyzer name which did detect the 
        /// </summary>
        public string DetectedByAnalyzer
        {
            get;
        }

        /// <summary>
        /// Description of issue 
        /// </summary>
        public string Description
        {
            get;
        }

        /// <summary>
        /// Issue Category
        /// </summary>
        public Classification Category
        {
            get;
        }

        /// <summary>
        /// Severity
        /// </summary>
        public Severities Severity
        {
            get;
        }

        /// <summary>
        /// Link back to Testcase from where it did originate
        /// </summary>
        [JsonIgnore]
        public TestAnalysisResult Parent
        {
            get;
            internal set;
        }

        /// <summary>
        /// Create a new issue
        /// </summary>
        /// <param name="analyzer">Analyzer instance which found the issue</param>
        /// <param name="description">Description of issue</param>
        /// <param name="category"></param>
        /// <param name="severity"></param>
        public Issue(AnalyzerBase analyzer, string description, Classification category, Severities severity)
        {
            if( analyzer == null )
            {
                throw new ArgumentNullException($"{nameof(analyzer)}");
            }
            DetectedByAnalyzer = analyzer.GetType().Name;
            Description = description;
            Category = category;
            Severity = severity;
        }

        /// <summary>
        /// Create a new issue
        /// </summary>
        /// <param name="analyzer">Analyzer instance which found the issue</param>
        /// <param name="description">Description of issue</param>
        /// <param name="category"></param>
        /// <param name="severity"></param>
        /// <param name="details">Issue details</param>
        public Issue(AnalyzerBase analyzer, string description, Classification category, Severities severity, List<string> details)
        {
            if (analyzer == null)
            {
                throw new ArgumentNullException($"{nameof(analyzer)}");
            }
            DetectedByAnalyzer = analyzer.GetType().Name;
            Description = description;
            Category = category;
            Severity = severity;
            if( details != null)
            {
                Details = details;
            }
        }


        /// <summary>
        /// Additional details of current issue
        /// </summary>
        public IReadOnlyList<string> Details
        {
            get;
        } = new List<string>();


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Category} {Severity} {DetectedByAnalyzer} {Description}";
        }
    }
}
