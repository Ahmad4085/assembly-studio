using System.Diagnostics;
using System.IO;
using System.Windows.Automation;
using Xunit;

namespace AssemblyStudio.App.Tests.Documents;

/// <summary>
/// AC-050 (NFR-011) end to end: the real application gets unsaved edits through UI Automation, autosaves them, is
/// killed, and on the next start offers to restore them; Restore brings the text back. Each run uses its own
/// settings and recovery folders, so the user's own data is never touched.
/// </summary>
[Collection(nameof(AppProcessTests))]
public sealed class CrashRecoveryProcessTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private readonly string _folder = Directory.CreateTempSubdirectory("asmstudio-ac050-").FullName;
    private readonly List<Process> _processes = [];

    public void Dispose()
    {
        foreach (Process process in _processes)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(10_000);
            }

            process.Dispose();
        }

        Directory.Delete(_folder, recursive: true);
    }

    private string RecoveryFolder => Path.Combine(_folder, "recovery");

    private Process Launch()
    {
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "AssemblyStudio.App.exe")) { UseShellExecute = false };
        foreach (string argument in new[] { "--settings", Path.Combine(_folder, "settings.json"), "--recovery", RecoveryFolder, "--autosave-seconds", "1" })
        {
            start.ArgumentList.Add(argument);
        }

        Process process = Process.Start(start)!;
        _processes.Add(process);
        return process;
    }

    private static T Poll<T>(Func<T?> find, string what) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            if (find() is { } found)
            {
                return found;
            }

            Assert.True(stopwatch.Elapsed < Timeout, $"Timed out waiting for {what}.");
            Thread.Sleep(100);
        }
    }

    /// <summary>A window of <paramref name="process"/>: a top-level one, or (for an owned dialog, which UI Automation
    /// lists under its owner) one inside a top-level window.</summary>
    private static AutomationElement Window(Process process, string? name = null) => Poll(() =>
    {
        var processCondition = new PropertyCondition(AutomationElement.ProcessIdProperty, process.Id);
        foreach (AutomationElement top in AutomationElement.RootElement.FindAll(TreeScope.Children, processCondition))
        {
            if (name is null || top.Current.Name == name)
            {
                return top;
            }

            if (top.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
                new PropertyCondition(AutomationElement.NameProperty, name))) is { } owned)
            {
                return owned;
            }
        }

        return null;
    }, (name ?? "the main window") + " (windows now: " + string.Join(", ", AutomationElement.RootElement
        .FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ProcessIdProperty, process.Id))
        .Cast<AutomationElement>().Select(w => w.Current.Name)) + ")");

    private static AutomationElement Editor(AutomationElement window) => Poll(() =>
        window.FindFirst(TreeScope.Descendants, new AndCondition(
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
            new PropertyCondition(AutomationElement.NameProperty, "Untitled"))), "the editor");

    [Fact]
    public void AC_050_unsaved_edits_survive_a_killed_process_and_are_restored_on_the_next_start()
    {
        const string text = "; typed before the crash\r\nmov ax, 4C00h\r\n";

        Process first = Launch();
        AutomationElement editor = Editor(Window(first));
        ((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).SetValue(text);
        Poll(() => Directory.Exists(RecoveryFolder)
            ? Directory.GetFiles(RecoveryFolder, "*.json", SearchOption.AllDirectories).FirstOrDefault(f => File.ReadAllText(f).Contains("typed before the crash", StringComparison.Ordinal))
            : null, "the autosave file");

        first.Kill(entireProcessTree: true); // an unexpected termination: no clean-exit cleanup
        Assert.True(first.WaitForExit(10_000));

        Process second = Launch();
        AutomationElement prompt = Window(second, "Restore unsaved work?");
        AutomationElement restore = Poll(() => prompt.FindFirst(TreeScope.Descendants, new AndCondition(
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
            new PropertyCondition(AutomationElement.NameProperty, "Restore"))), "the Restore button");
        ((InvokePattern)restore.GetCurrentPattern(InvokePattern.Pattern)).Invoke();

        AutomationElement restored = Editor(Window(second, "Assembly Studio"));
        Poll(() => ((ValuePattern)restored.GetCurrentPattern(ValuePattern.Pattern)).Current.Value == text ? restored : null, "the restored text");
    }
}
