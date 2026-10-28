using AssemblyStudio.Core;

namespace AssemblyStudio.Runtime.Loading;

/// <summary>
/// Constants and layout helpers for the 256-byte (0x100) Program Segment Prefix (PSP) structure
/// placed at offset 0000h of a program's segment in DOS (FR-034; ADR-0004 §2; research R-DOS §3).
/// </summary>
public static class ProgramSegmentPrefix
{
    /// <summary>
    /// Total size of the Program Segment Prefix in bytes (256 bytes = 0x100 = 16 paragraphs).
    /// </summary>
    public const int Size = 256;

    public const int OffsetInt20 = 0x00;           // 2 bytes: CD 20 (INT 20h)
    public const int OffsetTopOfMemory = 0x02;     // 2 bytes: segment of first paragraph beyond allocation
    public const int OffsetCall5 = 0x05;           // 5 bytes: far call to CP/M CALL 5 entry at PSP:0050h
    public const int OffsetInt22Terminate = 0x0A;  // 4 bytes: saved INT 22h termination address
    public const int OffsetInt23CtrlC = 0x0E;      // 4 bytes: saved INT 23h Ctrl-C address
    public const int OffsetInt24CritErr = 0x12;    // 4 bytes: saved INT 24h critical error address
    public const int OffsetParentPsp = 0x16;       // 2 bytes: parent PSP segment
    public const int OffsetJobFileTable = 0x18;    // 20 bytes: default JFT
    public const int OffsetEnvironmentSeg = 0x2C;  // 2 bytes: environment segment
    public const int OffsetInt21Entry = 0x50;      // 3 bytes: CD 21 CB (INT 21h; RETF)
    public const int OffsetFcb1 = 0x5C;            // 16 bytes: default FCB 1
    public const int OffsetFcb2 = 0x6C;            // 20 bytes: default FCB 2
    public const int OffsetCommandTail = 0x80;     // 128 bytes: command tail / default DTA

    /// <summary>
    /// Default top-of-memory segment beyond a 64 KiB allocation at 0x0800 (0x0800 + 0x1000 = 0x1800).
    /// </summary>
    public const ushort DefaultTopOfMemorySegment = 0x1800;

    /// <summary>
    /// Writes the full 256-byte PSP structure into <paramref name="memory"/> at <paramref name="pspSegment"/>:0000h.
    /// </summary>
    /// <param name="memory">The target memory.</param>
    /// <param name="pspSegment">The segment where the PSP is being constructed.</param>
    /// <param name="commandTail">Optional command tail text.</param>
    /// <param name="topOfMemorySegment">Segment of first paragraph beyond allocation (defaults to 0x1800).</param>
    /// <param name="parentPspSegment">Parent PSP segment (defaults to pspSegment).</param>
    /// <param name="environmentSegment">Environment segment (defaults to 0x0000).</param>
    public static void Initialize(
        IMemory memory,
        ushort pspSegment,
        string? commandTail = null,
        ushort topOfMemorySegment = DefaultTopOfMemorySegment,
        ushort? parentPspSegment = null,
        ushort environmentSegment = 0x0000)
    {
        ArgumentNullException.ThrowIfNull(memory);

        ushort parent = parentPspSegment ?? pspSegment;

        // 1. 00h: CD 20 (INT 20h CP/M style exit)
        memory.Write8(pspSegment, 0x00, 0xCD);
        memory.Write8(pspSegment, 0x01, 0x20);

        // 2. 02h: Top of memory segment
        memory.Write16(pspSegment, 0x02, topOfMemorySegment);

        // 3. 04h: Reserved byte
        memory.Write8(pspSegment, 0x04, 0x00);

        // 4. 05h..09h: CALL far to PSP:0050h (9A 50 00 [seg_lo] [seg_hi])
        memory.Write8(pspSegment, 0x05, 0x9A);
        memory.Write16(pspSegment, 0x06, 0x0050);
        memory.Write16(pspSegment, 0x08, pspSegment);

        // 5. 0Ah..15h: Saved INT 22h, 23h, 24h vectors from IVT
        ushort int22Ip = memory.Read16(0x0000, 0x22 * 4);
        ushort int22Cs = memory.Read16(0x0000, (ushort)(0x22 * 4 + 2));
        memory.Write16(pspSegment, 0x0A, int22Ip);
        memory.Write16(pspSegment, 0x0C, int22Cs);

        ushort int23Ip = memory.Read16(0x0000, 0x23 * 4);
        ushort int23Cs = memory.Read16(0x0000, (ushort)(0x23 * 4 + 2));
        memory.Write16(pspSegment, 0x0E, int23Ip);
        memory.Write16(pspSegment, 0x10, int23Cs);

        ushort int24Ip = memory.Read16(0x0000, 0x24 * 4);
        ushort int24Cs = memory.Read16(0x0000, (ushort)(0x24 * 4 + 2));
        memory.Write16(pspSegment, 0x12, int24Ip);
        memory.Write16(pspSegment, 0x14, int24Cs);

        // 6. 16h: Parent PSP
        memory.Write16(pspSegment, 0x16, parent);

        // 7. 18h..2Bh: Job file table (20 bytes)
        // Standard handles 0..4 (stdin, stdout, stderr, stdaux, stdprn) open, rest unused (0xFF)
        for (ushort i = 0; i < 5; i++)
        {
            memory.Write8(pspSegment, (ushort)(0x18 + i), (byte)i);
        }
        for (ushort i = 5; i < 20; i++)
        {
            memory.Write8(pspSegment, (ushort)(0x18 + i), 0xFF);
        }

        // 8. 2Ch: Environment segment
        memory.Write16(pspSegment, 0x2C, environmentSegment);

        // 9. 2Eh..4Fh: Reserved (zeroed)
        for (ushort i = 0x2E; i < 0x50; i++)
        {
            memory.Write8(pspSegment, i, 0x00);
        }

        // 10. 50h: CD 21 CB (INT 21h; RETF)
        memory.Write8(pspSegment, 0x50, 0xCD);
        memory.Write8(pspSegment, 0x51, 0x21);
        memory.Write8(pspSegment, 0x52, 0xCB);

        // 11. 53h..5Bh: Reserved (zeroed)
        for (ushort i = 0x53; i < 0x5C; i++)
        {
            memory.Write8(pspSegment, i, 0x00);
        }

        // 12. 5Ch..6Bh: Default unopened FCB 1 (zeroed)
        for (ushort i = 0x5C; i < 0x6C; i++)
        {
            memory.Write8(pspSegment, i, 0x00);
        }

        // 13. 6Ch..7Fh: Default unopened FCB 2 (zeroed)
        for (ushort i = 0x6C; i < 0x80; i++)
        {
            memory.Write8(pspSegment, i, 0x00);
        }

        // 14. 80h..FFh: Command tail (also default DTA)
        if (string.IsNullOrEmpty(commandTail))
        {
            // Empty tail: length 0, followed by CR (0x0D)
            memory.Write8(pspSegment, 0x80, 0x00);
            memory.Write8(pspSegment, 0x81, 0x0D);
            for (ushort i = 0x82; i <= 0xFF; i++)
            {
                memory.Write8(pspSegment, i, 0x00);
            }
        }
        else
        {
            // Max command tail text is 126 characters (0x81 to 0xFE, ending with 0x0D at 0xFF)
            int len = Math.Min(commandTail.Length, 126);
            memory.Write8(pspSegment, 0x80, (byte)len);
            for (int i = 0; i < len; i++)
            {
                memory.Write8(pspSegment, (ushort)(0x81 + i), (byte)commandTail[i]);
            }
            memory.Write8(pspSegment, (ushort)(0x81 + len), 0x0D);
            for (ushort i = (ushort)(0x82 + len); i <= 0xFF; i++)
            {
                memory.Write8(pspSegment, i, 0x00);
            }
        }
    }
}
