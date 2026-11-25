# Contributing

## Build

```
dotnet build Bleess.Extensions.Logging.File.slnx
```

The library multi-targets `net8.0`, `net10.0` and `net11.0`. The .NET 11 SDK can build all of them; the targeting packs are restored automatically.

## Test

```
dotnet test tests/Tests.csproj
```

Tests run on all three target frameworks. They use the internal `TestTimeProvider` so rolling and timestamp assertions are deterministic, and each test writes to a unique temporary directory.

## Benchmarks

```
dotnet run -c Release --project benchmark/Benchmarks -- --filter '*'
```

## Conventions

- Keep the library trim and AOT friendly. Avoid reflection over application types; configuration binding is the only reflection based path and it is annotated.
- New public API needs XML documentation (`GenerateDocumentationFile` is on).
- Options types are mutable classes; do not mutate options instances returned by `IOptionsMonitor` (they are shared).
- The write thread owns the `FileWriter`. Do not touch the writer from other threads; hand work to it through the queue.
- Add a regression test for every bug fix. Tests should use a unique directory and a deterministic `TimeProvider`.
- Update `CHANGELOG.md` for user visible changes.
- Keep the build warning free; the library enables `latest-recommended` analyzers and AOT/trim analyzers.
