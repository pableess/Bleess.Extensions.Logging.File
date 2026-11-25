extern alias Karambolo;

using Karambolo::Karambolo.Extensions.Logging.File;
using Karambolo::Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Benchmarks;

internal static class KaramboloTargetFactory
{
    public static LoggingTarget Create(TargetOptions options)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            if (options.Format == LogFormat.Json)
            {
                builder.AddJsonFile(o => Configure(o, options));
            }
            else
            {
                builder.AddFile(o => Configure(o, options));
            }
        });

        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("benchmark");

        return new LoggingTarget(Library.Karambolo, logger, provider);
    }

    private static void Configure(FileLoggerOptions options, TargetOptions target)
    {
        // Karambolo resolves file paths relative to RootPath; absolute paths are rejected
        options.RootPath = target.Directory;
        options.IncludeScopes = target.IncludeScopes;
        options.MaxFileSize = target.MaxFileSizeBytes;
        options.MaxQueueSize = 1024;
        options.DateFormat = TargetOptions.TimestampFormat;
        options.CounterFormat = "000";
        // no BOM, matching the other libraries
        options.FileEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        // KeepOpen buffers writes instead of flushing every entry (KeepOpenAndAutoFlush is the default)
        options.FileAccessMode = LogFileAccessMode.KeepOpen;
        options.Files = Enumerable.Range(0, target.FileCount)
            .Select(i => new LogFileOptions { Path = PathFor(target, i) })
            .ToArray();

        if (target.Format == LogFormat.MessageOnly)
        {
            options.TextBuilder = new MessageOnlyTextBuilder();
        }
    }

    private static string PathFor(TargetOptions target, int index)
    {
        string extension = Path.GetExtension(target.FileName);
        string name = Path.GetFileNameWithoutExtension(target.FileName);

        // the <counter> placeholder is required for MaxFileSize to be enforced
        return index == 0
            ? $"{name}-<counter>{extension}"
            : $"{name}{index}-<counter>{extension}";
    }
}
