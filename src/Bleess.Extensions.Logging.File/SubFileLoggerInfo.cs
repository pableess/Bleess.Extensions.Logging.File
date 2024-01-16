using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace Bleess.Extensions.Logging.File;

internal readonly struct SubFileLoggerInfo
{
    public SubFileLoggerInfo(FileLogger logger, string subProviderName, LogLevel? minLevel, Func<string?, string?, LogLevel, bool>? filter)
    {
        this.Logger = logger;
        this.SubProviderName = subProviderName;
        this.MinLevel = minLevel;
        this.Filter = filter;
    }

    public string SubProviderName { get; }

    public FileLogger Logger { get; }

    public LogLevel? MinLevel{ get; }

    public Func<string?, string?, LogLevel, bool>? Filter { get; }
}
