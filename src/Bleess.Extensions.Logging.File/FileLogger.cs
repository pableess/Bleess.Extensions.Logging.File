using Microsoft.Extensions.Logging;
using System;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bleess.Extensions.Logging.File
{
    // adapted from ConsoleLogger from https://github.com/dotnet/runtime
    internal sealed class FileLogger : ILogger
    {
        private readonly string _name;
        private readonly FileLoggerProcessor _queueProcessor;

        internal FileLogger(string name, FileLoggerProcessor loggerProcessor)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _queueProcessor = loggerProcessor;
        }

        internal FileFormatter? Formatter { get; set; }

        /// fallback setting to include scopes, deprecated API for setting this on the log provider
        internal bool? FallbackIncludeScope { get; set; }

        internal string? SubProviderName { get; set; }

        internal IExternalScopeProvider? ScopeProvider { get; set; }

        [ThreadStatic]
        private static StringWriter? t_stringWriter;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            ArgumentNullException.ThrowIfNull(formatter);

            // the provider validates the formatter at startup, this is only a guard against a bad registration
            var logFormatter = this.Formatter
                ?? throw new InvalidOperationException($"No file formatter is configured for logger '{_name}'.");

            t_stringWriter ??= new StringWriter();
            LogEntry<TState> logEntry = new LogEntry<TState>(logLevel, _name, eventId, state, exception, formatter);
            logFormatter.Write(in logEntry, ScopeProvider, t_stringWriter, SubProviderName, FallbackIncludeScope);

            var sb = t_stringWriter.GetStringBuilder();
            if (sb.Length == 0)
            {
                return;
            }
            string computedAnsiString = sb.ToString();
            sb.Clear();
            if (sb.Capacity > 1024)
            {
                sb.Capacity = 1024;
            }
            _queueProcessor.EnqueueMessage(new LogMessageEntry(computedAnsiString));
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => ScopeProvider?.Push(state) ?? NullScope.Instance;
    }

    internal sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();

        private NullScope()
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
