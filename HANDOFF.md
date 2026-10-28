# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-28 13:28 +0500 by Antigravity (IDE, Gemini 3.8 Flash), after completing TASK-017 |
| Current task | **TASK-018 Flag ops, misc and the full opcode table: `Ready`, approved, not started, no owner** (TASK-061 themes also eligible) |
| Last completed | TASK-017 String instructions and REP prefixes (Done 2026-10-28 13:28 +0500) |
| Git state | HEAD `398ee36` (not pushed). TASK-017 is committed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-28 13:20 +0500).
- `dotnet test tests/AssemblyStudio.Engine.I8086.Tests/AssemblyStudio.Engine.I8086.Tests.csproj -c Release` → **466/466 passed** (2026-10-28 13:23 +0500); 276 oracle files at 100%.
- `dotnet test tests/AssemblyStudio.ArchitectureTests/AssemblyStudio.ArchitectureTests.csproj -c Release` → **39/39 passed** (2026-10-28 13:23 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **564/565 passed** (1 failure: pre-existing DEF-001 in App.Tests; 2026-10-28 13:27 +0500).

## Changes since the previous handoff
- TASK-017: String instructions and REP prefixes implemented in `src/AssemblyStudio.Engine.I8086/Cpu8086.Strings.cs` and `Cpu8086.cs`:
  - Opcodes implemented:
    - `0xA4`: `MOVSB`
    - `0xA5`: `MOVSW`
    - `0xA6`: `CMPSB`
    - `0xA7`: `CMPSW`
    - `0xAA`: `STOSB`
    - `0xAB`: `STOSW`
    - `0xAC`: `LODSB`
    - `0xAD`: `LODSW`
    - `0xAE`: `SCASB`
    - `0xAF`: `SCASW`
  - Prefixes supported: `F2` (REPNZ / REPNE) and `F3` (REP / REPZ / REPE).
  - Segment rules: Destination `[ES:DI]` is always relative to ES (unoverridable by prefixes); Source `[DS:SI]` defaults to DS and respects segment override prefixes (CS/DS/ES/SS).
  - Direction flag: `DF=0` increments pointer(s) by 1 (byte) or 2 (word); `DF=1` decrements by 1 (byte) or 2 (word); addresses wrap at 16 bits within segment.
  - Flags: `MOVS`, `STOS`, `LODS` do not modify any flags; `CMPS` (`[src] - [dst]`) and `SCAS` (`acc - [dst]`) compute arithmetic flags via subtraction (CF, AF, ZF, SF, PF, OF).
  - Repeat behavior:
    - `CX=0` with REP prefix is a no-op (advances IP past the instruction without touching registers, memory or flags).
    - `MOVS`, `STOS`, `LODS` loop while `CX != 0` (decrementing CX each iteration).
    - `CMPS`, `SCAS`:
      - `REPE` / `REPZ` (F3): loops while `CX != 0` and `ZF == 1` (terminates early on mismatch `ZF == 0`).
      - `REPNE` / `REPNZ` (F2): loops while `CX != 0` and `ZF == 0` (terminates early on match `ZF == 1`).
    - Without REP prefix: executes 1 iteration and leaves CX untouched.
  - Documented decision on per-iteration step semantics: In `ICpu.StepInstruction`, an instruction with a REP prefix executes as a single, complete instruction step until termination, advancing IP past the instruction.
- Tests:
  - 10 SingleStepTests oracle files added to `SingleStepOracleTests.cs` (A4, A5, A6, A7, AA, AB, AC, AD, AE, AF), passing 100% across all cases (276 total oracle files).
  - 16 hand tests in `StringInstructionTests.cs` verifying MOVSB/MOVSW, DF=0/1, 16-bit address wrap, source segment override, fixed ES destination, STOS, LODS, CMPS, SCAS, CX=0 no-op, REP loops, and REPE/REPNE early termination.

## Failed tests and known issues
- `DEF-001`: `MainWindowSmokeTests.View_menu_opens_with_shortcuts_and_reset_layout` fails in non-interactive/background runners because WPF top-level Menu does not maintain submenu open state without active window focus. Recorded in `docs/07-testing/defects.md`.
- Engine: F6/F7 /1 and FF arms /7 halt until their tasks (or out of scope). Flag ops and misc (F4, F5, F8-FD, 9B) belong to TASK-018.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-016.
- Core has no project references; Engine.I8086 references only Core.
- String instruction destination is strictly `ES:DI` and cannot be overridden.

## Exact next action
1. Optional: commit TASK-017 pending changes (`git add ...; git commit -m "TASK-017: String instructions and REP prefixes"`).
2. User runs `/assembly-implement TASK-018` (Flag ops, misc and full opcode table — the final instruction task of M1!) or `/assembly-implement TASK-061` (themes and contrast test).
3. In parallel (user action): TASK-006 test VMs.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
