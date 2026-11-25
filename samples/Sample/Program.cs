using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(s => s.AddHostedService<LoggingService>())
                .ConfigureLogging(l =>
                {

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

                        // example using a custom formatter. the name must match the name the
                        // formatter passes to the FileFormatter constructor
                        b.AddFile("CustomLog", o => o.Path = "logs/custom.txt")
                            .WithFormatter<MyCustomFormatter, MyOptions>(MyCustomFormatter.FormatterName, o => o.Option1 = true)
                            .WithMinLevel(LogLevel.Warning);
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

    internal sealed class LoggingService : BackgroundService
    {
        private readonly ILogger _logger;

        public LoggingService(ILogger<LoggingService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int i = 0;
            var r = new Random();


            await Task.Delay(5000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                i++;

                using var a = new Activity("Sample");
                a.Start();

                a.SetTag("bar", i.ToString(CultureInfo.InvariantCulture));

                using (this._logger.BeginScope("Iteration {i}", i++))
                {
                    this._logger.LogTrace("This should not be logged");

                    this._logger.LogDebug("This should be logged");

                    if (r.Next(100) > 75)
                    {
                        this._logger.LogError("This is an error");
                    }
                }

                a.Stop();
            }
        }
    }
}
