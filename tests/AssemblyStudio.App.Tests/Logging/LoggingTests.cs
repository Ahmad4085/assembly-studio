using System.IO;
using System.Reflection;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Logging;
using AssemblyStudio.App.Tests.Documents;
using Xunit;

namespace AssemblyStudio.App.Tests.Logging;

/// <summary>TASK-068, NFR-012 / AC-063: the local rolling diagnostics log.</summary>
public sealed class LoggingTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("asmstudio-task068-log-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string[] LogFiles() => Directory.GetFiles(_folder, "assemblystudio-*.log");

    private string AllText() => string.Concat(LogFiles().Order(StringComparer.Ordinal).Select(File.ReadAllText));

    [Fact]
    public void Entries_have_a_time_a_level_the_message_and_the_exception()
    {
        var time = new DateTimeOffset(2026, 10, 30, 13, 20, 1, 123, TimeSpan.FromHours(5));
        Exception error;
        try
        {
            throw new IOException("disk full");
        }
        catch (IOException ex)
        {
            error = ex;
        }

        string entry = FileLog.Format(time, LogLevel.Warning, "Save failed: C:\\a.asm", error);

        Assert.StartsWith("2026-10-30 13:20:01.123 +05:00 WARNING Save failed: C:\\a.asm" + Environment.NewLine, entry, StringComparison.Ordinal);
        Assert.Contains("    System.IO.IOException: disk full", entry, StringComparison.Ordinal);
        Assert.Contains(nameof(Entries_have_a_time_a_level_the_message_and_the_exception), entry, StringComparison.Ordinal); // the stack
        Assert.All(entry.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Skip(1), line => Assert.StartsWith("    ", line, StringComparison.Ordinal));
    }

    [Fact]
    public void Multi_line_messages_stay_one_indented_entry()
    {
        string entry = FileLog.Format(DateTimeOffset.Now, LogLevel.Info, "first\nsecond\r\nthird", null);

        Assert.Equal(3, entry.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.Contains(Environment.NewLine + "    second" + Environment.NewLine + "    third", entry, StringComparison.Ordinal);
    }

    [Fact]
    public void AC_063_errors_are_written_to_the_log_folder()
    {
        var log = new FileLog(Path.Combine(_folder));

        log.Write(LogLevel.Info, "started");
        log.Write(LogLevel.Error, "Unhandled exception.", new InvalidOperationException("boom"));

        string text = AllText();
        Assert.Contains(" INFO started", text, StringComparison.Ordinal);
        Assert.Contains(" ERROR Unhandled exception.", text, StringComparison.Ordinal);
        Assert.Contains("System.InvalidOperationException: boom", text, StringComparison.Ordinal);
        Assert.Single(LogFiles());
    }

    [Fact]
    public void AC_063_the_total_size_stays_within_the_cap_and_the_newest_entries_are_kept()
    {
        const long cap = 50 * 1024;
        var log = new FileLog(_folder, cap);
        string padding = new('x', 200);

        for (int i = 0; i < 2000; i++) // about 470 KB written, nearly ten times the cap
        {
            log.Write(LogLevel.Warning, $"entry {i:D4} {padding}");
        }

        FileInfo[] files = [.. LogFiles().Select(f => new FileInfo(f))];
        Assert.InRange(files.Sum(f => f.Length), cap / 2, cap);
        Assert.All(files, f => Assert.True(f.Length <= log.FileLimit, $"{f.Name}: {f.Length}"));
        Assert.Contains("entry 1999 ", AllText(), StringComparison.Ordinal);
        Assert.DoesNotContain("entry 0000 ", AllText(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_default_cap_is_10_MB_and_the_folder_is_local()
    {
        Assert.Equal(10 * 1024 * 1024, FileLog.DefaultTotalLimit);
        Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AssemblyStudio", "logs"), FileLog.DefaultFolder);
    }

    [Fact]
    public void A_huge_entry_is_shortened_rather_than_breaking_the_cap()
    {
        var log = new FileLog(_folder, 10 * 1024);

        log.Write(LogLevel.Error, new string('y', 100_000));

        Assert.Contains("[entry truncated]", AllText(), StringComparison.Ordinal);
        Assert.True(LogFiles().Sum(f => new FileInfo(f).Length) <= 10 * 1024);
    }

    [Fact]
    public void Writing_never_throws_when_the_folder_cannot_be_used()
    {
        string blocker = Path.Combine(_folder, "not-a-folder");
        File.WriteAllText(blocker, "a file where the log folder should be");
        var log = new FileLog(Path.Combine(blocker, "logs"));

        log.Write(LogLevel.Error, "lost, but harmless");

        Assert.Equal("a file where the log folder should be", File.ReadAllText(blocker));
    }

    [Fact]
    public void Concurrent_writers_produce_whole_entries()
    {
        var log = new FileLog(_folder);

        Parallel.For(0, 400, i => log.Write(LogLevel.Info, $"parallel {i:D3} end"));

        string[] lines = AllText().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(400, lines.Length);
        Assert.All(lines, l => Assert.Matches(@" INFO parallel \d{3} end$", l));
    }

    [Fact]
    public void File_errors_shown_to_the_user_are_also_logged()
    {
        var log = new MemoryLog();
        var dialogs = new WorkspaceViewModelTests.FakeDialogs();
        var workspace = new WorkspaceViewModel(dialogs, new MemoryRecentFilesStore(), log);
        string missing = Path.Combine(_folder, "missing.asm");

        workspace.OpenFile(missing);

        Assert.Single(dialogs.Errors);
        (LogLevel level, string message, Exception? exception) = Assert.Single(log.Entries);
        Assert.Equal((LogLevel.Warning, $"Open failed: {missing}"), (level, message));
        Assert.IsType<FileNotFoundException>(exception);
    }

    [Fact]
    public void AC_063_no_application_assembly_references_a_networking_library()
    {
        string[] network = ["System.Net.Http", "System.Net.Sockets", "System.Net.Requests", "System.Net.WebClient", "System.Net.Mail", "System.Net.WebSockets.Client"];
        Assembly[] ours = [.. Directory.GetFiles(AppContext.BaseDirectory, "AssemblyStudio.*.dll")
            .Where(f => !Path.GetFileName(f).Contains(".Tests", StringComparison.Ordinal))
            .Select(Assembly.LoadFrom)];
        Assert.Contains(ours, a => a.GetName().Name == "AssemblyStudio.App");

        foreach (Assembly assembly in ours)
        {
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), r => network.Contains(r.Name));
        }
    }

    internal sealed class MemoryLog : ILog
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public void Write(LogLevel level, string message, Exception? exception = null) => Entries.Add((level, message, exception));
    }
}
