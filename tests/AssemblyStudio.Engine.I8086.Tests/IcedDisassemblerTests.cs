using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using AssemblyStudio.Core;
using AssemblyStudio.Engine.I8086.Disassembly;
using Iced.Intel;
using Xunit;

namespace AssemblyStudio.Engine.I8086.Tests;

/// <summary>
/// Unit and oracle verification tests for <see cref="IcedDisassembler"/> (TASK-019, FR-053, FR-058; ADR-0003 §6).
/// Verifies NASM formatting, 8086-only filtering (rejection of 186+, 286+, 386+ extensions and undocumented opcodes),
/// and instruction length agreement against SingleStepTests oracle cases.
/// </summary>
public class IcedDisassemblerTests
{
    private readonly IcedDisassembler _disassembler = new();

    [Theory]
    [InlineData(new byte[] { 0x89, 0xD8 }, "mov ax, bx", 2)]
    [InlineData(new byte[] { 0x8A, 0x00 }, "mov al, [bx+si]", 2)]
    [InlineData(new byte[] { 0x88, 0x47, 0x05 }, "mov [bx+5], al", 3)]
    [InlineData(new byte[] { 0x01, 0xD8 }, "add ax, bx", 2)]
    [InlineData(new byte[] { 0x29, 0xD8 }, "sub ax, bx", 2)]
    [InlineData(new byte[] { 0x50 }, "push ax", 1)]
    [InlineData(new byte[] { 0x58 }, "pop ax", 1)]
    [InlineData(new byte[] { 0x90 }, "nop", 1)]
    [InlineData(new byte[] { 0xF4 }, "hlt", 1)]
    [InlineData(new byte[] { 0xC3 }, "ret", 1)]
    [InlineData(new byte[] { 0xCB }, "retf", 1)]
    [InlineData(new byte[] { 0xCF }, "iret", 1)]
    [InlineData(new byte[] { 0xFA }, "cli", 1)]
    [InlineData(new byte[] { 0xFB }, "sti", 1)]
    [InlineData(new byte[] { 0xF8 }, "clc", 1)]
    [InlineData(new byte[] { 0xF9 }, "stc", 1)]
    [InlineData(new byte[] { 0xFC }, "cld", 1)]
    [InlineData(new byte[] { 0xFD }, "std", 1)]
    [InlineData(new byte[] { 0xF5 }, "cmc", 1)]
    [InlineData(new byte[] { 0x9B }, "wait", 1)]
    [InlineData(new byte[] { 0xCD, 0x21 }, "int 21h", 2)]
    [InlineData(new byte[] { 0xCC }, "int3", 1)]
    [InlineData(new byte[] { 0xCE }, "into", 1)]
    [InlineData(new byte[] { 0xD7 }, "xlatb", 1)]
    [InlineData(new byte[] { 0x9E }, "sahf", 1)]
    [InlineData(new byte[] { 0x9F }, "lahf", 1)]
    [InlineData(new byte[] { 0xA4 }, "movsb", 1)]
    [InlineData(new byte[] { 0xA5 }, "movsw", 1)]
    public void Disassemble_formats_documented_8086_instructions_correctly(byte[] bytes, string expectedNasm, int expectedLength)
    {
        string? text = _disassembler.Disassemble(bytes);
        Assert.NotNull(text);
        Assert.Equal(expectedNasm, text);

        var (disText, length) = IcedDisassembler.DisassembleInstruction(bytes);
        Assert.Equal(expectedNasm, disText);
        Assert.Equal(expectedLength, length);
    }

    [Theory]
    [InlineData(new byte[] { 0xF3, 0xA4 }, "rep movsb", 2)]
    [InlineData(new byte[] { 0xF3, 0xA5 }, "rep movsw", 2)]
    [InlineData(new byte[] { 0xF3, 0xAB }, "rep stosw", 2)]
    [InlineData(new byte[] { 0xF3, 0xAE }, "repe scasb", 2)]
    [InlineData(new byte[] { 0xF2, 0xA6 }, "repne cmpsb", 2)]
    [InlineData(new byte[] { 0x2E, 0x8B, 0x07 }, "mov ax, [cs:bx]", 3)]
    [InlineData(new byte[] { 0x26, 0x8B, 0x07 }, "mov ax, [es:bx]", 3)]
    [InlineData(new byte[] { 0x36, 0x8B, 0x07 }, "mov ax, [ss:bx]", 3)]
    [InlineData(new byte[] { 0x3E, 0x8B, 0x07 }, "mov ax, [ds:bx]", 3)]
    public void Disassemble_handles_valid_8086_prefixes(byte[] bytes, string expectedNasm, int expectedLength)
    {
        string? text = _disassembler.Disassemble(bytes);
        Assert.NotNull(text);
        Assert.Equal(expectedNasm, text);

        var (disText, length) = IcedDisassembler.DisassembleInstruction(bytes);
        Assert.Equal(expectedNasm, disText);
        Assert.Equal(expectedLength, length);
    }

    [Fact]
    public void Disassemble_relative_branches_uses_ip()
    {
        // 0xEB 0x05 at IP 0x0100 -> target is 0x0100 + 2 + 5 = 0x0107
        byte[] jmpShort = [0xEB, 0x05];
        var (text, len) = IcedDisassembler.DisassembleInstruction(jmpShort, 0x0100);
        Assert.Equal(2, len);
        Assert.Equal("jmp short 0107h", text);

        // 0x74 0x0A at IP 0x0200 -> target is 0x0200 + 2 + 10 = 0x020C
        byte[] je = [0x74, 0x0A];
        var (jeText, jeLen) = IcedDisassembler.DisassembleInstruction(je, 0x0200);
        Assert.Equal(2, jeLen);
        Assert.Equal("je short 020ch", jeText);
    }

    [Fact]
    public void Disassemble_returns_null_on_empty_or_invalid_bytes()
    {
        Assert.Null(_disassembler.Disassemble([]));
        var (text, len) = IcedDisassembler.DisassembleInstruction([]);
        Assert.Null(text);
        Assert.Equal(0, len);
    }

    [Theory]
    [InlineData(new byte[] { 0x60 })]                     // PUSHA (186+)
    [InlineData(new byte[] { 0x61 })]                     // POPA (186+)
    [InlineData(new byte[] { 0x62, 0x00 })]               // BOUND (186+)
    [InlineData(new byte[] { 0x69, 0xC0, 0x01, 0x00 })]   // IMUL r16, imm16 (186+)
    [InlineData(new byte[] { 0xC0, 0xE0, 0x02 })]         // SHL r8, imm8 (186+)
    [InlineData(new byte[] { 0xC1, 0xE0, 0x02 })]         // SHL r16, imm8 (186+)
    [InlineData(new byte[] { 0xC8, 0x04, 0x00, 0x00 })]   // ENTER (186+)
    [InlineData(new byte[] { 0xC9 })]                     // LEAVE (186+)
    public void Disassemble_rejects_80186_extensions(byte[] bytes)
    {
        Assert.Null(_disassembler.Disassemble(bytes));
        var (text, len) = IcedDisassembler.DisassembleInstruction(bytes);
        Assert.Null(text);
        Assert.Equal(0, len);
    }

    [Theory]
    [InlineData(new byte[] { 0x0F })]                     // Lone 0x0F prefix
    [InlineData(new byte[] { 0x0F, 0x01, 0x10 })]         // LGDT (286+)
    [InlineData(new byte[] { 0x0F, 0x02, 0xC0 })]         // LAR (286+)
    [InlineData(new byte[] { 0x0F, 0x20, 0xC0 })]         // MOV EAX, CR0 (386+)
    [InlineData(new byte[] { 0x0F, 0xA0 })]               // PUSH FS (386+)
    [InlineData(new byte[] { 0x0F, 0xA8 })]               // PUSH GS (386+)
    public void Disassemble_rejects_286_and_386_two_byte_opcodes(byte[] bytes)
    {
        Assert.Null(_disassembler.Disassemble(bytes));
        var (text, len) = IcedDisassembler.DisassembleInstruction(bytes);
        Assert.Null(text);
        Assert.Equal(0, len);
    }

    [Theory]
    [InlineData(new byte[] { 0x64, 0x89, 0xD8 })]         // FS: MOV AX, BX (386+ FS prefix)
    [InlineData(new byte[] { 0x65, 0x89, 0xD8 })]         // GS: MOV AX, BX (386+ GS prefix)
    [InlineData(new byte[] { 0x66, 0x89, 0xD8 })]         // 0x66 operand size prefix (386+)
    [InlineData(new byte[] { 0x67, 0x89, 0xD8 })]         // 0x67 address size prefix (386+)
    public void Disassemble_rejects_non_8086_prefixes(byte[] bytes)
    {
        Assert.Null(_disassembler.Disassemble(bytes));
        var (text, len) = IcedDisassembler.DisassembleInstruction(bytes);
        Assert.Null(text);
        Assert.Equal(0, len);
    }

    [Theory]
    [InlineData(new byte[] { 0xD6 })]                     // SALC (undocumented, ADR-0003 §4 Unsupported)
    public void Disassemble_rejects_undocumented_opcodes_unsupported_in_engine(byte[] bytes)
    {
        Assert.Null(_disassembler.Disassemble(bytes));
        var (text, len) = IcedDisassembler.DisassembleInstruction(bytes);
        Assert.Null(text);
        Assert.Equal(0, len);
    }

    [Fact]
    public void Adr0003_unverified_items_verified()
    {
        // ADR-0003 unverified claim: "Iced 1.21.0 on .NET 10 and whether INTEL8086_ONLY covers POP CS"
        // 1. Verify INTEL8086_ONLY exists in Iced CpuidFeature enum
        Assert.True(Enum.IsDefined(CpuidFeature.INTEL8086_ONLY));

        // 2. Verify that Code.Popw_CS is tagged with INTEL8086_ONLY
        var popwCs = Instruction.Create(Code.Popw_CS);
        Assert.Contains(CpuidFeature.INTEL8086_ONLY, popwCs.CpuidFeatures);

        // 3. Verify that 0x0F byte alone or followed by a second byte is NOT decoded as POP CS by Iced's 16-bit decoder
        // (0x0F lone byte is invalid, 0x0F followed by byte is 286+ two-byte escape sequence)
        byte[] lone0F = [0x0F];
        var (text0F, _) = IcedDisassembler.DisassembleInstruction(lone0F);
        Assert.Null(text0F); // Confirms disassembler returns null for 0x0F, matching engine halt
    }

    [Theory]
    [InlineData("00.json.gz")] // ADD
    [InlineData("01.json.gz")] // ADD
    [InlineData("28.json.gz")] // SUB
    [InlineData("38.json.gz")] // CMP
    [InlineData("50.json.gz")] // PUSH
    [InlineData("58.json.gz")] // POP
    [InlineData("88.json.gz")] // MOV rm8, r8
    [InlineData("89.json.gz")] // MOV rm16, r16
    [InlineData("8A.json.gz")] // MOV r8, rm8
    [InlineData("8B.json.gz")] // MOV r16, rm16
    [InlineData("A4.json.gz")] // MOVSB
    [InlineData("A5.json.gz")] // MOVSW
    [InlineData("B8.json.gz")] // MOV AX, imm16
    [InlineData("D0.0.json.gz")] // ROL rm8, 1
    [InlineData("D1.0.json.gz")] // ROL rm16, 1
    [InlineData("F4.json.gz")] // HLT
    [InlineData("F8.json.gz")] // CLC
    public void Instruction_lengths_agree_with_single_step_test_oracle_cases(string fileName)
    {
        string dir = FindTestDataDir();
        string path = Path.Combine(dir, fileName);
        if (!File.Exists(path))
        {
            return;
        }

        using var stream = File.OpenRead(path);
        using var gz = new GZipStream(stream, CompressionMode.Decompress);
        using var doc = JsonDocument.Parse(gz);

        foreach (var test in doc.RootElement.EnumerateArray())
        {
            var rawBytes = test.GetProperty("bytes").EnumerateArray().Select(b => b.GetByte()).ToArray();
            var (text, length) = IcedDisassembler.DisassembleInstruction(rawBytes);

            Assert.NotNull(text);
            Assert.Equal(rawBytes.Length, length);
        }
    }

    private static string FindTestDataDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "testdata", "singlesteptests", "8086")))
        {
            dir = dir.Parent;
        }
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "testdata", "singlesteptests", "8086");
    }
}
