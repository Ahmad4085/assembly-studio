using System.Diagnostics;
using AssemblyStudio.Core;
using AssemblyStudio.Engine.I8086;
using AssemblyStudio.Os.Dos;
using AssemblyStudio.Runtime;
using AssemblyStudio.Runtime.Console;
using AssemblyStudio.Runtime.Interrupts;
using AssemblyStudio.Runtime.Loading;
using AssemblyStudio.Runtime.Memory;
using Xunit;

namespace AssemblyStudio.Debugging.Tests;

/// <summary>
/// TASK-050: the debug session worker (ADR-0007 §2) driving real machines (engine, runtime, DOS) built from
/// hand-assembled .COM images. AC-027, AC-030 and AC-054 at session level.
/// </summary>
public sealed class DebugSessionTests
{
    private const ushort Psp = ComLoader.DefaultProgramSegment;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static readonly byte[] EndlessLoop = [0xEB, 0xFE]; // jmp $

    private static readonly byte[] PrintAThenLoop =
    [
        0xB2, 0x41,       // mov dl, 'A'
        0xB4, 0x02,       // mov ah, 02h
        0xCD, 0x21,       // int 21h
        0xEB, 0xFE,       // jmp $
    ];

    private static readonly byte[] Hello =
    [
        0xBA, 0x0C, 0x01, // mov dx, 010Ch
        0xB4, 0x09,       // mov ah, 09h
        0xCD, 0x21,       // int 21h
        0xB8, 0x00, 0x4C, // mov ax, 4C00h
        0xCD, 0x21,       // int 21h
        .. "Hi!\r\n$"u8.ToArray(),
    ];

    private static readonly byte[] IncrementForever =
    [
        0xFF, 0x06, 0x00, 0x02, // inc word [0200h]
        0xEB, 0xFA,             // jmp short -6
    ];

    private static readonly byte[] DivideByZero =
    [
        0xB3, 0x00, // mov bl, 0
        0xF6, 0xF3, // div bl
    ];

    /// <summary>A factory for real machines with <paramref name="image"/> loaded (as the composition root does).</summary>
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

    private static bool IsPaused(DebugSession session, PauseReason reason) =>
        session.LatestSnapshot is { State: SessionState.Paused } && session.State == SessionState.Paused && session.PauseReason == reason;

    // ---- Loading and stepping ----

    [Fact]
    public void A_new_session_loads_the_program_and_is_paused_at_the_entry_point()
    {
        using var session = new DebugSession(Loader(Hello));

        MachineSnapshot loaded = WaitFor(session, s => IsPaused(s, PauseReason.Loaded));

        Assert.Equal((Psp, (ushort)0x0100), (loaded.Cpu.CS, loaded.Cpu.IP));
        Assert.Equal(0UL, session.InstructionsExecuted);
        Assert.Empty(loaded.MemoryChanges);
        Assert.Equal(string.Empty, loaded.ConsoleDelta);
        Assert.Null(session.LastStep);
    }

    [Fact]
    public void Step_into_executes_exactly_one_instruction()
    {
        using var session = new DebugSession(Loader(Hello));
        WaitFor(session, s => IsPaused(s, PauseReason.Loaded));

        session.StepInto();
        MachineSnapshot stepped = WaitFor(session, s => IsPaused(s, PauseReason.Step));

        Assert.Equal(0x0103, stepped.Cpu.IP); // after mov dx, 010Ch
        Assert.Equal(0x010C, stepped.Cpu.DX);
        Assert.Equal(1UL, session.InstructionsExecuted);
        Assert.Equal(StepOutcome.Executed, session.LastStep?.Outcome);
    }

    [Fact]
    public void Run_executes_to_termination_and_publishes_output_and_exit_code()
    {
        using var session = new DebugSession(Loader(Hello));
        WaitFor(session, s => IsPaused(s, PauseReason.Loaded));

        session.Run();
        MachineSnapshot ended = WaitFor(session, s => s.LatestSnapshot is { State: SessionState.Terminated });

        Assert.Equal("Hi!\r\n", ended.ConsoleDelta);
        Assert.Equal(nameof(TerminationPath.Int21hFunction4Ch), ended.TerminationPath);
        Assert.Equal(0, ended.ExitCode);
        Assert.Equal(4UL, session.InstructionsExecuted); // the terminating INT doesn't complete
    }

    [Fact]
    public void A_runtime_error_halts_the_session_with_the_state_inspectable()
    {
        using var session = new DebugSession(Loader(DivideByZero));
        WaitFor(session, s => IsPaused(s, PauseReason.Loaded));

        session.Run();
        MachineSnapshot halted = WaitFor(session, s => s.LatestSnapshot is { State: SessionState.Halted });

        Assert.Equal(HaltReason.RuntimeError, session.LastStep?.HaltReason);
        Assert.Equal(0x0102, halted.Cpu.IP); // on the div

        session.Run(); // nothing to continue: stays halted
        session.StepInto();
        Thread.Sleep(50);
        Assert.Equal(SessionState.Halted, session.State);
    }

    // ---- AC-027: pause, stop, reset ----

    [Fact]
    public void AC_027_pause_shows_the_state_stop_ends_the_session_and_reset_runs_like_the_first_time()
    {
        using var session = new DebugSession(Loader(PrintAThenLoop));
        MachineSnapshot loaded = WaitFor(session, s => IsPaused(s, PauseReason.Loaded));

        session.Run();
        WaitFor(session, s => s.InstructionsExecuted >= 10); // past the print, inside the loop
        session.Pause();
        MachineSnapshot paused = WaitFor(session, s => IsPaused(s, PauseReason.UserPause));

        Assert.Equal(0x0106, paused.Cpu.IP); // in the loop
        Assert.Equal("A", paused.ConsoleDelta);

        session.StopSession();
        WaitFor(session, s => s.LatestSnapshot is { State: SessionState.Idle });

        session.Reset();
        MachineSnapshot reset = WaitFor(session, s => IsPaused(s, PauseReason.Loaded));
        Assert.Equal(loaded.Cpu, reset.Cpu);              // initial registers restored
        Assert.Equal(string.Empty, reset.ConsoleDelta);  // console cleared
        Assert.Equal(0UL, session.InstructionsExecuted);

        session.Run();
        WaitFor(session, s => s.InstructionsExecuted >= 10);
        session.Pause();
        MachineSnapshot again = WaitFor(session, s => IsPaused(s, PauseReason.UserPause));
        Assert.Equal(("A", paused.Cpu.IP), (again.ConsoleDelta, again.Cpu.IP)); // the next run behaves like the first
    }

    // ---- AC-030: runaway program ----

    [Fact]
    public void AC_030_commands_return_at_once_while_an_endless_loop_runs()
    {
        using var session = new DebugSession(Loader(EndlessLoop));
        WaitFor(session, s => IsPaused(s, PauseReason.Loaded));

        var stopwatch = Stopwatch.StartNew();
        session.Run();
        session.Pause();
        session.Run();
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(100), $"Posting commands took {stopwatch.Elapsed}.");
        session.Pause();
        WaitFor(session, s => IsPaused(s, PauseReason.UserPause));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AC_030_pause_and_stop_take_effect_within_500_ms(bool stop)
    {
        using var session = new DebugSession(Loader(EndlessLoop));
        WaitFor(session, s => IsPaused(s, PauseReason.Loaded));
        session.Run();
        WaitFor(session, s => s.State == SessionState.Running);
        Thread.Sleep(100); // let it run

        var stopwatch = Stopwatch.StartNew();
        if (stop)
        {
            session.StopSession();
            WaitFor(session, s => s.LatestSnapshot is { State: SessionState.Idle });
        }
        else
        {
            session.Pause();
            WaitFor(session, s => IsPaused(s, PauseReason.UserPause));
        }

        stopwatch.Stop();
        TestContext.Current.TestOutputHelper?.WriteLine($"{(stop ? "Stop" : "Pause")} took effect in {stopwatch.Elapsed.TotalMilliseconds:F2} ms (NFR-004: 500 ms)");
        Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(500), $"Took {stopwatch.Elapsed}.");
        Assert.True(session.InstructionsExecuted > 0);
    }

    [Fact]
    public void AC_030_an_endless_loop_pauses_at_the_budget_and_run_continues_with_a_new_budget()
    {
        using var session = new DebugSession(Loader(EndlessLoop)) { InstructionBudget = 200_000 };
        WaitFor(session, s => IsPaused(s, PauseReason.Loaded));

        session.Run();
        WaitFor(session, s => IsPaused(s, PauseReason.BudgetExhausted));
        Assert.Equal(200_000UL, session.InstructionsExecuted);

        session.Run(); // continue
        WaitFor(session, s => IsPaused(s, PauseReason.BudgetExhausted) && s.InstructionsExecuted == 400_000);
    }

    [Fact]
    public void The_default_budget_is_10_million()
    {
        using var session = new DebugSession(Loader(EndlessLoop));

        Assert.Equal(10_000_000UL, session.InstructionBudget);
        Assert.Throws<ArgumentOutOfRangeException>(() => session.InstructionBudget = 0);
    }

    // ---- AC-054 (session level): the budget setting is honored ----

    [Fact]
    public void AC_054_a_budget_of_1000_pauses_after_exactly_1000_instructions()
    {
        using var session = new DebugSession(Loader(EndlessLoop)) { InstructionBudget = 1_000 };
        WaitFor(session, s => IsPaused(s, PauseReason.Loaded));

        session.Run();
        WaitFor(session, s => IsPaused(s, PauseReason.BudgetExhausted));

        Assert.Equal(1_000UL, session.InstructionsExecuted);
    }

    // ---- Snapshot content and limits ----

    [Fact]
    public void Memory_changes_of_a_long_run_are_reported_once_per_byte_with_the_original_value()
    {
        using var session = new DebugSession(Loader(IncrementForever)) { InstructionBudget = 300_001 }; // odd: the next instruction is the jmp
        WaitFor(session, s => IsPaused(s, PauseReason.Loaded));

        session.Run();
        MachineSnapshot paused = WaitFor(session, s => IsPaused(s, PauseReason.BudgetExhausted));

        int word = (Psp << 4) + 0x0200;
        Assert.Equal([new MemoryChange(word, 0x00), new MemoryChange(word + 1, 0x00)], paused.MemoryChanges);

        session.StepInto();
        MachineSnapshot stepped = WaitFor(session, s => IsPaused(s, PauseReason.Step));
        Assert.Empty(stepped.MemoryChanges); // the next instruction is the jmp: only changes since the last snapshot
    }

    // ---- Faults (NFR-010) ----

    [Fact]
    public void An_engine_fault_becomes_a_session_error_and_reset_recovers()
    {
        bool broken = true;
        Func<IMachine> load = Loader(Hello);
        using var session = new DebugSession(() => broken ? new FaultyMachine(load()) : load());
        WaitFor(session, s => IsPaused(s, PauseReason.Loaded));

        session.StepInto();
        WaitFor(session, s => s.LatestSnapshot is { State: SessionState.Error });

        broken = false;
        session.Reset();
        WaitFor(session, s => IsPaused(s, PauseReason.Loaded));
        session.Run();
        WaitFor(session, s => s.LatestSnapshot is { State: SessionState.Terminated });
    }

    [Fact]
    public void A_loader_fault_becomes_a_session_error()
    {
        using var session = new DebugSession(() => throw new InvalidOperationException("image too large"));

        WaitFor(session, s => s.LatestSnapshot is { State: SessionState.Error });
    }

    [Fact]
    public void Dispose_interrupts_a_run()
    {
        var session = new DebugSession(Loader(EndlessLoop));
        WaitFor(session, s => IsPaused(s, PauseReason.Loaded));
        session.Run();
        WaitFor(session, s => s.State == SessionState.Running);

        var stopwatch = Stopwatch.StartNew();
        session.Dispose();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"Dispose took {stopwatch.Elapsed}.");
        Assert.Throws<ObjectDisposedException>(session.Run);
    }

    /// <summary>A machine whose CPU throws, standing in for an engine bug.</summary>
    private sealed class FaultyMachine(IMachine inner) : IMachine
    {
        public ICpu Cpu { get; } = new ThrowingCpu(inner.Cpu);

        public IMemory Memory => inner.Memory;

        public IConsoleDevice Console => inner.Console;

        public IInterruptRouter? InterruptRouter => inner.InterruptRouter;

        private sealed class ThrowingCpu(ICpu inner) : ICpu
        {
            public CpuState State => inner.State;

            public void SetState(CpuState state) => inner.SetState(state);

            public StepResult StepInstruction() => throw new InvalidOperationException("engine bug");
        }
    }
}
