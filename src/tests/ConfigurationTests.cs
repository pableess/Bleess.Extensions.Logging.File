using Bleess.Extensions.Logging.File;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tests
{
    [TestClass]
    public class ConfigurationTests
    {
#if NET8_0_OR_GREATER

        [TestMethod]
        public void ConfigurationMultipleLoggersTest() 
        {
            IServiceCollection services = new ServiceCollection();
            services.AddLogging(logBuilder => 
            {
                logBuilder.AddFile("A", o => o.Path = @"C:\temp\A.txt");

                logBuilder.AddFile("B", o => o.Path = @"C:\temp\B.txt");

                logBuilder.AddFile("A", o => o.Path = @"C:\temp\A.txt");
            });

            var sp = services.BuildServiceProvider();

            var providers = sp.GetServices<ILoggerProvider>()?.ToArray();

            Assert.IsTrue(providers.Length == 2);
            Assert.IsTrue(providers.All(p => p.GetType() == typeof(FileLoggerProvider)));

            var foo = providers[0] as FileLoggerProvider;
            var bar = providers[1] as FileLoggerProvider;
        }

        [TestMethod]
        public void ConfigurationFileMultipleLoggersTest()
        {
            var app = Host.CreateApplicationBuilder();
            app.Logging.AddFile("Foo")
                .AddFile("Bar");

            var host = app.Build();

            var providers = host.Services.GetServices<ILoggerProvider>()?.ToArray();

            Assert.IsTrue(providers.Length == 2);
            Assert.IsTrue(providers.All(p => p.GetType() == typeof(FileLoggerProvider)));

            var foo = providers[0] as FileLoggerProvider;
            var bar = providers[1] as FileLoggerProvider;
        }

#endif
        [TestMethod]
        public void ConfigurationTest() 
        {
            IServiceCollection services = new ServiceCollection();
            
            services.AddLogging(logBuilder => 
            {
                logBuilder.AddFile();
            });

            var sp = services.BuildServiceProvider();
            var providers = sp.GetServices<ILoggerProvider>();

            

            Assert.IsTrue(providers.Count() == 1);
            Assert.IsTrue(providers.FirstOrDefault().GetType() == typeof(FileLoggerProvider));

        }
    }
}
