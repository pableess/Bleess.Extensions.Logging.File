using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace Bleess.Extensions.Logging.File
{
    internal class CompositeLoggerFormatterOptionsChangeTokenSource<TOptions> : CompositeOptionsChangeTokenSourceSourceBase<TOptions>
    {
        public CompositeLoggerFormatterOptionsChangeTokenSource(ILoggerProviderConfiguration<CompositeFileLoggerProvider> providerConfiguration, string name)
            : base(providerConfiguration, name, conf => conf.GetSection(GetName(name)).GetSection("FormatterOptions").GetReloadToken())
        {
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Options are bound from configuration. This is the same reflection requirement as the built-in Microsoft.Extensions.Logging providers; trimmed apps must preserve their options members.")]
    [UnconditionalSuppressMessage("Aot", "IL3050", Justification = "See IL2026: configuration binding of TOptions requires reflection.")]
    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "TOptions is supplied by the application and bound from configuration.")]
    internal class CompositeFileLoggerFormatterConfigureOptions<TOptions> : IConfigureNamedOptions<TOptions>
        where TOptions : class
    {
        private readonly IConfiguration _configuration;

        public CompositeFileLoggerFormatterConfigureOptions(ILoggerProviderConfiguration<CompositeFileLoggerProvider> providerConfiguration)
        {
            _configuration = providerConfiguration.Configuration;
        }

        public void Configure(string? name, TOptions options)
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
