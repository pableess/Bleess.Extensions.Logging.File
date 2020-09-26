using System;
using System.Collections.Generic;
using System.Text;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// Options for the built-in default console log formatter.
    /// </summary>
    public class SimpleFileFormatterOptions : FileFormatterOptions
    {
        public SimpleFileFormatterOptions() { }

        /// <summary>
        /// When <see langword="false" />, the entire message gets logged in a single line.
        /// </summary>
        public bool SingleLine { get; set; } = false;

    }
}
