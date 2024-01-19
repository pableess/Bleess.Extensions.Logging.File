using Bleess.Extensions.Logging.File;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample
{
    public record MyOptions
    {
        public bool Option1 { get; set; }
    }

    public class MyCustomeFormatter : FileFormatter<MyOptions>
    {
        public const string FormatterName = "MyCustom";

        public MyCustomeFormatter(IOptionsMonitor<MyOptions> options)
            : base(FormatterName, options)
        {
        }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter, string subProviderName = null, bool? fallbackIncludeScopes = null)
        {
            var options = this.GetOptions(subProviderName);

            if (options.Option1)
            {
                textWriter.WriteLine("Foo");
            }
            else
            {
                textWriter.WriteLine("Bar");
            }
        }
    }
}
