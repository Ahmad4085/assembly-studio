using System.Diagnostics;
using AssemblyStudio.Core;
using AssemblyStudio.Engine.I8086;
using AssemblyStudio.Engine.I8086.Disassembly;
using AssemblyStudio.Os.Dos;
using AssemblyStudio.Runtime;
using AssemblyStudio.Runtime.Console;
using AssemblyStudio.Runtime.Interrupts;
using AssemblyStudio.Runtime.Loading;
using AssemblyStudio.Runtime.Memory;
using Xunit;

namespace AssemblyStudio.Debugging.Tests;

/// <summary>
/// Test phase 2026-10-30 (gap fill): the session part of AC-024 (FR-049) and AC-026 (FR-054, FR-055, FR-056), checked on
/// the snapshots the panels will show. The panels themselves (TASK-080, TASK-082) aren't built yet, so the UI part of
/// these criteria remains not run.
/// </summary>
public sealed class AcceptanceStepTests
{
    private const ushort Psp = ComLoader.DefaultProgramSegment;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly IcedDisassembler Disassembler = new();

    private static Func<IMachine> Loader(byte[] image) => () =>
    {
        var memory = new RealModeMemory();
        var dos = new DosServices(Psp);
        var router = new InterruptRouter(memory, dos);
        var machine = new Machine(new Cpu8086(memory, router), memory, new ConsoleDevice(), router, dos);
        ComLoader.Load(machine, image);
        return machine;
    };

    private static MachineSnapshot StepOnce(DebugSession session)
    {
        ulong before = session.InstructionsExecuted;
        session.StepInto();
        var stopwatch = Stopwatch.StartNew();
        while (!(session.InstructionsExecuted == before + 1 && session.PauseReason == PauseReason.Step && session.LatestSnapshot is { State: SessionState.Paused }))
        {
            Assert.True(stopwatch.Elapsed < Timeout, "The step didn't complete.");
            Thread.Sleep(1);
        }

        return session.LatestSnapshot!.Value;
    }

    private static MachineSnapshot Loaded(DebugSession session)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!(session.PauseReason == PauseReason.Loaded && session.LatestSnapshot is { State: SessionState.Paused }))
        {
            Assert.True(stopwatch.Elapsed < Timeout, "The program didn't load.");
            Thread.Sleep(1);
        }

        return session.LatestSnapshot!.Value;
    }

    [Fact]
    public void AC_024_Step_Into_executes_exactly_one_instruction_and_moves_IP_and_the_current_instruction()
    {
        byte[] image =
        [
            0xB8, 0x34, 0x12, // 0100 mov ax, 1234h
            0xEB, 0x02,       // 0103 jmp short 0107
            0x90, 0x90,       // 0105 (skipped)
            0x40,             // 0107 inc ax
            0xCD, 0x20,       // 0108 int 20h
        ];
        using var session = new DebugSession(Loader(image), Disassembler);
        MachineSnapshot start = Loaded(session);
        Assert.Equal(0x0100, start.Cpu.IP);
        Assert.Equal((ushort)0x0100, start.CurrentInstruction?.Ip);

        MachineSnapshot first = StepOnce(session);
        Assert.Equal((ushort)0x1234, first.Cpu.AX);
        Assert.Equal(0x0103, first.Cpu.IP);                // the next instruction
        Assert.Equal((ushort)0x0103, first.CurrentInstruction?.Ip); // the current-instruction display follows
        Assert.Equal(1UL, session.InstructionsExecuted);

        MachineSnapshot jump = StepOnce(session);
        Assert.Equal(0x0107, jump.Cpu.IP);                 // the jump target
        Assert.Equal((ushort)0x1234, jump.Cpu.AX);          // nothing else ran
        Assert.Equal(2UL, session.InstructionsExecuted);

        MachineSnapshot inc = StepOnce(session);
        Assert.Equal((ushort)0x1235, inc.Cpu.AX);
        Assert.Equal(0x0108, inc.Cpu.IP);
        Assert.Equal(3UL, session.InstructionsExecuted);
    }

    [Fact]
    public void AC_026_after_push_ax_the_snapshot_has_SP_FFFCh_1234h_on_top_and_the_changed_bytes_34_12()
    {
        byte[] image =
        [
            0xB8, 0x34, 0x12, // 0100 mov ax, 1234h
            0x50,             // 0103 push ax
            0xCD, 0x20,       // 0104 int 20h
        ];
        using var session = new DebugSession(Loader(image), Disassembler);
        Loaded(session);
        MachineSnapshot before = StepOnce(session);
        Assert.Equal((ushort)0xFFFE, before.Cpu.SP);
        Assert.Equal((ushort)0x1234, before.Cpu.AX);
        int watch = session.WatchMemory(Psp, 0xFFFC, 4);

        MachineSnapshot after = StepOnce(session);

        Assert.Equal((ushort)0xFFFC, after.Cpu.SP);                       // FR-055: SP marked at FFFCh
        Assert.Equal((ushort)0x1234, after.Stack!.Value.Words[0]);         // the stack view's top word
        MemoryWindow window = Assert.Single(after.MemoryWindows!);       // FR-054: memory at SS:FFFC
        Assert.Equal(((ushort)Psp, (ushort)0xFFFC), (window.Segment, window.Offset));
        Assert.Equal([0x34, 0x12], window.Bytes.Take(2));
        int top = (Psp << 4) + 0xFFFC;                                    // FR-056: the changed bytes are reported
        Assert.Equal([top, top + 1], after.MemoryChanges.Select(c => c.PhysicalAddress).Order());
        session.UnwatchMemory(watch);
    }
}
