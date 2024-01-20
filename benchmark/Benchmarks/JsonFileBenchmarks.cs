extern alias Bleess;

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// alias ns
using Bleess::Microsoft.Extensions.Logging;


namespace Benchmarks
{
    [MemoryDiagnoser()]
    [SimpleJob(2, 10, 10)]
    public partial class JsonFileBenchmarks
    {


        public JsonFileBenchmarks()
        {
            this.SetupBleessLogging();
            this.SetupKaramboloLogging();
        }

        // specific benchmarks for bleess logging

        private Microsoft.Extensions.Logging.ILogger? _bleessLogger;
        public void SetupBleessLogging()
        {
            ServiceCollection sc = new ServiceCollection();
            sc.AddLogging(lobBuilder =>
            {
                lobBuilder.AddJsonFile(o =>
                {
                    o.Path = "logs/Bleess.txt";
                    o.MaxFileSizeInMB = 1024; // 1 GB
                    o.MaxNumberFiles = 31;
                },
                o =>
                {
                    o.EmptyLineBetweenMessages = true;
                    o.IncludeScopes = false;
                    o.UseUtcTimestamp = true;
                });
            });

            var sp = sc.BuildServiceProvider();

            _bleessLogger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger("default");

        }


        [Benchmark]
        [BenchmarkCategory("json")]
        public void Bleess_single_write_json()
        {
            _bleessLogger!.LogError("This is a test message with some parameters {a}, {b}, {c}", 100, "some string", true);
        }
    }
}
