# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-30 21:34 +0500 by Claude Code (Opus 5.5), after WVR-001 and TASK-087 |
| Current task | **None in progress. Next (user-approved): `TASK-064` editor (AvalonEdit), then toward TASK-080, one task at a time with a commit after each** |
| Last completed | TASK-087 Test reliability (Done 2026-10-30 21:34 +0500) |
| Git state | HEAD `26c0a12` + uncommitted WVR-001/TASK-087 changes (commit only when the user asks). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-30 21:20 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1382/1382 passed**, two consecutive runs (2026-10-30 21:30 and 21:33 +0500).
- `build/coverage.ps1 -NoBuild` → complete (21:27): engine 99.6%, runtime 93.5%.

## Changes since the previous handoff
- **WVR-001 and TASK-087** (Claude Code, uncommitted):
  - WVR-001: AC-059 postponed to QA, TASK-061 Done under it.
  - TASK-087: `FloodPerformanceTests` (CPU-time limit), `Category=Performance` trait + `build/coverage.ps1` filter, 6 SkeletonTests deleted.
  - Test plan v0.2, defects DEF-006/009 Fixed (unverified).

## Failed tests and known issues
- DEF-006 (flood test, failed in report 2026-10-30-1): fixed in TASK-087 (now `FloodPerformanceTests`, CPU-time limit); passed in two full runs (21:30, 21:33). **Fixed (unverified)** until `/assembly-test rerun DEF-006`.
- DEF-009 (coverage incomplete): fixed in TASK-087 (performance trait excluded); coverage run complete at 21:27. **Fixed (unverified)**.
- `DEF-001`: App smoke test can fail in background runners (WPF menu focus). Open.
- `DEF-002`: one full solution run stalled in the engine tests (2026-10-29 10:36). Not reproduced since.
- `DEF-003`: `ComLoader` accepts at most 65,278 bytes while FR-031 and the build allow 65,280. **Needs a user decision.**
- `WVR-001`: the AC-059 manual display check is waived until QA (BLK-002 resolved by the waiver); it must still pass before release.

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

- **Durable working notes (TASK-060 to the testing phase):**
  - Tests that launch the real app must pass temporary `--settings`, `--recovery` and `--log` paths, and belong to the non-parallel `AppProcessTests` collection.
  - The WPF test thread sets `App.ComposeOnStartup = false` and sets `ShutdownMode` after `InitializeComponent` (DEF-007).
  - Strings: edit `Shell/Strings.resx`, then run `build/generate-strings.ps1`.
  - Content: files under `src/AssemblyStudio.Content/Content/` plus `catalog.json` entries (samples need `expectedOutput`, and `input` when they read the keyboard; tutorials need `sample`). Text is stored with LF.
  - The instruction reference and the "Unsupported instructions" help page are generated: edit `ReferenceData/instructions.json`, run `ReferenceTests`/`HelpTests` with `ASSEMBLY_STUDIO_WRITE_REFERENCE=1`, rebuild, run the tests again.
  - The help consistency tests (TC-029, TC-043, TC-056) fail when DOS functions, NASM arguments, build codes, limits, start state or menu shortcuts change: update the help with them.
  - Installer: the AppId in `build/installer/AssemblyStudio.iss` must never change. Run ISCC from PowerShell (Git Bash rewrites `/Q` and `/D`). `test-installer.ps1` always uninstalls what it installed.
  - Performance tests carry `[Trait("Category", "Performance")]` and a non-parallel collection; the coverage script excludes the trait.
  - Test cases: one TC per test class (inventory in `docs/07-testing/test-plan.md`); new classes take the next free number.

## Exact next action
1. When the user asks: commit WVR-001 + TASK-087. Files:
   - `PROJECT_STATE.md` (waiver, approval, blocker), `docs/05-planning/backlog.md` (TASK-061 Done, TASK-087)
   - `tests/AssemblyStudio.Debugging.Tests/Robustness/{FloodPerformanceTests.cs, HostileProgramTests.cs}`
   - tagged test files: `tests/AssemblyStudio.Engine.I8086.Tests/EnginePerformanceTests.cs`, `tests/AssemblyStudio.Toolchain.Nasm.Tests/BuildTimeTests.cs`, `tests/AssemblyStudio.App.Tests/StartupTimeTests.cs`, `tests/AssemblyStudio.Debugging.Tests/{DebugSessionTests.cs, SnapshotTests.cs}`, `tests/AssemblyStudio.Runtime.Tests/ConsoleDeviceTests.cs`
   - the deleted `tests/*/SkeletonTests.cs` (6), `build/coverage.ps1`, `docs/07-testing/{test-plan.md, defects.md}`, shared memory files
2. `/assembly-implement TASK-064` (user-approved path to Run/Debug).
3. User: TASK-006, TASK-100, DEF-003 decision; AC-059 in QA (WVR-001).

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
