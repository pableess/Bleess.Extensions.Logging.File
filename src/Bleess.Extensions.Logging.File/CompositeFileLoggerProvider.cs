using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        private ConcurrentDictionary<string, FileFormatter> _formatters;
        private readonly IOptionsMonitor<FileLoggerOptions> _providerOptions;
        private readonly IOptionsMonitor<CompositeLoggerFilterOptions> _compositeFilterOptions;
        private readonly IOptionsMonitor<LoggerFilterOptions> _filterOptions;

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
        public CompositeFileLoggerProvider(IEnumerable<ISubLoggerRegistration> subLoggerProviderNames,
            IOptionsMonitor<FileLoggerOptions> providerOptions,
            IOptionsMonitor<CompositeLoggerFilterOptions> compositeFilterOptions,
            IOptionsMonitor<LoggerFilterOptions> filterOptions,
            IEnumerable<FileFormatter> formatters)
        {
            // create all the providers
            _providers = new ConcurrentDictionary<string, FileLoggerProvider>(); 
            _loggers = new ConcurrentDictionary<string, CompositeFileLogger>(); 
            _compositeFilterOptions = compositeFilterOptions; 
            _filterOptions = filterOptions;
            _lastFilterOptions = _filterOptions.CurrentValue;

            _formatters = new ConcurrentDictionary<string, FileFormatter>(); 

            foreach (var f in formatters) 
            {
                _formatters.TryAdd(f.Name, f);
            }

            _providerOptions = providerOptions;

            var providers = subLoggerProviderNames.Select(p => p.Name).Distinct();

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
            foreach (var disposable in _changeHandlers) 
            {
                disposable?.Dispose();
            }

            _loggers.Clear();
            if (_providers != null)
            {
                foreach (var provider in _providers)
                {
                    provider.Value.Dispose();
                }
            }
            _providers?.Clear();
        }

        /// <inheritdoc/>
        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;

            foreach (var provider in _providers)
            {
                provider.Value.SetScopeProvider(scopeProvider);
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
            if (!options.Equals(_lastFilterOptions))
            {
                // todo: this could be more effient to actually see if anything has changed
                ApplyFilters(null);
                _lastFilterOptions = options;
            }
        }

        private void CreateSubProviders(IEnumerable<string> subProviders)
        {
            // add new providers
            if (subProviders != null)
            {
                foreach (var p in subProviders)
                {
                    if (!_providers.ContainsKey(p))
                    {
                        _providers.TryAdd(p, new FileLoggerProvider(_providerOptions, _formatters.Values, p));
                    }
                }
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
        /// 
        /// </summary>
        public void ApplyFilters(string? subProviderName) 
        {
            foreach (var logger in _loggers) 
            {
                // update the info
                if (logger.Value is CompositeFileLogger compositeLogger) 
                {
                    var loggerInfos = subProviderName == null ? compositeLogger.SubLoggers : compositeLogger.SubLoggers.Where(p => p.SubProviderName == subProviderName);

                    foreach (var info in loggerInfos)
                    {
                        CompositeLoggerRuleSelector.Select(CreateFilterOptionsForComposite(info.SubProviderName), info.SubProviderName, compositeLogger.Category, out var minLevel, out var filter);

                        // update the composite logger
                        compositeLogger.Update(info.SubProviderName, minLevel, filter);
                    }
                }
            }
        }
        
    }
}
