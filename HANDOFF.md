# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-28 15:28 +0500 by Antigravity (IDE, Gemini 3.8 Flash), after completing TASK-019 |
| Current task | **TASK-020 Engine conformance and performance gate: `Ready`, approved, not started, no owner** (TASK-061 themes also eligible) |
| Last completed | TASK-019 Disassembler adapter (Iced) (Done 2026-10-28 15:24 +0500) |
| Git state | HEAD `c2cdd8d` (not pushed). TASK-019 is committed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-28 15:13 +0500).
- `& ...\AssemblyStudio.Engine.I8086.Tests.exe -class "*IcedDisassemblerTests*"` → **76/76 passed** (2026-10-28 15:13 +0500).
- `dotnet test tests/AssemblyStudio.Engine.I8086.Tests/AssemblyStudio.Engine.I8086.Tests.csproj -c Release` → **589/589 passed** (283 oracle files at 100% + 76 disassembler tests + all hand tests).
- `dotnet test tests/AssemblyStudio.ArchitectureTests/AssemblyStudio.ArchitectureTests.csproj -c Release` → **39/39 passed** (2026-10-28 15:17 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **687/688 passed** (1 failure: pre-existing DEF-001 in App.Tests; 2026-10-28 15:21 +0500).

## Changes since the previous handoff
- **TASK-019 Disassembler adapter (Iced)** implemented:
  - Pinned `Iced 1.21.0` (MIT) in `Directory.Packages.props` and added reference in `src/AssemblyStudio.Engine.I8086/AssemblyStudio.Engine.I8086.csproj`.
  - Implemented `IDisassembler` in `src/AssemblyStudio.Engine.I8086/Disassembly/IcedDisassembler.cs`:
    - 16-bit decoding with NASM formatter configured with `SpaceAfterOperandSeparator = true`, `UppercaseHex = false`, and `ShowUselessPrefixes = true`.
    - Static helper `(string? Text, int Length) DisassembleInstruction(ReadOnlySpan<byte> bytes, ushort ip = 0)` for low-allocation decoding with relative branch target resolution.
    - Strict 8086-only filtering:
      - Rejects invalid encodings (`instruction.IsInvalid` or length 0).
      - Rejects 386+ segment prefixes (`Register.FS`, `Register.GS`).
      - Validates CPU features: requires all `instruction.CpuidFeatures` to be `CpuidFeature.INTEL8086` or `CpuidFeature.INTEL8086_ONLY`, filtering out 186+, 286+, 386+ extensions (e.g. `PUSHA`, `ENTER`, `SHL r, imm`, 32-bit registers/addressing).
      - Filters out 386+ prefixes (`0x66` operand size, `0x67` address size, `0x64` FS, `0x65` GS).
      - Rejects undocumented/unsupported MVP opcodes (e.g. `0xD6` SALC halts engine per ADR-0003 §4 and returns `null` from disassembler).
  - Resolved ADR-0003 unverified claim in `docs/decisions/ADR-0003-execution-engine.md`: verified `Iced 1.21.0` on .NET 10, confirmed `INTEL8086_ONLY` tags `Code.Popw_CS`, and verified that Iced's 16-bit decoder treats `0x0F` as invalid (lone byte) or 286+ two-byte escape, producing `null` in `IcedDisassembler` which matches the engine's `UnsupportedInstruction` halt.
- Tests in `tests/AssemblyStudio.Engine.I8086.Tests/IcedDisassemblerTests.cs`:
  - 28 documented 8086 instructions across all categories verified for NASM text and length agreement.
  - 9 prefix combinations (REP, REPE, REPNE, CS/ES/SS/DS segment overrides) tested.
  - Relative branch target calculations using IP.
  - Rejection of 186+ opcodes, 286/386+ opcodes and prefixes, and undocumented opcodes.
  - Length agreement test across 17 SingleStepTests oracle files (thousands of test cases): 100% agreement between Iced decoded length and oracle instruction byte length.
  - ADR-0003 verification test.
  - 76/76 disassembler tests passed.

## Milestone 1 Status
- **Instruction set**: complete (283 SingleStepTests oracle files at 100%).
- **Disassembly**: complete (`IcedDisassembler` with 8086-only filtering and NASM formatter).
- Remaining in Milestone 1:
  - `TASK-020`: Engine conformance and performance gate — ready to run.

## Failed tests and known issues
- `DEF-001`: `MainWindowSmokeTests.View_menu_opens_with_shortcuts_and_reset_layout` fails in non-interactive/background runners because WPF top-level Menu does not maintain submenu open state without active window focus. Recorded in `docs/07-testing/defects.md`.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-016.
- Core has no project references; Engine.I8086 references only Core.
- Every unsupported opcode halts with `UnsupportedInstruction` and pre-step state preserved (FR-037).
- Disassembler returns `null` on invalid or non-8086 instructions.

## Exact next action
1. Optional / when requested: commit TASK-019 changes (`git add ...; git commit -m "TASK-019: Disassembler adapter (Iced)"`).
2. Run `/assembly-implement TASK-020` (Engine conformance and performance gate, M1 gate) or `/assembly-implement TASK-061` (themes and contrast test).
3. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
