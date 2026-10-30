namespace AssemblyStudio.Core;

/// <summary>Why a session is <see cref="SessionState.Paused"/> (UI-012 "reason paused", FR-042).</summary>
public enum PauseReason
{
    /// <summary>Not paused.</summary>
    None,

    /// <summary>The program was just loaded (or reset) and hasn't run yet (ui-spec §4 "Ready").</summary>
    Loaded,

    /// <summary>A Step Into completed.</summary>
    Step,

    /// <summary>The user paused a run.</summary>
    UserPause,

    /// <summary>The run used up its instruction budget; the user may continue (Run) or stop (FR-042, AC-030).</summary>
    BudgetExhausted,

    /// <summary>A breakpoint was hit (FR-052, AC-032, ui-spec §4/§10).</summary>
    Breakpoint,
}

/// <summary>One input byte delivered to a program, with the number of instructions executed when it arrived (NFR-016).</summary>
/// <param name="AtInstruction">Instructions executed since load or reset when the byte was queued.</param>
/// <param name="Value">The byte.</param>
public readonly record struct InputRecord(ulong AtInstruction, byte Value);

/// <summary>
/// A debug session (C07, ADR-0007 §2): owns one emulated machine on a dedicated worker thread and runs it on
/// commands. Commands return at once; the worker processes them in order and publishes a
/// <see cref="MachineSnapshot"/> after each one, so the caller never blocks on execution (FR-067, NFR-004).
/// A run checks for a new command before every instruction, so Pause and Stop take effect within one
/// instruction.
/// </summary>
public interface IDebugSession : IDisposable
{
    /// <summary>The session state (as in the latest snapshot).</summary>
    SessionState State { get; }

    /// <summary>Why the session is paused, or <see cref="PauseReason.None"/>.</summary>
    PauseReason PauseReason { get; }

    /// <summary>Instructions one Run may execute before it pauses with <see cref="PauseReason.BudgetExhausted"/>
    /// (FR-042; default 10,000,000). A new Run starts a new budget.</summary>
    ulong InstructionBudget { get; set; }

    /// <summary>Instructions executed since the program was loaded or reset, as of the latest published snapshot.</summary>
    ulong InstructionsExecuted { get; }

    /// <summary>The result of the last step, or <see langword="null"/> before the first one. A halt's reason and a
    /// termination's path and exit code are read from here.</summary>
    StepResult? LastStep { get; }

    /// <summary>When <see cref="State"/> is <see cref="SessionState.Error"/>: what failed (exception type and message),
    /// else <see langword="null"/>. The session stays usable; Reset clears it (NFR-010).</summary>
    string? LastFault { get; }

    /// <summary>The latest published snapshot, or <see langword="null"/> before the first one.</summary>
    MachineSnapshot? LatestSnapshot { get; }

    /// <summary>The execution history: the last executed instructions (at most 1,000), oldest first (FR-058).</summary>
    IReadOnlyList<HistoryEntry> History { get; }

    /// <summary>Subscribes a memory region: each snapshot then carries an immutable copy of it in
    /// <see cref="MachineSnapshot.MemoryWindows"/>, in subscription order (ADR-0007 v1.1; at most 4 regions of at
    /// most 4 KiB). Returns an id for <see cref="UnwatchMemory"/>. The offset wraps within the segment.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is not 1–4096.</exception>
    /// <exception cref="InvalidOperationException">Four regions are already subscribed.</exception>
    int WatchMemory(ushort segment, ushort offset, int length);

    /// <summary>Ends a <see cref="WatchMemory"/> subscription. Unknown ids are ignored.</summary>
    void UnwatchMemory(int id);

    /// <summary>Raised on the worker thread after each command is processed. Handlers must return quickly and
    /// marshal to the UI thread themselves (ADR-0007 §3).</summary>
    event EventHandler<MachineSnapshot>? SnapshotPublished;

    /// <summary>Runs until termination, a halt, waiting for input, Pause, or the budget (FR-051).</summary>
    void Run();

    /// <summary>Pauses a run (FR-035).</summary>
    void Pause();

    /// <summary>Ends the session: the machine is released and the state becomes <see cref="SessionState.Idle"/> (FR-035).</summary>
    void StopSession();

    /// <summary>Reloads the original image into a fresh machine: initial registers, cleared console (FR-035).</summary>
    void Reset();

    /// <summary>All breakpoints configured in the session (FR-052).</summary>
    IReadOnlyList<Breakpoint> Breakpoints { get; }

    /// <summary>
    /// Toggles a line breakpoint: removes it if one already exists at <paramref name="file"/>:<paramref name="line"/>,
    /// otherwise adds an enabled breakpoint (FR-052).
    /// </summary>
    /// <param name="file">Full path of the source file, or <see langword="null"/> for the untitled document.</param>
    /// <param name="line">1-based line number.</param>
    /// <returns><see langword="true"/> if a breakpoint was added; <see langword="false"/> if one was removed.</returns>
    bool ToggleBreakpoint(string? file, int line);

    /// <summary>
    /// Adds or updates a line breakpoint (FR-052).
    /// </summary>
    /// <param name="file">Full path of the source file, or <see langword="null"/> for the untitled document.</param>
    /// <param name="line">1-based line number.</param>
    /// <param name="enabled">Whether the breakpoint is enabled.</param>
    void SetBreakpoint(string? file, int line, bool enabled = true);

    /// <summary>
    /// Removes a line breakpoint (FR-052).
    /// </summary>
    /// <param name="file">Full path of the source file, or <see langword="null"/> for the untitled document.</param>
    /// <param name="line">1-based line number.</param>
    /// <returns><see langword="true"/> if a breakpoint was found and removed; <see langword="false"/> otherwise.</returns>
    bool RemoveBreakpoint(string? file, int line);

    /// <summary>
    /// Re-evaluates all breakpoints with a new source map after a rebuild (FR-052).
    /// </summary>
    /// <param name="sourceMap">The new source map, or <see langword="null"/> if the build produced none.</param>
    void RebindBreakpoints(ISourceMap? sourceMap);

    /// <summary>Executes exactly one instruction (FR-049).</summary>
    void StepInto();

    /// <summary>
    /// Delivers keyboard input to the program (FR-064): the bytes are queued in the console and recorded with the
    /// instruction count at which they arrived. A running program keeps running. A program waiting for input
    /// continues what it was doing: a run keeps running, a step completes. Ignored while replaying.
    /// </summary>
    /// <param name="bytes">The input bytes (code page 437; an extended key is 00h followed by its scan code).</param>
    void SendInput(IReadOnlyList<byte> bytes);

    /// <summary>Delivers Ctrl-C (03h) to the program, as <see cref="SendInput"/> does (D-015). DOS services check for it
    /// (TASK-036): INT 21h 01h/02h/08h/09h/0Ah break, 07h returns it as a character.</summary>
    void SendCtrlC();

    /// <summary>The input delivered since the program was loaded or reset, in order, each byte with the instruction
    /// count at which it arrived (NFR-016).</summary>
    IReadOnlyList<InputRecord> RecordedInput { get; }

    /// <summary>Reloads the program (as <see cref="Reset"/>) and replays <paramref name="recording"/>: each byte is
    /// delivered at its recorded instruction count, so the same commands produce the same execution (NFR-016,
    /// AC-047). Live input is ignored until the next Reset.</summary>
    void Replay(IReadOnlyList<InputRecord> recording);

    /// <summary>
    /// Executes a CALL or INT and runs until control returns to the next instruction, stopping earlier at
    /// breakpoints, errors or termination. For other instructions, executes exactly one instruction like
    /// <see cref="StepInto"/> (FR-050, AC-033).
    /// </summary>
    void StepOver();
}
