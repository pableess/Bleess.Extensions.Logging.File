using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
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
                    l.AddConsole();
                    //l.AddFiles();
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
            }
        }
    }
}
