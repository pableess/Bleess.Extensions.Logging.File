using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// Options for the built-in json console log formatter.
    /// </summary>
    public record class JsonFileFormatterOptions : FileFormatterOptions
    {
        /// <summary>
        /// Creates <see cref="JsonFileFormatterOptions"/>
        /// </summary>
        public JsonFileFormatterOptions() { }

        /// <summary>
        /// Gets or sets JsonWriterOptions.
        /// </summary>
        public JsonWriterOptions JsonWriterOptions { get; set; }
    }
}
