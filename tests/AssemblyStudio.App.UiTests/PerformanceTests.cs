using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using Xunit;

namespace AssemblyStudio.App.UiTests;

/// <summary>
/// TASK-086 measurements on the real application process (reference machine, D-014): NFR-005 startup to the rendered main
/// window, and NFR-007 working set with a 1,000-line file and an active debug session. The application is driven through UI
/// Automation actions (invoke), never with key presses. Run by <c>build/bench/run-e2e.ps1</c> (<c>ASSEMBLY_STUDIO_BENCH=1</c>).
/// </summary>
[Collection(nameof(UiTestGroup))]
[Trait("Category", "Performance")]
public sealed class PerformanceTests
{
    private const string BenchVariable = "ASSEMBLY_STUDIO_BENCH";

    private static void RequireBench() =>
        Assert.SkipUnless(Environment.GetEnvironmentVariable(BenchVariable) == "1",
            $"End-to-end measurement: run build/bench/run-e2e.ps1 (sets {BenchVariable}=1) on the reference machine.");

    private static void Report(string line) => TestContext.Current.TestOutputHelper?.WriteLine(line);

    // The application's own startup timeline line from its log (TASK-108), or why it isn't there.
    private static string StartupLine(string logFolder)
    {
        string? line = Directory.Exists(logFolder)
            ? Directory.GetFiles(logFolder, "*.log").SelectMany(File.ReadLines).LastOrDefault(l => l.Contains(" ms to the first frame:", StringComparison.Ordinal))
            : null;
        return line is null ? "(no startup timeline in the log)" : line[(line.IndexOf("Startup ", StringComparison.Ordinal))..];
    }

    [Fact]
    public void NFR_005_the_main_window_is_rendered_within_3_s_of_the_process_start()
    {
        RequireBench();
        var samples = new List<double>();
        Report($"BENCH NFR-005 application: {AppUnderTest.Executable}");
        for (int i = 0; i < 5; i++)
        {
            string folder = Directory.CreateTempSubdirectory("asmstudio-start-").FullName;
            try
            {
                string report = Path.Combine(folder, "startup.txt");
                var start = new ProcessStartInfo(AppUnderTest.Executable) { UseShellExecute = false };
                foreach (string argument in new[] { "--settings", Path.Combine(folder, "settings.json"), "--recovery", Path.Combine(folder, "recovery"), "--log", Path.Combine(folder, "logs"), "--smoke-report", report })
                {
                    start.ArgumentList.Add(argument);
                }

                using Process process = Process.Start(start)!;
                Assert.True(process.WaitForExit(60_000), "the smoke run didn't close");
                samples.Add(double.Parse(File.ReadAllText(report), CultureInfo.InvariantCulture)); // process start to first rendered frame
                Report($"BENCH NFR-005 start {i + 1}: {StartupLine(Path.Combine(folder, "logs"))}"); // where the time went (TASK-108)
            }
            finally
            {
                Directory.Delete(folder, recursive: true);
            }
        }

        double first = samples[0];
        samples.Sort();
        Report(string.Create(CultureInfo.InvariantCulture,
            $"BENCH NFR-005 process start to rendered main window (warm starts, not after a reboot): n={samples.Count} median {samples[samples.Count / 2]:F0} ms, max {samples[^1]:F0} ms, first {first:F0} ms; target 3000 ms"));
        Assert.True(samples[^1] <= 3000, $"a start took {samples[^1]:F0} ms");
    }

    [Fact]
    public void NFR_007_the_working_set_stays_within_300_MB_with_a_1000_line_file_and_an_active_session()
    {
        RequireBench();
        string folder = Directory.CreateTempSubdirectory("asmstudio-mem-").FullName;
        try
        {
            string file = Path.Combine(folder, "thousand.asm");
            File.WriteAllText(file, ThousandLines(), Encoding.UTF8); // Encoding.UTF8 writes a BOM (DEF-017)
            using var app = new AppUnderTest(file);
            Window window = app.Window;
            AutomationElement Button(string id) => window.FindFirstDescendant(cf => cf.ByAutomationId(id)) ?? throw new Xunit.Sdk.XunitException(id);
            AutomationElement badge = Button("StateBadge");
            using Process process = Process.GetProcessById(app.ProcessId);
            var samples = new List<(string When, long WorkingSet, long Private)>();

            void Sample(string when)
            {
                process.Refresh();
                samples.Add((when, process.WorkingSet64, process.PrivateMemorySize64));
            }

            void Until(Func<bool> condition, string what) =>
                Assert.True(Retry.WhileFalse(condition, TimeSpan.FromSeconds(60), TimeSpan.FromMilliseconds(100)).Result, what);

            Until(() => window.FindFirstDescendant(cf => cf.ByName("thousand.asm")) is not null, "the file is open");
            Sample("file open");
            AutomationElement build = Button("BarBuild");
            Until(() => build.IsEnabled, "Build is enabled");
            build.Patterns.Invoke.Pattern.Invoke(); // the file has a UTF-8 BOM: also checks DEF-017 end to end
            Until(() => badge.Name.Contains("Ready", StringComparison.Ordinal), "Ready");
            Sample("built and loaded");
            for (int i = 0; i < 50; i++)
            {
                Button("BarStepInto").Patterns.Invoke.Pattern.Invoke();
                Thread.Sleep(30);
            }

            Until(() => badge.Name.Contains("Paused", StringComparison.Ordinal), "Paused after the steps");
            Sample("after 50 steps");
            Button("BarRun").Patterns.Invoke.Pattern.Invoke(); // the endless loop runs to its instruction budget (10,000,000)
            Thread.Sleep(3000);
            Sample("running / at the budget");
            Until(() => badge.Name.Contains("Paused", StringComparison.Ordinal), "paused at the budget");
            Thread.Sleep(500);
            Sample("paused at the budget, history full");
            process.Refresh();
            long peak = process.PeakWorkingSet64;

            foreach ((string when, long ws, long priv) in samples)
            {
                Report(string.Create(CultureInfo.InvariantCulture, $"BENCH NFR-007 {when}: working set {ws / 1048576.0:F1} MB, private {priv / 1048576.0:F1} MB"));
            }

            Report(string.Create(CultureInfo.InvariantCulture, $"BENCH NFR-007 peak working set {peak / 1048576.0:F1} MB; target 300 MB"));
            Assert.True(peak <= 300L * 1024 * 1024, $"peak working set {peak / 1048576.0:F1} MB");
        }
        finally
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (IOException)
            {
                // the closing app may still hold the file for a moment; the folder is under %TEMP%
            }
        }
    }

    /// <summary>The same 1,000-line program as the in-process build measurement (an endless loop at the end).</summary>
    private static string ThousandLines()
    {
        var text = new StringBuilder("org 100h\r\nstart:\r\n");
        for (int i = 0; i < 995; i++)
        {
            text.Append((i % 3) switch { 0 => "    inc ax\r\n", 1 => "    add bx, 3\r\n", _ => "    xor dx, cx\r\n" });
        }

        text.Append("again:\r\n    inc cx\r\n    jmp again\r\n");
        return text.ToString();
    }
}
