namespace AssemblyStudio.Core;

/// <summary>
/// Decodes 8086 instruction bytes for the current-instruction view and the history (FR-053, FR-058;
/// ADR-0003 §6: implemented over Iced with the NASM formatter, 16-bit). The implementation is an
/// adapter in <c>Engine.I8086</c>; it must not know about the machine or session.
/// </summary>
public interface IDisassembler
{
    /// <summary>Decodes the first instruction in <paramref name="bytes"/> and returns its
    /// NASM-like text (including prefixes, e.g. "mov ax, bx"). Returns <see langword="null"/>
    /// when the bytes are not a valid 8086 encoding.</summary>
    /// <param name="bytes">Instruction bytes starting at the instruction address (at least the
    /// instruction length; up to 6 bytes is enough for any 8086 instruction).</param>
    string? Disassemble(ReadOnlySpan<byte> bytes);

    /// <summary>Decodes the first instruction in <paramref name="bytes"/> located at offset <paramref name="ip"/>
    /// (so relative jump and call targets show their real address) and returns its text and length, or
    /// <see langword="null"/> when the bytes are not a valid 8086 encoding.</summary>
    /// <param name="bytes">Instruction bytes starting at the instruction address (6 bytes cover any 8086 instruction).</param>
    /// <param name="ip">The instruction's offset in its code segment.</param>
    DecodedInstruction? Decode(ReadOnlySpan<byte> bytes, ushort ip);
}

/// <summary>A decoded instruction: its NASM-like text and its length in bytes.</summary>
/// <param name="Text">The instruction text, such as "mov ax, bx".</param>
/// <param name="Length">The instruction length in bytes.</param>
public readonly record struct DecodedInstruction(string Text, int Length);
