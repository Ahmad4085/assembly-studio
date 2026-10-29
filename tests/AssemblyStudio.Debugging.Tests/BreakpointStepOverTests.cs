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
/// TASK-052: Breakpoints and Step Over (FR-050, FR-052, ADR-0007 §2).
/// Verifies AC-032 (breakpoints on looping programs) and AC-033 (step over procedures and early stops).
/// </summary>
public sealed class BreakpointStepOverTests
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

    [Fact]
    public void AC_032_breakpoint_pauses_before_line_on_every_loop_iteration_and_runs_to_end_when_removed()
    {
        // Program:
        // 0100: B9 03 00       mov cx, 3
        // 0103: 40             inc ax       <-- breakpoint on line 10 (offset 0103h)
        // 0104: E2 FD          loop 0103h
        // 0106: B8 00 4C       mov ax, 4C00h
        // 0109: CD 21          int 21h
        byte[] image =
        [
            0xB9, 0x03, 0x00,
            0x40,
            0xE2, 0xFD,
            0xB8, 0x00, 0x4C,
            0xCD, 0x21,
        ];

        string sourceFile = "D:/loop.asm";
        var sourceMap = new TestSourceMap(new Dictionary<(string?, int), ushort>
        {
            [(sourceFile, 5)] = 0x0100,
            [(sourceFile, 10)] = 0x0103,
            [(sourceFile, 15)] = 0x0104,
            [(sourceFile, 20)] = 0x0106,
        });

        using var session = new DebugSession(Loader(image), Disassembler, sourceMap);
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded);

        // Add breakpoint on line 10
        bool added = session.ToggleBreakpoint(sourceFile, 10);
        Assert.True(added);
        Assert.Single(session.Breakpoints);
        Assert.True(session.Breakpoints[0].IsBound);
        Assert.Equal((ushort)0x0103, session.Breakpoints[0].Offset);

        // Iteration 1: pauses BEFORE line 10 (inc ax)
        session.Run();
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Breakpoint);
        Assert.Equal((ushort)0x0103, session.LatestSnapshot!.Value.Cpu.IP);
        Assert.Equal((ushort)0x0000, session.LatestSnapshot!.Value.Cpu.AX); // not yet incremented
        Assert.Equal((ushort)3, session.LatestSnapshot!.Value.Cpu.CX);

        // Iteration 2: next loop iteration reaches line 10
        ulong prevExecuted = session.InstructionsExecuted;
        session.Run();
        WaitFor(session, s => s.InstructionsExecuted > prevExecuted && s.State == SessionState.Paused && s.PauseReason == PauseReason.Breakpoint);
        Assert.Equal((ushort)0x0103, session.LatestSnapshot!.Value.Cpu.IP);
        Assert.Equal((ushort)0x0001, session.LatestSnapshot!.Value.Cpu.AX); // incremented once
        Assert.Equal((ushort)2, session.LatestSnapshot!.Value.Cpu.CX);

        // Iteration 3: third iteration reaches line 10
        prevExecuted = session.InstructionsExecuted;
        session.Run();
        WaitFor(session, s => s.InstructionsExecuted > prevExecuted && s.State == SessionState.Paused && s.PauseReason == PauseReason.Breakpoint);
        Assert.Equal((ushort)0x0103, session.LatestSnapshot!.Value.Cpu.IP);
        Assert.Equal((ushort)0x0002, session.LatestSnapshot!.Value.Cpu.AX);
        Assert.Equal((ushort)1, session.LatestSnapshot!.Value.Cpu.CX);

        // Remove breakpoint: run continues to termination
        bool removed = session.RemoveBreakpoint(sourceFile, 10);
        Assert.True(removed);
        Assert.Empty(session.Breakpoints);

        session.Run();
        WaitFor(session, s => s.State == SessionState.Terminated);
        Assert.Equal((byte)0, session.LatestSnapshot!.Value.ExitCode);
        Assert.Equal(nameof(TerminationPath.Int21hFunction4Ch), session.LatestSnapshot!.Value.TerminationPath);
    }

    [Fact]
    public void AC_033_step_over_runs_through_procedure_and_pauses_at_instruction_after_call()
    {
        // Program:
        // 0100: E8 03 00       call 0106h   ; next instruction at 0103h
        // 0103: BB 34 12       mov bx, 1234h ; 3 bytes
        // 0106: ; procedure containing 20 instructions: 19 x nop (90h), 1 x ret (C3h)
        byte[] procedure = [.. Enumerable.Repeat((byte)0x90, 19), 0xC3];
        byte[] image =
        [
            0xE8, 0x03, 0x00,
            0xBB, 0x34, 0x12,
            .. procedure,
        ];

        Assert.Equal(20, procedure.Length);

        using var session = new DebugSession(Loader(image), Disassembler);
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded);

        // Current IP is at the CALL (0100h)
        Assert.Equal((ushort)0x0100, session.LatestSnapshot!.Value.Cpu.IP);

        // Step Over the call
        session.StepOver();
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Step);

        // Must pause at the instruction after the call (0103h)
        Assert.Equal((ushort)0x0103, session.LatestSnapshot!.Value.Cpu.IP);
        // 1 call + 19 nops + 1 ret = 21 instructions executed
        Assert.Equal((ulong)21, session.InstructionsExecuted);
    }

    [Fact]
    public void AC_033_step_over_stops_early_if_breakpoint_inside_procedure_hit()
    {
        // Program:
        // 0100: E8 03 00       call 0106h   ; next instruction at 0103h
        // 0103: BB 34 12       mov bx, 1234h
        // 0106: 19 x nop, 1 x ret
        byte[] procedure = [.. Enumerable.Repeat((byte)0x90, 19), 0xC3];
        byte[] image =
        [
            0xE8, 0x03, 0x00,
            0xBB, 0x34, 0x12,
            .. procedure,
        ];

        string sourceFile = "D:/proc.asm";
        // Let's set a breakpoint inside procedure at instruction index 5 (offset 0106 + 5 = 010Bh)
        ushort bpOffset = 0x010B;
        var sourceMap = new TestSourceMap(new Dictionary<(string?, int), ushort>
        {
            [(sourceFile, 1)] = 0x0100,
            [(sourceFile, 10)] = bpOffset,
        });

        using var session = new DebugSession(Loader(image), Disassembler, sourceMap);
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded);

        session.SetBreakpoint(sourceFile, 10);
        Assert.Single(session.Breakpoints);
        Assert.True(session.Breakpoints[0].IsBound);

        // Step Over the call
        session.StepOver();
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Breakpoint);

        // Stopped early at the breakpoint inside the procedure
        Assert.Equal(bpOffset, session.LatestSnapshot!.Value.Cpu.IP);
        Assert.Equal(PauseReason.Breakpoint, session.PauseReason);
    }

    [Fact]
    public void Step_over_on_int_instruction_pauses_at_next_instruction()
    {
        // Program:
        // 0100: B4 02          mov ah, 02h
        // 0102: B2 5A          mov dl, 'Z'
        // 0104: CD 21          int 21h      ; 2 bytes -> next instruction at 0106h
        // 0106: B8 00 4C       mov ax, 4C00h
        // 0109: CD 21          int 21h
        byte[] image =
        [
            0xB4, 0x02,
            0xB2, 0x5A,
            0xCD, 0x21,
            0xB8, 0x00, 0x4C,
            0xCD, 0x21,
        ];

        using var session = new DebugSession(Loader(image), Disassembler);
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded);

        // Step twice into int 21h
        session.StepInto();
        WaitFor(session, s => s.InstructionsExecuted == 1);
        session.StepInto();
        WaitFor(session, s => s.InstructionsExecuted == 2);
        Assert.Equal((ushort)0x0104, session.LatestSnapshot!.Value.Cpu.IP);

        // Step over int 21h
        session.StepOver();
        WaitFor(session, s => s.InstructionsExecuted == 3 && s.State == SessionState.Paused && s.PauseReason == PauseReason.Step);

        // Reached next instruction after int 21h
        Assert.Equal((ushort)0x0106, session.LatestSnapshot!.Value.Cpu.IP);
        Assert.Contains("Z", session.LatestSnapshot!.Value.ConsoleDelta);
    }

    [Fact]
    public void Step_over_on_non_call_instruction_steps_one_instruction()
    {
        byte[] image =
        [
            0xB8, 0x12, 0x34, // mov ax, 3412h
            0xBB, 0x56, 0x78, // mov bx, 7856h
        ];

        using var session = new DebugSession(Loader(image), Disassembler);
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded);

        session.StepOver();
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Step);

        Assert.Equal((ushort)0x0103, session.LatestSnapshot!.Value.Cpu.IP);
        Assert.Equal((ushort)0x3412, session.LatestSnapshot!.Value.Cpu.AX);
        Assert.Equal((ulong)1, session.InstructionsExecuted);
    }

    [Fact]
    public void Step_over_stops_early_on_termination()
    {
        // Procedure terminates rather than returning
        byte[] image =
        [
            0xE8, 0x03, 0x00, // call 0106h
            0x90, 0x90, 0x90,
            0xB8, 0x00, 0x4C, // mov ax, 4C00h
            0xCD, 0x21,       // int 21h
        ];

        using var session = new DebugSession(Loader(image), Disassembler);
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded);

        session.StepOver();
        WaitFor(session, s => s.State == SessionState.Terminated);
        Assert.Equal((byte)0, session.LatestSnapshot!.Value.ExitCode);
    }

    [Fact]
    public void Step_over_stops_early_on_runtime_error()
    {
        // Procedure divide by zero
        byte[] image =
        [
            0xE8, 0x03, 0x00, // call 0106h
            0x90, 0x90, 0x90,
            0xB3, 0x00,       // mov bl, 0
            0xF6, 0xF3,       // div bl
        ];

        using var session = new DebugSession(Loader(image), Disassembler);
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded);

        session.StepOver();
        WaitFor(session, s => s.State == SessionState.Halted);
        Assert.Equal(StepOutcome.Halted, session.LastStep?.Outcome);
        Assert.Equal(HaltReason.RuntimeError, session.LastStep?.HaltReason);
    }

    [Fact]
    public void Breakpoints_bound_and_unbound_state_and_rebind_after_rebuild()
    {
        byte[] image = [0x90, 0x90, 0x90];
        string file = "D:/test.asm";

        // Initial source map has no offset for line 10
        var map1 = new TestSourceMap(new Dictionary<(string?, int), ushort>
        {
            [(file, 1)] = 0x0100,
        });

        using var session = new DebugSession(Loader(image), Disassembler, map1);
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded);

        session.SetBreakpoint(file, 10);
        Assert.Single(session.Breakpoints);
        Assert.False(session.Breakpoints[0].IsBound);
        Assert.Null(session.Breakpoints[0].Offset);

        // Rebuild yields new source map where line 10 now generates bytes at 0102h
        var map2 = new TestSourceMap(new Dictionary<(string?, int), ushort>
        {
            [(file, 1)] = 0x0100,
            [(file, 10)] = 0x0102,
        });

        session.RebindBreakpoints(map2);
        Assert.Single(session.Breakpoints);
        Assert.True(session.Breakpoints[0].IsBound);
        Assert.Equal((ushort)0x0102, session.Breakpoints[0].Offset);
    }

    [Fact]
    public void Disabled_breakpoint_does_not_pause_execution()
    {
        byte[] image =
        [
            0xB8, 0x01, 0x00, // mov ax, 1
            0xBB, 0x02, 0x00, // mov bx, 2
            0xB8, 0x00, 0x4C, // mov ax, 4C00h
            0xCD, 0x21,       // int 21h
        ];

        string file = "D:/test.asm";
        var map = new TestSourceMap(new Dictionary<(string?, int), ushort>
        {
            [(file, 2)] = 0x0103, // mov bx, 2
        });

        using var session = new DebugSession(Loader(image), Disassembler, map);
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded);

        // Set disabled breakpoint
        session.SetBreakpoint(file, 2, enabled: false);
        Assert.False(session.Breakpoints[0].IsEnabled);

        session.Run();
        WaitFor(session, s => s.State == SessionState.Terminated);
        Assert.Equal((byte)0, session.LatestSnapshot!.Value.ExitCode);
    }

    [Fact]
    public void Breakpoints_persist_across_reset()
    {
        byte[] image =
        [
            0x40,             // inc ax
            0xB8, 0x00, 0x4C, // mov ax, 4C00h
            0xCD, 0x21,       // int 21h
        ];

        string file = "D:/test.asm";
        var map = new TestSourceMap(new Dictionary<(string?, int), ushort>
        {
            [(file, 1)] = 0x0100,
        });

        using var session = new DebugSession(Loader(image), Disassembler, map);
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded);

        session.SetBreakpoint(file, 1);
        Assert.Single(session.Breakpoints);

        // Run hits breakpoint
        session.Run();
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Breakpoint);

        // Reset
        session.Reset();
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded);

        // Breakpoints still exist!
        Assert.Single(session.Breakpoints);
        Assert.True(session.Breakpoints[0].IsBound);

        // Run hits breakpoint again
        session.Run();
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Breakpoint);
        Assert.Equal((ushort)0x0100, session.LatestSnapshot!.Value.Cpu.IP);
    }

    [Fact]
    public void Breakpoint_at_entry_point_pauses_before_first_instruction()
    {
        byte[] image =
        [
            0xB8, 0x12, 0x34, // mov ax, 3412h
            0xB8, 0x00, 0x4C, // mov ax, 4C00h
            0xCD, 0x21,       // int 21h
        ];

        string file = "D:/test.asm";
        var map = new TestSourceMap(new Dictionary<(string?, int), ushort>
        {
            [(file, 1)] = 0x0100,
        });

        using var session = new DebugSession(Loader(image), Disassembler, map);
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded);

        session.SetBreakpoint(file, 1);

        session.Run();
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Breakpoint);
        Assert.Equal((ushort)0x0100, session.LatestSnapshot!.Value.Cpu.IP);
        Assert.Equal((ulong)0, session.InstructionsExecuted);
        Assert.Equal((ushort)0x0000, session.LatestSnapshot!.Value.Cpu.AX);

        // Run again steps past entry point and completes
        session.Run();
        WaitFor(session, s => s.State == SessionState.Terminated);
    }

    [Fact]
    public void Breakpoint_toggle_and_remove_semantics()
    {
        using var session = new DebugSession(Loader([0x90]), Disassembler);
        WaitFor(session, s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded);

        Assert.Empty(session.Breakpoints);

        // Toggle on
        Assert.True(session.ToggleBreakpoint("a.asm", 5));
        Assert.Single(session.Breakpoints);

        // Toggle off
        Assert.False(session.ToggleBreakpoint("a.asm", 5));
        Assert.Empty(session.Breakpoints);

        // Remove non-existent returns false
        Assert.False(session.RemoveBreakpoint("a.asm", 5));
    }

    private sealed class TestSourceMap : ISourceMap
    {
        private readonly Dictionary<(string?, int), ushort> _lineOffsets;

        public TestSourceMap(Dictionary<(string?, int), ushort> lineOffsets)
        {
            _lineOffsets = lineOffsets;
        }

        public SourceMapping? Find(ushort offset)
        {
            var pair = _lineOffsets.FirstOrDefault(kv => kv.Value == offset);
            return pair.Value == offset ? new SourceMapping(new SourceLocation(pair.Key.Item1, pair.Key.Item2), false) : null;
        }

        public ushort? FirstOffsetOf(string? file, int line)
        {
            foreach (var ((f, l), off) in _lineOffsets)
            {
                if (l == line && (f == file || (f is not null && file is not null && Path.GetFullPath(f).Equals(Path.GetFullPath(file), StringComparison.OrdinalIgnoreCase))))
                {
                    return off;
                }
            }

            return null;
        }
    }
}
