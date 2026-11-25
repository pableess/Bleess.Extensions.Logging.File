using BenchmarkDotNet.Attributes;

namespace Benchmarks;

/// <summary>
/// Cost of <c>Log()</c> on the calling thread: format + enqueue. The write thread and file I/O are
/// not part of the measurement; use <see cref="EndToEndBenchmarks"/> for that.
/// </summary>
[SimpleJob(2, 10, 10)]
public class EnqueueTextBenchmarks
{
    [Params(MessageShape.Small, MessageShape.Medium, MessageShape.Large, MessageShape.Exception)]
    public MessageShape Shape { get; set; }

    private TempDirectory _directory = null!;
    private MessageSet _messages = null!;
    private LoggingTarget _bleess = null!;
    private LoggingTarget _karambolo = null!;
    private LoggingTarget _nReco = null!;

    [GlobalSetup]
    public void Setup()
    {
        _messages = MessageSet.Create(Shape);
        _directory = new TempDirectory();

        _bleess = TargetFactory.Create(Library.Bleess, TargetFactory.Options(_directory, "bleess.log", LogFormat.MessageOnly));
        _karambolo = TargetFactory.Create(Library.Karambolo, TargetFactory.Options(_directory, "karambolo.log", LogFormat.MessageOnly));
        _nReco = TargetFactory.Create(Library.NReco, TargetFactory.Options(_directory, "nreco.log", LogFormat.MessageOnly));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _bleess.Dispose();
        _karambolo.Dispose();
        _nReco.Dispose();
        _directory.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void Bleess_single_write() => _messages.Write(_bleess.Logger);

    [Benchmark]
    public void Karambolo_single_write() => _messages.Write(_karambolo.Logger);

    [Benchmark]
    public void NReco_single_write() => _messages.Write(_nReco.Logger);
}

/// <summary>
/// JSON formatting cost on the calling thread. NReco has no JSON formatter.
/// </summary>
[SimpleJob(2, 10, 10)]
public class EnqueueJsonBenchmarks
{
    [Params(MessageShape.Small, MessageShape.Medium, MessageShape.Large, MessageShape.Exception)]
    public MessageShape Shape { get; set; }

    private TempDirectory _directory = null!;
    private MessageSet _messages = null!;
    private LoggingTarget _bleess = null!;
    private LoggingTarget _karambolo = null!;

    [GlobalSetup]
    public void Setup()
    {
        _messages = MessageSet.Create(Shape);
        _directory = new TempDirectory();

        _bleess = TargetFactory.Create(Library.Bleess, TargetFactory.Options(_directory, "bleess.json", LogFormat.Json));
        _karambolo = TargetFactory.Create(Library.Karambolo, TargetFactory.Options(_directory, "karambolo.json", LogFormat.Json));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _bleess.Dispose();
        _karambolo.Dispose();
        _directory.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void Bleess_single_write_json() => _messages.Write(_bleess.Logger);

    [Benchmark]
    public void Karambolo_single_write_json() => _messages.Write(_karambolo.Logger);
}
