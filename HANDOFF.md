# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-28 12:45 +0500 by Antigravity (IDE, Gemini 3.8 Flash), after completing TASK-016 |
| Current task | **TASK-017 String instructions and REP prefixes: `Ready`, approved, not started, no owner** (TASK-061 themes also eligible) |
| Last completed | TASK-016 Control transfer and interrupts (Done 2026-10-28 12:45 +0500) |
| Git state | HEAD `e3812b0` (not pushed). TASK-016 is committed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → 0 warnings, 0 errors (2026-10-28 12:45 +0500).
- `dotnet test tests/AssemblyStudio.Engine.I8086.Tests/AssemblyStudio.Engine.I8086.Tests.csproj -c Release` → **440/440 passed** (2026-10-28 12:45 +0500); 266 oracle files at 100%.
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **538/539 passed** (1 failure: DEF-001 in App.Tests).

## Changes since the previous handoff
- TASK-016: Control transfer and interrupts implemented in `src/AssemblyStudio.Engine.I8086/Cpu8086.ControlTransfer.cs` and `Cpu8086.Arithmetic.cs`:
  - 70-7F conditional jumps: all 16 condition codes evaluated against FLAGS, 8-bit sign-extended displacement, 16-bit IP wrap; FLAGS remain completely untouched.
  - E3 JCXZ: jumps on CX=0; leaves FLAGS untouched.
  - E0-E2 Loops: LOOP, LOOPE/LOOPZ, LOOPNE/LOOPNZ decrement CX without flag modification; jump conditions verified.
  - EB, E9, EA: JMP short (rel8), near (rel16), and far (ptr16:16).
  - E8, 9A: CALL near (rel16) and far (ptr16:16), pushing return address and segment.
  - C2, C3, CA, CB: RET near/far with optional immediate byte stack cleanup.
  - FF /2-/5: Group 5 near/far indirect CALL and JMP; mod=3 halts `UnsupportedInstruction`.
  - CC (INT 3), CD (INT imm8), CE (INTO on OF=1): Router interception evaluated first; null router vectors through IVT with FLAGS/CS/return IP push and IF/TF clear.
  - CF (IRET): Pops IP, CS, and FLAGS with canonical 8086 reserved bits.
  - Fixed IVT entry calculation from `vector * 2` to `vector * 4` (2-byte IP + 2-byte CS per vector in real hardware).
- 37 new SingleStepTests oracle files added to `SingleStepOracleTests.cs` (266 total, all at 100%).
- 37 hand tests in `ControlTransferTests.cs`.
- Logged `DEF-001` in `docs/07-testing/defects.md` for pre-existing `MainWindowSmokeTests.View_menu_opens_with_shortcuts_and_reset_layout` in App.Tests.

## Failed tests and known issues
- `DEF-001`: `MainWindowSmokeTests.View_menu_opens_with_shortcuts_and_reset_layout` fails in non-interactive/background runners because WPF top-level Menu does not maintain submenu open state without active window focus. Recorded in `docs/07-testing/defects.md`.
- Engine: F6/F7 /1 and FF arms /7 halt until their tasks (or out of scope). String instructions (A4-A7, AA-AF) belong to TASK-017.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-016.
- Core has no project references; Engine.I8086 references only Core.
- Interrupt vector table entries are 4 bytes (2-byte offset, 2-byte segment).

## Exact next action
1. Optional: commit TASK-016 pending changes (`git add ...; git commit -m "TASK-016: Control transfer and interrupts"`).
2. User runs `/assembly-implement TASK-017` (string instructions and REP prefixes) or `/assembly-implement TASK-061` (themes and contrast test).
3. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
