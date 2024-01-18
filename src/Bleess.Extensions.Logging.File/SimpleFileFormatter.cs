using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

#nullable enable

namespace Bleess.Extensions.Logging.File;

internal class SimpleFileFormatter : FileFormatter<SimpleFileFormatterOptions>
{
    private const string LoglevelPadding = ": ";
    private static readonly string _messagePadding = new string(' ', GetLogLevelString(LogLevel.Information).Length + LoglevelPadding.Length);
    private static readonly string _newLineWithMessagePadding = Environment.NewLine + _messagePadding;

    /// <summary>
    /// Create a <see cref="SimpleFileFormatter"/>
    /// </summary>
    /// <param name="options"></param>
    public SimpleFileFormatter(IOptionsMonitor<SimpleFileFormatterOptions> options)
        : base(FileFormatterNames.Simple, options)
    {
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter, string? subProviderName = null, bool? fallbackIncludeScope = null)
    {
        var formatterOptions = this.GetOptions(subProviderName);

        // support obsolete include scope from the provider config, if not set on the formatter
        formatterOptions.IncludeScopes = formatterOptions.IncludeScopes ?? fallbackIncludeScope;

        string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (logEntry.Exception == null && message == null)
        {
            return;
        }
        LogLevel logLevel = logEntry.LogLevel;
        string logLevelString = GetLogLevelString(logLevel);

        string? timestamp = null;
        string timestampFormat = formatterOptions.TimestampFormat;
        if (timestampFormat != null)
        {
            DateTimeOffset dateTimeOffset = GetCurrentDateTime(formatterOptions);
            timestamp = dateTimeOffset.ToString(timestampFormat) + " ";
        }
        if (timestamp != null)
        {
            textWriter.Write(timestamp);
        }
        if (logLevelString != null)
        {
            textWriter.Write($" {logLevelString}");
        }
        CreateDefaultLogMessage(textWriter, logEntry, message, scopeProvider, formatterOptions);
    }

    private void CreateDefaultLogMessage<TState>(TextWriter textWriter, in LogEntry<TState> logEntry, string message, IExternalScopeProvider scopeProvider, SimpleFileFormatterOptions formatterOptions)
    {
        bool singleLine = formatterOptions.SingleLine;
        
        Exception exception = logEntry.Exception;

        // Example:
        // info: ConsoleApp.Program[10]
        //       Request received

        // category and event id
        WriteLevelAndCategory<TState>(textWriter, logEntry);

        WriteMessage(textWriter, message, singleLine);

        // Example:
        // System.InvalidOperationException
        //    at Namespace.Class.Function() in File:line X
        if (exception != null)
        {
            // exception message
            WriteMessage(textWriter, exception.ToString(), singleLine);
        }

        WriteScopeInformation(textWriter, scopeProvider, singleLine, formatterOptions);

        if (formatterOptions.EmptyLineBetweenMessages)
        {
            textWriter.Write(Environment.NewLine);
        }
    }

    private void WriteLevelAndCategory<TState>(TextWriter textWriter, LogEntry<TState> logEntry) 
    {
        StringBuilder b = new StringBuilder(LoglevelPadding);
        b.Append(logEntry.Category);

        b.Append(" [");
        b.Append(logEntry.EventId.Id);
        b.Append("]");

        textWriter.Write(b.ToString());
    }

    private void WriteMessage(TextWriter textWriter, string message, bool singleLine)
    {
        if (!string.IsNullOrEmpty(message))
        {
            if (singleLine)
            {
                textWriter.Write(' ');
                WriteReplacing(textWriter, Environment.NewLine, " ", message);
            }
            else
            {
                textWriter.Write(Environment.NewLine);

                textWriter.Write(_messagePadding);
                WriteReplacing(textWriter, Environment.NewLine, _newLineWithMessagePadding, message);
            }

        }

        static void WriteReplacing(TextWriter writer, string oldValue, string newValue, string message)
        {
            string newMessage = message.Replace(oldValue, newValue);
            writer.Write(newMessage);
        }
    }

    private DateTimeOffset GetCurrentDateTime(SimpleFileFormatterOptions formatterOptions)
    {
        return formatterOptions.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
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
   
    private void WriteScopeInformation(TextWriter textWriter, IExternalScopeProvider scopeProvider, bool singleLine, SimpleFileFormatterOptions formatterOptions)
    {
        if (formatterOptions.IncludeScopes == true && scopeProvider != null)
        {
            bool paddingNeeded = !singleLine;

         

            scopeProvider.ForEachScope((scope, state) =>
            {
                if (!singleLine)
                {
                    textWriter.Write(Environment.NewLine);
                }

                if (paddingNeeded)
                {
                    state.Write(_messagePadding + "=> ");
                }
                else
                {
                    state.Write(" => ");
                }
                state.Write(scope);
            }, textWriter);
        }
    }
}
