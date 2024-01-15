using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(s => s.AddHostedService<LoggingService>())
                .ConfigureLogging(l =>
                {
                    l.AddConsole(c => { c.IncludeScopes = true; });
                    l.AddFile(); // default log file

                    l.AddFiles(b =>
                    {
                        // example adding log provider, will use settings in configuration
                        b.AddFile("TraceLog");

                        // example configuration certain properties in code, which would override config settings
                        b.AddFile("ErrorLog")
                            .WithOptions(o =>
                            {
                                o.Path = "logs/errors.json";
                            })
                            .WithJsonFormatter(o =>
                            {
                                o.IncludeScopes = true;
                                o.EmptyLineBetweenMessages = false;
                                o.TimestampFormat = "dd h:mm tt";
                            })
                            .WithMinLevel(LogLevel.Error);
                    });


                    l.Configure(f => 
                    {
                        f.ActivityTrackingOptions = ActivityTrackingOptions.SpanId
                                      | ActivityTrackingOptions.TraceId
                                      | ActivityTrackingOptions.ParentId
                                      | ActivityTrackingOptions.Baggage
                                      | ActivityTrackingOptions.Tags;
                    });

                })
                .Build();

            host.Run();
        }
    }

    class LoggingService : BackgroundService 
    {  

        private readonly ILogger logger;
        public LoggingService(ILogger<LoggingService> logger)
        {
            this.logger = logger;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int i = 0;
            var r = new Random();

            while (!stoppingToken.IsCancellationRequested) 
            {
                i++;

                using var a = new Activity("Sample");
                a.Start();

                a.SetTag("bar", i.ToString());

                using (this.logger.BeginScope("Iteration {i}", i++))
                {
                    this.logger.LogTrace("This should not be logged");

                    this.logger.LogDebug("This should be logged");

                    if (r.Next(100) > 75)
                    {
                        this.logger.LogError("This is an error");
                    }

                    await Task.Delay(1000);
                }

                a.Stop();
            }
        }
    }
}
