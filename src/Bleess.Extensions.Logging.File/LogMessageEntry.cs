using System;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// An entry in the write queue.  Either a formatted log message or a marker that tells the
    /// write thread to apply new provider options.
    /// </summary>
    internal readonly struct LogMessageEntry
    {
        private LogMessageEntry(string? message, bool isConfigurationChanged)
        {
            Message = message;
            IsConfigurationChanged = isConfigurationChanged;
        }

        public LogMessageEntry(string message)
            : this(message, false)
        {
        }

        /// <summary>
        /// Marker enqueued when the provider options changed.  It is processed by the write thread so that
        /// the writer is never swapped or closed from another thread.
        /// </summary>
        public static LogMessageEntry ConfigurationChanged { get; } = new LogMessageEntry(null, true);

        public string? Message { get; }

        public bool IsConfigurationChanged { get; }
    }
}
