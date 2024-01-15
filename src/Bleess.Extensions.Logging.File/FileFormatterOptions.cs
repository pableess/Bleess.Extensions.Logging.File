using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// Base class for filter formatter options
    /// </summary>
    public record class FileFormatterOptions
    {
        /// <summary>
        /// Creates FileFormatter options
        /// </summary>
        public FileFormatterOptions() { }

        /// <summary>
        /// Includes scopes when <see langword="true" />.
        /// </summary>
        public bool? IncludeScopes { get; set; }

        /// <summary>
        /// Gets or sets format string used to format timestamp in logging messages. Defaults to <c>[yyyy-MM-dd h:mm tt"]</c>.
        /// </summary>
        public string TimestampFormat { get; set; } = "yyyy-MM-dd h:mm tt";

        /// <summary>
        /// Gets or sets indication whether or not UTC timezone should be used to for timestamps in logging messages. Defaults to <c>false</c>.
        /// </summary>
        public bool UseUtcTimestamp { get; set; }

        /// <summary>
        /// Create an empty line between log message
        /// </summary>
        public bool EmptyLineBetweenMessages { get; set; } = true;
    }
}
