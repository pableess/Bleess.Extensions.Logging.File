# Bleess.Extensions.Logging.File

High performance rolling file logger for `Microsoft.Extensions.Logging` with no third party dependencies beyond `Microsoft.Extensions.Logging`. Modeled after the standard console logger.

- Rolling files by size and/or time, max number of files
- Text or JSON output, plus custom formatters
- Standard idiomatic configuration (the same shape as the built-in logging providers) from `IConfiguration` or code
- Settings such as path, limits, formatter and flush behavior can be changed while the application is running
- Logging scopes and activity tracking
- High throughput using a bounded queue and a dedicated write thread
- Multiple log files with independent settings

## Supported frameworks

The package targets **.NET 8**, **.NET 10** and **.NET 11**. `net6.0` and `net9.0` were removed in 4.0 because both are out of support; `net8.0` reached end of support on 10 November 2026 but is kept for consumers that cannot move yet.

The library itself is trim and AOT friendly. Binding options from `IConfiguration` requires reflection, exactly like the built-in Microsoft logging providers; if you publish with trimming or Native AOT, preserve the properties of your options types (or configure the provider in code instead).

## Install

```
dotnet add package Bleess.Extensions.Logging.File
```

## Usage

The provider is configured like any other `Microsoft.Extensions.Logging` provider.

```csharp
Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        // uses the "Logging:File" configuration section if one is present
        logging.AddFile();
    });
```

`AddFile()` also has overloads that configure options in code, and `AddSimpleFile()` / `AddJsonFile()` select the built-in formatter:

```csharp
logging.AddFile(o => o.Path = "logs/app.txt");
logging.AddJsonFile(o => o.Path = "logs/app.json");
```

## Configuration

The provider reads the `Logging:File` section. The values below are the defaults.

```json
{
  "Logging": {
    "File": {
      "Path": "logs/log.txt",
      "RollInterval": "Infinite",
      "MaxNumberFiles": 7,
      "MaxFileSizeInMB": 50,
      "FormatterName": "simple",
      "Append": true,
      "FlushToDisk": false,
      "MaxFlushInterval": null,
      "UseUtcTimestamp": false,
      "FormatterOptions": {
        "IncludeScopes": false,
        "SingleLine": false,
        "EmptyLineBetweenMessages": true,
        "TimestampFormat": "yyyy-MM-dd h:mm tt",
        "InvariantTimestampFormat": false,
        "UseUtcTimestamp": false
      },
      "LogLevel": {
        "Default": "Information"
      }
    },
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

| Option | Description |
| --- | --- |
| `Path` | Base file name. May be relative or absolute and may contain environment variables. |
| `RollInterval` | `Infinite` (default), `Year`, `Month`, `Day`, `Hour` or `Minute`. The interval is appended to the file name, for example `logs/log_20240115.txt`. |
| `MaxNumberFiles` | Total number of files to keep, **including the file currently being written**. Must be at least 1. |
| `MaxFileSizeInMB` | Roll when the current file exceeds this size. `0` disables size based rolling. Fractions are allowed. |
| `FormatterName` | `simple` (default), `json`, or the name of a custom formatter. |
| `Append` | `true` appends to an existing file, `false` truncates it when the application starts. |
| `FlushToDisk` | Also flushes the OS file buffers (`fsync` on Linux). Slower, but survives a machine crash. |
| `MaxFlushInterval` | Maximum time in milliseconds between flushes when the queue never drains. `null` (default) or a negative value disables interval based flushing, `0` flushes after every message. |
| `UseUtcTimestamp` | Use UTC for the date portion of the rolling file name. Defaults to local time. |

`FileLoggerOptions.IncludeScopes` is obsolete. Set `FormatterOptions.IncludeScopes` instead; it is used as a fallback when the formatter option is not set. There is no validation of unknown configuration keys, so a typo such as `MaxNumberFile` is silently ignored — see the startup validation note below.

### Startup validation

`Path`, `MaxNumberFiles`, `MaxFileSizeInMB` and the formatter name are validated when the provider is created. An invalid configuration throws at startup instead of silently discarding log messages:

- `Path` must not be empty
- `MaxNumberFiles` must be at least 1
- `MaxFileSizeInMB` must be 0 or greater
- `FormatterName` must match a registered formatter

An unwritable path also fails at startup, because the file is opened eagerly.

## Rolling behavior

Files roll when either the configured date interval changes or the current file exceeds `MaxFileSizeInMB`.

- Size rolling appends a sequence number: `log.txt`, `log_001.txt`, `log_002.txt`, and so on. Once `MaxNumberFiles` files exist the oldest file is overwritten.
- Date rolling appends the interval to the name, and the sequence number is kept: `log_20240115_003.txt`.
- Files are enumerated and cleaned up by name prefix, so use a dedicated directory (or a unique prefix) for each application.
- With `RollInterval = Infinite` and `MaxNumberFiles = 1`, the log file is reused and truncated each time it reaches the size limit.

## Formatting

Two formatters are built in.

```json
"FormatterOptions": {
  "IncludeScopes": false,
  "SingleLine": false,
  "EmptyLineBetweenMessages": true,
  "TimestampFormat": "yyyy-MM-dd h:mm tt",
  "InvariantTimestampFormat": false,
  "UseUtcTimestamp": false
}
```

`JsonFileFormatterOptions` adds `JsonWriterOptions`:

```json
"FormatterOptions": {
  "IncludeScopes": false,
  "EmptyLineBetweenMessages": false,
  "JsonWriterOptions": {
    "Indented": false
  }
}
```

Notes:

- `SingleLine = true` replaces new lines in the message with spaces. It is the safest option for untrusted input, see [Security](#security).
- With `EmptyLineBetweenMessages = false` the JSON formatter emits one JSON object per line (JSON Lines), which can be read with `JsonSerializer.DeserializeAsyncEnumerable` or any JSONL tool.
- `InvariantTimestampFormat = true` formats timestamps with `CultureInfo.InvariantCulture`; otherwise the current culture is used.

## Multiple log files

`AddFiles` adds a composite provider that writes the same log events to several files with independent settings. Configure the providers in code:

```csharp
logging.AddFiles(files =>
{
    // configured from "Logging:Files:TraceLog"
    files.AddFile("TraceLog");

    // configured in code, overrides configuration
    files.AddFile("ErrorLog", o => o.Path = "logs/errors.json")
         .WithJsonFormatter(o =>
         {
             o.IncludeScopes = true;
             o.EmptyLineBetweenMessages = false;
             o.TimestampFormat = "dd h:mm tt";
         })
         .WithMinLevel(LogLevel.Error);
});
```

and, if desired, in configuration under `Logging:Files`:

```json
{
  "Logging": {
    "Files": {
      "TraceLog": {
        "Path": "logs/trace.txt",
        "RollInterval": "Hour",
        "FormatterOptions": {
          "IncludeScopes": false,
          "SingleLine": true,
          "EmptyLineBetweenMessages": false
        },
        "LogLevel": {
          "Default": "Trace",
          "Microsoft": "Warning"
        }
      },
      "ErrorLog": {
        "Path": "logs/errors.json",
        "FormatterName": "json"
      }
    },
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

Each sub provider has its own file, writer thread, queue and filters. `WithFilterRule`, `WithFilter(category, level)`, `WithFilter(predicate)` and `WithMinLevel` add filters for a single sub provider.

## Custom formatters

Derive from `FileFormatter<TOptions>` and register the formatter. The name passed to the base constructor is what `FormatterName` must reference.

```csharp
public sealed record class MyFormatterOptions : FileFormatterOptions
{
    public string Prefix { get; set; } = "app";
}

public sealed class MyFormatter : FileFormatter<MyFormatterOptions>
{
    public const string FormatterName = "my";

    public MyFormatter(IOptionsMonitor<MyFormatterOptions> options, TimeProvider timeProvider)
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
        var options = GetOptions(subProviderName);

        textWriter.Write(options.Prefix);
        textWriter.Write(": ");
        textWriter.WriteLine(logEntry.Formatter(logEntry.State, logEntry.Exception));
    }
}
```

```csharp
logging.AddFile(o => o.FormatterName = MyFormatter.FormatterName);
logging.AddFileFormatter<MyFormatter, MyFormatterOptions>();
```

`GetOptions(subProviderName)` returns the named options for the sub provider when the formatter is used by a composite logger, so one formatter instance can be shared by several files with different settings.

## Reloading settings

Options are read through `IOptionsMonitor`, so changes to configuration (for example `appsettings.json` reload) are applied without restarting:

- the new path, limits, formatter and flush settings are applied by the write thread when it reaches the change in the queue, so a few messages may still be written with the previous settings
- if the new file cannot be opened, the previous file continues to be used
- log level and filter changes are applied immediately by `LoggerFactory`

## Flush behavior

Messages are queued and written by a dedicated thread. The file is flushed when the queue is empty, or after `MaxFlushInterval` milliseconds. A flush writes to the operating system buffers; set `FlushToDisk = true` to also flush to the physical disk.

The queue holds 1024 messages. When it is full, `Log` blocks until there is room (backpressure), which is the same behavior as the console logger. A slow or full disk therefore slows down the threads that log; size your volume accordingly.

## Operational notes

- The log file is opened with `FileShare.Read`, so other processes can read or tail it, but two processes cannot write to the same file. A second writer fails at startup.
- The provider is thread safe. `FileWriter` itself is only used by its write thread; changing limits while running is supported.
- Logging after the provider is disposed is ignored.
- Rolling removes files that match `<path prefix>*<extension>` in the log directory. Do not point `Path` at a directory that contains unrelated files with the same prefix.

## Security

- Log messages can contain user controlled values. A value containing a new line can forge log entries; use `SingleLine = true` or the JSON formatter for untrusted input.
- Log files may contain personal or sensitive data. Restrict the permissions of the log directory (files inherit the process umask on Unix) and consider a log shipper that can redact content.
- There is no limit on total disk usage. Monitor the log volume and use `MaxNumberFiles` / `MaxFileSizeInMB` to bound it.
- Logging is not a security audit trail; it can be modified by anyone with write access to the directory.

## Benchmarks

Measured with BenchmarkDotNet against NReco.Logging.File and Karambolo.Extensions.Logging.File. The numbers below were produced on .NET 8.0.11 (see `benchmark/Benchmarks`); re-run them locally for your hardware.

### Simple text message

```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2605)
AMD Ryzen 5 5600H with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
IterationCount=10  LaunchCount=2  WarmupCount=10
```

| Method                 | Mean            | Gen0     | Allocated |
|----------------------- |----------------:|---------:|----------:|
| Bleess_single_write    |        594.1 ns |   0.0877 |     744 B |
| Karambolo_single_write |        541.3 ns |   0.0839 |     707 B |
| NReco_single_write     |      2,199.3 ns |   0.0839 |     718 B |
| Bleess_10000_writes    |  7,285,301.5 ns | 875.0000 | 7440023 B |
| Karambolo_10000_write  |  8,466,456.8 ns | 843.7500 | 7067165 B |
| NReco_10000_write      | 20,273,803.3 ns | 843.7500 | 7165933 B |

### JSON file

| Method                      | Mean     | Gen0   | Allocated |
|---------------------------- |---------:|-------:|----------:|
| Bleess_single_write_json    | 1.950 us | 0.9766 |   7.99 KB |
| Karambolo_single_write_json | 1.717 us | 0.3204 |   2.64 KB |

### Multiple files

| Method                           | Mean     | Allocated |
|--------------------------------- |---------:|----------:|
| Bleess_multifile_single_write    | 1.249 us |   1.36 KB |
| Karambolo_multifile_single_write | 1.281 us |   1.31 KB |

## Upgrading from 3.x

4.0 contains a few deliberate breaking changes:

- Targets `net8.0`, `net10.0` and `net11.0`; `net6.0` and `net9.0` are gone.
- `FileLoggerProvider` takes a `TimeProvider`. `AddFile()` and `AddFiles()` register `TimeProvider.System`, and a `TimeProvider` registered by the application (for example a `FakeTimeProvider` in tests) takes precedence.
- `FileFormatter.Write` takes `IExternalScopeProvider?` instead of `IExternalScopeProvider`, and `FileFormatter`/`FileFormatter<T>` accept an optional `TimeProvider`.
- Options are validated at startup, and the log file is opened eagerly, so invalid configuration throws instead of silently dropping messages.
- `NamedFileLoggerProviderBuilder.WithFormatter` selects the formatter name you pass, and applies formatter options to the sub provider. Previously the provider alias was used instead, which could result in a missing formatter at runtime.
- `WithFilter(Func<string?, LogLevel, bool>)` now actually uses the predicate.
- Date based rolling can use UTC with `FileLoggerOptions.UseUtcTimestamp`.

## Credits

- Some code was adapted from the .NET runtime (`Microsoft.Extensions.Logging.Console`): https://github.com/dotnet/runtime
- `FileWriter` was originally adapted from https://github.com/nreco/logging and has since been substantially modified.

## License

MIT, see [LICENSE](LICENSE).
