# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-30 20:05 +0500 by Claude Code (Opus 5.5), after completing TASK-093 |
| Current task | **None in progress. M4: TASK-061 Blocked (BLK-002), so the editor tasks and M5 wait. M6: all tasks Done. Eligible: `TASK-101` installer** |
| Last completed | TASK-093 Help (Done 2026-10-30 20:05 +0500) |
| Git state | HEAD `aabcfea` (TASK-093 committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-30 19:58 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1383/1383 passed**, two consecutive runs (2026-10-30 20:02 and 20:05 +0500).

## Changes since the previous handoff
- **TASK-093 Help** (Claude Code, uncommitted):
  - `src/AssemblyStudio.Content/Content/Help/*.txt` (7), catalog entries.
  - `NasmAssembler.Arguments` (internal, extracted).
  - Tests: `ProgramTests/HelpTests.cs`, `Toolchain.Nasm.Tests/HelpConsistencyTests.cs` (+ Content reference), `App.Tests/Learn/HelpPagesTests.cs`.
  - **When behavior changes, update the help:** these tests fail when the DOS functions, NASM arguments, build codes, limits, start state or menu shortcuts change.
  - DEF-003 is described in the scope and differences pages; update them when it is resolved.

## Failed tests and known issues
- `DEF-001`: App smoke test can fail in background runners (WPF menu focus). Open.
- `DEF-002`: one full solution run stalled in the engine tests (2026-10-29 10:36). Not reproduced since.
- `DEF-003`: `ComLoader` accepts at most 65,278 bytes while FR-031 and the build allow 65,280. **Needs a user decision.**
- `BLK-002`: TASK-061 waits for the user's manual AC-059 display check.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-016.
- `AssemblyStudio.Runtime`, `AssemblyStudio.Os.Dos`, and `AssemblyStudio.Toolchain.Nasm` reference only `AssemblyStudio.Core` (ADR-0002 graph, enforced by architecture tests).
- Console buffer cap is 1 MB with oldest output trimmed. Input events stamped with instruction count for replay determinism.
- Composition for a runnable machine (as in `DosServicesTests.Rig`):
  1. `memory = new RealModeMemory()`
  2. `dos = new DosServices(psp)`
  3. `router = new InterruptRouter(memory, dos)`
  4. `cpu = new Cpu8086(memory, router)`
  5. `new Machine(cpu, memory, console, router, dos)`, then `ComLoader.Load(machine, image)`
- For TASK-050: a `RuntimeError` halt from a DOS service carries its message in `DosServices.LastError`. The router's `LastHalt` covers only unsupported services and CPU exceptions.

## Exact next action
1. Done: TASK-093 committed. Files were:
   - `src/AssemblyStudio.Content/Content/{catalog.json, Help/*}`
   - `src/AssemblyStudio.Toolchain.Nasm/NasmAssembler.cs`
   - `tests/AssemblyStudio.ProgramTests/HelpTests.cs`
   - `tests/AssemblyStudio.Toolchain.Nasm.Tests/{AssemblyStudio.Toolchain.Nasm.Tests.csproj, HelpConsistencyTests.cs}`
   - `tests/AssemblyStudio.App.Tests/Learn/HelpPagesTests.cs`
   - `docs/05-planning/backlog.md`, shared memory files
2. User: AC-059 check → TASK-061 → TASK-064 and M5. Agent meanwhile: `/assembly-implement TASK-101` or `/assembly-test`.
3. User: TASK-006, TASK-100, DEF-003 decision, the first version number, the Learn reading-width proposal.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
