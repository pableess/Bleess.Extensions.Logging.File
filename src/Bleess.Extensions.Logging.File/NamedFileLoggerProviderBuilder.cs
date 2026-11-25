using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

#nullable enable

namespace Bleess.Extensions.Logging.File;

/// <summary>
/// A builder to configure a named file logger for use when multiple log files are necessary
/// </summary>
public sealed class NamedFileLoggerProviderBuilder
{
    private readonly ILoggingBuilder _builder;
    private readonly string _providerAlias;

    internal NamedFileLoggerProviderBuilder(ILoggingBuilder builder, string providerAlias)
    {
        _builder = builder;
        _providerAlias = providerAlias;
    }

    /// <summary>
    /// Configures logger options for this named log provider
    /// </summary>
    /// <param name="configure">Configuration delegate</param>
    /// <returns></returns>
    public NamedFileLoggerProviderBuilder WithOptions(Action<FileLoggerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        _builder.Services.Configure(_providerAlias, configure);
        return this;
    }

    /// <summary>
    /// Configures logger options for this named log provider
    /// </summary>
    /// <param name="configure">Configuration delegate</param>
    /// <returns></returns>
    public NamedFileLoggerProviderBuilder WithJsonFormatter(Action<JsonFileFormatterOptions>? configure = null)
    {
        _builder.Services.Configure<FileLoggerOptions>(_providerAlias, c => c.FormatterName = FileFormatterNames.Json);
        if (configure != null)
        {
            // formatter options are resolved by the sub provider name
            _builder.Services.Configure(_providerAlias, configure);
        }
        return this;
    }

    /// <summary>
    /// Configures logger options for this named log provider
    /// </summary>
    /// <param name="configure">Configuration delegate</param>
    /// <returns></returns>
    public NamedFileLoggerProviderBuilder WithSimpleFormatter(Action<SimpleFileFormatterOptions>? configure = null)
    {
        _builder.Services.Configure<FileLoggerOptions>(_providerAlias, c => c.FormatterName = FileFormatterNames.Simple);
        if (configure != null)
        {
            // formatter options are resolved by the sub provider name
            _builder.Services.Configure(_providerAlias, configure);
        }
        return this;
    }

    /// <summary>
    /// Adds a customer formatter and configures the logging provider to use that formatter
    /// </summary>
    /// <typeparam name="TFormatter"></typeparam>
    /// <typeparam name="TFormatterOptions"></typeparam>
    /// <param name="formatterName">
    /// The formatter name to select for this provider.  This must match the name that
    /// <typeparamref name="TFormatter"/> passes to the <see cref="FileFormatter"/> constructor.
    /// </param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public NamedFileLoggerProviderBuilder WithFormatter<TFormatter, TFormatterOptions>(string formatterName, Action<TFormatterOptions>? configure = null)
        where TFormatter : FileFormatter
        where TFormatterOptions : class
    {
        ArgumentNullException.ThrowIfNull(formatterName);

        // register the formatter type with the container
        _builder.AddFileFormatter<TFormatter, TFormatterOptions>();

        // select the formatter for this sub provider
        _builder.Services.Configure<FileLoggerOptions>(_providerAlias, c => c.FormatterName = formatterName);

        // formatter options are resolved by the sub provider name (see FileFormatter.GetOptions)
        if (configure != null)
        {
            _builder.Services.Configure(_providerAlias, configure);
        }

        return this;
    }

    /// <summary>
    /// Sets the default minimum log level for the log provider
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    public NamedFileLoggerProviderBuilder WithMinLevel(LogLevel level) => this.WithFilterRule(null, level, null);

    /// <summary>
    /// Adds a filtering rule
    /// </summary>
    /// <param name="category"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    public NamedFileLoggerProviderBuilder WithFilter(string? category = null, LogLevel? level = null) => this.WithFilterRule(category, level, null);

    /// <summary>
    /// Add a filter delegate 
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public NamedFileLoggerProviderBuilder WithFilter(Func<string?, LogLevel, bool> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        return this.WithFilterRule(null, null, filter);
    }

    /// <summary>
    /// Adds the filtering rule
    /// </summary>
    /// <param name="category"></param>
    /// <param name="level"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    public NamedFileLoggerProviderBuilder WithFilterRule(string? category = null,
           LogLevel? level = null,
           Func<string?, LogLevel, bool>? filter = null)
    {
        _builder.Services.Configure<CompositeLoggerFilterOptions>(_providerAlias, c =>
        {
            Func<string?, string?, LogLevel, bool>? func = null;
            if (filter != null)
            {
                func = new Func<string?, string?, LogLevel, bool>((p, c, l) => filter(c, l));
            }

            AddRule(c, _providerAlias, category, level, func);
        });
        return this;
    }

    private static CompositeLoggerFilterOptions AddRule(CompositeLoggerFilterOptions options,
           string? type = null,
           string? category = null,
           LogLevel? level = null,
           Func<string?, string?, LogLevel, bool>? filter = null)
    {
        options.Rules.Add(new LoggerFilterRule(type, category, level, filter));
        return options;
    }
}
