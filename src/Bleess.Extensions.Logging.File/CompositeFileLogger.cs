using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

#nullable enable

namespace Bleess.Extensions.Logging.File;

internal sealed class CompositeFileLogger : ILogger
{
    // because sub providers don't change immutable dictionary will have less allocations on iteration
    private ImmutableDictionary<string, SubFileLoggerInfo> _loggers;
    private volatile IExternalScopeProvider? _scopeProvider;

    public CompositeFileLogger(string category, IEnumerable<SubFileLoggerInfo> loggers, IExternalScopeProvider? scopeProvider)
    {
        ArgumentNullException.ThrowIfNull(loggers);

        Category = category;
        _scopeProvider = scopeProvider;
        _loggers = ImmutableDictionary.CreateRange(
            loggers.ToDictionary(l => l.SubProviderName, l => l));
    }

    public IEnumerable<SubFileLoggerInfo> SubLoggers => _loggers.Values;

    /// <summary>
    /// Gets the category
    /// </summary>
    public string Category { get; }

    public void Update(string provider, LogLevel? minLogLevel, Func<string?, string?, LogLevel, bool>? filter)
    {
        // replace the entry in one atomic publish; publishing per iteration would expose a partially
        // built dictionary to concurrent readers and drop messages for some sub providers
        ImmutableInterlocked.Update(
            ref _loggers,
            static (current, state) =>
            {
                if (!current.TryGetValue(state.Provider, out var existing))
                {
                    return current;
                }

                return current.SetItem(
                    state.Provider,
                    new SubFileLoggerInfo(existing.Logger, existing.SubProviderName, state.MinLevel, state.Filter));
            },
            (Provider: provider, MinLevel: minLogLevel, Filter: filter));
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

    // true when any sub logger accepts the level (mirrors Microsoft.Extensions.Logging.Logger.IsEnabled)
    public bool IsEnabled(LogLevel logLevel) => _loggers.Values.Any(l => IsEnabled(l, logLevel));

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
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
