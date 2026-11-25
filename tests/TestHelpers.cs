using Microsoft.Extensions.Options;
using System;
using System.IO;

namespace Tests;

/// <summary>
/// Deterministic clock for rolling and timestamp tests.
/// </summary>
internal sealed class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public TestTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public void Advance(TimeSpan delta) => _utcNow += delta;

    public void Set(DateTimeOffset value) => _utcNow = value;
}

/// <summary>
/// Minimal <see cref="IOptionsMonitor{TOptions}"/> that can raise a change notification the same way
/// the real monitor does: with <see cref="Options.DefaultName"/> for the default instance.
/// </summary>
internal sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    where T : class
{
    private T _value;
    private Action<T, string>? _listener;

    public TestOptionsMonitor(T value)
    {
        _value = value;
    }

    public T CurrentValue => _value;

    public T Get(string? name) => _value;

    public IDisposable OnChange(Action<T, string> listener)
    {
        _listener = listener;
        return new NoopDisposable();
    }

    public void Set(T value)
    {
        _value = value;
        _listener?.Invoke(value, Options.DefaultName);
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}

/// <summary>
/// A unique, disposable directory so tests do not share log files.
/// </summary>
internal sealed class TestDirectory : IDisposable
{
    private TestDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bleess-logging-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public static TestDirectory Create() => new TestDirectory();

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
