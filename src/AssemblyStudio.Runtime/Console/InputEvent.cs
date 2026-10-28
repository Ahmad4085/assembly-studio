namespace AssemblyStudio.Runtime.Console;

/// <summary>
/// An immutable input event stamped with the execution instruction count at which it was read
/// (ADR-0004 §5, NFR-016). Recorded during execution to enable bit-for-bit deterministic replay (AC-047).
/// </summary>
/// <param name="InstructionCount">The instruction count executed when the input was read.</param>
/// <param name="Value">The raw byte value read from the console input queue.</param>
public readonly record struct InputEvent(ulong InstructionCount, byte Value);
