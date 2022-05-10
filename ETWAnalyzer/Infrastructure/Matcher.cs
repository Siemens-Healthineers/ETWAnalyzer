//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ETWAnalyzer.Infrastructure
{
    [Flags]
    enum MatchingMode
    {
        CaseInsensitive = 0,
        CaseSensitive = 4
    }

    /// <summary>
    /// Matches strings with wildcards
    /// </summary>
    static class Matcher
    {
        /// <summary>
        /// Multiple Filters are split by ;
        /// </summary>
        static readonly char[] FilterSplit = new char[] { ';' };

        /// <summary>
        /// Split a filter string into separate parts which are separated by char in <see cref="FilterSplit"/>
        /// </summary>
        /// <param name="filter">Input filter string. Can be null</param>
        /// <returns>Array of filters or an empty arry if none present.</returns>
        public static string[] ParseFilterString(string filter)
        {
            return filter?.Split(FilterSplit, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        }

        /// <summary>
        /// Create a case insensitive filter which matches the supplied patterns. Multiple patterns are separated by;
        /// Exclusion patterns start with !
        /// Supported wildcards are * and ?
        /// </summary>
        /// <param name="pattern">Pattern string which can contain multiple patterns. Null is allowd.</param>
        /// <returns>Delegate which matches the pattern. Null strings as input are allowed and will not cause exceptions.</returns>
        public static Func<string, bool> CreateMatcher(string pattern)
        {
            return CreateMatcher(pattern, MatchingMode.CaseInsensitive);
        }


        /// <summary>
        /// Create a filter which matches the supplied patterns. Multiple patterns are separated by;
        /// Exclusion patterns start with !
        /// Supported wildcards are * and ?
        /// </summary>
        /// <param name="pattern">Pattern string which can contain multiple patterns. Null is allowd.</param>
        /// <param name="mode">mode</param>
        /// <param name="pidFilterFormat">When true and the input pattern is a number ddd the pattern is extended with *(ddd) to match the process names by id</param>
        /// <returns>Delegate which matches the pattern. Null strings as input are allowed and will not cause exceptions.</returns>
        public static Func<string,bool> CreateMatcher(string pattern, MatchingMode mode,bool pidFilterFormat=false)
        {
            string[] parts = ParseFilterString(pattern);

            List<Func<string, bool>> negativeFilters = new();
            List<Func<string, bool>> positiveFilters = new();
            foreach(string filter in parts)
            {
                if( filter.StartsWith("!"))
                {
                    string filterNoPrefix = filter.Substring(1);
                    Func<string,bool> negativeFilter = CreateFilter(filterNoPrefix, mode, pidFilterFormat);
                    negativeFilters.Add(negativeFilter);   
                }
                else
                {
                    positiveFilters.Add(CreateFilter(filter, mode, pidFilterFormat));
                }
            }

            return (x) =>
            {
                bool lret = positiveFilters.Count == 0 ? true : false;
                foreach (var pos in positiveFilters)
                {
                    lret = pos(x);
                    if (lret)
                    {
                        lret = true;
                        break;
                    }
                }
                if (lret)
                {
                    foreach (var neg in negativeFilters)
                    {
                        lret = !neg(x);
                        if (!lret)
                        {
                            break;
                        }
                    }
                }
                return lret;
            };
        }

        // If you want to implement both "*" and "?"
        private static String WildCardToRegular(String value, bool includeStartStop)
        {
            string pattern = Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*");

            string lret = includeStartStop ? "^" + pattern + "$" : pattern;

            return lret;
        }

        static Func<string,bool> CreateFilter(string singleFilter, MatchingMode mode, bool processMatchMode)
        {
            string[] parts = singleFilter.Split(new char[] { '*' });
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            var compMode = mode switch
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            {
                MatchingMode.CaseSensitive => RegexOptions.None,
                MatchingMode.CaseInsensitive => RegexOptions.IgnoreCase,
            };
            Regex rex = null;
            if (processMatchMode)
            {
                bool isPidFilter = int.TryParse(singleFilter, out int _);
                if( isPidFilter )
                {
                    rex = new Regex($"^.*\\({singleFilter}\\)$", compMode);
                }
                else
                {
                    rex = new Regex($"^{WildCardToRegular(singleFilter,false)}(.exe)?(\\(\\d+\\))?$", compMode);
                }
            }
            else
            {
                rex = new Regex(WildCardToRegular(singleFilter,true), compMode | RegexOptions.Multiline);
            }

            return x =>
            {
                return rex.IsMatch(x ?? "");
            };
        }

        static bool IsSet(this MatchingMode self, MatchingMode flag)
        {
            return (self & flag) == flag;
        }

        /// <summary>
        /// Match a list of patterns against a given string. If any of the patterns matches or if pattern is null or empty then true is returned.
        /// </summary>
        /// <param name="patterns">List of patterns.</param>
        /// <param name="mode"></param>
        /// <param name="str"></param>
        /// <returns>true if any pattern matches or if patterns is null or empty. False otherwise.</returns>
        public static bool IsMatch(string[] patterns, MatchingMode mode, string str)
        {
            if( patterns == null || patterns.Length == 0)
            {
                return true;
            }

            foreach(var pattern in patterns)
            {
                if( IsMatch(pattern, mode, str))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="mode"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsMatch(string pattern, MatchingMode mode, string str)
        {
            if( String.IsNullOrEmpty(pattern) || pattern == "*")
            {
                return true;
            }

            if (str == null)
            { 
                throw new ArgumentException($"{nameof(str)} input string was null");
            }

            string noStarpattern = pattern.Trim(new char[] { '*' });

            StringComparison strCompareMode = mode.IsSet(MatchingMode.CaseSensitive) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            Func<string, bool> comparer = s => noStarpattern.Equals(s, strCompareMode);


            if (pattern.StartsWith("*", StringComparison.Ordinal) && pattern.EndsWith("*", StringComparison.Ordinal))
            {
                comparer = s => s.IndexOf(noStarpattern, strCompareMode) != -1;
            }
            else if (pattern.StartsWith("*", StringComparison.Ordinal))
            {
                comparer = s => s.EndsWith(noStarpattern, strCompareMode);
            }
            else if (pattern.EndsWith("*", StringComparison.Ordinal))
            {
                comparer = s => s.StartsWith(noStarpattern, strCompareMode);
            }


            return comparer(str);

        }

    }
}
