using Bleess.Extensions.Logging.File;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class FileWriterTests
    {
        [TestMethod]
        public void ChangeLimits()
        {
            using var dir = TestDirectory.Create();
            using FileWriter w = new FileWriter(dir.File("fw.txt"), 1024, 4, false, RollingInterval.Infinite, false, false, TimeProvider.System);

            for (int i = 0; i < 5000; i++)
            {
                w.WriteMessage($"Test message {i}", true);
            }

            Assert.AreEqual(4, Directory.GetFiles(dir.Path, "fw*").Length);

            // try this on the background thread to test the threading
            Task.Run(() => w.SetLimits(1024, 2, false)).Wait();

            for (int i = 0; i < 50000; i++)
            {
                w.WriteMessage($"Test message {i}", true);
            }

            w.Close();

            Assert.AreEqual(2, Directory.GetFiles(dir.Path, "fw*").Length);
        }

        [TestMethod]
        public void RollingBySize_NeverDeletesTheCurrentFile()
        {
            using var dir = TestDirectory.Create();
            using FileWriter w = new FileWriter(dir.File("single.txt"), 512, 1, false, RollingInterval.Infinite, false, false, TimeProvider.System);

            for (int i = 0; i < 2000; i++)
            {
                w.WriteMessage($"Test message {i} with some padding to fill the file", true);
            }

            w.Close();

            var files = Directory.GetFiles(dir.Path, "single*");
            Assert.AreEqual(1, files.Length);
            Assert.IsTrue(new FileInfo(files[0]).Length > 0);
        }

        [TestMethod]
        public void ZeroSizeLimit_DoesNotRoll()
        {
            using var dir = TestDirectory.Create();
            using FileWriter w = new FileWriter(dir.File("nolimit.txt"), 0, 2, false, RollingInterval.Infinite, false, false, TimeProvider.System);

            for (int i = 0; i < 2000; i++)
            {
                w.WriteMessage($"Test message {i}", true);
            }

            w.Close();

            var files = Directory.GetFiles(dir.Path, "nolimit*");
            Assert.AreEqual(1, files.Length);
        }

        [TestMethod]
        public void Roll_KeepsOnlyConfiguredNumberOfFiles()
        {
            using var dir = TestDirectory.Create();
            using FileWriter w = new FileWriter(dir.File("keep.txt"), 256, 3, false, RollingInterval.Infinite, false, false, TimeProvider.System);

            for (int i = 0; i < 3000; i++)
            {
                w.WriteMessage($"Test message {i} padded so the limit is reached", true);
            }

            w.Close();

            var files = Directory.GetFiles(dir.Path, "keep*");
            Assert.IsTrue(files.Length <= 3, $"Expected at most 3 files but found {files.Length}.");
            Assert.IsTrue(File.Exists(dir.File("keep.txt")));
        }

        [TestMethod]
        public void WritesUtf8ThroughSpanOverload()
        {
            using var dir = TestDirectory.Create();
            using FileWriter w = new FileWriter(dir.File("unicode.txt"), 0, 1, false, RollingInterval.Infinite, false, false, TimeProvider.System);

            w.WriteMessage("héllo wörld", true);
            w.Close();

            StringAssert.Contains(File.ReadAllText(dir.File("unicode.txt")), "héllo wörld");
        }

        [TestMethod]
        public void SizeRolling_CreatesMultipleFiles()
        {
            // relies on the write counting stream tracking the span based writes made by StreamWriter
            using var dir = TestDirectory.Create();
            using FileWriter w = new FileWriter(dir.File("multi.txt"), 256, 10, false, RollingInterval.Infinite, false, false, TimeProvider.System);

            for (int i = 0; i < 3000; i++)
            {
                w.WriteMessage($"Test message {i} padded so the limit is reached", true);
            }

            w.Close();

            var files = Directory.GetFiles(dir.Path, "multi*");
            Assert.AreEqual(10, files.Length, "Size based rolling should have created the maximum number of files.");
        }
    }
}
