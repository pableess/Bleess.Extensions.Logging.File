extern alias Karambolo;

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
using Karambolo::Microsoft.Extensions.Logging;
using Karambolo::Karambolo.Extensions.Logging.File;

namespace Benchmarks
{
    public partial class SimpleFileBenchmarks
    {
        private Microsoft.Extensions.Logging.ILogger? _karamboloLogger;
        public void SetupKaramboloLogging() 
        {
            ServiceCollection sc = new ServiceCollection();
            sc.AddLogging(logBuilder => 
            {
                logBuilder.AddFile(o => 
                {
                    o.MaxFileSize = 1024 * 1024 * 1024; // 1 MB

                    o.MaxQueueSize = 1024;

                    o.IncludeScopes = false;

                    o.Files = new LogFileOptions[] 
                    {
                        new LogFileOptions{ Path = "logs/karambolo.txt" }
                    };
                });
            });

            var sp = sc.BuildServiceProvider();

            _karamboloLogger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger("default");
        }


        [Benchmark]
        [BenchmarkCategory("text")]
        public void Karambolo_single_write() 
        {
            _karamboloLogger!.LogError("This is a test message with some parameters {a}, {b}, {c}", 100, "some string", true);
        }

        [Benchmark]
        [BenchmarkCategory("text", "10000")]
        public void Karambolo_10000_write()
        {
            for (int i = 0; i < 10000; i++)
            {
                _karamboloLogger!.LogError("This is a test message with some parameters {a}, {b}, {c}", 100, "some string", true);
            }
        }
    }
}
