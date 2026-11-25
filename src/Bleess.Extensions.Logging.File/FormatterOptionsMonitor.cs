using Microsoft.Extensions.Options;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// Minimal <see cref="IOptionsMonitor{TOptions}"/> used for the built-in formatters when no formatter
    /// instances were registered with the container.
    /// </summary>
    internal sealed class FormatterOptionsMonitor<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)] TOptions> : IOptionsMonitor<TOptions>
        where TOptions : FileFormatterOptions
    {
        private readonly TOptions _options;

        public FormatterOptionsMonitor(TOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public TOptions Get(string? name) => _options;

        public IDisposable OnChange(Action<TOptions, string> listener) => NoopDisposable.Instance;

        public TOptions CurrentValue => _options;

        private sealed class NoopDisposable : IDisposable
        {
            public static NoopDisposable Instance { get; } = new NoopDisposable();

            public void Dispose()
            {
            }
        }
    }
}
