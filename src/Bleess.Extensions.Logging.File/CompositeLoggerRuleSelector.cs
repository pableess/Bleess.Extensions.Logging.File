using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace Bleess.Extensions.Logging.File
{
    internal class CompositeLoggerRuleSelector
    {
        /// <summary>
        /// Selects a rule that applies to a sub logger.
        /// rules that apply 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="provider"></param>
        /// <param name="category"></param>
        /// <param name="minLevel"></param>
        /// <param name="filter"></param>
        public static void Select(LoggerFilterOptions options, string? provider, string category, out LogLevel? minLevel, out Func<string?, string?, LogLevel, bool>? filter)
        {
            filter = null;
            minLevel = options.MinLevel;

           // Filter rule selection:
           // 1. Select rules for current logger type, if there is none, select ones without logger type specified
           // 2. Select rules with longest matching categories
           // 3. If there nothing matched by category take all rules without category
           // 3. If there is only one rule use it's level and filter
           // 4. If there are multiple rules use last
           // 5. If there are no applicable rules use global minimal level

            LoggerFilterRule? current = null;
            foreach (LoggerFilterRule rule in options.Rules)
            {
                if (IsBetter(rule, current, provider, category)
                    || (!string.IsNullOrEmpty(provider) && IsBetter(rule, current, provider, category)))
                {
                    current = rule;
                }
            }

            if (current != null)
            {
                filter = current.Filter;
                minLevel = current.LogLevel;
            }
        }

        private static bool IsBetter(LoggerFilterRule rule, LoggerFilterRule? current, string? logger, string category)
        {
            // Skip rules with inapplicable type or category
            if (rule.ProviderName != null && rule.ProviderName != logger)
            {
                return false;
            }

            string? categoryName = rule.CategoryName;
            if (categoryName != null)
            {
                const char WildcardChar = '*';

                int wildcardIndex = categoryName.IndexOf(WildcardChar);
                if (wildcardIndex != -1 &&
                    categoryName.IndexOf(WildcardChar, wildcardIndex + 1) != -1)
                {
                    throw new InvalidOperationException("More than one wildcard");
                }

                ReadOnlySpan<char> prefix, suffix;
                if (wildcardIndex == -1)
                {
                    prefix = categoryName.AsSpan();
                    suffix = default;
                }
                else
                {
                    prefix = categoryName.AsSpan(0, wildcardIndex);
                    suffix = categoryName.AsSpan(wildcardIndex + 1);
                }

                if (!category.AsSpan().StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    !category.AsSpan().EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (current?.ProviderName != null)
            {
                if (rule.ProviderName == null)
                {
                    return false;
                }
            }
            else
            {
                // We want to skip category check when going from no provider to having provider
                if (rule.ProviderName != null)
                {
                    return true;
                }
            }

            if (current?.CategoryName != null)
            {
                if (rule.CategoryName == null)
                {
                    return false;
                }

                if (current.CategoryName.Length > rule.CategoryName.Length)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
