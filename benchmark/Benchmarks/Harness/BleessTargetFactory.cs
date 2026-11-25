extern alias Bleess;

using Bleess::Bleess.Extensions.Logging.File;
using Bleess::Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Benchmarks;

internal static class BleessTargetFactory
{
    public static LoggingTarget Create(TargetOptions options)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            if (options.FileCount <= 1)
            {
                AddSingleFile(builder, options);
            }
            else
            {
                AddMultipleFiles(builder, options);
            }
        });

        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("benchmark");

        return new LoggingTarget(Library.Bleess, logger, provider);
    }

    private static void AddSingleFile(ILoggingBuilder builder, TargetOptions options)
    {
        switch (options.Format)
        {
            case LogFormat.MessageOnly:
                builder.AddFile(o => ConfigureLogger(o, options, 0));
                builder.AddFileFormatter<MessageOnlyFormatter, FileFormatterOptions>();
                break;

            case LogFormat.Realistic:
                builder.AddSimpleFile(o => ConfigureLogger(o, options, 0), o => ConfigureSimpleFormatter(o, options));
                break;

            case LogFormat.Json:
                builder.AddJsonFile(o => ConfigureLogger(o, options, 0), o => ConfigureJsonFormatter(o, options));
                break;

            default:
                throw new System.ArgumentOutOfRangeException(nameof(options));
        }
    }

    private static void AddMultipleFiles(ILoggingBuilder builder, TargetOptions options)
    {
        if (options.Format == LogFormat.MessageOnly)
        {
            builder.AddFileFormatter<MessageOnlyFormatter, FileFormatterOptions>();
        }

        builder.AddFiles(files =>
        {
            for (int i = 0; i < options.FileCount; i++)
            {
                int index = i;
                var file = files.AddFile($"file{index}", o => ConfigureLogger(o, options, index));

                switch (options.Format)
                {
                    case LogFormat.Realistic:
                        file.WithSimpleFormatter(o => ConfigureSimpleFormatter(o, options));
                        break;

                    case LogFormat.Json:
                        file.WithJsonFormatter(o => ConfigureJsonFormatter(o, options));
                        break;
                }
            }
        });
    }

    private static void ConfigureLogger(FileLoggerOptions options, TargetOptions target, int index)
    {
        options.Path = target.PathFor(index);
        options.MaxFileSizeInMB = target.MaxFileSizeBytes / (1024f * 1024f);
        options.MaxNumberFiles = target.MaxNumberFiles;
        options.Append = true;
        options.FlushToDisk = target.FlushToDisk;

        if (target.Format == LogFormat.MessageOnly)
        {
            options.FormatterName = MessageOnlyFormatter.FormatterName;
        }
    }

    private static void ConfigureSimpleFormatter(SimpleFileFormatterOptions options, TargetOptions target)
    {
        options.IncludeScopes = target.IncludeScopes;
        options.SingleLine = true;
        options.EmptyLineBetweenMessages = true;
        options.UseUtcTimestamp = true;
        options.TimestampFormat = TargetOptions.TimestampFormat;
    }

    private static void ConfigureJsonFormatter(JsonFileFormatterOptions options, TargetOptions target)
    {
        options.IncludeScopes = target.IncludeScopes;
        options.EmptyLineBetweenMessages = false;
        options.UseUtcTimestamp = true;
        options.TimestampFormat = TargetOptions.TimestampFormat;
    }
}
