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
    /// <summary>How long the main window may take to appear: generous, because a full test run loads the machine (DEF-020).</summary>
    internal static readonly TimeSpan WindowTimeout = TimeSpan.FromSeconds(60);

    private readonly string _folder = Directory.CreateTempSubdirectory("asmstudio-ui-").FullName;
    private bool _hadFocus;

    public AppUnderTest(params string[] files)
        : this(new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "AssemblyStudio.App.exe")), WindowTimeout, files)
    {
    }

    /// <summary>Starts <paramref name="start"/> with the temporary folders and waits for its main window. When the window doesn't
    /// appear (or anything else fails), the process is stopped and the folder deleted before the exception leaves: a failed
    /// start leaves nothing behind (DEF-020), and the message carries the application's log.</summary>
    internal AppUnderTest(ProcessStartInfo start, TimeSpan windowTimeout, params string[] files)
    {
        start.UseShellExecute = false;
        foreach (string argument in new[] { "--settings", Path.Combine(_folder, "settings.json"), "--recovery", Path.Combine(_folder, "recovery"), "--log", Path.Combine(_folder, "logs") }.Concat(files))
        {
            start.ArgumentList.Add(argument);
        }

        Automation = new UIA3Automation();
        try
        {
            var waited = Stopwatch.StartNew();
            Application = Application.Launch(start);
            Window = Application.GetMainWindow(Automation, windowTimeout)
                ?? throw new InvalidOperationException(
                    $"The main window didn't appear within {windowTimeout.TotalSeconds:F0} s ({waited.Elapsed.TotalSeconds:F1} s waited). {LogText(_folder)}");
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>The temporary folder (settings, recovery, logs); deleted on dispose.</summary>
    internal string Folder => _folder;

    /// <summary>The application's log from <paramref name="folder"/>, for a failure message.</summary>
    internal static string LogText(string folder)
    {
        string logs = Path.Combine(folder, "logs");
        string[] files = Directory.Exists(logs) ? Directory.GetFiles(logs, "*.log") : [];
        if (files.Length == 0)
        {
            return "The application wrote no log.";
        }

        try
        {
            string text = string.Join(Environment.NewLine, files.Order(StringComparer.Ordinal).Select(f =>
            {
                using var reader = new StreamReader(new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
                return reader.ReadToEnd().TrimEnd();
            }));
            return $"Application log:{Environment.NewLine}{text}";
        }
        catch (IOException e)
        {
            return $"The application's log couldn't be read: {e.Message}";
        }
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
        if (Application is { } application) // null when the launch itself failed
        {
            if (!application.HasExited)
            {
                application.Kill(); // no save prompts: the test's documents are never saved
                WaitForExit(application.ProcessId); // its files are closed before the folder is deleted
            }

            application.Dispose();
        }

        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
            // the log may still be closing; the folder is under %TEMP%
        }
    }

    private static void WaitForExit(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            process.WaitForExit(10_000);
        }
        catch (ArgumentException)
        {
            // already gone
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
