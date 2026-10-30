using System.Diagnostics;
using System.Globalization;
using System.IO;
using Xunit;

namespace AssemblyStudio.App.Tests;

/// <summary>
/// NFR-005 measurement (TASK-060 test requirement): launches the real app in smoke mode, which
/// reports the time from process start to the first rendered frame of the main window and exits.
/// The 3 s target applies to the reference machine; this test records the value and fails only on
/// a gross regression (10 s) or a failed launch.
/// </summary>
[Collection(nameof(AppProcessTests))]
public class StartupTimeTests
{
    [Fact]
    public void App_starts_renders_the_main_window_and_reports_startup_time()
    {
        string exe = Path.Combine(AppContext.BaseDirectory, "AssemblyStudio.App.exe");
        Assert.True(File.Exists(exe), $"App executable not found next to the tests: {exe}");
        string dir = Path.Combine(Path.GetTempPath(), "AssemblyStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string report = Path.Combine(dir, "startup.txt");
        try
        {
            var start = new ProcessStartInfo(exe) { UseShellExecute = false };
            start.ArgumentList.Add("--settings");
            start.ArgumentList.Add(Path.Combine(dir, "settings.json"));
            start.ArgumentList.Add("--smoke-report");
            start.ArgumentList.Add(report);
            start.ArgumentList.Add("--recovery");
            start.ArgumentList.Add(Path.Combine(dir, "recovery")); // never touch the user's recovery folder
            start.ArgumentList.Add("--log");
            start.ArgumentList.Add(Path.Combine(dir, "logs")); // nor the user's log folder

            using Process process = Process.Start(start)!;
            bool exited = process.WaitForExit(TimeSpan.FromSeconds(60));
            if (!exited)
            {
                process.Kill();
            }

            Assert.True(exited, "The app did not exit within 60 s in smoke mode.");
            Assert.Equal(0, process.ExitCode);
            double startupMs = double.Parse(File.ReadAllText(report), CultureInfo.InvariantCulture);
            TestContext.Current.TestOutputHelper?.WriteLine($"Startup to first rendered frame: {startupMs:F0} ms (NFR-005 target 3000 ms on the reference machine)");
            Assert.InRange(startupMs, 1, 10_000);
            Assert.False(File.Exists(Path.Combine(dir, "settings.json")), "Smoke mode must not save the layout.");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
