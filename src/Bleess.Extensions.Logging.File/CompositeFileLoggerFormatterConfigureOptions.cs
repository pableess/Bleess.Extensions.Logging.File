using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace Bleess.Extensions.Logging.File
{
    internal class CompositeLoggerFormatterOptionsChangeTokenSource<TOptions> : CompositeOptionsChangeTokenSourceSourceBase<TOptions>
    {
        public CompositeLoggerFormatterOptionsChangeTokenSource(ILoggerProviderConfiguration<CompositeFileLoggerProvider> providerConfiguration, string name)
            : base(providerConfiguration, name, conf => conf.GetSection(GetName(name)).GetSection("FormatterOptions").GetReloadToken())
        {
        }
    }

    internal class CompositeFileLoggerFormatterConfigureOptions<TOptions> : IConfigureNamedOptions<TOptions>
        where TOptions : class
    {
        private readonly IConfiguration _configuration;

        public CompositeFileLoggerFormatterConfigureOptions(ILoggerProviderConfiguration<CompositeFileLoggerProvider> providerConfiguration)
        {
            _configuration = providerConfiguration.Configuration;
        }

        public void Configure(string name, TOptions options)
        {
            if (!string.IsNullOrEmpty(name))
            {
                // look for a file provider 
                var section = _configuration.GetSection(name);

                var formatterOptions = section.GetSection("FormatterOptions");

                formatterOptions.Bind(options);
            }
            else             
            {
                this.Configure(options);
            }
        }

        public void Configure(TOptions options)
        {
            var formatterOptions = _configuration.GetSection("FormatterOptions");
            formatterOptions.Bind(options);
        }
    }
}
