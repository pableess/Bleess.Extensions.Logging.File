using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Bleess.Extensions.Logging.File
{
    internal sealed class CompositeFileLogger : ILogger
    {
        private readonly IEnumerable<FileLogger> _loggers;
        private IExternalScopeProvider? _scopeProvider;

        public CompositeFileLogger(IEnumerable<FileLogger> loggers, IExternalScopeProvider? scopeProvider)
        {
            _scopeProvider = scopeProvider;
            _loggers = loggers ?? throw new ArgumentNullException(nameof(loggers));
        }

        internal IExternalScopeProvider? ScopeProvider 
        {
            get => _scopeProvider;
            set
            {
                _scopeProvider = value;
                foreach (var logger in _loggers)
                {
                    logger.ScopeProvider = value;
                }
            }
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => ScopeProvider?.Push(state) ?? NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => _loggers.Any(l => l.IsEnabled(logLevel));

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
        {
            foreach (var logger in _loggers) 
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, state, exception, formatter);
                }
            }
        }
    }
}
