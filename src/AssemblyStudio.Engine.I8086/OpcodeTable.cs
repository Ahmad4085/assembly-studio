using System.Collections.ObjectModel;

namespace AssemblyStudio.Engine.I8086;

/// <summary>Status of an 8086 opcode byte in AssemblyStudio (TASK-018, FR-036, FR-037, FR-045).</summary>
public enum OpcodeStatus : byte
{
    /// <summary>Instruction is supported and executes per 8086 semantics.</summary>
    Supported,

    /// <summary>Byte is an instruction prefix (segment override, repeat, or lock).</summary>
    Prefix,

    /// <summary>Opcode is unsupported in the MVP (halts with UnsupportedInstruction per FR-037).</summary>
    Unsupported,
}

/// <summary>Category of instruction or byte encoding.</summary>
public enum OpcodeCategory : byte
{
    DataTransfer,
    Arithmetic,
    Logic,
    ShiftRotate,
    MulDivBcd,
    ControlTransfer,
    StringOperation,
    FlagOps,
    ProcessorControl,
    Prefix,
    CoprocessorEsc,
    PortIo,
    UndocumentedOrAlias,
}

/// <summary>Record describing one 8086 opcode byte.</summary>
/// <param name="Opcode">The 8-bit opcode value (0x00..0xFF).</param>
/// <param name="Mnemonic">Standard mnemonic or descriptive name.</param>
/// <param name="Status">Support status in AssemblyStudio.</param>
/// <param name="Category">Functional category.</param>
/// <param name="Notes">Detailed semantics, hardware quirks, or reason for unsupported status.</param>
public readonly record struct OpcodeEntry(
    byte Opcode,
    string Mnemonic,
    OpcodeStatus Status,
    OpcodeCategory Category,
    string Notes);

/// <summary>
/// Exhaustive table of all 256 opcode bytes for the 8086 CPU engine (TASK-018, FR-036, FR-045).
/// Every opcode byte is explicitly classified as Supported, Prefix, or Unsupported.
/// </summary>
public static class OpcodeTable
{
    private static readonly ReadOnlyCollection<OpcodeEntry> AllEntries = new(BuildTable());

    /// <summary>All 256 opcode entries, indexed by opcode byte (0x00..0xFF).</summary>
    public static IReadOnlyList<OpcodeEntry> Entries => AllEntries;

    /// <summary>Gets the entry for the specified opcode byte.</summary>
    public static OpcodeEntry Get(byte opcode) => AllEntries[opcode];

    /// <summary>Returns true if the opcode is supported and executable in the engine.</summary>
    public static bool IsSupported(byte opcode) => AllEntries[opcode].Status == OpcodeStatus.Supported;

    /// <summary>Returns true if the opcode byte is an instruction prefix.</summary>
    public static bool IsPrefix(byte opcode) => AllEntries[opcode].Status == OpcodeStatus.Prefix;

    /// <summary>Returns true if the opcode byte is unsupported and halts per FR-037.</summary>
    public static bool IsUnsupported(byte opcode) => AllEntries[opcode].Status == OpcodeStatus.Unsupported;

    private static OpcodeEntry[] BuildTable()
    {
        var t = new OpcodeEntry[256];

        // 00-05: ADD
        t[0x00] = new(0x00, "ADD rm8, r8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Standard arithmetic ADD");
        t[0x01] = new(0x01, "ADD rm16, r16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Standard arithmetic ADD");
        t[0x02] = new(0x02, "ADD r8, rm8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Standard arithmetic ADD");
        t[0x03] = new(0x03, "ADD r16, rm16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Standard arithmetic ADD");
        t[0x04] = new(0x04, "ADD AL, imm8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Standard arithmetic ADD");
        t[0x05] = new(0x05, "ADD AX, imm16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Standard arithmetic ADD");

        // 06-07: PUSH/POP ES
        t[0x06] = new(0x06, "PUSH ES", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Pushes segment register ES onto stack");
        t[0x07] = new(0x07, "POP ES", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Pops top of stack into segment register ES");

        // 08-0D: OR
        t[0x08] = new(0x08, "OR rm8, r8", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical OR");
        t[0x09] = new(0x09, "OR rm16, r16", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical OR");
        t[0x0A] = new(0x0A, "OR r8, rm8", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical OR");
        t[0x0B] = new(0x0B, "OR r16, rm16", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical OR");
        t[0x0C] = new(0x0C, "OR AL, imm8", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical OR");
        t[0x0D] = new(0x0D, "OR AX, imm16", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical OR");

        // 0E: PUSH CS, 0F: POP CS / 286+ escape
        t[0x0E] = new(0x0E, "PUSH CS", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Pushes segment register CS onto stack");
        t[0x0F] = new(0x0F, "POP CS / 286+ escape", OpcodeStatus.Unsupported, OpcodeCategory.UndocumentedOrAlias, "8086 POP CS alias / 286+ two-byte escape; unsupported in MVP");

        // 10-15: ADC
        t[0x10] = new(0x10, "ADC rm8, r8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Add with Carry");
        t[0x11] = new(0x11, "ADC rm16, r16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Add with Carry");
        t[0x12] = new(0x12, "ADC r8, rm8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Add with Carry");
        t[0x13] = new(0x13, "ADC r16, rm16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Add with Carry");
        t[0x14] = new(0x14, "ADC AL, imm8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Add with Carry");
        t[0x15] = new(0x15, "ADC AX, imm16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Add with Carry");

        // 16-17: PUSH/POP SS
        t[0x16] = new(0x16, "PUSH SS", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Pushes segment register SS onto stack");
        t[0x17] = new(0x17, "POP SS", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Pops top of stack into segment register SS");

        // 18-1D: SBB
        t[0x18] = new(0x18, "SBB rm8, r8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Subtract with Borrow");
        t[0x19] = new(0x19, "SBB rm16, r16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Subtract with Borrow");
        t[0x1A] = new(0x1A, "SBB r8, rm8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Subtract with Borrow");
        t[0x1B] = new(0x1B, "SBB r16, rm16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Subtract with Borrow");
        t[0x1C] = new(0x1C, "SBB AL, imm8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Subtract with Borrow");
        t[0x1D] = new(0x1D, "SBB AX, imm16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Subtract with Borrow");

        // 1E-1F: PUSH/POP DS
        t[0x1E] = new(0x1E, "PUSH DS", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Pushes segment register DS onto stack");
        t[0x1F] = new(0x1F, "POP DS", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Pops top of stack into segment register DS");

        // 20-25: AND
        t[0x20] = new(0x20, "AND rm8, r8", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical AND");
        t[0x21] = new(0x21, "AND rm16, r16", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical AND");
        t[0x22] = new(0x22, "AND r8, rm8", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical AND");
        t[0x23] = new(0x23, "AND r16, rm16", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical AND");
        t[0x24] = new(0x24, "AND AL, imm8", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical AND");
        t[0x25] = new(0x25, "AND AX, imm16", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical AND");

        // 26: ES:
        t[0x26] = new(0x26, "ES:", OpcodeStatus.Prefix, OpcodeCategory.Prefix, "Segment override prefix for ES");

        // 27: DAA
        t[0x27] = new(0x27, "DAA", OpcodeStatus.Supported, OpcodeCategory.MulDivBcd, "Decimal Adjust AL after Addition");

        // 28-2D: SUB
        t[0x28] = new(0x28, "SUB rm8, r8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Standard arithmetic SUB");
        t[0x29] = new(0x29, "SUB rm16, r16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Standard arithmetic SUB");
        t[0x2A] = new(0x2A, "SUB r8, rm8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Standard arithmetic SUB");
        t[0x2B] = new(0x2B, "SUB r16, rm16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Standard arithmetic SUB");
        t[0x2C] = new(0x2C, "SUB AL, imm8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Standard arithmetic SUB");
        t[0x2D] = new(0x2D, "SUB AX, imm16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Standard arithmetic SUB");

        // 2E: CS:
        t[0x2E] = new(0x2E, "CS:", OpcodeStatus.Prefix, OpcodeCategory.Prefix, "Segment override prefix for CS");

        // 2F: DAS
        t[0x2F] = new(0x2F, "DAS", OpcodeStatus.Supported, OpcodeCategory.MulDivBcd, "Decimal Adjust AL after Subtraction");

        // 30-35: XOR
        t[0x30] = new(0x30, "XOR rm8, r8", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical XOR");
        t[0x31] = new(0x31, "XOR rm16, r16", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical XOR");
        t[0x32] = new(0x32, "XOR r8, rm8", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical XOR");
        t[0x33] = new(0x33, "XOR r16, rm16", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical XOR");
        t[0x34] = new(0x34, "XOR AL, imm8", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical XOR");
        t[0x35] = new(0x35, "XOR AX, imm16", OpcodeStatus.Supported, OpcodeCategory.Logic, "Bitwise logical XOR");

        // 36: SS:
        t[0x36] = new(0x36, "SS:", OpcodeStatus.Prefix, OpcodeCategory.Prefix, "Segment override prefix for SS");

        // 37: AAA
        t[0x37] = new(0x37, "AAA", OpcodeStatus.Supported, OpcodeCategory.MulDivBcd, "ASCII Adjust AL after Addition");

        // 38-3D: CMP
        t[0x38] = new(0x38, "CMP rm8, r8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Compare operands (SUB without storing result)");
        t[0x39] = new(0x39, "CMP rm16, r16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Compare operands (SUB without storing result)");
        t[0x3A] = new(0x3A, "CMP r8, rm8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Compare operands (SUB without storing result)");
        t[0x3B] = new(0x3B, "CMP r16, rm16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Compare operands (SUB without storing result)");
        t[0x3C] = new(0x3C, "CMP AL, imm8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Compare operands (SUB without storing result)");
        t[0x3D] = new(0x3D, "CMP AX, imm16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Compare operands (SUB without storing result)");

        // 3E: DS:
        t[0x3E] = new(0x3E, "DS:", OpcodeStatus.Prefix, OpcodeCategory.Prefix, "Segment override prefix for DS");

        // 3F: AAS
        t[0x3F] = new(0x3F, "AAS", OpcodeStatus.Supported, OpcodeCategory.MulDivBcd, "ASCII Adjust AL after Subtraction");

        // 40-47: INC r16
        string[] r16Names = ["AX", "CX", "DX", "BX", "SP", "BP", "SI", "DI"];
        for (int i = 0; i < 8; i++)
        {
            t[0x40 + i] = new((byte)(0x40 + i), $"INC {r16Names[i]}", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Increment register (preserves CF)");
            t[0x48 + i] = new((byte)(0x48 + i), $"DEC {r16Names[i]}", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Decrement register (preserves CF)");
            t[0x50 + i] = new((byte)(0x50 + i), $"PUSH {r16Names[i]}", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Push register to stack (PUSH SP pushes decremented SP)");
            t[0x58 + i] = new((byte)(0x58 + i), $"POP {r16Names[i]}", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Pop top of stack into register");
        }

        // 60-6F: 186+ extensions
        for (int i = 0x60; i <= 0x6F; i++)
        {
            t[i] = new((byte)i, "80186+ opcode", OpcodeStatus.Unsupported, OpcodeCategory.UndocumentedOrAlias, "80186+ instruction extension (PUSHA/POPA/BOUND/IMUL imm); unsupported on 8086 MVP");
        }

        // 70-7F: Jcc
        string[] jccNames = ["JO", "JNO", "JB", "JNB", "JZ", "JNZ", "JBE", "JA", "JS", "JNS", "JP", "JNP", "JL", "JGE", "JLE", "JG"];
        for (int i = 0; i < 16; i++)
        {
            t[0x70 + i] = new((byte)(0x70 + i), $"{jccNames[i]} rel8", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Conditional jump with 8-bit sign-extended displacement");
        }

        // 80-83: Group 1 Arith imm
        t[0x80] = new(0x80, "GRP1 rm8, imm8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Immediate group 1: ADD/OR/ADC/SBB/AND/SUB/XOR/CMP");
        t[0x81] = new(0x81, "GRP1 rm16, imm16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Immediate group 1: ADD/OR/ADC/SBB/AND/SUB/XOR/CMP");
        t[0x82] = new(0x82, "GRP1 rm8, imm8 (alias of 80)", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Undocumented 8086 alias of opcode 80");
        t[0x83] = new(0x83, "GRP1 rm16, imm8 (sign-ext)", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Immediate group 1 with sign-extended 8-bit immediate");

        // 84-85: TEST
        t[0x84] = new(0x84, "TEST rm8, r8", OpcodeStatus.Supported, OpcodeCategory.Logic, "Logical compare (AND without storing result)");
        t[0x85] = new(0x85, "TEST rm16, r16", OpcodeStatus.Supported, OpcodeCategory.Logic, "Logical compare (AND without storing result)");

        // 86-87: XCHG
        t[0x86] = new(0x86, "XCHG rm8, r8", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Exchange register and r/m");
        t[0x87] = new(0x87, "XCHG rm16, r16", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Exchange register and r/m");

        // 88-8B: MOV
        t[0x88] = new(0x88, "MOV rm8, r8", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Move byte from reg to r/m");
        t[0x89] = new(0x89, "MOV rm16, r16", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Move word from reg to r/m");
        t[0x8A] = new(0x8A, "MOV r8, rm8", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Move byte from r/m to reg");
        t[0x8B] = new(0x8B, "MOV r16, rm16", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Move word from r/m to reg");

        // 8C: MOV rm16, Sreg
        t[0x8C] = new(0x8C, "MOV rm16, Sreg", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Move segment register to r/m (reg&3 aliases to ES/CS/SS/DS)");

        // 8D: LEA
        t[0x8D] = new(0x8D, "LEA r16, m", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Load effective address offset (mod=11 halts)");

        // 8E: MOV Sreg, rm16
        t[0x8E] = new(0x8E, "MOV Sreg, rm16", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Move r/m to segment register");

        // 8F: POP rm16
        t[0x8F] = new(0x8F, "POP rm16", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Pop top of stack to r/m (all reg values alias to /0)");

        // 90-97: XCHG AX, r16 (90 = NOP)
        t[0x90] = new(0x90, "NOP", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "No operation (encoded as XCHG AX, AX)");
        for (int i = 1; i < 8; i++)
        {
            t[0x90 + i] = new((byte)(0x90 + i), $"XCHG AX, {r16Names[i]}", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Exchange accumulator with register");
        }

        // 98-99: CBW/CWD
        t[0x98] = new(0x98, "CBW", OpcodeStatus.Supported, OpcodeCategory.MulDivBcd, "Convert Byte to Word (sign-extend AL into AX)");
        t[0x99] = new(0x99, "CWD", OpcodeStatus.Supported, OpcodeCategory.MulDivBcd, "Convert Word to Doubleword (sign-extend AX into DX:AX)");

        // 9A: CALL far
        t[0x9A] = new(0x9A, "CALL ptr16:16", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Direct intersegment (far) procedure call");

        // 9B: WAIT
        t[0x9B] = new(0x9B, "WAIT / FWAIT", OpcodeStatus.Supported, OpcodeCategory.ProcessorControl, "Wait for coprocessor TEST pin (no-op in 8086 MVP)");

        // 9C-9D: PUSHF/POPF
        t[0x9C] = new(0x9C, "PUSHF", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Push FLAGS verbatim onto stack");
        t[0x9D] = new(0x9D, "POPF", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Pop top of stack into FLAGS (canonical reserved bits forced)");

        // 9E-9F: SAHF/LAHF
        t[0x9E] = new(0x9E, "SAHF", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Store AH into low byte of FLAGS");
        t[0x9F] = new(0x9F, "LAHF", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Load low byte of FLAGS into AH");

        // A0-A3: MOV moffs
        t[0xA0] = new(0xA0, "MOV AL, [moffs8]", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Move byte from direct memory offset to AL");
        t[0xA1] = new(0xA1, "MOV AX, [moffs16]", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Move word from direct memory offset to AX");
        t[0xA2] = new(0xA2, "MOV [moffs8], AL", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Move byte from AL to direct memory offset");
        t[0xA3] = new(0xA3, "MOV [moffs16], AX", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Move word from AX to direct memory offset");

        // A4-A7: MOVS/CMPS
        t[0xA4] = new(0xA4, "MOVSB", OpcodeStatus.Supported, OpcodeCategory.StringOperation, "Move byte from [DS:SI] to [ES:DI] (ES unoverridable)");
        t[0xA5] = new(0xA5, "MOVSW", OpcodeStatus.Supported, OpcodeCategory.StringOperation, "Move word from [DS:SI] to [ES:DI] (ES unoverridable)");
        t[0xA6] = new(0xA6, "CMPSB", OpcodeStatus.Supported, OpcodeCategory.StringOperation, "Compare byte [DS:SI] with [ES:DI]");
        t[0xA7] = new(0xA7, "CMPSW", OpcodeStatus.Supported, OpcodeCategory.StringOperation, "Compare word [DS:SI] with [ES:DI]");

        // A8-A9: TEST acc, imm
        t[0xA8] = new(0xA8, "TEST AL, imm8", OpcodeStatus.Supported, OpcodeCategory.Logic, "Logical compare imm8 with AL");
        t[0xA9] = new(0xA9, "TEST AX, imm16", OpcodeStatus.Supported, OpcodeCategory.Logic, "Logical compare imm16 with AX");

        // AA-AF: STOS/LODS/SCAS
        t[0xAA] = new(0xAA, "STOSB", OpcodeStatus.Supported, OpcodeCategory.StringOperation, "Store AL to [ES:DI]");
        t[0xAB] = new(0xAB, "STOSW", OpcodeStatus.Supported, OpcodeCategory.StringOperation, "Store AX to [ES:DI]");
        t[0xAC] = new(0xAC, "LODSB", OpcodeStatus.Supported, OpcodeCategory.StringOperation, "Load byte from [DS:SI] to AL");
        t[0xAD] = new(0xAD, "LODSW", OpcodeStatus.Supported, OpcodeCategory.StringOperation, "Load word from [DS:SI] to AX");
        t[0xAE] = new(0xAE, "SCASB", OpcodeStatus.Supported, OpcodeCategory.StringOperation, "Compare AL with [ES:DI]");
        t[0xAF] = new(0xAF, "SCASW", OpcodeStatus.Supported, OpcodeCategory.StringOperation, "Compare AX with [ES:DI]");

        // B0-BF: MOV reg, imm
        string[] r8Names = ["AL", "CL", "DL", "BL", "AH", "CH", "DH", "BH"];
        for (int i = 0; i < 8; i++)
        {
            t[0xB0 + i] = new((byte)(0xB0 + i), $"MOV {r8Names[i]}, imm8", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Move immediate byte to register");
            t[0xB8 + i] = new((byte)(0xB8 + i), $"MOV {r16Names[i]}, imm16", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Move immediate word to register");
        }

        // C0-C1: 186+ shifts
        t[0xC0] = new(0xC0, "80186+ shift rm8, imm8", OpcodeStatus.Unsupported, OpcodeCategory.UndocumentedOrAlias, "80186+ shift with immediate count; unsupported on 8086 MVP");
        t[0xC1] = new(0xC1, "80186+ shift rm16, imm8", OpcodeStatus.Unsupported, OpcodeCategory.UndocumentedOrAlias, "80186+ shift with immediate count; unsupported on 8086 MVP");

        // C2-C3: RET near
        t[0xC2] = new(0xC2, "RET near imm16", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Intrasegment return and pop imm16 bytes");
        t[0xC3] = new(0xC3, "RET near", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Intrasegment return to calling procedure");

        // C4-C5: LES/LDS
        t[0xC4] = new(0xC4, "LES r16, m16:16", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Load pointer into ES and destination register");
        t[0xC5] = new(0xC5, "LDS r16, m16:16", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Load pointer into DS and destination register");

        // C6-C7: MOV rm, imm
        t[0xC6] = new(0xC6, "MOV rm8, imm8", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Move immediate byte to r/m (all reg alias to /0)");
        t[0xC7] = new(0xC7, "MOV rm16, imm16", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Move immediate word to r/m (all reg alias to /0)");

        // C8-C9: 186+ ENTER/LEAVE
        t[0xC8] = new(0xC8, "80186+ ENTER", OpcodeStatus.Unsupported, OpcodeCategory.UndocumentedOrAlias, "80186+ ENTER stack frame; unsupported on 8086 MVP");
        t[0xC9] = new(0xC9, "80186+ LEAVE", OpcodeStatus.Unsupported, OpcodeCategory.UndocumentedOrAlias, "80186+ LEAVE stack frame; unsupported on 8086 MVP");

        // CA-CB: RET far
        t[0xCA] = new(0xCA, "RET far imm16", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Intersegment return and pop imm16 bytes");
        t[0xCB] = new(0xCB, "RET far", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Intersegment return to calling procedure");

        // CC-CF: Interrupts
        t[0xCC] = new(0xCC, "INT 3", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Breakpoint interrupt (vector 3, router interception supported)");
        t[0xCD] = new(0xCD, "INT imm8", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Software interrupt (router interception supported)");
        t[0xCE] = new(0xCE, "INTO", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Interrupt on Overflow (vector 4 if OF=1)");
        t[0xCF] = new(0xCF, "IRET", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Interrupt return (pops IP, CS, and FLAGS)");

        // D0-D3: Group 2 Shifts/Rotates
        t[0xD0] = new(0xD0, "GRP2 rm8, 1", OpcodeStatus.Supported, OpcodeCategory.ShiftRotate, "Shift/rotate rm8 by 1 (ROL/ROR/RCL/RCR/SHL/SHR/SETMO/SAR)");
        t[0xD1] = new(0xD1, "GRP2 rm16, 1", OpcodeStatus.Supported, OpcodeCategory.ShiftRotate, "Shift/rotate rm16 by 1 (ROL/ROR/RCL/RCR/SHL/SHR/SETMO/SAR)");
        t[0xD2] = new(0xD2, "GRP2 rm8, CL", OpcodeStatus.Supported, OpcodeCategory.ShiftRotate, "Shift/rotate rm8 by CL (count not masked, OF only for count 1)");
        t[0xD3] = new(0xD3, "GRP2 rm16, CL", OpcodeStatus.Supported, OpcodeCategory.ShiftRotate, "Shift/rotate rm16 by CL (count not masked, OF only for count 1)");

        // D4-D5: AAM/AAD
        t[0xD4] = new(0xD4, "AAM imm8", OpcodeStatus.Supported, OpcodeCategory.MulDivBcd, "ASCII Adjust AX after Multiply (divisor 0 raises INT 0)");
        t[0xD5] = new(0xD5, "AAD imm8", OpcodeStatus.Supported, OpcodeCategory.MulDivBcd, "ASCII Adjust AX before Division");

        // D6: SALC alias
        t[0xD6] = new(0xD6, "SALC / SETALC", OpcodeStatus.Unsupported, OpcodeCategory.UndocumentedOrAlias, "Undocumented Set AL on Carry; unsupported in MVP");

        // D7: XLAT
        t[0xD7] = new(0xD7, "XLATB", OpcodeStatus.Supported, OpcodeCategory.DataTransfer, "Table lookup translation ([DS:BX+AL], override honored)");

        // D8-DF: Coprocessor ESC 0..7
        for (int i = 0xD8; i <= 0xDF; i++)
        {
            t[i] = new((byte)i, $"ESC {i - 0xD8}, rm", OpcodeStatus.Unsupported, OpcodeCategory.CoprocessorEsc, "8087 coprocessor instruction; unsupported in MVP");
        }

        // E0-E3: Loops and JCXZ
        t[0xE0] = new(0xE0, "LOOPNE rel8", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Decrement CX; jump if CX != 0 and ZF == 0");
        t[0xE1] = new(0xE1, "LOOPE rel8", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Decrement CX; jump if CX != 0 and ZF == 1");
        t[0xE2] = new(0xE2, "LOOP rel8", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Decrement CX; jump if CX != 0");
        t[0xE3] = new(0xE3, "JCXZ rel8", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Jump short if CX is zero (flags untouched)");

        // E4-E7: Fixed Port I/O
        t[0xE4] = new(0xE4, "IN AL, imm8", OpcodeStatus.Unsupported, OpcodeCategory.PortIo, "Port input from fixed port; unsupported in MVP");
        t[0xE5] = new(0xE5, "IN AX, imm8", OpcodeStatus.Unsupported, OpcodeCategory.PortIo, "Port input from fixed port; unsupported in MVP");
        t[0xE6] = new(0xE6, "OUT imm8, AL", OpcodeStatus.Unsupported, OpcodeCategory.PortIo, "Port output to fixed port; unsupported in MVP");
        t[0xE7] = new(0xE7, "OUT imm8, AX", OpcodeStatus.Unsupported, OpcodeCategory.PortIo, "Port output to fixed port; unsupported in MVP");

        // E8-EB: CALL and JMP
        t[0xE8] = new(0xE8, "CALL rel16", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Direct near call relative to next instruction");
        t[0xE9] = new(0xE9, "JMP rel16", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Direct near jump relative to next instruction");
        t[0xEA] = new(0xEA, "JMP ptr16:16", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Direct far jump to target segment and offset");
        t[0xEB] = new(0xEB, "JMP rel8", OpcodeStatus.Supported, OpcodeCategory.ControlTransfer, "Direct short jump with 8-bit sign-extended displacement");

        // EC-EF: Variable Port I/O
        t[0xEC] = new(0xEC, "IN AL, DX", OpcodeStatus.Unsupported, OpcodeCategory.PortIo, "Port input from DX port; unsupported in MVP");
        t[0xED] = new(0xED, "IN AX, DX", OpcodeStatus.Unsupported, OpcodeCategory.PortIo, "Port input from DX port; unsupported in MVP");
        t[0xEE] = new(0xEE, "OUT DX, AL", OpcodeStatus.Unsupported, OpcodeCategory.PortIo, "Port output to DX port; unsupported in MVP");
        t[0xEF] = new(0xEF, "OUT DX, AX", OpcodeStatus.Unsupported, OpcodeCategory.PortIo, "Port output to DX port; unsupported in MVP");

        // F0: LOCK prefix
        t[0xF0] = new(0xF0, "LOCK", OpcodeStatus.Prefix, OpcodeCategory.Prefix, "Bus lock prefix; ignored in MVP (ADR-0003 §4)");

        // F1: Undocumented ICEBP
        t[0xF1] = new(0xF1, "ICEBP / INT1", OpcodeStatus.Unsupported, OpcodeCategory.UndocumentedOrAlias, "Undocumented in-circuit emulator breakpoint; unsupported in MVP");

        // F2-F3: REP prefixes
        t[0xF2] = new(0xF2, "REPNE / REPNZ", OpcodeStatus.Prefix, OpcodeCategory.Prefix, "Repeat string operation while not equal");
        t[0xF3] = new(0xF3, "REP / REPE / REPZ", OpcodeStatus.Prefix, OpcodeCategory.Prefix, "Repeat string operation while equal");

        // F4: HLT
        t[0xF4] = new(0xF4, "HLT", OpcodeStatus.Supported, OpcodeCategory.ProcessorControl, "Halt CPU execution (HaltReason.Halt, ADR-0003 §4)");

        // F5: CMC
        t[0xF5] = new(0xF5, "CMC", OpcodeStatus.Supported, OpcodeCategory.FlagOps, "Complement Carry Flag (CF = ~CF)");

        // F6-F7: Group 3
        t[0xF6] = new(0xF6, "GRP3 rm8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Group 3: TEST, NOT, NEG, MUL, IMUL, DIV, IDIV (arm /1 unsupported)");
        t[0xF7] = new(0xF7, "GRP3 rm16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Group 3: TEST, NOT, NEG, MUL, IMUL, DIV, IDIV (arm /1 unsupported)");

        // F8-FD: Flag operations
        t[0xF8] = new(0xF8, "CLC", OpcodeStatus.Supported, OpcodeCategory.FlagOps, "Clear Carry Flag (CF = 0)");
        t[0xF9] = new(0xF9, "STC", OpcodeStatus.Supported, OpcodeCategory.FlagOps, "Set Carry Flag (CF = 1)");
        t[0xFA] = new(0xFA, "CLI", OpcodeStatus.Supported, OpcodeCategory.FlagOps, "Clear Interrupt Flag (IF = 0)");
        t[0xFB] = new(0xFB, "STI", OpcodeStatus.Supported, OpcodeCategory.FlagOps, "Set Interrupt Flag (IF = 1)");
        t[0xFC] = new(0xFC, "CLD", OpcodeStatus.Supported, OpcodeCategory.FlagOps, "Clear Direction Flag (DF = 0)");
        t[0xFD] = new(0xFD, "STD", OpcodeStatus.Supported, OpcodeCategory.FlagOps, "Set Direction Flag (DF = 1)");

        // FE-FF: Group 4 and Group 5
        t[0xFE] = new(0xFE, "GRP4 rm8", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Group 4: /0 INC rm8, /1 DEC rm8 (arms /2../7 unsupported)");
        t[0xFF] = new(0xFF, "GRP5 rm16", OpcodeStatus.Supported, OpcodeCategory.Arithmetic, "Group 5: /0 INC, /1 DEC, /2 CALL near, /3 CALL far, /4 JMP near, /5 JMP far, /6 PUSH (arm /7 unsupported)");

        return t;
    }
}
