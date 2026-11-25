using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;

namespace Benchmarks;

/// <summary>
/// Provider creation cost. Bleess scans the log directory to align the sequence number when rolling
/// by size, so the number of existing files affects startup.
/// </summary>
[SimpleJob(2, 10, 10)]
public class StartupBenchmarks
{
    [Params(0, 100, 1000)]
    public int ExistingFiles { get; set; }

    private TempDirectory _directory = null!;

    [GlobalSetup]
    public void Setup()
    {
        _directory = new TempDirectory();

        for (int i = 0; i < ExistingFiles; i++)
        {
            string path = Path.Combine(_directory.Path, $"startup_{i:000}.log");
            File.WriteAllText(path, "existing");
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-ExistingFiles + i));
        }
    }

    [GlobalCleanup]
    public void Cleanup() => _directory.Dispose();

    [Benchmark]
    public void Bleess_provider_startup()
    {
        using var target = TargetFactory.Create(
            Library.Bleess,
            TargetFactory.Options(_directory, "startup.log", LogFormat.MessageOnly, maxFileSizeBytes: 64 * 1024, maxNumberFiles: 4));
    }
}

/// <summary>
/// Baseline for the harness and the caller side argument allocation, with no provider involved.
/// </summary>
[SimpleJob(2, 10, 10)]
public class NoOpBenchmarks
{
    [Benchmark(Baseline = true)]
    public void Null_logger() =>
        NullLogger.Instance.LogError("This is a test message with some parameters {a}, {b}, {c}", 100, "some string", true);
}
