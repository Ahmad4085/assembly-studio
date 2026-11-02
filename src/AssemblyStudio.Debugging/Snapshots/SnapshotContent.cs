using AssemblyStudio.Core;

namespace AssemblyStudio.Debugging.Snapshots;

/// <summary>
/// Builds the ADR-0007 v1.1 parts of a snapshot on the worker thread, from the live machine at publish time, as
/// immutable copies (NFR-018 by construction): the current instruction, the subscribed memory windows and the
/// stack window.
/// </summary>
internal static class SnapshotContent
{
    /// <summary>Words copied from SS:SP into the stack window (ADR-0007 v1.1 default).</summary>
    public const int StackWords = 16;

    private const int MaxInstructionLength = 6;

    /// <summary>The instruction at CS:IP with its bytes, disassembly and, when CS is the program segment, its source line.</summary>
    public static CurrentInstruction CurrentInstruction(IMachine machine, IDisassembler disassembler, ISourceMap? sourceMap, ushort programSegment)
    {
        CpuState cpu = machine.Cpu.State;
        Span<byte> buffer = stackalloc byte[MaxInstructionLength];
        for (int i = 0; i < MaxInstructionLength; i++)
        {
            buffer[i] = machine.Memory.Read8(cpu.CS, (ushort)(cpu.IP + i));
        }

        DecodedInstruction? decoded = disassembler.Decode(buffer, cpu.IP);
        SourceMapping? mapping = cpu.CS == programSegment ? sourceMap?.Find(cpu.IP) : null;
        return new CurrentInstruction(
            cpu.CS,
            cpu.IP,
            // An instruction that can't be decoded has no known length: it carries the bytes read at CS:IP (AC-028).
            (decoded is { } d ? buffer[..d.Length] : buffer).ToArray(),
            decoded?.Text,
            mapping?.Location.File,
            mapping?.Location.Line,
            mapping?.FromMacroExpansion ?? false);
    }

    /// <summary>Copies each watched region, in subscription order.</summary>
    public static MemoryWindow[] MemoryWindows(IMemory memory, IEnumerable<MemoryWatch> watches) =>
        [.. watches.Select(w =>
        {
            var bytes = new byte[w.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = memory.Read8(w.Segment, (ushort)(w.Offset + i));
            }

            return new MemoryWindow(w.Segment, w.Offset, bytes);
        })];

    /// <summary>Copies <see cref="StackWords"/> words from SS:SP upward (SP wraps within the segment).</summary>
    public static StackWindow Stack(IMachine machine)
    {
        CpuState cpu = machine.Cpu.State;
        var words = new ushort[StackWords];
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = machine.Memory.Read16(cpu.SS, (ushort)(cpu.SP + (2 * i)));
        }

        return new StackWindow(words);
    }
}

/// <summary>A subscribed memory region.</summary>
internal readonly record struct MemoryWatch(int Id, ushort Segment, ushort Offset, int Length);
