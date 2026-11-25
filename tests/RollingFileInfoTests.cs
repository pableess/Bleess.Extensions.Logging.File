using Bleess.Extensions.Logging.File;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class RollingFileInfoTests
    {
        private static readonly DateTimeOffset Now = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);

        private static TestTimeProvider Clock() => new TestTimeProvider(Now);

        [TestMethod]
        public void TestFileName()
        {
            RollingFileInfo info = new RollingFileInfo("logs/log.txt", RollingInterval.Day, true, Clock());

            Assert.AreEqual("logs/log_20240115.txt", info.CurrentFile);
        }

        [TestMethod]
        public void TestFileNameSeq()
        {
            RollingFileInfo info = new RollingFileInfo("logs/log.txt", RollingInterval.Day, true, Clock());

            info.Roll(true);
            info.Roll(true);

            Assert.AreEqual("logs/log_20240115_002.txt", info.CurrentFile);
        }

        [TestMethod]
        public void TestFileNameNoDate()
        {
            RollingFileInfo info = new RollingFileInfo("logs/log.txt", RollingInterval.Infinite, true, Clock());

            Assert.AreEqual("logs/log.txt", info.CurrentFile);
        }

        [TestMethod]
        public void TestFileNameNoDateSeq()
        {
            RollingFileInfo info = new RollingFileInfo("logs/log.txt", RollingInterval.Infinite, true, Clock());

            info.Roll(true);
            Assert.AreEqual("logs/log_001.txt", info.CurrentFile);
        }

        [TestMethod]
        public void TestFileNameWithoutExtensionDoesNotGetTrailingDot()
        {
            RollingFileInfo info = new RollingFileInfo("logs/log", RollingInterval.Day, true, Clock());

            Assert.AreEqual("logs/log_20240115", info.CurrentFile);
        }

        [TestMethod]
        public void TestMinuteInterval()
        {
            RollingFileInfo info = new RollingFileInfo("logs/log.txt", RollingInterval.Minute, true, Clock());

            Assert.AreEqual("logs/log_202401151030.txt", info.CurrentFile);
        }

        [TestMethod]
        public void TestShouldDateRoll()
        {
            var clock = Clock();
            RollingFileInfo info = new RollingFileInfo("logs/log.txt", RollingInterval.Day, true, clock);

            Assert.IsFalse(info.ShouldDateRoll());

            clock.Advance(TimeSpan.FromDays(1));

            Assert.IsTrue(info.ShouldDateRoll());
        }

        [TestMethod]
        public void TestShouldDateRollIsFalseForInfinite()
        {
            var clock = Clock();
            RollingFileInfo info = new RollingFileInfo("logs/log.txt", RollingInterval.Infinite, true, clock);

            clock.Advance(TimeSpan.FromDays(400));

            Assert.IsFalse(info.ShouldDateRoll());
        }

        [TestMethod]
        public void TestAlign()
        {
            using var dir = TestDirectory.Create();

            // alignment uses the last write timestamp, so set them explicitly
            WriteFile(dir.File("foo_2024011410.json"), Now.AddDays(-1));
            WriteFile(dir.File("foo_2024011510.json"), Now.AddMinutes(-3));
            WriteFile(dir.File("foo_2024011510_001.json"), Now.AddMinutes(-2));
            WriteFile(dir.File("foo_2024011510_002.json"), Now.AddMinutes(-1));

            RollingFileInfo info = new RollingFileInfo(dir.File("foo.json"), RollingInterval.Hour, true, Clock());

            var result = info.AlignToDirectory();

            Assert.IsTrue(result);
            Assert.AreEqual(2, info.FileSequence);
            Assert.AreEqual(dir.File("foo_2024011510_002.json"), info.CurrentFile);
        }

        [TestMethod]
        public void TestAlignNoDate()
        {
            using var dir = TestDirectory.Create();

            WriteFile(dir.File("foo.json"), Now.AddMinutes(-3));
            WriteFile(dir.File("foo_002.json"), Now.AddMinutes(-2));
            WriteFile(dir.File("foo_003.json"), Now.AddMinutes(-1));

            RollingFileInfo info = new RollingFileInfo(dir.File("foo.json"), RollingInterval.Infinite, true, Clock());

            var result = info.AlignToDirectory();

            Assert.IsTrue(result);
            Assert.AreEqual(3, info.FileSequence);
            Assert.AreEqual(dir.File("foo_003.json"), info.CurrentFile);
        }

        [TestMethod]
        public void TestAlignDateFileWithoutSequenceIsNotTreatedAsASequence()
        {
            using var dir = TestDirectory.Create();

            WriteFile(dir.File("foo_2024011510.json"), Now.AddMinutes(-1));

            RollingFileInfo info = new RollingFileInfo(dir.File("foo.json"), RollingInterval.Hour, true, Clock());

            Assert.IsTrue(info.AlignToDirectory());
            Assert.AreEqual(0, info.FileSequence);
            Assert.AreEqual(dir.File("foo_2024011510.json"), info.CurrentFile);
        }

        [TestMethod]
        public void TestAlignSupportsSequencesWiderThanThreeDigits()
        {
            using var dir = TestDirectory.Create();

            WriteFile(dir.File("foo_1234.json"), Now.AddMinutes(-1));

            RollingFileInfo info = new RollingFileInfo(dir.File("foo.json"), RollingInterval.Infinite, true, Clock());

            Assert.IsTrue(info.AlignToDirectory());
            Assert.AreEqual(1234, info.FileSequence);
            Assert.AreEqual(dir.File("foo_1234.json"), info.CurrentFile);
        }

        [TestMethod]
        public void TestAlignIgnoresFilesFromAnotherInterval()
        {
            using var dir = TestDirectory.Create();

            WriteFile(dir.File("foo_2024011410.json"), Now.AddMinutes(-1));

            RollingFileInfo info = new RollingFileInfo(dir.File("foo.json"), RollingInterval.Hour, true, Clock());

            Assert.IsFalse(info.AlignToDirectory());
            Assert.AreEqual(0, info.FileSequence);
        }

        [TestMethod]
        public void TestMatching()
        {
            using var dir = TestDirectory.Create();

            WriteFile(dir.File("sample_20240115.txt"), Now.AddDays(-1));
            WriteFile(dir.File("sample_20240116.txt"), Now.AddMinutes(-2));
            WriteFile(dir.File("sample_20240116_001.txt"), Now.AddMinutes(-1));

            RollingFileInfo info = new RollingFileInfo(dir.File("sample.txt"), RollingInterval.Infinite, true, Clock());

            var result = info.GetMatchingFilesByOldest();
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual("sample_20240115.txt", result[0].Name);
            Assert.AreEqual("sample_20240116.txt", result[1].Name);
            Assert.AreEqual("sample_20240116_001.txt", result[2].Name);
        }

        private static void WriteFile(string path, DateTimeOffset lastWrite)
        {
            File.WriteAllText(path, "test");
            File.SetLastWriteTimeUtc(path, lastWrite.UtcDateTime);
        }
    }
}
