using Bleess.Extensions.Logging.File;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class CompositeLoggerTests
    {
        [TestMethod]
        public void IsEnabled_IsTrueWhenAnySubLoggerIsEnabled()
        {
            using var dir = TestDirectory.Create();
            using var processor = new FileLoggerProcessor(new FileLoggerOptions { Path = dir.File("composite.txt") }, TimeProvider.System);

            var traceLogger = new FileLogger("category", processor);
            var errorLogger = new FileLogger("category", processor);

            var composite = new CompositeFileLogger("category", new[]
            {
                new SubFileLoggerInfo(traceLogger, "trace", LogLevel.Trace, null),
                new SubFileLoggerInfo(errorLogger, "error", LogLevel.Error, null),
            }, null);

            // regression test: this must not require *all* sub loggers to be enabled
            Assert.IsTrue(composite.IsEnabled(LogLevel.Information));
            Assert.IsTrue(composite.IsEnabled(LogLevel.Trace));
        }

        [TestMethod]
        public void IsEnabled_IsFalseWhenNoSubLoggerIsEnabled()
        {
            using var dir = TestDirectory.Create();
            using var processor = new FileLoggerProcessor(new FileLoggerOptions { Path = dir.File("composite.txt") }, TimeProvider.System);

            var composite = new CompositeFileLogger("category", new[]
            {
                new SubFileLoggerInfo(new FileLogger("category", processor), "one", LogLevel.Error, null),
                new SubFileLoggerInfo(new FileLogger("category", processor), "two", LogLevel.Error, null),
            }, null);

            Assert.IsFalse(composite.IsEnabled(LogLevel.Debug));
        }

        [TestMethod]
        public void Update_OnlyReplacesTheTargetedSubLogger()
        {
            using var dir = TestDirectory.Create();
            using var processor = new FileLoggerProcessor(new FileLoggerOptions { Path = dir.File("composite.txt") }, TimeProvider.System);

            var composite = new CompositeFileLogger("category", new[]
            {
                new SubFileLoggerInfo(new FileLogger("category", processor), "a", LogLevel.Trace, null),
                new SubFileLoggerInfo(new FileLogger("category", processor), "b", LogLevel.Error, null),
                new SubFileLoggerInfo(new FileLogger("category", processor), "c", LogLevel.Warning, null),
            }, null);

            composite.Update("b", LogLevel.Debug, null);

            var subLoggers = composite.SubLoggers.ToArray();
            Assert.AreEqual(3, subLoggers.Length);
            Assert.AreEqual(LogLevel.Debug, subLoggers.Single(s => s.SubProviderName == "b").MinLevel);
            Assert.AreEqual(LogLevel.Trace, subLoggers.Single(s => s.SubProviderName == "a").MinLevel);
            Assert.AreEqual(LogLevel.Warning, subLoggers.Single(s => s.SubProviderName == "c").MinLevel);
        }

        [TestMethod]
        public void WithFilter_AddsTheDelegateToTheRule()
        {
            // regression test: WithFilter(Func<...>) used to drop the predicate
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddFiles(m => m.AddFile("filtered")
                .WithFilter((category, level) => level >= LogLevel.Warning)));

            using var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptionsMonitor<CompositeLoggerFilterOptions>>().Get("filtered");
            var rule = options.Rules.Single(r => r.Filter != null);

            Assert.IsTrue(rule.Filter!(null, null, LogLevel.Error));
            Assert.IsFalse(rule.Filter!(null, null, LogLevel.Information));
        }

        [TestMethod]
        public void WithFilter_DelegateFiltersLogMessages()
        {
            using var dir = TestDirectory.Create();
            string path = dir.File("filtered.txt");

            var services = new ServiceCollection();
            services.AddLogging(b => b.AddFiles(m => m.AddFile("filtered", o => o.Path = path)
                .WithFilter((category, level) => level >= LogLevel.Warning)));

            using (var provider = services.BuildServiceProvider())
            {
                var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("filtered.test");
                logger.LogInformation("dropped");
                logger.LogWarning("kept");
            }

            string text = File.ReadAllText(path);
            StringAssert.Contains(text, "kept");
            Assert.IsFalse(text.Contains("dropped"), "Information level messages should be filtered out.");
        }

        [TestMethod]
        public void WithFormatter_UsesTheGivenFormatterNameAndOptions()
        {
            // regression test: WithFormatter used to select the provider alias as the formatter name
            using var dir = TestDirectory.Create();
            string path = dir.File("custom.log");

            var services = new ServiceCollection();
            services.AddLogging(b => b.AddFiles(m => m.AddFile("custom", o => o.Path = path)
                .WithFormatter<TestFormatter, TestFormatterOptions>(TestFormatter.FormatterName, o => o.Prefix = "CUSTOM")));

            using (var provider = services.BuildServiceProvider())
            {
                var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("custom.test");
                logger.LogInformation("hello");
            }

            string text = File.ReadAllText(path);
            StringAssert.Contains(text, "CUSTOM:hello");
        }

        [TestMethod]
        public void WithMinLevel_AppliesPerSubProvider()
        {
            using var dir = TestDirectory.Create();
            string allPath = dir.File("all.txt");
            string errorPath = dir.File("errors.txt");

            var services = new ServiceCollection();
            services.AddLogging(b => b.AddFiles(m =>
            {
                m.AddFile("all", o => o.Path = allPath);
                m.AddFile("errors", o => o.Path = errorPath).WithMinLevel(LogLevel.Error);
            }));

            using (var provider = services.BuildServiceProvider())
            {
                var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("levels.test");
                logger.LogInformation("info message");
                logger.LogError("error message");
            }

            StringAssert.Contains(File.ReadAllText(allPath), "info message");
            StringAssert.Contains(File.ReadAllText(allPath), "error message");

            StringAssert.Contains(File.ReadAllText(errorPath), "error message");
            Assert.IsFalse(File.ReadAllText(errorPath).Contains("info message"));
        }
    }

    internal sealed record class TestFormatterOptions : FileFormatterOptions
    {
        public string Prefix { get; set; } = "TEST";
    }

    internal sealed class TestFormatter : FileFormatter<TestFormatterOptions>
    {
        public const string FormatterName = "testfmt";

        public TestFormatter(IOptionsMonitor<TestFormatterOptions> options, TimeProvider timeProvider)
            : base(FormatterName, options, timeProvider)
        {
        }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter, string? subProviderName = null, bool? fallbackIncludeScopes = null)
        {
            var options = this.GetOptions(subProviderName);

            textWriter.Write(options.Prefix);
            textWriter.Write(':');
            textWriter.Write(logEntry.Formatter(logEntry.State, logEntry.Exception));
        }
    }
}
