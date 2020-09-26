namespace Bleess.Extensions.Logging.File
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;


    /// <summary>
    /// A rolling file log provider 
    /// </summary>
    [ProviderAlias("File")]
    public sealed class FileLoggerProvider : ILoggerProvider, ISupportExternalScope, IDisposable
    {
        private readonly IOptionsMonitor<FileLoggerOptions> options;
        private IDisposable optionsReloadToken;
        private IExternalScopeProvider scopeProvider  = NullScopeProvider.Instance;
        private readonly ConcurrentDictionary<string, FileLogger> loggers;
        private ConcurrentDictionary<string, FileFormatter> formatters;
        private readonly FileLoggerProcessor messageQueue;

        /// Creates an instance of <see cref="FileLoggerProvider"/>.
        /// </summary>
        /// <param name="options">The options to create <see cref="FileLogger"/> instances with.</param>
        /// <param name="formatters">Log formatters added for <see cref="FileLogger"/> insteaces.</param>
        public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> options, IEnumerable<FileFormatter> formatters)
        {
            this.options = options;
            this.loggers = new ConcurrentDictionary<string, FileLogger>();

            SetFormatters(formatters);
            this.messageQueue = new FileLoggerProcessor(options.CurrentValue);

            optionsReloadToken = this.options.OnChange(ReloadLoggerOptions);

            ReloadFormatters(options.CurrentValue);
        }

        private void SetFormatters(IEnumerable<FileFormatter> formatters = null)
        {
            this.formatters = new ConcurrentDictionary<string, FileFormatter>(StringComparer.OrdinalIgnoreCase);
            if (formatters == null || !formatters.Any())
            {
                var defaultMonitor = new FormatterOptionsMonitor<SimpleFileFormatterOptions>(new SimpleFileFormatterOptions());
                var jsonMonitor = new FormatterOptionsMonitor<JsonFileFormatterOptions>(new JsonFileFormatterOptions());
                this.formatters.GetOrAdd(FileFormatterNames.Simple, formatterName => new SimpleFileFormatter(defaultMonitor));
                this.formatters.GetOrAdd(FileFormatterNames.Json, formatterName => new JsonFileFormatter(jsonMonitor));
            }
            else
            {
                foreach (FileFormatter formatter in formatters)
                {
                    this.formatters.GetOrAdd(formatter.Name, formatterName => formatter);
                }
            }
        }

        private void ReloadLoggerOptions(FileLoggerOptions options)
        {
            this.messageQueue.ConfigureWriter(options);
            ReloadFormatters(options);

        }

        private void ReloadFormatters(FileLoggerOptions options)
        {
            this.formatters.TryGetValue(this.options.CurrentValue.FormatterName, out var logFormatter);

            UpdateFormatterOptions(logFormatter, options);

            foreach (KeyValuePair<string, FileLogger> logger in this.loggers)
            {
                logger.Value.Formatter = logFormatter;
            }
        }

        /// <inheritdoc/>
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name)
        {
            this.formatters.TryGetValue(this.options.CurrentValue.FormatterName, out var logFormatter);

            UpdateFormatterOptions(logFormatter, this.options.CurrentValue);

            return this.loggers.GetOrAdd(name, loggerName => new FileLogger(name, this.messageQueue)
            {
                ScopeProvider = this.scopeProvider,
                Formatter = logFormatter
            });
        }

        /// <summary>
        /// Allows setting formatter setting from parent
        /// </summary>
        /// <param name="formatter"></param>
        /// <param name="easyOptions"></param>
        private void UpdateFormatterOptions(FileFormatter formatter, FileLoggerOptions easyOptions)
        {
            // kept for deprecated apis:
            if (formatter is SimpleFileFormatter defaultFormatter)
            {
                defaultFormatter.FormatterOptions.IncludeScopes = defaultFormatter.FormatterOptions.IncludeScopes ?? easyOptions.IncludeScopes;
            }
            if (formatter is JsonFileFormatter jsonFormatter)
            {
                jsonFormatter.FormatterOptions.IncludeScopes = jsonFormatter.FormatterOptions.IncludeScopes ?? easyOptions.IncludeScopes;
            }
        }

        public void Dispose()
        {
            this.messageQueue?.Dispose();
            this.optionsReloadToken?.Dispose();
        }

        /// <inheritdoc />
        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            this.scopeProvider = scopeProvider;

            foreach (System.Collections.Generic.KeyValuePair<string, FileLogger> logger in this.loggers)
            {
                logger.Value.ScopeProvider = scopeProvider;
            }
        }
    }

    internal class NullScopeProvider : IExternalScopeProvider
    {
        public static NullScopeProvider Instance { get; } = new NullScopeProvider();

        private NullScopeProvider()
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        public void ForEachScope<TState>(Action<object, TState> callback, TState state)
        {
        }

        public IDisposable Push(object state)
        {
            return NullScope.Instance;
        }
    }
}
