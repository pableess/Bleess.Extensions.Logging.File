using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// A composite logging provider that can write to many files
    /// </summary>
    [ProviderAlias("Files")]
    public class CompositeFileLoggerProvider : ILoggerProvider, ISupportExternalScope, IDisposable
    {
        private readonly Dictionary<string, FileLoggerProvider> _providers;
        private readonly ConcurrentDictionary<string, CompositeFileLogger> _loggers;
        private ConcurrentDictionary<string, FileFormatter> _formatters;
        private readonly IOptionsMonitor<FileLoggerOptions> _providerOptions;
        private readonly IOptionsMonitor<CompositeLoggerFilterOptions> _filterOptions;
        private readonly IDisposable? _change;

        private IExternalScopeProvider? _scopeProvider;

        /// <inheritdoc/>
        public CompositeFileLoggerProvider(IOptionsMonitor<CompositeFileLoggerProviderOptions> options, 
            IOptionsMonitor<FileLoggerOptions> providerOptions,
            IOptionsMonitor<CompositeLoggerFilterOptions> filterOptions,
            IEnumerable<FileFormatter> formatters)
        {
            // create all the providers
            _providers = new Dictionary<string, FileLoggerProvider>();
            _loggers = new ConcurrentDictionary<string, CompositeFileLogger>();

            _formatters = new ConcurrentDictionary<string, FileFormatter>();
            foreach (var f in formatters) 
            {
                _formatters.TryAdd(f.Name, f);
            }

            _providerOptions = providerOptions;

            var providers = options.CurrentValue.Providers;
            _change = options.OnChange(OnCompositeChange);

            this.OnCompositeChange(options.CurrentValue);            
            
        }

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, cn => 
            {
                var loggers = new List<FileLogger>();
                foreach (var provider in _providers)
                {
                    loggers.Add((FileLogger)provider.Value.CreateLogger(categoryName));
                }

                return new CompositeFileLogger(loggers, _scopeProvider);

            });
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _change?.Dispose();
            _loggers.Clear();
            if (_providers != null)
            {
                foreach (var provider in _providers)
                {
                    provider.Value.Dispose();
                }
            }
            _providers.Clear();
        }

        /// <inheritdoc/>
        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            foreach (var provider in _providers)
            {
                provider.Value.SetScopeProvider(scopeProvider);
            }
        }

        private void OnCompositeChange(CompositeFileLoggerProviderOptions options)
        {
            // add new providers
            if (options.Providers != null)
            {
                foreach (var p in options.Providers)
                {
                    if (!_providers.ContainsKey(p))
                    {
                        _providers.Add(p, new FileLoggerProvider(_providerOptions, _formatters.Values, p));
                    }
                }
            }

            // delete removed ones
            foreach (var provider in _providers.ToArray()) 
            {
                if (!options.Providers.Contains(provider.Key)) 
                {
                    _providers.Remove(provider.Key);
                    provider.Value.Dispose();

                }
            }

            
        }
        
    }
}
