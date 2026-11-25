using Bleess.Extensions.Logging.File;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Tests
{
    [TestClass]
    public class FileLoggerProviderTests
    {
        [TestMethod]
        public void Reload_AppliesNewPathForDefaultOptions()
        {
            // regression test: IOptionsMonitor.OnChange reports Options.DefaultName ("") for the default
            // instance, so the provider must not compare it against a null logger name
            using var dir = TestDirectory.Create();
            string firstPath = dir.File("first.txt");
            string secondPath = dir.File("second.txt");

            var monitor = new TestOptionsMonitor<FileLoggerOptions>(new FileLoggerOptions { Path = firstPath });

            using (var provider = new FileLoggerProvider(monitor, Array.Empty<FileFormatter>(), TimeProvider.System))
            {
                var logger = provider.CreateLogger("tests.reload");
                logger.LogInformation("before reload");

                monitor.Set(new FileLoggerOptions { Path = secondPath });

                logger.LogInformation("after reload");
            }

            Assert.IsTrue(File.Exists(firstPath), "The first file should have been created.");
            Assert.IsTrue(File.Exists(secondPath), "The reloaded path should have been used.");

            StringAssert.Contains(File.ReadAllText(firstPath), "before reload");
            StringAssert.Contains(File.ReadAllText(secondPath), "after reload");
        }

        [TestMethod]
        public void Reload_AppliesNewLimitsWithoutRecreatingTheWriter()
        {
            using var dir = TestDirectory.Create();
            string path = dir.File("limits.txt");

            var monitor = new TestOptionsMonitor<FileLoggerOptions>(new FileLoggerOptions { Path = path, MaxNumberFiles = 7 });

            using (var provider = new FileLoggerProvider(monitor, Array.Empty<FileFormatter>(), TimeProvider.System))
            {
                var logger = provider.CreateLogger("tests.limits");
                logger.LogInformation("first");

                monitor.Set(new FileLoggerOptions { Path = path, MaxNumberFiles = 2, MaxFileSizeInMB = 0.001f });

                for (int i = 0; i < 500; i++)
                {
                    logger.LogInformation("message {i} with enough padding to roll the file", i);
                }
            }

            Assert.IsTrue(Directory.GetFiles(dir.Path, "limits*").Length <= 2);
        }

        [TestMethod]
        public void UnknownFormatterName_FailsAtStartup()
        {
            using var dir = TestDirectory.Create();

            var monitor = new TestOptionsMonitor<FileLoggerOptions>(new FileLoggerOptions
            {
                Path = dir.File("bad.txt"),
                FormatterName = "does-not-exist",
            });

            var ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => new FileLoggerProvider(monitor, Array.Empty<FileFormatter>(), TimeProvider.System));

            StringAssert.Contains(ex.Message, "does-not-exist");
        }

        [TestMethod]
        public void UnwritablePath_FailsAtStartup()
        {
            // a directory path that cannot be created as a file
            string invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "log.txt");
            string blocker = Path.GetDirectoryName(invalidPath)!;
            File.WriteAllText(blocker, "not a directory");

            try
            {
                var monitor = new TestOptionsMonitor<FileLoggerOptions>(new FileLoggerOptions { Path = invalidPath });

                Assert.ThrowsExactly<InvalidOperationException>(
                    () => new FileLoggerProvider(monitor, Array.Empty<FileFormatter>(), TimeProvider.System));
            }
            finally
            {
                File.Delete(blocker);
            }
        }

        [TestMethod]
        public void InvalidOptions_FailValidation()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "log.txt");

            var services = new ServiceCollection();
            services.AddLogging(b => b.AddFile(o =>
            {
                o.Path = path;
                o.MaxNumberFiles = 0;
            }));

            using var provider = services.BuildServiceProvider();

            Assert.ThrowsExactly<OptionsValidationException>(() => provider.GetRequiredService<ILoggerFactory>());
        }

        [TestMethod]
        public void JsonFormatter_WritesOneValidJsonObjectPerLine()
        {
            using var dir = TestDirectory.Create();
            string path = dir.File("log.json");

            var services = new ServiceCollection();
            services.AddLogging(b => b.AddJsonFile(o => o.Path = path));

            using (var provider = services.BuildServiceProvider())
            {
                var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("json.test");

                using (logger.BeginScope("scope {value}", 1))
                {
                    logger.LogInformation("hello {value}", 42);
                    logger.LogError(new InvalidOperationException("boom"), "failed {value}", 1);
                }
            }

            var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            Assert.AreEqual(2, lines.Length);

            foreach (string line in lines)
            {
                using var document = JsonDocument.Parse(line);
                Assert.AreEqual("json.test", document.RootElement.GetProperty("Category").GetString());
            }

            using var first = JsonDocument.Parse(lines[0]);
            Assert.AreEqual("Information", first.RootElement.GetProperty("LogLevel").GetString());
            Assert.AreEqual(1, first.RootElement.GetProperty("Scopes").GetArrayLength());
        }

        [TestMethod]
        public void JsonFormatter_WritesMessagesLargerThanTheInitialBuffer()
        {
            // regression test: the JSON formatter used a fixed size buffer and threw for entries > 1 KB
            using var dir = TestDirectory.Create();
            string path = dir.File("large.json");
            string payload = new string('x', 16 * 1024);

            var services = new ServiceCollection();
            services.AddLogging(b => b.AddJsonFile(o => o.Path = path));

            using (var provider = services.BuildServiceProvider())
            {
                var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("json.large");
                logger.LogInformation("payload {payload}", payload);
                logger.LogError(new InvalidOperationException("boom" + new string('y', 4096)), "boom");
            }

            var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            Assert.AreEqual(2, lines.Length);

            using var first = JsonDocument.Parse(lines[0]);
            Assert.AreEqual("payload " + payload, first.RootElement.GetProperty("Message").GetString());

            using var second = JsonDocument.Parse(lines[1]);
            StringAssert.Contains(second.RootElement.GetProperty("Exception").GetString(), "boom");
        }

        [TestMethod]
        public void SimpleFormatter_MatchesTheConsoleFormatterShape()
        {
            // the simple formatter is modeled on the console formatter; pin the exact prefix so
            // changes to padding or the timestamp separator are caught
            using var dir = TestDirectory.Create();
            string path = dir.File("shape.txt");
            var clock = new TestTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));

            var services = new ServiceCollection();
            services.AddSingleton<TimeProvider>(clock);
            services.AddLogging(b => b.AddSimpleFile(
                o => o.Path = path,
                o =>
                {
                    o.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
                    o.UseUtcTimestamp = true;
                    o.InvariantTimestampFormat = true;
                    o.EmptyLineBetweenMessages = false;
                }));

            using (var provider = services.BuildServiceProvider())
            {
                var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("golden.test");
                logger.Log(LogLevel.Information, new EventId(42), "hello", null, (s, e) => s);
            }

            var lines = File.ReadAllLines(path);
            Assert.AreEqual(2, lines.Length);
            Assert.AreEqual("2024-01-15 10:30:00  info: golden.test [42]", lines[0]);
            Assert.AreEqual("      hello", lines[1]);
        }

        [TestMethod]
        public void SimpleFormatter_SingleLineReplacesNewLines()
        {
            using var dir = TestDirectory.Create();
            string path = dir.File("single.txt");

            var services = new ServiceCollection();
            services.AddLogging(b => b.AddSimpleFile(
                o => o.Path = path,
                o =>
                {
                    o.SingleLine = true;
                    o.EmptyLineBetweenMessages = false;
                }));

            using (var provider = services.BuildServiceProvider())
            {
                var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("single");
                logger.LogInformation("line one{newline}line two", Environment.NewLine);
            }

            var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            Assert.AreEqual(1, lines.Length, "Single line mode should not emit embedded new lines.");
        }
    }
}
