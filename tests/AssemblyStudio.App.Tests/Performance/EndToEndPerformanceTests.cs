using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows.Threading;
using AssemblyStudio.App.Commands;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Shell;
using AssemblyStudio.App.Tests.Commands;
using AssemblyStudio.App.Tests.Documents;
using Xunit;

namespace AssemblyStudio.App.Tests.Performance;

/// <summary>
/// TASK-086 end-to-end measurements through the real main window, the real NASM and the real session, on the reference
/// machine (D-014): NFR-002 step latency, NFR-004 responsiveness while running and building and Pause/Stop latency (AC-030
/// UI part), NFR-006 build turnaround. Wall-clock measurements are sensitive to load, so they run only in the measurement
/// script (<c>build/bench/run-e2e.ps1</c> sets <c>ASSEMBLY_STUDIO_BENCH=1</c>) and print a <c>BENCH</c> line each.
/// </summary>
[Collection(nameof(AppProcessTests))]
[Trait("Category", "Performance")]
public sealed class EndToEndPerformanceTests
{
    internal const string BenchVariable = "ASSEMBLY_STUDIO_BENCH";

    internal static void RequireBench() =>
        Assert.SkipUnless(Environment.GetEnvironmentVariable(BenchVariable) == "1",
            $"End-to-end measurement: run build/bench/run-e2e.ps1 (sets {BenchVariable}=1) on the reference machine.");

    /// <summary>A 1,000-line program (NFR-006, NFR-007) that ends in an endless loop.</summary>
    internal static string ThousandLines()
    {
        var text = new StringBuilder("org 100h\nstart:\n");
        for (int i = 0; i < 995; i++)
        {
            text.Append((i % 3) switch { 0 => "    inc ax\n", 1 => "    add bx, 3\n", _ => "    xor dx, cx\n" });
        }

        text.Append("again:\n    inc cx\n    jmp again\n");
        return text.ToString();
    }

    internal static string Summary(string id, string what, List<double> samples, double target, string unit = "ms")
    {
        samples.Sort();
        double p95 = samples[Math.Min(samples.Count - 1, (int)Math.Ceiling(samples.Count * 0.95) - 1)];
        string line = string.Create(CultureInfo.InvariantCulture,
            $"BENCH {id} {what}: n={samples.Count} median {samples[samples.Count / 2]:F1} {unit}, p95 {p95:F1} {unit}, max {samples[^1]:F1} {unit}; target {target:F0} {unit}");
        TestContext.Current.TestOutputHelper?.WriteLine(line);
        return line;
    }

    /// <summary>Pumps the UI thread until <paramref name="condition"/> holds. Each pump ends at Input priority, where user
    /// input runs, so the wait isn't starved by the app's own higher-priority work as a Background wait would be.</summary>
    private static void Pump(Func<bool> condition, string what, int seconds = 30)
    {
        var deadline = Stopwatch.StartNew();
        while (!condition())
        {
            Assert.True(deadline.Elapsed < TimeSpan.FromSeconds(seconds), $"Timed out waiting for {what}.");
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Input, () => frame.Continue = false);
            Dispatcher.PushFrame(frame);
        }
    }

    private static void WithWindow(Action<MainWindow, BuildRunViewModel, DocumentViewModel> body, Func<WorkspaceViewModel, BuildRunViewModel>? create = null)
    {
        StaTestThread.Run(() =>
        {
            WorkspaceViewModel workspace = BuildRunTests.Workspace();
            BuildRunViewModel buildRun = create is null ? BuildRunTests.Create(workspace) : create(workspace);
            var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace, buildRun: buildRun);
            var window = new MainWindow(shell, persistLayout: false) { Width = 1400, Height = 900 };
            DocumentViewModel document = workspace.New();
            try
            {
                window.Show();
                window.UpdateLayout();
                body(window, buildRun, document);
            }
            finally
            {
                document.MarkSaved("x.asm");
                window.Close();
            }
        });
    }

    /// <summary>Lets the UI thread finish layout and rendering of what the last change caused.</summary>
    private static void Render(MainWindow window)
    {
        window.UpdateLayout();
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded); // below Render: runs after the frame is laid out and rendered
    }

    /// <summary>How late input-priority work runs on the UI thread, posted every 50 ms from another thread for
    /// <paramref name="duration"/> while the UI thread keeps running (pumping) as usual.</summary>
    private static List<double> UiDelays(TimeSpan duration)
    {
        Dispatcher ui = Dispatcher.CurrentDispatcher;
        var delays = new List<double>();
        bool done = false;
        var sampler = new Thread(() =>
        {
            var total = Stopwatch.StartNew();
            while (total.Elapsed < duration)
            {
                var posted = Stopwatch.StartNew();
                ui.BeginInvoke(DispatcherPriority.Input, () => delays.Add(posted.Elapsed.TotalMilliseconds));
                Thread.Sleep(50);
            }

            Thread.Sleep(500);
            done = true;
        })
        { IsBackground = true };
        sampler.Start();
        Pump(() => Volatile.Read(ref done), "the sampling", seconds: (int)duration.TotalSeconds + 30);
        return delays;
    }

    [Fact]
    public void NFR_002_a_step_updates_every_visible_panel_within_100_ms_at_the_95th_percentile()
    {
        RequireBench();
        WithWindow((window, run, document) =>
        {
            document.Text = "org 100h\n" + string.Concat(Enumerable.Repeat("    inc ax\n    push ax\n    pop bx\n", 100)) + "    int 20h\n";
            run.BuildCommand.Execute(null);
            Pump(() => run.State == RunState.Ready, "Ready");
            Render(window);

            var samples = new List<double>();
            for (int i = 0; i < 200; i++)
            {
                ulong before = run.InstructionCount;
                var stopwatch = Stopwatch.StartNew();
                run.StepIntoCommand.Execute(null);
                Pump(() => run.State == RunState.Paused && run.InstructionCount == before + 1, "a step");
                Render(window); // registers, flags, memory, stack, strip, history, editor arrow
                samples.Add(stopwatch.Elapsed.TotalMilliseconds);
            }

            Summary("NFR-002", "step to all panels rendered", samples, 100);
            Assert.True(samples[(int)Math.Ceiling(samples.Count * 0.95) - 1] <= 100, "p95 above 100 ms");
        });
    }

    [Fact]
    public void NFR_004_and_AC_030_the_UI_stays_responsive_while_running_and_Pause_and_Stop_act_within_500_ms()
    {
        RequireBench();
        WithWindow((window, run, document) =>
        {
            document.Text = "org 100h\nagain:\n    inc ax\n    mov dl, 'x'\n    mov ah, 2\n    int 21h\n    jmp again\n"; // endless, with console output
            run.BuildCommand.Execute(null);
            Pump(() => run.State == RunState.Ready, "Ready");
            run.RunCommand.Execute(null);
            Pump(() => run.State == RunState.Running, "Running");

            List<double> delays = UiDelays(TimeSpan.FromSeconds(5));
            Summary("NFR-004", "UI thread delay while running (endless loop with output)", delays, 100);

            var pauses = new List<double>();
            var stops = new List<double>();
            for (int i = 0; i < 5; i++)
            {
                var pause = Stopwatch.StartNew();
                run.PauseCommand.Execute(null);
                Pump(() => run.State == RunState.Paused, "Paused");
                Render(window);
                pauses.Add(pause.Elapsed.TotalMilliseconds);
                run.RunCommand.Execute(null);
                Pump(() => run.State == RunState.Running, "Running again");
            }

            var stop = Stopwatch.StartNew();
            run.StopCommand.Execute(null);
            Render(window);
            stops.Add(stop.Elapsed.TotalMilliseconds);
            Summary("NFR-004", "Pause to paused and rendered", pauses, 500);
            Summary("NFR-004", "Stop to stopped and rendered", stops, 500);

            Assert.True(delays.Max() <= 100, $"UI thread delayed {delays.Max():F1} ms");
            Assert.True(pauses.Max() <= 500 && stops.Max() <= 500, "Pause or Stop above 500 ms");
        });
    }

    [Fact]
    public void NFR_004_the_UI_stays_responsive_while_the_assembler_hangs()
    {
        RequireBench();
        var assembler = new BuildRunTests.WaitingAssembler();
        WithWindow((window, buildRun, document) =>
        {
            document.Text = BuildRunTests.Hello;
            buildRun.BuildCommand.Execute(null);
            Pump(() => assembler.Started, "the hanging build");
            List<double> delays = UiDelays(TimeSpan.FromSeconds(3));
            Summary("NFR-004", "UI thread delay while the assembler hangs", delays, 100);
            var cancel = Stopwatch.StartNew();
            buildRun.CancelBuildCommand.Execute(null);
            Pump(() => buildRun.State == RunState.BuildFailed, "cancelled");
            Summary("NFR-004", "Cancel build to cancelled", [cancel.Elapsed.TotalMilliseconds], 500);
            Assert.True(delays.Max() <= 100, $"UI thread delayed {delays.Max():F1} ms");
        }, workspace => new BuildRunViewModel(workspace, () => assembler, build => SessionFactory.Create(build, 1000)));
    }

    [Fact]
    public void NFR_006_a_1000_line_build_shows_its_result_within_2_s()
    {
        RequireBench();
        WithWindow((window, run, document) =>
        {
            document.Text = ThousandLines();
            Assert.Equal(1001, document.Text.Split('\n').Length); // 1,000 lines and the final newline
            run.BuildCommand.Execute(null); // the first assembler launch is excluded (NFR-006)
            Pump(() => run.State == RunState.Ready, "the warm-up build");

            var samples = new List<double>();
            for (int i = 0; i < 5; i++)
            {
                document.Text += "; edit " + i.ToString(CultureInfo.InvariantCulture) + "\n"; // a real rebuild each time
                var stopwatch = Stopwatch.StartNew();
                run.BuildCommand.Execute(null);
                Pump(() => run.State == RunState.Ready, "the build result");
                Render(window); // the result shown: status bar, problems, panels
                samples.Add(stopwatch.Elapsed.TotalMilliseconds);
            }

            Summary("NFR-006", "1,000-line build to result shown", samples, 2000);
            Assert.True(samples.Max() <= 2000, "a build above 2 s");
        });
    }
}
