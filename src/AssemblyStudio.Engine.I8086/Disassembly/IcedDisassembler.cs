using System;
using System.Buffers;
using System.Linq;
using System.Threading;
using AssemblyStudio.Core;
using Iced.Intel;
using IcedDecoder = Iced.Intel.Decoder;

namespace AssemblyStudio.Engine.I8086.Disassembly;

/// <summary>
/// 8086 instruction disassembler adapter (FR-053, FR-058; ADR-0003 §6).
/// Uses <see cref="Iced"/> 1.21.0 in 16-bit mode with the NASM formatter, filtering out
/// non-8086 instructions (e.g. 186/286/386+ extensions, 32-bit registers, FS/GS prefixes).
/// </summary>
public sealed class IcedDisassembler : IDisassembler
{
    private static readonly ThreadLocal<NasmFormatter> Formatter = new(() =>
    {
        var f = new NasmFormatter();
        f.Options.SpaceAfterOperandSeparator = true;
        f.Options.UppercaseHex = false;
        f.Options.ShowUselessPrefixes = true;
        return f;
    });

    [ThreadStatic]
    private static byte[]? t_buffer;

    /// <summary>
    /// Decodes the first instruction in <paramref name="bytes"/> and returns its
    /// NASM-like text (including prefixes, e.g. "mov ax, bx"). Returns <see langword="null"/>
    /// when the bytes are not a valid 8086 encoding.
    /// </summary>
    /// <param name="bytes">Instruction bytes starting at the instruction address.</param>
    public string? Disassemble(ReadOnlySpan<byte> bytes)
    {
        var (text, _) = DisassembleInstruction(bytes, 0);
        return text;
    }

    /// <inheritdoc />
    public DecodedInstruction? Decode(ReadOnlySpan<byte> bytes, ushort ip)
    {
        var (text, length) = DisassembleInstruction(bytes, ip);
        return text is null ? null : new DecodedInstruction(text, length);
    }

    /// <summary>
    /// Decodes the first instruction in <paramref name="bytes"/> at the specified <paramref name="ip"/>
    /// and returns its NASM text along with the decoded instruction length.
    /// </summary>
    /// <param name="bytes">Instruction bytes starting at <paramref name="ip"/>.</param>
    /// <param name="ip">Instruction pointer value for relative branch target calculation.</param>
    /// <returns>A tuple of (text, length), or (null, 0) if invalid or non-8086.</returns>
    public static (string? Text, int Length) DisassembleInstruction(ReadOnlySpan<byte> bytes, ushort ip = 0)
    {
        if (bytes.IsEmpty)
        {
            return (null, 0);
        }

        // Limit decode slice to max x86 instruction length (15 bytes)
        int length = Math.Min(bytes.Length, 15);
        byte[] buffer = t_buffer ??= new byte[15];
        bytes[..length].CopyTo(buffer);

        var reader = new ByteArrayCodeReader(buffer, 0, length);
        var decoder = IcedDecoder.Create(16, reader, (ulong)ip, DecoderOptions.None);
        decoder.Decode(out var instruction);

        if (!IsValid8086(instruction, buffer, length))
        {
            return (null, 0);
        }

        var output = new StringOutput();
        Formatter.Value!.Format(instruction, output);
        return (output.ToString(), instruction.Length);
    }

    /// <summary>
    /// Validates that the decoded instruction is a genuine 8086 instruction and not a later
    /// processor extension (186+, 286+, 386+, FS/GS segment, etc.) or unsupported alias.
    /// </summary>
    private static bool IsValid8086(in Instruction instruction, byte[] buffer, int bufferLength)
    {
        if (instruction.IsInvalid || instruction.Length == 0)
        {
            return false;
        }

        // Segment prefix validation: 8086 only has ES, CS, SS, DS (no FS, GS)
        if (instruction.SegmentPrefix is Register.FS or Register.GS)
        {
            return false;
        }

        // CpuidFeature validation: all required CPU features must be INTEL8086 or INTEL8086_ONLY
        var features = instruction.CpuidFeatures;
        if (features.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < features.Length; i++)
        {
            var f = features[i];
            if (f is not (CpuidFeature.INTEL8086 or CpuidFeature.INTEL8086_ONLY))
            {
                return false;
            }
        }

        // Undocumented / unsupported opcodes per ADR-0003 §4:
        // SALC (0xD6) is physically present on 8086 silicon but unsupported in AssemblyStudio MVP
        if (instruction.Mnemonic == Mnemonic.Salc)
        {
            return false;
        }

        // Validate prefix bytes at start of instruction: only valid 8086 prefixes (ES, CS, SS, DS, LOCK, REP, REPNE)
        // Ensure 386+ prefixes like 0x66 (operand size) or 0x67 (address size) are not present
        for (int i = 0; i < instruction.Length && i < bufferLength; i++)
        {
            byte b = buffer[i];
            if (b is 0x66 or 0x67 or 0x64 or 0x65)
            {
                return false;
            }

            // Once we reach a non-prefix byte, stop scanning prefix bytes
            if (b is not (0x26 or 0x2E or 0x36 or 0x3E or 0xF0 or 0xF2 or 0xF3))
            {
                break;
            }
        }

        return true;
    }
}
