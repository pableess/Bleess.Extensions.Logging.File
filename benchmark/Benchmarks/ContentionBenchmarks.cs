using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Benchmarks;

/// <summary>
/// Several threads logging at the same time. Measures contention on the provider's queue and the
/// cost of the bounded queue backpressure.
/// </summary>
[SimpleJob(2, 10, 10)]
public class ContentionBenchmarks
{
    private const int MessagesPerProducer = 2000;

    [Params(1, 4, 8)]
    public int Producers { get; set; }

    [Params(MessageShape.Small, MessageShape.Large)]
    public MessageShape Shape { get; set; }

    private MessageSet _messages = null!;
    private TempDirectory _directory = null!;
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
    public void Bleess_contention() => RunProducers(_bleess.Logger);

    [Benchmark]
    public void Karambolo_contention() => RunProducers(_karambolo.Logger);

    [Benchmark]
    public void NReco_contention() => RunProducers(_nReco.Logger);

    private void RunProducers(ILogger logger)
    {
        var threads = new Thread[Producers];

        for (int i = 0; i < threads.Length; i++)
        {
            int index = i;
            threads[i] = new Thread(
                () =>
                {
                    for (int message = 0; message < MessagesPerProducer; message++)
                    {
                        _messages.Write(logger);
                    }
                })
            {
                IsBackground = true,
                Name = $"producer-{index}",
            };

            threads[i].Start();
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }
    }
}
