using System;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// Options for FileLogger.
    /// </summary>
    public class FileLoggerOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileLoggerOptions"/> class.
        /// </summary>
        public FileLoggerOptions()
        {
        }

        /// <summary>
        /// Include scopes in the log output
        /// </summary>
        public bool IncludeScopes { get; set; } = true;


        /// <summary>
        /// Formatter to use use.
        /// Current values are "simple" or "json".  You many also create your own
        /// </summary>
        public string FormatterName { get; set; } = FileFormatterNames.Simple;

        /// <summary>
        /// Gets or sets the base log file name.
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        public string Path { get; set; } = "logs/log.txt";

        internal string ExpandedPath => Environment.ExpandEnvironmentVariables(Path);

        /// <summary>
        /// Gets or sets the Json.
        /// </summary>
        /// <value>
        /// The Json.
        /// </value>
        public bool Json { get; set; } = false;

        /// <summary>
        /// Gets or sets the MaxFileSizeInMB.
        /// </summary>
        /// <value>
        /// The MaxFileSizeInMB.
        /// </value>
        public float MaxFileSizeInMB { get; set; } = 50;

        /// <summary>
        /// Gets or sets the MaxNumberFiles.
        /// </summary>
        /// <value>
        /// The MaxNumberFiles.
        /// </value>
        public int MaxNumberFiles { get; set; } = 7;

        /// <summary>
       /// Roll the log file on maximum size
        /// </summary>
        public bool RollOnFileSizeLimit { get; set; } = true;

        /// <summary>
        /// Whether or not to append to and existing file
        /// False will overwrite an existing log file
        /// </summary>
        public bool Append { get; set; } = true;

        internal long MaxFileSizeInBytes => (long)(MaxFileSizeInMB * 1024 * 1024);
    }
}
