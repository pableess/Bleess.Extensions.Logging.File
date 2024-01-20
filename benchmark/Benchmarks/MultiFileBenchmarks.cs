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
    public partial class MultiFileBenchmarks
    {


        public MultiFileBenchmarks()
        {
            this.SetupBleessLogging();
            this.SetupKaramboloLogging();
        }

        // specific benchmarks for bleess logging

        private Microsoft.Extensions.Logging.ILogger? _bleessLogger;
        public void SetupBleessLogging()
        {
            ServiceCollection sc = new ServiceCollection();
            sc.AddLogging(logBuilder =>
            {
                // file 1
                logBuilder.AddFiles(m => 
                {
                    m.AddFile("File1", o => 
                    {
                        o.Path = "logs/Bleess1.txt";
                        o.MaxFileSizeInMB = 1024 * 1024; // 1 GB
                        o.MaxNumberFiles = 31;
                    }).WithSimpleFormatter(o =>
                    {
                        o.SingleLine = true;
                        o.EmptyLineBetweenMessages = true;
                        o.IncludeScopes = false;
                        o.UseUtcTimestamp = true;
                    });
              
                    // file 2 
                    m.AddFile("File2", o => 
                    {
                        o.Path = "logs/Bleess2.txt";
                        o.MaxFileSizeInMB = 1024 * 1024; // 1 GB
                        o.MaxNumberFiles = 31;
                    }).WithSimpleFormatter(o => 
                    {
                        o.SingleLine = true;
                        o.EmptyLineBetweenMessages = true;
                        o.IncludeScopes = false;
                        o.UseUtcTimestamp = true;
                    });
                });
            });

            var sp = sc.BuildServiceProvider();

            _bleessLogger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger("default");
        }


        [Benchmark]
        [BenchmarkCategory("multifile")]
        public void Bleess_multifile_single_write()
        {
            _bleessLogger!.LogError("This is a test message with some parameters {a}, {b}, {c}", 100, "some string", true);
        }
    }
}
