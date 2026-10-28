# 8086 Opcode Support and Deviations Reference

| Field | Value |
|---|---|
| Document | 8086 Opcode Support and Deviations Reference |
| Status | Approved |
| Version | 1.0 |
| Date | 2026-10-28 |
| Task | TASK-018 (FR-036, FR-037, FR-045, NFR-017) |
| Architecture / Decisions | `docs/03-architecture/architecture.md`, ADR-0003 |
| Verification | SingleStepTests 8086 oracle suite (283 files, 100%), `FlagsMiscAndOpcodeTableTests` |

This document serves as the authoritative implementation record and offline help reference (FR-045) for all 256 opcode bytes in the Intel 8086 CPU instruction set, documenting which opcodes are fully supported, which are instruction prefixes, and which are explicitly unsupported with defined deviation semantics.

---

## 1. Summary Statistics

| Category | Opcode Count | Percentage | Engine Behavior |
|---|---|---|---|
| **Supported Instructions** | 206 | 80.5% | Executes per 8086 semantics; returns `StepOutcome.Executed` (or `StepOutcome.Halted` with `HaltReason.Halt` for HLT). |
| **Instruction Prefixes** | 7 | 2.7% | Decoded by `Decoder.ParsePrefixes` (segment overrides, REP/REPNE, LOCK). |
| **Unsupported Opcodes** | 43 | 16.8% | Halts execution before the instruction; returns `StepOutcome.Halted` with `HaltReason.UnsupportedInstruction` and state preserved byte-identical (FR-037). |
| **Total Opcode Bytes** | **256** | **100.0%** | Exhaustively validated in `FlagsMiscAndOpcodeTableTests`. |

---

## 2. Policy and Architectural Decisions (ADR-0003 §4)

1. **`HLT` (Opcode 0xF4):**
   - Deliberate processor halt. Advances `IP` past the `HLT` opcode (offset + 1).
   - Returns `StepOutcome.Halted` with `HaltReason.Halt`. Registers and memory remain intact.
2. **`WAIT` / `FWAIT` (Opcode 0x9B):**
   - In 8086 systems without an 8087 numeric coprocessor, the `TEST` line is inactive; `WAIT` is a no-op.
   - Advances `IP` by 1; registers and flags are completely untouched. Returns `StepOutcome.Executed`.
3. **`LOCK` (Opcode 0xF0):**
   - Captured by `Decoder.ParsePrefixes` and ignored in emulation (single-threaded CPU model per ADR-0003 §4).
4. **Coprocessor `ESC` instructions (Opcodes 0xD8..0xDF):**
   - 8087 coprocessor instructions (`ESC 0..7, ModR/M`) are out of scope for the MVP.
   - Halts execution with `HaltReason.UnsupportedInstruction` and pre-step state preserved (FR-037).
5. **Port I/O instructions (Opcodes 0xE4..0xE7, 0xEC..0xEF):**
   - Peripheral I/O port instructions (`IN AL/AX, imm8/DX` and `OUT imm8/DX, AL/AX`) are unsupported in MVP.
   - Halts execution with `HaltReason.UnsupportedInstruction` and pre-step state preserved (FR-037, NFR-013).
6. **Undocumented and alias opcodes:**
   - 80186+ instruction extensions (`0x60..0x6F`, `0xC0..0xC1`, `0xC8..0xC9`): unsupported in MVP.
   - Undocumented aliases (`0x0F` POP CS, `0xD6` SALC, `0xF1` ICEBP): unsupported in MVP.
   - Every undocumented opcode halts with `HaltReason.UnsupportedInstruction` per FR-037.
7. **Documented 8086 Hardware Quirks (SingleStepTests-verified):**
   - `PUSH SP` (0x54 and `FF /6` reg form): pushes the decremented SP.
   - `POP SP` (0x5C and `8F` rm form): loads SP with the popped value from the stack.
   - `PUSHF` (0x9C): pushes FLAGS verbatim without masking.
   - `POPF` (0x9D): forces canonical 8086 reserved bits (`(value | 0xF002) & ~0x0028`).
   - `8C`/`8E` (MOV Sreg): `reg & 3` aliases to ES (0), CS (1), SS (2), DS (3).
   - `8F`, `C6`, `C7`: reg fields alias to `/0` behavior.
   - `D0..D3 /6`: undocumented SETMO sets operand to all ones (0xFF/0xFFFF); raw flags match hardware.
   - `IDIV`: division overflow pushes 8086 microcode loop FLAGS on stack; returns vector 0 via router.
   - String instructions (`A4..A7`, `AA..AF`): Destination `[ES:DI]` is unoverridable; Source `[DS:SI]` honors segment overrides.

---

## 3. Group ModR/M Reg Arm Specification

For opcodes that use the `reg` field of the ModR/M byte as an opcode extension:

| Opcode(s) | Reg /0 | Reg /1 | Reg /2 | Reg /3 | Reg /4 | Reg /5 | Reg /6 | Reg /7 |
|---|---|---|---|---|---|---|---|---|
| **80, 81, 82, 83** | ADD (✔) | OR (✔) | ADC (✔) | SBB (✔) | AND (✔) | SUB (✔) | XOR (✔) | CMP (✔) |
| **D0, D1, D2, D3** | ROL (✔) | ROR (✔) | RCL (✔) | RCR (✔) | SHL (✔) | SHR (✔) | SETMO (✔) | SAR (✔) |
| **F6, F7** | TEST (✔) | TEST alias (❌) | NOT (✔) | NEG (✔) | MUL (✔) | IMUL (✔) | DIV (✔) | IDIV (✔) |
| **FE** | INC rm8 (✔) | DEC rm8 (✔) | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **FF** | INC rm16 (✔) | DEC rm16 (✔) | CALL near (✔) | CALL far (✔) | JMP near (✔) | JMP far (✔) | PUSH rm16 (✔) | ❌ |

*(✔ = Supported and executed; ❌ = Unsupported, halts with `HaltReason.UnsupportedInstruction`)*

---

## 4. Full 256-Opcode Reference Table (0x00 – 0xFF)

| Hex | Mnemonic | Status | Category | Task | Notes |
|---|---|---|---|---|---|
| `00` | ADD rm8, r8 | Supported | Arithmetic | TASK-013 | Standard arithmetic byte addition |
| `01` | ADD rm16, r16 | Supported | Arithmetic | TASK-013 | Standard arithmetic word addition |
| `02` | ADD r8, rm8 | Supported | Arithmetic | TASK-013 | Standard arithmetic byte addition |
| `03` | ADD r16, rm16 | Supported | Arithmetic | TASK-013 | Standard arithmetic word addition |
| `04` | ADD AL, imm8 | Supported | Arithmetic | TASK-013 | Fast accumulator byte addition |
| `05` | ADD AX, imm16 | Supported | Arithmetic | TASK-013 | Fast accumulator word addition |
| `06` | PUSH ES | Supported | DataTransfer | TASK-012 | Push segment register ES |
| `07` | POP ES | Supported | DataTransfer | TASK-012 | Pop stack top into ES |
| `08` | OR rm8, r8 | Supported | Logic | TASK-014 | Bitwise logical OR |
| `09` | OR rm16, r16 | Supported | Logic | TASK-014 | Bitwise logical OR |
| `0A` | OR r8, rm8 | Supported | Logic | TASK-014 | Bitwise logical OR |
| `0B` | OR r16, rm16 | Supported | Logic | TASK-014 | Bitwise logical OR |
| `0C` | OR AL, imm8 | Supported | Logic | TASK-014 | Fast accumulator logical OR |
| `0D` | OR AX, imm16 | Supported | Logic | TASK-014 | Fast accumulator logical OR |
| `0E` | PUSH CS | Supported | DataTransfer | TASK-012 | Push segment register CS |
| `0F` | POP CS alias / 286+ | Unsupported | UndocumentedOrAlias | TASK-018 | 8086 alias / 286+ escape; unsupported in MVP |
| `10` | ADC rm8, r8 | Supported | Arithmetic | TASK-013 | Add with carry byte |
| `11` | ADC rm16, r16 | Supported | Arithmetic | TASK-013 | Add with carry word |
| `12` | ADC r8, rm8 | Supported | Arithmetic | TASK-013 | Add with carry byte |
| `13` | ADC r16, rm16 | Supported | Arithmetic | TASK-013 | Add with carry word |
| `14` | ADC AL, imm8 | Supported | Arithmetic | TASK-013 | Fast accumulator add with carry |
| `15` | ADC AX, imm16 | Supported | Arithmetic | TASK-013 | Fast accumulator add with carry |
| `16` | PUSH SS | Supported | DataTransfer | TASK-012 | Push segment register SS |
| `17` | POP SS | Supported | DataTransfer | TASK-012 | Pop stack top into SS |
| `18` | SBB rm8, r8 | Supported | Arithmetic | TASK-013 | Subtract with borrow byte |
| `19` | SBB rm16, r16 | Supported | Arithmetic | TASK-013 | Subtract with borrow word |
| `1A` | SBB r8, rm8 | Supported | Arithmetic | TASK-013 | Subtract with borrow byte |
| `1B` | SBB r16, rm16 | Supported | Arithmetic | TASK-013 | Subtract with borrow word |
| `1C` | SBB AL, imm8 | Supported | Arithmetic | TASK-013 | Fast accumulator subtract with borrow |
| `1D` | SBB AX, imm16 | Supported | Arithmetic | TASK-013 | Fast accumulator subtract with borrow |
| `1E` | PUSH DS | Supported | DataTransfer | TASK-012 | Push segment register DS |
| `1F` | POP DS | Supported | DataTransfer | TASK-012 | Pop stack top into DS |
| `20` | AND rm8, r8 | Supported | Logic | TASK-014 | Bitwise logical AND |
| `21` | AND rm16, r16 | Supported | Logic | TASK-014 | Bitwise logical AND |
| `22` | AND r8, rm8 | Supported | Logic | TASK-014 | Bitwise logical AND |
| `23` | AND r16, rm16 | Supported | Logic | TASK-014 | Bitwise logical AND |
| `24` | AND AL, imm8 | Supported | Logic | TASK-014 | Fast accumulator logical AND |
| `25` | AND AX, imm16 | Supported | Logic | TASK-014 | Fast accumulator logical AND |
| `26` | ES: | Prefix | Prefix | TASK-011 | Segment override prefix ES |
| `27` | DAA | Supported | MulDivBcd | TASK-015 | Decimal Adjust AL after Addition |
| `28` | SUB rm8, r8 | Supported | Arithmetic | TASK-013 | Standard arithmetic byte subtraction |
| `29` | SUB rm16, r16 | Supported | Arithmetic | TASK-013 | Standard arithmetic word subtraction |
| `2A` | SUB r8, rm8 | Supported | Arithmetic | TASK-013 | Standard arithmetic byte subtraction |
| `2B` | SUB r16, rm16 | Supported | Arithmetic | TASK-013 | Standard arithmetic word subtraction |
| `2C` | SUB AL, imm8 | Supported | Arithmetic | TASK-013 | Fast accumulator byte subtraction |
| `2D` | SUB AX, imm16 | Supported | Arithmetic | TASK-013 | Fast accumulator word subtraction |
| `2E` | CS: | Prefix | Prefix | TASK-011 | Segment override prefix CS |
| `2F` | DAS | Supported | MulDivBcd | TASK-015 | Decimal Adjust AL after Subtraction |
| `30` | XOR rm8, r8 | Supported | Logic | TASK-014 | Bitwise logical exclusive-OR |
| `31` | XOR rm16, r16 | Supported | Logic | TASK-014 | Bitwise logical exclusive-OR |
| `32` | XOR r8, rm8 | Supported | Logic | TASK-014 | Bitwise logical exclusive-OR |
| `33` | XOR r16, rm16 | Supported | Logic | TASK-014 | Bitwise logical exclusive-OR |
| `34` | XOR AL, imm8 | Supported | Logic | TASK-014 | Fast accumulator logical XOR |
| `35` | XOR AX, imm16 | Supported | Logic | TASK-014 | Fast accumulator logical XOR |
| `36` | SS: | Prefix | Prefix | TASK-011 | Segment override prefix SS |
| `37` | AAA | Supported | MulDivBcd | TASK-015 | ASCII Adjust AL after Addition |
| `38` | CMP rm8, r8 | Supported | Arithmetic | TASK-013 | Compare byte operands |
| `39` | CMP rm16, r16 | Supported | Arithmetic | TASK-013 | Compare word operands |
| `3A` | CMP r8, rm8 | Supported | Arithmetic | TASK-013 | Compare byte operands |
| `3B` | CMP r16, rm16 | Supported | Arithmetic | TASK-013 | Compare word operands |
| `3C` | CMP AL, imm8 | Supported | Arithmetic | TASK-013 | Fast accumulator byte compare |
| `3D` | CMP AX, imm16 | Supported | Arithmetic | TASK-013 | Fast accumulator word compare |
| `3E` | DS: | Prefix | Prefix | TASK-011 | Segment override prefix DS |
| `3F` | AAS | Supported | MulDivBcd | TASK-015 | ASCII Adjust AL after Subtraction |
| `40`..`47` | INC r16 | Supported | Arithmetic | TASK-013 | Increment 16-bit register (CF preserved) |
| `48`..`4F` | DEC r16 | Supported | Arithmetic | TASK-013 | Decrement 16-bit register (CF preserved) |
| `50`..`57` | PUSH r16 | Supported | DataTransfer | TASK-012 | Push 16-bit register (PUSH SP pushes decremented SP) |
| `58`..`5F` | POP r16 | Supported | DataTransfer | TASK-012 | Pop stack top into 16-bit register |
| `60`..`6F` | 80186+ extensions | Unsupported | UndocumentedOrAlias | TASK-018 | PUSHA/POPA/BOUND/IMUL imm; unsupported in MVP |
| `70`..`7F` | Jcc rel8 | Supported | ControlTransfer | TASK-016 | All 16 conditional jumps with 8-bit sign-extended disp |
| `80` | GRP1 rm8, imm8 | Supported | Arithmetic | TASK-013 | Immediate group 1: ADD/OR/ADC/SBB/AND/SUB/XOR/CMP |
| `81` | GRP1 rm16, imm16 | Supported | Arithmetic | TASK-013 | Immediate group 1: ADD/OR/ADC/SBB/AND/SUB/XOR/CMP |
| `82` | GRP1 rm8, imm8 | Supported | Arithmetic | TASK-013 | Undocumented 8086 alias of opcode 80 |
| `83` | GRP1 rm16, imm8 | Supported | Arithmetic | TASK-013 | Immediate group 1 with sign-extended 8-bit immediate |
| `84` | TEST rm8, r8 | Supported | Logic | TASK-014 | Logical compare byte operands |
| `85` | TEST rm16, r16 | Supported | Logic | TASK-014 | Logical compare word operands |
| `86` | XCHG rm8, r8 | Supported | DataTransfer | TASK-012 | Exchange register and byte r/m |
| `87` | XCHG rm16, r16 | Supported | DataTransfer | TASK-012 | Exchange register and word r/m |
| `88` | MOV rm8, r8 | Supported | DataTransfer | TASK-012 | Move byte reg to r/m |
| `89` | MOV rm16, r16 | Supported | DataTransfer | TASK-012 | Move word reg to r/m |
| `8A` | MOV r8, rm8 | Supported | DataTransfer | TASK-012 | Move byte r/m to reg |
| `8B` | MOV r16, rm16 | Supported | DataTransfer | TASK-012 | Move word r/m to reg |
| `8C` | MOV rm16, Sreg | Supported | DataTransfer | TASK-012 | Move segment register to r/m (`reg&3` aliases) |
| `8D` | LEA r16, m | Supported | DataTransfer | TASK-012 | Load effective address offset (mod=11 halts) |
| `8E` | MOV Sreg, rm16 | Supported | DataTransfer | TASK-012 | Move r/m to segment register |
| `8F` | POP rm16 | Supported | DataTransfer | TASK-012 | Pop stack top to word r/m (reg aliases to /0) |
| `90` | NOP | Supported | DataTransfer | TASK-012 | No operation (encoded as XCHG AX, AX) |
| `91`..`97` | XCHG AX, r16 | Supported | DataTransfer | TASK-012 | Exchange accumulator with 16-bit register |
| `98` | CBW | Supported | MulDivBcd | TASK-015 | Convert Byte to Word (AL sign-extended to AX) |
| `99` | CWD | Supported | MulDivBcd | TASK-015 | Convert Word to Doubleword (AX sign-extended to DX:AX) |
| `9A` | CALL ptr16:16 | Supported | ControlTransfer | TASK-016 | Direct intersegment (far) call |
| `9B` | WAIT / FWAIT | Supported | ProcessorControl | TASK-018 | No-op in 8086 MVP (no 8087 coprocessor) |
| `9C` | PUSHF | Supported | DataTransfer | TASK-012 | Push FLAGS verbatim onto stack |
| `9D` | POPF | Supported | DataTransfer | TASK-012 | Pop stack into FLAGS (canonical reserved bits forced) |
| `9E` | SAHF | Supported | DataTransfer | TASK-012 | Store AH into low byte of FLAGS |
| `9F` | LAHF | Supported | DataTransfer | TASK-012 | Load low byte of FLAGS into AH |
| `A0` | MOV AL, [moffs8] | Supported | DataTransfer | TASK-012 | Move byte direct offset to AL |
| `A1` | MOV AX, [moffs16] | Supported | DataTransfer | TASK-012 | Move word direct offset to AX |
| `A2` | MOV [moffs8], AL | Supported | DataTransfer | TASK-012 | Move AL to direct byte offset |
| `A3` | MOV [moffs16], AX | Supported | DataTransfer | TASK-012 | Move AX to direct word offset |
| `A4` | MOVSB | Supported | StringOperation | TASK-017 | Move byte [DS:SI] to [ES:DI] (ES unoverridable) |
| `A5` | MOVSW | Supported | StringOperation | TASK-017 | Move word [DS:SI] to [ES:DI] (ES unoverridable) |
| `A6` | CMPSB | Supported | StringOperation | TASK-017 | Compare byte [DS:SI] with [ES:DI] |
| `A7` | CMPSW | Supported | StringOperation | TASK-017 | Compare word [DS:SI] with [ES:DI] |
| `A8` | TEST AL, imm8 | Supported | Logic | TASK-014 | Logical compare imm8 with AL |
| `A9` | TEST AX, imm16 | Supported | Logic | TASK-014 | Logical compare imm16 with AX |
| `AA` | STOSB | Supported | StringOperation | TASK-017 | Store AL to [ES:DI] |
| `AB` | STOSW | Supported | StringOperation | TASK-017 | Store AX to [ES:DI] |
| `AC` | LODSB | Supported | StringOperation | TASK-017 | Load byte from [DS:SI] to AL |
| `AD` | LODSW | Supported | StringOperation | TASK-017 | Load word from [DS:SI] to AX |
| `AE` | SCASB | Supported | StringOperation | TASK-017 | Compare AL with [ES:DI] |
| `AF` | SCASW | Supported | StringOperation | TASK-017 | Compare AX with [ES:DI] |
| `B0`..`B7` | MOV r8, imm8 | Supported | DataTransfer | TASK-012 | Move immediate byte to 8-bit register |
| `B8`..`BF` | MOV r16, imm16 | Supported | DataTransfer | TASK-012 | Move immediate word to 16-bit register |
| `C0`..`C1` | 80186+ shifts | Unsupported | UndocumentedOrAlias | TASK-018 | Shift rm by immediate count; unsupported in MVP |
| `C2` | RET near imm16 | Supported | ControlTransfer | TASK-016 | Return near and pop imm16 bytes from stack |
| `C3` | RET near | Supported | ControlTransfer | TASK-016 | Intrasegment return to caller |
| `C4` | LES r16, m16:16 | Supported | DataTransfer | TASK-012 | Load pointer into ES:r16 (mod=11 halts) |
| `C5` | LDS r16, m16:16 | Supported | DataTransfer | TASK-012 | Load pointer into DS:r16 (mod=11 halts) |
| `C6` | MOV rm8, imm8 | Supported | DataTransfer | TASK-012 | Move imm8 to rm8 (reg field aliases to /0) |
| `C7` | MOV rm16, imm16 | Supported | DataTransfer | TASK-012 | Move imm16 to rm16 (reg field aliases to /0) |
| `C8`..`C9` | 80186+ ENTER/LEAVE | Unsupported | UndocumentedOrAlias | TASK-018 | Stack frame management; unsupported in MVP |
| `CA` | RET far imm16 | Supported | ControlTransfer | TASK-016 | Return far and pop imm16 bytes from stack |
| `CB` | RET far | Supported | ControlTransfer | TASK-016 | Intersegment return to caller |
| `CC` | INT 3 | Supported | ControlTransfer | TASK-016 | Breakpoint trap (vector 3, router interception) |
| `CD` | INT imm8 | Supported | ControlTransfer | TASK-011/016 | Software interrupt (router interception / real IVT) |
| `CE` | INTO | Supported | ControlTransfer | TASK-016 | Interrupt 4 on Overflow if OF=1 |
| `CF` | IRET | Supported | ControlTransfer | TASK-016 | Interrupt return (pops IP, CS, FLAGS) |
| `D0` | GRP2 rm8, 1 | Supported | ShiftRotate | TASK-014 | Shift/rotate rm8 by 1 |
| `D1` | GRP2 rm16, 1 | Supported | ShiftRotate | TASK-014 | Shift/rotate rm16 by 1 |
| `D2` | GRP2 rm8, CL | Supported | ShiftRotate | TASK-014 | Shift/rotate rm8 by CL (count not masked) |
| `D3` | GRP2 rm16, CL | Supported | ShiftRotate | TASK-014 | Shift/rotate rm16 by CL (count not masked) |
| `D4` | AAM imm8 | Supported | MulDivBcd | TASK-015 | ASCII Adjust AX after Multiply (divisor 0 raises INT 0) |
| `D5` | AAD imm8 | Supported | MulDivBcd | TASK-015 | ASCII Adjust AX before Division |
| `D6` | SALC / SETALC | Unsupported | UndocumentedOrAlias | TASK-018 | Undocumented Set AL on Carry; unsupported in MVP |
| `D7` | XLATB | Supported | DataTransfer | TASK-012 | Translate byte using table at [DS:BX+AL] |
| `D8`..`DF` | ESC 0..7, rm | Unsupported | CoprocessorEsc | TASK-018 | 8087 coprocessor instructions; unsupported in MVP |
| `E0` | LOOPNE rel8 | Supported | ControlTransfer | TASK-016 | Decrement CX; jump if CX != 0 and ZF == 0 |
| `E1` | LOOPE rel8 | Supported | ControlTransfer | TASK-016 | Decrement CX; jump if CX != 0 and ZF == 1 |
| `E2` | LOOP rel8 | Supported | ControlTransfer | TASK-016 | Decrement CX; jump if CX != 0 |
| `E3` | JCXZ rel8 | Supported | ControlTransfer | TASK-016 | Jump short if CX == 0 (flags untouched) |
| `E4`..`E7` | IN/OUT fixed port | Unsupported | PortIo | TASK-018 | Peripheral port I/O; unsupported in MVP |
| `E8` | CALL rel16 | Supported | ControlTransfer | TASK-016 | Direct near call relative to next instruction |
| `E9` | JMP rel16 | Supported | ControlTransfer | TASK-016 | Direct near jump relative to next instruction |
| `EA` | JMP ptr16:16 | Supported | ControlTransfer | TASK-016 | Direct far jump to target segment and offset |
| `EB` | JMP rel8 | Supported | ControlTransfer | TASK-016 | Direct short jump with 8-bit sign-extended disp |
| `EC`..`EF` | IN/OUT DX port | Unsupported | PortIo | TASK-018 | Peripheral port I/O; unsupported in MVP |
| `F0` | LOCK | Prefix | Prefix | TASK-011/018 | Bus lock prefix; ignored in MVP |
| `F1` | ICEBP / INT 1 | Unsupported | UndocumentedOrAlias | TASK-018 | Undocumented in-circuit emulator breakpoint |
| `F2` | REPNE / REPNZ | Prefix | Prefix | TASK-017 | Repeat string instruction while not equal |
| `F3` | REP / REPE / REPZ | Prefix | Prefix | TASK-017 | Repeat string instruction while equal |
| `F4` | HLT | Supported | ProcessorControl | TASK-018 | Halt processor execution (HaltReason.Halt) |
| `F5` | CMC | Supported | FlagOps | TASK-018 | Complement Carry Flag (CF = ~CF) |
| `F6` | GRP3 rm8 | Supported | Arithmetic/MulDiv | TASK-014/015 | TEST, NOT, NEG, MUL, IMUL, DIV, IDIV (arm /1 halts) |
| `F7` | GRP3 rm16 | Supported | Arithmetic/MulDiv | TASK-014/015 | TEST, NOT, NEG, MUL, IMUL, DIV, IDIV (arm /1 halts) |
| `F8` | CLC | Supported | FlagOps | TASK-018 | Clear Carry Flag (CF = 0) |
| `F9` | STC | Supported | FlagOps | TASK-018 | Set Carry Flag (CF = 1) |
| `FA` | CLI | Supported | FlagOps | TASK-018 | Clear Interrupt Flag (IF = 0) |
| `FB` | STI | Supported | FlagOps | TASK-018 | Set Interrupt Flag (IF = 1) |
| `FC` | CLD | Supported | FlagOps | TASK-018 | Clear Direction Flag (DF = 0) |
| `FD` | STD | Supported | FlagOps | TASK-018 | Set Direction Flag (DF = 1) |
| `FE` | GRP4 rm8 | Supported | Arithmetic | TASK-013 | /0 INC rm8, /1 DEC rm8 (arms /2../7 halt) |
| `FF` | GRP5 rm16 | Supported | Arithmetic/Ctrl/Data | TASK-012/013/016 | /0 INC, /1 DEC, /2 CALL near, /3 CALL far, /4 JMP near, /5 JMP far, /6 PUSH (arm /7 halts) |
