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

namespace AssemblyStudio.Debugging.Tests.Robustness;

/// <summary>Shared pieces for the TASK-054 robustness tests: real machines and an event-driven wait.</summary>
internal static class RobustnessKit
{
    public const ushort Psp = ComLoader.DefaultProgramSegment;

    public static readonly IcedDisassembler Disassembler = new();

    /// <summary>A real machine (engine, runtime, DOS, console) with <paramref name="image"/> loaded.</summary>
    public static Machine Load(byte[] image)
    {
        var memory = new RealModeMemory();
        var dos = new DosServices(Psp);
        var router = new InterruptRouter(memory, dos);
        var machine = new Machine(new Cpu8086(memory, router), memory, new ConsoleDevice(), router, dos);
        ComLoader.Load(machine, image);
        return machine;
    }

    /// <summary>Waits until the session's latest snapshot matches <paramref name="settled"/> (already true counts).</summary>
    public static MachineSnapshot WaitSettled(DebugSession session, TimeSpan timeout, Func<DebugSession, bool> settled) =>
        Settle(session, null, timeout, settled);

    /// <summary>
    /// Sends <paramref name="command"/> and waits for a snapshot published <b>after</b> it that is no longer running
    /// and matches <paramref name="settled"/>. Subscribing before the command means the snapshot that was current
    /// before it (for example "loaded", which is not running either) can't be mistaken for the result. Event-driven,
    /// so thousands of short runs don't pay the ~15 ms timer tick of a polling sleep.
    /// </summary>
    public static MachineSnapshot Settle(DebugSession session, Action? command, TimeSpan timeout, Func<DebugSession, bool>? settled = null)
    {
        settled ??= s => s.State != SessionState.Running;
        using var signal = new SemaphoreSlim(0);
        int published = 0;
        void OnPublished(object? sender, MachineSnapshot snapshot)
        {
            Interlocked.Increment(ref published);
            signal.Release();
        }

        session.SnapshotPublished += OnPublished;
        try
        {
            command?.Invoke();
            var stopwatch = Stopwatch.StartNew();
            while (!((command is null || Volatile.Read(ref published) > 0) && session.LatestSnapshot is not null && settled(session)))
            {
                TimeSpan left = timeout - stopwatch.Elapsed;
                Assert.True(left > TimeSpan.Zero && signal.Wait(left), $"No settled snapshot within {timeout} (state {session.State}).");
            }

            return session.LatestSnapshot!.Value;
        }
        finally
        {
            session.SnapshotPublished -= OnPublished;
        }
    }
}
