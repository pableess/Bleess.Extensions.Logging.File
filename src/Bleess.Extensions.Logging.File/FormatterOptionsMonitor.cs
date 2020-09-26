using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bleess.Extensions.Logging.File
{
    internal class FormatterOptionsMonitor<TOptions> :
       IOptionsMonitor<TOptions>
       where TOptions : FileFormatterOptions
    {
        private TOptions _options;
        public FormatterOptionsMonitor(TOptions options)
        {
            _options = options;
        }

        public TOptions Get(string name) => _options;

        public IDisposable OnChange(Action<TOptions, string> listener)
        {
            return null;
        }

        public TOptions CurrentValue => _options;
    }
}
