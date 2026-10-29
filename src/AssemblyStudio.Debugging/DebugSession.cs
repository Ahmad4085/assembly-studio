using System.Diagnostics;
using System.Threading.Channels;
using AssemblyStudio.Core;
using AssemblyStudio.Debugging.Snapshots;

namespace AssemblyStudio.Debugging;

/// <summary>
/// The debug session (C07, ADR-0007 §2). One dedicated background thread owns the machine; commands arrive
/// through a channel and are processed in order. A run executes instructions until the program terminates,
/// halts or waits for input, a command arrives (checked before every instruction), or the run's instruction
/// budget is used up (FR-042, FR-051, FR-067, NFR-004).
/// <para>
/// The session doesn't know how a machine is built or loaded: it receives a factory that returns a freshly
/// loaded machine (the composition root wires engine, runtime and loader, ADR-0002). Reset calls the factory
/// again, so the next run behaves like the first (FR-035). An exception from the engine or the factory
/// becomes <see cref="SessionState.Error"/>; the session stays usable (NFR-010).
/// </para>
/// <para>
/// Snapshots (ADR-0007 §2 and v1.1) are built on the worker from the live machine as immutable copies (NFR-018):
/// CPU state, undefined-flag mask, memory changes since the last snapshot (one entry per changed byte, so a long
/// run can't grow them without limit, NFR-015), console output since the last snapshot, session state,
/// termination, the current instruction with its source line, the subscribed memory windows, the stack window
/// and the history delta. One is published immediately after each command (step, pause, stop, reset) and at
/// most 30 per second while running; running snapshots have no current instruction ("not current", FR-057).
/// </para>
/// </summary>
public sealed class DebugSession : IDebugSession
{
    /// <summary>The default instruction budget per run (FR-042).</summary>
    public const ulong DefaultInstructionBudget = 10_000_000;

    /// <summary>The most memory regions a session can watch (ADR-0007 v1.1).</summary>
    public const int MaxMemoryWatches = 4;

    /// <summary>The largest watched region, in bytes (ADR-0007 v1.1).</summary>
    public const int MaxMemoryWatchLength = 4096;

    private const int JournalDrainInterval = 65_536;
    private const int RunningCheckInterval = 1_024;
    private static readonly TimeSpan RunningPublishInterval = TimeSpan.FromMilliseconds(1000.0 / 30);

    private readonly Func<IMachine> _loadMachine;
    private readonly Channel<Command> _commands = Channel.CreateUnbounded<Command>(new UnboundedChannelOptions { SingleReader = true });
    private readonly Thread _worker;
    private readonly Dictionary<int, byte> _changedBytes = [];
    private readonly Lock _snapshotLock = new();
    private readonly IDisassembler _disassembler;
    private readonly ISourceMap? _sourceMap;
    private readonly ExecutionHistory _history = new();
    private readonly List<MemoryWatch> _watches = [];
    private readonly Lock _watchLock = new();
    private int _nextWatchId;
    private ushort _programSegment;
    private IReadOnlyList<HistoryEntry> _historyView = [];

    private IMachine? _machine;
    private int _pendingCommands;
    private SessionState _state = SessionState.Idle; // worker-only; readers use the published copy
    private PauseReason _pauseReason = PauseReason.None;
    private long _instructionsExecuted;
    private long _budget = (long)DefaultInstructionBudget;
    private StepResult? _lastStep;
    private Published _published = new(null, PauseReason.None, null, []);
    private bool _disposed;

    /// <summary>Creates the session and loads the program: the state becomes <see cref="SessionState.Paused"/>
    /// with <see cref="PauseReason.Loaded"/> once the worker has loaded it.</summary>
    /// <param name="loadMachine">Returns a new machine with the program loaded and the CPU at its entry point; the
    /// entry CS is taken as the program segment the source map refers to.</param>
    /// <param name="disassembler">Decodes the current instruction and the history (FR-053, FR-058).</param>
    /// <param name="sourceMap">The build's source map, or <see langword="null"/> when there is none (FR-053).</param>
    public DebugSession(Func<IMachine> loadMachine, IDisassembler disassembler, ISourceMap? sourceMap = null)
    {
        ArgumentNullException.ThrowIfNull(loadMachine);
        ArgumentNullException.ThrowIfNull(disassembler);
        _loadMachine = loadMachine;
        _disassembler = disassembler;
        _sourceMap = sourceMap;
        _worker = new Thread(WorkerLoop) { IsBackground = true, Name = "Assembly Studio debug session" };
        _worker.Start();
        Post(CommandKind.Reset);
    }

    /// <inheritdoc />
    public event EventHandler<MachineSnapshot>? SnapshotPublished;

    private enum CommandKind
    {
        Run,
        Pause,
        Stop,
        Reset,
        StepInto,
        Refresh,
    }

    /// <inheritdoc />
    /// <remarks>Taken from the latest published snapshot, so it always agrees with <see cref="LatestSnapshot"/>.</remarks>
    public SessionState State => ReadPublished().Snapshot?.State ?? SessionState.Idle;

    /// <inheritdoc />
    public PauseReason PauseReason => ReadPublished().Reason;

    /// <inheritdoc />
    public ulong InstructionBudget
    {
        get => (ulong)Interlocked.Read(ref _budget);
        set
        {
            ArgumentOutOfRangeException.ThrowIfZero(value);
            Interlocked.Exchange(ref _budget, (long)Math.Min(value, long.MaxValue));
        }
    }

    /// <inheritdoc />
    public ulong InstructionsExecuted => (ulong)Interlocked.Read(ref _instructionsExecuted);

    /// <inheritdoc />
    public StepResult? LastStep => ReadPublished().LastStep;

    /// <inheritdoc />
    public MachineSnapshot? LatestSnapshot => ReadPublished().Snapshot;

    /// <inheritdoc />
    public IReadOnlyList<HistoryEntry> History => ReadPublished().History;

    /// <inheritdoc />
    public int WatchMemory(ushort segment, ushort offset, int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, MaxMemoryWatchLength);
        int id;
        lock (_watchLock)
        {
            if (_watches.Count >= MaxMemoryWatches)
            {
                throw new InvalidOperationException($"At most {MaxMemoryWatches} memory regions can be watched.");
            }

            id = ++_nextWatchId;
            _watches.Add(new MemoryWatch(id, segment, offset, length));
        }

        Post(CommandKind.Refresh);
        return id;
    }

    /// <inheritdoc />
    public void UnwatchMemory(int id)
    {
        lock (_watchLock)
        {
            _watches.RemoveAll(w => w.Id == id);
        }

        Post(CommandKind.Refresh);
    }

    /// <inheritdoc />
    public void Run() => Post(CommandKind.Run);

    /// <inheritdoc />
    public void Pause() => Post(CommandKind.Pause);

    /// <inheritdoc />
    public void StopSession() => Post(CommandKind.Stop);

    /// <inheritdoc />
    public void Reset() => Post(CommandKind.Reset);

    /// <inheritdoc />
    public void StepInto() => Post(CommandKind.StepInto);

    /// <summary>Stops the worker thread. A run in progress is interrupted before its next instruction.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Interlocked.Increment(ref _pendingCommands);
        _commands.Writer.TryComplete();
        _worker.Join(TimeSpan.FromSeconds(5));
    }

    private void Post(CommandKind kind)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Interlocked.Increment(ref _pendingCommands);
        _commands.Writer.TryWrite(new Command(kind));
    }

    private void WorkerLoop()
    {
        ChannelReader<Command> reader = _commands.Reader;
        while (reader.WaitToReadAsync().AsTask().GetAwaiter().GetResult())
        {
            while (reader.TryRead(out Command command))
            {
                Interlocked.Decrement(ref _pendingCommands);
                try
                {
                    Execute(command.Kind);
                }
#pragma warning disable CA1031 // An engine or loader fault becomes a session error; the IDE stays usable (NFR-010).
                catch (Exception)
#pragma warning restore CA1031
                {
                    SetState(SessionState.Error, PauseReason.None);
                    Publish();
                }
            }
        }
    }

    private void Execute(CommandKind kind)
    {
        switch (kind)
        {
            case CommandKind.Reset:
                _machine = null;
                _lastStep = null;
                _history.Clear();
                _historyView = [];

                _changedBytes.Clear();
                Interlocked.Exchange(ref _instructionsExecuted, 0);
                _machine = _loadMachine();
                _programSegment = _machine.Cpu.State.CS;
                _machine.Memory.TakeJournal();
                _machine.Console.TakeOutput();
                SetState(SessionState.Paused, PauseReason.Loaded);
                Publish();
                break;

            case CommandKind.Stop:
                if (_machine is not null)
                {
                    DrainJournal();
                    SetState(SessionState.Idle, PauseReason.None);
                    Publish();
                    _machine = null;
                }

                break;

            case CommandKind.Pause:
                if (_state == SessionState.Paused && _pauseReason == PauseReason.UserPause)
                {
                    Publish(); // the run stopped for this Pause
                }

                break;

            case CommandKind.StepInto:
                if (CanExecute())
                {
                    StepResult result = Step();
                    if (result.Outcome == StepOutcome.Executed)
                    {
                        SetState(SessionState.Paused, PauseReason.Step);
                    }

                    Publish();
                }

                break;

            case CommandKind.Run:
                if (CanExecute())
                {
                    RunUntilStopped();
                }

                break;

            case CommandKind.Refresh:
                if (_machine is not null && _state != SessionState.Running)
                {
                    Publish(); // new memory windows while paused; a run includes them in its next snapshot
                }

                break;
        }
    }

    /// <summary>Execution can continue from Paused and from WaitingForInput (the INT re-executes).</summary>
    private bool CanExecute() => _machine is not null && _state is SessionState.Paused or SessionState.WaitingForInput;

    private void RunUntilStopped()
    {
        SetState(SessionState.Running, PauseReason.None);
        Publish();

        long budget = Interlocked.Read(ref _budget);
        long executed = 0;
        var sinceSnapshot = Stopwatch.StartNew();
        while (true)
        {
            if (Volatile.Read(ref _pendingCommands) > 0)
            {
                SetState(SessionState.Paused, PauseReason.UserPause); // published by the command that interrupted the run
                return;
            }

            if (executed >= budget)
            {
                SetState(SessionState.Paused, PauseReason.BudgetExhausted);
                Publish();
                return;
            }

            StepResult result = Step();
            if (result.Outcome != StepOutcome.Executed)
            {
                Publish();
                return;
            }

            executed++;
            if (executed % JournalDrainInterval == 0)
            {
                DrainJournal();
            }

            if (executed % RunningCheckInterval == 0 && sinceSnapshot.Elapsed >= RunningPublishInterval)
            {
                Publish(); // "running - not current" (FR-057), at most 30 per second
                sinceSnapshot.Restart();
            }
        }
    }

    /// <summary>Executes one instruction and moves the state to Halted, Terminated or WaitingForInput when the step ends there.</summary>
    private StepResult Step()
    {
        CpuState before = _machine!.Cpu.State;
        _history.Record(_machine.Memory, before.CS, before.IP);
        StepResult result = _machine.Cpu.StepInstruction();
        _lastStep = result;
        if (result.Outcome != StepOutcome.Executed)
        {
            _history.Unrecord(); // the instruction didn't complete
        }

        switch (result.Outcome)
        {
            case StepOutcome.Executed:
                Interlocked.Increment(ref _instructionsExecuted);
                break;
            case StepOutcome.Halted:
                SetState(SessionState.Halted, PauseReason.None);
                break;
            case StepOutcome.Terminated:
                SetState(SessionState.Terminated, PauseReason.None);
                break;
            case StepOutcome.WaitingForInput:
                SetState(SessionState.WaitingForInput, PauseReason.None);
                break;
        }

        return result;
    }

    private void SetState(SessionState state, PauseReason reason)
    {
        _pauseReason = reason;
        _state = state;
    }

    /// <summary>Folds the memory journal into one entry per changed byte (keeping the value before the first write).</summary>
    private void DrainJournal()
    {
        foreach (MemoryChange change in _machine!.Memory.TakeJournal())
        {
            _changedBytes.TryAdd(change.PhysicalAddress, change.OldValue);
        }
    }

    private void Publish()
    {
        MachineSnapshot snapshot;
        if (_machine is null)
        {
            snapshot = new MachineSnapshot(default, FlagBit.None, [], string.Empty, _state, null, null, null, null, null, []);
        }
        else
        {
            DrainJournal();
            MemoryChange[] changes = [.. _changedBytes.OrderBy(c => c.Key).Select(c => new MemoryChange(c.Key, c.Value))];
            _changedBytes.Clear();
            StepResult? last = _lastStep;
            ProgramTermination? termination = _state == SessionState.Terminated ? last?.Termination : null;
            MemoryWatch[] watches;
            lock (_watchLock)
            {
                watches = [.. _watches];
            }

            IReadOnlyList<HistoryEntry> delta = _history.TakeDelta(_disassembler);
            if (delta.Count > 0)
            {
                _historyView = [.. _historyView.Skip(Math.Max(0, _historyView.Count + delta.Count - ExecutionHistory.Capacity)), .. delta];
            }

            snapshot = new MachineSnapshot(
                _machine.Cpu.State,
                last?.UndefinedFlags ?? FlagBit.None,
                changes,
                _machine.Console.TakeOutput(),
                _state,
                termination?.Path.ToString(),
                termination?.ExitCode,
                _state == SessionState.Running ? null : SnapshotContent.CurrentInstruction(_machine, _disassembler, _sourceMap, _programSegment),
                watches.Length == 0 ? null : SnapshotContent.MemoryWindows(_machine.Memory, watches),
                SnapshotContent.Stack(_machine),
                delta);
        }

        lock (_snapshotLock)
        {
            _published = new Published(snapshot, _pauseReason, _lastStep, _historyView);
        }

        SnapshotPublished?.Invoke(this, snapshot);
    }

    private Published ReadPublished()
    {
        lock (_snapshotLock)
        {
            return _published;
        }
    }

    /// <summary>What readers see: the latest snapshot with the pause reason and step result it was published with.</summary>
    private sealed record Published(MachineSnapshot? Snapshot, PauseReason Reason, StepResult? LastStep, IReadOnlyList<HistoryEntry> History);

    private readonly record struct Command(CommandKind Kind);
}
