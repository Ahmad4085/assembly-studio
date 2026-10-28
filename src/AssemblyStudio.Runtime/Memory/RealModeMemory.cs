using AssemblyStudio.Core;

namespace AssemblyStudio.Runtime.Memory;

/// <summary>
/// Emulated 1 MiB real-mode memory with 20-bit physical address wrapping and a per-step write journal
/// (FR-033, FR-056, FR-059; ADR-0004 §2; NFR-015).
/// Not thread-safe; single caller on the debug-session worker thread.
/// </summary>
public sealed class RealModeMemory : IMemory
{
    /// <summary>
    /// Total addressable memory size in bytes: 1 MiB (1,048,576 bytes, 0x100000).
    /// </summary>
    public const int MemorySize = 1024 * 1024;

    /// <summary>
    /// 20-bit address mask corresponding to the 8086's 20 address pins (A0..A19; no A20 gate).
    /// </summary>
    public const int AddressMask = 0xFFFFF;

    private readonly byte[] _bytes = new byte[MemorySize];
    private readonly List<MemoryChange> _journal = new();

    /// <summary>
    /// The uncommitted memory writes made since initialization or the last <see cref="TakeJournal"/> call.
    /// </summary>
    public IReadOnlyList<MemoryChange> CurrentJournal => _journal;

    /// <summary>
    /// Computes the 20-bit physical address for a real-mode logical address <paramref name="segment"/>:<paramref name="offset"/>.
    /// Wraps modulo 1 MiB (20-bit address space).
    /// </summary>
    /// <param name="segment">16-bit segment register value.</param>
    /// <param name="offset">16-bit offset.</param>
    /// <returns>Physical address in the range 0..0xFFFFF.</returns>
    public static int ToPhysicalAddress(ushort segment, ushort offset) =>
        ((segment << 4) + offset) & AddressMask;

    /// <inheritdoc />
    public byte Read8(ushort segment, ushort offset)
    {
        int physicalAddress = ToPhysicalAddress(segment, offset);
        return _bytes[physicalAddress];
    }

    /// <inheritdoc />
    public ushort Read16(ushort segment, ushort offset)
    {
        int lowAddr = ToPhysicalAddress(segment, offset);
        // The high byte comes from offset + 1, wrapping within the 16-bit segment end (IMemory contract).
        int highAddr = ToPhysicalAddress(segment, (ushort)(offset + 1));

        byte low = _bytes[lowAddr];
        byte high = _bytes[highAddr];
        return (ushort)(low | (high << 8));
    }

    /// <inheritdoc />
    public void Write8(ushort segment, ushort offset, byte value)
    {
        int physicalAddress = ToPhysicalAddress(segment, offset);
        _journal.Add(new MemoryChange(physicalAddress, _bytes[physicalAddress]));
        _bytes[physicalAddress] = value;
    }

    /// <inheritdoc />
    public void Write16(ushort segment, ushort offset, ushort value)
    {
        int lowAddr = ToPhysicalAddress(segment, offset);
        // High byte offset wraps at the 16-bit segment boundary.
        int highAddr = ToPhysicalAddress(segment, (ushort)(offset + 1));

        byte low = (byte)(value & 0xFF);
        byte high = (byte)(value >> 8);

        _journal.Add(new MemoryChange(lowAddr, _bytes[lowAddr]));
        _bytes[lowAddr] = low;

        _journal.Add(new MemoryChange(highAddr, _bytes[highAddr]));
        _bytes[highAddr] = high;
    }

    /// <inheritdoc />
    public IReadOnlyList<MemoryChange> TakeJournal()
    {
        if (_journal.Count == 0)
        {
            return Array.Empty<MemoryChange>();
        }

        var snapshot = _journal.ToArray();
        _journal.Clear();
        return snapshot;
    }

    /// <summary>
    /// Clears the current uncommitted write journal without modifying memory contents.
    /// </summary>
    public void ClearJournal()
    {
        _journal.Clear();
    }

    /// <summary>
    /// Rolls back the specified journal entries in reverse chronological order, restoring the memory
    /// bytes to their pre-write state. Does not generate new journal entries.
    /// </summary>
    /// <param name="changes">The sequence of journal changes to revert.</param>
    public void Rollback(IEnumerable<MemoryChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        // Convert to list if needed to iterate in reverse
        var list = changes as IReadOnlyList<MemoryChange> ?? changes.ToList();
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var change = list[i];
            int physicalAddress = change.PhysicalAddress & AddressMask;
            _bytes[physicalAddress] = change.OldValue;
        }
    }

    /// <summary>
    /// Rolls back all uncommitted journal entries currently recorded in this memory instance,
    /// restoring their old values and clearing the journal.
    /// </summary>
    public void RollbackJournal()
    {
        for (int i = _journal.Count - 1; i >= 0; i--)
        {
            var change = _journal[i];
            int physicalAddress = change.PhysicalAddress & AddressMask;
            _bytes[physicalAddress] = change.OldValue;
        }

        _journal.Clear();
    }

    /// <summary>
    /// Extracts contiguous, non-overlapping, sorted physical address ranges from a set of journaled changes.
    /// Deduplicates multiple writes to the same address.
    /// </summary>
    /// <param name="changes">Journal changes to inspect.</param>
    /// <returns>List of contiguous <see cref="AddressRange"/> entries in ascending address order.</returns>
    public static IReadOnlyList<AddressRange> ExtractChangedRanges(IEnumerable<MemoryChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        var distinctAddresses = changes
            .Select(c => c.PhysicalAddress & AddressMask)
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        if (distinctAddresses.Count == 0)
        {
            return Array.Empty<AddressRange>();
        }

        var ranges = new List<AddressRange>();
        int rangeStart = distinctAddresses[0];
        int rangeLength = 1;

        for (int i = 1; i < distinctAddresses.Count; i++)
        {
            int current = distinctAddresses[i];
            if (current == rangeStart + rangeLength)
            {
                rangeLength++;
            }
            else
            {
                ranges.Add(new AddressRange(rangeStart, rangeLength));
                rangeStart = current;
                rangeLength = 1;
            }
        }

        ranges.Add(new AddressRange(rangeStart, rangeLength));
        return ranges;
    }

    /// <summary>
    /// Extracts contiguous changed ranges from the current uncommitted write journal.
    /// </summary>
    public IReadOnlyList<AddressRange> ExtractCurrentChangedRanges() =>
        ExtractChangedRanges(_journal);

    /// <summary>
    /// Reads a byte directly from a 20-bit physical address.
    /// </summary>
    public byte ReadPhysical(int physicalAddress) =>
        _bytes[physicalAddress & AddressMask];

    /// <summary>
    /// Writes a byte directly to a 20-bit physical address, optionally journaling the previous value.
    /// </summary>
    public void WritePhysical(int physicalAddress, byte value, bool journal = true)
    {
        int addr = physicalAddress & AddressMask;
        if (journal)
        {
            _journal.Add(new MemoryChange(addr, _bytes[addr]));
        }

        _bytes[addr] = value;
    }

    /// <summary>
    /// Loads raw byte data directly into physical memory without generating journal entries.
    /// Useful for initial program image loading and test setup.
    /// </summary>
    /// <param name="physicalAddress">Target physical start address.</param>
    /// <param name="source">Bytes to write.</param>
    public void LoadBytes(int physicalAddress, ReadOnlySpan<byte> source)
    {
        int start = physicalAddress & AddressMask;
        for (int i = 0; i < source.Length; i++)
        {
            int addr = (start + i) & AddressMask;
            _bytes[addr] = source[i];
        }
    }

    /// <summary>
    /// Copies a block of memory from the specified physical address into the destination buffer.
    /// Handles 20-bit physical wrapping if the block crosses the 1 MiB boundary.
    /// </summary>
    /// <param name="physicalAddress">Source physical start address.</param>
    /// <param name="destination">Destination span to receive the bytes.</param>
    public void CopyTo(int physicalAddress, Span<byte> destination)
    {
        int start = physicalAddress & AddressMask;
        for (int i = 0; i < destination.Length; i++)
        {
            int addr = (start + i) & AddressMask;
            destination[i] = _bytes[addr];
        }
    }

    /// <summary>
    /// Clears all 1 MiB of memory to zero (0x00) and clears the write journal.
    /// </summary>
    public void Reset()
    {
        Array.Clear(_bytes, 0, _bytes.Length);
        _journal.Clear();
    }
}
