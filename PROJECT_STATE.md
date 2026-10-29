# Project State

> Single source of truth for verified project status (it replaced `docs/status/project-status.md`
> on 2026-10-26). Update rules: `docs/sdlc/agent-protocol.md` §6. Phase detail: `ROADMAP.md`.

| Field | Value |
|---|---|
| Last updated | 2026-10-29 10:24 +0500 by Claude Code (Opus 5.5) (TASK-035 done) |
| Current phase | **6 Implementation**, milestone M2 in progress (TASK-030..035 done), M0 (TASK-006 pending), M4 (TASK-060 done) |
| Current milestone | M2 Runtime, DOS and toolchain (in progress) — TASK-030..035 done; M1 Complete; M0 nearly complete (TASK-006 pending) |
| Active task | None in progress. Next eligible: M2 `TASK-036` DOS buffered input and Ctrl-C; M2 `TASK-040` NASM runner; M4 `TASK-061` Themes, tokens and contrast test. Parallel user action: `TASK-006` (clean test VMs) |

| Next recommended action | `/assembly-implement TASK-036` (DOS buffered input and Ctrl-C) or `/assembly-implement TASK-040` (NASM runner) or `/assembly-implement TASK-061` (themes). User action TASK-006 (clean test VMs) can start any time |

## Verified status
- The SDLC process, tool-independent core (`docs/sdlc/`), templates, shared memory files and the tool
  entry points for Claude Code (`.claude/skills/`) and Antigravity (`.agent/`) exist and pass
  structural validation (SDLC-002, re-validated by `/assembly-init` on 2026-10-26 16:41 +0500).
- Requirements set **v1.0 Approved** in `docs/01-requirements/` (7 documents): 96 FR, 33 NFR (117 APPROVED, 12 NEEDS_RESEARCH),
  26 user stories, 63 acceptance criteria, 19 open questions. Numeric targets with basis PROPOSED are still unvalidated.
- Research set **v1.0 Approved** in `docs/02-research/` (6 documents + POC-001/002 definitions). Key recommendations: .NET 10 + WPF self-contained,
  Inno Setup per-user, custom C# 8086 core validated by SingleStepTests, NASM 3.02 bundled, AvalonEdit, DOS 3.3/4.0 semantics.
- Requirements **v2.0 approved** 2026-10-26 18:18 +0500: D-011 (Windows 11 24H2+ / Windows 10 Enterprise LTSC 21H2) and D-012 (INT 21h AH=00h) applied.
  Status: 118 APPROVED, 11 NEEDS_RESEARCH. 64 acceptance criteria.
- **Product skeleton, architecture tests, and SingleStepTests oracle in place:** `AssemblyStudio.slnx` with all 8 src projects and 9 test projects per ADR-0002. Hardware oracle data (324 files) fetched and verified. Release build and 53/53 tests verified green (2026-10-27 10:15 +0500, after the review fixes). The architecture tests also check `.csproj` files, and the oracle fetch verifies against committed SHA-256 manifests.

## Completed
| Item | Completed | Evidence |
|---|---|---|
| TASK-035 DOS character input services | 2026-10-29 10:24 +0500 | `DosServices`: INT 21h 01h (echo through the output path; TAB echoes as spaces, AL=09h), 07h/08h (no echo); extended keys 00h + scan code over two calls; empty queue → `WaitingForInput` with CS:IP on the INT, re-executed when input arrives. 12 new tests (Os.Dos 42/42). Release build 0w/0e; full solution 829/829 |
| TASK-034 DOS output and termination services | 2026-10-29 10:16 +0500 | `src/AssemblyStudio.Os.Dos/DosServices.cs`: INT 21h 02h/09h/00h/4Ch and INT 20h (DOS 3.3/4.0: own column with TAB → spaces, AL returns 02h → last char / 20h for TAB and 09h → 24h, CS = PSP check for 20h/00h → DOS001, RET to PSP:0000 → `ReturnToPsp`, 09h without '$' → DOS002 deviation). Core additions: `StepOutcome.Terminated`, `StepResult.Termination` (`ProgramTermination`). 30/30 Os.Dos tests incl. AC-064 (all four paths) and AC-022 runtime part through the real engine/router/loader. Release build 0w/0e; full solution 817/817 |
| TASK-033 Console device | 2026-10-28 18:04 +0500 | `src/AssemblyStudio.Runtime/Console/ConsoleDevice.cs` implementing `IConsoleDevice` per FR-063, FR-064, NFR-015, NFR-016, AC-045, AC-047, and ADR-0004 §6: circular output buffer capped at 1 MiB with oldest content trimmed; snapshot delta API (`TakeOutput`) decoding CP437; column counter tracking TAB (modulo 8), CR (reset to 0), BS (decrement), and printable characters; BEL (07h) event counter; input queue with instruction-stamped event recording (`InputEvent.cs`) and deterministic replay; 22 new unit tests in `ConsoleDeviceTests` (total 79/79 in Runtime.Tests); solution build 0w/0e, 39/39 architecture tests passed |
| TASK-032 Interrupt router and runtime error policy | 2026-10-28 17:52 +0500 | `src/AssemblyStudio.Runtime/Interrupts/` (`IvtStubs.cs`, `RuntimeHaltInfo.cs`, `RuntimeErrorPolicy.cs`, `InterruptRouter.cs`) implementing `IInterruptRouter` per FR-038, FR-040, NFR-013, AC-029, AC-031, ADR-0004 §3: IVT default stubs at reserved segment 0xF000 with IRET opcodes (0xCF); default vs program-installed vector detection (program-modified vectors return null to let CPU vector through IVT with real 8086 semantics); CPU exceptions (INT 0..4) halt with RuntimeError naming condition and address (AC-031 divide error); unsupported interrupts/functions halt with UnsupportedService naming interrupt and AH (AC-029 INT 21h function 3Dh); structured What/Where/Next diagnostic records; OS services delegation; 18 new unit tests in `InterruptRouterTests` (total 57/57 in Runtime.Tests); solution build 0w/0e, 39/39 architecture tests passed |
| TASK-031 .COM loader and PSP | 2026-10-28 16:55 +0500 | `src/AssemblyStudio.Runtime/Loading/ProgramSegmentPrefix.cs` and `ComLoader.cs` implementing .COM image loading per FR-034, AC-021, and ADR-0004 §2: PSP at 0800h with fields 00h (CD 20), 02h (top of memory 0x1800), 05h (far call to 0800:0050h), 0Ah..15h (IVT vectors), 16h (parent PSP), 2Ch (env 0000h), 50h (CD 21 CB), 5Ch/6Ch (zeroed FCBs), and 80h (command tail with length and CR); image placed at 0800:0100h; stack word 0000h at SS:SP (0800:FFFEh); registers initialized to CS=DS=ES=SS=0800h, IP=0100h, SP=FFFEh, AX=BX=CX=DX=SI=DI=BP=0000h, Flags=0002h; 9 new tests in `ComLoaderTests` (total 39/39 in Runtime.Tests); solution build 0w/0e |
| TASK-030 Runtime memory, journal and machine | 2026-10-28 16:40 +0500 | `src/AssemblyStudio.Runtime/Memory/RealModeMemory.cs` implementing `IMemory` with 1 MiB physical address space, 20-bit wrap, 16-bit word segment offset wrap, per-step write journal (`TakeJournal`, `Rollback`, `RollbackJournal`), and contiguous sorted changed-range extraction in `AddressRange.cs`; `src/AssemblyStudio.Runtime/Machine.cs` implementing `IMachine` composing `ICpu`, `IMemory`, `IConsoleDevice`, and optional `IInterruptRouter` and `IOsServices`; `NullConsoleDevice.cs` default fallback; 30 unit tests in `AssemblyStudio.Runtime.Tests` (wrap, aliasing, word wrap, journal, changed-range merging, rollback, machine wiring); solution build 0w/0e, 39/39 architecture tests passed |
| TASK-020 Engine conformance and performance gate | 2026-10-28 16:00 +0500 | Conformance report `docs/07-testing/engine-conformance-report.md` (TR-2026-10-28-01); full 283 SingleStepTests oracle files pass at 100.0% (~566,000 cases); secondary 8088 oracle suite verified; complete 256-opcode table classified (210 Supported, 7 Prefix, 39 Unsupported); NFR-002 step+snapshot latency mean 0.178 µs (p95 0.300 µs, budget <=100 ms); worst-case 1 MiB memory snapshot copy 91.9 µs; NFR-003 free-run throughput 6.42–7.29 M instr/s (target >=1.00 M/s); NFR-016 bit-for-bit execution determinism over long traces verified; 611/611 Engine tests pass |
| TASK-019 Disassembler adapter (Iced) | 2026-10-28 15:24 +0500 | `src/AssemblyStudio.Engine.I8086/Disassembly/IcedDisassembler.cs` implementing `IDisassembler` via `Iced 1.21.0` (MIT) with NASM formatter (space-after-comma, lowercase hex, explicit segment display), 8086-only filtering rejecting 186+, 286+, 386+ opcodes and prefixes (FS, GS, 0x66, 0x67) and undocumented SALC (0xD6); ADR-0003 unverified claims resolved in `docs/decisions/ADR-0003-execution-engine.md` and verified in tests; 76 tests in `IcedDisassemblerTests` (including instruction length agreement across thousands of SingleStepTests oracle cases); solution tests 687/688 passed (1 pre-existing DEF-001 App smoke test failure) |
| TASK-018 Flag ops, misc and the full opcode table | 2026-10-28 14:06 +0500 | CLC (F8), STC (F9), CMC (F5), CLD (FC), STD (FD), CLI (FA), STI (FB), HLT (F4, halts HaltReason.Halt), WAIT (9B, no-op) in `Cpu8086.FlagsMisc.cs` and `Cpu8086.cs`; comprehensive `OpcodeTable.cs` classifying all 256 opcode bytes; reference docs in `docs/06-implementation/opcode-support.md`; 7 SingleStepTests oracle files at 100% (283 total) + 40 hand tests in `FlagsMiscAndOpcodeTableTests` (including exhaustive 256 opcode table agreement and ModR/M group arm tests); 513/513 engine tests passed |
| TASK-017 String instructions and REP prefixes | 2026-10-28 13:28 +0500 | MOVSB/MOVSW (A4/A5), CMPSB/CMPSW (A6/A7), STOSB/STOSW (AA/AB), LODSB/LODSW (AC/AD), SCASB/SCASW (AE/AF) with F2/F3 prefixes in `Cpu8086.Strings.cs`; unoverridable ES:DI destination, overridable DS:SI source; DF=0/1 pointer delta with 16-bit wrapping; CX=0 no-op; REPE/REPNE early loop termination; per-iteration step semantics decision documented; 10 SingleStepTests oracle files at 100% (276 total) + 16 hand tests in `StringInstructionTests`; 466/466 engine tests passed |
| TASK-016 Control transfer and interrupts | 2026-10-28 12:45 +0500 | Jumps (70-7F, E3, EB, E9, EA), loops (E0-E2), calls (E8, 9A), returns (C2, C3, CA, CB), interrupts (CC, CD, CE, CF) and FF /2-/5 in `Cpu8086.ControlTransfer.cs` and `Cpu8086.Arithmetic.cs`; 4-byte IVT entry fix; 37 SingleStepTests oracle files at 100% (266 total) + 37 hand tests in `ControlTransferTests`; 440/440 engine tests passed |
| TASK-060 WPF shell and layout | 2026-10-28 11:31 +0500 | Main window per ui-spec §2 in `src/AssemblyStudio.App/Shell/` (MVVM, CommunityToolkit.Mvvm 8.4.2 per ADR-0007): menus (UI-002 skeleton, every command with access key and shortcut; later-task commands disabled), command bar, status bar (UI-016, app version), Learn / editor / machine column / bottom tab group + memory/stack regions with keyboard-resizable splitters, empty states only (no machine data). F6/Shift+F6 region cycling, View ▸ Go to (Ctrl+Alt+R/F/M/K/O/P/U/H/B/L), region toggles, Reset Layout; compact layout below 1280 px; layout persisted in `settings.json` "layout"; 2 px focus ring; PerMonitorV2 manifest; Windows text scaling applied to font tokens; strings in `Strings.resx`; dark tokens only (TASK-061 owns themes). Startup measured 1.5–2.5 s on the dev machine (NFR-005). Release build 0w/0e; 465/465 solution tests |
| TASK-015 Multiply, divide, BCD and conversions | 2026-10-28 10:25 +0500 | MUL/IMUL/DIV/IDIV (F6/F7 /4-/7), AAA/AAS/DAA/DAS, AAM/AAD, CBW/CWD in new `Cpu8086.MulDiv.cs`. Division follows the 8086 CORD microcode loop, so divide-error FLAGS pushed on the stack match the real chip; divide error (overflow, divisor 0, AAM 0) goes through the router as vector 0, else real vectoring with the 8086 return address (next instruction). 8086 quirks oracle-verified: IDIV rejects -128/-32768, REP negates the IDIV quotient, AAA/AAS adjust AL and AH separately, DAA/DAS high-digit threshold 9Fh when AF was set, DAS low-digit borrow does not set CF. Undefined-flag masks reported per step. 16 oracle files at 100% (229 total) + 21 hand tests incl. AC-031 engine part. Release build 0w/0e; 425/425 solution tests |
| TASK-014 Logic, shift and rotate instructions | 2026-10-27 16:12 +0500 | AND/OR/XOR (rm/r bases 08/20/30 + acc-imm 0C/24/34, reusing the shared `Arith` core), TEST (84/85, A8/A9, F6/F7 /0 — result discarded, AF untouched), NOT (F6/F7 /2, no flags), shifts/rotates D0-D3 all /reg with 8086 count semantics (count not masked, count 0 = full no-op, OF only for count 1 with oracle-verified rules, count>1 leaves OF untouched, rotates touch only CF/OF); undocumented /6 = SETMO (operand all ones, oracle raw-flag values); 58 SingleStepTests oracle files at 100% first run (213 total) + 23 hand tests. Release build 0w/0e; 389/389 solution tests |
| TASK-013 Arithmetic instructions | 2026-10-27 14:59 +0500 | ADD/ADC/SUB/SBB/CMP in all forms (rm/r bases 00/10/18/28/38 arms 0-3, acc-imm arms 4/5, immediate groups 80/82/81/83 all /reg with sign-extended imm8 in 83), INC/DEC r16 40-4F and rm FE/FF /0 /1 (CF untouched), NEG F6/F7 /3; exact width-aware 8086 flags; 84 SingleStepTests oracle files at 100% (155 total incl. TASK-012) + 20 hand tests incl. AC-025 verbatim. Release build 0w/0e; 308/308 solution tests |
| TASK-012 Data transfer instructions | 2026-10-27 13:55 +0500 | MOV (all forms: rm/reg, segment registers with `reg&3` aliasing, moffs A0-A3, reg-imm B0-BF, rm-imm C6/C7 with reg-field aliasing), XCHG (86/87 + 90-97), LEA/LDS/LES (memory forms; mod=11 halts undefined), XLAT, LAHF/SAHF, PUSH/POP reg+seg (50-5F, 06/07/0E/16/17/1E/1F), POP rm (8F), PUSH rm via FF/6, PUSHF/POPF; oracle-confirmed 8086 quirks (PUSH SP pushes decremented SP, POP SP loads popped value, PUSHF verbatim, POPF `(popped|0xF002)&~0x0028`); 71 SingleStepTests oracle files at 100% + 30 hand tests. Release build 0w/0e; 204/204 solution tests |
| TASK-011 Engine skeleton: decode, ModR/M, memory access | 2026-10-27 12:45 +0500 | `Cpu8086` (`ICpu`) with pure decode, prefix handling (segment overrides, REP capture, LOCK ignored), full 8086 ModR/M/EA table, 16-bit IP and EA wrap, INT imm8 interception contract (router + real IVT vectoring), unsupported/alias-opcode halt with state unchanged; `Decoder` internal seam + `FakeMemory` double; 37 hand tests. Release build 0w/0e; 103/103 solution tests |
| TASK-010 Core abstractions v1 (M1 start) | 2026-10-27 12:21 +0500 | `ICpu`, `CpuState`, `IMemory` + write journal (`MemoryChange`), `IInterruptRouter`, `StepResult` (`StepOutcome`/`HaltReason`), `IDisassembler`, `IMachine`, `IConsoleDevice`, `IOsServices`, `MachineSnapshot` + `SessionState`/`CurrentInstruction`/`MemoryWindow`/`StackWindow` (ADR-0007 v1.1 fields, types only), XML docs on all public abstractions; 8 model tests (CpuState equality/with-expressions, snapshot immutability). Release build 0w/0e; 66/66 solution tests |
| TASK-005 Bundled NASM tooling layout | 2026-10-27 12:00 +0500 | `build/fetch-nasm.ps1`, pinned NASM 3.02 win64 verified (SHA-256 `161d0bfa...`), `tools/nasm/` git-ignored with `SOURCE.txt` and `SHA256SUMS.txt`; `NasmLocator` test helper; 58/58 solution tests passed |
| TASK-004 Coverage tooling (coverlet with MTP) | 2026-10-27 10:48 +0500 | `coverlet.MTP` 10.1.0 (MIT), `build/coverage.ps1` → `artifacts/coverage/summary.md`; skeleton report generated; probe 4/5 merged as predicted; 53/53 tests |
| Review fixes TASK-001..003 (Claude Code, user request) | 2026-10-27 10:15 +0500 | Fetch script hardened + committed 8086 SHA-256 manifest (324/324 matched upstream git blobs); 16 `.csproj` rule tests (3 negative cases fail as expected); LF line endings (`.gitattributes`); 53/53 tests. 8088 v2 manifest pending |
| TASK-003 SingleStepTests oracle data fetch | 2026-10-26 20:18 +0500 | `build/fetch-singlesteptests.ps1`, 324 files verified/checksummed, SingleStepOracleHelper tests passed, 37/37 solution tests passed |
| TASK-002 Architecture rule test | 2026-10-26 20:01 +0500 | `tests/AssemblyStudio.ArchitectureTests/`, 23 architecture tests (ADR-0002 graph & UI isolation), negative test verified, 31/31 solution tests passed |
| TASK-001 Solution skeleton and build settings | 2026-10-26 19:42 +0500 | `AssemblyStudio.slnx`, 16 projects, Release build (0w/0e), 8/8 tests passed |
| SDLC-001 SDLC foundation (phase 0) | 2026-10-26 | `WORK_LOG.md` entry, files under `docs/`, `.claude/skills/` |
| SDLC-002 Multi-agent support (Claude Code + Antigravity) | 2026-10-26 | `WORK_LOG.md` entry, validation results |
| SDLC-009 OpenCode support (D-016) | 2026-10-27 20:24 +0500 | 11 commands, rules file, permission config; verified via OpenCode's local API (see `WORK_LOG.md`) |
| SDLC-008 Implementation plan (phase 5) | 2026-10-26 19:17 +0500 | Backlog v1.0 approved: 8 milestones, 62 tasks |
| SDLC-007 UI/UX design phase (phase 4) | 2026-10-26 19:03 +0500 | ui-spec v1.0 + design tokens v1.0 approved. ADR-0007 v1.1. D-015 |
| SDLC-006 Architecture phase (phase 3) | 2026-10-26 18:54 +0500 | Architecture v1.0 + ADR-0001..0009 approved, D-014 |
| POC-002 NASM listing + map source mapping | 2026-10-26 18:42 +0500 | **Sufficient with limitations**: `docs/02-research/poc-002.md`, `poc/POC-002-source-mapping/` |
| POC-001 Custom C# 8086 core feasibility | 2026-10-26 18:33 +0500 | **Feasible**: `docs/02-research/poc-001.md`, `poc/POC-001-cpu-core/` |
| SDLC-005 Requirements change v2.0 (D-011, D-012) | 2026-10-26 18:18 +0500 | Approved set v2.0, approval log below |
| SDLC-004 Research phase (phase 2) | 2026-10-26 17:59 +0500 | Approved research set v1.0 in `docs/02-research/` |
| SDLC-003 Requirements phase (phase 1) | 2026-10-26 17:11 +0500 | Approved set v1.0 in `docs/01-requirements/`, approval log below |

## Approved decisions
Architecture decisions: **ADR-0001 to ADR-0009 approved** (ADR-0009 code signing Deferred). Decisions **D-001 to D-015 approved**. ADR-0007 at v1.1.
See `DECISIONS.md`.

## Approval log
| Date | Artifact | Version | Approver | Scope / notes |
|---|---|---|---|---|
| 2026-10-26 16:39 +0500 | Process decisions D-002, D-003, D-005 (`DECISIONS.md`), incl. `docs/sdlc/agent-protocol.md` | 1.0 | User | Process and tooling decisions, not an SDLC phase gate |
| 2026-10-26 17:11 +0500 | **Requirements gate (phase 1)**: requirements set `docs/01-requirements/` + D-010 | 1.0 | User | "approve requirements v0.1 and D-010". PROPOSED → APPROVED. 12 NEEDS_RESEARCH items approved as goals. PROPOSED-basis targets remain unvalidated |
| 2026-10-26 17:59 +0500 | **Research gate (phase 2)**: `docs/02-research/` set + decisions D-011, D-012, D-013 | 1.0 | User | "approve research v0.1; D1=A; D2=add 00h; D3=approve POC-001,002 and install the SDK". Recommendations are input to ADRs, not decisions |
| 2026-10-26 18:54 +0500 | **Architecture gate (phase 3)**: `docs/03-architecture/architecture.md` + ADR-0001..0009 + reference machine (D-014) | 1.0 | User | "approve architecture v0.1 and ADR-0001..0009; reference machine OK". ADR-0009 code signing remains Deferred (OQ-020) |
| 2026-10-26 19:03 +0500 | **Design gate (phase 4)**: `docs/04-design/ui-spec.md` + `design-tokens.md`, ADR-0007 v1.1 amendment, Ctrl+C routing (D-015) | 1.0 | User | "approve", given to the gate question that named these four items |
| 2026-10-26 19:17 +0500 | **Plan gate (phase 5)**: `docs/05-planning/backlog.md` (all milestones M0–M7, TASK-001..105) | 1.0 | User | "approve backlog v0.1". All 62 tasks Approved |
| 2026-10-26 18:18 +0500 | **Requirements set v2.0** (D-011, D-012 applied; new AC-064) | 2.0 | User | "approve requirements v2.0". FR-039, FR-041, NFR-025 → APPROVED |

## Waivers
None.

## Outdated documents
None. Research documents revalidated against requirements v2.0.

## Latest build and test status
| Item | Status |
|---|---|
| Product build (2026-10-29 10:23 +0500) | `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** |
| Product tests (2026-10-29 10:24 +0500) | `dotnet test --solution AssemblyStudio.slnx -c Release` → **829/829 passed** (all 9 test projects; Os.Dos 42/42) |
| Coverage (2026-10-28 12:45 +0500) | `build/coverage.ps1` → exit 1 due to DEF-001 in App.Tests; Engine.I8086 alone has 49.7% coverage (951/1912 lines) |
| POC-001 build (2026-10-26 18:33 +0500) | `dotnet build poc/POC-001-cpu-core/Poc.Cpu.slnx -c Release` → **0 warnings, 0 errors** |
| POC-001 tests (2026-10-26 18:33 +0500) | `dotnet test --solution Poc.Cpu.slnx -c Release` → **94/94 passed** (178,000 SingleStepTests cases). Throwaway code, not product evidence |
| POC-002 build (2026-10-26 18:42 +0500) | `dotnet build poc/POC-002-source-mapping/Poc.Mapping.slnx -c Release` → **0 warnings, 0 errors** |
| POC-002 tests (2026-10-26 18:42 +0500) | `dotnet test --solution Poc.Mapping.slnx -c Release` → **24/24 passed** (real NASM 3.02). Throwaway code, not product evidence |
| Failed tests / open defects | DEF-001: `MainWindowSmokeTests.View_menu_opens_with_shortcuts_and_reset_layout` fails in non-interactive/background runner (`docs/07-testing/defects.md`) |

## Blockers
| ID | Blocker | Blocks | Resolution |
|---|---|---|---|
| ~~BLK-001~~ | **Resolved 2026-10-26 18:05 +0500**: .NET SDK 10.0.403 installed via WinGet (authorized D-013). `dotnet --list-sdks` → 10.0.403. A WPF template built with 0 warnings and 0 errors | — | — |

## Open high-impact questions
Authoritative list: `docs/01-requirements/OPEN_QUESTIONS.md`. High impact: OQ-001 (execution engine), OQ-002 (NASM
redistribution, CPU restriction, listing output), OQ-007 (specific permissive license. Model decided, D-007), OQ-008
(engine state access, breakpoints, snapshots, determinism).

## Environment (observed 2026-10-26)
| Item | Observation |
|---|---|
| OS | Windows 11 Pro 10.0.26200 |
| .NET | SDK 10.0.403 (installed 2026-10-26). Runtimes 10.0.14 (NETCore, WindowsDesktop, AspNetCore) and 8.0.8 |
| NASM | 3.02 on PATH (`C:\Program Files\Netwide Assembler 3.02`), observed via `nasm -v`. Not yet evaluated or approved |
| Git | Repository initialized 2026-10-26 (D-004) |
| Claude Code | 2.1.308 |
| Antigravity | CLI `agy` 1.4.7 (auto-updated from 1.4.4; 16 workspace skills re-verified). Desktop IDE 1.11.17 (`D:\Antigravity`) |
| OpenCode | v2.0.29 (`opencode`). Project config `opencode.jsonc` + `.opencode/` (D-016). Model and provider are per-user settings |
| JetBrains Rider | Project files present (`.idea/`, `.mcp.json`) |

## Changelog
- 2026-10-26: Created by migrating `docs/status/project-status.md` (SDLC-001 content) and adding SDLC-002 results.
