using System.Collections.Concurrent;
using AssemblyStudio.Core;
using Xunit;
using static AssemblyStudio.Debugging.Tests.Robustness.RobustnessKit;

namespace AssemblyStudio.Debugging.Tests.Robustness;

/// <summary>
/// TASK-054, NFR-010: faults of the engine, the loader, the machine and snapshot subscribers become a session error
/// (or are only logged) instead of ending the process; the session stays usable.
/// </summary>
public sealed class FaultContainmentTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly byte[] Hello = [0xB4, 0x02, 0xB2, 0x41, 0xCD, 0x21, 0xB8, 0x00, 0x4C, 0xCD, 0x21]; // print 'A', exit 0

    [Fact]
    public void An_engine_exception_becomes_an_error_with_the_fault_logged_and_reset_clears_it()
    {
        var logged = new ConcurrentQueue<Exception>();
        bool broken = true;
        using var session = new DebugSession(
            () => broken ? new Faulty(Load(Hello), cpuThrows: true) : Load(Hello), Disassembler, faultLog: logged.Enqueue);
        WaitSettled(session, Timeout, s => s.PauseReason == PauseReason.Loaded);

        session.Run();
        WaitSettled(session, Timeout, s => s.State == SessionState.Error);

        Assert.Equal("InvalidOperationException: engine bug", session.LastFault);
        Exception fault = Assert.Single(logged);
        Assert.Equal("engine bug", fault.Message);

        broken = false;
        session.Reset();
        WaitSettled(session, Timeout, s => s.PauseReason == PauseReason.Loaded);
        Assert.Null(session.LastFault);
        session.Run();
        WaitSettled(session, Timeout, s => s.State == SessionState.Terminated);
    }

    [Fact]
    public void A_machine_that_fails_while_the_error_snapshot_is_built_still_ends_in_an_error()
    {
        var logged = new ConcurrentQueue<Exception>();
        bool broken = true;
        using var session = new DebugSession(
            () => broken ? new Faulty(Load(Hello), cpuThrows: true, memoryThrows: true) : Load(Hello), Disassembler, faultLog: logged.Enqueue);

        MachineSnapshot error = WaitSettled(session, Timeout, s => s.State == SessionState.Error); // the loaded snapshot itself fails

        Assert.Equal(default, error.Cpu); // published without the faulty machine
        Assert.NotEmpty(logged);
        broken = false;
        session.Reset();
        WaitSettled(session, Timeout, s => s.PauseReason == PauseReason.Loaded);
    }

    [Fact]
    public void A_throwing_snapshot_subscriber_is_logged_and_others_still_receive_snapshots()
    {
        var logged = new ConcurrentQueue<Exception>();
        var received = new ConcurrentQueue<SessionState>();
        using var session = new DebugSession(() => Load(Hello), Disassembler, faultLog: logged.Enqueue);
        session.SnapshotPublished += (_, _) => throw new InvalidOperationException("view model bug");
        session.SnapshotPublished += (_, snapshot) => received.Enqueue(snapshot.State);

        session.Reset();
        WaitSettled(session, Timeout, s => s.PauseReason == PauseReason.Loaded);
        session.Run();
        WaitSettled(session, Timeout, s => s.State == SessionState.Terminated);

        Assert.Contains(SessionState.Terminated, received);
        Assert.Null(session.LastFault);
        Assert.Contains(logged, e => e.Message == "view model bug");
    }

    [Fact]
    public void A_throwing_fault_log_does_not_take_the_session_down()
    {
        using var session = new DebugSession(
            () => new Faulty(Load(Hello), cpuThrows: true), Disassembler, faultLog: _ => throw new IOException("disk full"));
        WaitSettled(session, Timeout, s => s.PauseReason == PauseReason.Loaded);

        session.StepInto();
        WaitSettled(session, Timeout, s => s.State == SessionState.Error);
        session.Reset();
        WaitSettled(session, Timeout, s => s.PauseReason == PauseReason.Loaded);
    }

    /// <summary>A machine whose CPU and/or memory throw, standing in for engine and runtime bugs.</summary>
    private sealed class Faulty(IMachine inner, bool cpuThrows, bool memoryThrows = false) : IMachine
    {
        public ICpu Cpu { get; } = cpuThrows ? new ThrowingCpu(inner.Cpu) : inner.Cpu;

        public IMemory Memory { get; } = memoryThrows ? new ThrowingMemory() : inner.Memory;

        public IConsoleDevice Console => inner.Console;

        public IInterruptRouter? InterruptRouter => inner.InterruptRouter;

        private sealed class ThrowingCpu(ICpu cpu) : ICpu
        {
            public CpuState State => cpu.State;

            public void SetState(CpuState state) => cpu.SetState(state);

            public StepResult StepInstruction() => throw new InvalidOperationException("engine bug");
        }

        private sealed class ThrowingMemory : IMemory
        {
            public byte Read8(ushort segment, ushort offset) => throw new InvalidOperationException("memory bug");

            public ushort Read16(ushort segment, ushort offset) => throw new InvalidOperationException("memory bug");

            public void Write8(ushort segment, ushort offset, byte value) => throw new InvalidOperationException("memory bug");

            public void Write16(ushort segment, ushort offset, ushort value) => throw new InvalidOperationException("memory bug");

            public IReadOnlyList<MemoryChange> TakeJournal() => [];
        }
    }
}
