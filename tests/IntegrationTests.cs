using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Hosting;

namespace Tests
{
    [TestClass]
    public class IntegrationTests
    {
        [TestMethod]
        public void BasicTest()
        {
            using var dir = TestDirectory.Create();
            string filepath = dir.File("log.txt");

            var services = new ServiceCollection();
            services.AddLogging(lb => lb.AddFile(opt => opt.Path = filepath));

            using (var provider = services.BuildServiceProvider())
            {
                var logger = provider.GetService<ILogger<IntegrationTests>>()!;

                logger.LogInformation("Test message {token}", 10);
                logger.LogError(new Exception(), "Test message exception {token}", 10);

                using (logger.BeginScope("Test Scope 1{value}", 99))
                {
                    using (logger.BeginScope("Test Scope 2 {value}", 100))
                    {
                        logger.LogInformation("Test message with scope");
                    }
                }
            }

            Assert.IsTrue(File.Exists(filepath));

            var text = File.ReadAllText(filepath);
            StringAssert.Contains(text, "Test message 10");
            StringAssert.Contains(text, "Test message exception 10");
            StringAssert.Contains(text, "Test Scope 1");
            StringAssert.Contains(text, "Test Scope 2");
        }

        [TestMethod]
        public void JsonTest()
        {
            using var dir = TestDirectory.Create();
            string filepath = dir.File("log.json");

            var services = new ServiceCollection();
            services.AddLogging(lb => lb.AddFile(opt => opt.Path = filepath).AddJsonFile());

            using (var provider = services.BuildServiceProvider())
            {
                var logger = provider.GetService<ILogger<IntegrationTests>>()!;

                logger.LogInformation("Test message {token}", 10);
            }

            Assert.IsTrue(File.Exists(filepath));
            var lines = File.ReadAllLines(filepath).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            Assert.AreEqual(1, lines.Length);
            using var document = System.Text.Json.JsonDocument.Parse(lines[0]);
            Assert.AreEqual(10, document.RootElement.GetProperty("State").GetProperty("token").GetInt32());
        }

        [TestMethod]
        public void RollTest()
        {
            using var dir = TestDirectory.Create();
            string filename = "roll";
            string filepath = dir.File($"{filename}.txt");

            var services = new ServiceCollection();

            services.AddLogging(lb => lb.AddFile(opt =>
            {
                opt.Path = filepath;
                opt.MaxNumberFiles = 3;
                opt.MaxFileSizeInMB = 0.1f;
            }));

            using (var provider = services.BuildServiceProvider())
            {
                var logger = provider.GetService<ILogger<IntegrationTests>>()!;

                for (int i = 0; i < 10000; i++)
                {
                    logger.LogInformation("Test message {i} with some length over and over again", i);
                }
            }

            Assert.IsTrue(File.Exists(dir.File($"{filename}.txt")));
            Assert.AreEqual(3, Directory.GetFiles(dir.Path, $"{filename}*").Length);

            // file size can be a little bit bigger because size isn't exceed until next write
            Assert.IsTrue(new FileInfo(filepath).Length <= (1024 * 1024 * 0.13f));
        }

        [TestMethod]
        public void HostConfigTest()
        {
            // Default Host builder sets up Config section "Logging"
            using IHost host = Host.CreateDefaultBuilder(null).ConfigureLogging(lb =>
            {
                lb.AddFile();
            })
            .ConfigureAppConfiguration(c => c.AddJsonFile("configTest.json", true))
            .ConfigureServices(sc => { })
            .Build();

            IConfiguration c = host.Services.GetService<IConfiguration>()!;

            string filePath = c.GetValue<string>("Logging:File:Path")!;
            try
            {
                var logger = host.Services.GetService<ILogger<IntegrationTests>>()!;

                logger.LogInformation("Test message");

                host.Dispose();

                Assert.IsTrue(File.Exists(filePath));
                Assert.IsTrue(new FileInfo(filePath).Length > 0);

            }
            finally
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
