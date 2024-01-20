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
using Microsoft.Extensions.Options;

namespace Benchmarks
{
    public partial class MultiFileBenchmarks
    {
        private Microsoft.Extensions.Logging.ILogger? _karamboloLogger;
        public void SetupKaramboloLogging() 
        {
            ServiceCollection sc = new ServiceCollection();
            sc.AddLogging(logBuilder => 
            {
                logBuilder.AddFile(o => 
                {
                    o.MaxFileSize = 1024 * 1024 * 1024; // 1 GB
                    o.IncludeScopes = false;
                    o.MaxQueueSize = 1024;

                    o.Files = new LogFileOptions[]
                    {
                        new LogFileOptions{ Path = "logs/karambolo1.txt" }
                    };
                });

                logBuilder.AddFile<AltFileLoggerProvider>(configure: o =>
                {
                    o.MaxFileSize = 1024 * 1024 * 1024; // 1 GB
                    o.IncludeScopes = false;
                    o.MaxQueueSize = 1024;

                    o.Files = new LogFileOptions[]
                    {
                        new LogFileOptions{ Path = "logs/karambolo2.txt" }
                    };
                });
            });

            var sp = sc.BuildServiceProvider();

            _karamboloLogger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger("default");
        }


        [Benchmark]
        [BenchmarkCategory("multifile")]
        public void Karambolo_multifile_single_write() 
        {
            _karamboloLogger!.LogError("This is a test message with some parameters {a}, {b}, {c}", 100, "some string", true);
        }
    }

    [ProviderAlias("File2")]
    class AltFileLoggerProvider : FileLoggerProvider
    {
        public AltFileLoggerProvider(FileLoggerContext context, IOptionsMonitor<FileLoggerOptions> options, string optionsName) : base(context, options, optionsName) { }
    }
}
