using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Bleess.Extensions.Logging.File
{
    internal class CompositeFileLoggerProviderConfigureOptions : IConfigureOptions<CompositeFileLoggerProviderOptions>, 
        IConfigureNamedOptions<FileLoggerOptions>,
        IConfigureNamedOptions<LoggerFilterOptions>,
        IConfigureNamedOptions<CompositeLoggerFilterOptions>
    {
        private readonly IConfiguration _configuration;

        public CompositeFileLoggerProviderConfigureOptions(ILoggerProviderConfiguration<CompositeFileLoggerProvider> providerConfiguration)
        {
            _configuration = providerConfiguration.Configuration;
        }

        public void Configure(CompositeFileLoggerProviderOptions options) 
        {
            options.Providers = _configuration.GetChildren()?
                .Select(section => section.Key)
                .Where(k => !k.ToLower()
                .Equals("loglevel", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        public void Configure(string name, FileLoggerOptions options)
        {
            // look for a file provider 
            var section = _configuration.GetSection(name);

            if (section != null) 
            {
                section.Bind(options);
            }
        }

        public void Configure(string name, CompositeLoggerFilterOptions options)
        {
            // todo: parse and populate the options
        }



        public void Configure(FileLoggerOptions options)
        {
            // no op
        }

        public void Configure(string name, LoggerFilterOptions options)
        {
        }

        public void Configure(LoggerFilterOptions options)
        {
        }

        public void Configure(CompositeLoggerFilterOptions options)
        {
            throw new NotImplementedException();
        }

    }
}
