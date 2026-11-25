using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;

namespace Benchmarks;

internal enum Library
{
    Bleess,
    Karambolo,
    NReco,
}

internal enum LogFormat
{
    /// <summary>The formatter writes only the message. This is the fair comparison across libraries.</summary>
    MessageOnly,

    /// <summary>Each library's native text format with scopes disabled and UTC timestamps.</summary>
    Realistic,

    /// <summary>JSON output. Only Bleess and Karambolo support it.</summary>
    Json,
}

public enum MessageShape
{
    Small,
    Medium,
    Large,
    Exception,
}

internal sealed record TargetOptions(
    string Directory,
    string FileName,
    LogFormat Format,
    bool IncludeScopes = false,
    bool FlushToDisk = false,
    int FileCount = 1,
    long MaxFileSizeBytes = 8 * 1024 * 1024,
    int MaxNumberFiles = 4)
{
    public const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

    public string PathFor(int index)
    {
        if (index == 0)
        {
            return Path.Combine(Directory, FileName);
        }

        string name = Path.GetFileNameWithoutExtension(FileName);
        string extension = Path.GetExtension(FileName);
        return Path.Combine(Directory, $"{name}_{index}{extension}");
    }
}

internal sealed class LoggingTarget : IDisposable
{
    private readonly ServiceProvider _services;

    public LoggingTarget(Library library, ILogger logger, ServiceProvider services)
    {
        Library = library;
        Logger = logger;
        _services = services;
    }

    public Library Library { get; }

    public ILogger Logger { get; }

    public void Dispose() => _services.Dispose();
}

internal sealed class TempDirectory : IDisposable
{
    // defaults to the system temp directory (tmpfs on most Linux systems); set BENCHMARK_LOG_ROOT
    // to run against a real disk instead
    private static readonly string Root =
        Environment.GetEnvironmentVariable("BENCHMARK_LOG_ROOT")
        ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bleess-bench");

    public TempDirectory()
    {
        Path = System.IO.Path.Combine(Root, Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public long TotalBytes => System.IO.Directory.Exists(Path)
        ? new DirectoryInfo(Path).GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length)
        : 0;

    public void Dispose()
    {
        try
        {
            System.IO.Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

/// <summary>
/// The message and arguments passed to every logger, so all libraries format the same input.
/// </summary>
internal sealed class MessageSet
{
    private const string Template = "This is a test message with some parameters {a}, {b}, {c}";

    private MessageSet(MessageShape shape, string message, string payload, Exception? exception)
    {
        Shape = shape;
        Message = message;
        Payload = payload;
        Exception = exception;
    }

    public MessageShape Shape { get; }

    public string Message { get; }

    public string Payload { get; }

    public Exception? Exception { get; }

    public static MessageSet Create(MessageShape shape) => shape switch
    {
        MessageShape.Small => new MessageSet(shape, Template, string.Empty, null),
        MessageShape.Medium => new MessageSet(shape, "payload {payload}", new string('x', 1024), null),
        MessageShape.Large => new MessageSet(shape, "payload {payload}", new string('x', 16 * 1024), null),
        MessageShape.Exception => new MessageSet(shape, Template, string.Empty, CreateException()),
        _ => throw new ArgumentOutOfRangeException(nameof(shape)),
    };

    public void Write(ILogger logger)
    {
        switch (Shape)
        {
            case MessageShape.Small:
            case MessageShape.Exception:
                logger.LogError(Exception, Message, 100, "some string", true);
                break;
            default:
                logger.LogError(Exception, Message, Payload);
                break;
        }
    }

    private static Exception CreateException()
    {
        try
        {
            throw new InvalidOperationException("Benchmark exception with a representative stack trace.");
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}

internal static class TargetFactory
{
    public static LoggingTarget Create(Library library, TargetOptions options) => library switch
    {
        Library.Bleess => BleessTargetFactory.Create(options),
        Library.Karambolo => KaramboloTargetFactory.Create(options),
        Library.NReco => NRecoTargetFactory.Create(options),
        _ => throw new ArgumentOutOfRangeException(nameof(library)),
    };

    public static TargetOptions Options(TempDirectory directory, string fileName, LogFormat format, bool flushToDisk = false, int fileCount = 1, long maxFileSizeBytes = 8 * 1024 * 1024, int maxNumberFiles = 4)
        => new(directory.Path, fileName, format, IncludeScopes: false, FlushToDisk: flushToDisk, FileCount: fileCount, MaxFileSizeBytes: maxFileSizeBytes, MaxNumberFiles: maxNumberFiles);
}
