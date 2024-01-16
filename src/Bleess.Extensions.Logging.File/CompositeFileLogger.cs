using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Bleess.Extensions.Logging.File;

internal sealed class CompositeFileLogger : ILogger
{   
    private readonly ConcurrentDictionary<string, SubFileLoggerInfo> _loggers;
    private IExternalScopeProvider? _scopeProvider;

    public CompositeFileLogger(string category, IEnumerable<SubFileLoggerInfo> loggers, IExternalScopeProvider? scopeProvider)
    {
        Category = category;
        _scopeProvider = scopeProvider;
        _loggers = new ConcurrentDictionary<string, SubFileLoggerInfo>(loggers.ToDictionary(l => l.SubProviderName, l => l)) ?? throw new ArgumentNullException(nameof(loggers));
    }

    /// <summary>
    /// Gets the category
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets the sub loggers
    /// </summary>
    public IEnumerable<SubFileLoggerInfo> SubLoggers => _loggers.Values;

    public void Update(string provider, LogLevel? minLogLevel, Func<string?, string?, LogLevel, bool>? filter) 
    {
        if (_loggers.TryGetValue(provider, out var cur))
        {
            var newVal = new SubFileLoggerInfo(cur.Logger, cur.SubProviderName, minLogLevel, filter);
            _loggers.TryUpdate(provider, newVal, cur);
        }
    }

    public void Add(SubFileLoggerInfo info)
    {
        _loggers.TryAdd(info.SubProviderName, info);
    }
    public void Add(string providerKey)
    {
        _loggers.TryRemove(providerKey, out _);
    }


    internal IExternalScopeProvider? ScopeProvider 
    {
        get => _scopeProvider;
        set
        {
            _scopeProvider = value;
            foreach (var logger in _loggers.Values)
            {
                logger.Logger.ScopeProvider = value;
            }
        }
    }

    IDisposable ILogger.BeginScope<TState>(TState state)
    {
        if (ScopeProvider == null) 
        {
            return NullScope.Instance;
        }

        return ScopeProvider.Push(state);
    }

    public bool IsEnabled(LogLevel logLevel) => _loggers.Values.All(l => IsEnabled(l, logLevel));

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
    {
        foreach (var logger in _loggers.Values) 
        {
            if (IsEnabled(logger, logLevel))
            {
                logger.Logger.Log(logLevel, eventId, state, exception, formatter);
            }
        }
    }

    private bool IsEnabled(SubFileLoggerInfo info, LogLevel level)
    {
        if (info.MinLevel != null && info.MinLevel > level)
        {
            return false;
        }

        if (info.Filter != null && !info.Filter(info.SubProviderName, this.Category, level))
        {
            return false;
        }

        return true;

    }

}
