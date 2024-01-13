using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml.Linq;

#if NET8_0_OR_GREATER

namespace Bleess.Extensions.Logging.File.Named;

/// <summary>
/// Loads settings for FileLoggerProvider into FileLoggerOptions type.
/// </summary>
internal sealed class NamedLoggerProviderConfigureOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions, TProvider> :
    IConfigureNamedOptions<TOptions> where TOptions : class
{
    private readonly INamedLoggerProviderConfigurationFactory _namedConfigurationFactory;

    [RequiresDynamicCode(NamedLoggerProviderOptions.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(NamedLoggerProviderOptions.TrimmingRequiresUnreferencedCodeMessage)]
    public NamedLoggerProviderConfigureOptions(INamedLoggerProviderConfigurationFactory namedConfigurationFactory) 
    {
        _namedConfigurationFactory = namedConfigurationFactory;
    }

    public void Configure(string name, TOptions options)
    {
        var configuration = _namedConfigurationFactory.GetConfiguration(typeof(TProvider), name);

        ConfigurationBinder.Bind(configuration, options);
    }

    // no op.. this is handled non named factory
    public void Configure(TOptions options) { }
}

#endif
