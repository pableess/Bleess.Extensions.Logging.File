using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// Queues formatted messages and writes them from a dedicated thread.
    /// The write thread owns the <see cref="FileWriter"/>; options changes are handed to it as a queue
    /// entry so that the writer is never swapped or closed from another thread.
    /// </summary>
    internal sealed class FileLoggerProcessor : IDisposable
    {
        private readonly BlockingCollection<LogMessageEntry> _messageQueue;
        private readonly Thread _outputThread;
        private readonly TimeProvider _timeProvider;

        // owned by the write thread once the thread has started
        private FileWriter? _writer;
        private Stopwatch? _lastFlush;
        private long _maxFlushInterval;

        private readonly object _pendingOptionsLock = new();
        private bool _disposed;
        private FileLoggerOptions? _pendingOptions;
        private volatile bool _hasPendingOptions;

        public FileLoggerProcessor(FileLoggerOptions initialOptions, TimeProvider timeProvider, int maxQueuedMessages = 1024)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _messageQueue = new BlockingCollection<LogMessageEntry>(maxQueuedMessages);

            // fail fast: a provider that cannot open its file should not start silently dropping messages
            ApplyOptions(initialOptions, throwOnError: true);

            _outputThread = new Thread(ProcessLogQueue)
            {
                IsBackground = true,
                Name = "File logger queue processing thread"
            };
            _outputThread.Start();
        }

        public void EnqueueMessage(LogMessageEntry message)
        {
            try
            {
                _messageQueue.Add(message);
            }
            catch (InvalidOperationException)
            {
                // the queue is complete (provider is shutting down); logging after dispose is a no-op.
                // ObjectDisposedException derives from InvalidOperationException and is covered too.
            }
        }

        /// <summary>
        /// Applies new provider options.  Safe to call from any thread: the options are applied by the
        /// write thread when it reaches the marker in the queue.
        /// </summary>
        public void ConfigureWriter(FileLoggerOptions options)
        {
            lock (_pendingOptionsLock)
            {
                _pendingOptions = options;
                _hasPendingOptions = true;
            }

            try
            {
                // wake the write thread. If the queue is saturated the pending options are picked up
                // in front of the next message instead.
                if (!_messageQueue.TryAdd(LogMessageEntry.ConfigurationChanged, 100))
                {
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                // provider is shutting down
            }
        }

        private void ProcessLogQueue()
        {
            try
            {
                foreach (LogMessageEntry message in _messageQueue.GetConsumingEnumerable())
                {
                    if (message.IsConfigurationChanged)
                    {
                        ApplyPendingOptions();
                        continue;
                    }

                    if (_hasPendingOptions)
                    {
                        ApplyPendingOptions();
                    }

                    bool flush = _messageQueue.Count == 0
                        || (_lastFlush is not null && _lastFlush.ElapsedMilliseconds >= _maxFlushInterval);

                    _writer?.WriteMessage(message.Message!, flush);

                    if (flush)
                    {
                        _lastFlush?.Restart();
                    }
                }
            }
            catch (Exception ex)
            {
                // nothing sane to do here: completing the queue stops the producer side from blocking
                Trace.TraceError($"File logger write thread failed: {ex}");
                try
                {
                    _messageQueue.CompleteAdding();
                }
                catch (ObjectDisposedException)
                {
                }
            }
            finally
            {
                // the write thread owns the writer, so it closes it
                _writer?.Close();
                _writer = null;
            }
        }

        private void ApplyPendingOptions()
        {
            FileLoggerOptions? options;
            lock (_pendingOptionsLock)
            {
                options = _pendingOptions;
                _pendingOptions = null;
                _hasPendingOptions = false;
            }

            if (options is not null)
            {
                ApplyOptions(options, throwOnError: false);
            }
        }

        private void ApplyOptions(FileLoggerOptions options, bool throwOnError)
        {
            try
            {
                if (options.MaxFlushInterval is >= 0 and int maxFlushInterval)
                {
                    _lastFlush = Stopwatch.StartNew();
                    _maxFlushInterval = maxFlushInterval;
                }
                else
                {
                    _lastFlush = null;
                }

                // if the file path didn't change, just update the limits
                if (_writer is not null && _writer.FilePath == options.Path && _writer.RollInterval == options.RollInterval)
                {
                    _writer.SetLimits(options.MaxFileSizeInBytes, options.MaxNumberFiles, options.FlushToDisk);
                }
                else
                {
                    // create (and validate) the new writer before closing the old one so that a failed
                    // reload keeps writing to the previous file
                    var newWriter = new FileWriter(
                        options.Path,
                        options.MaxFileSizeInBytes,
                        options.MaxNumberFiles,
                        options.Append,
                        options.RollInterval,
                        options.FlushToDisk,
                        options.UseUtcTimestamp,
                        _timeProvider);

                    FileWriter? oldWriter = _writer;
                    _writer = newWriter;
                    oldWriter?.Close();
                }
            }
            catch (Exception writerError)
            {
                if (throwOnError)
                {
                    throw new InvalidOperationException($"Unable to open the log file '{options.Path}'.", writerError);
                }

                // keep writing to the previous file
                Trace.TraceError($"Error applying file logger options for '{options.Path}': {writerError}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _messageQueue.CompleteAdding();

            try
            {
                // the write thread drains the queue and closes the writer
                if (_outputThread.Join(5000))
                {
                    _messageQueue.Dispose();
                }
            }
            catch (ThreadStateException)
            {
            }

            // do not close the writer here: if the join timed out the write thread still owns it
        }
    }
}
