namespace Bleess.Extensions.Logging.File;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

/// <summary>
/// A rolling file log provider 
/// </summary>
[ProviderAlias("File")]
public sealed class FileLoggerProvider : ILoggerProvider, ISupportExternalScope, IDisposable
{
    private readonly IOptionsMonitor<FileLoggerOptions> optionsMonitor;
    private readonly IDisposable? optionsReloadToken;
    private IExternalScopeProvider scopeProvider = NullScopeProvider.Instance;
    private readonly ConcurrentDictionary<string, FileLogger> loggers;
    private readonly IReadOnlyDictionary<string, FileFormatter> formatters;
    private readonly FileLoggerProcessor messageQueue;
    private readonly string? loggerName;

    private FileLoggerOptions? cachedOptions;

    /// <summary>
    /// Creates an instance of <see cref="FileLoggerProvider"/>.
    /// </summary>
    /// <param name="options">The options to create <see cref="FileLogger"/> instances with.</param>
    /// <param name="formatters">Log formatters added for <see cref="FileLogger"/> insteaces.</param>
    /// <param name="timeProvider">The clock used for timestamps and date based rolling.</param>
    /// <param name="loggerName">The file logger named instance if part of a composite logger</param>
    public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> options, IEnumerable<FileFormatter> formatters, TimeProvider timeProvider, string? loggerName = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(formatters);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.loggerName = loggerName;
        this.optionsMonitor = options;
        this.loggers = new ConcurrentDictionary<string, FileLogger>();

        this.formatters = CreateFormatters(formatters, timeProvider);

        this.messageQueue = new FileLoggerProcessor(this.CurrentOptions, timeProvider);

        // fail fast when the configured formatter does not exist
        _ = GetFormatter(this.CurrentOptions.FormatterName);

        optionsReloadToken = this.optionsMonitor.OnChange(ReloadLoggerOptions);

        ReloadFormatters(this.CurrentOptions);
    }

    /// <summary>
    /// The name used to resolve the options for this provider.  The default provider uses
    /// <see cref="Options.DefaultName"/> so that the comparison with the name reported by
    /// <see cref="IOptionsMonitor{TOptions}.OnChange"/> matches.
    /// </summary>
    private string OptionsName => string.IsNullOrEmpty(this.loggerName) ? Microsoft.Extensions.Options.Options.DefaultName : this.loggerName;

    private FileLoggerOptions CurrentOptions
    {
        get
        {
            var cached = Volatile.Read(ref cachedOptions);
            if (cached is not null)
            {
                return cached;
            }

            var options = this.optionsMonitor.Get(this.loggerName);
            return Interlocked.CompareExchange(ref cachedOptions, options, null) ?? options;
        }
    }

    private static FrozenDictionary<string, FileFormatter> CreateFormatters(IEnumerable<FileFormatter>? formatters, TimeProvider timeProvider)
    {
        var formatterMap = new Dictionary<string, FileFormatter>(StringComparer.OrdinalIgnoreCase);
        if (formatters is null || !formatters.Any())
        {
            var defaultMonitor = new FormatterOptionsMonitor<SimpleFileFormatterOptions>(new SimpleFileFormatterOptions());
            var jsonMonitor = new FormatterOptionsMonitor<JsonFileFormatterOptions>(new JsonFileFormatterOptions());
            formatterMap[FileFormatterNames.Simple] = new SimpleFileFormatter(defaultMonitor, timeProvider);
            formatterMap[FileFormatterNames.Json] = new JsonFileFormatter(jsonMonitor, timeProvider);
        }
        else
        {
            foreach (FileFormatter formatter in formatters)
            {
                formatterMap[formatter.Name] = formatter;
            }
        }

        return formatterMap.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private FileFormatter GetFormatter(string formatterName)
    {
        if (this.formatters.TryGetValue(formatterName, out var formatter))
        {
            return formatter;
        }

        throw new InvalidOperationException(
            $"No file formatter named '{formatterName}' is registered. " +
            $"Registered formatters: {string.Join(", ", this.formatters.Keys)}.");
    }

    private void ReloadLoggerOptions(FileLoggerOptions options, string? optionName)
    {
        // options are named; the default provider is registered under Options.DefaultName (empty string),
        // not null, so compare against the effective name
        if (string.Equals(optionName ?? Microsoft.Extensions.Options.Options.DefaultName, OptionsName, StringComparison.Ordinal))
        {
            Volatile.Write(ref cachedOptions, options);
            this.messageQueue.ConfigureWriter(options);
            ReloadFormatters(options);
        }
    }

    private void ReloadFormatters(FileLoggerOptions options)
    {
        // a reload must never take logging down: if the new formatter is not registered keep the current one
        if (!this.formatters.TryGetValue(options.FormatterName, out var logFormatter))
        {
            Trace.TraceError(
                $"No file formatter named '{options.FormatterName}' is registered. Registered formatters: {string.Join(", ", this.formatters.Keys)}. Keeping the current formatter.");
            return;
        }

        foreach (KeyValuePair<string, FileLogger> logger in this.loggers)
        {
            logger.Value.Formatter = logFormatter;

#pragma warning disable CS0618 // Type or member is obsolete
            logger.Value.FallbackIncludeScope = options.IncludeScopes;
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }

    /// <inheritdoc/>
    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
    {
        var options = CurrentOptions;
        var logFormatter = GetFormatter(options.FormatterName);

#pragma warning disable CS0618 // Type or member is obsolete
        return this.loggers.GetOrAdd(categoryName, loggerName => new FileLogger(loggerName, this.messageQueue)
        {
            ScopeProvider = this.scopeProvider,
            Formatter = logFormatter,
            FallbackIncludeScope = options.IncludeScopes
        });
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        this.loggers.Clear();
        this.messageQueue.Dispose();
        this.optionsReloadToken?.Dispose();
    }

    /// <inheritdoc />
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        this.scopeProvider = scopeProvider;

        foreach (KeyValuePair<string, FileLogger> logger in this.loggers)
        {
            logger.Value.ScopeProvider = scopeProvider;
        }
    }
}

internal sealed class NullScopeProvider : IExternalScopeProvider
{
    public static NullScopeProvider Instance { get; } = new NullScopeProvider();

    private NullScopeProvider()
    {
    }

    /// <inheritdoc />
#pragma warning disable CA1822 // interface implementation, no instance state
    public void Dispose()
#pragma warning restore CA1822
    {
    }

    public void ForEachScope<TState>(Action<object?, TState> callback, TState state)
    {
    }

    public IDisposable Push(object? state)
    {
        return NullScope.Instance;
    }
}
