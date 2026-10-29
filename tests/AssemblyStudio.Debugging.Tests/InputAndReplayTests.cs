using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
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
/// TASK-053: keyboard input and Ctrl-C delivered through the session (FR-064, D-015), and deterministic replay
/// (NFR-016, AC-047 at session level). Real machines with the DOS services of TASK-034..036.
/// </summary>
public sealed class InputAndReplayTests
{
    private const ushort Psp = ComLoader.DefaultProgramSegment;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly IcedDisassembler Disassembler = new();

    private static readonly byte[] EchoForever =
    [
        0xB4, 0x01,       // 0100 mov ah, 01h
        0xCD, 0x21,       // 0102 int 21h          ; read with echo
        0x88, 0xC2,       // 0104 mov dl, al
        0xB4, 0x02,       // 0106 mov ah, 02h
        0xCD, 0x21,       // 0108 int 21h          ; print it again
        0xEB, 0xF4,       // 010A jmp 0100
    ];

    private static readonly byte[] ReadUntilDot =
    [
        0xB4, 0x01,       // 0100 mov ah, 01h
        0xCD, 0x21,       // 0102 int 21h
        0x3C, 0x2E,       // 0104 cmp al, '.'
        0x75, 0xF8,       // 0106 jne 0100
        0xB8, 0x00, 0x4C, // 0108 mov ax, 4C00h
        0xCD, 0x21,       // 010B int 21h
    ];

    private static readonly byte[] BusyThenRead =
    [
        0xB9, 0xFF, 0xFF, // 0100 mov cx, 0FFFFh
        0xE2, 0xFE,       // 0103 loop 0103
        0xB4, 0x01,       // 0105 mov ah, 01h
        0xCD, 0x21,       // 0107 int 21h
        0xB4, 0x4C,       // 0109 mov ah, 4Ch     ; exit code = the key
        0xCD, 0x21,       // 010B int 21h
    ];

    private static byte[] ReadThenExitWithKey(byte function) =>
    [
        0xB4, function,   // mov ah, function
        0xCD, 0x21,       // int 21h
        0xB4, 0x4C,       // mov ah, 4Ch          ; exit code = AL
        0xCD, 0x21,       // int 21h
    ];

    private static Func<IMachine> Loader(byte[] image) => () =>
    {
        var memory = new RealModeMemory();
        var dos = new DosServices(Psp);
        var router = new InterruptRouter(memory, dos);
        var machine = new Machine(new Cpu8086(memory, router), memory, new ConsoleDevice(), router, dos);
        ComLoader.Load(machine, image);
        return machine;
    };

    /// <summary>A session plus everything it published (the console view appends every delta).</summary>
    private sealed class Observed : IDisposable
    {
        private readonly StringBuilder _console = new();

        public Observed(byte[] image)
        {
            Session = new DebugSession(Loader(image), Disassembler);
            Session.SnapshotPublished += (_, snapshot) =>
            {
                States.Enqueue(snapshot.State);
                lock (_console)
                {
                    _console.Append(snapshot.ConsoleDelta);
                }
            };
            WaitFor(s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded);
        }

        public DebugSession Session { get; }

        public ConcurrentQueue<SessionState> States { get; } = new();

        public string Console
        {
            get
            {
                lock (_console)
                {
                    return _console.ToString();
                }
            }
        }

        public MachineSnapshot WaitFor(Func<DebugSession, bool> condition)
        {
            var stopwatch = Stopwatch.StartNew();
            while (!condition(Session))
            {
                Assert.True(stopwatch.Elapsed < Timeout, $"Timed out in state {Session.State}/{Session.PauseReason}.");
                Thread.Sleep(1);
            }

            return Session.LatestSnapshot!.Value;
        }

        public void Dispose() => Session.Dispose();
    }

    private static byte[] Keys(string text) => Encoding.Latin1.GetBytes(text);

    // ---- Input reaches the program ----

    [Fact]
    public void A_run_waiting_for_input_continues_when_input_arrives()
    {
        using var observed = new Observed(EchoForever);

        observed.Session.Run();
        observed.WaitFor(s => s.State == SessionState.WaitingForInput);
        Assert.Equal((ushort)0x0102, observed.Session.LatestSnapshot!.Value.Cpu.IP); // on the INT

        observed.Session.SendInput(Keys("x"));
        observed.WaitFor(s => s.State == SessionState.WaitingForInput && s.InstructionsExecuted > 1);

        Assert.Equal("xx", observed.Console); // echoed, then printed again
        Assert.Equal([new InputRecord(1, (byte)'x')], observed.Session.RecordedInput); // arrived after mov ah, 01h
    }

    [Fact]
    public void A_step_waiting_for_input_completes_when_input_arrives()
    {
        using var observed = new Observed(EchoForever);
        observed.Session.StepInto(); // mov ah, 01h
        observed.WaitFor(s => s.PauseReason == PauseReason.Step);

        observed.Session.StepInto(); // int 21h: no input yet
        observed.WaitFor(s => s.State == SessionState.WaitingForInput);
        observed.Session.SendInput(Keys("a"));
        MachineSnapshot stepped = observed.WaitFor(s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Step && s.InstructionsExecuted == 2);

        Assert.Equal((ushort)0x0104, stepped.Cpu.IP);
        Assert.Equal((byte)'a', (byte)stepped.Cpu.AX);
        Assert.Equal("a", observed.Console);
    }

    [Fact]
    public void Input_sent_while_running_is_delivered_without_pausing_the_run()
    {
        using var observed = new Observed(BusyThenRead);

        observed.Session.Run();
        observed.Session.SendInput(Keys("k")); // the program is still in its 65,535-iteration loop
        MachineSnapshot ended = observed.WaitFor(s => s.State == SessionState.Terminated);

        Assert.Equal((int)'k', ended.ExitCode);
        Assert.DoesNotContain(SessionState.Paused, observed.States.Skip(1)); // never paused after loading
        Assert.DoesNotContain(SessionState.WaitingForInput, observed.States);
    }

    [Fact]
    public void Input_sent_while_paused_is_kept_for_the_program()
    {
        using var observed = new Observed(ReadThenExitWithKey(0x01));

        observed.Session.SendInput(Keys("z"));
        Thread.Sleep(50);
        Assert.Equal(PauseReason.Loaded, observed.Session.PauseReason); // input alone doesn't start the program

        observed.Session.Run();
        MachineSnapshot ended = observed.WaitFor(s => s.State == SessionState.Terminated);
        Assert.Equal((int)'z', ended.ExitCode);
    }

    // ---- Ctrl-C (D-015, TASK-036) ----

    [Fact]
    public void Ctrl_c_during_function_01h_terminates_with_the_ctrl_c_path()
    {
        using var observed = new Observed(ReadThenExitWithKey(0x01));
        observed.Session.Run();
        observed.WaitFor(s => s.State == SessionState.WaitingForInput);

        observed.Session.SendCtrlC();
        MachineSnapshot ended = observed.WaitFor(s => s.State == SessionState.Terminated);

        Assert.Equal(nameof(TerminationPath.CtrlC), ended.TerminationPath);
        Assert.Equal("^C\r\n", observed.Console);
        Assert.Equal([new InputRecord(1, 0x03)], observed.Session.RecordedInput);
    }

    [Fact]
    public void Ctrl_c_during_function_07h_is_an_ordinary_character()
    {
        using var observed = new Observed(ReadThenExitWithKey(0x07));
        observed.Session.Run();
        observed.WaitFor(s => s.State == SessionState.WaitingForInput);

        observed.Session.SendCtrlC();
        MachineSnapshot ended = observed.WaitFor(s => s.State == SessionState.Terminated);

        Assert.Equal(nameof(TerminationPath.Int21hFunction4Ch), ended.TerminationPath);
        Assert.Equal(0x03, ended.ExitCode);
        Assert.Equal(string.Empty, observed.Console);
    }

    // ---- AC-047 (session level): deterministic replay ----

    private sealed record RunResult(string Console, CpuState Cpu, ulong Instructions, IReadOnlyList<HistoryEntry> History, string? Termination);

    private static RunResult LiveRun(Observed observed)
    {
        observed.Session.Run();
        observed.WaitFor(s => s.State == SessionState.WaitingForInput);
        observed.Session.SendInput(Keys("ab"));
        observed.WaitFor(s => s.State == SessionState.WaitingForInput && s.RecordedInput.Count == 2 && s.InstructionsExecuted > 7);
        observed.Session.SendInput(Keys("c."));
        MachineSnapshot ended = observed.WaitFor(s => s.State == SessionState.Terminated);
        return new RunResult(observed.Console, ended.Cpu, observed.Session.InstructionsExecuted, observed.Session.History, ended.TerminationPath);
    }

    [Fact]
    public void AC_047_replaying_the_recorded_input_reproduces_the_run_exactly()
    {
        using var live = new Observed(ReadUntilDot);
        RunResult original = LiveRun(live);
        IReadOnlyList<InputRecord> recording = live.Session.RecordedInput;
        Assert.Equal(4, recording.Count);

        using var replay = new Observed(ReadUntilDot);
        replay.Session.Replay(recording);
        replay.WaitFor(s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded && s.RecordedInput.Count == 0);
        replay.Session.Run();
        MachineSnapshot ended = replay.WaitFor(s => s.State == SessionState.Terminated);

        Assert.Equal(original.Console, replay.Console);
        Assert.Equal(original.Cpu, ended.Cpu);
        Assert.Equal(original.Instructions, replay.Session.InstructionsExecuted);
        Assert.Equal(original.History, replay.Session.History, HistoryComparer.Instance);
        Assert.Equal(original.Termination, ended.TerminationPath);
        Assert.Equal(recording, replay.Session.RecordedInput);
        Assert.DoesNotContain(SessionState.WaitingForInput, replay.States); // every byte was there when the INT ran
    }

    [Fact]
    public void Replay_in_the_same_session_reproduces_the_run_and_ignores_live_input()
    {
        using var observed = new Observed(ReadUntilDot);
        RunResult original = LiveRun(observed);
        IReadOnlyList<InputRecord> recording = observed.Session.RecordedInput;

        observed.Session.Replay(recording);
        observed.WaitFor(s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded);
        observed.Session.SendInput(Keys("zzz.")); // ignored while replaying
        observed.Session.Run();
        MachineSnapshot ended = observed.WaitFor(s => s.State == SessionState.Terminated);

        Assert.Equal(original.Cpu, ended.Cpu);
        Assert.Equal(original.History, observed.Session.History, HistoryComparer.Instance);
        Assert.Equal(recording, observed.Session.RecordedInput);
    }

    [Fact]
    public void Reset_clears_the_recording()
    {
        using var observed = new Observed(ReadUntilDot);
        LiveRun(observed);

        observed.Session.Reset();
        observed.WaitFor(s => s.State == SessionState.Paused && s.PauseReason == PauseReason.Loaded && s.RecordedInput.Count == 0);
    }

    // ---- Regression: a refresh request must not pause a run (TASK-051) ----

    [Fact]
    public void Watching_memory_while_running_does_not_pause_the_run()
    {
        using var observed = new Observed([0xEB, 0xFE]); // jmp $
        observed.Session.Run();
        observed.WaitFor(s => s.State == SessionState.Running);

        observed.Session.WatchMemory(Psp, 0x0100, 2);
        MachineSnapshot running = observed.WaitFor(s => s.LatestSnapshot is { State: SessionState.Running, MemoryWindows.Count: 1 });

        Assert.Equal(new byte[] { 0xEB, 0xFE }, running.MemoryWindows![0].Bytes);
        Assert.DoesNotContain(SessionState.Paused, observed.States.Skip(1));
        observed.Session.Pause();
        observed.WaitFor(s => s.PauseReason == PauseReason.UserPause);
    }

    /// <summary>History entries hold byte lists; compare them by content.</summary>
    private sealed class HistoryComparer : IEqualityComparer<HistoryEntry>
    {
        public static readonly HistoryComparer Instance = new();

        public bool Equals(HistoryEntry x, HistoryEntry y) =>
            x.Cs == y.Cs && x.Ip == y.Ip && x.Disassembly == y.Disassembly && x.Bytes.SequenceEqual(y.Bytes);

        public int GetHashCode(HistoryEntry obj) => HashCode.Combine(obj.Cs, obj.Ip);
    }
}
