using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// A builder for configuring multiple log files
    /// </summary>
    public sealed class MultipleFileLoggerProviderBuilder
    {
        private readonly ILoggingBuilder _builder;

        internal MultipleFileLoggerProviderBuilder(ILoggingBuilder logBuilder)
        {
            _builder = logBuilder;
        }

        /// <summary>
        /// Adds a log file with the provider alias.  This alias is used to configure the log file provider
        /// </summary>
        /// <param name="providerAlias">The provider alias, which can be referenced in configuration for setting options</param>
        /// <param name="configure">A configuration delegate for the options</param>
        /// <returns></returns>
        public NamedFileLoggerProviderBuilder AddFile(string providerAlias, Action<FileLoggerOptions> configure = null) 
        {
            if (configure == null)
            {
                _builder.AddFile(providerAlias);
            }
            else
            {
                _builder.AddFile(providerAlias, configure);
            }
            return new NamedFileLoggerProviderBuilder(_builder, providerAlias);
        }
    }
}

