using AssemblyStudio.Core;

namespace AssemblyStudio.Engine.I8086.Tests;

/// <summary>
/// Test double for <see cref="IMemory"/>: exactly the documented semantics — 1 MiB, 20-bit address
/// wrap (no A20), word access wrapping within the segment (offset wraps at 64 KiB). Records every
/// byte write so tests can assert "nothing else changed".
/// </summary>
public sealed class FakeMemory : IMemory
{
    public const int Size = 1 << 20;

    private readonly byte[] _data = new byte[Size];
    private readonly List<MemoryChange> _journal = [];

    public byte this[int physicalAddress]
    {
        get => _data[physicalAddress];
        set => _data[physicalAddress] = value;
    }

    private static int Phys(ushort segment, ushort offset) => ((segment << 4) + offset) & (Size - 1);

    public byte Read8(ushort segment, ushort offset) => _data[Phys(segment, offset)];

    public ushort Read16(ushort segment, ushort offset) =>
        (ushort)(_data[Phys(segment, offset)] | (_data[Phys(segment, (ushort)(offset + 1))] << 8));

    public void Write8(ushort segment, ushort offset, byte value)
    {
        int physical = Phys(segment, offset);
        _journal.Add(new MemoryChange(physical, _data[physical]));
        _data[physical] = value;
    }

    public void Write16(ushort segment, ushort offset, ushort value)
    {
        Write8(segment, offset, (byte)value);
        Write8(segment, (ushort)(offset + 1), (byte)(value >> 8));
    }

    public IReadOnlyList<MemoryChange> TakeJournal()
    {
        var taken = _journal.ToArray();
        _journal.Clear();
        return taken;
    }
}
