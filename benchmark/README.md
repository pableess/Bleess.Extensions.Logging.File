# Benchmarks

The benchmark suite compares `Bleess.Extensions.Logging.File` with `Karambolo.Extensions.Logging.File` (4.1.1) and `NReco.Logging.File` (1.4.0), and tracks the cost of the provider's own features.

```
dotnet run -c Release --project benchmark/Benchmarks                  # full suite
dotnet run -c Release --project benchmark/Benchmarks -- --filter '*Json*'
dotnet run -c Release --project benchmark/Benchmarks -- --parity      # byte parity report
```

Results are written to `BenchmarkDotNet.Artifacts/results` (CSV, HTML, GitHub markdown and JSON). The published tables are in [RESULTS.md](RESULTS.md); the summary tables are in the main [README](../README.md).

## What is measured

| Benchmark | Question it answers | Notes |
| --- | --- | --- |
| `EnqueueTextBenchmarks` | Cost of `Log()` on the calling thread: format + enqueue | The write thread and file I/O are **not** included. This is the latency the application pays. |
| `EnqueueJsonBenchmarks` | Same, for the JSON formatter | NReco has no JSON formatter. |
| `EndToEndBenchmarks` | Throughput including the write thread, flush and close | Logs a batch, then disposes the provider; OS buffered flush (no fsync). |
| `RollingBenchmarks` | Cost of size based rolling and retention cleanup | 64 KB files, so the batch rolls several times. |
| `FlushToDiskBenchmarks` | Cost of durable writes (`fsync`) | Bleess only; NReco and Karambolo do not expose an equivalent option. |
| `ContentionBenchmarks` | Several threads logging at once | Measures queue contention and backpressure. |
| `MultiFileBenchmarks` | Cost of writing the same event to 1, 2 or 4 files | |
| `StartupBenchmarks` | Provider creation with 0, 100 or 1,000 existing files | Bleess scans the directory to align the rolling sequence. |
| `NoOpBenchmarks` | Harness and caller side baseline | `NullLogger`, no provider. |

Parameters: message shape (`Small` 66 B, `Medium` 1 KB, `Large` 16 KB, `Exception` with a stack trace), producer count (1, 4, 8), file count (1, 2, 4).

The full matrix is 68 cases. A run with the default job (2 launches, 10 warmup, 10 iterations) takes roughly 30-40 minutes on a laptop. Use `--launchCount 1 --warmupCount 1 --iterationCount 2` for a quick smoke run while iterating on the harness.

## Parity

The comparison is only meaningful if every library does the same work, so the benchmarks use a `MessageOnly` output profile: each library is configured to write just the formatted message, with scopes disabled and the same message/arguments. The `--parity` mode writes 1,000 messages through every library and prints the bytes written per message:

```
Format       Library           Bytes    B/msg  Preview
MessageOnly  Bleess           67,000     67.0  This is a test message ...
MessageOnly  Karambolo        67,000     67.0  This is a test message ...
MessageOnly  NReco            67,000     67.0  This is a test message ...
```

Identical byte counts mean the providers are doing the same amount of output work. The `Realistic` profile uses each library's native text layout (scopes off, UTC where supported) and is reported separately; its byte counts differ because the layouts differ.

## Library configurations

| | Bleess | Karambolo | NReco |
| --- | --- | --- | --- |
| Text formatter | custom message-only formatter | custom message-only `IFileLogEntryTextBuilder` | `FormatLogEntry = m => m.Message` |
| Queue | bounded at 1024, **blocks** when full | `MaxQueueSize = 1024`, **discards** when full | bounded, blocks when full |
| Flush | when the queue drains | `FileAccessMode.KeepOpen` (buffered) | provider default |
| Multi-file | one composite provider, one writer thread per file | one provider with several `Files` | one provider per file |
| Rolling | automatic, `MaxNumberFiles` | `<counter>` path template | automatic, `MaxRollingFiles` |
| JSON | built in | separate package | not available |

Differences that cannot be normalized are called out here rather than hidden:

- Karambolo discards entries when its bounded queue is full, while the other two block the caller. For large messages this shows up as a lower enqueue cost for Karambolo because it is doing less work, not because it is faster.
- Bleess formats the message once per destination file in the composite provider; Karambolo formats once and writes to all files. The multi-file table reflects that design difference.
- NReco has no JSON formatter and no multi-file provider, so the JSON and multi-file comparisons use the closest equivalent configuration.

## Environment

Benchmark results are specific to the machine, OS, filesystem and runtime. The main README records the exact environment of the published numbers. Do not compare numbers across environments; compare the ratio between libraries within a single run.

The suite uses a unique temporary directory per target and deletes it when the target is disposed, so a run does not accumulate log files. By default the targets write under the system temp directory (`/tmp` on Linux, usually tmpfs), which isolates the libraries' CPU and memory cost from the physical disk. Set `BENCHMARK_LOG_ROOT` to a directory on your production filesystem if you need disk-bound numbers.
