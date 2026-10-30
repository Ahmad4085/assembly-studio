# Handoff

> Read this first, then `CURRENT_TASK.md`, then `PROJECT_STATE.md`. Rewritten at the end of every
> session with meaningful work (`docs/sdlc/agent-protocol.md` §6).

| Field | Value |
|---|---|
| Written | 2026-10-30 12:17 +0500 by Claude Code (Opus 5.5), after completing TASK-063 |
| Current task | **None in progress. M4: TASK-060, 062, 063 Done; TASK-061 Blocked (BLK-002), so TASK-064/065/066/069 wait. Eligible: `TASK-067` settings, `TASK-068` About/diagnostics/logging** |
| Last completed | TASK-063 Crash recovery (Done 2026-10-30 12:17 +0500) |
| Git state | HEAD `a54758d` + uncommitted TASK-063 changes (commit only when the user asks). Not pushed. Untracked, not ours: `.idea/vcs.xml`, `poc/task015-*.ps1` |

## Last verified working state
- `dotnet build AssemblyStudio.slnx -c Release` → **0 warnings, 0 errors** (2026-10-30 12:08 +0500).
- `dotnet test --project tests/AssemblyStudio.App.Tests -c Release --filter-class "*CrashRecoveryProcessTests*"` → passed (4 runs in total).
- `dotnet test --solution AssemblyStudio.slnx -c Release` → **1124/1124 passed**, two consecutive runs (2026-10-30 12:12 and 12:16 +0500).

## Changes since the previous handoff
- **TASK-063 Crash recovery** (Claude Code, uncommitted):
  - `src/AssemblyStudio.App/Documents/Recovery/{RecoveryStore.cs, AutosaveService.cs}`; startup wiring in `App.xaml.cs` (`StartRecovery`, `OfferRecovery`, DispatcherTimer).
  - `StartupOptions` gained `--recovery` and `--autosave-seconds`. **Any test that launches the app must pass `--recovery <temp>`** so it never sees or deletes the user's real recovery folder.
  - `IDialogService.AskRestore`: new fakes must implement it.
  - App-launching tests belong to the `AppProcessTests` collection (`tests/AssemblyStudio.App.Tests/AppProcessTests.cs`).
  - The settings service (TASK-067) could later make the autosave interval a setting; not required.

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
1. When the user asks: commit TASK-063. Files:
   - `src/AssemblyStudio.App/{App.xaml.cs, Documents/Recovery/*, Documents/DocumentViewModel.cs, Documents/WorkspaceViewModel.cs, Documents/IDialogService.cs, Documents/WpfDialogService.cs, Shell/StartupOptions.cs, Shell/Strings.resx, Shell/Strings.cs}`
   - `tests/AssemblyStudio.App.Tests/{AppProcessTests.cs, StartupTimeTests.cs, Documents/RecoveryTests.cs, Documents/CrashRecoveryProcessTests.cs, Documents/WorkspaceViewModelTests.cs}`
   - `docs/05-planning/backlog.md`, shared memory files
2. User: AC-059 check → TASK-061 Done → `/assembly-implement TASK-064`. Meanwhile `/assembly-implement TASK-067` or `TASK-068`.
3. User: TASK-006 test VMs, DEF-003 decision.

## Do not overwrite or revert
`AGENTS.md`, `CLAUDE.md`, `docs/sdlc/**`, `docs/00-project-overview/**`, `docs/templates/**`,
`.claude/skills/**`, `.agent/**`, all root shared memory files (update them per protocol, never
regenerate them), `WORK_LOG.md` history (append only), and the user's `.idea/`, `.mcp.json`, and `poc/task015-*.ps1`.
