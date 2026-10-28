# Current Task

> Exactly one active work item. It acts as a lock: if a task is `In progress` under another agent
> and `HANDOFF.md` doesn't mark it handed over, ask the user before continuing.
> Lifecycle and rules: `docs/sdlc/agent-protocol.md` §4.

## Next: TASK-019 Disassembler adapter (Iced)

| Field | Value |
|---|---|
| Status | **Ready** (approved) |
| Owner | — (unassigned. The next agent sets itself as owner when starting) |
| Definition | `docs/05-planning/backlog.md` § TASK-019 (goal, scope, tests, DoD) |
| Procedure | `docs/sdlc/procedures/assembly-implement.md` (invoke with `/assembly-implement TASK-019`) |

**Also eligible:** TASK-061 Themes, tokens and contrast test (depends on TASK-060, Done); TASK-067 Settings service (also needs TASK-040).
**Parallel user action:** TASK-006 (clean test VMs) can start any time.

---

## Completed: TASK-018 Flag ops, misc and the full opcode table (Done 2026-10-28 14:06 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- Implemented in `src/AssemblyStudio.Engine.I8086/Cpu8086.FlagsMisc.cs` and `Cpu8086.cs`:
  - Flag operations: CLC (F8), STC (F9), CMC (F5), CLD (FC), STD (FD), CLI (FA), STI (FB).
  - Processor control: HLT (F4) halts execution reporting `StepOutcome.Halted` and `HaltReason.Halt`, with IP advanced past HLT.
  - Synchronization: WAIT / FWAIT (9B) is a no-op on 8086 without 8087 coprocessor (advances IP by 1, flags/registers untouched).
  - Prefix LOCK (F0) is ignored per ADR-0003 §4 in `Decoder.ParsePrefixes`.
  - Coprocessor ESC (D8-DF) and Port I/O (E4-E7, EC-EF) explicitly unsupported in MVP, halting with `UnsupportedInstruction` and state preserved byte-identical (FR-037).
  - Undocumented aliases (0F, 60-6F, C0-C1, C8-C9, D6, F1) halt with `UnsupportedInstruction`.
  - Created `OpcodeTable.cs` enumerating and classifying all 256 opcode bytes into Supported (206), Prefix (7), and Unsupported (43).
  - Created `docs/06-implementation/opcode-support.md` documenting complete opcode space, policies, Group ModR/M arms, and full 256-opcode reference table for help (FR-045).
- Tests: 7 SingleStepTests oracle files at 100% (F5, F8, F9, FA, FB, FC, FD; 283 oracle files total) + 40 hand tests in `FlagsMiscAndOpcodeTableTests.cs` (including exhaustive 256 opcode table agreement and ModR/M group arm tests); all 513 engine tests passed.
- Release build 0 warnings, 0 errors.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | Antigravity (IDE, Gemini 3.8 Flash) |
| Started / finished | 2026-10-28 13:43 / 2026-10-28 14:06 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-018 |

---

## Completed: TASK-017 String instructions and REP prefixes (Done 2026-10-28 13:28 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- Implemented in `src/AssemblyStudio.Engine.I8086/Cpu8086.Strings.cs` and `Cpu8086.cs`:
  - MOVSB/MOVSW (A4/A5), CMPSB/CMPSW (A6/A7), STOSB/STOSW (AA/AB), LODSB/LODSW (AC/AD), SCASB/SCASW (AE/AF).
  - Prefix support: F2 (REPNZ / REPNE) and F3 (REP / REPZ / REPE).
  - Segment rules: Destination is always [ES:DI] (unoverridable); Source defaults to [DS:SI] and respects segment overrides (CS/DS/ES/SS).
  - Direction flag: DF=0 increments SI/DI by 1 (byte) or 2 (word); DF=1 decrements SI/DI by 1 (byte) or 2 (word); 16-bit address wrap.
  - Flags: MOVS, STOS, LODS leave flags unchanged; CMPS and SCAS compute arithmetic flags via subtraction (CF, AF, ZF, SF, PF, OF).
  - Loop behavior: CX=0 with REP prefix is a no-op (advances IP past instruction without touching registers, memory or flags); REPE stops on mismatch (ZF=0); REPNE stops on match (ZF=1); without REP prefix executes single iteration without touching CX.
  - Documented decision on per-iteration step semantics: In `ICpu.StepInstruction`, an instruction with a REP prefix executes as a single, complete instruction step until termination, advancing IP past the instruction.
- Tests: 10 SingleStepTests oracle files at 100% (A4, A5, A6, A7, AA, AB, AC, AD, AE, AF; 276 oracle files total) + 16 hand tests in `StringInstructionTests.cs`; all 466 engine tests passed.
- Release build 0 warnings, 0 errors.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | Antigravity (IDE, Gemini 3.8 Flash) |
| Started / finished | 2026-10-28 13:05 / 2026-10-28 13:28 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-017 |

---

## Completed: TASK-016 Control transfer and interrupts (Done 2026-10-28 12:45 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- Implemented in `src/AssemblyStudio.Engine.I8086/Cpu8086.ControlTransfer.cs` and `Cpu8086.Arithmetic.cs`:
  - 70-7F conditional jumps: all 16 conditions, 8-bit sign-extended displacement, 16-bit IP wrap, flags unchanged.
  - E3 JCXZ: jumps on CX=0 without flag modification.
  - E0-E2 Loops: LOOP, LOOPE/LOOPZ, LOOPNE/LOOPNZ decrement CX without touching flags; jump conditions verified.
  - EB, E9, EA: JMP short, near, and far.
  - E8, 9A: CALL near and far, pushing return addresses and segment appropriately.
  - C2, C3, CA, CB: RET near/far with optional immediate byte stack popping.
  - FF /2-/5: Indirect near and far CALL/JMP; mod=3 halts UnsupportedInstruction.
  - CC (INT 3), CD (INT imm8), CE (INTO on OF=1): Router interception evaluated first; null router vectors through IVT with FLAGS/CS/IP push and IF/TF clear.
  - CF (IRET): Pops IP, CS, and FLAGS with canonical 8086 reserved bits.
  - Fixed IVT entry calculation from `vector * 2` to `vector * 4` (2-byte IP + 2-byte CS per vector in real hardware).
- Tests: 37 SingleStepTests oracle files at 100% (266 oracle files total) + 37 hand tests in `ControlTransferTests.cs`; all 440 engine tests passed.
- Defects: DEF-001 opened for pre-existing `MainWindowSmokeTests.View_menu_opens_with_shortcuts_and_reset_layout` in App.Tests.
- Release build 0 warnings, 0 errors.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | Antigravity (IDE, Gemini 3.8 Flash) |
| Started / finished | 2026-10-28 12:00 / 2026-10-28 12:45 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-016 |

---

## Completed: TASK-060 WPF shell and layout (Done 2026-10-28 11:31 +0500, Claude Code (Opus 5.5))
- Main window per ui-spec §2 in `src/AssemblyStudio.App/Shell/` (MVVM, CommunityToolkit.Mvvm 8.4.2 per ADR-0007): menus (UI-002 skeleton, every command with access key and shortcut; later-task commands disabled), command bar, status bar (UI-016, app version), Learn / editor / machine column / bottom tab group + memory/stack regions with keyboard-resizable splitters, empty states only (no machine data). F6/Shift+F6 region cycling, View ▸ Go to (Ctrl+Alt+R/F/M/K/O/P/U/H/B/L), region toggles, Reset Layout; compact layout below 1280 px; layout persisted in `settings.json` "layout"; 2 px focus ring; PerMonitorV2 manifest; Windows text scaling applied to font tokens; strings in `Strings.resx`; dark tokens only (TASK-061 owns themes).
- Tests (App.Tests, 41): view-model layout rules, settings-file store, options/text-scale, UI smoke on the real XAML (window opens, F6 cycles,
  Go to focuses, hidden region returns focus, View menu opens), in-process renders for review (`artifacts/ui/`, not committed), and a real-exe
  startup measurement (NFR-005). Verification notes: `docs/06-implementation/task-060-verification.md`.
- Release build 0 warnings, 0 errors. Solution tests 465/465 passed.

| Field | Value |
|---|---|
| Status | Done (all DoD items met; partial items documented in the verification notes) |
| Owner | Claude Code (Opus 5.5) |
| Started / finished | 2026-10-28 11:02 / 2026-10-28 11:31 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-060 |

---

## Completed: TASK-015 Multiply, divide, BCD and conversions (Done 2026-10-28 10:25 +0500, Claude Code (Opus 5.5))
- Started by OpenCode (GLM-5.3-Flash Free) on 2026-10-27 (oracle analysis scripts `poc/task015-*.ps1`, untracked, no engine code); OpenCode
  hit its usage limit and the user asked Claude Code to continue ("please continue opencode task its limit reach").
- `src/AssemblyStudio.Engine.I8086/Cpu8086.MulDiv.cs` (new partial class): MUL/IMUL/DIV/IDIV (F6/F7 /4-/7), AAA/AAS/DAA/DAS, AAM/AAD, CBW/CWD in new `Cpu8086.MulDiv.cs`. Division follows the 8086 CORD microcode loop, so divide-error FLAGS pushed on the stack match the real chip; divide error (overflow, divisor 0, AAM 0) goes through the router as vector 0, else real vectoring with the 8086 return address (next instruction). 8086 quirks oracle-verified: IDIV rejects -128/-32768, REP negates the IDIV quotient, AAA/AAS adjust AL and AH separately, DAA/DAS high-digit threshold 9Fh when AF was set, DAS low-digit borrow does not set CF. Undefined-flag masks reported per step. 16 oracle files at 100% (229 total) + 21 hand tests incl. AC-031 engine part.
- `Cpu8086.cs`: dispatch for 27/2F/37/3F/98/99/D4/D5; INT vectoring factored into `VectorInterrupt(vector, flagsToPush, returnIp)` (shared by
  INT imm8 and the divide error). `Cpu8086.Logic.cs`: `ExecF6F7Group` arms 4-7. The undocumented F6/F7 /1 TEST alias still halts (FR-037).
- Tests: `MulDivBcdTests.cs` (21); the TASK-014 test that expected MUL to halt was removed (MUL is now in scope).
- No new dependencies. Release build 0 warnings, 0 errors. Solution tests 425/425 passed.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | Claude Code (Opus 5.5) (taken over from OpenCode (GLM-5.3-Flash Free) on the user's request) |
| Started / finished | 2026-10-27 (OpenCode, time not recorded) / 2026-10-28 10:25 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-015 |

---

## Completed: TASK-014 Logic, shift and rotate instructions (Done 2026-10-27 16:12 +0500, OpenCode (GLM-5.3-Flash Free))
- `src/AssemblyStudio.Engine.I8086/Cpu8086.Logic.cs` (new partial class): AND/OR/XOR in rm/r families (bases 08/20/30, arms 0-3 via the shared TASK-013 `Arith` core — `opcode>>3` maps onto the existing `ArithKind`) and acc-imm forms (0C/0D, 24/25, 34/35); TEST (84/85 rm,reg; A8/A9 acc,imm; F6/F7 arm 0 rm,imm) — AND flag semantics with the result discarded, AF untouched; NOT (F6/F7 arm 2) with no flags; shifts/rotates D0-D3 all /reg.
- 8086 count semantics: count NOT masked (masking arrived with the 186), count 0 changes nothing at all (not even flags), OF set only for count 1 (oracle-verified: ROL/RCL/SHL = MSB(result)^CF; ROR = bit7(result)^bit6(result); RCR = MSB(orig)^MSB(result); SHR = MSB(orig); SAR = 0), count>1 leaves OF untouched (metadata masks exclude it: rotates D2/D3 0xF7FF, shifts 0xF7EF), rotates never touch SF/ZF/PF/AF, logic ops clear CF/OF (mask 0xFFEF excludes AF only).
- **Undocumented D0-D3 /6 = SETMO** (the backlog's "/6 handling decision", oracle-verified over all 2000+2000+2000 cases named "setmo"/"setmoc"): operand becomes all ones (0xFF/0xFFFF), no-op at CL count 0; flags set to the real chip's raw values (SF=1, ZF=0, PF=1, CF=OF=AF=0) — the metadata mask 0xF72A officially reports them undefined.
- `Cpu8086.cs`: dispatch cases for the logic families, 84/85, A8/A9, D0-D3, F6/F7; `ExecF6F7Group` (arms 0/2/3) and `ExecShiftGroup`. `ExecNeg` refactored to `ExecNegCore(m, cursor, wide)`. The undocumented F6/F7 /1 TEST alias and MUL/DIV arms 4-7 halt `UnsupportedInstruction` with state unchanged (FR-037; /1 not in approved scope).
- **Oracle harness**: 58 TASK-014 files added to `SingleStepOracleTests` → **100% pass on all 213 oracle files** (71 + 84 + 58), first run.
- 23 deterministic hand tests (`LogicShiftRotateTests.cs`): logic CF/OF clears, TEST discards (rm,reg / acc,imm / rm,imm), NOT no-flags, count=1 OF rules for all six shift/rotate ops, rotates CF-only, CL=0 no-op (byte-identical state), CL=2 OF untouched, SHR word, SETMO byte/word/CL=0, FR-037 halts.
- No new dependencies. Release build: 0 warnings, 0 errors. Solution tests: 389/389 passed.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | OpenCode (GLM-5.3-Flash Free) |
| Started / finished | 2026-10-27 15:45 / 16:12 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-014 (goal, scope, tests, DoD) |
| Procedure | `docs/sdlc/procedures/assembly-implement.md` (invoke with `/assembly-implement TASK-014`) |
| Depends on | TASK-011 (Done ✔) |

**Parallel user action:** TASK-006 (clean test VMs) can start any time.

---

## Completed: TASK-013 Arithmetic instructions (Done 2026-10-27 14:59 +0500, OpenCode (GLM-5.3-Flash Free))
- `src/AssemblyStudio.Engine.I8086/Cpu8086.Arithmetic.cs` (new partial class): ADD/ADC/SUB/SBB/CMP in all three shapes — rm/r families (bases 00/10/18/28/38, arms 0-3), acc-imm (arms 4/5: AL,imm8 / AX,imm16), and the immediate groups 80/82 (rm8, imm8; 82 = undocumented alias of 80), 81 (rm16, imm16), 83 (rm16, sign-extended imm8) with the reg field selecting the operation; INC/DEC r16 (40-4F, all INC/DEC flags except CF); INC/DEC rm (FE/FF arms 0/1); NEG (F6/F7 arm 3, halts for other arms).
- Exact 8086 flags: CF carry/borrow out; OF signed overflow (width-aware, bit 7 for 8-bit); AF carry/borrow out of bit 3; SF top bit; ZF on zero; PF even parity of the low 8 bits. INC/DEC leave CF unchanged; NEG sets CF unless the operand is 0 and overflows only on 0x80/0x8000; CMP computes SUB flags and discards the result. TF/IF/DF and reserved bits pass through; everything touched is defined (`FlagBit.None` report).
- **AC-025 hand test verbatim**: `add al, bl` with AL=7Fh, BL=01h → AL=80h, OF=1, SF=1, ZF=0, AF=1, PF=0, CF=0 (flags word 0x0A92).
- `Cpu8086.cs`: arithmetic cases added to dispatch; the FF group now dispatches arms 0/1 (INC/DEC rm16) alongside arm 6 (PUSH, TASK-012) — `ExecPushRm16` became `ExecFfGroup`; FE is its own group handler (`ExecFeGroup`, arms other than 0/1 halt unsupported).
- **Oracle harness**: 84 TASK-013 files added to `SingleStepOracleTests` (30 family + 16 INC/DEC r16 + 32 group 80-83 all /reg + NEG F6.3/F7.3 + rm INC/DEC FE.0/FE.1/FF.0/FF.1) → **100% pass on all 155 oracle files** (71 TASK-012 + 84 TASK-013).
- 20 deterministic hand tests (`ArithmeticTests.cs`): AC-025, ADC/SBB carry/borrow chains, INC/DEC CF-untouched, NEG of 0/0x80/word, CMP discards, 83 sign-extension, group imm writes to memory, ES override, 16-bit SF/ZF/OF edges, FE/FF out-of-scope arms halt with byte-identical state (FR-037).
- No new dependencies. Release build: 0 warnings, 0 errors. Solution tests: 308/308 passed.

| Field | Value |
|---|---|
| Status | Done (all DoD items met) |
| Owner | OpenCode (GLM-5.3-Flash Free) |
| Started / finished | 2026-10-27 14:00 / 14:59 +0500 |
| Definition | `docs/05-planning/backlog.md` § TASK-013 (goal, scope, tests, DoD) |
| Procedure | `docs/sdlc/procedures/assembly-implement.md` (invoke with `/assembly-implement TASK-013`) |
| Depends on | TASK-011 (Done ✔) |

**Parallel user action:** TASK-006 (clean test VMs) can start any time.

---

## Last completed: TASK-012, Data transfer instructions (Done 2026-10-27 13:55 +0500, OpenCode (GLM-5.3-Flash Free))
- `src/AssemblyStudio.Engine.I8086/Cpu8086.DataTransfer.cs` (new partial class): MOV all forms — rm/reg (88-8B), segment registers 8C/8E with the 8086 `reg&3` field aliasing (ES/CS/SS/DS), moffs A0-A3, reg-imm B0-BF, rm-imm C6/C7 (every reg field behaves as /0); XCHG 86/87 + AX-form 90-97 (90 = NOP); LEA 8D, LES C4, LDS C5 (memory forms; mod=11 is undefined and halts `UnsupportedInstruction` with state unchanged); XLAT D7 (DS default, override honoured); LAHF 9F (flags low byte verbatim), SAHF 9E (`(AH&0xD5)|0x02`); PUSH/POP reg 50-5F and seg 06/07/0E/16/17/1E/1F; POP rm 8F (every reg field behaves as /0); PUSH rm via FF/6 (other FF forms still unsupported); PUSHF 9C (FLAGS verbatim), POPF 9D (`(popped|0xF002)&~0x0028` — 8086 reserved bits forced to canonical form).
- Oracle-confirmed 8086 quirks baked in and hand-tested: PUSH SP pushes the NEW (decremented) SP — including the register form of FF/6; POP SP (5C and 8F rm4) loads SP with the popped value; PUSHF pushes FLAGS verbatim; POPF forces the canonical reserved bits.
- `Cpu8086.cs`: now `sealed partial`; dispatch is a switch expression returning `StepResult?` (null = unsupported/undefined → state restored, halt — FR-037 intact). Executed results carry the instruction's start address (CS:IP at entry, prefixes included); handlers advance IP via the `Executed(endIp)` helper.
- **Oracle harness** (`tests/.../SingleStepOracleTests.cs`): 71 TASK-012 opcode files, every case: load initial regs/ram into a fresh `FakeMemory`, one `StepInstruction()`, compare all 14 registers (flags via per-case mask resolved from `metadata.json` opcode → reg-subkey when present), compare every `final` ram byte, fail on unexpected writes → **100% pass on all 71 files**.
- 30 deterministic hand tests (`DataTransferTests.cs`): PUSH SP, POP SP (5C + 8F), segment moves incl. aliasing, LAHF/SAHF, PUSHF/POPF reserved bits, XLAT+override, LEA (+mod=11 halt), LDS/LES, moffs+override, C6/C7 alias regs, XCHG mem/reg and AX/SP, NOP, word access at offset 0xFFFF.
- No new dependencies. Release build: 0 warnings, 0 errors. Solution tests: 204/204 passed (includes new App.Tests project that exists in the tree, not touched by this task).

---

## Last completed: TASK-011, Engine skeleton: decode, ModR/M, memory access (Done 2026-10-27 12:45 +0500, OpenCode (GLM-5.3-Flash Free))
- `src/AssemblyStudio.Engine.I8086/`: `Cpu8086 : ICpu` — pure decode (state untouched until a decision), opcode dispatch with only `CD nn` (INT imm8) in this task as the interception-contract carrier; every other opcode (including aliases `0F`, `60–6F`, `C0/C1/C8/C9`, `D6`) halts `Halted(UnsupportedInstruction)` with CS:IP restored to the instruction start and full state byte-identical (FR-037, AC-028 engine part).
- `Decoder` (internal, pure): prefix parsing (segment overrides `26/2E/36/3E` last-wins, `F2/F3` REP capture, `F0` LOCK ignored), full 8086 ModR/M effective-address table with default segments (rm 2/3 and rm 6 mod≠0 → SS, else DS), disp8 sign-extension, disp16, `mod 00 rm 110` direct displacement, 16-bit EA and IP fetch wrap.
- Interrupt contract tested three ways: handled result returned as-is; `WaitingForInput` pass-through (CS:IP stays on the INT); `null`/no router → real vectoring (push FLAGS/CS/return-IP with 16-bit SP wrap, IF/TF cleared, CS:IP loaded from the IVT).
- 37 deterministic hand tests in `tests/AssemblyStudio.Engine.I8086.Tests` (full-state assertions, raw bytes, no assembler); `FakeMemory` double implementing the documented IMemory semantics (1 MiB, 20-bit wrap, word wrap within segment).
- No new dependencies (Iced deferred to TASK-019). No implementations of instruction families (TASK-012+).
- Release build: 0 warnings, 0 errors. Solution tests: 103/103 passed (9 test projects).

---

## Last completed: TASK-010, Core abstractions v1 (Done 2026-10-27 12:21 +0500, OpenCode (GLM-5.3-Flash Free))
- `src/AssemblyStudio.Core/`: `FlagBit`, `CpuState` (record struct, 14 ushort fields), `StepOutcome`/`HaltReason`/`StepResult`, `IMemory` (+ `MemoryChange` write-journal drain), `IInterruptRouter` (`StepResult? Intercept`, null = vector through IVT), `ICpu` (`State`/`SetState`/`StepInstruction`), `IDisassembler`, `IConsoleDevice`, `IMachine`, `IOsServices` (`HandleService(vector, state, IMachine)`), `SessionState`/`CurrentInstruction`/`MemoryWindow`/`StackWindow`/`MachineSnapshot` (ADR-0007 v1.1 fields, types only). XML docs on every public abstraction, citing ADR/FR.
- No implementations added (out of scope): `IAssembler`/`ISourceMap` (TASK-040/043) and `IDebugSession` (TASK-050) intentionally not defined here.
- 8 model tests in `tests/AssemblyStudio.Core.Tests`: CpuState equality/hash/with-expressions/value-copy, MachineSnapshot immutability (CPU advancing after publish leaves the snapshot unchanged), v1.1 field shape.
- Renamed `ICpu.Step()` → `StepInstruction()` (CA1716 reserved keyword, warnings-as-errors). No suppressions added.
- Release build: 0 warnings, 0 errors. Solution tests: 66/66 passed (9 test projects).

---

## Last completed: TASK-005, Bundled NASM tooling layout (Done 2026-10-27 12:00 +0500, Antigravity (IDE, Gemini 3.8 Flash))
- `build/fetch-nasm.ps1`: Fetches and extracts pinned NASM 3.02 win64 binary into `tools/nasm/` (git-ignored per ADR-0005).
- Verifies archive SHA-256 (`161d0bfa...`), extracts `nasm.exe`, `ndisasm.exe`, and `LICENSE` (BSD-2-Clause).
- Verifies extracted file hashes and writes deterministic `tools/nasm/SHA256SUMS.txt` and `tools/nasm/SOURCE.txt` (no timestamps).
- `NasmLocator` test helper in `tests/AssemblyStudio.Toolchain.Nasm.Tests` with 6 passing tests: locating nasm, verifying version 3.02, ndisasm locator, BSD-2-Clause license & source metadata, SHA-256 manifest integrity, and actionable failure on missing tool.
- Release build: 0 warnings, 0 errors. Solution tests: 58/58 passed (9 test projects). `build/coverage.ps1`: exit 0.

---

## Earlier: TASK-004, Coverage tooling (Done 2026-10-27 10:48 +0500, Claude Code)
- `coverlet.MTP` 10.1.0 (MIT) pinned in `Directory.Packages.props`, referenced by every test project via `Directory.Build.props`.
- `build/coverage.ps1`: per-project Cobertura, merged per product assembly, `artifacts/coverage/summary.md`; exit 1 on a failed or missing run.
- Skeleton report: 9 Cobertura files from 9 projects, all assemblies "n/a (no coverable lines)".
