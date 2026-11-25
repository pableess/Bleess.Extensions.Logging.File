using BenchmarkDotNet.Attributes;

namespace Benchmarks;

/// <summary>
/// Cost of writing the same event to several files. Bleess uses one composite provider with one
/// writer thread per file, Karambolo uses one provider with several files, NReco needs one provider
/// per file.
/// </summary>
[SimpleJob(2, 10, 10)]
public class MultiFileBenchmarks
{
    [Params(1, 2, 4)]
    public int FileCount { get; set; }

    private MessageSet _messages = null!;
    private TempDirectory _directory = null!;
    private LoggingTarget _bleess = null!;
    private LoggingTarget _karambolo = null!;
    private LoggingTarget _nReco = null!;

    [GlobalSetup]
    public void Setup()
    {
        _messages = MessageSet.Create(MessageShape.Small);
        _directory = new TempDirectory();

        _bleess = TargetFactory.Create(Library.Bleess, TargetFactory.Options(_directory, "bleess.log", LogFormat.MessageOnly, fileCount: FileCount));
        _karambolo = TargetFactory.Create(Library.Karambolo, TargetFactory.Options(_directory, "karambolo.log", LogFormat.MessageOnly, fileCount: FileCount));
        _nReco = TargetFactory.Create(Library.NReco, TargetFactory.Options(_directory, "nreco.log", LogFormat.MessageOnly, fileCount: FileCount));
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
    public void Bleess_multifile_single_write() => _messages.Write(_bleess.Logger);

    [Benchmark]
    public void Karambolo_multifile_single_write() => _messages.Write(_karambolo.Logger);

    [Benchmark]
    public void NReco_multifile_single_write() => _messages.Write(_nReco.Logger);
}
