using System.Collections.Concurrent;
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
/// TASK-051: snapshot content (ADR-0007 v1.1), checked against the live machine the session runs (NFR-018),
/// AC-048 and AC-049 at session level, the publish rate while running, and step latency (NFR-002).
/// </summary>
public sealed class SnapshotTests
{
    private const ushort Psp = ComLoader.DefaultProgramSegment;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly IcedDisassembler Disassembler = new();

    private static readonly byte[] CountLoop =
    [
        0xB9, 0x0A, 0x00, // 0100 mov cx, 10
        0x40,             // 0103 inc ax
        0xE2, 0xFD,       // 0104 loop 0103
        0xEB, 0xFE,       // 0106 jmp $
    ];

    private static readonly byte[] EndlessLoop = [0xEB, 0xFE];

    private static readonly byte[] IncrementForever = [0xFF, 0x06, 0x00, 0x02, 0xEB, 0xFA]; // inc word [0200h] / jmp

    /// <summary>Loads <paramref name="image"/> and remembers the machine, so tests can compare snapshots with it.</summary>
    private sealed class MachineSource(byte[] image)
    {
        public IMachine? Machine { get; private set; }

        public IMachine Load()
        {
            var memory = new RealModeMemory();
            var dos = new DosServices(Psp);
            var router = new InterruptRouter(memory, dos);
            Machine = new Machine(new Cpu8086(memory, router), memory, new ConsoleDevice(), router, dos);
            ComLoader.Load(Machine, image);
            return Machine;
        }
    }

    private static MachineSnapshot WaitFor(DebugSession session, Func<DebugSession, bool> condition)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition(session))
        {
            Assert.True(stopwatch.Elapsed < Timeout, $"Timed out in state {session.State}/{session.PauseReason}.");
            Thread.Sleep(1);
        }

        return session.LatestSnapshot!.Value;
    }

    private static bool PausedBy(DebugSession session, PauseReason reason) => session.State == SessionState.Paused && session.PauseReason == reason;

    private static MachineSnapshot StepAndWait(DebugSession session)
    {
        MachineSnapshot before = session.LatestSnapshot!.Value;
        session.StepInto();
        return WaitFor(session, s => PausedBy(s, PauseReason.Step) && !Equals(s.LatestSnapshot, before));
    }

    // ---- NFR-018: a paused snapshot equals the machine ----

    [Fact]
    public void A_paused_snapshot_equals_the_machine_state()
    {
        var source = new MachineSource(IncrementForever);
        using var session = new DebugSession(source.Load, Disassembler) { InstructionBudget = 999 };
        WaitFor(session, s => PausedBy(s, PauseReason.Loaded));
        session.WatchMemory(Psp, 0x0100, 16);
        session.WatchMemory(Psp, 0x01F8, 16);
        session.Run();
        MachineSnapshot snapshot = WaitFor(session, s => PausedBy(s, PauseReason.BudgetExhausted));

        IMachine machine = source.Machine!;
        CpuState cpu = machine.Cpu.State;
        Assert.Equal(cpu, snapshot.Cpu);
        Assert.Equal(2, snapshot.MemoryWindows!.Count);
        foreach (MemoryWindow window in snapshot.MemoryWindows)
        {
            for (int i = 0; i < window.Bytes.Count; i++)
            {
                Assert.Equal(machine.Memory.Read8(window.Segment, (ushort)(window.Offset + i)), window.Bytes[i]);
            }
        }

        Assert.Equal(0x01F4, machine.Memory.Read16(Psp, 0x0200)); // 500 increments in 999 instructions
        Assert.Equal(SnapshotWords(machine, cpu), snapshot.Stack!.Value.Words);
        CurrentInstruction current = snapshot.CurrentInstruction!.Value;
        Assert.Equal((cpu.CS, cpu.IP), (current.Cs, current.Ip));
        Assert.Equal(Disassembler.Decode([.. current.Bytes, 0, 0, 0, 0, 0], cpu.IP)!.Value.Text, current.Disassembly);
    }

    private static ushort[] SnapshotWords(IMachine machine, CpuState cpu) =>
        [.. Enumerable.Range(0, 16).Select(i => machine.Memory.Read16(cpu.SS, (ushort)(cpu.SP + (2 * i))))];

    [Fact]
    public void The_current_instruction_has_its_exact_bytes_and_real_branch_target()
    {
        var source = new MachineSource(CountLoop);
        using var session = new DebugSession(source.Load, Disassembler);
        WaitFor(session, s => PausedBy(s, PauseReason.Loaded));

        StepAndWait(session); // mov cx, 10
        MachineSnapshot atInc = StepAndWait(session); // inc ax, now at the loop

        CurrentInstruction loop = atInc.CurrentInstruction!.Value;
        Assert.Equal((ushort)0x0104, loop.Ip);
        Assert.Equal([0xE2, 0xFD], loop.Bytes);
        Assert.Equal("loop 0103h", loop.Disassembly); // decoded at its real IP
    }

    [Fact]
    public void The_current_instruction_carries_its_source_line_in_the_program_segment_only()
    {
        var map = new FakeSourceMap(new Dictionary<ushort, SourceMapping>
        {
            [0x0100] = new(new SourceLocation(@"C:\p\count.asm", 3), false),
            [0x0103] = new(new SourceLocation(@"C:\p\count.asm", 5), true),
        });
        var source = new MachineSource(CountLoop);
        using var session = new DebugSession(source.Load, Disassembler, map);

        CurrentInstruction loaded = WaitFor(session, s => PausedBy(s, PauseReason.Loaded)).CurrentInstruction!.Value;
        CurrentInstruction stepped = StepAndWait(session).CurrentInstruction!.Value;

        Assert.Equal((@"C:\p\count.asm", (int?)3, false), (loaded.SourcePath, loaded.SourceLine, loaded.FromMacroExpansion));
        Assert.Equal((@"C:\p\count.asm", (int?)5, true), (stepped.SourcePath, stepped.SourceLine, stepped.FromMacroExpansion));

        var unmapped = new DebugSession(source.Load, Disassembler);
        using (unmapped)
        {
            CurrentInstruction none = WaitFor(unmapped, s => PausedBy(s, PauseReason.Loaded)).CurrentInstruction!.Value;
            Assert.Null(none.SourceLine); // no source map: "no source mapping available" (FR-053)
        }
    }

    [Fact]
    public void Undefined_flags_of_the_last_instruction_are_reported()
    {
        byte[] mulThenNop = [0xF6, 0xE3, 0x90]; // mul bl (SF, ZF, AF, PF undefined) / nop
        using var session = new DebugSession(new MachineSource(mulThenNop).Load, Disassembler);
        WaitFor(session, s => PausedBy(s, PauseReason.Loaded));

        MachineSnapshot afterMul = StepAndWait(session);
        MachineSnapshot afterNop = StepAndWait(session);

        Assert.Equal(FlagBit.SF | FlagBit.ZF | FlagBit.AF | FlagBit.PF, afterMul.UndefinedFlags);
        Assert.Equal(FlagBit.None, afterNop.UndefinedFlags);
    }

    // ---- AC-049: execution history ----

    [Fact]
    public void AC_049_after_20_steps_the_history_lists_them_in_order_with_addresses_and_disassembly()
    {
        using var session = new DebugSession(new MachineSource(CountLoop).Load, Disassembler);
        WaitFor(session, s => PausedBy(s, PauseReason.Loaded));

        for (int i = 0; i < 20; i++)
        {
            MachineSnapshot step = StepAndWait(session);
            Assert.Single(step.HistoryDelta); // each step reports exactly its instruction
        }

        IReadOnlyList<HistoryEntry> history = session.History;
        Assert.Equal(20, history.Count);
        ushort[] expected = [0x0100, .. Enumerable.Range(0, 19).Select(i => (ushort)(i % 2 == 0 ? 0x0103 : 0x0104))];
        Assert.Equal(expected, history.Select(h => h.Ip));
        Assert.All(history, h => Assert.Equal(Psp, h.Cs));
        Assert.Equal("mov cx, 0ah", history[0].Disassembly);
        Assert.Equal("inc ax", history[1].Disassembly);
        Assert.Equal([0x40], history[1].Bytes);
    }

    [Fact]
    public void An_instruction_that_halts_is_not_in_the_history()
    {
        byte[] divideByZero = [0xB3, 0x00, 0xF6, 0xF3]; // mov bl, 0 / div bl
        using var session = new DebugSession(new MachineSource(divideByZero).Load, Disassembler);
        WaitFor(session, s => PausedBy(s, PauseReason.Loaded));

        session.Run();
        WaitFor(session, s => s.State == SessionState.Halted);

        HistoryEntry only = Assert.Single(session.History);
        Assert.Equal((ushort)0x0100, only.Ip);
        Assert.Equal(0x0102, session.LatestSnapshot!.Value.CurrentInstruction!.Value.Ip); // the div is the current instruction
    }

    [Fact]
    public void The_history_keeps_the_last_1000_instructions()
    {
        using var session = new DebugSession(new MachineSource(EndlessLoop).Load, Disassembler) { InstructionBudget = 5_000 };
        WaitFor(session, s => PausedBy(s, PauseReason.Loaded));

        session.Run();
        MachineSnapshot paused = WaitFor(session, s => PausedBy(s, PauseReason.BudgetExhausted));

        Assert.Equal(1_000, session.History.Count);
        Assert.True(paused.HistoryDelta.Count <= 1_000);
        Assert.All(session.History, h => Assert.Equal("jmp short 0100h", h.Disassembly));
    }

    // ---- AC-048 and the publish rate while running ----

    [Fact]
    public void AC_048_running_snapshots_are_marked_not_current_at_most_30_per_second_and_pause_refreshes()
    {
        using var session = new DebugSession(new MachineSource(EndlessLoop).Load, Disassembler);
        WaitFor(session, s => PausedBy(s, PauseReason.Loaded));
        var published = new ConcurrentQueue<(TimeSpan At, MachineSnapshot Snapshot)>();
        var clock = Stopwatch.StartNew();
        session.SnapshotPublished += (_, snapshot) => published.Enqueue((clock.Elapsed, snapshot));

        session.Run();
        Thread.Sleep(1_000);
        session.Pause();
        MachineSnapshot paused = WaitFor(session, s => PausedBy(s, PauseReason.UserPause));

        List<(TimeSpan At, MachineSnapshot Snapshot)> running = [.. published.Where(p => p.Snapshot.State == SessionState.Running)];
        Assert.All(running, p => Assert.Null(p.Snapshot.CurrentInstruction)); // "running - not current"
        TimeSpan span = running[^1].At - running[0].At;
        double perSecond = (running.Count - 1) / span.TotalSeconds;
        TestContext.Current.TestOutputHelper?.WriteLine($"{running.Count} running snapshots over {span.TotalMilliseconds:F0} ms = {perSecond:F1}/s");
        Assert.InRange(running.Count, 3, 40); // periodic; how many depends on the CPU the run gets (7 seen under full-suite load)
        Assert.True(perSecond <= 31, $"{perSecond:F1} snapshots per second while running.");
        Assert.NotNull(paused.CurrentInstruction); // refreshed on pause
    }

    // ---- WatchMemory ----

    [Fact]
    public void Watching_memory_while_paused_publishes_the_window_at_once()
    {
        using var session = new DebugSession(new MachineSource(CountLoop).Load, Disassembler);
        WaitFor(session, s => PausedBy(s, PauseReason.Loaded));

        int id = session.WatchMemory(Psp, 0x0100, 4);
        MachineSnapshot watched = WaitFor(session, s => s.LatestSnapshot?.MemoryWindows is { Count: 1 });

        Assert.Equal(new byte[] { 0xB9, 0x0A, 0x00, 0x40 }, watched.MemoryWindows![0].Bytes);
        session.UnwatchMemory(id);
        WaitFor(session, s => s.LatestSnapshot?.MemoryWindows is null);
    }

    [Fact]
    public void At_most_four_regions_of_at_most_4_KiB_can_be_watched()
    {
        using var session = new DebugSession(new MachineSource(CountLoop).Load, Disassembler);

        Assert.Throws<ArgumentOutOfRangeException>(() => session.WatchMemory(Psp, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.WatchMemory(Psp, 0, 4097));
        int[] ids = [.. Enumerable.Range(0, 4).Select(i => session.WatchMemory(Psp, (ushort)(i * 4096), 4096))];
        Assert.Throws<InvalidOperationException>(() => session.WatchMemory(Psp, 0, 1));

        session.UnwatchMemory(ids[0]);
        session.WatchMemory(Psp, 0, 1);
    }

    [Fact]
    public void A_published_window_does_not_change_when_execution_continues()
    {
        using var session = new DebugSession(new MachineSource(IncrementForever).Load, Disassembler);
        WaitFor(session, s => PausedBy(s, PauseReason.Loaded));
        session.WatchMemory(Psp, 0x0200, 2);
        MachineSnapshot before = WaitFor(session, s => s.LatestSnapshot?.MemoryWindows is { Count: 1 });

        StepAndWait(session); // inc word [0200h]
        MachineSnapshot after = StepAndWait(session);

        Assert.Equal(new byte[] { 0, 0 }, before.MemoryWindows![0].Bytes);
        Assert.Equal(new byte[] { 1, 0 }, after.MemoryWindows![0].Bytes);
    }

    // ---- NFR-002: step latency (session part) ----

    [Trait("Category", "Performance")]
    [Fact]
    public void Step_to_published_snapshot_takes_well_under_100_ms()
    {
        using var session = new DebugSession(new MachineSource(EndlessLoop).Load, Disassembler);
        WaitFor(session, s => PausedBy(s, PauseReason.Loaded));
        session.WatchMemory(Psp, 0x0100, 4096);
        WaitFor(session, s => s.LatestSnapshot?.MemoryWindows is { Count: 1 });

        // Timed with the publish event, not by polling (a 1 ms sleep rounds up to the ~15.6 ms Windows timer tick).
        using var published = new AutoResetEvent(false);
        session.SnapshotPublished += (_, _) => published.Set();
        var times = new List<double>();
        for (int i = 0; i < 200; i++)
        {
            published.Reset();
            var stopwatch = Stopwatch.StartNew();
            session.StepInto();
            Assert.True(published.WaitOne(Timeout));
            times.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        times.Sort();
        double p95 = times[(int)(times.Count * 0.95)];
        TestContext.Current.TestOutputHelper?.WriteLine($"Step → snapshot: median {times[100]:F2} ms, p95 {p95:F2} ms (NFR-002 budget 100 ms for all panels)");
        Assert.True(p95 < 100, $"p95 {p95:F2} ms");
    }

    private sealed class FakeSourceMap(IReadOnlyDictionary<ushort, SourceMapping> lines) : ISourceMap
    {
        public SourceMapping? Find(ushort offset) => lines.TryGetValue(offset, out SourceMapping m) ? m : null;

        public ushort? FirstOffsetOf(string? file, int line) => null;
    }
}
