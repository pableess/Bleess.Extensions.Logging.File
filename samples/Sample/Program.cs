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
                .ConfigureLogging(l => l.AddFile())
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

            while (!stoppingToken.IsCancellationRequested) 
            {
                using (this.logger.BeginScope("Iteration {i}", i++))
                {
                    this.logger.LogTrace("This should not be logged");

                    this.logger.LogDebug("This should be logged");

                    await Task.Delay(50);
                }
            }
        }
    }
}
