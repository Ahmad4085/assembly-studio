namespace AssemblyStudio.Core;

/// <summary>
/// The emulated console device (C05, ADR-0004 §6): a bounded output buffer with a column counter
/// for TAB expansion, and an input queue fed by recorded input events (NFR-015, NFR-016).
/// Output is raw bytes (IBM PC code page 437); the view models decode bytes for display.
/// </summary>
public interface IConsoleDevice
{
    /// <summary>Appends one output byte (DOS output functions, C06), expanding the buffer with the
    /// oldest content trimmed at its cap (NFR-015).</summary>
    void WriteOutput(byte value);

    /// <summary>Returns the output produced since the previous call and clears the delta (the
    /// console delta in snapshots, ADR-0007 §2).</summary>
    string TakeOutput();

    /// <summary>Takes one queued input byte; <see langword="false"/> when the queue is empty, which
    /// the router reports as <see cref="StepOutcome.WaitingForInput"/> (ADR-0004 §5).</summary>
    bool TryReadInput(out byte value);

    /// <summary>Returns the byte <see cref="TryReadInput"/> would take next without taking or
    /// recording it; <see langword="false"/> when none is available. DOS uses it for the Ctrl-C, ^S and
    /// ^P checks during output, which leave other keys queued (ADR-0004 §4).</summary>
    bool TryPeekInput(out byte value);

    /// <summary>Queues one input byte from the keyboard (the debug session delivers typed keys and Ctrl-C, FR-064).</summary>
    void EnqueueInput(byte value);
}
