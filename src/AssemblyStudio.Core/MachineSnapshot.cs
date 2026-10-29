namespace AssemblyStudio.Core;

/// <summary>Debug-session state as published in <see cref="MachineSnapshot"/> (C07, ADR-0007 §2).</summary>
public enum SessionState
{
    /// <summary>No program loaded.</summary>
    Idle,

    /// <summary>Running; the snapshot may lag ("running — not current", FR-057, 30 Hz cap).</summary>
    Running,

    /// <summary>Paused or stopped; the snapshot is current.</summary>
    Paused,

    /// <summary>Awaiting console input; CS:IP is on the INT instruction (ADR-0004 §5).</summary>
    WaitingForInput,

    /// <summary>Execution halted (unsupported instruction, runtime error, unsupported service, HLT).</summary>
    Halted,

    /// <summary>The program terminated normally; the termination path and exit code are known (FR-041).</summary>
    Terminated,

    /// <summary>An engine fault became a session error; the IDE stays usable (NFR-010).</summary>
    Error,
}

/// <summary>The instruction the CPU is about to execute (ADR-0007 v1.1, from ui-spec §10).</summary>
/// <param name="Cs">CS of the instruction.</param>
/// <param name="Ip">IP of the instruction.</param>
/// <param name="Bytes">The instruction bytes (up to 6).</param>
/// <param name="Disassembly">Decoded text via <see cref="IDisassembler"/>, or <see langword="null"/> when not decodable.</param>
/// <param name="SourcePath">Source file from the source map, or <see langword="null"/> when unknown.</param>
/// <param name="SourceLine">1-based source line from the source map, or <see langword="null"/> when unknown.</param>
/// <param name="FromMacroExpansion"><see langword="true"/> when the line came from a macro expansion.</param>
public readonly record struct CurrentInstruction(
    ushort Cs,
    ushort Ip,
    IReadOnlyList<byte> Bytes,
    string? Disassembly,
    string? SourcePath,
    int? SourceLine,
    bool FromMacroExpansion);

/// <summary>An immutable copy of a UI-subscribed memory region (ADR-0007 v1.1:
/// <c>IDebugSession.WatchMemory(seg, off, length)</c>; up to 4 regions of ≤ 4 KiB each).</summary>
/// <param name="Segment">Segment the window was subscribed at.</param>
/// <param name="Offset">Offset the window was subscribed at.</param>
/// <param name="Bytes">The region contents (≤ 4 KiB).</param>
public readonly record struct MemoryWindow(ushort Segment, ushort Offset, IReadOnlyList<byte> Bytes);

/// <summary>An immutable copy of N words from SS:SP (ADR-0007 v1.1; default 16). Word 0 is [SS:SP].</summary>
/// <param name="Words">The stack words, starting at SS:SP.</param>
public readonly record struct StackWindow(IReadOnlyList<ushort> Words);

/// <summary>
/// The immutable state the debug session publishes after a step or pause (ADR-0007 §2 and the v1.1
/// amendment, OQ-023). The UI consumes only these snapshots (NFR-018: no live engine state, no
/// fabricated data). Producer contract: pass lists created for the snapshot and never mutate them
/// afterwards. <see cref="Cpu"/> and <see cref="UndefinedFlags"/> are value copies, so later CPU
/// execution cannot change a published snapshot.
/// </summary>
/// <param name="Cpu">CPU state at publish time.</param>
/// <param name="UndefinedFlags">Mask of flags currently shown as "undefined" (FR-048).</param>
/// <param name="MemoryChanges">Memory writes since the last snapshot, from the journal (FR-056).</param>
/// <param name="ConsoleDelta">Console output since the last snapshot (ADR-0007 §2).</param>
/// <param name="State">Session state.</param>
/// <param name="TerminationPath">Termination description when <see cref="SessionState.Terminated"/>, else <see langword="null"/> (FR-041).</param>
/// <param name="ExitCode">DOS exit code when terminated (AL for 4Ch, 00h for the other paths, AC-064), else <see langword="null"/>.</param>
/// <param name="CurrentInstruction">The decoded current instruction, when the session is current (ADR-0007 v1.1).</param>
/// <param name="MemoryWindows">Copies of the subscribed memory regions, when subscribed (ADR-0007 v1.1).</param>
/// <param name="Stack">The stack window copy, when subscribed (ADR-0007 v1.1).</param>
/// <param name="HistoryDelta">Instructions executed since the last snapshot, bounded by the 1,000-entry history (ADR-0007 v1.1).</param>
public readonly record struct MachineSnapshot(
    CpuState Cpu,
    FlagBit UndefinedFlags,
    IReadOnlyList<MemoryChange> MemoryChanges,
    string ConsoleDelta,
    SessionState State,
    string? TerminationPath,
    int? ExitCode,
    CurrentInstruction? CurrentInstruction,
    IReadOnlyList<MemoryWindow>? MemoryWindows,
    StackWindow? Stack,
    int HistoryDelta);
