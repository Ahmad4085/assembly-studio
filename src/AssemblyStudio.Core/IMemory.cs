namespace AssemblyStudio.Core;

/// <summary>One journaled memory write: the 20-bit physical address and the value before the write
/// (ADR-0003 §5: the journal backs change highlighting FR-056, history FR-058 and later reverse
/// stepping FR-060 without full 1 MiB copies).</summary>
/// <param name="PhysicalAddress">20-bit physical address, 0..0xFFFFF.</param>
/// <param name="OldValue">The byte before the write.</param>
public readonly record struct MemoryChange(int PhysicalAddress, byte OldValue);

/// <summary>
/// The emulated 1 MiB real-mode memory with a per-step write journal (FR-033; ADR-0004 §2). Word
/// access wraps within the segment; the 20-bit physical address wraps at 1 MiB (no A20 on the 8086).
/// Implementations are owned by the runtime (C05) and are not thread-safe; the debug-session worker
/// thread is the only caller.
/// </summary>
public interface IMemory
{
    /// <summary>Reads one byte at <paramref name="segment"/>:<paramref name="offset"/> (physical
    /// ((segment × 16) + offset) mod 1 MiB).</summary>
    byte Read8(ushort segment, ushort offset);

    /// <summary>Reads one little-endian word; the high byte comes from
    /// <paramref name="offset"/> + 1, wrapping at the segment end.</summary>
    ushort Read16(ushort segment, ushort offset);

    /// <summary>Writes one byte and journals the previous value.</summary>
    void Write8(ushort segment, ushort offset, byte value);

    /// <summary>Writes one little-endian word (two journaled byte writes).</summary>
    void Write16(ushort segment, ushort offset, ushort value);

    /// <summary>Returns the writes made since the last call (in write order) and clears the journal
    /// (the "changed ranges since the last publish" of ADR-0007 §2).</summary>
    IReadOnlyList<MemoryChange> TakeJournal();
}
