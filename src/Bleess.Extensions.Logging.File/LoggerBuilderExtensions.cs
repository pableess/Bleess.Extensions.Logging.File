namespace Microsoft.Extensions.Logging
{
    using System;
    using Bleess.Extensions.Logging.File;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Configuration;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Extensions for LoggerBuilder.
    /// </summary>
    public static class LoggerBuilderExtensions
    {
        /// <summary>
        /// Adds a file logger named 'File' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.AddFileFormatter<JsonFileFormatter, JsonFileFormatterOptions>();
            builder.AddFileFormatter<SimpleFileFormatter, SimpleFileFormatterOptions>();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());
            LoggerProviderOptions.RegisterProviderOptions<FileLoggerOptions, FileLoggerProvider>(builder.Services);

            return builder;
        }

        /// <summary>
        /// Adds a File logger named 'File' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">A delegate to configure the <see cref="FileLogger"/>.</param>
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, Action<FileLoggerOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.AddFile();
            builder.Services.Configure(configure);

            return builder;
        }

        /// <summary>
        /// Add and configure a File log formatter named 'simple' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configureLoggerOptions">A delegate to configure the <see cref="FileLoggerOptions"/> options</param>
        /// <param name="configureFormatterOptions">A delegate to configure the <see cref="SimpleFileFormatterOptions"/> options for the built-in default log formatter.</param>
        public static ILoggingBuilder AddSimpleFile(this ILoggingBuilder builder, Action<FileLoggerOptions> configureLoggerOptions = null, Action<SimpleFileFormatterOptions> configureFormatterOptions = null)
        {
            builder.AddFileUsingFormatter<SimpleFileFormatterOptions>(FileFormatterNames.Simple, configureFormatterOptions);

            if (configureLoggerOptions != null)
            {
                builder.Services.Configure<FileLoggerOptions>(configureLoggerOptions);
            }

            return builder;
        }

        /// <summary>
        /// Add and configure a File log formatter named 'json' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configureLoggerOptions">A delegate to configure the <see cref="FileLoggerOptions"/> for the built-in json log formatter.</param>
        /// <param name="configureFormatterOptions">A delegate to configure the <see cref="JsonFileFormatterOptions"/> for the built-in json log formatter.</param>
        public static ILoggingBuilder AddJsonFile(this ILoggingBuilder builder, Action<FileLoggerOptions> configureLoggerOptions = null, Action<JsonFileFormatterOptions> configureFormatterOptions = null)
        {
            builder.AddFileUsingFormatter<JsonFileFormatterOptions>(FileFormatterNames.Json, configureFormatterOptions);

            if (configureLoggerOptions != null)
            {
                builder.Services.Configure<FileLoggerOptions>(configureLoggerOptions);
            }
            return builder;
        }


        /// <summary>
        /// Adds a file logger that is configured to use the specified formatter
        /// </summary>
        /// <typeparam name="TFormatterOptions"></typeparam>
        /// <param name="builder"></param>
        /// <param name="formatterName">The type of formatter </param>
        /// <param name="configure"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static ILoggingBuilder AddFileUsingFormatter<TFormatterOptions>(this ILoggingBuilder builder, string formatterName, Action<TFormatterOptions> configure = null)
            where TFormatterOptions : FileFormatterOptions
        {
            // Add file and configure options to use the named options
            builder.AddFile((FileLoggerOptions options) => options.FormatterName = formatterName);

            if (configure != null)
            {
                builder.Services.Configure(configure);
            }
            return builder;
        }

        /// <summary>
        /// Adds a custom File logger formatter 'TFormatter' to be configured with options 'TOptions'.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddFileFormatter<TFormatter, TFormatterOptions>(this ILoggingBuilder builder)
            where TFormatter : FileFormatter
            where TFormatterOptions : class
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<FileFormatter, TFormatter>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<TFormatterOptions>, FileLoggerFormatterConfigureOptions<TFormatter, TFormatterOptions>>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TFormatterOptions>, FileLoggerFormatterOptionsChangeTokenSource<TFormatter, TFormatterOptions>>());

            return builder;
        }

        /// <summary>
        /// Adds a custom File logger formatter 'TFormatter' to be configured with options 'TOptions'.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">A delegate to configure options 'TOptions' for custom formatter 'TFormatter'.</param>
        public static ILoggingBuilder AddFileFormatter<TFormatter, TFormatterOptions>(this ILoggingBuilder builder, Action<TFormatterOptions> configure)
            where TFormatterOptions : class
            where TFormatter : FileFormatter
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.AddFileFormatter<TFormatter, TFormatterOptions>();
            builder.Services.Configure(configure);
            return builder;
        }
    }

    internal class FileLoggerFormatterConfigureOptions<TFormatter, TOptions> : ConfigureFromConfigurationOptions<TOptions>
        where TOptions : class
        where TFormatter : FileFormatter
    {
        public FileLoggerFormatterConfigureOptions(ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration) :
            base(providerConfiguration.Configuration.GetSection("FormatterOptions"))
        {
        }
    }

    internal class FileLoggerFormatterOptionsChangeTokenSource<TFormatter, TOptions> : ConfigurationChangeTokenSource<TOptions>
        where TOptions : class
        where TFormatter : FileFormatter
    {
        public FileLoggerFormatterOptionsChangeTokenSource(ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration)
            : base(providerConfiguration.Configuration.GetSection("FormatterOptions"))
        {
        }
    }
}
