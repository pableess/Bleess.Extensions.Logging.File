namespace Bleess.Extensions.Logging.File
{
    using Microsoft.Extensions.Logging.Configuration;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// FileLogger Options setup class.
    /// </summary>
    internal class FileLoggerOptionsSetup : ConfigureFromConfigurationOptions<FileLoggerOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileLoggerOptionsSetup"/> class.
        /// </summary>
        /// <param name="providerConfiguration">The providerConfiguration.</param>
        public FileLoggerOptionsSetup(ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration)
            : base(providerConfiguration.Configuration)
        {
        }
    }
}
