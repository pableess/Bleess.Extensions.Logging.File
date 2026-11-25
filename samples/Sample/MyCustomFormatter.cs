using Bleess.Extensions.Logging.File;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;

namespace Sample
{
    public record MyOptions
    {
        public bool Option1 { get; set; }
    }

    public class MyCustomFormatter : FileFormatter<MyOptions>
    {
        public const string FormatterName = "MyCustom";

        public MyCustomFormatter(IOptionsMonitor<MyOptions> options, TimeProvider timeProvider)
            : base(FormatterName, options, timeProvider)
        {
        }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter, string? subProviderName = null, bool? fallbackIncludeScopes = null)
        {
            var options = this.GetOptions(subProviderName);

            textWriter.WriteLine(options.Option1 ? "Foo" : "Bar");
        }
    }
}
