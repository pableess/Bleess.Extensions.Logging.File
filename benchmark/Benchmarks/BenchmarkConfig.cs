using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Order;

namespace Benchmarks;

/// <summary>
/// Shared configuration: memory and threading diagnosers for every benchmark, JSON export for
/// regenerating the documentation tables, and fastest-first ordering so ratios are easy to read.
/// </summary>
internal sealed class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddLogger(ConsoleLogger.Default);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddDiagnoser(ThreadingDiagnoser.Default);
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddExporter(MarkdownExporter.GitHub);
        AddExporter(CsvExporter.Default);
        AddExporter(HtmlExporter.Default);
        AddExporter(JsonExporter.Full);
        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
    }
}
