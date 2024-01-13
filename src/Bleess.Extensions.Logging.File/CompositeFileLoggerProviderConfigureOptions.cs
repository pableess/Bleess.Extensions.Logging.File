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

        private const string LogLevelKey = "LogLevel";
        private const string DefaultCategory = "Default";

        public void Configure(string name, CompositeLoggerFilterOptions options)
        {
            // todo: parse and populate the options

            // find config section 
            var section = _configuration.GetSection(name);
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
        }


        private static void LoadRules(LoggerFilterOptions options, IConfigurationSection configurationSection, string? logger)
        {
            foreach (System.Collections.Generic.KeyValuePair<string, string?> section in configurationSection.AsEnumerable(true))
            {
                if (TryGetSwitch(section.Value, out LogLevel level))
                {
                    string? category = section.Key;
                    if (category.Equals(DefaultCategory, StringComparison.OrdinalIgnoreCase))
                    {
                        category = null;
                    }
                    var newRule = new LoggerFilterRule(logger, category, level, null);
                    options.Rules.Add(newRule);
                }
            }
        }

        private static bool TryGetSwitch(string? value, out LogLevel level)
        {
            if (string.IsNullOrEmpty(value))
            {
                level = LogLevel.None;
                return false;
            }
            else if (Enum.TryParse(value, true, out level))
            {
                return true;
            }
            else
            {
                throw new InvalidOperationException($"Configuration value '{value}' is not supported.");
            }
        }
    }
}
