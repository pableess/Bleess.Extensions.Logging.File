﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

#nullable enable

namespace Bleess.Extensions.Logging.File;

internal class JsonFileFormatter : FileFormatter<JsonFileFormatterOptions>
{   
    public JsonFileFormatter(IOptionsMonitor<JsonFileFormatterOptions> options)
        : base(FileFormatterNames.Json, options)
    {
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter, string? subProviderName, bool? fallbackIncludeScopes)
    {
        JsonFileFormatterOptions formatterOptions = this.GetOptions(subProviderName);
        
        // support obsolete setting on the provider
        formatterOptions.IncludeScopes = formatterOptions.IncludeScopes ?? fallbackIncludeScopes;

        string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
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
        try
        {
            using (MemoryStream mem = new MemoryStream(buffer))
            {
                using (var writer = new Utf8JsonWriter(mem, formatterOptions.JsonWriterOptions))
                {
                    writer.WriteStartObject();
                    var timestampFormat = formatterOptions.TimestampFormat;
                    if (timestampFormat != null)
                    {
                        DateTimeOffset dateTimeOffset = formatterOptions.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
                        if (formatterOptions.InvariantTimestampFormat)
                        {
                            writer.WriteString("Timestamp", dateTimeOffset.ToString(timestampFormat, CultureInfo.InvariantCulture));
                        }
                        else 
                        {
                            writer.WriteString("Timestamp", dateTimeOffset.ToString(timestampFormat));
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
                    WriteScopeInformation(writer, scopeProvider, formatterOptions);
                    writer.WriteEndObject();
                    writer.Flush();

                    textWriter.Write(Encoding.UTF8.GetString(mem.ToArray(), 0, (int)writer.BytesCommitted));

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

    private void WriteScopeInformation(Utf8JsonWriter writer, IExternalScopeProvider scopeProvider, JsonFileFormatterOptions formatterOptions)
    {
        if (formatterOptions.IncludeScopes == true && scopeProvider != null)
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

    private void WriteItem(Utf8JsonWriter writer, KeyValuePair<string, object> item)
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
#if NET5_0_OR_GREATER
                writer.WriteString(key, System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref charValue, 1));
#else
                writer.WriteString(key, charValue.ToString());
#endif
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
