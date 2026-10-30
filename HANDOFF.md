# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-30 16:48 +0500 by Claude Code (Opus 5.5), after completing TASK-092 |
| Current task | **None in progress. M4: TASK-060, 062, 063, 067, 068 Done; TASK-061 Blocked (BLK-002), so the editor tasks and M5 wait. M6: TASK-090, 091, 092 Done. Eligible: `TASK-093` help, `TASK-101` installer** |
| Last completed | TASK-092 Tutorials and reference (Done 2026-10-30 16:48 +0500) |
| Git state | HEAD `82c493a` (TASK-092 committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-30 16:28 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1315/1315 passed**, two consecutive runs (2026-10-30, finished 16:34 and after).
- Engine suite including the SingleStepTests oracle: 629/629 after the DEF-008 fix.

## Changes since the previous handoff
- **TASK-092 Tutorials and reference** (Claude Code, uncommitted):
  - Content: `Tutorials/*.txt` (5), `Reference/*.txt` (4 concept + 7 generated instruction pages), catalog entries, `ContentItem.Sample` (tutorial → sample link, validated).
  - Data: `src/AssemblyStudio.Content/ReferenceData/instructions.json` (not embedded).
  - **To change the instruction reference:** edit `instructions.json`, then run `ReferenceTests` with `ASSEMBLY_STUDIO_WRITE_REFERENCE=1`, rebuild (so the pages are embedded), and run the tests again.
  - **DEF-008 engine change** in `Cpu8086.Arithmetic.cs` and `Cpu8086.Logic.cs`: reports AF/OF undefined. The Flags panel (TASK-081) should show `UndefinedFlags` as undefined.
  - Learn: tutorial sample-link button (`LearnViewModel.LinkedSample`, `OpenLinkedSampleCommand`).
  - Open proposal: a wider reading view for Learn text (UI-015 design decision).

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
1. Done: TASK-092 committed. Files were:
   - `src/AssemblyStudio.Content/{ContentCatalog.cs, ReferenceData/instructions.json, Content/catalog.json, Content/Tutorials/*, Content/Reference/*}`
   - `src/AssemblyStudio.Engine.I8086/{Cpu8086.Arithmetic.cs, Cpu8086.Logic.cs}`
   - `src/AssemblyStudio.App/{Learn/LearnViewModel.cs, Shell/MainWindow.xaml, Shell/Strings.resx, Shell/Strings.cs}`
   - `tests/AssemblyStudio.ProgramTests/{ProgramHarness.cs, ReferenceTests.cs}`
   - `tests/AssemblyStudio.Engine.I8086.Tests/UndefinedFlagMaskTests.cs`
   - `tests/AssemblyStudio.Content.Tests/{ContentCatalogTests.cs, Fixtures/valid/catalog.json, Fixtures/tutorial-without-sample/*}`
   - `tests/AssemblyStudio.App.Tests/Learn/LearnTests.cs`
   - `docs/05-planning/backlog.md`, `docs/07-testing/defects.md`, shared memory files
2. `/assembly-implement TASK-093` (help). User: AC-059 check → TASK-061 → TASK-064 and M5.
3. User: TASK-006, TASK-100, DEF-003 decision, the first version number, the Learn reading-width proposal.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
