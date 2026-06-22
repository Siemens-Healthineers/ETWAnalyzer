//// SPDX-FileCopyrightText:  © 2026 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ETWAnalyzer.EventDump
{
    /// <summary>
    /// Filter for the <c>-Dump TraceLog -Provider</c> command line option.
    /// It filters by provider name/Guid and optionally by a comma separated list of event names or event ids per provider.
    /// Syntax: <c>prov1:evName1,evName2;prov2:id1,id2</c>
    /// <list type="bullet">
    /// <item>Multiple provider filters are separated by ;</item>
    /// <item>The optional event filter list follows the provider name/Guid after a : and is separated by ,</item>
    /// <item>Provider name/Guid and event name/id filters can be negated with a leading !</item>
    /// <item>Provider names support the wildcards * and ?</item>
    /// <item>Event ids are matched exactly while event names are matched as case insensitive substrings.</item>
    /// </list>
    /// </summary>
    internal class TraceLoggingProviderFilter
    {
        /// <summary>
        /// Event filters of a single provider clause are separated by ,
        /// </summary>
        static readonly char[] EventSplitChars = new char[] { ',' };

        /// <summary>
        /// Original filter string as it was passed on the command line.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Matches the provider name or Guid string against the positive (include) provider patterns.
        /// When no positive pattern was supplied all providers match.
        /// </summary>
        readonly Func<string, bool> myPositiveProviderMatcher;

        /// <summary>
        /// True when at least one positive (include) provider pattern was supplied.
        /// </summary>
        readonly bool myHasPositiveProviderPattern;

        /// <summary>
        /// Matches the provider name or Guid string against the negative (exclude) provider patterns.
        /// A provider is filtered out when its name or Guid matches.
        /// </summary>
        readonly Func<string, bool> myNegativeProviderMatcher;

        /// <summary>
        /// True when at least one negative (exclude) provider pattern was supplied.
        /// </summary>
        readonly bool myHasNegativeProviderPattern;

        /// <summary>
        /// Per provider event filters. Only clauses which contain at least one event filter are added here.
        /// </summary>
        readonly List<EventFilterClause> myEventClauses = new();

        /// <summary>
        /// Parse the -Provider filter string.
        /// </summary>
        /// <param name="filter">Filter string. E.g. prov1:evName1,evName2;prov2:id1,id2</param>
        public TraceLoggingProviderFilter(string filter)
        {
            Value = filter;

            List<string> positiveProviderPatterns = new();
            List<string> negativeProviderPatterns = new();

            foreach (string clause in Matcher.ParseFilterString(filter))
            {
                int colonIdx = clause.IndexOf(':');
                string providerPattern = colonIdx < 0 ? clause : clause.Substring(0, colonIdx);
                string eventList = colonIdx < 0 ? null : clause.Substring(colonIdx + 1);

                if (providerPattern.StartsWith("!", StringComparison.Ordinal))
                {
                    negativeProviderPatterns.Add(providerPattern.Substring(1));
                }
                else
                {
                    positiveProviderPatterns.Add(providerPattern);

                    if (!String.IsNullOrWhiteSpace(eventList))
                    {
                        EventFilterClause eventClause = new(Matcher.CreateMatcher(providerPattern));
                        foreach (string token in eventList.Split(EventSplitChars, StringSplitOptions.RemoveEmptyEntries))
                        {
                            eventClause.AddToken(token.Trim());
                        }

                        if (eventClause.HasTokens)
                        {
                            myEventClauses.Add(eventClause);
                        }
                    }
                }
            }

            myHasPositiveProviderPattern = positiveProviderPatterns.Count > 0;
            myHasNegativeProviderPattern = negativeProviderPatterns.Count > 0;
            myPositiveProviderMatcher = Matcher.CreateMatcher(String.Join(";", positiveProviderPatterns));
            myNegativeProviderMatcher = Matcher.CreateMatcher(String.Join(";", negativeProviderPatterns));
        }

        /// <summary>
        /// Check if a provider matches the filter by its name or Guid.
        /// </summary>
        /// <param name="providerName">Provider name.</param>
        /// <param name="providerGuid">Provider Guid.</param>
        /// <returns>true when the provider matches, false otherwise.</returns>
        public bool IsMatchingProvider(string providerName, Guid providerGuid)
        {
            string providerGuidStr = providerGuid.ToString();

            bool included = !myHasPositiveProviderPattern || myPositiveProviderMatcher(providerName) || myPositiveProviderMatcher(providerGuidStr);
            bool excluded = myHasNegativeProviderPattern && (myNegativeProviderMatcher(providerName) || myNegativeProviderMatcher(providerGuidStr));

            return included && !excluded;
        }

        /// <summary>
        /// Check if an explicit event filter list (event names or ids) was supplied for a provider.
        /// When false a matching provider should print an event summary instead of its individual events.
        /// </summary>
        /// <param name="providerName">Provider name.</param>
        /// <param name="providerGuid">Provider Guid.</param>
        /// <returns>true when at least one event name or id filter was supplied for the provider, false otherwise.</returns>
        public bool HasEventFilterForProvider(string providerName, Guid providerGuid)
        {
            string providerGuidStr = providerGuid.ToString();

            foreach (EventFilterClause clause in myEventClauses)
            {
                if (clause.ProviderMatcher(providerName) || clause.ProviderMatcher(providerGuidStr))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if an event matches the filter. When no event filter was supplied for the matching provider all events match.
        /// </summary>
        /// <param name="providerName">Provider name of the event.</param>
        /// <param name="providerGuid">Provider Guid of the event.</param>
        /// <param name="eventName">Event name.</param>
        /// <param name="eventId">Event id.</param>
        /// <returns>true when the event should be shown, false when it is filtered out.</returns>
        public bool IsMatchingEvent(string providerName, Guid providerGuid, string eventName, int eventId)
        {
            if (myEventClauses.Count == 0)
            {
                return true;
            }

            string providerGuidStr = providerGuid.ToString();
            bool anyClauseForProvider = false;
            bool hasPositive = false;
            bool matchedPositive = false;

            foreach (EventFilterClause clause in myEventClauses)
            {
                if (!(clause.ProviderMatcher(providerName) || clause.ProviderMatcher(providerGuidStr)))
                {
                    continue;
                }

                anyClauseForProvider = true;

                foreach (Func<string, int, bool> neg in clause.NegativeEventFilters)
                {
                    if (neg(eventName, eventId))
                    {
                        return false;
                    }
                }

                foreach (Func<string, int, bool> pos in clause.PositiveEventFilters)
                {
                    hasPositive = true;
                    if (pos(eventName, eventId))
                    {
                        matchedPositive = true;
                    }
                }
            }

            if (!anyClauseForProvider)
            {
                return true;
            }

            if (hasPositive)
            {
                return matchedPositive;
            }

            return true;
        }

        /// <summary>
        /// Event filters of a single provider clause.
        /// </summary>
        class EventFilterClause
        {
            /// <summary>
            /// Matches the provider name or Guid this clause applies to.
            /// </summary>
            public Func<string, bool> ProviderMatcher { get; }

            /// <summary>
            /// Include event filters. An event matches when at least one of them matches.
            /// </summary>
            public List<Func<string, int, bool>> PositiveEventFilters { get; } = new();

            /// <summary>
            /// Exclude event filters. An event is filtered out when any of them matches.
            /// </summary>
            public List<Func<string, int, bool>> NegativeEventFilters { get; } = new();

            /// <summary>
            /// True when at least one event filter was added.
            /// </summary>
            public bool HasTokens => PositiveEventFilters.Count > 0 || NegativeEventFilters.Count > 0;

            public EventFilterClause(Func<string, bool> providerMatcher)
            {
                ProviderMatcher = providerMatcher;
            }

            /// <summary>
            /// Add a single event filter token. Numeric tokens are matched against the event id, all other tokens
            /// are matched as a case insensitive substring against the event name. A leading ! negates the filter.
            /// </summary>
            /// <param name="token">Event filter token.</param>
            public void AddToken(string token)
            {
                if (String.IsNullOrEmpty(token))
                {
                    return;
                }

                bool exclude = token.StartsWith("!", StringComparison.Ordinal);
                if (exclude)
                {
                    token = token.Substring(1);
                }

                if (token.Length == 0)
                {
                    return;
                }

                Func<string, int, bool> predicate;
                if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                {
                    predicate = (name, eventId) => eventId == id;
                }
                else if (token.IndexOf('*') >= 0 || token.IndexOf('?') >= 0)
                {
                    Func<string, bool> wildcardMatcher = Matcher.CreateMatcher(token);
                    predicate = (name, eventId) => wildcardMatcher(name);
                }
                else
                {
                    string needle = token;
                    predicate = (name, eventId) => name != null && name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
                }

                if (exclude)
                {
                    NegativeEventFilters.Add(predicate);
                }
                else
                {
                    PositiveEventFilters.Add(predicate);
                }
            }
        }
    }
}
