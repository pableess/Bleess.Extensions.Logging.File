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

public static class CompositeFileLoggerExtensions
{
    /// <summary>
    /// Adds a composite file logger that allows multiple loggers
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
    public static ILoggingBuilder AddFiles(this ILoggingBuilder builder)
    {
        builder.AddConfiguration();

        builder.AddFileFormatter<JsonFileFormatter, JsonFileFormatterOptions>();
        builder.AddFileFormatter<SimpleFileFormatter, SimpleFileFormatterOptions>();

        // register configuration of composite and the options 
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CompositeFileLoggerProviderOptions>, CompositeFileLoggerProviderConfigureOptions>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<FileLoggerOptions>, CompositeFileLoggerProviderConfigureOptions>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CompositeLoggerFilterOptions>, CompositeFileLoggerProviderConfigureOptions>());

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, CompositeFileLoggerProvider>());

        return builder;
    }
}
