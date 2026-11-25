using BenchmarkDotNet.Running;
using System;
using System.Linq;

namespace Benchmarks
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Any(a => string.Equals(a, "--parity", StringComparison.OrdinalIgnoreCase)))
            {
                ParityReport.Run();
                return;
            }

            // runs every benchmark by default; pass BDN arguments to filter, for example:
            //   dotnet run -c Release --project benchmark/Benchmarks -- --filter '*Json*'
            BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(args.Length == 0 ? ["--filter", "*"] : args, new BenchmarkConfig());
        }
    }
}
