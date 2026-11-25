extern alias NReco;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NReco::NReco.Logging.File;
using System;

namespace Benchmarks;

internal static class NRecoTargetFactory
{
    public static LoggingTarget Create(TargetOptions options)
    {
        if (options.Format == LogFormat.Json)
        {
            throw new NotSupportedException("NReco.Logging.File does not ship a JSON formatter.");
        }

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            // NReco has no multi-file provider, so register one provider per file
            for (int i = 0; i < options.FileCount; i++)
            {
                string path = options.PathFor(i);
                builder.AddFile(path, o => Configure(o, options));
            }
        });

        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("benchmark");

        return new LoggingTarget(Library.NReco, logger, provider);
    }

    private static void Configure(FileLoggerOptions options, TargetOptions target)
    {
        options.FileSizeLimitBytes = target.MaxFileSizeBytes;
        options.MaxRollingFiles = target.MaxNumberFiles;
        options.IncludeScopes = target.IncludeScopes;
        options.UseUtcTimestamp = true;
        options.Append = true;

        if (target.Format == LogFormat.MessageOnly)
        {
            options.FormatLogEntry = message => message.Message;
        }
    }
}
