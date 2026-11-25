using BenchmarkDotNet.Running;

namespace Benchmarks
{
    internal class Program
    {
        // runs every benchmark by default; pass BDN arguments to filter, for example:
        //   dotnet run -c Release --project benchmark/Benchmarks -- --filter '*Json*'
        static void Main(string[] args) =>
            BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(args.Length == 0 ? ["--filter", "*"] : args);
    }
}
