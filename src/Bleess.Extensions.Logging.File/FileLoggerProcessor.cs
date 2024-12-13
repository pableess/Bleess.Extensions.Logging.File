using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Bleess.Extensions.Logging.File
{
    internal class FileLoggerProcessor : IDisposable
    {
        private readonly BlockingCollection<LogMessageEntry> _messageQueue;
        private readonly Thread _outputThread;

        private FileWriter _writer;
        Stopwatch _lastFlush;
        long _maxFlushInterval;

        public FileLoggerProcessor(FileLoggerOptions initialOptions, int maxMessageQueuedMessage = 1024)
        {
            _messageQueue = new BlockingCollection<LogMessageEntry>(maxMessageQueuedMessage);

            ConfigureWriter(initialOptions);

            // Start file message queue processor
            _outputThread = new Thread(ProcessLogQueue)
            {
                IsBackground = true,
                Name = "File logger queue processing thread"
            };
            _outputThread.Start();
        }

        public virtual void EnqueueMessage(ref LogMessageEntry message)
        {
            if (!_messageQueue.IsAddingCompleted)
            {
                try
                {
                    _messageQueue.Add(message);
                    return;
                }
                catch (InvalidOperationException) { }
            }

            // Adding is completed so just log the message
            try
            {
                _writer?.WriteMessage(message.Message, true);
            }
            catch (Exception) { }
        }

        // this needs to be called before the 
        public void ConfigureWriter(FileLoggerOptions options) 
        {
            try
            {
                if (options.MaxFlushInterval != null && options.MaxFlushInterval >= 0)
                {
                    _lastFlush = Stopwatch.StartNew();
                    _maxFlushInterval = options.MaxFlushInterval.Value;
                }

                // if the file path didn't change, just update the limits
                if (this._writer != null && this._writer?.FilePath == options.Path && this._writer.RollInterval == options.RollInterval)
                {
                    this._writer.SetLimits(options.MaxFileSizeInBytes, options.MaxNumberFiles, options.FlushToDisk);
                }
                else
                {
                    // else swap out the writer and close the old one 
                    var newWriter = new FileWriter(options.Path, options.MaxFileSizeInBytes, options.MaxNumberFiles, options.Append, options.RollInterval, options.FlushToDisk);

                    FileWriter oldWriter = Interlocked.Exchange(ref this._writer, newWriter) as FileWriter;

                    oldWriter?.Close();
                }
            }
            catch (Exception writerError) 
            {
                System.Diagnostics.Trace.TraceError($"Error creating file writer: {writerError.ToString()}");
            }
        }



        private void ProcessLogQueue()
        {
            try
            {
                foreach (LogMessageEntry message in _messageQueue.GetConsumingEnumerable())
                {
                    bool flush = _messageQueue.Count == 0 || (_lastFlush != null && _lastFlush.ElapsedMilliseconds >= _maxFlushInterval);

                    _writer?.WriteMessage(message.Message, flush);

                    if (flush && _lastFlush != null) 
                    {
                        _lastFlush.Restart();
                    }
                }
            }
            catch
            {
                try
                {
                    _messageQueue.CompleteAdding();
                }
                catch { }
            }
        }

        public void Dispose()
        {
            _lastFlush?.Stop();
            _messageQueue.CompleteAdding();

            try
            {
                // wait for the queue to finish processing
                _outputThread.Join(5000); // shouldn't need a timeout here, but this is more than enough time
            }
            catch (ThreadStateException) { }


            // flush and close writer
            _writer?.Close();

        }
    }
}
