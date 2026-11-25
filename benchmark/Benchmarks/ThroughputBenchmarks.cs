using BenchmarkDotNet.Attributes;

namespace Benchmarks;

/// <summary>
/// End to end cost: log a batch, then dispose the provider so the write thread drains the queue,
/// flushes and closes the file. OS buffered flush (no fsync), which is the default configuration.
/// One invocation per iteration because the body disposes the provider.
/// </summary>
[SimpleJob(2, 10, 10, 1)]
public class EndToEndBenchmarks
{
    private const int MessageCount = 500;

    [Params(MessageShape.Small, MessageShape.Medium, MessageShape.Large, MessageShape.Exception)]
    public MessageShape Shape { get; set; }

    private MessageSet _messages = null!;
    private TempDirectory _directory = null!;
    private LoggingTarget _bleess = null!;
    private LoggingTarget _karambolo = null!;
    private LoggingTarget _nReco = null!;

    [GlobalSetup]
    public void Setup() => _messages = MessageSet.Create(Shape);

    [IterationSetup]
    public void SetupIteration()
    {
        _directory = new TempDirectory();
        _bleess = TargetFactory.Create(Library.Bleess, TargetFactory.Options(_directory, "bleess.log", LogFormat.MessageOnly));
        _karambolo = TargetFactory.Create(Library.Karambolo, TargetFactory.Options(_directory, "karambolo.log", LogFormat.MessageOnly));
        _nReco = TargetFactory.Create(Library.NReco, TargetFactory.Options(_directory, "nreco.log", LogFormat.MessageOnly));
    }

    [IterationCleanup]
    public void CleanupIteration()
    {
        _bleess.Dispose();
        _karambolo.Dispose();
        _nReco.Dispose();
        _directory.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void Bleess_end_to_end()
    {
        for (int i = 0; i < MessageCount; i++)
        {
            _messages.Write(_bleess.Logger);
        }

        _bleess.Dispose();
    }

    [Benchmark]
    public void Karambolo_end_to_end()
    {
        for (int i = 0; i < MessageCount; i++)
        {
            _messages.Write(_karambolo.Logger);
        }

        _karambolo.Dispose();
    }

    [Benchmark]
    public void NReco_end_to_end()
    {
        for (int i = 0; i < MessageCount; i++)
        {
            _messages.Write(_nReco.Logger);
        }

        _nReco.Dispose();
    }
}

/// <summary>
/// End to end with a 64 KB file size limit, so the batch rolls the file several times and exercises
/// roll, truncate and retention cleanup.
/// </summary>
[SimpleJob(2, 10, 10, 1)]
public class RollingBenchmarks
{
    private const int MessageCount = 5000;
    private const long MaxFileSizeBytes = 64 * 1024;
    private const int MaxNumberFiles = 4;

    private MessageSet _messages = null!;
    private TempDirectory _directory = null!;
    private LoggingTarget _bleess = null!;
    private LoggingTarget _karambolo = null!;
    private LoggingTarget _nReco = null!;

    [GlobalSetup]
    public void Setup() => _messages = MessageSet.Create(MessageShape.Small);

    [IterationSetup]
    public void SetupIteration()
    {
        _directory = new TempDirectory();
        _bleess = TargetFactory.Create(Library.Bleess, TargetFactory.Options(_directory, "bleess.log", LogFormat.MessageOnly, maxFileSizeBytes: MaxFileSizeBytes, maxNumberFiles: MaxNumberFiles));
        _karambolo = TargetFactory.Create(Library.Karambolo, TargetFactory.Options(_directory, "karambolo.log", LogFormat.MessageOnly, maxFileSizeBytes: MaxFileSizeBytes, maxNumberFiles: MaxNumberFiles));
        _nReco = TargetFactory.Create(Library.NReco, TargetFactory.Options(_directory, "nreco.log", LogFormat.MessageOnly, maxFileSizeBytes: MaxFileSizeBytes, maxNumberFiles: MaxNumberFiles));
    }

    [IterationCleanup]
    public void CleanupIteration()
    {
        _bleess.Dispose();
        _karambolo.Dispose();
        _nReco.Dispose();
        _directory.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void Bleess_rolling()
    {
        for (int i = 0; i < MessageCount; i++)
        {
            _messages.Write(_bleess.Logger);
        }

        _bleess.Dispose();
    }

    [Benchmark]
    public void Karambolo_rolling()
    {
        for (int i = 0; i < MessageCount; i++)
        {
            _messages.Write(_karambolo.Logger);
        }

        _karambolo.Dispose();
    }

    [Benchmark]
    public void NReco_rolling()
    {
        for (int i = 0; i < MessageCount; i++)
        {
            _messages.Write(_nReco.Logger);
        }

        _nReco.Dispose();
    }
}

/// <summary>
/// Cost of durable writes: <c>FlushToDisk = true</c> flushes to the physical disk (fsync) on every
/// flush. NReco and Karambolo do not expose an equivalent option.
/// </summary>
[SimpleJob(2, 10, 10, 1)]
public class FlushToDiskBenchmarks
{
    private const int MessageCount = 200;

    [Params(true, false)]
    public bool FlushToDisk { get; set; }

    private MessageSet _messages = null!;
    private TempDirectory _directory = null!;
    private LoggingTarget _bleess = null!;

    [GlobalSetup]
    public void Setup() => _messages = MessageSet.Create(MessageShape.Small);

    [IterationSetup]
    public void SetupIteration()
    {
        _directory = new TempDirectory();
        _bleess = TargetFactory.Create(
            Library.Bleess,
            TargetFactory.Options(_directory, "bleess.log", LogFormat.MessageOnly, flushToDisk: FlushToDisk));
    }

    [IterationCleanup]
    public void CleanupIteration()
    {
        _bleess.Dispose();
        _directory.Dispose();
    }

    [Benchmark]
    public void Bleess_flush_to_disk()
    {
        for (int i = 0; i < MessageCount; i++)
        {
            _messages.Write(_bleess.Logger);
        }

        _bleess.Dispose();
    }
}
