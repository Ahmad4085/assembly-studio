# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-30 16:09 +0500 by Claude Code (Opus 5.5), after completing TASK-091 |
| Current task | **None in progress. M4: TASK-060, 062, 063, 067, 068 Done; TASK-061 Blocked (BLK-002), so the editor tasks and M5 wait. M6: TASK-090, 091 Done. Eligible: `TASK-092`, `TASK-093`, `TASK-101`** |
| Last completed | TASK-091 Sample programs with automated verification (Done 2026-10-30 16:09 +0500) |
| Git state | HEAD `ca3d160` + uncommitted TASK-091 changes (commit only when the user asks). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-30 16:00 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1214/1214 passed**, two consecutive runs (2026-10-30 16:05 and 16:08 +0500).

## Changes since the previous handoff
- **TASK-091 Sample programs** (Claude Code, uncommitted):
  - 8 samples in `src/AssemblyStudio.Content/Content/Samples/*.asm` plus their `catalog.json` entries. `ContentItem.Input` holds the documented keyboard input.
  - `tests/AssemblyStudio.ProgramTests/SampleTests.cs` runs every catalog sample. **Adding a sample = add the .asm + a catalog entry with `expectedOutput` (and `input` if it reads the keyboard); the test picks it up.**
  - Reminder: DOS 09h returns AL = 24h; save AX around print calls.

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
1. When the user asks: commit TASK-091. Files:
   - `src/AssemblyStudio.Content/{ContentCatalog.cs, Content/catalog.json, Content/Samples/*}`
   - `tests/AssemblyStudio.ProgramTests/{AssemblyStudio.ProgramTests.csproj, ProgramHarness.cs, SampleTests.cs}`
   - `tests/AssemblyStudio.Content.Tests/{ContentCatalogTests.cs, Fixtures/input-on-tutorial/*}`
   - `tests/AssemblyStudio.App.Tests/Learn/LearnTests.cs`
   - `docs/05-planning/backlog.md`, shared memory files
2. `/assembly-implement TASK-092` (tutorials and reference) or `TASK-093` (help). User: AC-059 check → TASK-061 → TASK-064 and M5.
3. User: TASK-006 test VMs, TASK-100 license decision, DEF-003 decision, the first version number.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
