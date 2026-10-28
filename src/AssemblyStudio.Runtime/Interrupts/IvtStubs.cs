using AssemblyStudio.Core;

namespace AssemblyStudio.Runtime.Interrupts;

/// <summary>
/// Manages the real-mode Interrupt Vector Table (IVT at 0000:0000h..03FFh) and the default
/// interrupt handler stubs located in the reserved ROM BIOS segment (0xF000:0000h..03FCh; ADR-0004 §2–3).
/// </summary>
public static class IvtStubs
{
    /// <summary>
    /// Reserved segment in upper real-mode memory (0xF000, physical 0xF0000) where default interrupt
    /// handler stubs reside (ADR-0004 §2–3).
    /// </summary>
    public const ushort DefaultStubSegment = 0xF000;

    /// <summary>
    /// Opcode for the 8086 <c>IRET</c> instruction (<c>0xCF</c>).
    /// </summary>
    public const byte IretOpcode = 0xCF;

    /// <summary>
    /// Computes the offset in <see cref="DefaultStubSegment"/> for a given vector's stub.
    /// Each stub is allocated 4 bytes (e.g. vector 0 at offset 0, vector 1 at offset 4, etc.).
    /// </summary>
    /// <param name="vector">Interrupt vector number (0–255).</param>
    /// <returns>Offset in segment <see cref="DefaultStubSegment"/>.</returns>
    public static ushort GetDefaultStubOffset(byte vector) => (ushort)(vector * 4);

    /// <summary>
    /// Initializes all 256 IVT entries (at 0000:0000h..03FFh) to point to the reserved stub area
    /// (<see cref="DefaultStubSegment"/>:offset), and places an <c>IRET</c> instruction (0xCF) at each stub address.
    /// </summary>
    /// <param name="memory">The target real-mode memory.</param>
    public static void Initialize(IMemory memory)
    {
        ArgumentNullException.ThrowIfNull(memory);

        for (int v = 0; v < 256; v++)
        {
            byte vector = (byte)v;
            ushort stubOffset = GetDefaultStubOffset(vector);
            ushort ivtOffset = (ushort)(vector * 4);

            // IVT entry: offset (low word), then segment (high word)
            memory.Write16(0x0000, ivtOffset, stubOffset);
            memory.Write16(0x0000, (ushort)(ivtOffset + 2), DefaultStubSegment);

            // Write IRET stub instruction in the reserved stub segment
            memory.Write8(DefaultStubSegment, stubOffset, IretOpcode);
        }
    }

    /// <summary>
    /// Checks whether the vector entry for <paramref name="vector"/> in <paramref name="memory"/>
    /// still points to the default reserved stub address.
    /// </summary>
    /// <param name="memory">The memory to inspect.</param>
    /// <param name="vector">The interrupt vector number.</param>
    /// <returns><see langword="true"/> if the vector is the default stub address; otherwise <see langword="false"/> (program-installed vector).</returns>
    public static bool IsDefaultVector(IMemory memory, byte vector)
    {
        ArgumentNullException.ThrowIfNull(memory);

        ushort ivtOffset = (ushort)(vector * 4);
        ushort ip = memory.Read16(0x0000, ivtOffset);
        ushort cs = memory.Read16(0x0000, (ushort)(ivtOffset + 2));

        return cs == DefaultStubSegment && ip == GetDefaultStubOffset(vector);
    }

    /// <summary>
    /// Checks if the IVT is unpopulated (i.e. vector 0 has 0000:0000).
    /// </summary>
    /// <param name="memory">The memory to inspect.</param>
    /// <returns><see langword="true"/> if vector 0 is zeroed; otherwise <see langword="false"/>.</returns>
    public static bool IsIvtEmpty(IMemory memory)
    {
        ArgumentNullException.ThrowIfNull(memory);
        return memory.Read16(0x0000, 0x0000) == 0 && memory.Read16(0x0000, 0x0002) == 0;
    }
}
