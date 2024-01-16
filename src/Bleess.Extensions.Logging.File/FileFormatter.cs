using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#nullable enable

namespace Bleess.Extensions.Logging.File;

/// <summary>
/// Base class for file formatter
/// </summary>
public abstract class FileFormatter
{
    /// <summary>
    /// Cache key for options 
    /// </summary>
    protected const string DefaultOptionsKey = "Default";

    /// <summary>
    /// Creates a <see cref="FileFormatter"/>
    /// </summary>
    /// <param name="name"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public FileFormatter(string name)
    {
        Name = name ?? throw new ArgumentNullException(name);
    }

    /// <summary>
    /// Gets the name associated with the console log formatter.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Writes the log message to the specified TextWriter.
    /// </summary>
    /// <remarks>
    /// if the formatter wants to write colors to the console, it can do so by embedding ANSI color codes into the string
    /// </remarks>
    /// <param name="logEntry">The log entry.</param>
    /// <param name="scopeProvider">The provider of scope data.</param>
    /// <param name="textWriter">The string writer embedding ansi code for colors.</param>
    /// <param name="subProviderName">The name of the sub provider</param>
    /// <param name="fallbackIncludeScopes">fallback value for including the scopes</param>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    public abstract void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter, string? subProviderName = null, bool? fallbackIncludeScopes = null);
}
