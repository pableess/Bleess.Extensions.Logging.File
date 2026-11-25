# Changelog

All notable changes to this project are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/) and the project uses [Semantic Versioning](https://semver.org/).

## [4.0.0] - 2026-11-24

### Changed

- Targets `net8.0`, `net10.0` and `net11.0`. `net6.0` and `net9.0` were removed (both are out of support, and `net8.0` reached end of support on 10 November 2026).
- Package versions are managed centrally in `Directory.Packages.props` and NuGet audit runs against all dependency graphs.
- `FileLoggerProvider` and `CompositeFileLoggerProvider` take a `TimeProvider`. `AddFile()`/`AddFiles()` register `TimeProvider.System` unless the application already registered one.
- `FileFormatter` and `FileFormatter<TOptions>` accept an optional `TimeProvider`; the built-in formatters use it for timestamps.
- `FileFormatter.Write` accepts `IExternalScopeProvider?` and `LogEntry<TState>` uses `Exception?`.
- Date based rolling can use UTC via `FileLoggerOptions.UseUtcTimestamp`.
- The log file is opened eagerly, so an invalid path fails at startup.
- File names are formatted with the invariant culture, and sequences wider than three digits are still parsed on startup alignment.
- Removed the unused `FileLoggerOptionsSetup` and `RollingFileInfo.CountSubstring`.
- Solutions use the `.slnx` format.

### Added

- `FileLoggerOptionsValidator`: `Path`, `MaxNumberFiles`, `MaxFileSizeInMB` and the formatter name are validated when the provider is created.
- Configuration changes now reload provider options (path, limits, formatter, flush settings) for both `AddFile()` and `AddFiles()`. The writer is reconfigured on the write thread, so it is never closed while a write is in progress.
- Tests for reload, option validation, JSON output, composite filters, custom formatter selection, rolling limits and UTC/local file naming; tests now run on all three target frameworks.
- CI workflow that builds, tests and packs the solution.
- `.editorconfig`, `CHANGELOG.md` and `CONTRIBUTING.md`.
- The benchmark harness accepts BenchmarkDotNet arguments (for example `--filter '*Json*'`), and the benchmarks were re-run on .NET 10 with updated results in the README.

### Fixed

- Configuration reload never applied for the default (`AddFile()`) provider because `IOptionsMonitor.OnChange` reports `Options.DefaultName` (`""`) while the provider compared against `null`.
- `CompositeFileLogger.IsEnabled` returned `false` unless *all* sub loggers accepted the level; it now returns `true` when any sub logger does.
- `CompositeFileLogger.Update` published a partially built dictionary on every iteration, which could drop messages for some sub providers and allocated O(n²) on filter changes.
- `NamedFileLoggerProviderBuilder.WithFilter(Func<string?, LogLevel, bool>)` silently discarded the predicate and added a catch-all rule instead.
- `NamedFileLoggerProviderBuilder.WithFormatter` selected the provider alias as the formatter name instead of the supplied name, which could throw on every log call, and applied formatter options under the wrong options name.
- Unknown formatter names now throw a clear `InvalidOperationException` at startup instead of a `NullReferenceException` per log call.
- Writer swaps (path/interval changes) no longer race with the write thread; the write thread owns the writer lifecycle, and a failed reload keeps the previous writer.
- `RemoveExtraFiles` could delete the file currently being written when `MaxNumberFiles` was 0; the current file is now always skipped.
- `RollingFileInfo.AlignToDirectory` treated files without an extension as having one, and file masks were inconsistent for extensionless paths.
- `WriteCountingFileStream` now overrides `Write(ReadOnlySpan<byte>)`, avoiding an `ArrayPool` rent and copy on every `StreamWriter` flush.
- The JSON formatter no longer calls `MemoryStream.ToArray()` or allocates an intermediate string per message.
- `FileLoggerOptions.IncludeScopes`/formatter `IncludeScopes` fallback no longer mutates the shared options instance.

## [3.0.1]

Previous release. See the git history for details.
