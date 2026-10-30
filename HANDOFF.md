# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-30 13:54 +0500 by Claude Code (Opus 5.5), after completing TASK-068 |
| Current task | **None in progress. M4: TASK-060, 062, 063, 067, 068 Done; TASK-061 Blocked (BLK-002), so TASK-064/065/066/069 wait; TASK-070 also needs TASK-006. Eligible: `TASK-090` content catalog, `TASK-101` installer** |
| Last completed | TASK-068 About/Diagnostics, release notes, notices, logging (Done 2026-10-30 13:54 +0500) |
| Git state | HEAD `c2af6fc` + uncommitted TASK-068 changes (commit only when the user asks). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-30 13:46 +0500).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1175/1175 passed**, two consecutive runs (2026-10-30 13:50 and 13:53 +0500), with no files written to the user's real log or recovery folders. A run at 13:38 had 1174/1175: DEF-006 (Open).

## Changes since the previous handoff
- **TASK-068 About/Diagnostics, release notes, notices, logging** (Claude Code, uncommitted):
  - `src/AssemblyStudio.App/Logging/FileLog.cs` (`ILog`, `NullLog`, `FileLog`) and `src/AssemblyStudio.App/About/{AboutViewModel.cs, AboutWindow.xaml(.cs)}`.
  - `WorkspaceViewModel`, `AutosaveService` and `MainWindow` take an optional `ILog`. `ShellViewModel` has an `openAbout` parameter. New option `--log`.
  - **Every app-launching test must pass `--settings`, `--recovery` and `--log` temp paths.**
  - **DEF-007:** `App.ComposeOnStartup` is false in the WPF test thread, so `OnStartup` does nothing there. Any new composition must live in `OnStartup` after that check. The test thread sets `ShutdownMode` after `InitializeComponent`.
  - DEF-006 (Open): the flood test's 10 s limit fails under full-suite load. Fixing it needs a task.

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
1. When the user asks: commit TASK-068. Files:
   - `src/AssemblyStudio.App/{App.xaml.cs, AssemblyStudio.App.csproj, About/*, Logging/*, Documents/WorkspaceViewModel.cs, Documents/Recovery/AutosaveService.cs, Shell/MainWindow.xaml, Shell/MainWindow.xaml.cs, Shell/ShellViewModel.cs, Shell/StartupOptions.cs, Shell/Strings.resx, Shell/Strings.cs}`
   - `tests/AssemblyStudio.App.Tests/{About/*, Logging/*, StaTestThread.cs, StartupTimeTests.cs, Documents/CrashRecoveryProcessTests.cs, Settings/SettingsProcessTests.cs}`
   - `docs/05-planning/backlog.md`, `docs/07-testing/defects.md`, shared memory files
2. User: AC-059 check → TASK-061 Done → `/assembly-implement TASK-064` (then M5). Meanwhile `/assembly-implement TASK-090` or `TASK-101`.
3. User: TASK-006 test VMs, TASK-100 license decision, DEF-003 decision, the version number for the first release.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
