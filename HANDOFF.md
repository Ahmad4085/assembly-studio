# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-11-02 13:11 +0500 by Claude Code (Opus 5.5), after completing TASK-085 |
| Current task | **None in progress. Next: `TASK-086` end-to-end performance check (last M5 task)** |
| Last completed | TASK-085 Announcements, keyboard-only flow and UI smoke tests (Done 2026-11-02 13:11 +0500) |
| Git state | HEAD `fed96b8` (TASK-085 committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-11-02 12:54 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1503 passed, 1 skipped, 0 failed**, two consecutive runs (2026-11-02 12:54–13:01 and 13:01–13:06 +0500). The skipped test is the opt-in real-key UI test (D-018); it passed separately at 12:52.
- Notes: full runs now take 5–6.5 min (the UI test project starts the app). Debug runs can't judge the performance tests. While the user runs the app from `src/AssemblyStudio.App/bin/Release`, the Release build can't replace its files; never stop the user's app. Real-key UI tests: `ASSEMBLY_STUDIO_UI_INPUT=1 dotnet test --project tests/AssemblyStudio.App.UiTests -c Release`, with the user's agreement and the user away from the PC (D-018).

## Changes since the previous handoff
- **TASK-085** (Claude Code, Done; Narrator check by the user):
  - Announcements: `BuildRunViewModel.Announcement`/`Announced`; `AnnouncementRegion` (polite live region) in `MainWindow.xaml`; the state badge no longer announces.
  - New project `tests/AssemblyStudio.App.UiTests` (FlaUI.UIA3 5.0.0; `Directory.Packages.props`, `AssemblyStudio.slnx`): `AppUnderTest` (temp folders, focus guard, waits for a click), `UiSmokeTests` (structure; AC-043 opt-in).
  - D-018 (opt-in real-key tests), ADR-0008 changelog (licenses), accessibility-findings.md Part C.
  - **For TASK-086:** NFR targets and the existing performance tests (`Category=Performance`) are the starting point; the UI test project can drive end-to-end timings without key presses.

- **TASK-069 Editor accessibility** (Claude Code, Done; Narrator walkthrough run by the user, overall pass):
  - `CodeEditor`: TextArea UIA name bound to the editor's name + help text; `tests/AssemblyStudio.App.Tests/Accessibility/EditorAccessibilityTests.cs`; `docs/06-implementation/accessibility-findings.md` (Part A measured, Part B manual walkthrough NOT RUN); ADR-0006 changelog.
  - **For TASK-085:** the step announcement ("Paused at line 7. Changed: AX, DX, ZF.") can use `CpuViewModel.ChangedNames`; the state badge already raises LiveRegionChanged.

- **TASK-083 Session panels** (Claude Code, uncommitted):
  - `src/AssemblyStudio.App/Panels/Session/SessionPanels.cs`; strip and tabs in `MainWindow.xaml`; `BuildRunViewModel.{Strip, Messages, History, Breakpoints}` and the event log (`LogEvent`).
  - **Additive contract change:** `StepResult.Message` (`RuntimeMessage` in Core) set by `InterruptRouter`; `RuntimeErrorPolicy` RTE010/011/090; `SnapshotContent` keeps 6 bytes for an undecodable instruction. ADR-0007 changelog updated.
  - **For TASK-085:** `CpuViewModel.ChangedNames` and the runtime messages list (polite live region) are ready for the step announcement ("Paused at line 7. Changed: AX, DX, ZF.").

- **TASK-082 Memory and stack** (Claude Code, uncommitted):
  - `src/AssemblyStudio.App/Panels/Memory/{MemoryAddress, MemoryViewModel}.cs`; `MemoryPanelTemplate`/`StackPanelTemplate` in `MainWindow.xaml` (docked and compact tabs); `BuildRunViewModel.Memory` and the write-journal collection.
  - **For TASK-083:** the history panel can use `IDebugSession.History` (or accumulate `HistoryDelta` like the write journal); breakpoints already live on `DocumentViewModel.Breakpoints` and `IDebugSession.Breakpoints`; the runtime messages come from `RunState`/`Detail`/`ExitCode` in `BuildRunViewModel`; `ApplyPendingSnapshot` now separates a new pause from a refresh (`newPause`).

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
- ADR-0001..0009 (0007 v1.1, 0008 v1.2), D-001..D-018.
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
1. Done: TASK-085 committed (see Git state).
2. `/assembly-implement TASK-086` (end-to-end performance check), the last M5 task.
3. User: TASK-006, TASK-100, DEF-003 decision, OQ-026; AC-059 in QA (WVR-001). Test phase: `/assembly-test rerun` for DEF-006, 009, 010, 011, 013; DEF-012, DEF-014, DEF-015, DEF-016 to investigate.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
