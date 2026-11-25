using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;

#nullable enable

namespace Bleess.Extensions.Logging.File;

/// <summary>
/// Base class for a file formatter, withot any formatting options
/// </summary>
public abstract class FileFormatter
{
    /// <summary>
    /// Base constructor for <see cref="FileFormatter"/>
    /// </summary>
    /// <param name="formatterName"></param>
    /// <exception cref="ArgumentNullException"></exception>
    protected FileFormatter(string formatterName)
        : this(formatterName, null)
    {
    }

    /// <summary>
    /// Base constructor for <see cref="FileFormatter"/>
    /// </summary>
    /// <param name="formatterName">The name used to select this formatter in the logger options.</param>
    /// <param name="timeProvider">The clock used for timestamps. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException"></exception>
    protected FileFormatter(string formatterName, TimeProvider? timeProvider)
    {
        Name = formatterName ?? throw new ArgumentNullException(nameof(formatterName));
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Gets the name associated with the console log formatter.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the clock used to create timestamps for log entries.
    /// </summary>
    protected TimeProvider TimeProvider { get; }

    /// <summary>
    /// Writes the log message to the specified TextWriter.
    /// </summary>
    /// <remarks>
    /// if the formatter wants to write colors to the console, it can do so by embedding ANSI color codes into the string
    /// </remarks>
    /// <param name="logEntry">The log entry.</param>
    /// <param name="scopeProvider">The provider of scope data.</param>
    /// <param name="textWriter">The string writer embedding ansi code for colors.</param>
    /// <param name="subProviderName">The name of the sub provider</param>
    /// <param name="fallbackIncludeScopes">fallback value for including the scopes</param>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    public abstract void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter, string? subProviderName = null, bool? fallbackIncludeScopes = null);

}

/// <summary>
/// Base class for file formatter with options
/// </summary>
public abstract class FileFormatter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)] TOptions> : FileFormatter, IDisposable
{
    private static readonly string DefaultCacheKey = string.Empty;

    private readonly IOptionsMonitor<TOptions> _options;
    private readonly ConcurrentDictionary<string, TOptions> _cachedOptions; // cache options per sub provider

    private readonly IDisposable? _optionsReloadToken;


    /// <summary>
    /// Creates a <see cref="FileFormatter{TOptions}"/>
    /// </summary>
    /// <param name="formatterName"></param>
    /// <param name="options"></param>
    /// <param name="timeProvider">The clock used for timestamps. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public FileFormatter(string formatterName, IOptionsMonitor<TOptions> options, TimeProvider? timeProvider = null)
        : base(formatterName, timeProvider)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _cachedOptions = new ConcurrentDictionary<string, TOptions>();
        _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
    }

    /// <summary>
    /// Disposes of the formatter
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _optionsReloadToken?.Dispose();
    }

    /// <summary>
    /// Gets the correct options for the formatter given the subProvider.  For non-composite logger, pass null
    /// </summary>
    /// <param name="subProvider"></param>
    /// <returns></returns>
    protected TOptions GetOptions(string? subProvider)
    {
        string key = subProvider ?? DefaultCacheKey;

        return _cachedOptions.GetOrAdd(key, (k) => _options.Get(k));
    }

    private void ReloadLoggerOptions(TOptions options, string? subProviderName)
    {
        var provider = subProviderName ?? DefaultCacheKey;

        _cachedOptions.AddOrUpdate(provider, options, (s, o) => options);
    }

}
