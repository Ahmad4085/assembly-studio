using System.Globalization;
using System.IO;
using System.Text;

namespace AssemblyStudio.App.Logging;

/// <summary>How serious a log entry is.</summary>
public enum LogLevel
{
    /// <summary>Normal events (startup).</summary>
    Info,

    /// <summary>Something failed but the application carried on.</summary>
    Warning,

    /// <summary>An error, such as an unhandled exception.</summary>
    Error,
}

/// <summary>The local diagnostics log (NFR-012, ADR-0007 §5). Writing never throws.</summary>
public interface ILog
{
    /// <summary>Writes an entry; <paramref name="exception"/> adds its type, message and stack.</summary>
    void Write(LogLevel level, string message, Exception? exception = null);
}

/// <summary>A log that discards everything (tests, and when the log folder can't be used).</summary>
public sealed class NullLog : ILog
{
    /// <summary>The shared instance.</summary>
    public static NullLog Instance { get; } = new();

    /// <inheritdoc />
    public void Write(LogLevel level, string message, Exception? exception = null)
    {
    }
}

/// <summary>
/// A small rolling text log in <c>%LOCALAPPDATA%\AssemblyStudio\logs</c> (NFR-012, ADR-0007 §5: in-house, no
/// dependency). Entries go to <c>assemblystudio-&lt;timestamp&gt;.log</c>; when that file would pass
/// <see cref="FileLimit"/> a new one is started, and the oldest files are deleted so the folder holds at most
/// <see cref="TotalLimit"/> bytes of logs. Each entry is appended and flushed on its own, so a crash loses nothing
/// already written. Local only: nothing here, or anywhere in the application, sends log data anywhere (NFR-009).
/// </summary>
public sealed class FileLog : ILog
{
    /// <summary>The cap on all log files together (NFR-012: 10 MB).</summary>
    public const long DefaultTotalLimit = 10 * 1024 * 1024;

    private const string Prefix = "assemblystudio-";
    private const string Extension = ".log";
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private readonly Lock _lock = new();
    private string? _current;
    private int _sequence;

    /// <summary>Creates the log in <paramref name="folder"/> (created on the first write).</summary>
    /// <param name="folder">The log folder.</param>
    /// <param name="totalLimit">The cap on all log files together; one file holds at most a fifth of it.</param>
    public FileLog(string folder, long totalLimit = DefaultTotalLimit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentOutOfRangeException.ThrowIfLessThan(totalLimit, 5 * 1024);
        Folder = Path.GetFullPath(folder);
        TotalLimit = totalLimit;
        FileLimit = totalLimit / 5;
    }

    /// <summary>The default log folder for the current user.</summary>
    public static string DefaultFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AssemblyStudio", "logs");

    /// <summary>The log folder.</summary>
    public string Folder { get; }

    /// <summary>The cap on all log files together.</summary>
    public long TotalLimit { get; }

    /// <summary>The largest size of one log file.</summary>
    public long FileLimit { get; }

    /// <inheritdoc />
    public void Write(LogLevel level, string message, Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        byte[] entry = Utf8.GetBytes(Format(DateTimeOffset.Now, level, message, exception));
        if (entry.Length > FileLimit)
        {
            entry = Utf8.GetBytes(Format(DateTimeOffset.Now, level, message[..Math.Min(message.Length, 1000)] + " [entry truncated]", null));
        }

        lock (_lock)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                string file = CurrentFile(entry.Length);
                using (var stream = new FileStream(file, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                {
                    stream.Write(entry);
                }

                Trim();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Logging must never disturb the application (NFR-010).
            }
        }
    }

    /// <summary>One entry: "2026-10-30 13:20:01.123 +05:00 ERROR message", then the exception, if any.</summary>
    internal static string Format(DateTimeOffset time, LogLevel level, string message, Exception? exception)
    {
        var text = new StringBuilder()
            .Append(time.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture))
            .Append(' ')
            .Append(level.ToString().ToUpperInvariant())
            .Append(' ')
            .Append(message.ReplaceLineEndings(Environment.NewLine + "    "))
            .AppendLine();
        if (exception is not null)
        {
            text.Append("    ").AppendLine(exception.ToString().ReplaceLineEndings(Environment.NewLine + "    "));
        }

        return text.ToString();
    }

    /// <summary>The file to append <paramref name="length"/> bytes to: the current one, or a new one when it is full.</summary>
    private string CurrentFile(int length)
    {
        if (_current is not null && (!File.Exists(_current) || new FileInfo(_current).Length + length <= FileLimit))
        {
            return _current;
        }

        string name;
        do
        {
            name = Path.Combine(Folder, $"{Prefix}{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}-{_sequence++:D3}{Extension}");
        }
        while (File.Exists(name));

        _current = name;
        return name;
    }

    /// <summary>Deletes the oldest log files until the folder is within <see cref="TotalLimit"/>.</summary>
    private void Trim()
    {
        List<FileInfo> files = [.. new DirectoryInfo(Folder).GetFiles(Prefix + "*" + Extension)
            .OrderBy(f => f.LastWriteTimeUtc).ThenBy(f => f.Name, StringComparer.Ordinal)];
        long total = files.Sum(f => f.Length);
        foreach (FileInfo file in files)
        {
            if (total <= TotalLimit)
            {
                break;
            }

            if (string.Equals(file.FullName, _current, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                file.Delete();
                total -= file.Length;
            }
            catch (IOException)
            {
                // In use by another instance: it is trimmed later.
            }
        }
    }
}
