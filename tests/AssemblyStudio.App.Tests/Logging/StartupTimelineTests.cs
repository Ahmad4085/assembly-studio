using System.Text.RegularExpressions;
using AssemblyStudio.App.Logging;
using Xunit;

namespace AssemblyStudio.App.Tests.Logging;

/// <summary>
/// TASK-108 (NFR-005, DEF-021): the startup timeline splits the time from process start to the first frame into the runtime's
/// part and each composition step, with the JIT time of each, in one log line.
/// </summary>
public sealed partial class StartupTimelineTests
{
    [Fact]
    public void Each_step_lasts_from_the_previous_mark_and_the_first_from_the_process_start()
    {
        var start = new DateTime(2026, 11, 2, 21, 0, 0, DateTimeKind.Local);
        var timeline = new StartupTimeline(start, start.AddMilliseconds(600));
        timeline.Mark("settings", start.AddMilliseconds(650));
        timeline.Mark("window", start.AddMilliseconds(1650));
        timeline.Mark("first frame", start.AddMilliseconds(2000));

        Assert.Equal([(StartupTimeline.Runtime, 600.0), ("settings", 50.0), ("window", 1000.0), ("first frame", 350.0)], timeline.Steps);
        Assert.Equal(4, timeline.JitMs.Count);
        Assert.All(timeline.JitMs, ms => Assert.True(ms >= 0));
    }

    [Fact]
    public void The_log_line_names_the_total_each_step_and_the_JIT_time()
    {
        var start = new DateTime(2026, 11, 2, 21, 0, 0, DateTimeKind.Local);
        var timeline = new StartupTimeline(start, start.AddMilliseconds(600));
        timeline.Mark("window", start.AddMilliseconds(1650));
        timeline.Mark("first frame", start.AddMilliseconds(2000));

        Assert.Matches(Line(), timeline.Describe());
    }

    [GeneratedRegex(@"^Startup 2000 ms to the first frame: runtime and WPF 600 \(JIT \d+\), window 1050 \(JIT \d+\), first frame 350 \(JIT \d+\); JIT \d+ ms in all$")]
    private static partial Regex Line();
}
