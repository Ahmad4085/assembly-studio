using System.Diagnostics;
using System.Globalization;

namespace AssemblyStudio.App.Logging;

/// <summary>
/// Where the startup time goes (NFR-005, TASK-108): the time from process start to the start of <c>OnStartup</c> (the .NET
/// runtime, WPF and <c>App.xaml</c>), then each composition step, then the first rendered frame. Written to the local log as
/// one line, e.g. "Startup 2344 ms to the first frame: runtime and WPF 910, log 12, settings 18, …, first frame 380".
/// </summary>
public sealed class StartupTimeline
{
    private readonly DateTime _processStart;
    private readonly List<(string Step, double EndMs, double JitMs)> _steps = [];

    /// <summary>Starts the timeline at the beginning of <c>OnStartup</c>.</summary>
    public StartupTimeline()
        : this(Process.GetCurrentProcess().StartTime, DateTime.Now)
    {
    }

    /// <summary>A timeline over given times (tests).</summary>
    internal StartupTimeline(DateTime processStart, DateTime now)
    {
        _processStart = processStart;
        Mark(Runtime, now);
    }

    /// <summary>The name of the first step: from process start to <c>OnStartup</c>.</summary>
    public const string Runtime = "runtime and WPF";

    /// <summary>Milliseconds from process start to now.</summary>
    public double ElapsedMs => (DateTime.Now - _processStart).TotalMilliseconds;

    /// <summary>The steps marked so far: name and duration in milliseconds.</summary>
    public IReadOnlyList<(string Step, double Ms)> Steps =>
        [.. _steps.Select((s, i) => (s.Step, s.EndMs - (i == 0 ? 0 : _steps[i - 1].EndMs)))];

    /// <summary>Of each step, the time the JIT compiler took (all threads; from <see cref="System.Runtime.JitInfo"/>).</summary>
    public IReadOnlyList<double> JitMs => [.. _steps.Select((s, i) => s.JitMs - (i == 0 ? 0 : _steps[i - 1].JitMs))];

    /// <summary>The step that just ended.</summary>
    public void Mark(string step) => Mark(step, DateTime.Now);

    /// <summary>The step that ended at <paramref name="now"/>.</summary>
    internal void Mark(string step, DateTime now) =>
        _steps.Add((step, (now - _processStart).TotalMilliseconds, System.Runtime.JitInfo.GetCompilationTime().TotalMilliseconds));

    /// <summary>"Startup 2344 ms to the first frame: runtime and WPF 910 (JIT 120), log 12 (JIT 3), …; JIT 1250 ms in all".</summary>
    public string Describe()
    {
        IReadOnlyList<(string Step, double Ms)> steps = Steps;
        IReadOnlyList<double> jit = JitMs;
        return string.Create(CultureInfo.InvariantCulture,
            $"Startup {(_steps.Count == 0 ? 0 : _steps[^1].EndMs):F0} ms to the first frame: {string.Join(", ", steps.Select((s, i) => $"{s.Step} {s.Ms:F0} (JIT {jit[i]:F0})"))}; JIT {(_steps.Count == 0 ? 0 : _steps[^1].JitMs):F0} ms in all");
    }
}
