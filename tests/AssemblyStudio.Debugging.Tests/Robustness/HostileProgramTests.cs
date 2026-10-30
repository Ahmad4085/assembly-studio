using System.Collections.Concurrent;
using System.Diagnostics;
using AssemblyStudio.Core;
using AssemblyStudio.Runtime;
using AssemblyStudio.Runtime.Console;
using Xunit;
using static AssemblyStudio.Debugging.Tests.Robustness.RobustnessKit;

namespace AssemblyStudio.Debugging.Tests.Robustness;

/// <summary>
/// TASK-054, FR-096: output floods (AC-045 at session level), a destroyed interrupt vector table and self-modifying
/// code neither crash nor hang the session. The long-string flood timing check is in <see cref="FloodPerformanceTests"/>.
/// </summary>
public sealed class HostileProgramTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public void AC_045_an_output_flood_stays_within_the_console_cap_and_stop_still_works()
    {
        byte[] flood =
        [
            0xB2, 0x58,       // mov dl, 'X'
            0xB4, 0x02,       // mov ah, 02h
            0xCD, 0x21,       // int 21h
            0xEB, 0xFC,       // jmp back to the int
        ];
        ConsoleDevice? console = null;
        using var session = new DebugSession(() =>
        {
            Machine machine = Load(flood);
            console = (ConsoleDevice)machine.Console;
            return machine;
        }, Disassembler);
        var deltas = new ConcurrentQueue<int>();
        session.SnapshotPublished += (_, snapshot) => deltas.Enqueue(snapshot.ConsoleDelta.Length);
        WaitSettled(session, Timeout, s => s.PauseReason == PauseReason.Loaded);

        session.Run();
        var flooding = Stopwatch.StartNew();
        while (console!.TotalBytesWritten < 3 * ConsoleDevice.DefaultMaxCapacity / 2)
        {
            Assert.True(flooding.Elapsed < Timeout, $"Only {console.TotalBytesWritten} bytes written.");
            Thread.Sleep(10);
        }

        var stopping = Stopwatch.StartNew();
        Settle(session, session.StopSession, Timeout, s => s.State == SessionState.Idle);
        stopping.Stop();

        TestContext.Current.TestOutputHelper?.WriteLine(
            $"{console.TotalBytesWritten:N0} bytes written, {console.TotalBytesTrimmed:N0} trimmed, {deltas.Count} snapshots, largest delta {deltas.Max():N0} chars; Stop took {stopping.Elapsed.TotalMilliseconds:F1} ms");
        Assert.Equal(ConsoleDevice.DefaultMaxCapacity, console.BufferLength); // capped (NFR-015)
        Assert.True(console.HasTrimmed);                                      // oldest output trimmed
        Assert.All(deltas, d => Assert.True(d <= ConsoleDevice.DefaultMaxCapacity));
        Assert.True(stopping.Elapsed < TimeSpan.FromMilliseconds(500), $"Stop took {stopping.Elapsed}.");
    }

    [Fact]
    public void A_program_that_destroys_the_interrupt_vector_table_is_contained()
    {
        byte[] wipeIvt =
        [
            0x31, 0xFF,       // xor di, di
            0x8E, 0xC7,       // mov es, di
            0xB9, 0x00, 0x02, // mov cx, 512
            0xB8, 0xFF, 0xFF, // mov ax, 0FFFFh
            0xF3, 0xAB,       // rep stosw          ; every vector = FFFF:FFFF
            0xB4, 0x09,       // mov ah, 09h
            0xCD, 0x21,       // int 21h            ; vectors into garbage
        ];
        using var session = new DebugSession(() => Load(wipeIvt), Disassembler) { InstructionBudget = 200_000 };
        WaitSettled(session, Timeout, s => s.PauseReason == PauseReason.Loaded);

        MachineSnapshot settled = Settle(session, session.Run, Timeout);

        Assert.NotEqual(SessionState.Error, settled.State);
        Assert.Null(session.LastFault);
        Assert.Equal(0xFFFF, settled.Cpu.CS); // the INT 21h really took the garbage vector FFFF:FFFF
        Assert.Contains(settled.State, new[] { SessionState.Halted, SessionState.Paused, SessionState.Terminated, SessionState.WaitingForInput });
        TestContext.Current.TestOutputHelper?.WriteLine($"Ended {settled.State}/{session.PauseReason} at {settled.Cpu.CS:X4}:{settled.Cpu.IP:X4} after {session.InstructionsExecuted:N0} instructions");
    }

    [Fact]
    public void Self_modifying_code_executes_the_bytes_it_wrote()
    {
        byte[] selfModifying =
        [
            0xC6, 0x06, 0x07, 0x01, 0x40, // 0100 mov byte [0107h], 40h  ; turn the int3 below into inc ax
            0x40,                         // 0105 inc ax
            0x90,                         // 0106 nop
            0xCC,                         // 0107 int3 → inc ax
            0xB4, 0x4C,                   // 0108 mov ah, 4Ch
            0xCD, 0x21,                   // 010A int 21h         ; exit code = AL
        ];
        using var session = new DebugSession(() => Load(selfModifying), Disassembler);
        WaitSettled(session, Timeout, s => s.PauseReason == PauseReason.Loaded);

        MachineSnapshot ended = Settle(session, session.Run, Timeout);

        Assert.Equal(SessionState.Terminated, ended.State);
        Assert.Equal(2, ended.ExitCode);
        Assert.Contains(session.History, h => h.Ip == 0x0107 && h.Disassembly == "inc ax"); // the history shows what ran
    }
}
