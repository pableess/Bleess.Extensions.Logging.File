using BenchmarkDotNet.Running;

namespace Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run(
            [
                BenchmarkConverter.TypeToBenchmarks(typeof(SimpleFileBenchmarks)),
                BenchmarkConverter.TypeToBenchmarks(typeof(JsonFileBenchmarks)),
                BenchmarkConverter.TypeToBenchmarks(typeof(MultiFileBenchmarks))
            ]);
        }
    }
}
