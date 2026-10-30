using System.Diagnostics;
using System.IO;
using System.Windows.Automation;
using Xunit;

namespace AssemblyStudio.App.Tests.Settings;

/// <summary>
/// AC-039 (FR-081) in the real application: started with a corrupted settings file, it opens with defaults, keeps the
/// bad file as a backup and tells the user. Uses its own settings and recovery folders.
/// </summary>
[Collection(nameof(AppProcessTests))]
public sealed class SettingsProcessTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private readonly string _folder = Directory.CreateTempSubdirectory("asmstudio-ac039-").FullName;
    private Process? _process;

    public void Dispose()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(10_000);
        }

        _process?.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void AC_039_a_corrupted_settings_file_is_backed_up_and_the_user_is_told()
    {
        string settings = Path.Combine(_folder, "settings.json");
        File.WriteAllText(settings, "{ \"settings\": { \"fontSize\": ");
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "AssemblyStudio.App.exe")) { UseShellExecute = false };
        string logs = Path.Combine(_folder, "logs");
        foreach (string argument in new[] { "--settings", settings, "--recovery", Path.Combine(_folder, "recovery"), "--log", logs })
        {
            start.ArgumentList.Add(argument);
        }

        _process = Process.Start(start)!;
        AutomationElement notice = Poll(() =>
            AutomationElement.RootElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ProcessIdProperty, _process.Id))?
                .FindFirst(TreeScope.Descendants, new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
                    new PropertyCondition(AutomationElement.NameProperty, "Settings were reset"))));

        string backup = Assert.Single(Directory.GetFiles(_folder, "settings.invalid-*.json"));
        Assert.Equal("{ \"settings\": { \"fontSize\": ", File.ReadAllText(backup));
        Assert.NotNull(notice.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty,
            $"The settings file couldn't be read, so Assembly Studio started with default settings. The unreadable file was kept as {backup}.")));

        // AC-063: the start and the problem are in the local log.
        string log = File.ReadAllText(Assert.Single(Directory.GetFiles(logs, "assemblystudio-*.log")));
        Assert.Contains(" INFO Assembly Studio ", log, StringComparison.Ordinal);
        Assert.Contains($" WARNING The settings file could not be read; defaults are used. Backup: {backup}", log, StringComparison.Ordinal);
    }

    private static AutomationElement Poll(Func<AutomationElement?> find)
    {
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            if (find() is { } found)
            {
                return found;
            }

            Assert.True(stopwatch.Elapsed < Timeout, "Timed out waiting for the settings notice.");
            Thread.Sleep(100);
        }
    }
}
