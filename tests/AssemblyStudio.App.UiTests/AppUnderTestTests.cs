using System.Diagnostics;
using System.IO;
using Xunit;

namespace AssemblyStudio.App.UiTests;

/// <summary>
/// TASK-107 (DEF-020): a UI test whose application window never appears fails with the reason and the application's log, and
/// leaves no process and no temporary folder behind.
/// </summary>
[Collection(nameof(UiTestGroup))]
public sealed class AppUnderTestTests
{
    [Fact]
    public void DEF_020_a_start_without_a_window_fails_with_the_reason_and_leaves_no_process_or_folder()
    {
        string temp = Path.GetTempPath();
        HashSet<string> foldersBefore = [.. Directory.GetDirectories(temp, "asmstudio-ui-*")];
        HashSet<int> shellsBefore = [.. Process.GetProcessesByName("powershell").Select(p => p.Id)];

        // A process that runs but never shows a window; the folder arguments appended by AppUnderTest end up after "#".
        var start = new ProcessStartInfo("powershell.exe") { CreateNoWindow = true };
        foreach (string argument in new[] { "-NoProfile", "-NonInteractive", "-Command", "Start-Sleep -Seconds 120 #" })
        {
            start.ArgumentList.Add(argument);
        }

        var waited = Stopwatch.StartNew();
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => new AppUnderTest(start, TimeSpan.FromSeconds(3)));

        Assert.StartsWith("The main window didn't appear within 3 s", error.Message, StringComparison.Ordinal);
        Assert.Contains("The application wrote no log.", error.Message, StringComparison.Ordinal);
        Assert.True(waited.Elapsed < TimeSpan.FromSeconds(30), $"took {waited.Elapsed}"); // stopped, not waited out
        Assert.DoesNotContain(Process.GetProcessesByName("powershell"), p => !shellsBefore.Contains(p.Id) && !p.HasExited);
        Assert.DoesNotContain(Directory.GetDirectories(temp, "asmstudio-ui-*"), f => !foldersBefore.Contains(f));
    }

    [Fact]
    public void The_failure_message_carries_the_application_log()
    {
        string folder = Directory.CreateTempSubdirectory("asmstudio-uitest-log-").FullName;
        try
        {
            Assert.Equal("The application wrote no log.", AppUnderTest.LogText(folder));

            Directory.CreateDirectory(Path.Combine(folder, "logs"));
            File.WriteAllText(Path.Combine(folder, "logs", "assemblystudio-1.log"), "2026-11-02 20:06:47.525 +05:00 INFO Assembly Studio 1.0.0 started.\n");
            Assert.Equal($"Application log:{Environment.NewLine}2026-11-02 20:06:47.525 +05:00 INFO Assembly Studio 1.0.0 started.", AppUnderTest.LogText(folder));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
