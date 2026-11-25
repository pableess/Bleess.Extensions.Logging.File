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
dotnet run -c Release --project benchmark/Benchmarks                  # full suite
dotnet run -c Release --project benchmark/Benchmarks -- --filter '*Json*'
dotnet run -c Release --project benchmark/Benchmarks -- --parity      # byte parity report
```

See `benchmark/README.md` for what each benchmark measures and the known configuration differences between the compared libraries. Do not compare numbers across machines; compare ratios within one run.

## Release

Publishing runs from `.github/workflows/publish.yml` and uses [NuGet trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing), so no long-lived NuGet API key is stored in the repository.

1. On nuget.org, open your account menu and choose **Trusted Publishing**, then add a policy:
   - **Repository Owner:** `pableess`
   - **Repository:** `Bleess.Extensions.Logging.File`
   - **Workflow File:** `publish.yml` (the file name only)
   - **Environment:** leave empty
2. Add a `NUGET_USER` repository secret containing your nuget.org profile name (not your email address).
3. Tag the release commit and push the tag, for example `git tag v4.0.1 && git push origin v4.0.1`.
4. The workflow builds and tests on `net8.0`, `net10.0` and `net11.0`, packs `Bleess.Extensions.Logging.File.<version>.nupkg`, exchanges the GitHub OIDC token for a short-lived API key and pushes the package to nuget.org.

The tag is the source of truth for the package version: the workflow passes it as `VersionPrefix`/`VersionSuffix`, so the project file does not need to be edited for each release. A tag such as `v4.1.0-rc.1` produces a prerelease package. The workflow can also be started manually with a version input, and `--skip-duplicate` makes reruns safe.

## Conventions

- Keep the library trim and AOT friendly. Avoid reflection over application types; configuration binding is the only reflection based path and it is annotated.
- New public API needs XML documentation (`GenerateDocumentationFile` is on).
- Options types are mutable classes; do not mutate options instances returned by `IOptionsMonitor` (they are shared).
- The write thread owns the `FileWriter`. Do not touch the writer from other threads; hand work to it through the queue.
- Add a regression test for every bug fix. Tests should use a unique directory and a deterministic `TimeProvider`.
- Update `CHANGELOG.md` for user visible changes.
- Keep the build warning free; the library enables `latest-recommended` analyzers and AOT/trim analyzers.
