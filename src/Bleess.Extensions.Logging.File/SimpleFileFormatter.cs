using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.IO;

#nullable enable

namespace Bleess.Extensions.Logging.File;

internal sealed class SimpleFileFormatter : FileFormatter<SimpleFileFormatterOptions>
{
    private const string LoglevelPadding = ": ";
    private static readonly string s_messagePadding = new string(' ', GetLogLevelString(LogLevel.Information).Length + LoglevelPadding.Length);
    private static readonly string s_newLineWithMessagePadding = Environment.NewLine + s_messagePadding;

    /// <summary>
    /// Create a <see cref="SimpleFileFormatter"/>
    /// </summary>
    /// <param name="options"></param>
    /// <param name="timeProvider"></param>
    public SimpleFileFormatter(IOptionsMonitor<SimpleFileFormatterOptions> options, TimeProvider timeProvider)
        : base(FileFormatterNames.Simple, options, timeProvider)
    {
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter, string? subProviderName = null, bool? fallbackIncludeScope = null)
    {
        var formatterOptions = this.GetOptions(subProviderName);

        // support obsolete include scope from the provider config, if not set on the formatter.
        // the options instance is shared, so do not mutate it here.
        bool includeScopes = formatterOptions.IncludeScopes ?? fallbackIncludeScope ?? false;

        string? message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (logEntry.Exception == null && message == null)
        {
            return;
        }
        LogLevel logLevel = logEntry.LogLevel;
        string logLevelString = GetLogLevelString(logLevel);

        string? timestamp = null;
        string? timestampFormat = formatterOptions.TimestampFormat;
        if (timestampFormat != null)
        {
            DateTimeOffset dateTimeOffset = GetCurrentDateTime(formatterOptions);

            if (formatterOptions.InvariantTimestampFormat)
            {
                timestamp = dateTimeOffset.ToString(timestampFormat, CultureInfo.InvariantCulture) + " ";
            }
            else
            {
                timestamp = dateTimeOffset.ToString(timestampFormat, CultureInfo.CurrentCulture) + " ";
            }
        }
        if (timestamp != null)
        {
            textWriter.Write(timestamp);
        }
        if (logLevelString != null)
        {
            textWriter.Write(' ');
            textWriter.Write(logLevelString);
        }
        CreateDefaultLogMessage(textWriter, logEntry, message, scopeProvider, formatterOptions, includeScopes);
    }

    private static void CreateDefaultLogMessage<TState>(TextWriter textWriter, in LogEntry<TState> logEntry, string? message, IExternalScopeProvider? scopeProvider, SimpleFileFormatterOptions formatterOptions, bool includeScopes)
    {
        bool singleLine = formatterOptions.SingleLine;

        Exception? exception = logEntry.Exception;

        // Example:
        // info: ConsoleApp.Program[10]
        //       Request received

        // category and event id
        WriteCategoryAndEventId(textWriter, logEntry);

        WriteMessage(textWriter, message, singleLine);

        // Example:
        // System.InvalidOperationException
        //    at Namespace.Class.Function() in File:line X
        if (exception != null)
        {
            // exception message
            WriteMessage(textWriter, exception.ToString(), singleLine);
        }

        WriteScopeInformation(textWriter, scopeProvider, singleLine, includeScopes);

        if (formatterOptions.EmptyLineBetweenMessages)
        {
            textWriter.Write(Environment.NewLine);
        }
    }

    private static void WriteCategoryAndEventId<TState>(TextWriter textWriter, LogEntry<TState> logEntry)
    {
        textWriter.Write(LoglevelPadding);
        textWriter.Write(logEntry.Category);
        textWriter.Write(" [");
        textWriter.Write(logEntry.EventId.Id);
        textWriter.Write("]");
    }

    private static void WriteMessage(TextWriter textWriter, string? message, bool singleLine)
    {
        if (!string.IsNullOrEmpty(message))
        {
            if (singleLine)
            {
                textWriter.Write(' ');
                textWriter.Write(message.Replace(Environment.NewLine, " "));
            }
            else
            {
                textWriter.Write(Environment.NewLine);

                textWriter.Write(s_messagePadding);
                textWriter.Write(message.Replace(Environment.NewLine, s_newLineWithMessagePadding));
            }
        }
    }

    private DateTimeOffset GetCurrentDateTime(SimpleFileFormatterOptions formatterOptions)
    {
        return formatterOptions.UseUtcTimestamp ? TimeProvider.GetUtcNow() : TimeProvider.GetLocalNow();
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }

    private static void WriteScopeInformation(TextWriter textWriter, IExternalScopeProvider? scopeProvider, bool singleLine, bool includeScopes)
    {
        if (!includeScopes || scopeProvider == null)
        {
            return;
        }

        bool paddingNeeded = !singleLine;

        scopeProvider.ForEachScope((scope, state) =>
        {
            if (!singleLine)
            {
                state.Write(Environment.NewLine);
            }

            if (paddingNeeded)
            {
                state.Write(s_messagePadding + "=> ");
            }
            else
            {
                state.Write(" => ");
            }
            state.Write(scope);
        }, textWriter);
    }
}
