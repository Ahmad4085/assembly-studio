# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-30 11:51 +0500 by Claude Code (Opus 5.5), after completing TASK-062 |
| Current task | **None in progress. M1–M3 complete; M4: TASK-060, TASK-062 Done, TASK-061 Blocked (BLK-002). Next eligible: `TASK-063` crash recovery, `TASK-064` editor integration** |
| Last completed | TASK-062 Document service and file workflow (Done 2026-10-30 11:51 +0500) |
| Git state | HEAD `c2e6f06` (TASK-062 committed). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-30 11:43 +0500).
- `dotnet test --project tests/AssemblyStudio.App.Tests -c Release` → **102/102 passed** (2026-10-30 11:44 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1116/1116 passed**, two consecutive runs (2026-10-30 11:47 and 11:50 +0500).

## Changes since the previous handoff
- **TASK-062 Document service and file workflow** (Claude Code, uncommitted):
  - `src/AssemblyStudio.App/Documents/*` (codec, atomic file, errors, dialogs, recent files, document and workspace view models). `ShellViewModel.Workspace` (optional constructor parameter; the default uses WPF dialogs and an in-memory recent list). App composes `JsonRecentFilesStore(settingsPath)` and opens an Untitled document at startup.
  - `MainWindow.xaml`:
    - The editor area is `DocumentTabs` (tab strip only, empty content template) + `DocumentEditors` (ItemsControl over Documents, a Grid panel, each item visible when `IsActive`, holding a TextBox `EditorText`).
    - **TASK-064 should replace the TextBox in that item template with AvalonEdit**, keeping one editor instance per document (AC-006), and keep `MainWindow.ActiveEditor()` returning the active one for focus.
  - Strings: edit `Shell/Strings.resx`, then run `powershell -ExecutionPolicy Bypass -File build/generate-strings.ps1`.
  - `Shell/MessageDialog` (UI-019) and `Shell/VisibilityConverters.cs`.

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
1. Done: TASK-062 committed. Files were:
   - `build/generate-strings.ps1`
   - `src/AssemblyStudio.App/{App.xaml.cs, Documents/*, Shell/MainWindow.xaml, Shell/MainWindow.xaml.cs, Shell/MessageDialog.xaml(.cs), Shell/ShellViewModel.cs, Shell/Strings.resx, Shell/Strings.cs, Shell/VisibilityConverters.cs}`
   - `tests/AssemblyStudio.App.Tests/Documents/*`
   - `docs/05-planning/backlog.md`, shared memory files
2. `/assembly-implement TASK-064` (AvalonEdit editor integration; ADR-0006) or `TASK-063` (crash recovery).
3. User: AC-059 check (BLK-002), TASK-006 test VMs, DEF-003 decision.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
