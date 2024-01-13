using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NET8_0_OR_GREATER

namespace Bleess.Extensions.Logging.File.Named;


/// <summary>
/// Provides a set of helpers to initialize options objects from logger provider configuration.
/// </summary>
public static class NamedLoggerProviderOptions
{
    internal const string RequiresDynamicCodeMessage = "Binding TOptions to configuration values may require generating dynamic code at runtime.";
    internal const string TrimmingRequiresUnreferencedCodeMessage = "TOptions's dependent types may have their members trimmed. Ensure all required members are preserved.";

    /// <summary>
    /// Indicates that settings for <typeparamref name="TProvider"/> should be loaded into <typeparamref name="TOptions"/> type.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register on.</param>
    /// <typeparam name="TOptions">The options class </typeparam>
    /// <typeparam name="TProvider">The provider class</typeparam>
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(TrimmingRequiresUnreferencedCodeMessage)]
    public static void RegisterProviderOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions, TProvider>(IServiceCollection services) where TOptions : class
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<TOptions>, NamedLoggerProviderConfigureOptions<TOptions, TProvider>>());
    }
}

#endif