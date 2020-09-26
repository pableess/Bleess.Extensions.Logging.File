using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bleess.Extensions.Logging.File
{
    // adapted from ConsoleLogger from https://github.com/dotnet/runtime
    class FileLogger : ILogger
    {
            private readonly string _name;
            private readonly FileLoggerProcessor _queueProcessor;

            internal FileLogger(string name, FileLoggerProcessor loggerProcessor)
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                _name = name;
                _queueProcessor = loggerProcessor;
            }

            internal FileFormatter Formatter { get; set; }
            internal IExternalScopeProvider ScopeProvider { get; set; }

            [ThreadStatic]
            private static StringWriter t_stringWriter;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }
                if (formatter == null)
                {
                    throw new ArgumentNullException(nameof(formatter));
                }
                t_stringWriter ??= new StringWriter();
                LogEntry<TState> logEntry = new LogEntry<TState>(logLevel, _name, eventId, state, exception, formatter);
                Formatter.Write(in logEntry, ScopeProvider, t_stringWriter);

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

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel != LogLevel.None;
            }

            public IDisposable BeginScope<TState>(TState state) => ScopeProvider?.Push(state) ?? NullScope.Instance;
    }

    internal class NullScope : IDisposable
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
