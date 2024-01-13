using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

#if NET8_0_OR_GREATER

namespace Bleess.Extensions.Logging.File.Named
{
    /// <summary>
    /// Configuration factory for named loggers
    /// </summary>
    public interface INamedLoggerProviderConfigurationFactory 
    {
        /// <summary>
        /// Gets a configuration for the named logger
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="name"></param>
        public IConfiguration GetConfiguration(Type providerType, string name);
    }

    internal sealed class NamedLoggerProviderConfigurationFactory : INamedLoggerProviderConfigurationFactory
    {
        private readonly IEnumerable<IConfiguration> _configurations;

        public NamedLoggerProviderConfigurationFactory(LoggingConfigurationAccessor accessor)
        {
            _configurations = accessor.Configurations;
        }

        public IConfiguration GetConfiguration(Type providerType, string name)
        {
            ArgumentNullException.ThrowIfNull(providerType);

            string fullName = providerType.FullName!;
            var configurationBuilder = new ConfigurationBuilder();
            foreach (IConfiguration configuration in _configurations)
            {
                IConfigurationSection sectionFromFullName = configuration.GetSection(fullName);
                configurationBuilder.AddConfiguration(sectionFromFullName);

                if (!string.IsNullOrWhiteSpace(name))
                {
                    IConfigurationSection sectionFromAlias = configuration.GetSection(name);
                    configurationBuilder.AddConfiguration(sectionFromAlias);
                }
            }
            return configurationBuilder.Build();
        }
    }

    internal sealed class LoggingConfigurationAccessor
    {
        public IEnumerable<IConfiguration> Configurations { get; }

        public LoggingConfigurationAccessor(IServiceProvider serviceProvider)
        {
            var loggingConfigurationType = typeof(LoggerProviderOptions).Assembly.GetType("Microsoft.Extensions.Logging.Configuration.LoggingConfiguration") 
                ?? throw new InvalidOperationException("Unable to access internal LoggingConfiguration value.  Not compatible with Logging configuration version");

            var confs = serviceProvider.GetServices(loggingConfigurationType);

            Configurations = confs.Select(c => c.GetType().GetProperty("Configuration", BindingFlags.Instance | BindingFlags.Public)?.GetValue(c) as IConfiguration).Where(c => c != null);
        }
    }
}

#endif
