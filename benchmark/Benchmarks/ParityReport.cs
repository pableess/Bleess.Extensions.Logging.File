using System;
using System.IO;
using System.Linq;

namespace Benchmarks;

/// <summary>
/// Writes the same input through every library and reports the bytes written per message, so the
/// benchmark tables can be checked for parity. Run with <c>dotnet run -c Release -- --parity</c>.
/// </summary>
internal static class ParityReport
{
    private const int MessageCount = 1000;

    public static void Run()
    {
        Console.WriteLine($"Parity report: {MessageCount} messages per case, bytes measured after the provider is flushed and closed.");
        Console.WriteLine();
        Console.WriteLine($"{"Format",-12} {"Library",-10} {"Bytes",12} {"B/msg",8}  Preview");
        Console.WriteLine(new string('-', 100));

        foreach (LogFormat format in new[] { LogFormat.MessageOnly, LogFormat.Realistic, LogFormat.Json })
        {
            foreach (Library library in new[] { Library.Bleess, Library.Karambolo, Library.NReco })
            {
                if (format == LogFormat.Json && library == Library.NReco)
                {
                    continue;
                }

                RunCase(format, library);
            }
        }
    }

    private static void RunCase(LogFormat format, Library library)
    {
        using var directory = new TempDirectory();
        var messages = MessageSet.Create(MessageShape.Small);

        var target = TargetFactory.Create(library, TargetFactory.Options(directory, "parity.log", format));
        try
        {
            for (int i = 0; i < MessageCount; i++)
            {
                messages.Write(target.Logger);
            }
        }
        finally
        {
            target.Dispose();
        }

        long bytes = directory.TotalBytes;
        string preview = Preview(directory.Path);

        Console.WriteLine($"{format,-12} {library,-10} {bytes,12:N0} {bytes / (double)MessageCount,8:F1}  {preview}");
    }

    private static string Preview(string directory)
    {
        string? firstFile = Directory.EnumerateFiles(directory).OrderBy(f => f, StringComparer.Ordinal).FirstOrDefault();
        if (firstFile is null)
        {
            return "<no file>";
        }

        string? line = File.ReadLines(firstFile).FirstOrDefault();
        if (line is null)
        {
            return "<empty>";
        }

        return line.Length <= 70 ? line : line.Substring(0, 70) + "...";
    }
}
