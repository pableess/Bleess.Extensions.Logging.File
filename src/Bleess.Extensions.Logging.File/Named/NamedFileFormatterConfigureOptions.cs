using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#if NET8_0_OR_GREATER

namespace Bleess.Extensions.Logging.File.Named;

internal class NamedFileFormatterConfigureOptions : IConfigureOptions<FileFormatterOptions>, IConfigureNamedOptions<FileFormatterOptions>
{
    private readonly IConfiguration _configuration;

    public NamedFileFormatterConfigureOptions(ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration)
    {
        _configuration = providerConfiguration.GetFormatterOptionsSection();
    }

    public void Configure(FileFormatterOptions options) => _configuration.Bind(options);

    public void Configure(string name, FileFormatterOptions options)
    {
        
    }
}

#endif