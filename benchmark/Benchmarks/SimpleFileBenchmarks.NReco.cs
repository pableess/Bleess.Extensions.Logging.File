extern alias NReco;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Loggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// alias ns
using NReco::NReco.Logging.File;

namespace Benchmarks
{
    public partial class SimpleFileBenchmarks
    {
        private Microsoft.Extensions.Logging.ILogger? _nRecoLogger;
        public void SetupNRecoLogging() 
        {
            ServiceCollection sc = new ServiceCollection();
            sc.AddLogging(logBuilder => 
            {
                logBuilder.AddFile("logs/NReco.txt", o => 
                {
                    
                    o.FileSizeLimitBytes = 1024 * 1024 * 1024; // 1 GB
                    o.MaxRollingFiles = 31;
                    o.UseUtcTimestamp = true;
                });
            });

            var sp = sc.BuildServiceProvider();

            _nRecoLogger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger("default");
        }


        [Benchmark]
        [BenchmarkCategory("text")]
        public void NReco_single_write() 
        {
            _nRecoLogger!.LogError("This is a test message with some parameters {a}, {b}, {c}", 100, "some string", true);
        }

        [Benchmark]
        [BenchmarkCategory("text", "10000")]
        public void NReco_10000_write()
        {
            for (int i = 0; i < 10000; i++)
            {
                _nRecoLogger!.LogError("This is a test message with some parameters {a}, {b}, {c}", 100, "some string", true);
            }
        }
    }
}
