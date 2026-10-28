# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-28 14:06 +0500 by Antigravity (IDE, Gemini 3.8 Flash), after completing TASK-018 |
| Current task | **TASK-019 Disassembler adapter (Iced): `Ready`, approved, not started, no owner** (TASK-061 themes also eligible) |
| Last completed | TASK-018 Flag ops, misc and the full opcode table (Done 2026-10-28 14:06 +0500) |
| Git state | HEAD `7e5ac5d` (not pushed). TASK-018 is committed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-28 13:52 +0500).
- `dotnet test tests/AssemblyStudio.Engine.I8086.Tests/AssemblyStudio.Engine.I8086.Tests.csproj -c Release` → **513/513 passed** (2026-10-28 14:01 +0500); 283 oracle files at 100%.
- `dotnet test tests/AssemblyStudio.ArchitectureTests/AssemblyStudio.ArchitectureTests.csproj -c Release` → **39/39 passed** (2026-10-28 14:02 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **611/612 passed** (1 failure: pre-existing DEF-001 in App.Tests; 2026-10-28 14:06 +0500).

## Changes since the previous handoff
- TASK-018: Flag ops, misc and the full opcode table implemented, completing all instruction set implementation tasks in Milestone 1:
  - Flag operations: CLC (F8), STC (F9), CMC (F5), CLD (FC), STD (FD), CLI (FA), STI (FB) in `src/AssemblyStudio.Engine.I8086/Cpu8086.FlagsMisc.cs` and dispatch in `Cpu8086.cs`.
  - Processor control: HLT (F4) halts execution reporting `StepOutcome.Halted` and `HaltReason.Halt`, with IP advanced past HLT.
  - Synchronization: WAIT / FWAIT (9B) is a no-op on 8086 without 8087 coprocessor (advances IP by 1, flags/registers untouched).
  - Prefix LOCK (F0) is ignored per ADR-0003 §4 in `Decoder.ParsePrefixes`.
  - Coprocessor ESC (D8-DF) and Port I/O (E4-E7, EC-EF) explicitly unsupported in MVP, halting with `UnsupportedInstruction` and state preserved byte-identical (FR-037).
  - Undocumented aliases (0F, 60-6F, C0-C1, C8-C9, D6, F1) halt with `UnsupportedInstruction`.
  - Created `OpcodeTable.cs` in `AssemblyStudio.Engine.I8086` enumerating and classifying all 256 opcode bytes into Supported (206), Prefix (7), and Unsupported (43).
  - Created `docs/06-implementation/opcode-support.md` documenting complete opcode space, policies, Group ModR/M arms, and full 256-opcode reference table for help (FR-045).
- Tests:
  - 7 SingleStepTests oracle files added to `SingleStepOracleTests.cs` (F5, F8, F9, FA, FB, FC, FD), passing 100% across all cases (283 total oracle files).
  - 40 hand tests in `FlagsMiscAndOpcodeTableTests.cs` verifying CLC, STC, CMC, CLD, STD, CLI, STI, WAIT, HLT, Coprocessor ESC, Port I/O, undocumented aliases, Group 3/4/5 ModR/M arm boundaries, and an exhaustive 256-opcode table agreement test ensuring the engine matches the table for all 256 opcodes.

## Milestone 1 Status
- **Instruction set complete**: All 8086 opcode families (Data transfer, Arithmetic, Logic/shift/rotate, Mul/div/BCD, Control transfer, Strings, Flags/misc) are implemented, tested against 283 SingleStepTests hardware oracle files (100% pass), and documented.
- Remaining in Milestone 1:
  - `TASK-019`: Disassembler adapter (Iced) — `Ready`
  - `TASK-020`: Engine conformance and performance gate — depends on TASK-018, TASK-019

## Failed tests and known issues
- `DEF-001`: `MainWindowSmokeTests.View_menu_opens_with_shortcuts_and_reset_layout` fails in non-interactive/background runners because WPF top-level Menu does not maintain submenu open state without active window focus. Recorded in `docs/07-testing/defects.md`.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-016.
- Core has no project references; Engine.I8086 references only Core.
- Every unsupported opcode halts with `UnsupportedInstruction` and pre-step state preserved (FR-037).

## Exact next action
1. Optional: commit TASK-018 pending changes (`git add ...; git commit -m "TASK-018: Flag ops, misc and the full opcode table"`).
2. User runs `/assembly-implement TASK-019` (Disassembler adapter (Iced)) or `/assembly-implement TASK-061` (themes and contrast test).
3. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
