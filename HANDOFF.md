# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-11-01 17:16 +0500 by Claude Code (Opus 5.5), after completing TASK-081 (with layout D-017) |
| Current task | **None in progress. Next (user-approved): `TASK-082` memory and stack, then TASK-083** |
| Last completed | TASK-081 Registers and flags panels (Done 2026-11-01 17:16 +0500) |
| Git state | HEAD `1e4632e` + uncommitted TASK-081 changes (commit only when the user asks). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-11-01 16:53 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1464/1464 passed**, two consecutive runs (2026-11-01 17:07–17:11 and 17:11–17:15 +0500). Earlier runs that afternoon failed once each on DEF-015 (startup time), DEF-016 (View menu popup) and DEF-012.
- Notes: Debug runs can't judge the performance tests (unoptimized). While the user runs the app from `src/AssemblyStudio.App/bin/Release`, the Release build can't replace its files; never stop the user's app.

## Changes since the previous handoff
- **TASK-081 Registers and flags** (Claude Code, uncommitted):
  - `src/AssemblyStudio.App/Panels/Machine/{ValueFormat, CpuViewModel}.cs`; templates and panels in `MainWindow.xaml`; `BuildRunViewModel.Cpu` and `LatestSnapshot`.
  - **D-017 (user request):** register rows show AX–DX with their halves on the same row, values in boxes (`RegisterRow`, `RegisterChipTemplate`, `RegisterRowTemplate`); DECISIONS.md and ui-spec UI-005 updated. Note: the decision table in DECISIONS.md is not in numeric order; check the highest ID before adding one.
  - **For TASK-082:** reuse `CpuViewModel.Format` (or move it to a shared machine-panel object) for the memory and stack values; `MemoryWindows` need `IDebugSession.WatchMemory`; follow the same Show/MarkRunning/Clear pattern from `ApplyPendingSnapshot`. Ctrl+Alt+N is bound on `MachineRegion`; the memory and stack panels are in the bottom region, so add the binding there too.

- **TASK-084 Console** (Claude Code, uncommitted):
  - `src/AssemblyStudio.App/Panels/Console/{ConsoleScreen, ConsoleViewModel, ConsoleKeys, ConsoleView}.cs`; console tab in `MainWindow.xaml`; `BuildRunViewModel.Console` (bounded pending output); `ShellViewModel` tab follows the run state.
  - **For TASK-081..083:** same pattern as the console: give `BuildRunViewModel` a panel view model and feed it from `ApplyPendingSnapshot` (UI thread, latest snapshot only).

- **TASK-080 Build/Run/Step** (Claude Code, uncommitted):
  - `src/AssemblyStudio.App/Commands/{BuildRunViewModel, SessionFactory, CommandTable}.cs`, `src/AssemblyStudio.App/Panels/Problems/ProblemsViewModel.cs`.
  - `ShellViewModel.BuildRun`; the App composes it with Settings (assembler path, instruction budget) and the log.
  - **For TASK-084 (console):** `BuildRunViewModel.ConsoleText` holds the output since load/reset (cleared on Reset and on a new run); input goes through the session (`SendInput`); `SendCtrlCCommand` exists (Ctrl+Break). The console's own Ctrl+C routing (D-015) and Copy (Ctrl+Shift+C) are TASK-084.
  - **For TASK-081..083:** read the session through `BuildRunViewModel` (add a snapshot property or event there); running snapshots have no current instruction (FR-057).
  - Tests that wait for snapshots pump the dispatcher, so they must be in the `AppProcessTests` (non-parallel) collection.
- DEF-013 fixed (`Renderers.cs`), DEF-014 Open, OQ-026 Open.

- **TASK-066 Gutter and diagnostics** (Claude Code, uncommitted):
  - `src/AssemblyStudio.App/Editor/Adornments/`; `CodeEditor`, `DocumentViewModel`, `MainWindow.xaml`, strings.
  - **For TASK-080:** set `DocumentViewModel.Diagnostics` from `BuildResult.Diagnostics` (lines in the built file); bind `Breakpoints` and replace each with `Bound` + address or `Unbound` after a build; set `ExecutionLine`/`ExecutionMarker` from the paused snapshot through the source map; set `HasSession` while a session runs (editing then sets `IsSessionStale`).
- Test fixes: DEF-010 (latency test serial, best of 3), DEF-011 (clipboard retry). DEF-012 Open.

- **TASK-064 Editor** (Claude Code, uncommitted):
  - AvalonEdit package; `src/AssemblyStudio.App/Editor/{CodeEditor.cs, NasmHighlighting.cs, NasmSyntax.xshd}`; zoom in `ShellViewModel`/`LayoutState`/`EditorAppearance`/App; MainWindow editor template and Edit/View menu wiring.
  - **For TASK-066:** add the diagnostics squiggles (a background renderer) and the breakpoint/current-line margin to `CodeEditor`.
  - `MainWindow.ActiveEditor()` returns the active `CodeEditor`.
  - UI Automation sees the editor as ControlType **Document** (Value/Text patterns).

## Failed tests and known issues
- DEF-006 (flood test, failed in report 2026-10-30-1): fixed in TASK-087 (now `FloodPerformanceTests`, CPU-time limit); passed in two full runs (21:30, 21:33). **Fixed (unverified)** until `/assembly-test rerun DEF-006`.
- DEF-009 (coverage incomplete): fixed in TASK-087 (performance trait excluded); coverage run complete at 21:27. **Fixed (unverified)**.
- `DEF-001`: App smoke test can fail in background runners (WPF menu focus). Open.
- `DEF-002`: one full solution run stalled in the engine tests (2026-10-29 10:36). Not reproduced since.
- `DEF-003`: `ComLoader` accepts at most 65,278 bytes while FR-031 and the build allow 65,280. **Needs a user decision.**
- `WVR-001`: the AC-059 manual display check is waived until QA (BLK-002 resolved by the waiver); it must still pass before release.

## Decisions and constraints to respect
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-017.
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
1. When the user asks: commit TASK-081. Files:
   - `src/AssemblyStudio.App/{Panels/Machine/*, Commands/BuildRunViewModel.cs, Commands/CommandTable.cs, App.xaml.cs, Shell/MainWindow.xaml, Shell/MainWindow.xaml.cs, Shell/Strings.cs, Shell/Strings.resx}`
   - `tests/AssemblyStudio.App.Tests/{Panels/CpuTests.cs, Commands/CommandTableTests.cs}`
   - `DECISIONS.md` (D-017), `docs/04-design/ui-spec.md` (UI-005), `docs/07-testing/defects.md` (DEF-014 note, DEF-015, DEF-016), `docs/05-planning/backlog.md`, shared memory files
2. `/assembly-implement TASK-082`.
3. User: TASK-006, TASK-100, DEF-003 decision, OQ-026; AC-059 in QA (WVR-001). Test phase: `/assembly-test rerun` for DEF-006, 009, 010, 011, 013; DEF-012, DEF-014, DEF-015, DEF-016 to investigate (all load or focus sensitive).

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
