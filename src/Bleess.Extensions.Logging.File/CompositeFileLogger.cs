using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Bleess.Extensions.Logging.File;

internal sealed class CompositeFileLogger : ILogger
{   
    // because sub providers don't change immutable dictionary will have less allocations on iteration
    private volatile ImmutableDictionary<string, SubFileLoggerInfo> _loggers;
    private IExternalScopeProvider? _scopeProvider;

    public CompositeFileLogger(string category, IEnumerable<SubFileLoggerInfo> loggers, IExternalScopeProvider? scopeProvider)
    {
        Category = category;
        _scopeProvider = scopeProvider;
        _loggers = ImmutableDictionary.CreateRange(
            loggers.ToDictionary(l => l.SubProviderName, l => l)) ?? throw new ArgumentNullException(nameof(loggers));
    }

    public IEnumerable<SubFileLoggerInfo> SubLoggers => _loggers.Values;

    /// <summary>
    /// Gets the category
    /// </summary>
    public string Category { get; }

   
    public void Update(string provider, LogLevel? minLogLevel, Func<string?, string?, LogLevel, bool>? filter) 
    {
        var updateBuilder = ImmutableDictionary.CreateBuilder<string, SubFileLoggerInfo>();

        foreach (var l in _loggers) 
        {
            if (l.Key == provider)
            {
                var newVal = new SubFileLoggerInfo(l.Value.Logger, l.Value.SubProviderName, minLogLevel, filter);
                updateBuilder.Add(provider, newVal);
            }
            else
            {
                updateBuilder.Add(l);
            }

            Interlocked.Exchange(ref _loggers, updateBuilder.ToImmutable());
        }
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
        foreach (var logger in _loggers) 
        {
            if (IsEnabled(logger.Value, logLevel))
            {
                logger.Value.Logger.Log(logLevel, eventId, state, exception, formatter);
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
