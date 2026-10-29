using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.Core.Tests;

/// <summary>MachineSnapshot immutability: a published snapshot never changes when the machine moves on.</summary>
public class MachineSnapshotTests
{
    [Fact]
    public void Snapshot_cpu_state_survives_cpu_advancing()
    {
        // Simulates: snapshot published, then the CPU (or its view model) computes a new state.
        CpuState atSnapshot = new(0xB804, 0, 0, 0, 0xFFFE, 0, 0, 0, 0x0800, 0x0800, 0x0800, 0x0800, 0x0100, 0x0202);
        var snapshot = new MachineSnapshot(
            atSnapshot, FlagBit.None, [], "", SessionState.Paused,
            null, null, null, null, null, []);

        CpuState later = atSnapshot with { AX = 0x0000, IP = 0x0103, Flags = 0x0246 };
        Assert.NotEqual(atSnapshot, later);

        Assert.Equal(0xB804, snapshot.Cpu.AX);
        Assert.Equal(0x0100, snapshot.Cpu.IP);
        Assert.Equal(0x0202, snapshot.Cpu.Flags);
    }

    [Fact]
    public void Equal_snapshots_are_equal()
    {
        var a = new MachineSnapshot(default, FlagBit.ZF, [new MemoryChange(0x100, 0x00)], "hi", SessionState.Paused, null, null, null, null, null, [new HistoryEntry(0x0800, 0x0100, [0x90], "nop")]);
        var b = a with { };

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void With_expression_produces_a_new_snapshot()
    {
        var initial = new MachineSnapshot(default, FlagBit.None, [], "", SessionState.Running, null, null, null, null, null, []);

        var after = initial with { State = SessionState.Paused, HistoryDelta = [new HistoryEntry(0x0800, 0x0100, [0x90], "nop")] };

        Assert.Equal(SessionState.Paused, after.State);
        Assert.Single(after.HistoryDelta);
        Assert.Equal(SessionState.Running, initial.State);
        Assert.Empty(initial.HistoryDelta);
    }

    [Fact]
    public void Snapshot_holds_the_v1_1_fields_as_types()
    {
        var current = new CurrentInstruction(0x0800, 0x0100, [0xB8, 0x04, 0x00], "mov ax, 0x0004", "prog.asm", 3, false);
        var windows = new List<MemoryWindow> { new(0x0800, 0x0100, [1, 2, 3]) };
        var stack = new StackWindow([0x0000, 0x1234]);

        var snapshot = new MachineSnapshot(
            default, FlagBit.CF, [], "A>", SessionState.Halted,
            "unsupported instruction", null, current, windows, stack, []);

        Assert.True(snapshot.CurrentInstruction.HasValue);
        Assert.Equal("mov ax, 0x0004", snapshot.CurrentInstruction!.Value.Disassembly);
        Assert.True(snapshot.CurrentInstruction.Value.Bytes.Count == 3);
        Assert.Single(snapshot.MemoryWindows!);
        Assert.Equal(2, snapshot.Stack!.Value.Words.Count);
        Assert.Equal(FlagBit.CF, snapshot.UndefinedFlags);
        Assert.Equal("unsupported instruction", snapshot.TerminationPath);
    }
}
