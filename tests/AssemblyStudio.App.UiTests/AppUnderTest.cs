using System.Diagnostics;
using System.IO;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace AssemblyStudio.App.UiTests;

/// <summary>
/// The real application started for a UI test with temporary settings, recovery and log folders (never the user's), its
/// main window through FlaUI.UIA3 (ADR-0008), and a guard that sends keyboard input only while this application's window is
/// in front (TASK-085: the user's other windows must never receive the test's key presses).
/// </summary>
internal sealed class AppUnderTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("asmstudio-ui-").FullName;
    private bool _hadFocus;

    public AppUnderTest(params string[] files)
    {
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "AssemblyStudio.App.exe")) { UseShellExecute = false };
        foreach (string argument in new[] { "--settings", Path.Combine(_folder, "settings.json"), "--recovery", Path.Combine(_folder, "recovery"), "--log", Path.Combine(_folder, "logs") }.Concat(files))
        {
            start.ArgumentList.Add(argument);
        }

        Application = Application.Launch(start);
        Automation = new UIA3Automation();
        Window = Application.GetMainWindow(Automation, TimeSpan.FromSeconds(30))
            ?? throw new InvalidOperationException("The main window didn't appear.");
    }

    public Application Application { get; }

    public UIA3Automation Automation { get; }

    public Window Window { get; }

    public int ProcessId => Application.ProcessId;

    /// <summary>Brings the window to the front and returns only when the keyboard focus is in this application; else fails
    /// without sending anything.</summary>
    public void EnsureForeground()
    {
        // Windows may refuse to bring a background process to the front (focus-stealing prevention). The first time, wait up
        // to a minute for the person running the test to click the window; nothing is sent until the focus is here.
        if (!_hadFocus)
        {
            var waited = Stopwatch.StartNew();
            Window.SetForeground();
            while (!IsForeground() && waited.Elapsed < TimeSpan.FromSeconds(60))
            {
                Thread.Sleep(250);
            }

            _hadFocus = IsForeground();
        }

        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (IsForeground())
            {
                return;
            }

            Window.WaitUntilClickable(TimeSpan.FromSeconds(5));
            Window.Focus();
            Window.SetForeground();
            Thread.Sleep(400);
        }

        string holder;
        try
        {
            int pid = Automation.FocusedElement()?.Properties.ProcessId.ValueOrDefault ?? 0;
            holder = pid == 0 ? "unknown" : $"{Process.GetProcessById(pid).ProcessName} (pid {pid})";
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            holder = "unknown";
        }

        throw new InvalidOperationException($"The application isn't the foreground window (focus is in {holder}); no keys were sent.");
    }

    public void Dispose()
    {
        Automation.Dispose();
        if (!Application.HasExited)
        {
            Application.Kill(); // no save prompts: the test's documents are never saved
        }

        Application.Dispose();
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
            // the log may still be closing; the folder is under %TEMP%
        }
    }

    // The keyboard focus is in this process only while its window is the foreground window.
    private bool IsForeground()
    {
        try
        {
            return Automation.FocusedElement()?.Properties.ProcessId.ValueOrDefault == ProcessId;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return false; // the focused element went away while it was read
        }
    }
}
