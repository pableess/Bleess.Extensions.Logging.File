using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

#nullable enable

namespace Bleess.Extensions.Logging.File;

internal sealed class JsonFileFormatter : FileFormatter<JsonFileFormatterOptions>
{
    public JsonFileFormatter(IOptionsMonitor<JsonFileFormatterOptions> options, TimeProvider timeProvider)
        : base(FileFormatterNames.Json, options, timeProvider)
    {
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter, string? subProviderName, bool? fallbackIncludeScopes)
    {
        JsonFileFormatterOptions formatterOptions = this.GetOptions(subProviderName);

        // support obsolete setting on the provider. the options instance is shared, so do not mutate it here.
        bool includeScopes = formatterOptions.IncludeScopes ?? fallbackIncludeScopes ?? false;

        string? message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (logEntry.Exception == null && message == null)
        {
            return;
        }
        LogLevel logLevel = logEntry.LogLevel;
        string category = logEntry.Category;
        int eventId = logEntry.EventId.Id;
        Exception? exception = logEntry.Exception;
        const int DefaultBufferSize = 1024;

        byte[] buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
        char[]? charBuffer = null;
        try
        {
            using (MemoryStream mem = new MemoryStream(buffer, 0, buffer.Length, writable: true, publiclyVisible: true))
            {
                using (var writer = new Utf8JsonWriter(mem, formatterOptions.JsonWriterOptions))
                {
                    writer.WriteStartObject();
                    string? timestampFormat = formatterOptions.TimestampFormat;
                    if (timestampFormat != null)
                    {
                        DateTimeOffset dateTimeOffset = formatterOptions.UseUtcTimestamp ? TimeProvider.GetUtcNow() : TimeProvider.GetLocalNow();
                        if (formatterOptions.InvariantTimestampFormat)
                        {
                            writer.WriteString("Timestamp", dateTimeOffset.ToString(timestampFormat, CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            writer.WriteString("Timestamp", dateTimeOffset.ToString(timestampFormat, CultureInfo.CurrentCulture));
                        }
                    }
                    writer.WriteNumber(nameof(logEntry.EventId), eventId);
                    writer.WriteString(nameof(logEntry.LogLevel), GetLogLevelString(logLevel));
                    writer.WriteString(nameof(logEntry.Category), category);
                    writer.WriteString("Message", message);

                    if (exception != null)
                    {
                        string exceptionMessage = exception.ToString();
                        if (!formatterOptions.JsonWriterOptions.Indented)
                        {
                            exceptionMessage = exceptionMessage.Replace(Environment.NewLine, " ");
                        }
                        writer.WriteString(nameof(Exception), exceptionMessage);
                    }

                    if (logEntry.State != null)
                    {
                        writer.WriteStartObject(nameof(logEntry.State));
                        writer.WriteString("Message", logEntry.State.ToString());
                        if (logEntry.State is IReadOnlyCollection<KeyValuePair<string, object>> stateProperties)
                        {
                            foreach (KeyValuePair<string, object> item in stateProperties)
                            {
                                WriteItem(writer, item);
                            }
                        }
                        writer.WriteEndObject();
                    }
                    WriteScopeInformation(writer, scopeProvider, includeScopes);
                    writer.WriteEndObject();
                    writer.Flush();

                    // decode the written bytes without an intermediate string or array copy
                    int written = (int)writer.BytesCommitted;
                    charBuffer = ArrayPool<char>.Shared.Rent(Encoding.UTF8.GetMaxCharCount(written));
                    int charCount = Encoding.UTF8.GetChars(mem.GetBuffer(), 0, written, charBuffer, 0);
                    textWriter.Write(charBuffer.AsSpan(0, charCount));

                    if (formatterOptions.EmptyLineBetweenMessages)
                    {
                        textWriter.Write(Environment.NewLine);
                    }
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            if (charBuffer is not null)
            {
                ArrayPool<char>.Shared.Return(charBuffer);
            }
        }
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "Trace",
            LogLevel.Debug => "Debug",
            LogLevel.Information => "Information",
            LogLevel.Warning => "Warning",
            LogLevel.Error => "Error",
            LogLevel.Critical => "Critical",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }

    private static void WriteScopeInformation(Utf8JsonWriter writer, IExternalScopeProvider? scopeProvider, bool includeScopes)
    {
        if (includeScopes && scopeProvider != null)
        {
            writer.WriteStartArray("Scopes");
            scopeProvider.ForEachScope((scope, state) =>
            {
                if (scope is IReadOnlyCollection<KeyValuePair<string, object>> scopes)
                {
                    state.WriteStartObject();
                    state.WriteString("Message", scope.ToString());
                    foreach (KeyValuePair<string, object> item in scopes)
                    {
                        WriteItem(state, item);
                    }
                    state.WriteEndObject();
                }
                else
                {
                    state.WriteStringValue(ToInvariantString(scope));
                }
            }, writer);
            writer.WriteEndArray();
        }
    }

    private static void WriteItem(Utf8JsonWriter writer, KeyValuePair<string, object> item)
    {
        var key = item.Key;
        switch (item.Value)
        {
            case bool boolValue:
                writer.WriteBoolean(key, boolValue);
                break;
            case byte byteValue:
                writer.WriteNumber(key, byteValue);
                break;
            case sbyte sbyteValue:
                writer.WriteNumber(key, sbyteValue);
                break;
            case char charValue:
                writer.WriteString(key, System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref charValue, 1));
                break;
            case decimal decimalValue:
                writer.WriteNumber(key, decimalValue);
                break;
            case double doubleValue:
                writer.WriteNumber(key, doubleValue);
                break;
            case float floatValue:
                writer.WriteNumber(key, floatValue);
                break;
            case int intValue:
                writer.WriteNumber(key, intValue);
                break;
            case uint uintValue:
                writer.WriteNumber(key, uintValue);
                break;
            case long longValue:
                writer.WriteNumber(key, longValue);
                break;
            case ulong ulongValue:
                writer.WriteNumber(key, ulongValue);
                break;
            case short shortValue:
                writer.WriteNumber(key, shortValue);
                break;
            case ushort ushortValue:
                writer.WriteNumber(key, ushortValue);
                break;
            case null:
                writer.WriteNull(key);
                break;
            default:
                writer.WriteString(key, ToInvariantString(item.Value));
                break;
        }
    }

    private static string? ToInvariantString(object? obj) => Convert.ToString(obj, CultureInfo.InvariantCulture);
}
