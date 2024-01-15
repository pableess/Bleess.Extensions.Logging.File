using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Bleess.Extensions.Logging.File;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Extension methods for configuring multiple files
/// </summary>
public static class CompositeFileLoggerExtensions 
{
    /// <summary>
    /// Adds a file logging provider that supports multiple files.  Use the builder to multiple sub log providers.  
    /// For configuration each Named sub log provider should be placed in the "Files" section of the "Logging" section.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="multiBuilder">Delegate to add and configure multiple log files</param>
    /// <returns></returns>
    public static ILoggingBuilder AddFiles(this ILoggingBuilder builder, Action<MultipleFileLoggerProviderBuilder> multiBuilder)
    {
        if (multiBuilder == null)
        {
            throw new ArgumentNullException(nameof(multiBuilder));
        }
        var internalBuilder = new MultipleFileLoggerProviderBuilder(builder);
        multiBuilder(internalBuilder);
        return builder;
    }
}

/// <summary>
/// Extension methods related to composite file logger for multiple files
/// </summary>
internal static class InternalCompositeFileLoggerExtensions
{  


    /// <summary>
    /// Adds a logging provider using the given provider alias, which supports multiple file log providers
    /// The multiple log providers must be defined in the configuration section "Files".
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configAlias">The alias used for the providers settings, relative to the </param>
    /// <returns></returns>
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string configAlias)
    {
        builder.AddConfiguration();

        if (string.IsNullOrEmpty(configAlias))
            throw new ArgumentNullException(nameof(configAlias));

        // register the named sub logger, for the composite logger
        builder.Services.AddTransient<ISubLoggerRegistration>(sp => new SubLoggerRegistration(configAlias));

        // need to add a rule so that log statements flow to the sub loggers and rules can be applied at that level
        builder.AddFilter<CompositeFileLoggerProvider>(null, LogLevel.Trace);

        // register the composite logger
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, CompositeFileLoggerProvider>());

        // register configuration of composite and the options 
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CompositeLoggerFilterOptions>, CompositeFileLoggerProviderConfigureOptions>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<FileLoggerOptions>, CompositeFileLoggerProviderConfigureOptions>());

        // register specialized change providers that use the config alias
        builder.Services.Add(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<CompositeLoggerFilterOptions>>(sp => ActivatorUtilities.CreateInstance<CompositeLoggerFilterOptionsChangeTokenSource>(sp, configAlias)));
        builder.Services.Add(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<FileLoggerOptions>>(sp => ActivatorUtilities.CreateInstance<CompositeFileLoggerOptionsChangeTokenSource>(sp, configAlias)));
        
        //  register specialized filters tied to the config alias
        builder.AddFileFormatter<JsonFileFormatter, JsonFileFormatterOptions>(configAlias);
        builder.AddFileFormatter<SimpleFileFormatter, SimpleFileFormatterOptions>(configAlias);

        return builder;
    }

    /// <summary>
    /// Adds a logging provider using the given provider alias, which supports multiple file log providers
    /// The multiple log providers must be defined in the configuration section "Files".
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configAlias">The alias used for the providers settings, relative to the </param>
    /// <param name="configure">A configuration delegate</param>
    /// <returns></returns>
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string configAlias, Action<FileLoggerOptions> configure)
    {
        if (configure == null) 
        {
            throw new ArgumentNullException(nameof(configure));
        }

        builder.AddFile(configAlias);
        builder.Services.Configure(configAlias, configure);
        return builder;
    }


    /// <summary>
    /// Adds a custom File logger formatter 'TFormatter' to be configured with options 'TOptions'.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
    /// <param name="subProviderAlias">Add a custom file logger formatter 'TFormatter' to be configured for the sub provider alias when using multiple log providers</param>
    public static ILoggingBuilder AddFileFormatter<TFormatter, TOptions>(this ILoggingBuilder builder, string subProviderAlias)
        where TOptions : FileFormatterOptions
        where TFormatter : FileFormatter
    {
        builder.AddConfiguration();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<FileFormatter, TFormatter>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<TOptions>, CompositeFileLoggerFormatterConfigureOptions<TOptions>>());
        builder.Services.Add(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TOptions>>(sp => ActivatorUtilities.CreateInstance<CompositeLoggerFormatterOptionsChangeTokenSource<TOptions>>(sp, subProviderAlias)));
                
        return builder;
    }

    /// <summary>
    /// Adds a custom File logger formatter 'TFormatter' to be configured with options 'TOptions'.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
    /// <param name="subProviderAlias">Add a custom file logger formatter 'TFormatter' to be configured for the sub provider alias when using multiple log providers</param>
    /// <param name="configureOptions">Formatter configuration callback</param>
    public static ILoggingBuilder AddFileFormatter<TFormatter, TOptions>(this ILoggingBuilder builder, string subProviderAlias, Action<TOptions> configureOptions)
       where TOptions : FileFormatterOptions
       where TFormatter : FileFormatter
    {
        builder.AddFileFormatter<TFormatter, TOptions>(subProviderAlias);
        builder.Services.Configure(subProviderAlias, configureOptions);

        return builder;
    }
}
