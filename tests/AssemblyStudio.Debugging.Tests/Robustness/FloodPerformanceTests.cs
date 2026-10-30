using System.Diagnostics;
using AssemblyStudio.Core;
using Xunit;
using static AssemblyStudio.Debugging.Tests.Robustness.RobustnessKit;

namespace AssemblyStudio.Debugging.Tests.Robustness;

/// <summary>
/// The DEF-005 regression (found by the soak, soak image 2825): <c>mov ah, 09h / int 21h / jnc back</c> printing a long
/// string writes megabytes between two snapshots. The console's delta buffer used to trim one byte at a time from the
/// front of a list, so each byte cost a 1 MiB shift once the cap was reached: the run never finished.
/// <para>
/// TASK-087 (DEF-006): the limit is on the CPU time this test process spends, not on wall-clock time, so other test
/// projects running at the same time can't make it fail; the class runs on its own within this assembly. Performance
/// tests carry the <c>Category=Performance</c> trait, which the instrumented coverage run leaves out (DEF-009).
/// </para>
/// </summary>
[Collection(nameof(RobustnessSequential))]
[Trait("Category", "Performance")]
public sealed class FloodPerformanceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    [Fact]
    public void A_flood_of_long_strings_reaches_the_budget_quickly()
    {
        byte[] code =
        [
            0xBA, 0x0B, 0x01, // 0100 mov dx, text
            0xB4, 0x09,       // 0103 mov ah, 09h
            0xCD, 0x21,       // 0105 int 21h
            0x73, 0xFC,       // 0107 jnc 0105
            0xEB, 0xFE,       // 0109 (not reached)
        ];
        byte[] image = [.. code, .. Enumerable.Repeat((byte)'x', 1_600), (byte)'$'];
        using var session = new DebugSession(() => Load(image), Disassembler) { InstructionBudget = 50_000 };
        WaitSettled(session, Timeout, s => s.PauseReason == PauseReason.Loaded);

        using Process process = Process.GetCurrentProcess();
        TimeSpan cpuBefore = process.TotalProcessorTime;
        var wall = Stopwatch.StartNew();
        MachineSnapshot settled = Settle(session, session.Run, Timeout);
        wall.Stop();
        process.Refresh();
        TimeSpan cpu = process.TotalProcessorTime - cpuBefore;

        TestContext.Current.TestOutputHelper?.WriteLine(
            $"~{25_000 * 1_600 / 1_000_000} million characters: {cpu.TotalSeconds:F2} s CPU, {wall.Elapsed.TotalSeconds:F2} s elapsed");
        Assert.Equal((SessionState.Paused, PauseReason.BudgetExhausted), (settled.State, session.PauseReason));
        Assert.True(cpu < TimeSpan.FromSeconds(10), $"Took {cpu.TotalSeconds:F2} s of CPU time ({wall.Elapsed.TotalSeconds:F2} s elapsed).");
    }
}
