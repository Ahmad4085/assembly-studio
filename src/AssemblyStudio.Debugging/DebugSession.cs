using System.Collections.Concurrent;
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
/// <para>
/// Requests that don't change control flow (keyboard input, Ctrl-C, a memory-window refresh) go through a separate
/// queue that a run handles between instructions without stopping; only Run, Pause, Stop, Reset, Replay and the
/// steps interrupt a run. Input is recorded with the instruction count at which it arrived, and a replay delivers
/// each byte at that count again, so the execution is identical (NFR-016, AC-047).
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
    private ISourceMap? _sourceMap;
    private readonly List<Breakpoint> _breakpoints = [];
    private readonly Lock _breakpointLock = new();
    private volatile bool[] _activeBreakpoints = new bool[65536];
    private (ushort Cs, ushort Ip)? _temporaryBreakpoint;
    private (ushort Cs, ushort Ip)? _lastBreakpointHit;
    private readonly ExecutionHistory _history = new();
    private readonly List<MemoryWatch> _watches = [];
    private readonly Lock _watchLock = new();
    private int _nextWatchId;
    private ushort _programSegment;
    private IReadOnlyList<HistoryEntry> _historyView = [];
    private readonly ConcurrentQueue<InlineRequest> _inline = new();
    private int _inlineCount;
    private readonly List<InputRecord> _recorded = [];
    private readonly Lock _inputLock = new();
    private Queue<InputRecord>? _replay;
    private WaitResume _waitResume;

    private IMachine? _machine;
    private int _pendingCommands;
    private SessionState _state = SessionState.Idle; // worker-only; readers use the published copy
    private PauseReason _pauseReason = PauseReason.None;
    private long _instructionsExecuted;
    private long _budget = (long)DefaultInstructionBudget;
    private StepResult? _lastStep;
    private Published _published = new(null, PauseReason.None, null, [], 0);
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
        StepOver,
        Replay,
        Inline,
    }

    /// <summary>What continues when a program that waits for input gets it.</summary>
    private enum WaitResume
    {
        None,
        Run,
        Step,
    }

    private enum InlineKind
    {
        Refresh,
        Input,
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
    /// <remarks>As of the latest published snapshot, so it always agrees with <see cref="LatestSnapshot"/>.</remarks>
    public ulong InstructionsExecuted => ReadPublished().Instructions;

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

        PostInline(new InlineRequest(InlineKind.Refresh, null));
        return id;
    }

    /// <inheritdoc />
    public void UnwatchMemory(int id)
    {
        lock (_watchLock)
        {
            _watches.RemoveAll(w => w.Id == id);
        }

        PostInline(new InlineRequest(InlineKind.Refresh, null));
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

    /// <inheritdoc />
    public void StepOver() => Post(CommandKind.StepOver);

    /// <inheritdoc />
    public void SendInput(IReadOnlyList<byte> bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Count > 0)
        {
            PostInline(new InlineRequest(InlineKind.Input, [.. bytes]));
        }
    }

    /// <inheritdoc />
    public void SendCtrlC() => SendInput([0x03]);

    /// <inheritdoc />
    public IReadOnlyList<InputRecord> RecordedInput
    {
        get
        {
            lock (_inputLock)
            {
                return [.. _recorded];
            }
        }
    }

    /// <inheritdoc />
    public void Replay(IReadOnlyList<InputRecord> recording)
    {
        ArgumentNullException.ThrowIfNull(recording);
        ObjectDisposedException.ThrowIf(_disposed, this);
        Interlocked.Increment(ref _pendingCommands);
        _commands.Writer.TryWrite(new Command(CommandKind.Replay, [.. recording]));
    }

    /// <inheritdoc />
    public IReadOnlyList<Breakpoint> Breakpoints
    {
        get
        {
            lock (_breakpointLock)
            {
                return [.. _breakpoints];
            }
        }
    }

    /// <inheritdoc />
    public bool ToggleBreakpoint(string? file, int line)
    {
        lock (_breakpointLock)
        {
            int index = _breakpoints.FindIndex(b => b.Location.Line == line && SameFile(b.Location.File, file));
            if (index >= 0)
            {
                _breakpoints.RemoveAt(index);
                UpdateActiveBreakpoints();
                return false;
            }

            ushort? offset = Volatile.Read(ref _sourceMap)?.FirstOffsetOf(file, line);
            _breakpoints.Add(new Breakpoint(new SourceLocation(file, line), offset.HasValue, offset, IsEnabled: true));
            UpdateActiveBreakpoints();
            return true;
        }
    }

    /// <inheritdoc />
    public void SetBreakpoint(string? file, int line, bool enabled = true)
    {
        lock (_breakpointLock)
        {
            int index = _breakpoints.FindIndex(b => b.Location.Line == line && SameFile(b.Location.File, file));
            ushort? offset = Volatile.Read(ref _sourceMap)?.FirstOffsetOf(file, line);
            var bp = new Breakpoint(new SourceLocation(file, line), offset.HasValue, offset, enabled);
            if (index >= 0)
            {
                _breakpoints[index] = bp;
            }
            else
            {
                _breakpoints.Add(bp);
            }

            UpdateActiveBreakpoints();
        }
    }

    /// <inheritdoc />
    public bool RemoveBreakpoint(string? file, int line)
    {
        lock (_breakpointLock)
        {
            int index = _breakpoints.FindIndex(b => b.Location.Line == line && SameFile(b.Location.File, file));
            if (index >= 0)
            {
                _breakpoints.RemoveAt(index);
                UpdateActiveBreakpoints();
                return true;
            }

            return false;
        }
    }

    /// <inheritdoc />
    public void RebindBreakpoints(ISourceMap? sourceMap)
    {
        lock (_breakpointLock)
        {
            Volatile.Write(ref _sourceMap, sourceMap);
            for (int i = 0; i < _breakpoints.Count; i++)
            {
                Breakpoint bp = _breakpoints[i];
                ushort? offset = sourceMap?.FirstOffsetOf(bp.Location.File, bp.Location.Line);
                _breakpoints[i] = new Breakpoint(bp.Location, offset.HasValue, offset, bp.IsEnabled);
            }

            UpdateActiveBreakpoints();
        }
    }

    private void UpdateActiveBreakpoints()
    {
        var table = new bool[65536];
        foreach (Breakpoint bp in _breakpoints)
        {
            if (bp.IsEnabled && bp.IsBound && bp.Offset.HasValue)
            {
                table[bp.Offset.Value] = true;
            }
        }

        _activeBreakpoints = table;
    }

    private static bool SameFile(string? a, string? b)
    {
        if (a is null || b is null)
        {
            return a == b;
        }

        try
        {
            return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }

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

    /// <summary>Queues a request a run handles without stopping; the Inline command wakes an idle worker.</summary>
    private void PostInline(InlineRequest request)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _inline.Enqueue(request);
        Interlocked.Increment(ref _inlineCount);
        _commands.Writer.TryWrite(new Command(CommandKind.Inline));
    }

    private void WorkerLoop()
    {
        ChannelReader<Command> reader = _commands.Reader;
        while (reader.WaitToReadAsync().AsTask().GetAwaiter().GetResult())
        {
            while (reader.TryRead(out Command command))
            {
                if (command.Kind != CommandKind.Inline)
                {
                    Interlocked.Decrement(ref _pendingCommands);
                }

                try
                {
                    Execute(command);
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

    private void Execute(Command command)
    {
        switch (command.Kind)
        {
            case CommandKind.Reset:
                Load(replay: null);
                break;

            case CommandKind.Replay:
                Load(replay: command.Recording);
                break;

            case CommandKind.Inline:
                ProcessInline(running: false);
                break;

            case CommandKind.Stop:
                if (_machine is not null)
                {
                    _temporaryBreakpoint = null;
                    _lastBreakpointHit = null;
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
                    StepOnce();
                }

                break;

            case CommandKind.StepOver:
                ExecuteStepOver();
                break;

            case CommandKind.Run:
                if (CanExecute())
                {
                    RunUntilStopped();
                }

                break;
        }
    }

    /// <summary>Loads a fresh machine (Reset), optionally replaying a recording into it.</summary>
    private void Load(IReadOnlyList<InputRecord>? replay)
    {
        _machine = null;
        _lastStep = null;
        _history.Clear();
        _historyView = [];
        _temporaryBreakpoint = null;
        _lastBreakpointHit = null;

        _changedBytes.Clear();
        Interlocked.Exchange(ref _instructionsExecuted, 0);
        _machine = _loadMachine();
        _programSegment = _machine.Cpu.State.CS;
        _machine.Memory.TakeJournal();
        _machine.Console.TakeOutput();
        _waitResume = WaitResume.None;
        lock (_inputLock)
        {
            _recorded.Clear();
        }

        _replay = replay is null ? null : new Queue<InputRecord>(replay);
        SetState(SessionState.Paused, PauseReason.Loaded);
        Publish();
    }

    /// <summary>
    /// Handles queued input and refresh requests. During a run (<paramref name="running"/>) input is only delivered:
    /// the run continues and its snapshots include any new window. Otherwise new input lets a waiting program
    /// continue what it was doing, and a refresh republishes the snapshot.
    /// </summary>
    private void ProcessInline(bool running)
    {
        bool delivered = false;
        bool refresh = false;
        while (_inline.TryDequeue(out InlineRequest request))
        {
            Interlocked.Decrement(ref _inlineCount);
            if (request.Kind == InlineKind.Refresh)
            {
                refresh = true;
            }
            else if (_machine is not null && _replay is null)
            {
                ulong at = (ulong)Interlocked.Read(ref _instructionsExecuted);
                foreach (byte value in request.Bytes!)
                {
                    _machine.Console.EnqueueInput(value);
                    lock (_inputLock)
                    {
                        _recorded.Add(new InputRecord(at, value));
                    }
                }

                delivered = true;
            }
        }

        if (running || _machine is null)
        {
            return;
        }

        if (delivered && _state == SessionState.WaitingForInput && _waitResume != WaitResume.None)
        {
            WaitResume resume = _waitResume;
            _waitResume = WaitResume.None;
            if (resume == WaitResume.Run)
            {
                RunUntilStopped();
            }
            else
            {
                StepOnce();
            }

            return;
        }

        if (refresh)
        {
            Publish(); // new memory windows while paused
        }
    }

    /// <summary>Executes one instruction and publishes (Step Into, and Step Over of anything but CALL/INT).</summary>
    private void StepOnce()
    {
        _lastBreakpointHit = null;
        StepResult result = Step();
        if (result.Outcome == StepOutcome.Executed)
        {
            SetState(SessionState.Paused, PauseReason.Step);
        }
        else if (result.Outcome == StepOutcome.WaitingForInput)
        {
            _waitResume = WaitResume.Step;
        }

        Publish();
    }

    /// <summary>Execution can continue from Paused and from WaitingForInput (the INT re-executes).</summary>
    private bool CanExecute() => _machine is not null && _state is SessionState.Paused or SessionState.WaitingForInput;

    private void ExecuteStepOver()
    {
        if (!CanExecute())
        {
            return;
        }

        CpuState cpu = _machine!.Cpu.State;
        Span<byte> buffer = stackalloc byte[6];
        for (int i = 0; i < 6; i++)
        {
            buffer[i] = _machine.Memory.Read8(cpu.CS, (ushort)(cpu.IP + i));
        }

        DecodedInstruction? decoded = _disassembler.Decode(buffer, cpu.IP);
        if (decoded is not null && IsCallOrInterrupt(decoded.Value.Text))
        {
            ushort nextIp = (ushort)(cpu.IP + decoded.Value.Length);
            _temporaryBreakpoint = (cpu.CS, nextIp);
            RunUntilStopped();
        }
        else
        {
            StepOnce();
        }
    }

    private static bool IsCallOrInterrupt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string trimmed = text.Trim();
        return trimmed.StartsWith("call", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("int", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains(" call ", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains(" int ", StringComparison.OrdinalIgnoreCase);
    }

    private void RunUntilStopped()
    {
        SetState(SessionState.Running, PauseReason.None);
        Publish();

        long budget = Interlocked.Read(ref _budget);
        long executed = 0;
        var sinceSnapshot = Stopwatch.StartNew();

        bool skipBreakpointAtCurrentIp = _lastBreakpointHit.HasValue
            && _lastBreakpointHit.Value.Cs == _machine!.Cpu.State.CS
            && _lastBreakpointHit.Value.Ip == _machine.Cpu.State.IP;
        _lastBreakpointHit = null;

        while (true)
        {
            if (Volatile.Read(ref _inlineCount) > 0)
            {
                ProcessInline(running: true); // input and refreshes don't stop a run
            }

            if (Volatile.Read(ref _pendingCommands) > 0)
            {
                _temporaryBreakpoint = null;
                SetState(SessionState.Paused, PauseReason.UserPause); // published by the command that interrupted the run
                return;
            }

            CpuState cpu = _machine!.Cpu.State;

            // Check temporary breakpoint (Step Over)
            if (_temporaryBreakpoint.HasValue && _temporaryBreakpoint.Value.Cs == cpu.CS && _temporaryBreakpoint.Value.Ip == cpu.IP)
            {
                _temporaryBreakpoint = null;
                bool isUserBp = cpu.CS == _programSegment && _activeBreakpoints[cpu.IP];
                if (isUserBp)
                {
                    _lastBreakpointHit = (cpu.CS, cpu.IP);
                }

                SetState(SessionState.Paused, isUserBp ? PauseReason.Breakpoint : PauseReason.Step);
                Publish();
                return;
            }

            // Check user breakpoint (pauses BEFORE executing the instruction)
            if (!skipBreakpointAtCurrentIp && cpu.CS == _programSegment && _activeBreakpoints[cpu.IP])
            {
                _temporaryBreakpoint = null;
                _lastBreakpointHit = (cpu.CS, cpu.IP);
                SetState(SessionState.Paused, PauseReason.Breakpoint);
                Publish();
                return;
            }

            skipBreakpointAtCurrentIp = false;

            if (executed >= budget)
            {
                _temporaryBreakpoint = null;
                SetState(SessionState.Paused, PauseReason.BudgetExhausted);
                Publish();
                return;
            }

            StepResult result = Step();
            if (result.Outcome != StepOutcome.Executed)
            {
                if (result.Outcome == StepOutcome.WaitingForInput)
                {
                    _waitResume = WaitResume.Run; // input continues the run (a Step Over keeps its target)
                }
                else
                {
                    _temporaryBreakpoint = null;
                }

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
        if (_replay is not null)
        {
            // Replay: deliver each recorded byte at the instruction count at which it originally arrived.
            ulong now = (ulong)Interlocked.Read(ref _instructionsExecuted);
            while (_replay.Count > 0 && _replay.Peek().AtInstruction <= now)
            {
                InputRecord record = _replay.Dequeue();
                _machine!.Console.EnqueueInput(record.Value);
                lock (_inputLock)
                {
                    _recorded.Add(record);
                }
            }
        }

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
                _state == SessionState.Running ? null : SnapshotContent.CurrentInstruction(_machine, _disassembler, Volatile.Read(ref _sourceMap), _programSegment),
                watches.Length == 0 ? null : SnapshotContent.MemoryWindows(_machine.Memory, watches),
                SnapshotContent.Stack(_machine),
                delta);
        }

        lock (_snapshotLock)
        {
            _published = new Published(snapshot, _pauseReason, _lastStep, _historyView, (ulong)Interlocked.Read(ref _instructionsExecuted));
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
    private sealed record Published(MachineSnapshot? Snapshot, PauseReason Reason, StepResult? LastStep, IReadOnlyList<HistoryEntry> History, ulong Instructions);

    private readonly record struct Command(CommandKind Kind, IReadOnlyList<InputRecord>? Recording = null);

    private readonly record struct InlineRequest(InlineKind Kind, byte[]? Bytes);
}
