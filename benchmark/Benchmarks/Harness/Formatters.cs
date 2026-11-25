extern alias Bleess;
extern alias Karambolo;

using Bleess::Bleess.Extensions.Logging.File;
using Karambolo::Karambolo.Extensions.Logging.File;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text;

namespace Benchmarks;

/// <summary>
/// Bleess formatter that writes only the formatted message, so the comparison with the other
/// libraries measures the provider, not the output layout.
/// </summary>
internal sealed class MessageOnlyFormatter : FileFormatter<FileFormatterOptions>
{
    public const string FormatterName = "message-only";

    public MessageOnlyFormatter(IOptionsMonitor<FileFormatterOptions> options, TimeProvider timeProvider)
        : base(FormatterName, options, timeProvider)
    {
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter,
        string? subProviderName = null,
        bool? fallbackIncludeScopes = null)
    {
        textWriter.Write(logEntry.Formatter(logEntry.State, logEntry.Exception));
    }
}

/// <summary>
/// Karambolo text builder that writes only the formatted message.
/// </summary>
internal sealed class MessageOnlyTextBuilder : IFileLogEntryTextBuilder
{
    public void BuildEntryText(
        StringBuilder sb,
        string category,
        LogLevel logLevel,
        EventId eventId,
        string? message,
        Exception? exception,
        IExternalScopeProvider? scopeProvider,
        DateTimeOffset timestamp)
    {
        // the appender does not add a line break, so the builder must
        sb.Append(message);
        sb.Append(Environment.NewLine);
    }
}
