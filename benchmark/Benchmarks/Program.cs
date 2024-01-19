using BenchmarkDotNet.Running;

namespace Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // simple 
            BenchmarkRunner.Run<SimpleFileBenchmarks>();
            
            // json
            BenchmarkRunner.Run<JsonFileBenchmarks>();
            
            // composite
            BenchmarkRunner.Run<MultiFileBenchmarks>();
        }
    }
}
