using Bleess.Extensions.Logging.File;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class RollingFileInfoTests
    {
        [TestMethod]
        public void TestFileName() 
        {
            RollingFileInfo info = new RollingFileInfo("logs/log.txt", RollingInterval.Day, true);
           
            Assert.AreEqual($"logs/log_{DateTime.UtcNow.ToString("yyyyMMdd")}.txt", info.CurrentFile);
        }

        [TestMethod]
        public void TestFileNameSeq()
        {
            RollingFileInfo info = new RollingFileInfo("logs/log.txt", RollingInterval.Day, true);

            info.Roll(true);
            info.Roll(true);

            Assert.AreEqual($"logs/log_{DateTime.UtcNow.ToString("yyyyMMdd")}_002.txt", info.CurrentFile);
        }

        [TestMethod]
        public void TestFileNameNoDate()
        {
            RollingFileInfo info = new RollingFileInfo("logs/log.txt", RollingInterval.Infinite, true);

            Assert.AreEqual($"logs/log.txt", info.CurrentFile);
        }

        [TestMethod]
        public void TestFileNameNoDateSeq()
        {
            RollingFileInfo info = new RollingFileInfo("logs/log.txt", RollingInterval.Infinite, true);

            info.Roll(true);
            Assert.AreEqual($"logs/log_001.txt", info.CurrentFile);
        }

        [TestMethod]
        [DoNotParallelize]
        public void TestAlign()
        {
            var random = Path.ChangeExtension(Path.GetRandomFileName(), null);

            Directory.CreateDirectory($"logs/{random}");

            // alg uses last write timestamp, so write them in order
            File.AppendText($"logs/{random}/foo_{DateTime.Now.Subtract(TimeSpan.FromDays(1)).ToString("yyyyMMddHH")}.json").Close();
            Thread.Sleep(50);

            File.AppendText($"logs/{random}/foo_{DateTime.Now.ToString("yyyyMMddHH")}_001.json")?.Close();
            Thread.Sleep(50);

            File.AppendText($"logs/{random}/foo_{DateTime.Now.ToString("yyyyMMddHH")}_002.json")?.Close();

            try
            {
                RollingFileInfo info = new RollingFileInfo($"logs/{random}/foo.json", RollingInterval.Hour, false);

                var result = info.AlignToDirectory();

                Assert.IsTrue(result);
                Assert.AreEqual(2, info.FileSequence);
                Assert.AreEqual($"logs/{random}/foo_{DateTime.Now.ToString("yyyyMMddHH")}_002.json", info.CurrentFile);

            }
            finally 
            {
                Directory.Delete($"logs/{random}", true);
            }
        }

        [TestMethod]
        [DoNotParallelize]
        public void TestAlignNoDate()
        {
            var random = Path.ChangeExtension(Path.GetRandomFileName(), null);

            Directory.CreateDirectory($"logs/{random}");

            // alg uses last write timestamp, so write them in order
            File.AppendText($"logs/{random}/foo.json")?.Close();
            Thread.Sleep(50);
            File.AppendText($"logs/{random}/foo_002.json")?.Close();
            Thread.Sleep(50);
            File.AppendText($"logs/{random}/foo_003.json")?.Close();

           

            try
            {
                RollingFileInfo info = new RollingFileInfo($"logs/{random}/foo.json", RollingInterval.Infinite, false);

                var result = info.AlignToDirectory();

                Assert.IsTrue(result);
                Assert.AreEqual(3, info.FileSequence);
                Assert.AreEqual($"logs/{random}/foo_003.json", info.CurrentFile);

            }
            finally
            {
                Directory.Delete($"logs/{random}", true);
            }
        }

        [TestMethod]
        public void TestMatching()
        {
            RollingFileInfo info = new RollingFileInfo("data/sample.txt", RollingInterval.Infinite, true);

            var result = info.GetMatchingFilesByOldest().ToArray();
            Assert.IsTrue(result.Length == 3);
            Assert.IsTrue(result[0].Name == "sample_20240115.txt");
            Assert.IsTrue(result[1].Name == "sample_20240116.txt");
            Assert.IsTrue(result[2].Name == "sample_20240116_001.txt");


        }
    }
}
