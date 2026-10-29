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
}

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

    /// <summary>Instructions executed since the program was loaded or reset.</summary>
    ulong InstructionsExecuted { get; }

    /// <summary>The result of the last step, or <see langword="null"/> before the first one. A halt's reason and a
    /// termination's path and exit code are read from here.</summary>
    StepResult? LastStep { get; }

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

    /// <summary>Executes exactly one instruction (FR-049).</summary>
    void StepInto();
}
