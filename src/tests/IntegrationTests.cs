using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bleess.Extensions.Logging.File;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class IntegrationTests
    {
        [TestMethod]
        public void BasicTest()
        {
            ServiceCollection sc = new ServiceCollection();
            string filepath = "logs/log.txt";
            sc.AddLogging(lb => lb.AddFile(opt => opt.Path = filepath));

            var p = sc.BuildServiceProvider();

            var logger = p.GetService<ILogger<IntegrationTests>>();

            logger.LogInformation("Test message {token}", 10);
            logger.LogError(new Exception(), "Test message exception {token}", 10);

            using (logger.BeginScope("Test Scope 1{value}", 99))
            {
                using (logger.BeginScope("Test Scope 2 {value}", 100))
                {
                    logger.LogInformation("Test message with scope");
                }
            }

            // flush and close the writers
            p.Dispose();

            // todo: better test the file contents
            Assert.IsTrue(File.Exists(filepath));
            var lines = File.ReadAllLines(filepath);
            Assert.IsTrue(lines.Length > 0);
        }

        [TestMethod]
        public void JsonTest()
        {
            ServiceCollection sc = new ServiceCollection();

            string filepath = "logs/log.json";

            sc.AddLogging(lb => lb.AddFile(opt => opt.Path = filepath).AddJsonFile());

            var p = sc.BuildServiceProvider();

            var logger = p.GetService<ILogger<IntegrationTests>>();

            logger.LogInformation("Test message {token}", 10);
            logger.LogError(new Exception(), "Test message exception {token}", 10);

            using (logger.BeginScope("Test Scope 1{value}", 99))
            {
                using (logger.BeginScope("Test Scope 2 {value}", 100))
                {
                    logger.LogInformation("Test message with scope");
                }
            }

            p.Dispose();

            // todo: better test the file contents
            Assert.IsTrue(File.Exists(filepath));
            var lines = File.ReadAllLines(filepath);
            Assert.IsTrue(lines.Length > 0);

            // todo: test for valid json
        }

        [TestMethod]
        public void RollTest()
        {
            string filename = "roll";
            string filepath = $"logs/{filename}.txt";
            try
            {
                ServiceCollection sc = new ServiceCollection();
               
                sc.AddLogging(lb => lb.AddFile(opt =>
                {
                    opt.Path = filepath;
                    opt.MaxNumberFiles = 3;
                    opt.MaxFileSizeInMB = 0.1f;
                }));

                var p = sc.BuildServiceProvider();

                var logger = p.GetService<ILogger<IntegrationTests>>();

                for (int i = 0; i < 10000; i++)
                {
                    logger.LogInformation("Test message {i} with some length over and over again", i);
                }

                p.Dispose();

                Assert.IsTrue(File.Exists($"logs/{filename}.txt"));
                Assert.IsTrue(File.Exists($"logs/{filename}1.txt"));
                Assert.IsTrue(File.Exists($"logs/{filename}2.txt"));

                // file size can be a little bit bigger because size isn't exceed until next write
                Assert.IsTrue(new FileInfo(filepath).Length <= (1024 * 1024 * 0.13f));

            }
            finally 
            {
                try
                {
                    File.Delete($"logs/{filename}.txt");
                    File.Delete($"logs/{filename}1.txt");
                    File.Delete($"logs/{filename}2.txt");
                }
                catch (Exception) { }
            }
           
        }

        [TestMethod]
        public void HostConfigTest()
        {
            // Default Host builder sets up Config section "Logging"
            IHost host = Host.CreateDefaultBuilder(null).ConfigureLogging(lb => 
            {
                lb.AddFile();
            })
            .ConfigureAppConfiguration(c => c.AddJsonFile("configTest.json", true))
            .ConfigureServices(sc => { })
            .Build();

            IConfiguration c = host.Services.GetService<IConfiguration>();

            string filePath = c.GetValue<string>("Logging:File:Path");
            try
            {
           
                var logger = host.Services.GetService<ILogger<IntegrationTests>>();

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
                catch (Exception) { }
            }

        }

    }
}
