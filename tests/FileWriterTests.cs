using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bleess.Extensions.Logging.File;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class FileWriterTests
    {
        [TestMethod]
        public void ChangeLimits()
        {
            try
            {
                FileWriter w = new FileWriter("logs/fw.txt", 1024, 4, false);

                for (int i = 0; i < 5000; i++)
                {
                    w.WriteMessage($"Test message {i}", true);
                }

                Assert.IsTrue(Directory.GetFiles("logs", "fw*").Length == 4);

                // try this on the background thread to test the threading
                Task.Run(() =>
                {
                    w.SetLimits(1024, 2);
                });

                for (int i = 0; i < 50000; i++)
                {
                    w.WriteMessage($"Test message {i}", true);
                }

                w.Close();

                Assert.IsTrue(Directory.GetFiles("logs", "fw*").Length == 2);
            }
            finally
            {
                try
                {
                    Directory.GetFiles("logs", "fw*").ToList().ForEach(f => File.Delete(f));
                }
                catch (Exception) { }
            }
        }
    }
}
