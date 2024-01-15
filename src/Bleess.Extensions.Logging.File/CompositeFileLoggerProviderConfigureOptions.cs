using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

#nullable enable

namespace Bleess.Extensions.Logging.File
{
    internal class CompositeOptionsChangeTokenSourceSourceBase<TOptions> : IOptionsChangeTokenSource<TOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly string _name;
        private readonly Func<IConfiguration, IChangeToken> _getChangeToken;
        public CompositeOptionsChangeTokenSourceSourceBase(ILoggerProviderConfiguration<CompositeFileLoggerProvider> providerConfiguration, string name, Func<IConfiguration, IChangeToken> getToken)
        {
            _name = GetName(name);
            _configuration = providerConfiguration.Configuration;
            _getChangeToken = getToken;
        }

        protected static string GetName(string? name) => name ?? Options.DefaultName;

        public string? Name => _name;

        public IChangeToken GetChangeToken() => _getChangeToken(_configuration);
    }

    internal class CompositeFileLoggerOptionsChangeTokenSource : CompositeOptionsChangeTokenSourceSourceBase<FileLoggerOptions>
    {
        public CompositeFileLoggerOptionsChangeTokenSource(ILoggerProviderConfiguration<CompositeFileLoggerProvider> providerConfiguration, string name)
            : base(providerConfiguration, name, conf => conf.GetSection(GetName(name)).GetReloadToken())
        {
        }
    }

    internal class CompositeLoggerFilterOptionsChangeTokenSource : CompositeOptionsChangeTokenSourceSourceBase<CompositeLoggerFilterOptions>
    {
        public CompositeLoggerFilterOptionsChangeTokenSource(ILoggerProviderConfiguration<CompositeFileLoggerProvider> providerConfiguration, string name)
            : base(providerConfiguration, name, conf => conf.GetSection(GetName(name)).GetReloadToken())
        {
        }
    }

    internal class CompositeFileLoggerProviderConfigureOptions : 
        IConfigureNamedOptions<FileLoggerOptions>,
        IConfigureNamedOptions<CompositeLoggerFilterOptions>
    {
        private readonly IConfiguration _configuration;

        public CompositeFileLoggerProviderConfigureOptions(ILoggerProviderConfiguration<CompositeFileLoggerProvider> providerConfiguration)
        {
            _configuration = providerConfiguration.Configuration;
        }

       

        public void Configure(FileLoggerOptions options)
        {
            _configuration.Bind(options);
        }

        public void Configure(string? name, FileLoggerOptions options)
        {
            if (!string.IsNullOrEmpty(name))
            {
                // look for a file provider 
                var section = _configuration.GetSection(name);
                section.Bind(options);
            }
            else 
            {
                Configure(options);
            }
        }

        private const string LogLevelKey = "LogLevel";
        private const string DefaultCategory = "Default";


        public void Configure(string? name, CompositeLoggerFilterOptions options)
        {
            // todo: parse and populate the options
            if (!string.IsNullOrEmpty(name))
            {
                // find config section that represents the sub provider
                var section = _configuration.GetSection(name);

                IConfigurationSection logLevelSection = section.GetSection(LogLevelKey);
                if (logLevelSection != null)
                {
                    // Load logger specific rules
                    string logger = section.Key;
                    LoadRules(options, logLevelSection, logger);
                }
            }
            else 
            {
                this.Configure(options);
            }
        }


        private static void LoadRules(CompositeLoggerFilterOptions options, IConfigurationSection configurationSection, string? logger)
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

        #region no-op

        public void Configure(CompositeLoggerFilterOptions options)
        {
            IConfigurationSection logLevelSection = _configuration.GetSection(LogLevelKey);
            if (logLevelSection != null)
            {
                // Load logger specific rules
                LoadRules(options, logLevelSection, null);
            }
        }
       
        #endregion
    }
}
