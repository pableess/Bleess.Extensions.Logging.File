#if NET8_0_OR_GREATER

using Bleess.Extensions.Logging.File;
using Bleess.Extensions.Logging.File.Named;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Extenstion for named file loggers
/// </summary>
public static class NamedLoggerBuilderExtensions
{
    /// <summary>
    /// Adds a File logger named 'File' to the factory.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
    /// <param name="loggerName">The name of the file logger.  This name should not conflict with other logger provider alias</param>
    /// <param name="configure">A delegate to configure the <see cref="FileLogger"/>.</param>
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string loggerName, Action<FileLoggerOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(loggerName);

        builder.AddNamedConfigurationServices();
        if (configure != null)
        {
            builder.Services.Configure<FileLoggerOptions>(loggerName, configure);
        }

        builder.AddFileFormatterForNamedLogger<JsonFileFormatter, JsonFileFormatterOptions>(loggerName);
        builder.AddFileFormatterForNamedLogger<SimpleFileFormatter, SimpleFileFormatterOptions>(loggerName);


        if (!builder.Services.Any(d => d.ServiceType == typeof(FileLoggerProvider) && string.Equals(d.ServiceKey, loggerName)))
        {
            // register the provider as a keyed singleton against its own type using key, interface using key, and as a transient registritaion which will 
            // all use the same singleton instance.  This allows the the provider to be resolved any of these ways and adapts it to work with the logging framework
            builder.Services.TryAddKeyedSingleton<FileLoggerProvider>(loggerName, (sp, k) => ActivatorUtilities.CreateInstance<FileLoggerProvider>(sp, sp.GetKeyedServices<FileFormatter>(k), loggerName));
            builder.Services.TryAddKeyedSingleton<ILoggerProvider>(loggerName, (sp, k) => sp.GetKeyedService<FileLoggerProvider>(loggerName));
            builder.Services.AddTransient<ILoggerProvider>(sp => sp.GetKeyedService<FileLoggerProvider>(loggerName));
        }

        LoggerProviderOptions.RegisterProviderOptions<FileLoggerOptions, FileLoggerProvider>(builder.Services);
        NamedLoggerProviderOptions.RegisterProviderOptions<FileLoggerOptions, FileLoggerProvider>(builder.Services);

        return builder;
    }

    /// <summary>
    /// Add the default File log formatter named 'simple' to the factory with default properties.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
    /// <param name="loggerName">The name of the file logger.  This name should not conflict with other logger provider alias</param>
    public static ILoggingBuilder AddSimpleFile(this ILoggingBuilder builder, string loggerName) =>
        builder.AddFileFormatterWithName(FileFormatterNames.Simple, loggerName);

    /// <summary>
    /// Add and configure a File log formatter named 'simple' to the factory.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
    /// <param name="loggerName">The name of the file logger.  This name should not conflict with other logger provider alias</param>
    /// <param name="configure">A delegate to configure the <see cref="FileLogger"/> options for the built-in default log formatter.</param>
    public static ILoggingBuilder AddSimpleFile(this ILoggingBuilder builder, string loggerName, Action<SimpleFileFormatterOptions> configure)
    {
        return builder.AddFileWithFormatter<SimpleFileFormatterOptions>(FileFormatterNames.Simple, loggerName, configure);
    }

    /// <summary>
    /// Add a File log formatter named 'json' to the factory with default properties.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
    /// <param name="loggerName">The name of the file logger.  This name should not conflict with other logger provider alias</param>
    public static ILoggingBuilder AddJsonFile(this ILoggingBuilder builder, string loggerName) =>
        builder.AddFileFormatterWithName(FileFormatterNames.Json, loggerName);

    /// <summary>
    /// Add and configure a File log formatter named 'json' to the factory.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
    /// <param name="loggerName">The name of the file logger.  This name should not conflict with other logger provider alias</param>
    /// <param name="configure">A delegate to configure the <see cref="FileLogger"/> options for the built-in json log formatter.</param>
    public static ILoggingBuilder AddJsonFile(this ILoggingBuilder builder, string loggerName, Action<JsonFileFormatterOptions> configure)
    {
        return builder.AddFileWithFormatter<JsonFileFormatterOptions>(FileFormatterNames.Json, loggerName, configure);
    }

    internal static ILoggingBuilder AddFileWithFormatter<TOptions>(this ILoggingBuilder builder, string loggerName, string formatterName, Action<TOptions> configure)
        where TOptions : FileFormatterOptions
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }
        builder.AddFileFormatterWithName(loggerName, formatterName);
        builder.Services.Configure(configure);

        return builder;
    }

    private static ILoggingBuilder AddFileFormatterWithName(this ILoggingBuilder builder, string loggerName, string formatterName) =>
        builder.AddFile(loggerName, (FileLoggerOptions options) => options.FormatterName = formatterName);

    /// <summary>
    /// Adds a custom File logger formatter 'TFormatter' to be configured with options 'TOptions'.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
    /// <param name="loggerName">The name of the file logger.  This name should not conflict with other logger provider alias</param>
    public static ILoggingBuilder AddFileFormatterForNamedLogger<TFormatter, TOptions>(this ILoggingBuilder builder, string loggerName)
        where TOptions : FileFormatterOptions
        where TFormatter : FileFormatter
    {
        builder.AddConfiguration();

        // registered the formatter as a keyed service
        builder.Services.TryAddEnumerable(ServiceDescriptor.KeyedSingleton<FileFormatter, TFormatter>(loggerName));

        // register the 
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<TOptions>, NamedLoggerFormatterConfigureOptions<TOptions, TFormatter>>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TOptions>, FileLoggerFormatterOptionsChangeTokenSource<TFormatter, TOptions>>());

        return builder;
    }

    /// <summary>
    /// Adds a custom File logger formatter 'TFormatter' to be configured with options 'TOptions'.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
    /// <param name="loggerName"></param>
    /// <param name="configure">A delegate to configure options 'TOptions' for custom formatter 'TFormatter'.</param>
    public static ILoggingBuilder AddFileFormatterForNamedLogger<TFormatter, TOptions>(this ILoggingBuilder builder, string loggerName, Action<TOptions> configure)
        where TOptions : FileFormatterOptions
        where TFormatter : FileFormatter
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        builder.AddFileFormatterForNamedLogger<TFormatter, TOptions>(loggerName: loggerName);
        builder.Services.Configure(loggerName, configure);
        return builder;
    }

    internal static ILoggingBuilder AddNamedConfigurationServices(this ILoggingBuilder builder)
    {
        builder.AddConfiguration();

        builder.Services.TryAddSingleton<INamedLoggerProviderConfigurationFactory, NamedLoggerProviderConfigurationFactory>();
        builder.Services.TryAddSingleton<LoggingConfigurationAccessor>();
        return builder;
    }

    internal static IConfiguration GetFormatterOptionsSection(this ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration)
    {
        return providerConfiguration.Configuration.GetFormatterOptionsSection();
    }

    internal static IConfiguration GetFormatterOptionsSection(this IConfiguration section)
    {
        return section.GetSection("FormatterOptions");
    }
}


#endif
