using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// A composite logging provider that can write to many files
    /// </summary>
    [ProviderAlias("Files")]
    public class CompositeFileLoggerProvider : ILoggerProvider, ISupportExternalScope, IDisposable
    {
        private readonly ConcurrentDictionary<string, FileLoggerProvider> _providers; // sub providers
        private readonly ConcurrentDictionary<string, CompositeFileLogger> _loggers; // logger cat -> composite logger
        private readonly IReadOnlyDictionary<string, FileFormatter> _formatters;
        private readonly IOptionsMonitor<FileLoggerOptions> _providerOptions;
        private readonly IOptionsMonitor<CompositeLoggerFilterOptions> _compositeFilterOptions;
        private readonly IOptionsMonitor<LoggerFilterOptions> _filterOptions;
        private readonly TimeProvider _timeProvider;

        private LoggerFilterOptions _lastFilterOptions;

        private readonly IList<IDisposable?> _changeHandlers;

        private IExternalScopeProvider? _scopeProvider;

        /// <summary>
        /// Creates the composite file logger provider
        /// </summary>
        /// <param name="subLoggerProviderNames">list of names for registered sub providers in a composite</param>
        /// <param name="providerOptions">Options for the provider (uses named options)</param>
        /// <param name="compositeFilterOptions">Special composite (sub provider) level filter options (uses named options)</param>
        /// <param name="filterOptions">Overall Filter options</param>
        /// <param name="formatters">The registered file formatters</param>
        /// <param name="timeProvider">The clock used for timestamps and date based rolling</param>
        public CompositeFileLoggerProvider(
            IEnumerable<ISubLoggerRegistration> subLoggerProviderNames,
            IOptionsMonitor<FileLoggerOptions> providerOptions,
            IOptionsMonitor<CompositeLoggerFilterOptions> compositeFilterOptions,
            IOptionsMonitor<LoggerFilterOptions> filterOptions,
            IEnumerable<FileFormatter> formatters,
            TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(subLoggerProviderNames);
            ArgumentNullException.ThrowIfNull(providerOptions);
            ArgumentNullException.ThrowIfNull(compositeFilterOptions);
            ArgumentNullException.ThrowIfNull(filterOptions);
            ArgumentNullException.ThrowIfNull(formatters);
            ArgumentNullException.ThrowIfNull(timeProvider);

            // create all the providers
            _providers = new ConcurrentDictionary<string, FileLoggerProvider>();
            _loggers = new ConcurrentDictionary<string, CompositeFileLogger>();
            _compositeFilterOptions = compositeFilterOptions;
            _filterOptions = filterOptions;
            _timeProvider = timeProvider;
            _lastFilterOptions = _filterOptions.CurrentValue;

            _formatters = formatters
                .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            _providerOptions = providerOptions;

            var providers = subLoggerProviderNames.Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            _changeHandlers = new List<IDisposable?>
            {
                compositeFilterOptions.OnChange(OnSubFiltersChange),
                filterOptions.OnChange(OnBaseFiltersChange)
            };

            // create the sub providers
            this.CreateSubProviders(providers);
        }

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, cn =>
            {
                var loggers = new List<SubFileLoggerInfo>();
                foreach (var provider in _providers)
                {
                    var logger = (FileLogger)provider.Value.CreateLogger(categoryName);
                    logger.SubProviderName = provider.Key;
                    CompositeLoggerRuleSelector.Select(CreateFilterOptionsForComposite(provider.Key), provider.Key, categoryName, out var minLevel, out var filter);
                    var info = new SubFileLoggerInfo(logger, provider.Key, minLevel, filter);

                    loggers.Add(info);
                }

                return new CompositeFileLogger(categoryName, loggers, _scopeProvider);

            });
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            foreach (var disposable in _changeHandlers)
            {
                disposable?.Dispose();
            }

            _loggers.Clear();
            foreach (var provider in _providers)
            {
                provider.Value.Dispose();
            }
            _providers.Clear();
        }

        /// <inheritdoc/>
        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;

            foreach (var provider in _providers)
            {
                provider.Value.SetScopeProvider(scopeProvider);
            }

            // loggers created before the scope provider was set must be updated as well
            foreach (var logger in _loggers)
            {
                logger.Value.ScopeProvider = scopeProvider;
            }
        }

        private void OnSubFiltersChange(CompositeLoggerFilterOptions options, string? subProviderName)
        {
            // apply the filters
            ApplyFilters(subProviderName);
        }

        private void OnBaseFiltersChange(LoggerFilterOptions options)
        {
            // apply the filters
            if (!ReferenceEquals(options, _lastFilterOptions))
            {
                // todo: this could be more effient to actually see if anything has changed
                ApplyFilters(null);
                _lastFilterOptions = options;
            }
        }

        private void CreateSubProviders(IEnumerable<string> subProviders)
        {
            // add new providers
            foreach (var p in subProviders)
            {
                _providers.TryAdd(p, new FileLoggerProvider(_providerOptions, _formatters.Values, _timeProvider, p));
            }
        }

        private LoggerFilterOptions CreateFilterOptionsForComposite(string subProvider)
        {
            var options = _filterOptions.CurrentValue;
            var clone = new LoggerFilterOptions
            {
                CaptureScopes = options.CaptureScopes,
                MinLevel = options.MinLevel,
            };

            foreach (var rule in options.Rules.Where(r => r.ProviderName == null))
            {
                clone.Rules.Add(rule);
            }

            var subProviderRules = _compositeFilterOptions.Get(subProvider);

            foreach (var rule in subProviderRules.Rules)
            {
                clone.Rules.Add(rule);
            }

            return clone;
        }

        /// <summary>
        /// Recomputes the effective min level and filter for the sub loggers.
        /// </summary>
        /// <param name="subProviderName">A single sub provider to update, or null to update all of them.</param>
        public void ApplyFilters(string? subProviderName)
        {
            foreach (var logger in _loggers)
            {
                if (logger.Value is not CompositeFileLogger compositeLogger)
                {
                    continue;
                }

                // compute the rule set once per sub provider and reuse it for every category
                var filterOptionsCache = new Dictionary<string, LoggerFilterOptions>(StringComparer.OrdinalIgnoreCase);

                LoggerFilterOptions FilterOptionsFor(string name)
                {
                    if (!filterOptionsCache.TryGetValue(name, out var cached))
                    {
                        cached = CreateFilterOptionsForComposite(name);
                        filterOptionsCache[name] = cached;
                    }

                    return cached;
                }

                if (subProviderName != null)
                {
                    CompositeLoggerRuleSelector.Select(FilterOptionsFor(subProviderName), subProviderName, compositeLogger.Category, out var minLevel, out var filter);
                    compositeLogger.Update(subProviderName, minLevel, filter);
                    continue;
                }

                foreach (var info in compositeLogger.SubLoggers)
                {
                    CompositeLoggerRuleSelector.Select(FilterOptionsFor(info.SubProviderName), info.SubProviderName, compositeLogger.Category, out var minLevel, out var filter);
                    compositeLogger.Update(info.SubProviderName, minLevel, filter);
                }
            }
        }
    }
}
